using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BuildingManagement.Tests;

public class AuditLoggerTests
{
    [Fact]
    public async Task QueryPage_FiltersByApartmentAndSeverityInTargetAuditEvents()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(options));
        await using (var db = new AppDbContext(options))
        {
            db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "Test Building", BuildingNumber = "BLD-901" });
            db.AuditEvents.AddRange(
                Event("b1", "a1", 1, "x"),
                Event("b1", "a2", 1, "x"),
                Event("b1", "a1", 0, "x"));
            await db.SaveChangesAsync();
        }

        var logger = new AuditLogger(factory.Object, NullLogger<AuditLogger>.Instance);
        var page = await logger.QueryPageAsync("b1", "a1", null, null, null, null, "warning", 1, 10);
        Assert.Single(page.Items);
        Assert.Equal("a1", page.Items[0].ApartmentId);
        Assert.Equal("warning", page.Items[0].Severity);
    }

    [Fact]
    public async Task QueryPage_PreservesDistinctErrorAndCriticalSeverities()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(options));
        await using (var db = new AppDbContext(options))
        {
            db.AuditEvents.AddRange(Event(null, null, 2, "error-event"), Event(null, null, 3, "critical-event"));
            await db.SaveChangesAsync();
        }

        var logger = new AuditLogger(factory.Object, NullLogger<AuditLogger>.Instance);
        var page = await logger.QueryPageAsync(null, null, null, null, null, null, null, 1, 10);

        Assert.Equal(2, page.Items.Count);
        Assert.Contains(page.Items, x => x.Action == "error-event" && x.Severity == "error");
        Assert.Contains(page.Items, x => x.Action == "critical-event" && x.Severity == "critical");
    }

    [Fact]
    public async Task LogAsync_PersistsCorrelationAndActorRoleInSafeAuditMetadata()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(options));

        using var activity = new Activity("audit-test").Start();
        activity!.SetTag(CorrelationIdMiddleware.ActivityTagName, "0123456789abcdef0123456789abcdef");
        var logger = new AuditLogger(factory.Object, NullLogger<AuditLogger>.Instance);
        await logger.LogAsync("backup.restore", userRole: "superadmin",
            metadata: new { result = "success", apiToken = "must-not-persist" });

        await using var db = new AppDbContext(options);
        var entry = await db.AuditEvents.SingleAsync();
        var details = JObject.Parse(entry.SafeDetailsJson!);
        Assert.Equal("0123456789abcdef0123456789abcdef", details["correlationId"]?.Value<string>());
        Assert.Equal("superadmin", details["actorRole"]?.Value<string>());
        Assert.Equal("success", details["result"]?.Value<string>());
        Assert.Equal("[REDACTED]", details["apiToken"]?.Value<string>());
        Assert.Equal("Business", entry.EventCategory);
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsAuditEventUpdates()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using (var seed = new AppDbContext(options))
        {
            seed.AuditEvents.Add(Event(null, null, 0, "original"));
            await seed.SaveChangesAsync();
        }

        await using var db = new AppDbContext(options);
        var entry = await db.AuditEvents.SingleAsync();
        entry.EventType = "tampered";
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("append-only", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsAuditEventDeletes()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using (var seed = new AppDbContext(options))
        {
            seed.AuditEvents.Add(Event(null, null, 0, "keep"));
            await seed.SaveChangesAsync();
        }

        await using var db = new AppDbContext(options);
        var entry = await db.AuditEvents.SingleAsync();
        db.AuditEvents.Remove(entry);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    private static AuditEventEntity Event(string? buildingId, string? apartmentId, byte outcome, string action) => new()
    {
        AuditEventId = Guid.NewGuid(),
        BuildingId = buildingId,
        ResourceType = apartmentId == null ? null : "Apartment",
        ResourceReference = apartmentId,
        Outcome = outcome,
        EventType = action,
        EventCategory = "Business",
        OccurredAtUtc = DateTime.UtcNow
    };
}
