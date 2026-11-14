using System.Security.Cryptography;
using System.Text;

namespace BuildingManagementMvc.Services;

public interface IEncryptionService
{
    string Encrypt(string plain);
    string Decrypt(string cipher);
}

// التشفير الجديد: AES-GCM (تشفير + توثيق) — الصيغة: "gcm1:" + base64(nonce[12] | tag[16] | ciphertext)
// القيم القديمة (AES-CBC من غير MAC) لسه بتتفك عشان البيانات المخزنة (زي بيانات الـ DVR) تفضل شغالة،
// وأي قيمة بتتحفظ من هنا ورايح بتتشفّر GCM.
public class AesEncryptionService : IEncryptionService
{
    private const string GcmPrefix = "gcm1:";

    private readonly byte[] _legacyKey;
    private readonly byte[] _gcmKey;

    public AesEncryptionService(IConfiguration config)
    {
        var keyStr = config["Encryption:SecretKey"]
            ?? throw new InvalidOperationException("Encryption:Key missing");
        var keyBytes = Encoding.UTF8.GetBytes(keyStr);

        _legacyKey = SHA256.HashData(keyBytes);
        _gcmKey = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes("aes-gcm-v1"));
    }

    public string Encrypt(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return "";

        var plainBytes = Encoding.UTF8.GetBytes(plain);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var cipher = new byte[plainBytes.Length];

        using var gcm = new AesGcm(_gcmKey, 16);
        gcm.Encrypt(nonce, plainBytes, cipher, tag);

        var all = new byte[12 + 16 + cipher.Length];
        Buffer.BlockCopy(nonce, 0, all, 0, 12);
        Buffer.BlockCopy(tag, 0, all, 12, 16);
        Buffer.BlockCopy(cipher, 0, all, 28, cipher.Length);

        return GcmPrefix + Convert.ToBase64String(all);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return "";

        if (cipherText.StartsWith(GcmPrefix, StringComparison.Ordinal))
        {
            var all = Convert.FromBase64String(cipherText.Substring(GcmPrefix.Length));
            if (all.Length < 28) throw new CryptographicException("short ciphertext");

            var nonce = all.AsSpan(0, 12);
            var tag = all.AsSpan(12, 16);
            var cipher = all.AsSpan(28);
            var plain = new byte[cipher.Length];

            using var gcm = new AesGcm(_gcmKey, 16);
            gcm.Decrypt(nonce, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }

        return DecryptLegacyCbc(cipherText);
    }

    private string DecryptLegacyCbc(string cipherText)
    {
        var full = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = _legacyKey;
        var iv = new byte[16];
        Buffer.BlockCopy(full, 0, iv, 0, 16);
        aes.IV = iv;
        using var dec = aes.CreateDecryptor();
        var cipher = new byte[full.Length - 16];
        Buffer.BlockCopy(full, 16, cipher, 0, cipher.Length);
        var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }
}
