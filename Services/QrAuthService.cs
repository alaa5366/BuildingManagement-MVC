using BuildingManagementMvc.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/features/qr-auth.js (توليد والتحقق من توكن الدخول السريع)
public class QrAuthService
{
    // ⚠️ نفس المفتاح السري بالظبط اللي في الأصل، عشان أي توكن اتولّد من نسخة
    // الـ JS القديمة يفضل صالح، والعكس. غيّره لو عايز تفصل النسختين تمامًا.
    private const string SecretKey = "BM-qr-secret-2026-alaa-building-management-v1";

    private static string Sign(string data)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(data + SecretKey));
        return Convert.ToHexString(bytes).ToLower()[..16];
    }

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

    public (bool Valid, AccessTokenPayload? Payload, string? Reason) ValidateAccessToken(string token)
    {
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(FromUrlSafe(token)));
            var payload = JsonSerializer.Deserialize<AccessTokenPayload>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            if (payload == null || string.IsNullOrEmpty(payload.Sig)) return (false, null, "invalid-format");

            var dataToSign = $"{payload.Bld}|{payload.Apt}|{payload.Exp}|{payload.Use}|{payload.Uid}";
            if (Sign(dataToSign) != payload.Sig) return (false, null, "invalid-signature");

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