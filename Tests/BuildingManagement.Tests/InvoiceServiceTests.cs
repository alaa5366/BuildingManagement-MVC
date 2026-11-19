using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class InvoiceServiceTests
{
    [Fact]
    public async Task BuildInvoiceDataAsync_UsesTargetLedgerDepositsAndApartmentScope()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var now = DateTime.UtcNow;
        var priorPeriodId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var priorDepositTransactionId = Guid.NewGuid();
        var priorDepositReversalId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var chargeTransactionId = Guid.NewGuid();
        var expenseTransactionId = Guid.NewGuid();
        var expenseReversalId = Guid.NewGuid();
        var expenseSourceId = Guid.NewGuid();
        var activeExpenseTransactionId = Guid.NewGuid();
        var activeExpenseSourceId = Guid.NewGuid();
        var depositId = Guid.NewGuid();

        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(new BuildingEntity
            {
                Id = "B1", Name = "Target Building", BuildingNumber = "01",
                PaymentLabel = "Bank", PaymentAccountNumber = "12345",
                PaymentPhone = "201000000000", PaymentNotes = "Transfer note"
            });
            seed.BuildingSettings.Add(new BuildingSettingsEntity { BuildingId = "B1", Currency = "EGP" });
            seed.Floors.Add(new FloorEntity
            {
                BuildingId = "B1", Id = "F1", Label = "First Floor", SortOrder = 1
            });
            seed.Apartments.Add(new ApartmentEntity
            {
                BuildingId = "B1", Id = "A1", FloorId = "F1", Number = 101,
                MonthlyFee = 100m, IsClosed = false, IsDisabled = false
            });

            seed.FinancialPeriods.Add(new FinancialPeriodEntity
            {
                PeriodId = priorPeriodId, BuildingId = "B1", PeriodKey = "2026-10",
                TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Closed,
                StartsAtUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                EndsAtUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAtUtc = now
            });
            seed.FinancialPeriods.Add(new FinancialPeriodEntity
            {
                PeriodId = periodId, BuildingId = "B1", PeriodKey = "2026-11",
                TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Open,
                StartsAtUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                EndsAtUtc = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAtUtc = now
            });
            seed.LedgerTransactions.AddRange(
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = priorDepositTransactionId, PeriodId = priorPeriodId, BuildingId = "B1",
                    Kind = LedgerTransactionKind.Deposit, Status = LedgerTransactionStatus.Posted,
                    IdempotencyKey = "invoice-prior-deposit", CreatedAtUtc = now, PostedAtUtc = now
                },
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = priorDepositReversalId, PeriodId = periodId, BuildingId = "B1",
                    Kind = LedgerTransactionKind.Reversal, Status = LedgerTransactionStatus.Posted,
                    IdempotencyKey = "invoice-prior-deposit-reversal",
                    ReversesTransactionId = priorDepositTransactionId,
                    CreatedAtUtc = now, PostedAtUtc = now
                });
            seed.LedgerEntries.AddRange(
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = priorDepositTransactionId,
                    PeriodId = priorPeriodId, BuildingId = "B1", ApartmentId = "A1",
                    EntryKind = LedgerEntryKind.Deposit, SignedAmount = 50m, CurrencyCode = "EGP",
                    EffectiveAtUtc = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
                    CreatedAtUtc = now
                },
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = priorDepositReversalId,
                    PeriodId = periodId, BuildingId = "B1", ApartmentId = "A1",
                    EntryKind = LedgerEntryKind.Reversal, SignedAmount = -50m, CurrencyCode = "EGP",
                    EffectiveAtUtc = now, CreatedAtUtc = now
                });
            seed.PaymentDeposits.Add(new PaymentDepositEntity
            {
                PaymentDepositId = Guid.NewGuid(), PeriodId = priorPeriodId, BuildingId = "B1",
                ApartmentId = "A1", PostedTransactionId = priorDepositTransactionId,
                ReversalTransactionId = priorDepositReversalId, Amount = 50m,
                CurrencyCode = "EGP", Status = PaymentDepositStatus.Reversed,
                SubmittedAtUtc = now, ConfirmedAtUtc = now, ReversedAtUtc = now,
                SourceSystem = "application"
            });
            seed.LedgerTransactions.Add(new LedgerTransactionEntity
            {
                LedgerTransactionId = transactionId, PeriodId = periodId, BuildingId = "B1",
                Kind = LedgerTransactionKind.Deposit, Status = LedgerTransactionStatus.Posted,
                IdempotencyKey = "invoice-deposit", CreatedAtUtc = now, PostedAtUtc = now
            });
            seed.LedgerEntries.AddRange(
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = transactionId,
                    PeriodId = periodId, BuildingId = "B1", ApartmentId = "A1",
                    EntryKind = LedgerEntryKind.Deposit, SignedAmount = 200m, CurrencyCode = "EGP",
                    EffectiveAtUtc = now, CreatedAtUtc = now
                });
            seed.LedgerTransactions.Add(new LedgerTransactionEntity
            {
                LedgerTransactionId = chargeTransactionId, PeriodId = periodId, BuildingId = "B1",
                Kind = LedgerTransactionKind.MonthlyCharge, Status = LedgerTransactionStatus.Posted,
                IdempotencyKey = "invoice-monthly-charge", CreatedAtUtc = now, PostedAtUtc = now
            });
            seed.LedgerEntries.Add(new LedgerEntryEntity
            {
                LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = chargeTransactionId,
                PeriodId = periodId, BuildingId = "B1", ApartmentId = "A1",
                EntryKind = LedgerEntryKind.MonthlyCharge, SignedAmount = -125m, CurrencyCode = "EGP",
                EffectiveAtUtc = now, CreatedAtUtc = now
            });
            seed.LedgerTransactions.AddRange(
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = expenseTransactionId, PeriodId = periodId, BuildingId = "B1",
                    Kind = LedgerTransactionKind.Expense, Status = LedgerTransactionStatus.Posted,
                    IdempotencyKey = "invoice-expense", SourceSystem = "application",
                    SourceType = "Expense", SourceKey = expenseSourceId.ToString("D"),
                    CreatedAtUtc = now, PostedAtUtc = now
                },
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = expenseReversalId, PeriodId = periodId, BuildingId = "B1",
                    Kind = LedgerTransactionKind.Reversal, Status = LedgerTransactionStatus.Posted,
                    IdempotencyKey = "invoice-expense-reversal", SourceSystem = "application",
                    SourceType = "ExpenseReversal", SourceKey = Guid.NewGuid().ToString("D"),
                    ReversesTransactionId = expenseTransactionId,
                    CreatedAtUtc = now, PostedAtUtc = now
                });
            seed.LedgerTransactions.Add(new LedgerTransactionEntity
            {
                LedgerTransactionId = activeExpenseTransactionId, PeriodId = periodId, BuildingId = "B1",
                Kind = LedgerTransactionKind.Expense, Status = LedgerTransactionStatus.Posted,
                IdempotencyKey = "invoice-active-expense", SourceSystem = "application",
                SourceType = "Expense", SourceKey = activeExpenseSourceId.ToString("D"),
                CreatedAtUtc = now, PostedAtUtc = now
            });
            seed.LedgerEntries.AddRange(
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = expenseTransactionId,
                    PeriodId = periodId, BuildingId = "B1", ApartmentId = "A1",
                    EntryKind = LedgerEntryKind.ExpenseAllocation, SignedAmount = -40m,
                    CurrencyCode = "EGP", EffectiveAtUtc = now, CreatedAtUtc = now
                },
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = expenseReversalId,
                    PeriodId = periodId, BuildingId = "B1", ApartmentId = "A1",
                    EntryKind = LedgerEntryKind.Reversal, SignedAmount = 40m,
                    CurrencyCode = "EGP", EffectiveAtUtc = now, CreatedAtUtc = now
                },
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = activeExpenseTransactionId,
                    PeriodId = periodId, BuildingId = "B1", ApartmentId = "A1",
                    EntryKind = LedgerEntryKind.ExpenseAllocation, SignedAmount = -25m,
                    CurrencyCode = "EGP", EffectiveAtUtc = now, CreatedAtUtc = now
                });
            seed.FinancialCategories.AddRange(
                new FinancialCategoryEntity
                {
                    BuildingId = "B1", Id = "CAT1", Kind = "expense", Name = "Maintenance",
                    Color = "#123456", IsActive = true
                },
                new FinancialCategoryEntity
                {
                    BuildingId = "B1", Id = "CAT2", Kind = "expense", Name = "Cleaning",
                    Color = "#654321", IsActive = true
                });
            seed.Expenses.Add(new ExpenseEntity
            {
                BuildingId = "B1", Id = expenseSourceId.ToString("N"), MonthKey = "2026-11",
                CategoryId = "CAT1", CategoryKind = "expense", Note = "Reversed maintenance",
                Amount = 40m, ExpenseDate = new DateOnly(2026, 11, 10)
            });
            seed.Expenses.Add(new ExpenseEntity
            {
                BuildingId = "B1", Id = activeExpenseSourceId.ToString("N"), MonthKey = "2026-11",
                CategoryId = "CAT2", CategoryKind = "expense", Note = "Cleaning",
                Amount = 25m, ExpenseDate = new DateOnly(2026, 11, 12)
            });
            seed.PaymentDeposits.Add(new PaymentDepositEntity
            {
                PaymentDepositId = depositId, PeriodId = periodId, BuildingId = "B1",
                ApartmentId = "A1", PostedTransactionId = transactionId, Amount = 200m,
                CurrencyCode = "EGP", Status = PaymentDepositStatus.Confirmed,
                SubmittedAtUtc = now, ConfirmedAtUtc = now, SourceSystem = "application"
            });
            await seed.SaveChangesAsync();
        }

        var factory = new TestContextFactory(options);
        var wallet = new TargetWalletReadService(factory);
        var service = new InvoiceService(factory, wallet);
        var invoice = await service.BuildInvoiceDataAsync("B1", "A1", "2026-11");

        Assert.NotNull(invoice);
        Assert.Equal("Target Building", invoice!.Building.Name);
        Assert.Equal(101, invoice.Apt.Number);
        Assert.Equal(50m, invoice.Balance);
        Assert.Equal(50m, invoice.PreviousBalance);
        Assert.Equal(150m, invoice.MonthDeposits);
        Assert.Equal(125m, invoice.MonthFee);
        Assert.Equal(25m, invoice.MonthExpensesShare);
        var categoryShare = Assert.Single(invoice.CategoryShares);
        Assert.Equal("Cleaning", categoryShare.Name);
        Assert.Equal(25m, categoryShare.Total);
        Assert.Equal(25m, categoryShare.Share);
        Assert.Equal("12345", invoice.Payment.AccountNumber);
    }

    [Fact]
    public async Task FindBuildingIdByApartmentAsync_ReturnsNullWhenApartmentIdIsAmbiguous()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using (var seed = new AppDbContext(options))
        {
            seed.Apartments.AddRange(
                new ApartmentEntity { BuildingId = "B1", Id = "A1", FloorId = "F1", Number = 1 },
                new ApartmentEntity { BuildingId = "B2", Id = "A1", FloorId = "F1", Number = 1 });
            await seed.SaveChangesAsync();
        }

        var service = new InvoiceService(
            new TestContextFactory(options),
            new TargetWalletReadService(new TestContextFactory(options)));
        Assert.Null(await service.FindBuildingIdByApartmentAsync("A1"));
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
