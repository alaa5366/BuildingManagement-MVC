using System.Security.Cryptography;
using System.Text;
using BuildingManagementMvc.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BuildingManagement.Tests;

public class EncryptionServiceTests
{
    private const string Key = "unit-test-encryption-secret-key";

    private static AesEncryptionService Create() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Encryption:SecretKey"] = Key })
            .Build());

    [Fact]
    public void Roundtrip_uses_gcm_format()
    {
        var svc = Create();
        var enc = svc.Encrypt("dvr-password-123");

        Assert.StartsWith("gcm1:", enc);
        Assert.Equal("dvr-password-123", svc.Decrypt(enc));
    }

    [Fact]
    public void Same_plaintext_gives_different_ciphertext()
    {
        var svc = Create();
        Assert.NotEqual(svc.Encrypt("x"), svc.Encrypt("x"));
    }

    [Fact]
    public void Tampered_ciphertext_is_rejected()
    {
        var svc = Create();
        var enc = svc.Encrypt("secret");
        var bytes = Convert.FromBase64String(enc.Substring(5));
        bytes[^1] ^= 0x01;
        var tampered = "gcm1:" + Convert.ToBase64String(bytes);

        Assert.ThrowsAny<CryptographicException>(() => svc.Decrypt(tampered));
    }

    [Fact]
    public void Legacy_cbc_values_still_decrypt()
    {
        // نفس الخوارزمية القديمة: AES-CBC بمفتاح SHA256 وIV عشوائي في أول 16 بايت
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(Key));
        aes.GenerateIV();
        var plain = Encoding.UTF8.GetBytes("old-camera-password");
        var cipher = aes.CreateEncryptor().TransformFinalBlock(plain, 0, plain.Length);
        var all = aes.IV.Concat(cipher).ToArray();

        Assert.Equal("old-camera-password", Create().Decrypt(Convert.ToBase64String(all)));
    }

    [Fact]
    public void Empty_values_roundtrip_to_empty()
    {
        var svc = Create();
        Assert.Equal("", svc.Encrypt(""));
        Assert.Equal("", svc.Decrypt(""));
    }
}
