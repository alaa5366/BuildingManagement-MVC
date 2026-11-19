using BuildingManagementMvc.Services;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class ReportsServiceTests
{

    [Fact]
    public async Task BuildReportAsync_UsesTargetLedgerAndAllocationSourceRecords()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var now = DateTime.UtcNow;
        var month = now.ToString("yyyy-MM");
        var periodId = Guid.NewGuid();
        var depositTx = Guid.NewGuid();
        var expenseTx = Guid.NewGuid();
        var revenueTx = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        var revenueId = Guid.NewGuid();

        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(new BuildingEntity
            {
                Id = "B1", Name = "Target Building", BuildingNumber = "01"
            });
            seed.BuildingSettings.Add(new BuildingSettingsEntity { BuildingId = "B1", Currency = "EGP" });
            seed.Apartments.Add(new ApartmentEntity
            {
                BuildingId = "B1", Id = "A1", FloorId = "F1", Number = 101,
                MonthlyFee = 100m, IsClosed = false, IsDisabled = false
            });
            seed.FinancialPeriods.Add(new FinancialPeriodEntity
            {
                PeriodId = periodId, BuildingId = "B1", PeriodKey = month,
                TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Open,
                StartsAtUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                EndsAtUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1),
                CreatedAtUtc = now
            });
            seed.FinancialCategories.Add(new FinancialCategoryEntity
            {
                BuildingId = "B1", Id = "C1", Kind = "expense",
                Name = "Maintenance", Color = "#123456", IsActive = true, SortOrder = 1
            });
            seed.Expenses.Add(new ExpenseEntity
            {
                BuildingId = "B1", Id = expenseId.ToString("N"), MonthKey = month,
                CategoryId = "C1", CategoryKind = "expense", Note = "Repair",
                Amount = 200m, ExpenseDate = DateOnly.FromDateTime(now)
            });
            seed.Revenues.Add(new RevenueEntity
            {
                BuildingId = "B1", Id = revenueId.ToString("N"), MonthKey = month,
                CategoryId = null, CategoryKind = "revenue", Note = "Other income",
                Amount = 100m, RevenueDate = DateOnly.FromDateTime(now)
            });
            seed.LedgerTransactions.AddRange(
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = depositTx, PeriodId = periodId, BuildingId = "B1",
                    Kind = LedgerTransactionKind.Deposit, Status = LedgerTransactionStatus.Posted,
                    IdempotencyKey = "report-deposit", CreatedAtUtc = now, PostedAtUtc = now
                },
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = expenseTx, PeriodId = periodId, BuildingId = "B1",
                    Kind = LedgerTransactionKind.Expense, Status = LedgerTransactionStatus.Posted,
                    IdempotencyKey = "report-expense", SourceSystem = "application",
                    SourceType = "Expense", SourceKey = expenseId.ToString("D"),
                    CreatedAtUtc = now, PostedAtUtc = now
                },
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = revenueTx, PeriodId = periodId, BuildingId = "B1",
                    Kind = LedgerTransactionKind.Revenue, Status = LedgerTransactionStatus.Posted,
                    IdempotencyKey = "report-revenue", SourceSystem = "application",
                    SourceType = "Revenue", SourceKey = revenueId.ToString("D"),
                    CreatedAtUtc = now, PostedAtUtc = now
                });
            seed.LedgerEntries.AddRange(
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = depositTx,
                    PeriodId = periodId, BuildingId = "B1", ApartmentId = "A1",
                    EntryKind = LedgerEntryKind.Deposit, SignedAmount = 500m, CurrencyCode = "EGP",
                    EffectiveAtUtc = now, CreatedAtUtc = now
                },
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = expenseTx,
                    PeriodId = periodId, BuildingId = "B1", ApartmentId = "A1",
                    EntryKind = LedgerEntryKind.ExpenseAllocation, SignedAmount = -200m, CurrencyCode = "EGP",
                    EffectiveAtUtc = now, CreatedAtUtc = now
                },
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = revenueTx,
                    PeriodId = periodId, BuildingId = "B1", ApartmentId = "A1",
                    EntryKind = LedgerEntryKind.RevenueAllocation, SignedAmount = 100m, CurrencyCode = "EGP",
                    EffectiveAtUtc = now, CreatedAtUtc = now
                });
            await seed.SaveChangesAsync();
        }

        var factory = new TestContextFactory(options);
        var wallet = new TargetWalletReadService(factory);
        var finance = new TargetFinanceReadService(factory);
        var building = await wallet.GetBuildingShellAsync("B1");
        var service = new ReportsService(wallet, finance);
        var report = await service.BuildReportAsync(building!.Id, "6m", month);

        Assert.Equal(500m, report.TotalCollected);
        Assert.Equal(200m, report.TotalExpenses);
        Assert.Equal(100m, report.TotalRevenues);
        Assert.Equal(400m, report.NetBalance);
        Assert.Single(report.Categories);
        Assert.Equal(200m, report.Categories[0].Total);
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }

    [Fact]
    public void MonthLabel_ValidMonth_ReturnsLocalizedLabel()
    {
        var label = ReportsService.MonthLabel("2025-01");
        Assert.Contains("2025", label);
        Assert.NotEqual("2025-01", label);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("2025")]
    [InlineData("2025-13")]
    [InlineData("2025-00")]
    public void MonthLabel_InvalidMonth_ReturnsOriginal(string value) =>
        Assert.Equal(value, ReportsService.MonthLabel(value));

    [Fact]
    public void ShiftMonth_CrossesYearBoundaries()
    {
        Assert.Equal("2025-02", ReportsService.ShiftMonth("2025-01", 1));
        Assert.Equal("2024-12", ReportsService.ShiftMonth("2025-01", -1));
        Assert.Equal("2026-01", ReportsService.ShiftMonth("2025-12", 1));
    }

    [Fact]
    public void GetReportMonths_AllRange_UsesAvailableTargetPeriods()
    {
        var months = ReportsService.GetReportMonths(
            new[] { "2024-11", "2024-12", "2025-01" }, "all", "2025-01");
        Assert.Equal(new[] { "2024-11", "2024-12", "2025-01" }, months);
    }

    [Fact]
    public void GetReportMonths_YearRange_FiltersCurrentYear()
    {
        var months = ReportsService.GetReportMonths(
            new[] { "2024-11", "2025-01", "2025-02" }, "year", "2025-01");
        Assert.Equal(new[] { "2025-01", "2025-02" }, months);
    }

    [Theory]
    [InlineData("6m", 6)]
    [InlineData("12m", 12)]
    public void GetReportMonths_RollingRanges_ReturnExpectedCount(string range, int expected)
    {
        var months = ReportsService.GetReportMonths(Array.Empty<string>(), range, "2025-06");
        Assert.Equal(expected, months.Count);
        Assert.Equal("2025-06", months[^1]);
    }

    [Fact]
    public void GetReportMonths_AllRangeWithoutPeriods_FallsBackToCurrentMonth()
    {
        Assert.Equal(new[] { "2025-06" },
            ReportsService.GetReportMonths(Array.Empty<string>(), "all", "2025-06"));
    }
}
