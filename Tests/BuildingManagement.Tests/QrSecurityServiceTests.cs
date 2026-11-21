using BuildingManagementMvc.Data;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
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
    public async Task Recovery_token_is_revoked_and_cannot_be_resurrected_by_late_consume()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        var qr = Create(new TestContextFactory(options));
        var token = qr.GenerateQuickToken(RecoveryOptions());

        await qr.RegisterRecoveryTokenAsync(token, "b1", "a1");
        var validated = await qr.ValidateQuickToken(token);
        Assert.True(validated.IsSuccess);

        Assert.True(await qr.RevokeRecoveryTokenAsync("b1", "a1"));
        Assert.False((await qr.ValidateQuickToken(token)).IsSuccess);
        Assert.False(await qr.TryConsumeQuickTokenAsync(validated.Data!));
    }

    [Fact]
    public async Task Reissuing_recovery_token_invalidates_old_token_even_if_it_was_previously_validated()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        var qr = Create(new TestContextFactory(options));
        var oldToken = qr.GenerateQuickToken(RecoveryOptions());
        await qr.RegisterRecoveryTokenAsync(oldToken, "b1", "a1");
        var oldValidation = await qr.ValidateQuickToken(oldToken);
        Assert.True(oldValidation.IsSuccess);

        var newToken = qr.GenerateQuickToken(RecoveryOptions());
        await qr.RegisterRecoveryTokenAsync(newToken, "b1", "a1");

        Assert.False((await qr.ValidateQuickToken(oldToken)).IsSuccess);
        Assert.False(await qr.TryConsumeQuickTokenAsync(oldValidation.Data!));

        var newValidation = await qr.ValidateQuickToken(newToken);
        Assert.True(newValidation.IsSuccess);
        Assert.True(await qr.TryConsumeQuickTokenAsync(newValidation.Data!));
    }

    private static QrSecurityService Create(IDbContextFactory<AppDbContext> factory) =>
        new(factory,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Qr:SecretKey"] = "unit-test-qr-secret-key-0123456789" })
                .Build(),
            NullLogger<QrSecurityService>.Instance);

    private static QuickQrOptions RecoveryOptions() => new()
    {
        BuildingId = "b1", AptId = "a1", AptNumber = 1, FloorOrder = 0,
        Validity = TimeSpan.FromMinutes(5), MaxUses = 1, OneTime = true, Purpose = "recovery"
    };

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
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

    [Fact]
    public void Invoice_verification_signature_is_full_length_and_rejects_tampering()
    {
        var qr = Create();
        var invoiceNo = "INV-BLD-001-12-20261121-000001";
        var signature = qr.SignInvoiceVerification(invoiceNo);

        Assert.Equal(64, signature.Length);
        Assert.True(qr.VerifyInvoiceVerification(invoiceNo, signature));
        Assert.False(qr.VerifyInvoiceVerification(invoiceNo + "-tampered", signature));
        Assert.False(qr.VerifyInvoiceVerification(invoiceNo, "not-a-signature"));
        Assert.False(qr.VerifyInvoiceVerification(invoiceNo, null));
    }
}
