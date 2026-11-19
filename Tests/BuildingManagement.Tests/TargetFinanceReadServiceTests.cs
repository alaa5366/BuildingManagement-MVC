using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class TargetFinanceReadServiceTests
{
    [Fact]
    public async Task GetExpenseCategoryTotalsAsync_NetsReversalInItsPostingPeriod()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var originalPeriodId = Guid.NewGuid();
        var reversalPeriodId = Guid.NewGuid();
        var originalExpenseId = Guid.NewGuid();
        var currentExpenseId = Guid.NewGuid();
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
            seed.Expenses.AddRange(
                new ExpenseEntity
                {
                    BuildingId = "B1", Id = originalExpenseId.ToString("N"), MonthKey = "2026-10",
                    CategoryId = "maintenance", Amount = 100m,
                    ExpenseDate = new DateOnly(2026, 10, 15), Note = "Original maintenance"
                },
                new ExpenseEntity
                {
                    BuildingId = "B1", Id = currentExpenseId.ToString("N"), MonthKey = "2026-11",
                    CategoryId = "maintenance", Amount = 30m,
                    ExpenseDate = new DateOnly(2026, 11, 15), Note = "Current maintenance"
                });
            seed.LedgerTransactions.AddRange(
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = originalTransactionId, PeriodId = originalPeriodId,
                    BuildingId = "B1", Kind = LedgerTransactionKind.Expense,
                    Status = LedgerTransactionStatus.Posted, SourceSystem = "application",
                    SourceType = "Expense", SourceKey = originalExpenseId.ToString("D"),
                    IdempotencyKey = "expense-original", CreatedAtUtc = now, PostedAtUtc = now
                },
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = reversalTransactionId, PeriodId = reversalPeriodId,
                    BuildingId = "B1", Kind = LedgerTransactionKind.Reversal,
                    Status = LedgerTransactionStatus.Posted, SourceSystem = "application",
                    SourceType = "ExpenseReversal", SourceKey = originalExpenseId.ToString("D"),
                    ReversesTransactionId = originalTransactionId,
                    IdempotencyKey = "expense-reversal", CreatedAtUtc = now, PostedAtUtc = now
                },
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = currentTransactionId, PeriodId = reversalPeriodId,
                    BuildingId = "B1", Kind = LedgerTransactionKind.Expense,
                    Status = LedgerTransactionStatus.Posted, SourceSystem = "application",
                    SourceType = "Expense", SourceKey = currentExpenseId.ToString("D"),
                    IdempotencyKey = "expense-current", CreatedAtUtc = now, PostedAtUtc = now
                });
            await seed.SaveChangesAsync();
        }

        var service = new TargetFinanceReadService(new TestContextFactory(options));
        var totals = await service.GetExpenseCategoryTotalsAsync("B1", new[] { "2026-10", "2026-11" });

        Assert.Equal(30m, totals["maintenance"]);
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
