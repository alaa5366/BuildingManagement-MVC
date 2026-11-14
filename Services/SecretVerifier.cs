using System.Security.Cryptography;
using System.Text;

namespace BuildingManagementMvc.Services;

/// <summary>
/// نقطة واحدة للتعامل مع PIN / كلمات السر المخزنة.
/// </summary>
/// <remarks>
/// <para>
/// التخزين دايمًا <c>BCrypt</c>. مقارنة النص الصريح القديم مقفولة إلا لو
/// <see cref="AllowLegacyPlain"/> = <c>true</c> (مفتاح طوارئ مؤقت).
/// </para>
/// <para>
/// <b>⚠️ تحذير:</b> مفتاح <see cref="AllowLegacyPlain"/> يجب أن يكون <c>false</c>
/// في الإنتاج. تشغيله يفتح ثغرة أمنية (مقارنة نص صريح بدل BCrypt).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var hash = SecretVerifier.Hash("1234");
/// bool isValid = SecretVerifier.Verify("1234", hash); // true
/// </code>
/// </example>
public static class SecretVerifier
{
    /// <summary>
    /// يسمح بمقارنة النص الصريح القديم (مفتاح طوارئ مؤقت فقط).
    /// </summary>
    /// <remarks>
    /// بيتظبط عند الإقلاع من <c>Auth:AllowLegacyPlainPins</c> في <c>Program.cs</c>.
    /// </remarks>
    public static bool AllowLegacyPlain { get; set; } = false;

    /// <summary>
    /// بيتحقق لو القيمة دي BCrypt hash.
    /// </summary>
    /// <param name="v">القيمة اللي عايز تتحقق منها (ممكن تكون null).</param>
    /// <returns>
    /// <c>true</c> لو القيمة بتبدأ بـ <c>$2a$</c> أو <c>$2b$</c> أو <c>$2y$</c>؛
    /// خلاف كده <c>false</c>.
    /// </returns>
    public static bool IsBcrypt(string? v) =>
        !string.IsNullOrEmpty(v) &&
        (v.StartsWith("$2a$") || v.StartsWith("$2b$") || v.StartsWith("$2y$"));

    /// <summary>
    /// يعمل BCrypt hash للقيمة بـ work factor 10.
    /// </summary>
    /// <param name="secret">القيمة الأصلية (PIN أو password).</param>
    /// <returns>BCrypt hash جاهز للتخزين.</returns>
    public static string Hash(string secret) => BCrypt.Net.BCrypt.HashPassword(secret, 10);

    /// <summary>
    /// يعمل hash لو القيمة لسه نص صريح، وإلا يرجّعها زي ما هي.
    /// </summary>
    /// <param name="secret">القيمة (BCrypt hash أو نص صريح).</param>
    /// <returns>
    /// BCrypt hash لو <paramref name="secret"/> مش hash بالفعل؛
    /// أو نفس القيمة لو هي BCrypt hash.
    /// </returns>
    /// <remarks>
    /// مفيدة عشان ما تعملش double-hash لو الحقل متحفظ مرتين.
    /// </remarks>
    public static string HashIfPlain(string secret) => IsBcrypt(secret) ? secret : Hash(secret);

    /// <summary>
    /// hash وهمي بيتعمل عليه BCrypt لما الحساب مش موجود، عشان نمنع timing attacks.
    /// </summary>
    private static readonly Lazy<string> DummyHash = new(() => Hash("timing-equalizer"));

    /// <summary>
    /// يحرق وقت BCrypt (~50ms) عشان نمنع timing attacks.
    /// </summary>
    /// <remarks>
    /// استخدمها لما الحساب مش موجود أو لما الدخول فشل، عشان وقت الرد
    /// مايفرقش بين "الحساب مش موجود" و "الـ PIN غلط".
    /// </remarks>
    public static void BurnTime()
    {
        try { BCrypt.Net.BCrypt.Verify("timing-equalizer-input", DummyHash.Value); } catch { }
    }

    /// <summary>
    /// يتحقق من صحة الـ PIN / كلمة السر مقابل القيمة المخزنة.
    /// </summary>
    /// <param name="input">القيمة اللي المستخدم دخّلها.</param>
    /// <param name="stored">القيمة المخزنة (BCrypt hash أو نص صريح legacy).</param>
    /// <returns>
    /// <c>true</c> لو متطابقة؛ <c>false</c> لو مش متطابقة أو أي من القيمتين فاضي.
    /// </returns>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>لو <paramref name="stored"/> بـ BCrypt prefix، بيستخدم BCrypt.Verify.</item>
    ///   <item>لو <paramref name="stored"/> نص صريح، بيرجع <c>false</c> إلا لو <see cref="AllowLegacyPlain"/> = <c>true</c>.</item>
    ///   <item>لو الـ BCrypt.Verify رمى exception (hash تالف)، بيرجع <c>false</c>.</item>
    /// </list>
    /// </remarks>
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