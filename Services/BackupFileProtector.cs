using System.Security.Cryptography;
using System.Text;

namespace BuildingManagementMvc.Services;

// تشفير ملفات النسخ الاحتياطي المخزنة على قرص السيرفر (AES-GCM).
// الصيغة: "BMENC1\0" | nonce[12] | tag[16] | ciphertext
// الملفات القديمة غير المشفرة لسه بتتقرا (بنكتشفها من الـ header) وأي ملف جديد بيتكتب مشفّر.
public static class BackupFileProtector
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("BMENC1\0");
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private static byte[]? _key;

    public static void Initialize(string secret) =>
        _key = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes("backup-file-v1"));

    public static bool IsProtected(byte[] data) =>
        data.Length >= Magic.Length + NonceSize + TagSize &&
        data.AsSpan(0, Magic.Length).SequenceEqual(Magic);

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

    // لو الملف مش مشفّر (نسخة قديمة) بيرجّعه زي ما هو
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
        gcm.Decrypt(nonce, cipher, tag, plain, Magic);   // CryptographicException لو اتعدّل أو المفتاح اتغيّر
        return plain;
    }
}
