using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
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

        var audit = CreateAuditMock();
        var page = await new AccountManagementService(factory, audit.Object).GetPageAsync(null, null, null, 1, 25);

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


    [Fact]
    public async Task ResetResidentPinAsync_RotatesCredentialAndRequiresChangeWithoutLoggingPin()
    {
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var oldCredentialId = Guid.NewGuid();
        var oldHash = hasher.HashPin("2468");
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity { PersonId = personId, DisplayName = "Resident", PrimaryPhone = "+201000000222", CreatedAtUtc = now });
            db.UserAccounts.Add(new UserAccountEntity { AccountId = accountId, PersonId = personId, AccountStatus = 1, SecurityStamp = "before", CreatedAtUtc = now });
            db.RoleDefinitions.Add(new RoleDefinitionEntity { RoleKey = "resident", DisplayName = "Resident", IsActive = true });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "resident",
                ScopeType = 2, ScopeBuildingId = "building-1", ScopeApartmentId = "apt-1",
                ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
            });
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = oldCredentialId, AccountId = accountId, CredentialType = 0,
                CredentialHash = oldHash, HashAlgorithm = "BCrypt.Net-Next", CreatedAtUtc = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var audit = CreateAuditMock();
        var service = new AccountManagementService(factory, audit.Object);
        var result = await service.ResetResidentPinAsync(accountId, "superadmin-actor", hasher);

        Assert.True(result.Success);
        Assert.NotNull(result.TemporaryPin);
        Assert.Equal(6, result.TemporaryPin!.Length);
        Assert.All(result.TemporaryPin!, character => Assert.True(char.IsDigit(character)));

        await using var verify = await factory.CreateDbContextAsync();
        var account = await verify.UserAccounts.SingleAsync(x => x.AccountId == accountId);
        Assert.True(account.MustChangePin);
        Assert.NotEqual("before", account.SecurityStamp);
        Assert.NotNull((await verify.UserCredentials.SingleAsync(x => x.CredentialId == oldCredentialId)).RevokedAtUtc);
        var activeCredential = await verify.UserCredentials.SingleAsync(x => x.AccountId == accountId && x.RevokedAtUtc == null);
        Assert.True(hasher.VerifyPin(result.TemporaryPin!, activeCredential.CredentialHash));
        audit.Verify(x => x.LogAsync("identity.resident.pin.reset", null, null, "superadmin-actor", "superadmin",
            It.IsAny<object?>(), "info"), Times.Once);
    }

    [Fact]
    public async Task SetResidentStatusAsync_ChangesOnlyStandaloneResidentAccountAndRotatesSecurityStamp()
    {
        var factory = new TestContextFactory();
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity { PersonId = personId, DisplayName = "Resident", PrimaryPhone = "+201000000333", CreatedAtUtc = now });
            db.UserAccounts.Add(new UserAccountEntity { AccountId = accountId, PersonId = personId, AccountStatus = 1, SecurityStamp = "before", CreatedAtUtc = now });
            db.RoleDefinitions.Add(new RoleDefinitionEntity { RoleKey = "resident", DisplayName = "Resident", IsActive = true });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "resident",
                ScopeType = 2, ScopeBuildingId = "building-1", ScopeApartmentId = "apt-1",
                ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var audit = CreateAuditMock();
        var service = new AccountManagementService(factory, audit.Object);
        var result = await service.SetResidentStatusAsync(accountId, false, "superadmin-actor");

        Assert.True(result.Success);
        await using var verify = await factory.CreateDbContextAsync();
        var account = await verify.UserAccounts.SingleAsync(x => x.AccountId == accountId);
        Assert.Equal((byte)0, account.AccountStatus);
        Assert.NotEqual("before", account.SecurityStamp);
        audit.Verify(x => x.LogAsync("identity.resident.account.deactivated", null, null, "superadmin-actor", "superadmin",
            It.IsAny<object?>(), "info"), Times.Once);
    }

    private static Mock<IAuditLogger> CreateAuditMock()
    {
        var audit = new Mock<IAuditLogger>();
        audit.Setup(x => x.LogAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return audit;
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
