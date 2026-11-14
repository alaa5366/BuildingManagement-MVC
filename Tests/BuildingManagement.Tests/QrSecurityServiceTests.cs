using BuildingManagementMvc.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildingManagement.Tests;

// الاختبارات دي مابتلمسش الـ DB (الـ factory = null) — بتغطي بس مسارات الرفض قبل فحص الاستخدام
public class QrSecurityServiceTests
{
    private static QrSecurityService Create() =>
        new(null!,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Qr:SecretKey"] = "unit-test-qr-secret-key-0123456789" })
                .Build(),
            NullLogger<QrSecurityService>.Instance);

    private static QuickQrOptions Opts(TimeSpan validity) => new()
    {
        BuildingId = "b1", AptId = "a1", AptNumber = 1, FloorOrder = 0,
        Validity = validity, MaxUses = 1, OneTime = true
    };

    [Fact]
    public void Quick_token_has_v2_prefix_and_is_random()
    {
        var qr = Create();
        var t1 = qr.GenerateQuickToken(Opts(TimeSpan.FromMinutes(5)));
        var t2 = qr.GenerateQuickToken(Opts(TimeSpan.FromMinutes(5)));

        Assert.StartsWith("v2.", t1);
        Assert.NotEqual(t1, t2);
    }

    [Fact]
    public async Task Expired_token_is_rejected()
    {
        var qr = Create();
        var token = qr.GenerateQuickToken(Opts(TimeSpan.FromMinutes(-1)));

        var result = await qr.ValidateQuickToken(token);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Tampered_token_is_rejected()
    {
        var qr = Create();
        var token = qr.GenerateQuickToken(Opts(TimeSpan.FromMinutes(5)));
        var chars = token.ToCharArray();
        var i = chars.Length - 3;
        chars[i] = chars[i] == 'A' ? 'B' : 'A';

        var result = await qr.ValidateQuickToken(new string(chars));
        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("v1.abc.def")]
    public async Task Malformed_or_old_format_tokens_are_rejected(string token)
    {
        var result = await Create().ValidateQuickToken(token);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Stable_qr_signature_verifies_only_for_same_input()
    {
        var qr = Create();
        var sig = qr.SignStableQr("/Account/Qr", "b=1&a=2");

        Assert.True(qr.VerifyStableQr("/Account/Qr", "b=1&a=2", sig));
        Assert.False(qr.VerifyStableQr("/Account/Qr", "b=1&a=3", sig));
        Assert.False(qr.VerifyStableQr("/Account/Qr", "b=1&a=2", ""));
    }
}
