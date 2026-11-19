using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class BuildingSqlMapperTargetSchemaTests
{
    [Fact]
    public async Task LoadAsync_UsesTargetSchemaWithoutQueryingRetiredAggregateTables()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new AppDbContext(options);
        db.Buildings.Add(new BuildingEntity
        {
            Id = "building-target-1",
            BuildingNumber = "B-01",
            Name = "Target building",
            DataVersion = "4.0",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await BuildingSqlMapper.LoadAsync(db, "building-target-1");

        Assert.NotNull(result);
        Assert.Equal("building-target-1", result!.Id);
        Assert.Equal("Target building", result.Name);
        Assert.Empty(result.Months);
        Assert.Empty(result.Notifications);
        Assert.Empty(result.Polls);
        Assert.Empty(result.MaintenanceLog);
    }
}
