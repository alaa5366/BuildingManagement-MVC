using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class TargetMaintenanceServiceTests
{
    [Fact]
    public async Task AddAndGetPageAsync_UsesBuildingScopedTargetRecords()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.AddRange(
                new BuildingEntity { Id = "B1", BuildingNumber = "1", Name = "One" },
                new BuildingEntity { Id = "B2", BuildingNumber = "2", Name = "Two" });
            await seed.SaveChangesAsync();
        }

        var service = new TargetMaintenanceService(new TestContextFactory(options));
        var first = await service.AddAsync(
            "B1", "Pump repair", "Basement pump", "2026-11-18", 250m,
            "Vendor A", "open", "admin");
        await service.AddAsync(
            "B2", "Other building repair", null, "2026-11-18", 500m,
            null, "done", "admin");

        var page = await service.GetPageAsync("B1", 1, 50);

        Assert.Single(page.Items);
        Assert.Equal(first.Id, page.Items[0].Id);
        Assert.Equal("Pump repair", page.Items[0].Title);
        Assert.StartsWith("mnt-", first.Id);
    }

    [Fact]
    public async Task UpdateAndDeleteAsync_ProtectMaintenanceAlreadyPostedToFinance()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(new BuildingEntity { Id = "B1", BuildingNumber = "1", Name = "One" });
            seed.MaintenanceRecords.Add(new MaintenanceRecordEntity
            {
                BuildingId = "B1", Id = "mnt-" + Guid.NewGuid().ToString("N"),
                Title = "Posted repair", MaintenanceDate = new DateOnly(2026, 11, 1),
                Cost = 100m, Status = "done", AddedToExpense = true
            });
            await seed.SaveChangesAsync();
        }

        var service = new TargetMaintenanceService(new TestContextFactory(options));
        await using var verify = new AppDbContext(options);
        var recordId = await verify.MaintenanceRecords.Select(item => item.Id).SingleAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync("B1", recordId, "Changed", null, "2026-11-02",
                200m, null, "done"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync("B1", recordId));
    }


    [Fact]
    public async Task UpdateAndDeleteAsync_ProtectSourceWhenLedgerPostingExistsButFlagIsStale()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var sourceId = Guid.NewGuid();
        var maintenanceId = "mnt-" + sourceId.ToString("N");
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(new BuildingEntity { Id = "B1", BuildingNumber = "1", Name = "One" });
            seed.MaintenanceRecords.Add(new MaintenanceRecordEntity
            {
                BuildingId = "B1", Id = maintenanceId, Title = "Posted repair",
                MaintenanceDate = new DateOnly(2026, 11, 1), Cost = 100m,
                Status = "done", AddedToExpense = false
            });
            seed.LedgerTransactions.Add(new LedgerTransactionEntity
            {
                LedgerTransactionId = Guid.NewGuid(), BuildingId = "B1",
                Kind = LedgerTransactionKind.Expense, Status = LedgerTransactionStatus.Posted,
                SourceType = "Expense", SourceKey = sourceId.ToString("D"),
                IdempotencyKey = "maintenance-posted-expense",
                CreatedAtUtc = DateTime.UtcNow, PostedAtUtc = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        var service = new TargetMaintenanceService(new TestContextFactory(options));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync("B1", maintenanceId, "Changed", null, "2026-11-02",
                200m, null, "done"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync("B1", maintenanceId));
    }

    [Fact]
    public async Task MarkAddedToExpenseAsync_UpdatesOnlyTheMatchingBuildingRecord()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var id = "mnt-" + Guid.NewGuid().ToString("N");
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.AddRange(
                new BuildingEntity { Id = "B1", BuildingNumber = "1", Name = "One" },
                new BuildingEntity { Id = "B2", BuildingNumber = "2", Name = "Two" });
            seed.MaintenanceRecords.Add(new MaintenanceRecordEntity
            {
                BuildingId = "B1", Id = id, Title = "Repair",
                MaintenanceDate = new DateOnly(2026, 11, 18), Cost = 50m
            });
            await seed.SaveChangesAsync();
        }

        var service = new TargetMaintenanceService(new TestContextFactory(options));
        Assert.False(await service.MarkAddedToExpenseAsync("B2", id));
        Assert.True(await service.MarkAddedToExpenseAsync("B1", id));

        await using var verify = new AppDbContext(options);
        var record = await verify.MaintenanceRecords.SingleAsync();
        Assert.True(record.AddedToExpense);
        Assert.NotNull(record.AddedToExpenseAt);
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
