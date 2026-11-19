using BuildingManagementMvc.Data;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class PresenceServiceTests
{
    [Fact]
    public async Task Heartbeat_UsesTargetSessionAndAppendsHeartbeatHistory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(options));
        var service = new PresenceService(factory.Object, NullLogger<PresenceService>.Instance);

        await service.HeartbeatAsync("resident-uid", "apt-12", "building-1");
        await service.HeartbeatAsync("resident-uid", "apt-12", "building-1");

        await using var db = new AppDbContext(options);
        Assert.Equal(1, await db.PresenceSessions.CountAsync());
        Assert.Equal(2, await db.PresenceHeartbeats.CountAsync());
        Assert.Equal(1, await service.GetOnlineCountAsync("building-1"));
        var byBuilding = await service.GetByBuildingAsync("building-1");
        Assert.Contains("apt-12", byBuilding.Keys);
    }

    [Fact]
    public async Task Heartbeat_KeepsApartmentlessSessionsDistinctForOnlineCount()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(options));
        var service = new PresenceService(factory.Object, NullLogger<PresenceService>.Instance);

        await service.HeartbeatAsync("uid-1", "", "building-1");
        await service.HeartbeatAsync("uid-2", "", "building-1");

        Assert.Equal(2, await service.GetOnlineCountAsync("building-1"));
    }
}
