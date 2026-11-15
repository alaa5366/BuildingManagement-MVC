using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class PasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    // ================================================================
    // Hash
    // ================================================================

    [Fact]
    public void Hash_ValidPassword_ReturnsBcryptHash()
    {
        var hash = _hasher.Hash("my-password");
        Assert.StartsWith("$2", hash);
    }

    [Fact]
    public void Hash_SamePasswordDifferentHashes()
    {
        var h1 = _hasher.Hash("my-password");
        var h2 = _hasher.Hash("my-password");
        Assert.NotEqual(h1, h2); // salt مختلف
    }

    [Fact]
    public void Hash_EmptyPassword_Throws()
    {
        Assert.Throws<ArgumentException>(() => _hasher.Hash(""));
        Assert.Throws<ArgumentException>(() => _hasher.Hash("   "));
        Assert.Throws<ArgumentException>(() => _hasher.Hash(null!));
    }

    // ================================================================
    // Verify
    // ================================================================

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("my-password");
        Assert.True(_hasher.Verify("my-password", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("my-password");
        Assert.False(_hasher.Verify("wrong", hash));
    }

    [Fact]
    public void Verify_EmptyInput_ReturnsFalse()
    {
        var hash = _hasher.Hash("my-password");
        Assert.False(_hasher.Verify("", hash));
        Assert.False(_hasher.Verify("my-password", ""));
        Assert.False(_hasher.Verify(null!, hash));
    }

    [Fact]
    public void Verify_InvalidHash_ReturnsFalse()
    {
        Assert.False(_hasher.Verify("password", "not-a-bcrypt-hash"));
    }

    // ================================================================
    // HashPin / VerifyPin
    // ================================================================

    [Fact]
    public void HashPin_ValidPin_ReturnsBcryptHash()
    {
        var hash = _hasher.HashPin("1234");
        Assert.StartsWith("$2", hash);
        Assert.True(_hasher.VerifyPin("1234", hash));
    }

    [Fact]
    public void VerifyPin_WrongPin_ReturnsFalse()
    {
        var hash = _hasher.HashPin("1234");
        Assert.False(_hasher.VerifyPin("9999", hash));
    }

    [Fact]
    public void HashPin_EmptyPin_Throws()
    {
        Assert.Throws<ArgumentException>(() => _hasher.HashPin(""));
        Assert.Throws<ArgumentException>(() => _hasher.HashPin(null!));
    }

    [Fact]
    public void VerifyPin_EmptyInput_ReturnsFalse()
    {
        var hash = _hasher.HashPin("1234");
        Assert.False(_hasher.VerifyPin("", hash));
        Assert.False(_hasher.VerifyPin("1234", ""));
    }
}