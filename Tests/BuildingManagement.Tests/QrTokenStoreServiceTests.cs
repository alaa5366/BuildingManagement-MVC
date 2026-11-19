using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class QrTokenStoreServiceTests
{
    [Fact]
    public async Task Preview_does_not_consume_single_use_capability_and_final_use_is_recorded()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        var service = new QrTokenStoreService(new TestContextFactory(options),
            NullLogger<QrTokenStoreService>.Instance);
        var now = DateTimeOffset.UtcNow;
        var payload = new AccessTokenPayload
        {
            Uid = "token-1", Bld = "B1", Apt = "A1", Use = "single", By = "admin",
            Ts = now.ToUnixTimeMilliseconds(), Exp = now.AddMinutes(10).ToUnixTimeMilliseconds()
        };

        Assert.True(await service.SaveTokenAsync(payload));
        var preview = await service.PreviewTargetAsync("token-1");
        Assert.True(preview.Allowed);
        Assert.Equal("B1", preview.Bld);
        Assert.Equal("A1", preview.Apt);

        await using (var verifyPreview = new AppDbContext(options))
            Assert.Empty(await verifyPreview.QrCapabilityUses.ToListAsync());

        Assert.True((await service.CheckAndUseTokenAsync("token-1")).Allowed);
        var secondPreview = await service.PreviewTargetAsync("token-1");
        Assert.False(secondPreview.Allowed);
        Assert.Equal("already-used", secondPreview.Reason);
    }

    [Fact]
    public async Task Delete_revokes_capability_without_deleting_audit_use_records()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        var service = new QrTokenStoreService(new TestContextFactory(options),
            NullLogger<QrTokenStoreService>.Instance);
        var now = DateTimeOffset.UtcNow;
        Assert.True(await service.SaveTokenAsync(new AccessTokenPayload
        {
            Uid = "token-2", Bld = "B1", Apt = "A2", Use = "multi", By = "admin",
            Ts = now.ToUnixTimeMilliseconds(), Exp = now.AddMinutes(10).ToUnixTimeMilliseconds()
        }));
        Assert.True((await service.CheckAndUseTokenAsync("token-2")).Allowed);

        await service.DeleteTokenAsync("token-2");

        Assert.Equal("revoked", (await service.PreviewTargetAsync("token-2")).Reason);
        await using var db = new AppDbContext(options);
        Assert.Single(await db.QrCapabilities.ToListAsync());
        Assert.Single(await db.QrCapabilityUses.ToListAsync());
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
