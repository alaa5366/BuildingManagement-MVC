using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class TargetExpenseRevenueWorkflowServiceTests
{
    [Fact]
    public async Task AddExpense_PersistsSourceAllocationLedgerAndAuditAtomically()
    {
        var (factory, periodId, actorId) = await SeedAsync();
        var service = new TargetExpenseRevenueWorkflowService(factory, TimeProvider.System);

        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"));
        var commandId = Guid.NewGuid();
        var transactionId = await service.AddExpenseAsync(
            "B1", localNow.ToString("yyyy-MM"), "C-EXP", "Repair",
            10.01m, localNow.ToString("yyyy-MM-dd"),
            commandId, actorId);

        await using var verify = factory.CreateDbContext();
        var transaction = await verify.LedgerTransactions.SingleAsync();
        Assert.Equal(transactionId, transaction.LedgerTransactionId);
        Assert.Equal(LedgerTransactionKind.Expense, transaction.Kind);
        Assert.Equal(LedgerTransactionStatus.Posted, transaction.Status);
        Assert.Equal(3, await verify.LedgerEntries.CountAsync());
        Assert.Equal(-10.01m, await verify.LedgerEntries.SumAsync(x => x.SignedAmount));
        Assert.Equal(3, await verify.FinancialAllocationLines.CountAsync());
        Assert.Equal(10.01m, await verify.FinancialAllocationLines.SumAsync(x => x.AllocatedAmount));
        Assert.Single(await verify.FinancialSourceLinks.ToListAsync());
        Assert.Contains(await verify.AuditEvents.ToListAsync(), x => x.EventType == "ExpensePosted");
        var reversalId = await service.ReverseAsync(false, "B1", commandId.ToString("N"),
            "Test reversal", Guid.NewGuid(), actorId);
        await using var reversed = factory.CreateDbContext();
        Assert.NotEqual(transactionId, reversalId);
        Assert.Equal(2, await reversed.LedgerTransactions.CountAsync());
        Assert.Equal(0m, await reversed.LedgerEntries.SumAsync(x => x.SignedAmount));
        Assert.Contains(await reversed.AuditEvents.ToListAsync(), x => x.EventType == "ExpenseReversed");
    }

    [Fact]
    public async Task ReverseAsync_RejectsPostingFromAnotherFinancialPeriodWithoutCreatingReversal()
    {
        var (factory, _, actorId) = await SeedAsync();
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
        var currentMonthStart = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var previousMonthStart = currentMonthStart.AddMonths(-1);
        var previousPeriodId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        var originalTransactionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using (var seed = factory.CreateDbContext())
        {
            seed.FinancialPeriods.Add(new FinancialPeriodEntity
            {
                PeriodId = previousPeriodId,
                BuildingId = "B1",
                PeriodKey = localNow.AddMonths(-1).ToString("yyyy-MM"),
                TimeZoneId = "Africa/Cairo",
                Status = FinancialPeriodStatus.Open,
                StartsAtUtc = TimeZoneInfo.ConvertTimeToUtc(previousMonthStart, zone),
                EndsAtUtc = TimeZoneInfo.ConvertTimeToUtc(currentMonthStart, zone),
                CreatedAtUtc = now
            });
            seed.LedgerTransactions.Add(new LedgerTransactionEntity
            {
                LedgerTransactionId = originalTransactionId,
                PeriodId = previousPeriodId,
                BuildingId = "B1",
                Kind = LedgerTransactionKind.Expense,
                Status = LedgerTransactionStatus.Posted,
                IdempotencyKey = $"finance:expense:{commandId:D}",
                SourceSystem = "application",
                SourceType = "Expense",
                SourceKey = commandId.ToString("D"),
                ActorAccountId = actorId,
                CreatedAtUtc = now,
                PostedAtUtc = now
            });
            seed.LedgerEntries.Add(new LedgerEntryEntity
            {
                LedgerEntryId = Guid.NewGuid(),
                LedgerTransactionId = originalTransactionId,
                PeriodId = previousPeriodId,
                BuildingId = "B1",
                ApartmentId = "A1",
                EntryKind = LedgerEntryKind.ExpenseAllocation,
                SignedAmount = -10m,
                CurrencyCode = "EGP",
                EffectiveAtUtc = TimeZoneInfo.ConvertTimeToUtc(previousMonthStart.AddDays(2), zone),
                CreatedAtUtc = now
            });
            await seed.SaveChangesAsync();
        }

        var service = new TargetExpenseRevenueWorkflowService(factory, TimeProvider.System);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReverseAsync(false, "B1", commandId.ToString("D"),
                "Late reversal", Guid.NewGuid(), actorId));

        Assert.Equal(
            "A finance posting can only be reversed within its original financial period.",
            error.Message);
        await using var verify = factory.CreateDbContext();
        Assert.Single(await verify.LedgerTransactions.ToListAsync());
        Assert.Single(await verify.LedgerEntries.ToListAsync());
        Assert.DoesNotContain(await verify.AuditEvents.ToListAsync(),
            item => item.EventType == "ExpenseReversed");
    }

    [Fact]
    public async Task AddRevenue_UsesPositiveWalletMovementsAndIsIdempotent()
    {
        var (factory, _, actorId) = await SeedAsync();
        var service = new TargetExpenseRevenueWorkflowService(factory, TimeProvider.System);
        var commandId = Guid.NewGuid();
        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"));
        var month = localNow.ToString("yyyy-MM");
        var date = localNow.ToString("yyyy-MM-dd");

        var first = await service.AddRevenueAsync("B1", month, "C-REV", "Refund",
            7.50m, date, commandId, actorId);
        var repeated = await service.AddRevenueAsync("B1", month, "C-REV", "Refund",
            7.50m, date, commandId, actorId);

        Assert.Equal(first, repeated);
        await using var verify = factory.CreateDbContext();
        Assert.Single(await verify.LedgerTransactions.ToListAsync());
        Assert.Equal(7.50m, await verify.LedgerEntries.SumAsync(x => x.SignedAmount));
    }

    private static async Task<(TestContextFactory Factory, Guid PeriodId, Guid ActorId)> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var factory = new TestContextFactory(options);
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
        var now = DateTime.UtcNow;
        var localMonthStart = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var monthStart = TimeZoneInfo.ConvertTimeToUtc(localMonthStart, zone);
        var nextMonthStart = TimeZoneInfo.ConvertTimeToUtc(localMonthStart.AddMonths(1), zone);
        var periodId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        await using var db = factory.CreateDbContext();
        db.FinancialPeriods.Add(new FinancialPeriodEntity
        {
            PeriodId = periodId, BuildingId = "B1", PeriodKey = now.ToString("yyyy-MM"),
            TimeZoneId = "Africa/Cairo", Status = FinancialPeriodStatus.Open,
            StartsAtUtc = monthStart, EndsAtUtc = nextMonthStart, CreatedAtUtc = now
        });
        db.Apartments.AddRange(
            new ApartmentEntity { BuildingId = "B1", Id = "A1", FloorId = "F1", Number = 1 },
            new ApartmentEntity { BuildingId = "B1", Id = "A2", FloorId = "F1", Number = 2 },
            new ApartmentEntity { BuildingId = "B1", Id = "A3", FloorId = "F1", Number = 3 });
        db.FinancialCategories.AddRange(
            new FinancialCategoryEntity { BuildingId = "B1", Id = "C-EXP", Kind = "expense", Name = "Expense", IsActive = true },
            new FinancialCategoryEntity { BuildingId = "B1", Id = "C-REV", Kind = "revenue", Name = "Revenue", IsActive = true });
        db.Persons.Add(new PersonEntity { PersonId = personId, DisplayName = "Admin", CreatedAtUtc = now });
        db.UserAccounts.Add(new UserAccountEntity
        {
            AccountId = actorId, PersonId = personId, AccountStatus = 1,
            SecurityStamp = "test", CreatedAtUtc = now
        });
        db.RoleDefinitions.Add(new RoleDefinitionEntity
        {
            RoleKey = "building-admin", DisplayName = "Building Admin", IsActive = true
        });
        db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
        {
            RoleAssignmentId = Guid.NewGuid(), AccountId = actorId, RoleKey = "building-admin",
            ScopeType = 1, ScopeBuildingId = "B1", ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now
        });
        await db.SaveChangesAsync();
        return (factory, periodId, actorId);
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
