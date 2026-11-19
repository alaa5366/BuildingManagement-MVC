using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class QrTokenStoreServiceTests
{
    [Fact]
    public async Task PreviewTargetAsync_DoesNotConsumeSingleUseToken_AndFinalUseRemainsAtomic()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var now = DateTime.UtcNow;
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(new BuildingEntity { Id = "B1", BuildingNumber = "1", Name = "One" });
            seed.Apartments.Add(new ApartmentEntity
            {
                BuildingId = "B1", Id = "A1", FloorId = "F1", Number = 1
            });
            seed.QrTokens.Add(new QrTokenEntity
            {
                Uid = "token-1", BuildingId = "B1", ApartmentId = "A1",
                ExpiresAt = now.AddMinutes(10), IssuedAt = now,
                UseType = "single", CreatedBy = "admin", IsUsed = false, CreatedAt = now
            });
            await seed.SaveChangesAsync();
        }

        var service = new QrTokenStoreService(new TestContextFactory(options),
            NullLogger<QrTokenStoreService>.Instance);

        var preview = await service.PreviewTargetAsync("token-1");
        Assert.True(preview.Allowed);
        Assert.Equal("B1", preview.Bld);
        Assert.Equal("A1", preview.Apt);

        await using (var verifyPreview = new AppDbContext(options))
            Assert.False(await verifyPreview.QrTokens.Select(item => item.IsUsed).SingleAsync());

        var consumed = await service.CheckAndUseTokenAsync("token-1");
        Assert.True(consumed.Allowed);

        var secondPreview = await service.PreviewTargetAsync("token-1");
        Assert.False(secondPreview.Allowed);
        Assert.Equal("already-used", secondPreview.Reason);
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
