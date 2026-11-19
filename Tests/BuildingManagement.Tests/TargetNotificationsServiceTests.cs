using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class TargetNotificationsServiceTests
{
    [Fact]
    public async Task GetForAdminAsync_IsScopedToBuildingAndAdminRecipient()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.AddRange(
                new BuildingEntity { Id = "B1", BuildingNumber = "1", Name = "One" },
                new BuildingEntity { Id = "B2", BuildingNumber = "2", Name = "Two" });
            seed.Notifications.AddRange(
                Create("B1", "admin-1", "admin", null),
                Create("B1", "resident-1", "resident", "A1"),
                Create("B2", "admin-2", "admin", null));
            await seed.SaveChangesAsync();
        }

        var service = new TargetNotificationsService(new TestContextFactory(options));
        var items = await service.GetForAdminAsync("B1");

        var item = Assert.Single(items);
        Assert.Equal("admin-1", item.Id);
        Assert.Equal("admin", item.RecipientType);
    }

    [Fact]
    public async Task MarkReadAsync_RequiresExactBuildingAndRecipientScope()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(new BuildingEntity { Id = "B1", BuildingNumber = "1", Name = "One" });
            seed.Notifications.AddRange(
                Create("B1", "admin-1", "admin", null),
                Create("B1", "resident-1", "resident", "A1"),
                Create("B1", "resident-2", "resident", "A2"));
            await seed.SaveChangesAsync();
        }

        var service = new TargetNotificationsService(new TestContextFactory(options));
        Assert.False(await service.MarkReadAsync("B1", "resident-1", "resident", "A2"));
        Assert.False(await service.MarkReadAsync("B2", "admin-1", "admin"));
        Assert.True(await service.MarkReadAsync("B1", "resident-1", "resident", "A1"));

        await using var verify = new AppDbContext(options);
        Assert.True(await verify.Notifications.Where(item => item.Id == "resident-1")
            .Select(item => item.IsRead).SingleAsync());
        Assert.False(await verify.Notifications.Where(item => item.Id == "resident-2")
            .Select(item => item.IsRead).SingleAsync());
    }

    private static NotificationEntity Create(string buildingId, string id, string recipientType, string? apartmentId) => new()
    {
        BuildingId = buildingId,
        Id = id,
        Ts = new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc),
        RecipientType = recipientType,
        RecipientApartmentId = apartmentId,
        Type = "info",
        Icon = "bell",
        Title = id,
        Body = "Test notification",
        IsRead = false
    };

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
