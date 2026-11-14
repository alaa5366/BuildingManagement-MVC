using System.Security.Cryptography;
using System.Text;

namespace BuildingManagementMvc.Services;

// نقطة واحدة للتعامل مع PIN / كلمات السر المخزنة:
//  - التخزين دايمًا BCrypt
//  - مقارنة النص الصريح القديم مقفولة إلا لو Auth:AllowLegacyPlainPins=true (مفتاح طوارئ مؤقت)
public static class SecretVerifier
{
    // بتتظبط عند الإقلاع من الـ config (Auth:AllowLegacyPlainPins)
    public static bool AllowLegacyPlain { get; set; } = false;

    public static bool IsBcrypt(string? v) =>
        !string.IsNullOrEmpty(v) &&
        (v.StartsWith("$2a$") || v.StartsWith("$2b$") || v.StartsWith("$2y$"));

    public static string Hash(string secret) => BCrypt.Net.BCrypt.HashPassword(secret, 10);

    // يرجّع الـ hash لو القيمة لسه نص صريح، وإلا يرجّعها زي ما هي
    public static string HashIfPlain(string secret) => IsBcrypt(secret) ? secret : Hash(secret);

    // hash وهمي: بنعمل عليه BCrypt لما الحساب مش موجود، عشان وقت الرد مايكشفش هل الرقم/الإيميل مسجّل
    private static readonly Lazy<string> DummyHash = new(() => Hash("timing-equalizer"));

    public static void BurnTime()
    {
        try { BCrypt.Net.BCrypt.Verify("timing-equalizer-input", DummyHash.Value); } catch { }
    }

    public static bool Verify(string? input, string? stored)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(stored)) return false;

        if (IsBcrypt(stored))
        {
            try { return BCrypt.Net.BCrypt.Verify(input, stored); }
            catch { return false; }
        }

        if (!AllowLegacyPlain) return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(input), Encoding.UTF8.GetBytes(stored));
    }
}
