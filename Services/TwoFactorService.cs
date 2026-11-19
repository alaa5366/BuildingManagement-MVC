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

    /// <summary>Generates a cryptographically random Base32 secret for an authenticator application.</summary>
    public static string CreateSecret() => TotpAuthenticator.GenerateSecret();

    /// <summary>Creates an otpauth provisioning URI that can be encoded as an authenticator QR code.</summary>
    /// <param name="secret">The Base32 shared secret.</param>
    /// <param name="accountName">The account label displayed by the authenticator.</param>
    /// <param name="issuer">The issuer displayed by the authenticator.</param>
    /// <returns>The URI used to provision a TOTP authenticator.</returns>
    public static string CreateProvisioningUri(string secret, string accountName, string issuer = "BuildingManagement")
    {
        var label = Uri.EscapeDataString(issuer + ":" + accountName);
        return $"otpauth://totp/{label}?secret={Uri.EscapeDataString(secret)}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
    }

    /// <summary>Determines whether an active administrator account has two-factor authentication enabled.</summary>
    /// <param name="uid">The unique user identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the database operation.</param>
    /// <returns><see langword="true"/> when two-factor authentication is enabled; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> IsEnabledAsync(string uid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uid)) return false;
        if (Guid.TryParse(uid, out var accountId))
            return await IsTargetEnabledAsync(accountId, cancellationToken);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Users.AsNoTracking().AnyAsync(
            x => x.Uid == uid && (x.Role == "admin" || x.Role == "superadmin") && x.TwoFactorEnabled,
            cancellationToken);
    }

    /// <summary>Enables two-factor authentication after validating the initial TOTP code and creates one-time recovery codes.</summary>
    /// <param name="uid">The unique administrator identifier.</param>
    /// <param name="secret">The Base32 secret generated for the setup session.</param>
    /// <param name="code">The authenticator code proving possession of the secret.</param>
    /// <param name="cancellationToken">A token used to cancel the database operation.</param>
    /// <returns>The plaintext recovery codes to show once, or <see langword="null"/> when setup cannot be completed.</returns>
    public async Task<IReadOnlyList<string>?> EnableAsync(
        string uid, string secret, string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(secret)) return null;
        if (Guid.TryParse(uid, out var accountId))
            return await EnableTargetAsync(accountId, secret, code, cancellationToken);

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
        if (Guid.TryParse(uid, out var accountId))
            return await VerifyAndConsumeTargetAsync(accountId, code, cancellationToken);

        using var gate = await KeyedLock.AcquireAsync("2fa:" + uid, cancellationToken);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Uid == uid, cancellationToken);
        if (user == null || !TryConsumeCode(user, code)) return false;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Disables two-factor authentication after consuming a valid TOTP or recovery code.</summary>
    /// <param name="uid">The unique administrator identifier.</param>
    /// <param name="code">A current authenticator code or unused recovery code.</param>
    /// <param name="cancellationToken">A token used to cancel the database operation.</param>
    /// <returns><see langword="true"/> when two-factor authentication was disabled; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> DisableAsync(
        string uid, string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(code)) return false;
        if (Guid.TryParse(uid, out var accountId))
            return await DisableTargetAsync(accountId, code, cancellationToken);

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

    private async Task<bool> IsTargetEnabledAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        return await (
            from setting in db.UserTwoFactorSettings.AsNoTracking()
            join account in db.UserAccounts.AsNoTracking() on setting.AccountId equals account.AccountId
            join assignment in db.UserRoleAssignments.AsNoTracking() on account.AccountId equals assignment.AccountId
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            where setting.AccountId == accountId && setting.IsEnabled
                && account.AccountStatus == 1 && !person.IsArchived
                && (assignment.RoleKey == "admin" || assignment.RoleKey == "superadmin")
                && assignment.RevokedAtUtc == null && assignment.ValidFromUtc <= now
                && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
            select setting.AccountId).AnyAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<string>?> EnableTargetAsync(
        Guid accountId, string secret, string code, CancellationToken cancellationToken)
    {
        using var gate = await KeyedLock.AcquireAsync("2fa:" + accountId.ToString("D"), cancellationToken);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var now = DateTime.UtcNow;
        var eligible = await (
            from account in db.UserAccounts
            join person in db.Persons on account.PersonId equals person.PersonId
            join assignment in db.UserRoleAssignments on account.AccountId equals assignment.AccountId
            where account.AccountId == accountId && account.AccountStatus == 1 && !person.IsArchived
                && (assignment.RoleKey == "admin" || assignment.RoleKey == "superadmin")
                && assignment.RevokedAtUtc == null && assignment.ValidFromUtc <= now
                && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
            select account.AccountId).AnyAsync(cancellationToken);
        if (!eligible) return null;

        var setting = await db.UserTwoFactorSettings.SingleOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
        if (setting?.IsEnabled == true) return null;
        long acceptedStep;
        try
        {
            if (!TotpAuthenticator.TryVerify(secret, code, DateTimeOffset.UtcNow, out acceptedStep))
                return null;
        }
        catch (FormatException) { return null; }

        var recoveryCodes = Enumerable.Range(0, 10)
            .Select(_ => FormatRecoveryCode(TotpAuthenticator.EncodeBase32(RandomNumberGenerator.GetBytes(10))))
            .ToArray();
        if (setting == null)
        {
            setting = new UserTwoFactorSettingEntity { AccountId = accountId };
            db.UserTwoFactorSettings.Add(setting);
        }
        setting.IsEnabled = true;
        setting.SecretCiphertext = Encoding.UTF8.GetBytes(_encryption.Encrypt(secret));
        setting.KeyVersion = "aes-gcm-v1";
        setting.LastAcceptedTimeStep = acceptedStep;
        setting.EnrolledAtUtc = now;
        setting.LastVerifiedAtUtc = now;
        setting.UpdatedAtUtc = now;
        foreach (var recoveryCode in recoveryCodes)
        {
            db.UserRecoveryCodes.Add(new UserRecoveryCodeEntity
            {
                RecoveryCodeId = Guid.NewGuid(),
                AccountId = accountId,
                CodeHash = SHA256.HashData(Encoding.UTF8.GetBytes(NormalizeRecoveryCode(recoveryCode))),
                CreatedAtUtc = now
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        _logger.LogInformation("Two-factor authentication enabled for target administrator account {AccountId}", accountId);
        return recoveryCodes;
    }

    private async Task<bool> VerifyAndConsumeTargetAsync(
        Guid accountId, string code, CancellationToken cancellationToken)
    {
        using var gate = await KeyedLock.AcquireAsync("2fa:" + accountId.ToString("D"), cancellationToken);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var now = DateTime.UtcNow;
        var setting = await db.UserTwoFactorSettings.SingleOrDefaultAsync(
            x => x.AccountId == accountId && x.IsEnabled, cancellationToken);
        if (setting == null || !await IsEligibleTargetAdminAsync(db, accountId, now, cancellationToken))
            return false;

        var normalized = NormalizeRecoveryCode(code);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        var recovery = await db.UserRecoveryCodes
            .Where(x => x.AccountId == accountId && x.UsedAtUtc == null)
            .ToListAsync(cancellationToken);
        var matched = recovery.FirstOrDefault(x =>
            CryptographicOperations.FixedTimeEquals(x.CodeHash, hash));
        if (matched != null)
        {
            matched.UsedAtUtc = now;
        }
        else
        {
            if (setting.SecretCiphertext == null || setting.KeyVersion != "aes-gcm-v1")
                return false;
            string secret;
            try { secret = _encryption.Decrypt(Encoding.UTF8.GetString(setting.SecretCiphertext)); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to decrypt target two-factor secret for account {AccountId}", accountId);
                return false;
            }
            try
            {
                if (!TotpAuthenticator.TryVerify(secret, code, DateTimeOffset.UtcNow, out var step)
                    || (setting.LastAcceptedTimeStep.HasValue && step <= setting.LastAcceptedTimeStep.Value))
                    return false;
                setting.LastAcceptedTimeStep = step;
            }
            catch (FormatException) { return false; }
        }
        setting.LastVerifiedAtUtc = now;
        setting.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<bool> DisableTargetAsync(Guid accountId, string code, CancellationToken cancellationToken)
    {
        using var gate = await KeyedLock.AcquireAsync("2fa:" + accountId.ToString("D"), cancellationToken);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var now = DateTime.UtcNow;
        var setting = await db.UserTwoFactorSettings.SingleOrDefaultAsync(
            x => x.AccountId == accountId && x.IsEnabled, cancellationToken);
        if (setting == null || !await IsEligibleTargetAdminAsync(db, accountId, now, cancellationToken))
            return false;

        var normalized = NormalizeRecoveryCode(code);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        var recovery = await db.UserRecoveryCodes
            .Where(x => x.AccountId == accountId && x.UsedAtUtc == null)
            .ToListAsync(cancellationToken);
        var matched = recovery.FirstOrDefault(x => CryptographicOperations.FixedTimeEquals(x.CodeHash, hash));
        if (matched != null) matched.UsedAtUtc = now;
        else
        {
            if (setting.SecretCiphertext == null || setting.KeyVersion != "aes-gcm-v1") return false;
            string secret;
            try { secret = _encryption.Decrypt(Encoding.UTF8.GetString(setting.SecretCiphertext)); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to decrypt target two-factor secret for account {AccountId}", accountId);
                return false;
            }
            try
            {
                if (!TotpAuthenticator.TryVerify(secret, code, DateTimeOffset.UtcNow, out var step)
                    || (setting.LastAcceptedTimeStep.HasValue && step <= setting.LastAcceptedTimeStep.Value))
                    return false;
                setting.LastAcceptedTimeStep = step;
            }
            catch (FormatException) { return false; }
        }
        setting.IsEnabled = false;
        setting.SecretCiphertext = null;
        setting.KeyVersion = null;
        setting.EnrolledAtUtc = null;
        setting.LastAcceptedTimeStep = null;
        setting.LastVerifiedAtUtc = now;
        setting.UpdatedAtUtc = now;
        await db.UserRecoveryCodes.Where(x => x.AccountId == accountId && x.UsedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAtUtc, now), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static async Task<bool> IsEligibleTargetAdminAsync(
        AppDbContext db, Guid accountId, DateTime now, CancellationToken cancellationToken) =>
        await (from account in db.UserAccounts
               join person in db.Persons on account.PersonId equals person.PersonId
               join assignment in db.UserRoleAssignments on account.AccountId equals assignment.AccountId
               where account.AccountId == accountId && account.AccountStatus == 1 && !person.IsArchived
                   && (assignment.RoleKey == "admin" || assignment.RoleKey == "superadmin")
                   && assignment.RevokedAtUtc == null && assignment.ValidFromUtc <= now
                   && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
               select assignment.RoleAssignmentId).AnyAsync(cancellationToken);

    private static string NormalizeRecoveryCode(string code) =>
        new((code ?? "").ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

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
