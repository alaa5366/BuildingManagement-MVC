using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>
/// خدمة توليد والتحقق من Quick QR Tokens (v2 — AES-GCM).
/// </summary>
/// <remarks>
/// <para>
/// <b>أنواع التوكنات:</b>
/// </para>
/// <list type="bullet">
///   <item><b>Stable QR:</b> HMAC-SHA256 لتوقيع روابط ثابتة (زي QR الباب).</item>
///   <item><b>Quick QR (v2):</b> AES-GCM (تشفير + توثيق) لتوكنات الدخول السريع.</item>
/// </list>
/// <para>
/// <b>⚠️ الأمان:</b> التوكنات القديمة (v1) بطلت. لازم تولّد توكنات جديدة بعد الترقية.
/// </para>
/// </remarks>
/// <seealso cref="QuickQrOptions"/>
/// <seealso cref="QuickTokenPayload"/>
public class QrSecurityService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly string _secretKey;
    private readonly byte[] _quickKey;
    private readonly ILogger<QrSecurityService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="QrSecurityService"/> class.
    /// </summary>
    /// <param name="sqlFactory">Factory لفتح DbContext (لاستخدام QrUsages).</param>
    /// <param name="config">الـ Configuration (يقرأ منه <c>Qr:SecretKey</c>).</param>
    /// <param name="logger">الـ logger.</param>
    /// <exception cref="InvalidOperationException">
    /// لو <c>Qr:SecretKey</c> مش موجود في الـ configuration.
    /// </exception>
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

    /// <summary>
    /// يوقّع رابط Stable QR بـ HMAC-SHA256.
    /// </summary>
    /// <param name="path">مسار الرابط (مثال: <c>/Account/Qr</c>).</param>
    /// <param name="query">الـ query string (مثال: <c>b=1&amp;a=2</c>).</param>
    /// <returns>توقيع hex بأول 16 حرف.</returns>
    public string SignStableQr(string path, string query)
    {
        var data = $"{path}?{query}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower()[..16];
    }

    /// <summary>
    /// يتحقق من صحة توقيع Stable QR.
    /// </summary>
    /// <param name="path">مسار الرابط.</param>
    /// <param name="query">الـ query string.</param>
    /// <param name="sig">التوقيع المستلم.</param>
    /// <returns><c>true</c> لو التوقيع مطابق.</returns>
    public bool VerifyStableQr(string path, string query, string sig)
    {
        if (string.IsNullOrEmpty(sig)) return false;
        var expected = SignStableQr(path, query);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(sig));
    }

    /// <summary>
    /// يولد Quick QR Token (v2).
    /// </summary>
    /// <param name="options">خيارات التوكن (الصلاحية، الاستخدام، إلخ).</param>
    /// <returns>التوكن جاهز للاستخدام (يبدأ بـ <c>v2.</c>).</returns>
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

        var json = JsonSerializer.Serialize(payload);
        return "v2." + SealQuick(json);
    }

    /// <summary>
    /// يتحقق من صحة Quick QR Token (بدون استهلاكه).
    /// </summary>
    /// <param name="token">التوكن (يبدأ بـ <c>v2.</c>).</param>
    /// <param name="deviceFingerprint">بصمة الجهاز (اختياري).</param>
    /// <returns>نتيجة التحقق مع الـ payload لو صحيح.</returns>
    /// <remarks>
    /// <para>
    /// <b>⚠️ ملاحظة:</b> الدالة دي ما بتستهلكش التوكن. استخدم
    /// <see cref="TryConsumeQuickTokenAsync"/> عشان تستهلكه atomic.
    /// </para>
    /// </remarks>
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

        if (!DateTime.TryParse(payload.ExpiresAt, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var expiresAt))
            return QuickTokenValidation.Fail(Loc.T("Corrupted_Token"));

        if (DateTime.UtcNow > expiresAt.ToUniversalTime())
            return QuickTokenValidation.Fail(Loc.T("The_QR_Has_Expired"));

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

    /// <summary>
    /// يستهلك Quick QR Token بطريقة atomic (يمنع double-use).
    /// </summary>
    /// <param name="p">الـ payload الخاص بالتوكن.</param>
    /// <param name="deviceFingerprint">بصمة الجهاز (اختياري).</param>
    /// <returns>
    /// <c>true</c> لو الاستهلاك نجح؛ <c>false</c> لو التوكن وصل الحد الأقصى.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>⚠️ الأمان:</b> الدالة دي بتستخدم <c>UPDATE ... WHERE UseCount &lt; limit</c>
    /// عشان تمنع race condition لما طلبين يوصلوا في نفس اللحظة.
    /// </para>
    /// </remarks>
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

        if (await db.QrUsages.AnyAsync(x => x.TokenId == p.TokenId)) return false;

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

    /// <summary>
    /// يشفّر النص بـ AES-GCM (nonce عشوائي لكل مرة).
    /// </summary>
    /// <param name="plainText">النص العادي.</param>
    /// <returns>نص مشفّر بصيغة base64url.</returns>
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

    /// <summary>
    /// يفك تشفير النص المشفّر (بيرمي لو التوكن معدّل).
    /// </summary>
    /// <param name="token">النص المشفّر (بدون prefix <c>v2.</c>).</param>
    /// <returns>النص العادي.</returns>
    /// <exception cref="CryptographicException">
    /// لو النص معدّل أو المفتاح مختلف.
    /// </exception>
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
        gcm.Decrypt(nonce, cipher, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }
}

/// <summary>
/// خيارات توليد Quick QR Token.
/// </summary>
public class QuickQrOptions
{
    /// <summary>معرّف العمارة.</summary>
    public string BuildingId { get; set; } = "";

    /// <summary>معرّف الشقة.</summary>
    public string AptId { get; set; } = "";

    /// <summary>رقم الشقة (للعرض).</summary>
    public int AptNumber { get; set; }

    /// <summary>ترتيب الدور (للعرض).</summary>
    public int FloorOrder { get; set; }

    /// <summary>مدة صلاحية التوكن. الافتراضي: 5 دقائق.</summary>
    public TimeSpan Validity { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>أقصى عدد استخدامات. الافتراضي: 1.</summary>
    public int MaxUses { get; set; } = 1;

    /// <summary>لو <c>true</c>، التوكن يبقى مرة واحدة فقط.</summary>
    public bool OneTime { get; set; } = true;

    /// <summary>
    /// الهدف من التوكن: <c>"login"</c> للدخول السريع، <c>"setpin"</c> لتعيين PIN.
    /// </summary>
    public string Purpose { get; set; } = "login";
}

/// <summary>
/// محتوى Quick QR Token بعد فك التشفير.
/// </summary>
public class QuickTokenPayload
{
    /// <summary>معرّف العمارة.</summary>
    public string BuildingId { get; set; } = "";

    /// <summary>معرّف الشقة.</summary>
    public string AptId { get; set; } = "";

    /// <summary>رقم الشقة (للعرض).</summary>
    public int AptNumber { get; set; }

    /// <summary>ترتيب الدور (للعرض).</summary>
    public int FloorOrder { get; set; }

    /// <summary>تاريخ الانتهاء (UTC بصيغة "o").</summary>
    public string ExpiresAt { get; set; } = "";

    /// <summary>أقصى عدد استخدامات.</summary>
    public int MaxUses { get; set; }

    /// <summary>هل التوكن مرة واحدة؟</summary>
    public bool OneTime { get; set; }

    /// <summary>معرّف فريد للتوكن (يُستخدم كـ PK في QrUsages).</summary>
    public string TokenId { get; set; } = "";

    /// <summary>
    /// الهدف: <c>"login"</c> أو <c>"setpin"</c>.
    /// </summary>
    public string Purpose { get; set; } = "login";
}

/// <summary>
/// نتيجة التحقق من Quick QR Token.
/// </summary>
public class QuickTokenValidation
{
    /// <summary>هل التوكن صحيح؟</summary>
    public bool IsSuccess { get; set; }

    /// <summary>رسالة الخطأ (لو فشل).</summary>
    public string? Error { get; set; }

    /// <summary>الـ payload (لو نجح).</summary>
    public QuickTokenPayload? Data { get; set; }

    /// <summary>يرجع نتيجة نجاح.</summary>
    public static QuickTokenValidation Ok(QuickTokenPayload data) =>
        new() { IsSuccess = true, Data = data };

    /// <summary>يرجع نتيجة فشل.</summary>
    public static QuickTokenValidation Fail(string error) =>
        new() { IsSuccess = false, Error = error };
}

/// <summary>
/// نتيجة فك تشفير Quick QR Token.
/// </summary>
public class QrUsage
{
    public string TokenId { get; set; } = "";
    public int UseCount { get; set; }
    public string FirstUsedAt { get; set; } = "";
    public string LastUsedAt { get; set; } = "";
    public string? DeviceFingerprint { get; set; }
}