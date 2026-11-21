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
    public async Task QueryPage_FiltersByCorrelationIdWithoutCrossingOtherEvents()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(options));
        await using (var db = new AppDbContext(options))
        {
            db.AuditEvents.AddRange(
                Event("b1", null, 0, "matched", "trace-123"),
                Event("b1", null, 0, "not-matched", "trace-456"));
            await db.SaveChangesAsync();
        }

        var logger = new AuditLogger(factory.Object, NullLogger<AuditLogger>.Instance);
        var page = await logger.QueryPageAsync("b1", null, null, null, null, null, null, 1, 10, "trace-123");

        var entry = Assert.Single(page.Items);
        Assert.Equal("matched", entry.Action);
        Assert.Equal("trace-123", entry.CorrelationId);
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

        var invalidSeverity = await logger.QueryPageAsync(null, null, null, null, null, null, "unknown", 1, 10);
        Assert.Empty(invalidSeverity.Items);
    }

    [Fact]
    public async Task QueryPage_EnrichesDepositReceiptsWithoutCrossingBuildingOrApartmentScope()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(options));
        var depositId = Guid.NewGuid();
        var receiptUrl = "https://res.cloudinary.com/example/image/upload/receipt.png";

        await using (var db = new AppDbContext(options))
        {
            db.PaymentDeposits.Add(new PaymentDepositEntity
            {
                PaymentDepositId = depositId,
                PeriodId = Guid.NewGuid(),
                BuildingId = "b1",
                ApartmentId = "a1",
                Amount = 125m,
                CurrencyCode = "EGP",
                Status = PaymentDepositStatus.Pending,
                SubmittedAtUtc = DateTime.UtcNow,
                ReceiptMetadataJson = $$"""{"Url":"{{receiptUrl}}"}""",
                SourceSystem = "application"
            });

            AuditEventEntity DepositEvent(string buildingId, string apartmentId)
            {
                var entry = Event(buildingId, apartmentId, 0, "PaymentDepositSubmitted");
                entry.SafeDetailsJson = $$"""{"paymentDepositId":"{{depositId:D}}"}""";
                return entry;
            }

            db.AuditEvents.AddRange(
                DepositEvent("b1", "a1"),
                DepositEvent("b1", "a2"),
                DepositEvent("b2", "a1"));
            await db.SaveChangesAsync();
        }

        var logger = new AuditLogger(factory.Object, NullLogger<AuditLogger>.Instance);
        var page = await logger.QueryPageAsync(null, null, null, null, null, null, null, 1, 10);

        var ownBuildingApartment = Assert.Single(page.Items.Where(item =>
            item.BuildingId == "b1" && item.ApartmentId == "a1"));
        Assert.Equal(receiptUrl, ownBuildingApartment.Metadata?["receiptUrl"]?.ToString());

        var sameBuildingOtherApartment = Assert.Single(page.Items.Where(item =>
            item.BuildingId == "b1" && item.ApartmentId == "a2"));
        Assert.False(sameBuildingOtherApartment.Metadata?.ContainsKey("receiptUrl") == true);

        var otherBuilding = Assert.Single(page.Items.Where(item =>
            item.BuildingId == "b2" && item.ApartmentId == "a1"));
        Assert.False(otherBuilding.Metadata?.ContainsKey("receiptUrl") == true);
    }

    [Fact]
    public async Task QueryPage_UsesOrderedTopWindowForStablePageBoundaries()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(options));
        var baseline = DateTime.UtcNow.AddMinutes(-10);
        await using (var db = new AppDbContext(options))
        {
            for (var i = 1; i <= 5; i++)
            {
                var item = Event(null, null, 0, $"event-{i}");
                item.OccurredAtUtc = baseline.AddMinutes(i);
                db.AuditEvents.Add(item);
            }
            await db.SaveChangesAsync();
        }

        var logger = new AuditLogger(factory.Object, NullLogger<AuditLogger>.Instance);
        var firstPage = await logger.QueryPageAsync(null, null, null, null, null, null, null, 1, 2);
        var secondPage = await logger.QueryPageAsync(null, null, null, null, null, null, null, 2, 2);
        var lastPage = await logger.QueryPageAsync(null, null, null, null, null, null, null, 3, 2);

        Assert.Equal(new[] { "event-5", "event-4" }, firstPage.Items.Select(x => x.Action));
        Assert.Equal(new[] { "event-3", "event-2" }, secondPage.Items.Select(x => x.Action));
        Assert.Equal("event-1", Assert.Single(lastPage.Items).Action);
        Assert.Equal(5, firstPage.TotalItems);
        Assert.Equal(3, lastPage.CurrentPage);
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

    private static AuditEventEntity Event(string? buildingId, string? apartmentId, byte outcome, string action, string? correlationId = null) => new()
    {
        AuditEventId = Guid.NewGuid(),
        BuildingId = buildingId,
        ResourceType = apartmentId == null ? null : "Apartment",
        ResourceReference = apartmentId,
        Outcome = outcome,
        EventType = action,
        CorrelationId = correlationId,
        EventCategory = "Business",
        OccurredAtUtc = DateTime.UtcNow
    };
}
