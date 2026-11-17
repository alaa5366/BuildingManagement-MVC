using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Xunit;

namespace BuildingManagement.Tests;

public class BuildingSqlMapperConcurrencyTests
{
    [Fact]
    public void ValidateAggregateConcurrency_AcceptsMatchingSqlServerRowVersion()
    {
        var token = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        BuildingSqlMapper.ValidateAggregateConcurrency(token, token.ToArray());
    }

    [Fact]
    public void ValidateAggregateConcurrency_RejectsMissingToken()
    {
        var current = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        Assert.Throws<DbUpdateConcurrencyException>(
            () => BuildingSqlMapper.ValidateAggregateConcurrency(null, current));
    }

    [Fact]
    public void ValidateAggregateConcurrency_RejectsStaleToken()
    {
        var expected = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var current = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };

        Assert.Throws<DbUpdateConcurrencyException>(
            () => BuildingSqlMapper.ValidateAggregateConcurrency(expected, current));
    }

    [Fact]
    public void MarkAggregateWrite_ForcesParentUpdateForChildOnlyChanges()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        var building = new BuildingEntity
        {
            Id = "building-1",
            BuildingNumber = "1",
            Name = "Test",
            DataVersion = "3.2"
        };

        db.Buildings.Attach(building);
        BuildingSqlMapper.MarkAggregateWrite(db, building);

        Assert.Equal(EntityState.Modified, db.Entry(building).State);
        Assert.True(db.Entry(building).Property(x => x.DataVersion).IsModified);
    }
}
