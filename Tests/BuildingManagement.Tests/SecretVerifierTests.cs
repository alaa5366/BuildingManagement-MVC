using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class SecretVerifierTests
{
    public SecretVerifierTests() => SecretVerifier.AllowLegacyPlain = false;

    [Fact]
    public void Hash_then_Verify_succeeds()
    {
        var h = SecretVerifier.Hash("1234");
        Assert.True(SecretVerifier.IsBcrypt(h));
        Assert.True(SecretVerifier.Verify("1234", h));
        Assert.False(SecretVerifier.Verify("1235", h));
    }

    [Theory]
    [InlineData("$2a$10$abcdefghijklmnopqrstuv", true)]
    [InlineData("$2b$10$abcdefghijklmnopqrstuv", true)]
    [InlineData("$2y$10$abcdefghijklmnopqrstuv", true)]
    [InlineData("1234", false)]
    [InlineData("", false)]
    public void IsBcrypt_recognizes_prefixes(string value, bool expected)
        => Assert.Equal(expected, SecretVerifier.IsBcrypt(value));

    [Fact]
    public void Plain_text_is_rejected_by_default()
    {
        Assert.False(SecretVerifier.Verify("1234", "1234"));
    }

    [Fact]
    public void Plain_text_is_accepted_only_with_legacy_switch()
    {
        try
        {
            SecretVerifier.AllowLegacyPlain = true;
            Assert.True(SecretVerifier.Verify("1234", "1234"));
            Assert.False(SecretVerifier.Verify("9999", "1234"));
        }
        finally { SecretVerifier.AllowLegacyPlain = false; }
    }

    [Fact]
    public void Empty_input_or_stored_never_verifies()
    {
        Assert.False(SecretVerifier.Verify("", SecretVerifier.Hash("1234")));
        Assert.False(SecretVerifier.Verify("1234", ""));
        Assert.False(SecretVerifier.Verify(null, null));
    }

    [Fact]
    public void HashIfPlain_is_idempotent()
    {
        var h = SecretVerifier.Hash("1234");
        Assert.Equal(h, SecretVerifier.HashIfPlain(h));
        Assert.True(SecretVerifier.IsBcrypt(SecretVerifier.HashIfPlain("1234")));
    }

    [Fact]
    public void BurnTime_does_not_throw() => SecretVerifier.BurnTime();
}
