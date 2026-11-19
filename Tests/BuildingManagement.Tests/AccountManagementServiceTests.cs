using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class AccountManagementServiceTests
{
    [Fact]
    public async Task GetPageAsync_ReportsTargetRolesDuplicatePhonesAndSeparateLegacySuperAdmins()
    {
        var factory = new TestContextFactory();
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.AddRange(
                new PersonEntity { PersonId = personId, DisplayName = "Target Admin", PrimaryPhone = "+201000000111", CreatedAtUtc = now },
                new PersonEntity { PersonId = Guid.NewGuid(), DisplayName = "Archived matching person", PrimaryPhone = "+201000000111", IsArchived = true, ArchivedAtUtc = now, CreatedAtUtc = now });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId, PersonId = personId, AccountStatus = 1,
                SecurityStamp = "test-stamp", CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity { RoleKey = "admin", DisplayName = "Admin", IsActive = true });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "admin",
                ScopeType = 1, ScopeBuildingId = "building-1",
                ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
            });
            db.Users.Add(new UserEntity
            {
                Uid = "legacy-superadmin-1", Name = "Legacy SuperAdmin",
                Email = "root@example.invalid", Phone = "+201000000111",
                Role = "superadmin", IsActive = true, IsDisabled = false,
                Pin = "must-not-be-read-or-returned"
            });
            await db.SaveChangesAsync();
        }

        var page = await new AccountManagementService(factory).GetPageAsync(null, null, null, 1, 25);

        Assert.Equal(2, page.TotalAccounts);
        Assert.Equal(1, page.LegacySuperAdminAccounts);
        Assert.Equal(1, page.DuplicatePhoneAccounts);
        Assert.Equal(0, page.AccountsWithoutActiveRole);

        var target = Assert.Single(page.Rows.Where(x => x.AccountId == accountId));
        Assert.Equal("admin", target.Roles);
        Assert.Contains("building-1", target.RoleContexts);
        Assert.True(target.IsDuplicatePhone);

        var legacy = Assert.Single(page.Rows.Where(x => x.IsLegacyAccount));
        Assert.Equal("superadmin", legacy.Roles);
        Assert.Null(legacy.Phone);
        Assert.Equal("root@example.invalid", legacy.Email);
        Assert.Equal("legacy-superadmin-1", legacy.LegacyUserId);
    }

    private sealed class TestContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        public AppDbContext CreateDbContext() => new(_options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(_options));
    }
}
