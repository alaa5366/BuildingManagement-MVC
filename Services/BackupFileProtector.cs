using System.Security.Cryptography;
using System.Text;

namespace BuildingManagementMvc.Services;

/// <summary>
/// تشفير ملفات النسخ الاحتياطي المخزنة على قرص السيرفر (AES-GCM).
/// </summary>
/// <remarks>
/// <para>
/// <b>الصيغة:</b> <c>"BMENC1\0" | nonce[12] | tag[16] | ciphertext</c>
/// </para>
/// <para>
/// <b>ملاحظة:</b> الملفات القديمة غير المشفرة لسه بتتقرا (بنكتشفها من الـ header).
/// </para>
/// </remarks>
public static class BackupFileProtector
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("BMENC1\0");
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private static byte[]? _key;

    /// <summary>
    /// يهيّئ الخدمة بالمفتاح السري.
    /// </summary>
    /// <param name="secret">المفتاح السري من الـ configuration.</param>
    /// <remarks>
    /// لازم تتنادى مرة واحدة في <c>Program.cs</c> قبل الاستخدام.
    /// </remarks>
    public static void Initialize(string secret) =>
        _key = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes("backup-file-v1"));

    /// <summary>
    /// بيتحقق لو الملف مشفّر (بيبدأ بـ Magic header).
    /// </summary>
    /// <param name="data">محتوى الملف.</param>
    /// <returns><c>true</c> لو الملف مشفّر.</returns>
    public static bool IsProtected(byte[] data) =>
        data.Length >= Magic.Length + NonceSize + TagSize &&
        data.AsSpan(0, Magic.Length).SequenceEqual(Magic);

    /// <summary>
    /// يشفّر الملف بـ AES-GCM.
    /// </summary>
    /// <param name="plain">المحتوى الأصلي.</param>
    /// <returns>المحتوى المشفّر (مع الـ Magic header).</returns>
    /// <exception cref="InvalidOperationException">
    /// لو <see cref="Initialize"/> مش اتنادت.
    /// </exception>
    public static byte[] Protect(byte[] plain)
    {
        var key = _key ?? throw new InvalidOperationException("BackupFileProtector is not initialized");

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var cipher = new byte[plain.Length];

        using var gcm = new AesGcm(key, TagSize);
        gcm.Encrypt(nonce, plain, cipher, tag, Magic);

        var all = new byte[Magic.Length + NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(Magic, 0, all, 0, Magic.Length);
        Buffer.BlockCopy(nonce, 0, all, Magic.Length, NonceSize);
        Buffer.BlockCopy(tag, 0, all, Magic.Length + NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, all, Magic.Length + NonceSize + TagSize, cipher.Length);
        return all;
    }

    /// <summary>
    /// يفك تشفير الملف.
    /// </summary>
    /// <param name="data">المحتوى المشفّر (أو القديم غير المشفّر).</param>
    /// <returns>المحتوى الأصلي.</returns>
    /// <exception cref="CryptographicException">
    /// لو الملف معدّل أو المفتاح مختلف.
    /// </exception>
    public static byte[] Unprotect(byte[] data)
    {
        if (!IsProtected(data)) return data;

        var key = _key ?? throw new InvalidOperationException("BackupFileProtector is not initialized");

        var offset = Magic.Length;
        var nonce = data.AsSpan(offset, NonceSize);
        var tag = data.AsSpan(offset + NonceSize, TagSize);
        var cipher = data.AsSpan(offset + NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var gcm = new AesGcm(key, TagSize);
        gcm.Decrypt(nonce, cipher, tag, plain, Magic);
        return plain;
    }
}