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
        var periodId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var chargeTransactionId = Guid.NewGuid();
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
                PeriodId = periodId, BuildingId = "B1", PeriodKey = "2026-11",
                TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Open,
                StartsAtUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                EndsAtUtc = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAtUtc = now
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
        Assert.Equal(200m, invoice.Balance);
        Assert.Equal(0m, invoice.PreviousBalance);
        Assert.Equal(200m, invoice.MonthDeposits);
        Assert.Equal(125m, invoice.MonthFee);
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
