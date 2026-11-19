using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class TargetDepositWorkflowServiceTests
{
    [Fact]
    public async Task CreateAndConfirm_IsIdempotentAndPostsExactlyOneLedgerEntry()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var factory = new TestContextFactory(options);
        var now = DateTime.UtcNow;
        var periodId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        await using (var seed = new AppDbContext(options))
        {
            seed.FinancialPeriods.Add(new FinancialPeriodEntity
            {
                PeriodId = periodId,
                BuildingId = "B1",
                PeriodKey = now.ToString("yyyy-MM"),
                TimeZoneId = "Africa/Cairo",
                Status = FinancialPeriodStatus.Open,
                StartsAtUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                EndsAtUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1),
                CreatedAtUtc = now
            });
            seed.Apartments.Add(new ApartmentEntity
            {
                BuildingId = "B1", Id = "A1", FloorId = "F1", Number = 1
            });
            var personId = Guid.NewGuid();
            seed.Persons.Add(new PersonEntity
            {
                PersonId = personId, DisplayName = "Test actor", CreatedAtUtc = now
            });
            seed.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = actorId, PersonId = personId, AccountStatus = 1,
                SecurityStamp = "test", CreatedAtUtc = now
            });
            seed.RoleDefinitions.AddRange(
                new RoleDefinitionEntity { RoleKey = "resident", DisplayName = "Resident", IsActive = true },
                new RoleDefinitionEntity { RoleKey = "building-admin", DisplayName = "Building Admin", IsActive = true });
            seed.UserRoleAssignments.AddRange(
                new UserRoleAssignmentEntity
                {
                    RoleAssignmentId = Guid.NewGuid(), AccountId = actorId, RoleKey = "resident",
                    ScopeType = 1, ScopeBuildingId = "B1", ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now
                },
                new UserRoleAssignmentEntity
                {
                    RoleAssignmentId = Guid.NewGuid(), AccountId = actorId, RoleKey = "building-admin",
                    ScopeType = 1, ScopeBuildingId = "B1", ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now
                });
            await seed.SaveChangesAsync();
        }

        var service = new TargetDepositWorkflowService(factory, TimeProvider.System);
        const string receiptMetadata = """{"Url":"https://example.test/receipt.png","Success":true}""";
        var depositId = await service.CreatePendingAsync(
            "B1", "A1", periodId, actorId, 125m, sourceKey: "receipt-1",
            note: "Bank transfer", receiptMetadataJson: receiptMetadata);
        var repeatedId = await service.CreatePendingAsync(
            "B1", "A1", periodId, actorId, 125m, sourceKey: "receipt-1",
            note: "Bank transfer", receiptMetadataJson: receiptMetadata);
        Assert.Equal(depositId, repeatedId);

        var postedId = await service.ConfirmAsync(depositId, actorId, "test-admin");
        var repeatedPostId = await service.ConfirmAsync(depositId, actorId, "test-admin");

        Assert.NotNull(postedId);
        Assert.Equal(postedId, repeatedPostId);
        await using var verify = new AppDbContext(options);
        var deposit = await verify.PaymentDeposits.SingleAsync();
        Assert.Equal(PaymentDepositStatus.Confirmed, deposit.Status);
        Assert.Equal("Bank transfer", deposit.Note);
        Assert.Equal(receiptMetadata, deposit.ReceiptMetadataJson);
        Assert.Equal(postedId, deposit.PostedTransactionId);
        var ledger = await verify.LedgerTransactions.SingleAsync();
        Assert.Equal(LedgerTransactionStatus.Posted, ledger.Status);
        Assert.Single(await verify.LedgerEntries.ToListAsync());
        Assert.Equal(125m, (await verify.LedgerEntries.SingleAsync()).SignedAmount);
        Assert.Contains(await verify.AuditEvents.ToListAsync(), item => item.EventType == "PaymentDepositSubmitted");
        Assert.Contains(await verify.AuditEvents.ToListAsync(), item => item.EventType == "PaymentDepositConfirmed");

        var reversalId = await service.ReverseConfirmedAsync(depositId, actorId, "duplicate payment");
        var repeatedReversalId = await service.ReverseConfirmedAsync(depositId, actorId, "duplicate payment");
        Assert.NotNull(reversalId);
        Assert.Equal(reversalId, repeatedReversalId);
        await using var afterReversal = new AppDbContext(options);
        Assert.Equal(PaymentDepositStatus.Reversed, (await afterReversal.PaymentDeposits.SingleAsync()).Status);
        Assert.Equal(2, await afterReversal.LedgerTransactions.CountAsync());
        Assert.Equal(2, await afterReversal.LedgerEntries.CountAsync());
        Assert.Equal(0m, await afterReversal.LedgerEntries.SumAsync(entry => entry.SignedAmount));
        Assert.Contains(await afterReversal.AuditEvents.ToListAsync(), item => item.EventType == "PaymentDepositReversed");
    }

    [Fact]
    public async Task CancelPending_RequiresReasonAndDoesNotCreateLedgerPosting()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var now = DateTime.UtcNow;
        var periodId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
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
            var personId = Guid.NewGuid();
            seed.Persons.Add(new PersonEntity { PersonId = personId, DisplayName = "Actor", CreatedAtUtc = now });
            seed.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = actorId, PersonId = personId, AccountStatus = 1,
                SecurityStamp = "test", CreatedAtUtc = now
            });
            seed.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "building-admin", DisplayName = "Building Admin", IsActive = true
            });
            seed.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = actorId, RoleKey = "building-admin",
                ScopeType = 1, ScopeBuildingId = "B1", ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now
            });
            seed.PaymentDeposits.Add(new PaymentDepositEntity
            {
                PaymentDepositId = Guid.NewGuid(), PeriodId = periodId, BuildingId = "B1",
                ApartmentId = "A1", Amount = 50, CurrencyCode = "EGP",
                Status = PaymentDepositStatus.Pending, SubmittedAtUtc = now, SourceSystem = "application"
            });
            await seed.SaveChangesAsync();
        }

        var service = new TargetDepositWorkflowService(new TestContextFactory(options), TimeProvider.System);
        var depositId = (await new AppDbContext(options).PaymentDeposits.SingleAsync()).PaymentDepositId;
        await Assert.ThrowsAsync<ArgumentException>(() => service.CancelAsync(depositId, actorId, " "));
        Assert.True(await service.CancelAsync(depositId, actorId, "duplicate receipt"));

        await using var verify = new AppDbContext(options);
        var deposit = await verify.PaymentDeposits.SingleAsync();
        Assert.Equal(PaymentDepositStatus.Cancelled, deposit.Status);
        Assert.Equal("duplicate receipt", deposit.ResolutionReason);
        Assert.Empty(await verify.LedgerTransactions.ToListAsync());
        Assert.Empty(await verify.LedgerEntries.ToListAsync());
        Assert.Contains(await verify.AuditEvents.ToListAsync(), item => item.EventType == "PaymentDepositCancelled");
    }



    [Fact]
    public async Task UpdatePending_PreservesSourceAndWritesAppendOnlyAuditEvidence()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var now = DateTime.UtcNow;
        var periodId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var depositId = Guid.NewGuid();
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
            var personId = Guid.NewGuid();
            seed.Persons.Add(new PersonEntity { PersonId = personId, DisplayName = "Actor", CreatedAtUtc = now });
            seed.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = actorId, PersonId = personId, AccountStatus = 1,
                SecurityStamp = "test", CreatedAtUtc = now
            });
            seed.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "building-admin", DisplayName = "Building Admin", IsActive = true
            });
            seed.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = actorId, RoleKey = "building-admin",
                ScopeType = 1, ScopeBuildingId = "B1", ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now
            });
            seed.PaymentDeposits.Add(new PaymentDepositEntity
            {
                PaymentDepositId = depositId, PeriodId = periodId, BuildingId = "B1",
                ApartmentId = "A1", Amount = 50m, CurrencyCode = "EGP",
                Status = PaymentDepositStatus.Pending, SubmittedAtUtc = now,
                Note = "Original note", ReceiptMetadataJson = """{"Url":"https://example.test/r.png","Success":true}""",
                SourceSystem = "application", SourceKey = "resident-submit-1"
            });
            await seed.SaveChangesAsync();
        }

        var service = new TargetDepositWorkflowService(new TestContextFactory(options), TimeProvider.System);
        Assert.True(await service.UpdatePendingAsync(depositId, actorId, 75m, "Corrected note", "bank slip verified"));

        await using var verify = new AppDbContext(options);
        var deposit = await verify.PaymentDeposits.SingleAsync();
        Assert.Equal(75m, deposit.Amount);
        Assert.Equal("Corrected note", deposit.Note);
        Assert.Equal("resident-submit-1", deposit.SourceKey);
        Assert.Contains("example.test/r.png", deposit.ReceiptMetadataJson);
        var audit = await verify.AuditEvents.SingleAsync(item => item.EventType == "PaymentDepositUpdated");
        Assert.Contains("bank slip verified", audit.SafeDetailsJson);
        Assert.Contains("Original note", audit.SafeDetailsJson);
    }

    [Fact]
    public async Task CreatePending_RejectsInvalidOrOversizedReceiptMetadata()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var service = new TargetDepositWorkflowService(new TestContextFactory(options), TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePendingAsync(
            "B1", "A1", Guid.NewGuid(), Guid.NewGuid(), 10m, receiptMetadataJson: "{invalid"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePendingAsync(
            "B1", "A1", Guid.NewGuid(), Guid.NewGuid(), 10m, receiptMetadataJson: new string('x', 8001)));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePendingAsync(
            "B1", "A1", Guid.NewGuid(), Guid.NewGuid(), 10m, note: new string('n', 1001)));
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
