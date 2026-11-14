using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;

namespace BuildingManagementMvc.Services;

// تشفير مفاتيح ASP.NET DataProtection (اللي بتوقّع/تشفّر الكوكيز) على القرص بـ AES-GCM
// بمفتاح مشتق من Encryption:SecretKey. المفاتيح القديمة غير المشفرة لسه بتشتغل،
// وأي مفتاح جديد بيتكتب مشفّر. مهم: لو Encryption:SecretKey اتغيّر المفاتيح المشفرة مش هتتفك
// (المستخدمين هيتعملهم تسجيل خروج) — فما تغيّرهوش من غير سبب.
public sealed class DataProtectionKeyHolder
{
    public byte[] Key { get; }

    public DataProtectionKeyHolder(string secret) =>
        Key = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes("dataprotection-keys-v1"));
}

public sealed class AesGcmXmlEncryptor : IXmlEncryptor
{
    private readonly byte[] _key;

    public AesGcmXmlEncryptor(byte[] key) => _key = key;

    public EncryptedXmlInfo Encrypt(XElement plaintextElement)
    {
        var plain = Encoding.UTF8.GetBytes(plaintextElement.ToString(SaveOptions.DisableFormatting));
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var cipher = new byte[plain.Length];

        using var gcm = new AesGcm(_key, 16);
        gcm.Encrypt(nonce, plain, cipher, tag);

        var all = new byte[12 + 16 + cipher.Length];
        Buffer.BlockCopy(nonce, 0, all, 0, 12);
        Buffer.BlockCopy(tag, 0, all, 12, 16);
        Buffer.BlockCopy(cipher, 0, all, 28, cipher.Length);

        var element = new XElement("encryptedKey",
            new XComment(" This key is encrypted with AES-GCM. "),
            new XElement("value", Convert.ToBase64String(all)));

        return new EncryptedXmlInfo(element, typeof(AesGcmXmlDecryptor));
    }
}

public sealed class AesGcmXmlDecryptor : IXmlDecryptor
{
    private readonly byte[] _key;

    // بيتعمل له instance من الـ DataProtection مع IServiceProvider
    public AesGcmXmlDecryptor(IServiceProvider services) =>
        _key = ((DataProtectionKeyHolder)services.GetService(typeof(DataProtectionKeyHolder))!).Key;

    public XElement Decrypt(XElement encryptedElement)
    {
        var all = Convert.FromBase64String((string)encryptedElement.Element("value")!);
        var nonce = all.AsSpan(0, 12);
        var tag = all.AsSpan(12, 16);
        var cipher = all.AsSpan(28);
        var plain = new byte[cipher.Length];

        using var gcm = new AesGcm(_key, 16);
        gcm.Decrypt(nonce, cipher, tag, plain);

        return XElement.Parse(Encoding.UTF8.GetString(plain));
    }
}
