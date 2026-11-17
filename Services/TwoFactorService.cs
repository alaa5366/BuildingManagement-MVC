using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Manages opt-in TOTP two-factor authentication for administrator accounts.</summary>
public sealed class TwoFactorService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(
        IDbContextFactory<AppDbContext> factory,
        IEncryptionService encryption,
        ILogger<TwoFactorService> logger)
    {
        _factory = factory;
        _encryption = encryption;
        _logger = logger;
    }

    public static string CreateSecret() => TotpAuthenticator.GenerateSecret();

    public static string CreateProvisioningUri(string secret, string accountName, string issuer = "BuildingManagement")
    {
        var label = Uri.EscapeDataString(issuer + ":" + accountName);
        return $"otpauth://totp/{label}?secret={Uri.EscapeDataString(secret)}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
    }

    public async Task<bool> IsEnabledAsync(string uid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uid)) return false;
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Users.AsNoTracking().AnyAsync(
            x => x.Uid == uid && (x.Role == "admin" || x.Role == "superadmin") && x.TwoFactorEnabled,
            cancellationToken);
    }

    public async Task<IReadOnlyList<string>?> EnableAsync(
        string uid, string secret, string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(secret)) return null;

        using var gate = await KeyedLock.AcquireAsync("2fa:" + uid, cancellationToken);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Uid == uid, cancellationToken);
        if (user == null || user.Role is not ("admin" or "superadmin")
            || !user.IsActive || user.IsDisabled || user.TwoFactorEnabled)
            return null;

        long acceptedStep;
        try
        {
            if (!TotpAuthenticator.TryVerify(secret, code, DateTimeOffset.UtcNow, out acceptedStep))
                return null;
        }
        catch (FormatException)
        {
            return null;
        }

        var recoveryCodes = Enumerable.Range(0, 10)
            .Select(_ => FormatRecoveryCode(TotpAuthenticator.EncodeBase32(RandomNumberGenerator.GetBytes(10))))
            .ToArray();

        user.TwoFactorSecretEncrypted = _encryption.Encrypt(secret);
        user.TwoFactorEnabled = true;
        user.TwoFactorLastUsedStep = acceptedStep;
        user.TwoFactorRecoveryCodesJson = JsonSerializer.Serialize(recoveryCodes.Select(HashRecoveryCode));
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Two-factor authentication enabled for administrator {Uid}", uid);
        return recoveryCodes;
    }

    /// <summary>Verifies and consumes a TOTP value or a one-time recovery code.</summary>
    public async Task<bool> VerifyAndConsumeAsync(
        string uid, string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(code)) return false;

        using var gate = await KeyedLock.AcquireAsync("2fa:" + uid, cancellationToken);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Uid == uid, cancellationToken);
        if (user == null || !TryConsumeCode(user, code)) return false;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DisableAsync(
        string uid, string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(code)) return false;

        using var gate = await KeyedLock.AcquireAsync("2fa:" + uid, cancellationToken);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Uid == uid, cancellationToken);
        if (user == null || !TryConsumeCode(user, code)) return false;

        user.TwoFactorEnabled = false;
        user.TwoFactorSecretEncrypted = null;
        user.TwoFactorRecoveryCodesJson = null;
        user.TwoFactorLastUsedStep = null;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Two-factor authentication disabled for administrator {Uid}", uid);
        return true;
    }

    private bool TryConsumeCode(UserEntity user, string code)
    {
        if (!user.IsActive || user.IsDisabled || user.Role is not ("admin" or "superadmin")
            || !user.TwoFactorEnabled)
            return false;

        // Recovery codes are independent of the encrypted TOTP secret so they still
        // provide a recovery path if the encryption key is rotated or ciphertext is damaged.
        var hashes = DeserializeRecoveryHashes(user.TwoFactorRecoveryCodesJson);
        var candidate = HashRecoveryCode(code);
        var matchedIndex = -1;
        for (var i = 0; i < hashes.Count; i++)
        {
            if (FixedEquals(hashes[i], candidate))
                matchedIndex = i;
        }

        if (matchedIndex >= 0)
        {
            hashes.RemoveAt(matchedIndex);
            user.TwoFactorRecoveryCodesJson = JsonSerializer.Serialize(hashes);
            return true;
        }

        if (string.IsNullOrWhiteSpace(user.TwoFactorSecretEncrypted))
            return false;

        string secret;
        try
        {
            secret = _encryption.Decrypt(user.TwoFactorSecretEncrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to decrypt two-factor secret for user {Uid}", user.Uid);
            return false;
        }

        try
        {
            if (!TotpAuthenticator.TryVerify(secret, code, DateTimeOffset.UtcNow, out var acceptedStep))
                return false;
            if (user.TwoFactorLastUsedStep.HasValue && acceptedStep <= user.TwoFactorLastUsedStep.Value)
                return false;

            user.TwoFactorLastUsedStep = acceptedStep;
            return true;
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Stored two-factor secret is malformed for user {Uid}", user.Uid);
            return false;
        }
    }

    private static List<string> DeserializeRecoveryHashes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
        catch (JsonException) { return new List<string>(); }
    }

    private static string FormatRecoveryCode(string value)
    {
        var normalized = new string(value.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        return string.Join("-", Enumerable.Range(0, (normalized.Length + 3) / 4)
            .Select(i => normalized.Substring(i * 4, Math.Min(4, normalized.Length - i * 4))));
    }

    private static string HashRecoveryCode(string code)
    {
        var normalized = new string((code ?? "").ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private static bool FixedEquals(string left, string right)
    {
        try
        {
            return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(left), Convert.FromHexString(right));
        }
        catch (FormatException) { return false; }
    }
}

/// <summary>RFC 6238 TOTP primitives using Base32 secrets and six-digit SHA-1 codes.</summary>
internal static class TotpAuthenticator
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    internal static string GenerateSecret(int byteCount = 20) =>
        EncodeBase32(RandomNumberGenerator.GetBytes(byteCount));

    internal static string GenerateCode(string secret, DateTimeOffset timestamp)
    {
        var key = DecodeBase32(secret);
        var counter = (ulong)(timestamp.ToUnixTimeSeconds() / 30);
        Span<byte> message = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            message[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(message.ToArray());
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
            | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8)
            | (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static bool TryVerify(string secret, string? code, DateTimeOffset timestamp, out long acceptedStep)
    {
        acceptedStep = -1;
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6 || code.Any(c => c is < '0' or > '9'))
            return false;

        var currentStep = timestamp.ToUnixTimeSeconds() / 30;
        var supplied = Encoding.ASCII.GetBytes(code);
        for (var step = currentStep - 1; step <= currentStep + 1; step++)
        {
            if (step < 0) continue;
            var candidate = Encoding.ASCII.GetBytes(
                GenerateCode(secret, DateTimeOffset.FromUnixTimeSeconds(step * 30)));
            if (CryptographicOperations.FixedTimeEquals(candidate, supplied))
            {
                acceptedStep = step;
                return true;
            }
        }
        return false;
    }

    internal static string EncodeBase32(ReadOnlySpan<byte> data)
    {
        var output = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var value in data)
        {
            buffer = (buffer << 8) | value;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                output.Append(Alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }
        if (bitsLeft > 0) output.Append(Alphabet[(buffer << (5 - bitsLeft)) & 31]);
        return output.ToString();
    }

    internal static byte[] DecodeBase32(string value)
    {
        var normalized = new string((value ?? "").Where(c => !char.IsWhiteSpace(c) && c != '=').ToArray())
            .ToUpperInvariant();
        var output = new List<byte>(normalized.Length * 5 / 8);
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var c in normalized)
        {
            var index = Alphabet.IndexOf(c);
            if (index < 0) throw new FormatException("Invalid Base32 secret.");
            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                output.Add((byte)((buffer >> (bitsLeft - 8)) & 0xff));
                bitsLeft -= 8;
            }
        }
        if (output.Count < 16) throw new FormatException("Base32 secret is too short.");
        return output.ToArray();
    }
}
