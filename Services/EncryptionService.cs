using System.Security.Cryptography;
using System.Text;

namespace BuildingManagementMvc.Services;

public interface IEncryptionService
{
    string Encrypt(string plain);
    string Decrypt(string cipher);
}

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IConfiguration config)
    {
        var keyStr = config["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key missing");
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(keyStr));
    }

    public string Encrypt(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return "";
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        using var enc = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plain);
        var cipher = enc.TransformFinalBlock(bytes, 0, bytes.Length);
        var result = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);
        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return "";
        var full = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = _key;
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