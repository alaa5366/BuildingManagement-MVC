using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class TargetFinanceReadServiceTests
{
    [Fact]
    public async Task GetExpenseCategoryTotalsAsync_NetsReversalInItsPostingPeriod_WithoutLegacyExpenseRows()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var originalPeriodId = Guid.NewGuid();
        var reversalPeriodId = Guid.NewGuid();
        var originalTransactionId = Guid.NewGuid();
        var reversalTransactionId = Guid.NewGuid();
        var currentTransactionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(new BuildingEntity { Id = "B1", BuildingNumber = "1", Name = "One" });
            seed.FinancialPeriods.AddRange(
                new FinancialPeriodEntity
                {
                    PeriodId = originalPeriodId, BuildingId = "B1", PeriodKey = "2026-10",
                    TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Closed,
                    StartsAtUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndsAtUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedAtUtc = now
                },
                new FinancialPeriodEntity
                {
                    PeriodId = reversalPeriodId, BuildingId = "B1", PeriodKey = "2026-11",
                    TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Open,
                    StartsAtUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndsAtUtc = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedAtUtc = now
                });
            seed.LedgerTransactions.AddRange(
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = originalTransactionId, PeriodId = originalPeriodId,
                    BuildingId = "B1", Kind = LedgerTransactionKind.Expense,
                    Status = LedgerTransactionStatus.Posted, SourceSystem = "application",
                    SourceType = "Expense", SourceKey = Guid.NewGuid().ToString("D"),
                    IdempotencyKey = "expense-original", CreatedAtUtc = now, PostedAtUtc = now
                },
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = reversalTransactionId, PeriodId = reversalPeriodId,
                    BuildingId = "B1", Kind = LedgerTransactionKind.Reversal,
                    Status = LedgerTransactionStatus.Posted, SourceSystem = "application",
                    SourceType = "ExpenseReversal", SourceKey = Guid.NewGuid().ToString("D"),
                    ReversesTransactionId = originalTransactionId,
                    IdempotencyKey = "expense-reversal", CreatedAtUtc = now, PostedAtUtc = now
                },
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = currentTransactionId, PeriodId = reversalPeriodId,
                    BuildingId = "B1", Kind = LedgerTransactionKind.Expense,
                    Status = LedgerTransactionStatus.Posted, SourceSystem = "application",
                    SourceType = "Expense", SourceKey = Guid.NewGuid().ToString("D"),
                    IdempotencyKey = "expense-current", CreatedAtUtc = now, PostedAtUtc = now
                });
            seed.LedgerEntries.AddRange(
                Entry(originalTransactionId, originalPeriodId, "A1", -100m, now.AddDays(-20)),
                Entry(reversalTransactionId, reversalPeriodId, "A1", 100m, now.AddDays(-2)),
                Entry(currentTransactionId, reversalPeriodId, "A1", -30m, now.AddDays(-1)));
            await seed.SaveChangesAsync();
        }

        var service = new TargetFinanceReadService(new TestContextFactory(options));
        var totals = await service.GetExpenseCategoryTotalsAsync("B1", new[] { "2026-10", "2026-11" });

        Assert.Equal(30m, totals[""]);
    }

    [Fact]
    public async Task GetMonthAsync_UsesPostedLedgerRowsAndDoesNotRequireRetiredExpenseTable()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var periodId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var now = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        await using (var seed = new AppDbContext(options))
        {
            seed.FinancialPeriods.Add(new FinancialPeriodEntity
            {
                PeriodId = periodId, BuildingId = "B1", PeriodKey = "2026-11",
                TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Open,
                StartsAtUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                EndsAtUtc = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAtUtc = now
            });
            seed.LedgerTransactions.Add(new LedgerTransactionEntity
            {
                LedgerTransactionId = transactionId, PeriodId = periodId, BuildingId = "B1",
                Kind = LedgerTransactionKind.Expense, Status = LedgerTransactionStatus.Posted,
                SourceSystem = "application", SourceType = "Expense", SourceKey = Guid.NewGuid().ToString("D"),
                IdempotencyKey = "expense-one", Reason = "Lift maintenance",
                CreatedAtUtc = now, PostedAtUtc = now
            });
            seed.LedgerEntries.Add(Entry(transactionId, periodId, "A1", -125m, now));
            await seed.SaveChangesAsync();
        }

        var service = new TargetFinanceReadService(new TestContextFactory(options));
        var snapshot = await service.GetMonthAsync("B1", "2026-11", "expense");

        var item = Assert.Single(snapshot.Expenses);
        Assert.Equal(125m, item.Amount);
        Assert.Equal("Lift maintenance", item.Note);
        Assert.Equal("", item.CategoryId);
        Assert.Equal(125m, snapshot.Total);
    }

    private static LedgerEntryEntity Entry(Guid transactionId, Guid periodId, string apartmentId,
        decimal amount, DateTime effectiveAtUtc) => new()
    {
        LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = transactionId, PeriodId = periodId,
        BuildingId = "B1", ApartmentId = apartmentId, EntryKind = LedgerEntryKind.ExpenseAllocation,
        SignedAmount = amount, CurrencyCode = "EGP", EffectiveAtUtc = effectiveAtUtc,
        CreatedAtUtc = effectiveAtUtc
    };

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
