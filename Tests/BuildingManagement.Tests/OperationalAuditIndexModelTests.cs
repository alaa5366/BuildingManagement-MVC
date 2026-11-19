using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class OperationalAuditIndexModelTests
{
    [Fact]
    public void AuditEvents_ExposeScopedAggregateQueryIndex()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new AppDbContext(options);

        var entity = db.Model.FindEntityType(typeof(AuditEventEntity));
        Assert.NotNull(entity);

        var index = Assert.Single(entity!.GetIndexes().Where(x =>
            x.GetDatabaseName() == "IX_AuditEvents_Building_Category_Resource_Time"));

        Assert.Equal(
            new[] { nameof(AuditEventEntity.BuildingId), nameof(AuditEventEntity.EventCategory),
                nameof(AuditEventEntity.ResourceType), nameof(AuditEventEntity.OccurredAtUtc),
                nameof(AuditEventEntity.AuditEventId) },
            index.Properties.Select(x => x.Name));
        Assert.Equal(new[] { false, false, false, true, true }, index.IsDescending);
    }
}
