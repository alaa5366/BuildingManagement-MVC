using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BuildingManagement.Tests;

public class AuditLoggerTests
{
    [Fact]
    public async Task QueryPage_FiltersByApartmentAndSeverityInDatabaseQuery()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(options));
        await using (var db = new AppDbContext(options))
        {
            db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "Test Building", BuildingNumber = "BLD-901" });
            db.AuditLog.AddRange(
                new AuditLogEntity { BuildingId = "b1", ApartmentId = "a1", Severity = "warning", Action = "x", Ts = DateTime.UtcNow },
                new AuditLogEntity { BuildingId = "b1", ApartmentId = "a2", Severity = "warning", Action = "x", Ts = DateTime.UtcNow },
                new AuditLogEntity { BuildingId = "b1", ApartmentId = "a1", Severity = "info", Action = "x", Ts = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        var logger = new AuditLogger(factory.Object, NullLogger<AuditLogger>.Instance);
        var page = await logger.QueryPageAsync("b1", "a1", null, null, null, null, "warning", 1, 10);
        Assert.Single(page.Items);
        Assert.Equal("a1", page.Items[0].ApartmentId);
        Assert.Equal("warning", page.Items[0].Severity);
    }
}
