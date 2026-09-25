using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Services;

public class QrSecurityService
{
    private readonly FirestoreDb _db;
    private readonly string _secretKey;
    private readonly ILogger<QrSecurityService> _logger;

    public QrSecurityService(FirestoreContext ctx, IConfiguration config, ILogger<QrSecurityService> logger)
    {
        _db = ctx.Db;
        _secretKey = config["Qr:SecretKey"] ?? config["Excel:SecretKey"] ?? "CHANGE_ME";
        _logger = logger;
    }

    // ============================================================
    // 1. تشفير QR الثابت (Stable QR)
    // ============================================================
    // بنوقّع على (path + query) عشان يبقى ثابت مهما كان ترتيب الـ parameters
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
    // 2. QR السريع (Quick QR)
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
            TokenId = Guid.NewGuid().ToString("N")
        };

        // 1. تشفير الـ payload
        var json = JsonSerializer.Serialize(payload);
        var encrypted = EncryptAes(json);

        // 2. توقيع للتحقق
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(encrypted));
        var signature = Convert.ToHexString(hash).ToLower()[..16];

        return $"{encrypted}.{signature}";
    }

    public async Task<QuickTokenValidation> ValidateQuickToken(string token, string? deviceFingerprint = null)
    {
        // 1. تحليل الـ token
        var parts = token.Split('.');
        if (parts.Length != 2)
            return QuickTokenValidation.Fail("Token غير صالح");

        var encrypted = parts[0];
        var signature = parts[1];

        // 2. التحقق من التوقيع
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(encrypted));
        var expectedSig = Convert.ToHexString(hash).ToLower()[..16];
        if (signature != expectedSig)
            return QuickTokenValidation.Fail("Token تم التلاعب به");

        // 3. فك التشفير
        QuickTokenPayload payload;
        try
        {
            var json = DecryptAes(encrypted);
            payload = JsonSerializer.Deserialize<QuickTokenPayload>(json)!;
        }
        catch
        {
            return QuickTokenValidation.Fail("Token تالف");
        }

        // 4. التحقق من الصلاحية
        if (DateTime.UtcNow > DateTime.Parse(payload.ExpiresAt))
            return QuickTokenValidation.Fail("انتهت صلاحية الـ QR");

        // 5. التحقق من الاستخدام
        var usageDoc = await _db.Collection("qr_usage").Document(payload.TokenId).GetSnapshotAsync();

        if (usageDoc.Exists)
        {
            var usage = usageDoc.ConvertTo<QrUsage>();

            if (payload.OneTime && usage.UseCount >= 1)
                return QuickTokenValidation.Fail("الـ QR ده اتستخدم قبل كده");

            if (payload.MaxUses > 0 && usage.UseCount >= payload.MaxUses)
                return QuickTokenValidation.Fail("الـ QR ده وصل للحد الأقصى للاستخدام");

            if (!string.IsNullOrEmpty(deviceFingerprint) &&
                !string.IsNullOrEmpty(usage.DeviceFingerprint) &&
                usage.DeviceFingerprint != deviceFingerprint)
                return QuickTokenValidation.Fail("الـ QR ده مرتبط بجهاز تاني");
        }

        return QuickTokenValidation.Ok(payload);
    }

    public async Task MarkQuickTokenUsed(string tokenId, string? deviceFingerprint = null)
    {
        var docRef = _db.Collection("qr_usage").Document(tokenId);
        var doc = await docRef.GetSnapshotAsync();

        var usage = doc.Exists
            ? doc.ConvertTo<QrUsage>()
            : new QrUsage { TokenId = tokenId, FirstUsedAt = DateTime.UtcNow.ToString("o") };

        usage.UseCount++;
        usage.LastUsedAt = DateTime.UtcNow.ToString("o");

        if (!string.IsNullOrEmpty(deviceFingerprint))
            usage.DeviceFingerprint = deviceFingerprint;

        await docRef.SetAsync(usage);
    }

    // ============================================================
    // AES Encryption
    // ============================================================
    private string EncryptAes(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_secretKey.PadRight(32).Substring(0, 32));
        aes.IV = new byte[16];

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return Convert.ToBase64String(cipherBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private string DecryptAes(string cipherText)
    {
        var padded = cipherText.Replace("-", "+").Replace("_", "/");
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_secretKey.PadRight(32).Substring(0, 32));
        aes.IV = new byte[16];

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(padded);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}

// ============================================================
// Models
// ============================================================
public class QuickQrOptions
{
    public string BuildingId { get; set; } = "";
    public string AptId { get; set; } = "";
    public int AptNumber { get; set; }
    public int FloorOrder { get; set; }
    public TimeSpan Validity { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxUses { get; set; } = 1;
    public bool OneTime { get; set; } = true;
}

[FirestoreData]
public class QuickTokenPayload
{
    [FirestoreProperty("buildingId")] public string BuildingId { get; set; } = "";
    [FirestoreProperty("aptId")] public string AptId { get; set; } = "";
    [FirestoreProperty("aptNumber")] public int AptNumber { get; set; }
    [FirestoreProperty("floorOrder")] public int FloorOrder { get; set; }
    [FirestoreProperty("expiresAt")] public string ExpiresAt { get; set; } = "";
    [FirestoreProperty("maxUses")] public int MaxUses { get; set; }
    [FirestoreProperty("oneTime")] public bool OneTime { get; set; }
    [FirestoreProperty("tokenId")] public string TokenId { get; set; } = "";
}

[FirestoreData]
public class QrUsage
{
    [FirestoreProperty("tokenId")] public string TokenId { get; set; } = "";
    [FirestoreProperty("useCount")] public int UseCount { get; set; }
    [FirestoreProperty("firstUsedAt")] public string FirstUsedAt { get; set; } = "";
    [FirestoreProperty("lastUsedAt")] public string LastUsedAt { get; set; } = "";
    [FirestoreProperty("deviceFingerprint")] public string? DeviceFingerprint { get; set; }
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