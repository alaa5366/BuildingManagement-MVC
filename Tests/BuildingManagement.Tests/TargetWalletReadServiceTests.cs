using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class TargetWalletReadServiceTests
{
    [Fact]
    public async Task GetApartmentAsync_SumsOnlyPostedEntriesForRequestedApartmentAndCurrency()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var periodId = Guid.NewGuid();
        await using (var seed = new AppDbContext(options))
        {
            seed.FinancialPeriods.Add(new FinancialPeriodEntity
            {
                PeriodId = periodId,
                BuildingId = "B1",
                PeriodKey = "2026-11",
                TimeZoneId = "Africa/Cairo",
                StartsAtUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                EndsAtUtc = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAtUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            var postedId = Guid.NewGuid();
            seed.LedgerTransactions.AddRange(
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = postedId,
                    PeriodId = periodId,
                    BuildingId = "B1",
                    Kind = LedgerTransactionKind.Deposit,
                    Status = LedgerTransactionStatus.Posted,
                    IdempotencyKey = "posted",
                    CreatedAtUtc = DateTime.UtcNow,
                    PostedAtUtc = DateTime.UtcNow
                },
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = Guid.NewGuid(),
                    PeriodId = periodId,
                    BuildingId = "B1",
                    Kind = LedgerTransactionKind.Deposit,
                    Status = LedgerTransactionStatus.Draft,
                    IdempotencyKey = "draft",
                    CreatedAtUtc = DateTime.UtcNow
                });
            seed.PaymentDeposits.AddRange(
                new PaymentDepositEntity
                {
                    PaymentDepositId = Guid.NewGuid(), PeriodId = periodId, BuildingId = "B1",
                    ApartmentId = "A1", Amount = 45m, CurrencyCode = "EGP",
                    Status = PaymentDepositStatus.Pending, SubmittedAtUtc = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc)
                },
                new PaymentDepositEntity
                {
                    PaymentDepositId = Guid.NewGuid(), PeriodId = periodId, BuildingId = "B1",
                    ApartmentId = "A2", Amount = 75m, CurrencyCode = "EGP",
                    Status = PaymentDepositStatus.Pending, SubmittedAtUtc = new DateTime(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc)
                });
            seed.LedgerEntries.AddRange(
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = postedId,
                    PeriodId = periodId, BuildingId = "B1", ApartmentId = "A1",
                    EntryKind = LedgerEntryKind.Deposit, SignedAmount = 100,
                    CurrencyCode = "EGP", EffectiveAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow
                },
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = postedId,
                    PeriodId = periodId, BuildingId = "B1", ApartmentId = "A1",
                    EntryKind = LedgerEntryKind.Deposit, SignedAmount = 25,
                    CurrencyCode = "USD", EffectiveAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow
                },
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = postedId,
                    PeriodId = periodId, BuildingId = "B1", ApartmentId = "A2",
                    EntryKind = LedgerEntryKind.Deposit, SignedAmount = 900,
                    CurrencyCode = "EGP", EffectiveAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow
                });
            await seed.SaveChangesAsync();
        }

        var service = new TargetWalletReadService(new TestContextFactory(options));
        var result = await service.GetApartmentAsync("B1", "A1");

        Assert.Equal(100m, result.Balance);
        var entry = Assert.Single(result.Transactions);
        Assert.Equal("deposit", entry.Type);
        Assert.Equal(100m, entry.Amount);
        var pending = Assert.Single(result.PendingDeposits);
        Assert.Equal("A1", pending.AptId);
        Assert.Equal(45m, pending.Amount);
        Assert.Equal("pending", pending.Status);
    }

    [Fact]
    public async Task GetApartmentAsync_RejectsInvalidMonthAndCurrency()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var service = new TargetWalletReadService(new TestContextFactory(options));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetApartmentAsync("B1", "A1", fromMonth: "2026-13"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetApartmentAsync("B1", "A1", currencyCode: "EG"));
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
