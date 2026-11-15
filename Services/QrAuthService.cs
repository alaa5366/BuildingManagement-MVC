using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/features/qr-auth.js (توليد والتحقق من توكن الدخول السريع)
/// <summary>
/// Generates and verifies signed access tokens used by stable QR codes.
/// </summary>
public class QrAuthService
{
    // المفتاح السري من الـ configuration (Qr:TokenSecretKey أو Qr:SecretKey) — مش مكتوب في الكود.
    // ⚠️ التوكنات القديمة اللي اتولّدت بالمفتاح المكتوب في الكود هتبطل (مدتها قصيرة أصلاً).
    private readonly byte[] _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="QrAuthService"/> class.
    /// </summary>
    public QrAuthService(IConfiguration config)
    {
        _key = Encoding.UTF8.GetBytes(ConfigSecrets.Require(config, "Qr:TokenSecretKey", "Qr:SecretKey"));
    }

    private string Sign(string data)
    {
        using var hmac = new HMACSHA256(_key);
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private string SignLegacy(string data) => Sign(data)[..16];

    /// <summary>
    /// Generates the access token.
    /// </summary>
    public (string Token, AccessTokenPayload Payload, DateTime ExpiresAt) GenerateAccessToken(
        string buildingId, string aptId, int durationMinutes, bool oneTime, string createdBy)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var exp = now + durationMinutes * 60_000L;
        var uid = "tok-" + now + "-" + Guid.NewGuid().ToString("N")[..6];

        var payload = new AccessTokenPayload
        {
            Bld = buildingId,
            Apt = aptId,
            Exp = exp,
            Use = oneTime ? "single" : "multi",
            Uid = uid,
            Ts = now,
            By = createdBy
        };

        var dataToSign = $"{payload.Bld}|{payload.Apt}|{payload.Exp}|{payload.Use}|{payload.Uid}";
        payload.Sig = Sign(dataToSign);

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        return (UrlSafe(token), payload, DateTimeOffset.FromUnixTimeMilliseconds(exp).UtcDateTime);
    }

    /// <summary>
    /// Validates the access token.
    /// </summary>
    public (bool Valid, AccessTokenPayload? Payload, string? Reason) ValidateAccessToken(string token)
    {
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(FromUrlSafe(token)));
            var payload = JsonSerializer.Deserialize<AccessTokenPayload>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            if (payload == null || string.IsNullOrEmpty(payload.Sig)) return (false, null, "invalid-format");

            var dataToSign = $"{payload.Bld}|{payload.Apt}|{payload.Exp}|{payload.Use}|{payload.Uid}";
            var expectedSignature = payload.Sig.Length == 16 ? SignLegacy(dataToSign) : Sign(dataToSign);
            if (payload.Sig.Length is not (16 or 64) || !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expectedSignature), Encoding.UTF8.GetBytes(payload.Sig)))
                return (false, null, "invalid-signature");

            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > payload.Exp) return (false, null, "expired");

            return (true, payload, null);
        }
        catch
        {
            return (false, null, "parse-error");
        }
    }

    // Base64 عادي فيه + / = ممكن تعمل مشاكل في الـ URL، فبنستبدلها
    private static string UrlSafe(string b64) => b64.Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string FromUrlSafe(string s)
    {
        var b64 = s.Replace('-', '+').Replace('_', '/');
        switch (b64.Length % 4)
        {
            case 2: b64 += "=="; break;
            case 3: b64 += "="; break;
        }
        return b64;
    }
}