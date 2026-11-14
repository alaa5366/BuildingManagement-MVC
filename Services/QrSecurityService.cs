using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class QrSecurityService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly string _secretKey;
    // مفتاح تشفير/توثيق مشتق (منفصل عن مفتاح التوقيع الثابت) — v2
    private readonly byte[] _quickKey;
    private readonly ILogger<QrSecurityService> _logger;

    public QrSecurityService(
        IDbContextFactory<AppDbContext> sqlFactory,
        IConfiguration config,
        ILogger<QrSecurityService> logger)
    {
        _sqlFactory = sqlFactory;
        _secretKey = ConfigSecrets.Require(config, "Qr:SecretKey", "Excel:SecretKey");
        _quickKey = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_secretKey),
            Encoding.UTF8.GetBytes("qr-quick-token-v2"));
        _logger = logger;
    }

    // ============================================================
    // Stable QR
    // ============================================================
    public string SignStableQr(string path, string query)
    {
        var data = $"{path}?{query}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower()[..16];
    }

    public bool VerifyStableQr(string path, string query, string sig)
    {
        if (string.IsNullOrEmpty(sig)) return false;
        var expected = SignStableQr(path, query);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(sig));
    }

    // ============================================================
    // Quick QR
    // ============================================================
    public string GenerateQuickToken(QuickQrOptions options)
    {
        var payload = new QuickTokenPayload
        {
            BuildingId = options.BuildingId,
            AptId = options.AptId,
            AptNumber = options.AptNumber,
            FloorOrder = options.FloorOrder,
            ExpiresAt = DateTime.UtcNow.Add(options.Validity).ToString("o"),
            MaxUses = options.MaxUses,
            OneTime = options.OneTime,
            TokenId = Guid.NewGuid().ToString("N"),
            Purpose = options.Purpose
        };

        // v2: AES-GCM (تشفير + توثيق في خطوة واحدة) بمفتاح مشتق ومعاه nonce عشوائي
        var json = JsonSerializer.Serialize(payload);
        return "v2." + SealQuick(json);
    }

    public async Task<QuickTokenValidation> ValidateQuickToken(string token, string? deviceFingerprint = null)
    {
        if (string.IsNullOrEmpty(token) || !token.StartsWith("v2.", StringComparison.Ordinal))
            return QuickTokenValidation.Fail(Loc.T("Invalid_Token"));

        QuickTokenPayload payload;
        try
        {
            var json = OpenQuick(token.Substring(3));
            payload = JsonSerializer.Deserialize<QuickTokenPayload>(json)!;
        }
        catch (CryptographicException)
        {
            return QuickTokenValidation.Fail(Loc.T("The_Token_Has_Been_Tampered_With"));
        }
        catch
        {
            return QuickTokenValidation.Fail(Loc.T("Corrupted_Token"));
        }

        // وقت الانتهاء محفوظ UTC بصيغة "o" — نقراه RoundtripKind عشان توقيت السيرفر مايأثرش
        if (!DateTime.TryParse(payload.ExpiresAt, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var expiresAt))
            return QuickTokenValidation.Fail(Loc.T("Corrupted_Token"));

        if (DateTime.UtcNow > expiresAt.ToUniversalTime())
            return QuickTokenValidation.Fail(Loc.T("The_QR_Has_Expired"));

        // ✅ استخدام SQL بدل Firestore
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var usage = await db.QrUsages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenId == payload.TokenId);

        if (usage != null)
        {
            if (payload.OneTime && usage.UseCount >= 1)
                return QuickTokenValidation.Fail(Loc.T("This_QR_Has_Already_Been_Used"));

            if (payload.MaxUses > 0 && usage.UseCount >= payload.MaxUses)
                return QuickTokenValidation.Fail(Loc.T("This_QR_Has_Reached_Its_Maximum"));

            if (!string.IsNullOrEmpty(deviceFingerprint) &&
                !string.IsNullOrEmpty(usage.DeviceFingerprint) &&
                usage.DeviceFingerprint != deviceFingerprint)
                return QuickTokenValidation.Fail(Loc.T("This_QR_Is_Linked_To_Another"));
        }

        return QuickTokenValidation.Ok(payload);
    }

    public async Task MarkQuickTokenUsed(string tokenId, string? deviceFingerprint = null)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var usage = await db.QrUsages.FirstOrDefaultAsync(x => x.TokenId == tokenId);

        if (usage == null)
        {
            db.QrUsages.Add(new QrUsageEntity
            {
                Id = tokenId,
                TokenId = tokenId,
                UseCount = 1,
                FirstUsedAt = DateTime.UtcNow.ToString("o"),
                LastUsedAt = DateTime.UtcNow.ToString("o"),
                DeviceFingerprint = deviceFingerprint,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            usage.UseCount++;
            usage.LastUsedAt = DateTime.UtcNow.ToString("o");
            if (!string.IsNullOrEmpty(deviceFingerprint))
                usage.DeviceFingerprint = deviceFingerprint;
        }

        await db.SaveChangesAsync();
    }

    // ============================================================
    // استهلاك atomic: UPDATE ... WHERE UseCount < الحد الأقصى
    // (يمنع إن توكن "مرة واحدة" يتستخدم مرتين لو طلبين وصلوا مع بعض)
    // ============================================================
    public async Task<bool> TryConsumeQuickTokenAsync(QuickTokenPayload p, string? deviceFingerprint = null)
    {
        var limit = p.OneTime ? 1 : (p.MaxUses > 0 ? p.MaxUses : int.MaxValue);
        var now = DateTime.UtcNow.ToString("o");

        await using var db = await _sqlFactory.CreateDbContextAsync();

        var updated = await db.QrUsages
            .Where(x => x.TokenId == p.TokenId && x.UseCount < limit)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.UseCount, x => x.UseCount + 1)
                .SetProperty(x => x.LastUsedAt, now));
        if (updated == 1) return true;

        // موجود وواصل للحد الأقصى
        if (await db.QrUsages.AnyAsync(x => x.TokenId == p.TokenId)) return false;

        // أول استخدام: لو طلب تاني سبقنا بالـ insert (نفس الـ PK) نرجع نجرب الـ UPDATE مرة
        try
        {
            db.QrUsages.Add(new QrUsageEntity
            {
                Id = p.TokenId,
                TokenId = p.TokenId,
                UseCount = 1,
                FirstUsedAt = now,
                LastUsedAt = now,
                DeviceFingerprint = deviceFingerprint,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            updated = await db.QrUsages
                .Where(x => x.TokenId == p.TokenId && x.UseCount < limit)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.UseCount, x => x.UseCount + 1)
                    .SetProperty(x => x.LastUsedAt, now));
            return updated == 1;
        }
    }

    // ============================================================
    // AES-GCM  (nonce[12] | tag[16] | ciphertext) → base64url
    // ============================================================
    private string SealQuick(string plainText)
    {
        var plain = Encoding.UTF8.GetBytes(plainText);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var cipher = new byte[plain.Length];

        using var gcm = new AesGcm(_quickKey, 16);
        gcm.Encrypt(nonce, plain, cipher, tag);

        var all = new byte[12 + 16 + cipher.Length];
        Buffer.BlockCopy(nonce, 0, all, 0, 12);
        Buffer.BlockCopy(tag, 0, all, 12, 16);
        Buffer.BlockCopy(cipher, 0, all, 28, cipher.Length);

        return Convert.ToBase64String(all).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private string OpenQuick(string token)
    {
        var padded = token.Replace("-", "+").Replace("_", "/");
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        var all = Convert.FromBase64String(padded);
        if (all.Length < 28) throw new CryptographicException("short token");

        var nonce = all.AsSpan(0, 12);
        var tag = all.AsSpan(12, 16);
        var cipher = all.AsSpan(28);
        var plain = new byte[cipher.Length];

        using var gcm = new AesGcm(_quickKey, 16);
        gcm.Decrypt(nonce, cipher, tag, plain);   // بيرمي CryptographicException لو اتعدّل

        return Encoding.UTF8.GetString(plain);
    }
}

// Models
public class QuickQrOptions
{
    public string BuildingId { get; set; } = "";
    public string AptId { get; set; } = "";
    public int AptNumber { get; set; }
    public int FloorOrder { get; set; }
    public TimeSpan Validity { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxUses { get; set; } = 1;
    public bool OneTime { get; set; } = true;

    // "login" (دخول سريع) أو "setpin" (رابط تعيين PIN) — التوكنين مش بيتبدلوا ببعض
    public string Purpose { get; set; } = "login";
}

public class QuickTokenPayload
{
    public string BuildingId { get; set; } = "";
    public string AptId { get; set; } = "";
    public int AptNumber { get; set; }
    public int FloorOrder { get; set; }
    public string ExpiresAt { get; set; } = "";
    public int MaxUses { get; set; }
    public bool OneTime { get; set; }
    public string TokenId { get; set; } = "";
    public string Purpose { get; set; } = "login";   // التوكنات القديمة من غير Purpose = login
}

public class QrUsage
{
    public string TokenId { get; set; } = "";
    public int UseCount { get; set; }
    public string FirstUsedAt { get; set; } = "";
    public string LastUsedAt { get; set; } = "";
    public string? DeviceFingerprint { get; set; }
}

public class QuickTokenValidation
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public QuickTokenPayload? Data { get; set; }

    public static QuickTokenValidation Ok(QuickTokenPayload data) =>
        new() { IsSuccess = true, Data = data };

    public static QuickTokenValidation Fail(string error) =>
        new() { IsSuccess = false, Error = error };
}