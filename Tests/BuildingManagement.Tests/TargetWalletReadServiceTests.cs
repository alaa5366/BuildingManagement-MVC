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
    public async Task GetBuildingShellAsync_UsesTargetBuildingFloorAndApartmentRows()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(new BuildingEntity
            {
                Id = "B1", Name = "Target Building", BuildingNumber = "01",
                AdminWhatsapp = "201000000000"
            });
            seed.Floors.Add(new FloorEntity
            {
                BuildingId = "B1", Id = "F1", Label = "First Floor", SortOrder = 1
            });
            seed.Apartments.AddRange(
                new ApartmentEntity
                {
                    BuildingId = "B1", Id = "A1", FloorId = "F1", Number = 101,
                    Owner = "Resident", Phone = "201000000001", MonthlyFee = 500m,
                    Label = "North", IsClosed = false, IsDisabled = false
                },
                new ApartmentEntity
                {
                    BuildingId = "B1", Id = "A2", FloorId = "F1", Number = 102,
                    Owner = "Other resident", Phone = "201000000002", MonthlyFee = 500m,
                    Label = "South", IsClosed = false, IsDisabled = false
                });
            await seed.SaveChangesAsync();
        }

        var service = new TargetWalletReadService(new TestContextFactory(options));
        var building = await service.GetBuildingShellAsync("B1", "A1");

        Assert.NotNull(building);
        Assert.Equal("Target Building", building!.Name);
        Assert.Equal("First Floor", Assert.Single(building.Floors).Label);
        var apartment = Assert.Single(building.Apartments);
        Assert.Equal("A1", apartment.Id);
        Assert.Equal(101, apartment.Number);
        Assert.Equal("North", apartment.Label);

        var adminBuilding = await service.GetBuildingShellAsync("B1");
        Assert.Equal(2, adminBuilding!.Apartments.Count);
    }

    [Fact]
    public async Task GetAdminSnapshotAsync_UsesTargetLedgerAndIncludesPendingDepositsAcrossPeriods()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var now = DateTime.UtcNow;
        var currentPeriodId = Guid.NewGuid();
        var previousPeriodId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var pendingId = Guid.NewGuid();

        await using (var seed = new AppDbContext(options))
        {
            seed.BuildingSettings.Add(new BuildingSettingsEntity { BuildingId = "B1", Currency = "EGP" });
            seed.FinancialPeriods.AddRange(
                new FinancialPeriodEntity
                {
                    PeriodId = previousPeriodId, BuildingId = "B1", PeriodKey = "2026-10",
                    TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Closed,
                    StartsAtUtc = now.AddMonths(-2), EndsAtUtc = now.AddMonths(-1), CreatedAtUtc = now.AddMonths(-2)
                },
                new FinancialPeriodEntity
                {
                    PeriodId = currentPeriodId, BuildingId = "B1", PeriodKey = "2026-11",
                    TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Open,
                    StartsAtUtc = now.AddMonths(-1), EndsAtUtc = now.AddMonths(1), CreatedAtUtc = now.AddMonths(-1)
                });
            seed.LedgerTransactions.Add(new LedgerTransactionEntity
            {
                LedgerTransactionId = transactionId, PeriodId = previousPeriodId, BuildingId = "B1",
                Kind = LedgerTransactionKind.Deposit, Status = LedgerTransactionStatus.Posted,
                IdempotencyKey = "posted", CreatedAtUtc = now.AddMonths(-1), PostedAtUtc = now.AddMonths(-1)
            });
            seed.LedgerEntries.Add(new LedgerEntryEntity
            {
                LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = transactionId,
                PeriodId = previousPeriodId, BuildingId = "B1", ApartmentId = "A1",
                EntryKind = LedgerEntryKind.Deposit, SignedAmount = 120m, CurrencyCode = "EGP",
                EffectiveAtUtc = now.AddMonths(-1), CreatedAtUtc = now.AddMonths(-1)
            });
            seed.PaymentDeposits.AddRange(
                new PaymentDepositEntity
                {
                    PaymentDepositId = pendingId, PeriodId = currentPeriodId, BuildingId = "B1",
                    ApartmentId = "A2", Amount = 75m, CurrencyCode = "EGP",
                    Status = PaymentDepositStatus.Pending, SubmittedAtUtc = now,
                    Note = "Pending evidence", SourceSystem = "application"
                },
                new PaymentDepositEntity
                {
                    PaymentDepositId = Guid.NewGuid(), PeriodId = currentPeriodId, BuildingId = "B1",
                    ApartmentId = "A2", Amount = 25m, CurrencyCode = "EGP",
                    Status = PaymentDepositStatus.Pending, SubmittedAtUtc = now.AddDays(-2),
                    Note = "Older pending", SourceSystem = "application"
                },
                new PaymentDepositEntity
                {
                    PaymentDepositId = Guid.NewGuid(), PeriodId = currentPeriodId, BuildingId = "B1",
                    ApartmentId = "A1", Amount = 40m, CurrencyCode = "EGP",
                    Status = PaymentDepositStatus.Confirmed, SubmittedAtUtc = now,
                    ConfirmedAtUtc = now, SourceSystem = "application"
                },
                new PaymentDepositEntity
                {
                    PaymentDepositId = Guid.NewGuid(), PeriodId = currentPeriodId, BuildingId = "B1",
                    ApartmentId = "A1", Amount = 90m, CurrencyCode = "EGP",
                    Status = PaymentDepositStatus.Reversed, SubmittedAtUtc = now,
                    ConfirmedAtUtc = now, ReversedAtUtc = now, SourceSystem = "application"
                });
            await seed.SaveChangesAsync();
        }

        var service = new TargetWalletReadService(new TestContextFactory(options));
        var snapshot = await service.GetAdminSnapshotAsync("B1", "2026-11");

        Assert.Equal(120m, snapshot.Balances["A1"]);
        Assert.Equal(2, snapshot.PendingDeposits.Count);
        var pending = snapshot.PendingDeposits[0];
        Assert.Equal(pendingId.ToString("D"), pending.Deposit.Id);
        Assert.Equal("A2", pending.ApartmentId);
        Assert.Equal("2026-11", pending.MonthKey);
        Assert.Equal(75m, pending.Deposit.Amount);
        Assert.Equal(40m, snapshot.Totals.Collected);
    }


    [Fact]
    public async Task GetApartmentAsync_MarksReversedDepositAsReversedInsteadOfConfirmed()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var now = DateTime.UtcNow;
        var periodId = Guid.NewGuid();
        var depositId = Guid.NewGuid();
        var originalId = Guid.NewGuid();
        var reversalId = Guid.NewGuid();

        await using (var seed = new AppDbContext(options))
        {
            seed.FinancialPeriods.Add(new FinancialPeriodEntity
            {
                PeriodId = periodId, BuildingId = "B1", PeriodKey = now.ToString("yyyy-MM"),
                TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Open,
                StartsAtUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                EndsAtUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1),
                CreatedAtUtc = now
            });
            seed.PaymentDeposits.Add(new PaymentDepositEntity
            {
                PaymentDepositId = depositId, PeriodId = periodId, BuildingId = "B1",
                ApartmentId = "A1", Amount = 90m, CurrencyCode = "EGP",
                Status = PaymentDepositStatus.Reversed, SubmittedAtUtc = now.AddDays(-2),
                ConfirmedAtUtc = now.AddDays(-1), PostedTransactionId = originalId,
                ReversedAtUtc = now, ReversalTransactionId = reversalId,
                ResolutionReason = "Duplicate payment", SourceSystem = "application"
            });
            seed.LedgerTransactions.AddRange(
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = originalId, PeriodId = periodId, BuildingId = "B1",
                    Kind = LedgerTransactionKind.Deposit, Status = LedgerTransactionStatus.Posted,
                    IdempotencyKey = "original-deposit", CreatedAtUtc = now.AddDays(-1),
                    PostedAtUtc = now.AddDays(-1)
                },
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = reversalId, PeriodId = periodId, BuildingId = "B1",
                    Kind = LedgerTransactionKind.Reversal, Status = LedgerTransactionStatus.Posted,
                    IdempotencyKey = "reverse-deposit", ReversesTransactionId = originalId,
                    CreatedAtUtc = now, PostedAtUtc = now
                });
            seed.LedgerEntries.AddRange(
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = originalId,
                    PeriodId = periodId, BuildingId = "B1", ApartmentId = "A1",
                    EntryKind = LedgerEntryKind.Deposit, SignedAmount = 90m,
                    CurrencyCode = "EGP", EffectiveAtUtc = now.AddDays(-1), CreatedAtUtc = now.AddDays(-1)
                },
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = reversalId,
                    PeriodId = periodId, BuildingId = "B1", ApartmentId = "A1",
                    EntryKind = LedgerEntryKind.Reversal, SignedAmount = -90m,
                    CurrencyCode = "EGP", EffectiveAtUtc = now, CreatedAtUtc = now
                });
            await seed.SaveChangesAsync();
        }

        var service = new TargetWalletReadService(new TestContextFactory(options));
        var snapshot = await service.GetApartmentAsync("B1", "A1");

        Assert.Equal(0m, snapshot.Balance);
        var original = Assert.Single(snapshot.Transactions.Where(item => item.Id != depositId.ToString("D") &&
            item.Type == "deposit"));
        Assert.Equal("reversed", original.Status);
        Assert.Contains(snapshot.Transactions, item => item.Type == "adjustment" && item.Amount == -90m);
    }

    [Fact]
    public async Task GetApartmentAsync_ShowsCancelledDepositsWithoutAddingThemToBalance()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var periodId = Guid.NewGuid();
        var cancelledId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using (var seed = new AppDbContext(options))
        {
            seed.FinancialPeriods.Add(new FinancialPeriodEntity
            {
                PeriodId = periodId, BuildingId = "B1", PeriodKey = now.ToString("yyyy-MM"),
                TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Open,
                StartsAtUtc = now.AddDays(-10), EndsAtUtc = now.AddDays(20), CreatedAtUtc = now
            });
            seed.PaymentDeposits.Add(new PaymentDepositEntity
            {
                PaymentDepositId = cancelledId, PeriodId = periodId, BuildingId = "B1",
                ApartmentId = "A1", Amount = 125m, CurrencyCode = "EGP",
                Status = PaymentDepositStatus.Cancelled, SubmittedAtUtc = now.AddDays(-2),
                CancelledAtUtc = now.AddDays(-1), ResolutionReason = "Duplicate transfer",
                SourceSystem = "application"
            });
            await seed.SaveChangesAsync();
        }

        var service = new TargetWalletReadService(new TestContextFactory(options));
        var snapshot = await service.GetApartmentAsync("B1", "A1");

        Assert.Equal(0m, snapshot.Balance);
        var cancelled = Assert.Single(snapshot.Transactions);
        Assert.Equal(cancelledId.ToString("D"), cancelled.Id);
        Assert.Equal("deposit", cancelled.Type);
        Assert.Equal("cancelled", cancelled.Status);
        Assert.Equal(125m, cancelled.Amount);
        Assert.Equal("Duplicate transfer", cancelled.Note);
        Assert.Empty(snapshot.PendingDeposits);
    }


    [Fact]
    public async Task GetApartmentAsync_KeepsCumulativeBalanceWhenStatementIsMonthFiltered()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var priorPeriodId = Guid.NewGuid();
        var currentPeriodId = Guid.NewGuid();
        var priorTransactionId = Guid.NewGuid();
        var currentTransactionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.AddRange(
                new BuildingEntity { Id = "B1", BuildingNumber = "1", Name = "One" },
                new BuildingEntity { Id = "B2", BuildingNumber = "2", Name = "Two" });
            seed.FinancialPeriods.AddRange(
                new FinancialPeriodEntity
                {
                    PeriodId = priorPeriodId, BuildingId = "B1", PeriodKey = "2026-10",
                    TimeZoneId = "Africa/Cairo", StartsAtUtc = now.AddMonths(-1).AddDays(-10),
                    EndsAtUtc = now.AddDays(-10), CreatedAtUtc = now.AddMonths(-1)
                },
                new FinancialPeriodEntity
                {
                    PeriodId = currentPeriodId, BuildingId = "B1", PeriodKey = "2026-11",
                    TimeZoneId = "Africa/Cairo", StartsAtUtc = now.AddDays(-10),
                    EndsAtUtc = now.AddMonths(1), CreatedAtUtc = now
                });
            seed.LedgerTransactions.AddRange(
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = priorTransactionId, PeriodId = priorPeriodId,
                    BuildingId = "B1", Kind = LedgerTransactionKind.Deposit,
                    Status = LedgerTransactionStatus.Posted, IdempotencyKey = "prior",
                    CreatedAtUtc = now.AddDays(-20), PostedAtUtc = now.AddDays(-20)
                },
                new LedgerTransactionEntity
                {
                    LedgerTransactionId = currentTransactionId, PeriodId = currentPeriodId,
                    BuildingId = "B1", Kind = LedgerTransactionKind.Deposit,
                    Status = LedgerTransactionStatus.Posted, IdempotencyKey = "current",
                    CreatedAtUtc = now, PostedAtUtc = now
                });
            seed.LedgerEntries.AddRange(
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = priorTransactionId,
                    PeriodId = priorPeriodId, BuildingId = "B1", ApartmentId = "A1",
                    EntryKind = LedgerEntryKind.Deposit, SignedAmount = 100m, CurrencyCode = "EGP",
                    EffectiveAtUtc = now.AddDays(-20), CreatedAtUtc = now.AddDays(-20)
                },
                new LedgerEntryEntity
                {
                    LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = currentTransactionId,
                    PeriodId = currentPeriodId, BuildingId = "B1", ApartmentId = "A1",
                    EntryKind = LedgerEntryKind.Deposit, SignedAmount = 50m, CurrencyCode = "EGP",
                    EffectiveAtUtc = now, CreatedAtUtc = now
                });
            await seed.SaveChangesAsync();
        }

        var service = new TargetWalletReadService(new TestContextFactory(options));
        var result = await service.GetApartmentAsync("B1", "A1", "2026-11", "2026-11");

        Assert.Equal(150m, result.Balance);
        var statementEntry = Assert.Single(result.Transactions);
        Assert.Equal(50m, statementEntry.Amount);
    }


    [Fact]
    public async Task GetApartmentAsync_RetainsReceiptAndNoteAfterDepositConfirmation()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var periodId = Guid.NewGuid();
        var depositId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        const string receiptJson = """{"Url":"https://example.test/receipt.png","Success":true}""";

        await using (var seed = new AppDbContext(options))
        {
            seed.FinancialPeriods.Add(new FinancialPeriodEntity
            {
                PeriodId = periodId, BuildingId = "B1", PeriodKey = now.ToString("yyyy-MM"),
                TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Open,
                StartsAtUtc = now.AddDays(-10), EndsAtUtc = now.AddDays(20), CreatedAtUtc = now
            });
            seed.LedgerTransactions.Add(new LedgerTransactionEntity
            {
                LedgerTransactionId = transactionId, PeriodId = periodId, BuildingId = "B1",
                Kind = LedgerTransactionKind.Deposit, Status = LedgerTransactionStatus.Posted,
                IdempotencyKey = "confirmed-deposit", CreatedAtUtc = now, PostedAtUtc = now
            });
            seed.PaymentDeposits.Add(new PaymentDepositEntity
            {
                PaymentDepositId = depositId, PeriodId = periodId, BuildingId = "B1",
                ApartmentId = "A1", PostedTransactionId = transactionId, Amount = 80m,
                CurrencyCode = "EGP", Status = PaymentDepositStatus.Confirmed,
                SubmittedAtUtc = now.AddHours(-1), ConfirmedAtUtc = now,
                Note = "Bank transfer", ReceiptMetadataJson = receiptJson, SourceSystem = "application"
            });
            seed.LedgerEntries.Add(new LedgerEntryEntity
            {
                LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = transactionId,
                PeriodId = periodId, BuildingId = "B1", ApartmentId = "A1",
                EntryKind = LedgerEntryKind.Deposit, SignedAmount = 80m, CurrencyCode = "EGP",
                EffectiveAtUtc = now, CreatedAtUtc = now
            });
            await seed.SaveChangesAsync();
        }

        var service = new TargetWalletReadService(new TestContextFactory(options));
        var snapshot = await service.GetApartmentAsync("B1", "A1");

        var entry = Assert.Single(snapshot.Transactions);
        Assert.Equal("Bank transfer", entry.Note);
        Assert.Equal(depositId.ToString("N")[..8].ToUpperInvariant(), entry.Number);
        Assert.Equal("https://example.test/receipt.png", entry.ReceiptUrl);
        Assert.True(entry.HasReceipt);
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


    [Fact]
    public async Task GetSubmissionContextAsync_DoesNotExposeClosedPeriods()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using (var seed = new AppDbContext(options))
        {
            seed.FinancialPeriods.Add(new FinancialPeriodEntity
            {
                PeriodId = Guid.NewGuid(),
                BuildingId = "B1",
                PeriodKey = "2026-10",
                TimeZoneId = "Africa/Cairo",
                Status = FinancialPeriodStatus.Closed,
                StartsAtUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                EndsAtUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAtUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            await seed.SaveChangesAsync();
        }

        var service = new TargetWalletReadService(new TestContextFactory(options));

        Assert.Null(await service.GetSubmissionContextAsync("B1", "2026-10"));
    }


    [Fact]
    public async Task GetPendingForBuildingAsync_ReturnsOnlyPendingDepositsInTheRequestedBuilding()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var now = DateTime.UtcNow;
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.AddRange(
                new BuildingEntity { Id = "B1", BuildingNumber = "1", Name = "One" },
                new BuildingEntity { Id = "B2", BuildingNumber = "2", Name = "Two" });
            seed.FinancialPeriods.AddRange(
                new FinancialPeriodEntity
                {
                    PeriodId = Guid.NewGuid(), BuildingId = "B1", PeriodKey = now.ToString("yyyy-MM"),
                    TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Open,
                    StartsAtUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndsAtUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1),
                    CreatedAtUtc = now
                },
                new FinancialPeriodEntity
                {
                    PeriodId = Guid.NewGuid(), BuildingId = "B2", PeriodKey = now.ToString("yyyy-MM"),
                    TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Open,
                    StartsAtUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                    EndsAtUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1),
                    CreatedAtUtc = now
                });
            var periods = await seed.FinancialPeriods.ToListAsync();
            seed.Apartments.AddRange(
                new ApartmentEntity { BuildingId = "B1", Id = "A1", FloorId = "F1", Number = 1, Label = "Unit 1" },
                new ApartmentEntity { BuildingId = "B2", Id = "A1", FloorId = "F1", Number = 1, Label = "Other unit" });
            seed.PaymentDeposits.AddRange(
                new PaymentDepositEntity
                {
                    PaymentDepositId = Guid.NewGuid(), PeriodId = periods.Single(p => p.BuildingId == "B1").PeriodId,
                    BuildingId = "B1", ApartmentId = "A1", Amount = 120m, CurrencyCode = "EGP",
                    Status = PaymentDepositStatus.Pending, SubmittedAtUtc = now, SourceSystem = "application"
                },
                new PaymentDepositEntity
                {
                    PaymentDepositId = Guid.NewGuid(), PeriodId = periods.Single(p => p.BuildingId == "B2").PeriodId,
                    BuildingId = "B2", ApartmentId = "A1", Amount = 220m, CurrencyCode = "EGP",
                    Status = PaymentDepositStatus.Pending, SubmittedAtUtc = now, SourceSystem = "application"
                },
                new PaymentDepositEntity
                {
                    PaymentDepositId = Guid.NewGuid(), PeriodId = periods.Single(p => p.BuildingId == "B1").PeriodId,
                    BuildingId = "B1", ApartmentId = "A1", Amount = 320m, CurrencyCode = "EGP",
                    Status = PaymentDepositStatus.Cancelled, SubmittedAtUtc = now, SourceSystem = "application"
                });
            await seed.SaveChangesAsync();
        }

        var service = new TargetWalletReadService(new TestContextFactory(options));
        var pending = await service.GetPendingForBuildingAsync("B1");

        var item = Assert.Single(pending);
        Assert.Equal("A1", item.ApartmentId);
        Assert.Equal(1, item.ApartmentNumber);
        Assert.Equal("Unit 1", item.ApartmentLabel);
        Assert.Equal(120m, item.Amount);
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
