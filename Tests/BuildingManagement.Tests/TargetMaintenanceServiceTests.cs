using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class TargetMaintenanceServiceTests
{
    [Fact]
    public async Task AddAndGetPageAsync_UsesNormalizedBuildingScopedRequests()
    {
        var options = CreateOptions();
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.AddRange(Building("B1"), Building("B2"));
            await seed.SaveChangesAsync();
        }

        var service = new TargetMaintenanceService(new TestContextFactory(options));
        var first = await service.AddAsync("B1", "Pump repair", "Basement pump",
            "2026-11-18", 0m, "Vendor A", "open", "admin");
        await service.AddAsync("B2", "Other building repair", null,
            "2026-11-18", 0m, null, "done", "admin");

        var page = await service.GetPageAsync("B1", 1, 50);

        Assert.Single(page.Items);
        Assert.Equal(first.Id, page.Items[0].Id);
        Assert.Equal("Pump repair", page.Items[0].Title);
        Assert.StartsWith("mnt-", first.Id);
        await using var verify = new AppDbContext(options);
        Assert.Equal(2, await verify.MaintenanceRequests.CountAsync());
        Assert.Empty(await verify.MaintenanceRecords.ToListAsync());
        var request = await verify.MaintenanceRequests.Include(x => x.StatusEvents)
            .Include(x => x.Assignments).SingleAsync(x => x.BuildingId == "B1");
        Assert.Single(request.StatusEvents);
        Assert.Equal("Vendor A", Assert.Single(request.Assignments).VendorName);
    }

    [Fact]
    public async Task AddAsync_RejectsCostThatCannotBePersistedOutsideTheLedger()
    {
        var options = CreateOptions();
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(Building("B1"));
            await seed.SaveChangesAsync();
        }
        var service = new TargetMaintenanceService(new TestContextFactory(options));
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddAsync(
            "B1", "Repair", null, "2026-11-18", 100m, null, "open", "admin"));
        await using var verify = new AppDbContext(options);
        Assert.Empty(await verify.MaintenanceRequests.ToListAsync());
    }

    [Fact]
    public async Task UpdateAndDeleteAsync_ProtectRequestLinkedToPostedLedgerTransaction()
    {
        var options = CreateOptions();
        var requestId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(Building("B1"));
            seed.FinancialPeriods.Add(Period("B1"));
            seed.LedgerTransactions.Add(PostedExpense("B1", transactionId, requestId.ToString("D")));
            seed.MaintenanceRequests.Add(Request("B1", requestId));
            seed.MaintenanceLedgerLinks.Add(new MaintenanceLedgerLinkEntity
            {
                LinkId = Guid.NewGuid(), RequestId = requestId,
                LedgerTransactionId = transactionId, LinkType = 1, CreatedAtUtc = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        var service = new TargetMaintenanceService(new TestContextFactory(options));
        var id = "mnt-" + requestId.ToString("N");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync("B1", id, "Changed", null, "2026-11-02", 0m, null, "done"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync("B1", id));
    }

    [Fact]
    public async Task UpdateAndDeleteAsync_ProtectPostedExpenseEvenWhenMaintenanceLinkIsMissing()
    {
        var options = CreateOptions();
        var requestId = Guid.NewGuid();
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(Building("B1"));
            seed.FinancialPeriods.Add(Period("B1"));
            seed.LedgerTransactions.Add(PostedExpense("B1", Guid.NewGuid(), requestId.ToString("D")));
            seed.MaintenanceRequests.Add(Request("B1", requestId));
            await seed.SaveChangesAsync();
        }

        var service = new TargetMaintenanceService(new TestContextFactory(options));
        var id = "mnt-" + requestId.ToString("N");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync("B1", id, "Changed", null, "2026-11-02", 0m, null, "done"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync("B1", id));
    }

    [Fact]
    public async Task MarkAddedToExpenseAsync_CreatesNormalizedLedgerLinkAndDerivesCost()
    {
        var options = CreateOptions();
        var requestId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(Building("B1"));
            seed.FinancialPeriods.Add(Period("B1"));
            seed.MaintenanceRequests.Add(Request("B1", requestId));
            seed.LedgerTransactions.Add(PostedExpense("B1", transactionId, requestId.ToString("D")));
            seed.LedgerEntries.Add(new LedgerEntryEntity
            {
                LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = transactionId,
                PeriodId = PeriodId, BuildingId = "B1", ApartmentId = "A1",
                EntryKind = LedgerEntryKind.ExpenseAllocation, SignedAmount = -50m,
                CurrencyCode = "EGP", EffectiveAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        var service = new TargetMaintenanceService(new TestContextFactory(options));
        var id = "mnt-" + requestId.ToString("N");
        Assert.False(await service.MarkAddedToExpenseAsync("B2", id));
        Assert.True(await service.MarkAddedToExpenseAsync("B1", id));
        var record = await service.GetAsync("B1", id);
        Assert.NotNull(record);
        Assert.True(record!.AddedToExpense);
        Assert.Equal(50m, record.Cost);
        await using var verify = new AppDbContext(options);
        Assert.Single(await verify.MaintenanceLedgerLinks.ToListAsync());
        Assert.Empty(await verify.MaintenanceRecords.ToListAsync());
    }

    [Fact]
    public async Task DeleteAsync_RejectsRequestsWithStoredAttachments()
    {
        var options = CreateOptions();
        var requestId = Guid.NewGuid();
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(Building("B1"));
            seed.MaintenanceRequests.Add(Request("B1", requestId));
            seed.MaintenanceAttachments.Add(new MaintenanceAttachmentEntity
            {
                AttachmentId = Guid.NewGuid(), RequestId = requestId,
                StorageKey = "maintenance/file", FileName = "invoice.pdf",
                SizeBytes = 10, UploadedAtUtc = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }
        var service = new TargetMaintenanceService(new TestContextFactory(options));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync("B1", "mnt-" + requestId.ToString("N")));
    }

    private static readonly Guid PeriodId = Guid.NewGuid();

    private static DbContextOptions<AppDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;

    private static BuildingEntity Building(string id) => new()
    {
        Id = id, BuildingNumber = id, Name = id
    };

    private static FinancialPeriodEntity Period(string buildingId) => new()
    {
        PeriodId = PeriodId, BuildingId = buildingId, PeriodKey = "2026-11",
        TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Open,
        StartsAtUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
        EndsAtUtc = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
        CreatedAtUtc = DateTime.UtcNow
    };

    private static MaintenanceRequestEntity Request(string buildingId, Guid requestId) => new()
    {
        RequestId = requestId, BuildingId = buildingId, Title = "Repair",
        Description = "Test request", Status = 2, Priority = 2,
        RequestedByReference = "admin", CreatedAtUtc = DateTime.UtcNow
    };

    private static LedgerTransactionEntity PostedExpense(string buildingId, Guid transactionId, string sourceKey) => new()
    {
        LedgerTransactionId = transactionId, PeriodId = PeriodId, BuildingId = buildingId,
        Kind = LedgerTransactionKind.Expense, Status = LedgerTransactionStatus.Posted,
        SourceType = "Expense", SourceKey = sourceKey,
        IdempotencyKey = "maintenance-expense-" + transactionId.ToString("N"),
        CreatedAtUtc = DateTime.UtcNow, PostedAtUtc = DateTime.UtcNow
    };

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
