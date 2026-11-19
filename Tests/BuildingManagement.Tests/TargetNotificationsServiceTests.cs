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
        var options = CreateOptions();
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.AddRange(Building("B1"), Building("B2"));
            seed.NotificationMessages.AddRange(
                Message("B1", "admin-1", 0, null),
                Message("B1", "resident-1", 1, "A1"),
                Message("B2", "admin-2", 0, null));
            await seed.SaveChangesAsync();
        }

        var service = new TargetNotificationsService(new TestContextFactory(options));
        var items = await service.GetForAdminAsync("B1");
        var item = Assert.Single(items);
        Assert.Equal("admin-1", item.Title);
        Assert.Equal("admin", item.RecipientType);
    }

    [Fact]
    public async Task CreateResidentDepositNoticeAsync_WritesNormalizedMessageAndRecipient()
    {
        var options = CreateOptions();
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(Building("B1"));
            seed.Apartments.Add(new ApartmentEntity
            {
                BuildingId = "B1", Id = "A1", FloorId = "F1", Number = 1
            });
            await seed.SaveChangesAsync();
        }

        var service = new TargetNotificationsService(new TestContextFactory(options));
        await service.CreateResidentDepositNoticeAsync("B1", "A1", "Deposit confirmed", "Payment confirmed.");

        await using var verify = new AppDbContext(options);
        var message = await verify.NotificationMessages.Include(x => x.Recipients).SingleAsync();
        var recipient = Assert.Single(message.Recipients);
        Assert.Equal("B1", message.BuildingId);
        Assert.Equal("Deposit confirmed", message.Title);
        Assert.Equal("Payment confirmed.", message.Body);
        Assert.Equal((byte)1, recipient.RecipientType);
        Assert.Equal("A1", recipient.ApartmentId);
        Assert.Null(await verify.NotificationReadStates.SingleOrDefaultAsync());
    }

    [Fact]
    public async Task CreateResidentNotificationAsync_RejectsApartmentOutsideBuilding()
    {
        var options = CreateOptions();
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(Building("B1"));
            seed.Apartments.Add(new ApartmentEntity
            {
                BuildingId = "B1", Id = "A1", FloorId = "F1", Number = 1
            });
            await seed.SaveChangesAsync();
        }

        var service = new TargetNotificationsService(new TestContextFactory(options));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateResidentNotificationAsync("B2", "A1", "Deposit", "Notice"));
    }

    [Fact]
    public async Task MarkReadAndMarkAllRead_ChangeOnlyTheExactRecipientScope()
    {
        var options = CreateOptions();
        var resident1 = Message("B1", "A1 notice", 1, "A1");
        var resident2 = Message("B1", "A2 notice", 1, "A2");
        var admin = Message("B1", "admin notice", 0, null);
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.AddRange(Building("B1"), Building("B2"));
            seed.NotificationMessages.AddRange(resident1, resident2, admin,
                Message("B2", "other building", 1, "A1"));
            await seed.SaveChangesAsync();
        }

        var service = new TargetNotificationsService(new TestContextFactory(options));
        Assert.False(await service.MarkReadAsync("B1", resident1.MessageId.ToString("N"), "resident", "A2"));
        Assert.True(await service.MarkReadAsync("B1", resident1.MessageId.ToString("N"), "resident", "A1"));
        Assert.Equal(1, await service.GetUnreadCountForResidentAsync("B1", "A2"));
        Assert.Equal(1, await service.GetUnreadCountForAdminAsync("B1"));
        Assert.Equal(1, await service.MarkAllReadAsync("B1", "resident", "A2"));
        Assert.Equal(0, await service.GetUnreadCountForResidentAsync("B1", "A2"));
        Assert.Equal(1, await service.GetUnreadCountForAdminAsync("B1"));
    }

    private static DbContextOptions<AppDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;

    private static BuildingEntity Building(string id) => new()
    {
        Id = id, BuildingNumber = id, Name = id
    };

    private static NotificationMessageEntity Message(
        string buildingId, string title, byte recipientType, string? apartmentId)
    {
        var now = DateTime.UtcNow;
        return new NotificationMessageEntity
        {
            MessageId = Guid.NewGuid(),
            BuildingId = buildingId,
            Title = title,
            Body = title,
            Channel = 0,
            Origin = "info",
            CreatedAtUtc = now,
            Recipients =
            {
                new NotificationRecipientEntity
                {
                    RecipientId = Guid.NewGuid(),
                    RecipientType = recipientType,
                    RecipientReference = apartmentId ?? "building-admins",
                    ApartmentId = apartmentId,
                    CreatedAtUtc = now
                }
            }
        };
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
