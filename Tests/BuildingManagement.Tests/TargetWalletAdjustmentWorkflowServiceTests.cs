using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class TargetWalletAdjustmentWorkflowServiceTests
{
    [Fact]
    public async Task AddAsync_PostsOneSignedAdjustmentAndIsIdempotent()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var now = DateTime.UtcNow;
        var periodId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var personId = Guid.NewGuid();
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
            seed.Apartments.Add(new ApartmentEntity
            {
                BuildingId = "B1", Id = "A1", FloorId = "F1", Number = 1, IsClosed = false
            });
            seed.BuildingSettings.Add(new BuildingSettingsEntity { BuildingId = "B1", Currency = "EGP" });
            seed.Persons.Add(new PersonEntity { PersonId = personId, DisplayName = "Admin", CreatedAtUtc = now });
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
            await seed.SaveChangesAsync();
        }

        var service = new TargetWalletAdjustmentWorkflowService(
            new TestContextFactory(options), TimeProvider.System);
        var commandId = Guid.NewGuid().ToString("D");
        var first = await service.AddAsync("B1", "A1", now.ToString("yyyy-MM"), actorId,
            -25m, "Correcting duplicate charge", commandId);
        var repeated = await service.AddAsync("B1", "A1", now.ToString("yyyy-MM"), actorId,
            -25m, "Correcting duplicate charge", commandId);

        Assert.Equal(first, repeated);
        await using var verify = new AppDbContext(options);
        var transaction = await verify.LedgerTransactions.SingleAsync();
        Assert.Equal(LedgerTransactionKind.WalletAdjustment, transaction.Kind);
        Assert.Equal(LedgerTransactionStatus.Posted, transaction.Status);
        var entry = await verify.LedgerEntries.SingleAsync();
        Assert.Equal(-25m, entry.SignedAmount);
        Assert.Equal("EGP", entry.CurrencyCode);
        Assert.Single(await verify.AuditEvents.Where(item => item.EventType == "WalletAdjustmentPosted").ToListAsync());
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
