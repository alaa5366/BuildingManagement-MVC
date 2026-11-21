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
    public async Task GetPageAsync_ReportsCanonicalTargetRolesAndDuplicatePhones()
    {
        var factory = new TestContextFactory();
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var targetSuperAdminPersonId = Guid.NewGuid();
        var targetSuperAdminAccountId = Guid.NewGuid();

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.AddRange(
                new PersonEntity { PersonId = personId, DisplayName = "Target Admin", PrimaryPhone = "+201000000111", CreatedAtUtc = now },
                new PersonEntity { PersonId = Guid.NewGuid(), DisplayName = "Archived matching person", PrimaryPhone = "+201000000111", IsArchived = true, ArchivedAtUtc = now, CreatedAtUtc = now },
                new PersonEntity { PersonId = targetSuperAdminPersonId, DisplayName = "Target SuperAdmin", PrimaryPhone = "+201000000111", CreatedAtUtc = now });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId, PersonId = personId, AccountStatus = 1,
                SecurityStamp = "test-stamp", CreatedAtUtc = now
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = targetSuperAdminAccountId, PersonId = targetSuperAdminPersonId, AccountStatus = 1,
                SecurityStamp = "superadmin-stamp", CreatedAtUtc = now
            });
            db.RoleDefinitions.AddRange(
                new RoleDefinitionEntity { RoleKey = "admin", DisplayName = "Admin", IsActive = true },
                new RoleDefinitionEntity { RoleKey = "superadmin", DisplayName = "SuperAdmin", IsActive = true });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "admin",
                ScopeType = 1, ScopeBuildingId = "building-1",
                ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = targetSuperAdminAccountId, RoleKey = "superadmin",
                ScopeType = 0, ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var audit = CreateAuditMock();
        var page = await new AccountManagementService(factory, audit.Object).GetPageAsync(null, null, null, 1, 25);

        Assert.Equal(2, page.TotalAccounts);
        Assert.Equal(0, page.LegacySuperAdminAccounts);
        Assert.Equal(1, page.DuplicatePhoneAccounts);
        Assert.Equal(0, page.AccountsWithoutActiveRole);

        var target = Assert.Single(page.Rows.Where(x => x.AccountId == accountId));
        Assert.Equal("admin", target.Roles);
        Assert.Contains("building-1", target.RoleContexts);
        Assert.True(target.IsDuplicatePhone);

        var targetSuperAdmin = Assert.Single(page.Rows.Where(x => x.AccountId == targetSuperAdminAccountId));
        Assert.Equal("superadmin", targetSuperAdmin.Roles);
        Assert.Null(targetSuperAdmin.Phone);
        Assert.False(targetSuperAdmin.IsDuplicatePhone);

        Assert.All(page.Rows, row => Assert.False(row.IsLegacyAccount));
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
        var unverified = await service.ResetResidentPinAsync(accountId, "superadmin-actor", hasher);
        Assert.False(unverified.Success);
        var result = await service.ResetResidentPinAsync(accountId, "superadmin-actor", hasher, identityVerified: true);

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


    [Fact]
    public async Task ResetManagedAccountPinAsync_AllowsAdminAndRejectsMultiRoleAccounts()
    {
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var now = DateTime.UtcNow;
        var adminPersonId = Guid.NewGuid();
        var protectedPersonId = Guid.NewGuid();
        var adminAccountId = Guid.NewGuid();
        var protectedAccountId = Guid.NewGuid();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.AddRange(
                new PersonEntity { PersonId = adminPersonId, DisplayName = "Admin", PrimaryPhone = "+201000000444", CreatedAtUtc = now },
                new PersonEntity { PersonId = protectedPersonId, DisplayName = "Protected", PrimaryPhone = "+201000000445", CreatedAtUtc = now });
            db.UserAccounts.AddRange(
                new UserAccountEntity { AccountId = adminAccountId, PersonId = adminPersonId, AccountStatus = 1, SecurityStamp = "admin-before", CreatedAtUtc = now },
                new UserAccountEntity { AccountId = protectedAccountId, PersonId = protectedPersonId, AccountStatus = 1, SecurityStamp = "protected-before", CreatedAtUtc = now });
            db.RoleDefinitions.AddRange(
                new RoleDefinitionEntity { RoleKey = "admin", DisplayName = "Admin", IsActive = true },
                new RoleDefinitionEntity { RoleKey = "resident", DisplayName = "Resident", IsActive = true },
                new RoleDefinitionEntity { RoleKey = "superadmin", DisplayName = "SuperAdmin", IsActive = true });
            db.UserRoleAssignments.AddRange(
                new UserRoleAssignmentEntity { RoleAssignmentId = Guid.NewGuid(), AccountId = adminAccountId, RoleKey = "admin", ScopeType = 1, ScopeBuildingId = "building-a", ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1) },
                new UserRoleAssignmentEntity { RoleAssignmentId = Guid.NewGuid(), AccountId = adminAccountId, RoleKey = "admin", ScopeType = 1, ScopeBuildingId = "building-b", ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1) },
                new UserRoleAssignmentEntity { RoleAssignmentId = Guid.NewGuid(), AccountId = protectedAccountId, RoleKey = "admin", ScopeType = 1, ScopeBuildingId = "building-a", ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1) },
                new UserRoleAssignmentEntity { RoleAssignmentId = Guid.NewGuid(), AccountId = protectedAccountId, RoleKey = "superadmin", ScopeType = 0, ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1) });
            await db.SaveChangesAsync();
        }

        var audit = CreateAuditMock();
        var service = new AccountManagementService(factory, audit.Object);
        var result = await service.ResetManagedAccountPinAsync(adminAccountId, "superadmin-actor", hasher);
        Assert.True(result.Success);
        Assert.NotNull(result.TemporaryPin);
        await using (var verify = await factory.CreateDbContextAsync())
        {
            var admin = await verify.UserAccounts.SingleAsync(x => x.AccountId == adminAccountId);
            Assert.True(admin.MustChangePin);
            Assert.NotEqual("admin-before", admin.SecurityStamp);
            var credential = await verify.UserCredentials.SingleAsync(x => x.AccountId == adminAccountId && x.RevokedAtUtc == null);
            Assert.True(hasher.VerifyPin(result.TemporaryPin!, credential.CredentialHash));
        }

        var rejected = await service.ResetManagedAccountPinAsync(protectedAccountId, "superadmin-actor", hasher);
        Assert.False(rejected.Success);
        await using var final = await factory.CreateDbContextAsync();
        Assert.Empty(await final.UserCredentials.Where(x => x.AccountId == protectedAccountId).ToListAsync());
        Assert.Equal("protected-before", (await final.UserAccounts.SingleAsync(x => x.AccountId == protectedAccountId)).SecurityStamp);
        audit.Verify(x => x.LogAsync("identity.admin.pin.reset", null, null, "superadmin-actor", "superadmin",
            It.IsAny<object?>(), "info"), Times.Once);
    }

    [Fact]
    public async Task ResetManagedAccountPinAsync_AllowsOneIdentityWithResidentAndAdminRoles()
    {
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var oldCredentialId = Guid.NewGuid();

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId, DisplayName = "Resident Admin",
                PrimaryPhone = "+201000000447", CreatedAtUtc = now
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId, PersonId = personId, AccountStatus = 1,
                SecurityStamp = "before", CreatedAtUtc = now
            });
            db.RoleDefinitions.AddRange(
                new RoleDefinitionEntity { RoleKey = "resident", DisplayName = "Resident", IsActive = true },
                new RoleDefinitionEntity { RoleKey = "admin", DisplayName = "Admin", IsActive = true });
            db.UserRoleAssignments.AddRange(
                new UserRoleAssignmentEntity
                {
                    RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "resident",
                    ScopeType = 2, ScopeBuildingId = "building-a", ScopeApartmentId = "apt-12",
                    ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
                },
                new UserRoleAssignmentEntity
                {
                    RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "admin",
                    ScopeType = 1, ScopeBuildingId = "building-b",
                    ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
                });
            db.ApartmentOccupancies.Add(new ApartmentOccupancyEntity
            {
                OccupancyId = Guid.NewGuid(), PersonId = personId, BuildingId = "building-a",
                ApartmentId = "apt-12", OccupancyType = 1, IsPrimary = true,
                ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
            });
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = oldCredentialId, AccountId = accountId, CredentialType = 0,
                CredentialHash = hasher.HashPin("246810"), HashAlgorithm = "BCrypt.Net-Next",
                CreatedAtUtc = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var audit = CreateAuditMock();
        var service = new AccountManagementService(factory, audit.Object);

        var page = await service.GetPageAsync(null, null, null, 1, 25);
        var row = Assert.Single(page.Rows);
        Assert.True(row.CanManagePin);
        Assert.True(row.RequiresIdentityVerificationForPin);
        Assert.False(row.CanChangeStatus);

        var unverified = await service.ResetManagedAccountPinAsync(accountId, "superadmin-actor", hasher);
        Assert.False(unverified.Success);

        var result = await service.ResetManagedAccountPinAsync(
            accountId, "superadmin-actor", hasher, identityVerified: true);
        Assert.True(result.Success);
        Assert.NotNull(result.TemporaryPin);
        Assert.Equal(6, result.TemporaryPin!.Length);

        await using var verify = await factory.CreateDbContextAsync();
        var account = await verify.UserAccounts.SingleAsync(x => x.AccountId == accountId);
        Assert.Equal((byte)1, account.AccountStatus);
        Assert.True(account.MustChangePin);
        Assert.NotEqual("before", account.SecurityStamp);
        Assert.NotNull((await verify.UserCredentials.SingleAsync(x => x.CredentialId == oldCredentialId)).RevokedAtUtc);
        var activeCredential = await verify.UserCredentials.SingleAsync(
            x => x.AccountId == accountId && x.RevokedAtUtc == null);
        Assert.True(hasher.VerifyPin(result.TemporaryPin, activeCredential.CredentialHash));
        Assert.Equal(1, await verify.UserRoleAssignments.CountAsync(x =>
            x.AccountId == accountId && x.RoleKey == "resident" && x.ScopeBuildingId == "building-a"));
        Assert.Equal(1, await verify.UserRoleAssignments.CountAsync(x =>
            x.AccountId == accountId && x.RoleKey == "admin" && x.ScopeBuildingId == "building-b"));

        var authentication = new IdentityAccountAuthenticationService(factory, hasher);
        var adminLogin = await authentication.AuthenticateAdminAsync(
            "+201000000447", result.TemporaryPin, "building-b");
        Assert.NotNull(adminLogin);
        Assert.True(adminLogin.Success);
        Assert.True(adminLogin.MustChangePin);
        Assert.Equal("admin", adminLogin.Role);
        Assert.Equal(new[] { "building-b" }, adminLogin.BuildingIds);

        var residentLogin = await authentication.AuthenticateResidentAsync(
            "+201000000447", result.TemporaryPin, "building-a", "apt-12");
        Assert.NotNull(residentLogin);
        Assert.True(residentLogin.Success);
        Assert.True(residentLogin.MustChangePin);
        Assert.Equal("resident", residentLogin.Role);
        Assert.Equal(new[] { "building-a" }, residentLogin.BuildingIds);

        var unauthorizedBuildingLogin = await authentication.AuthenticateAdminAsync(
            "+201000000447", result.TemporaryPin, "building-c");
        Assert.Null(unauthorizedBuildingLogin);

        // The first-login PIN must be replaceable once, after which the same identity
        // can authenticate through either authorized role with the new shared PIN.
        var pinChangeSucceeded = await authentication.ChangeResidentPinAsync(
            accountId, account.SecurityStamp, "735192");
        Assert.True(pinChangeSucceeded);

        var adminLoginWithNewPin = await authentication.AuthenticateAdminAsync(
            "+201000000447", "735192", "building-b");
        Assert.NotNull(adminLoginWithNewPin);
        Assert.True(adminLoginWithNewPin.Success);
        Assert.False(adminLoginWithNewPin.MustChangePin);
        Assert.Equal("admin", adminLoginWithNewPin.Role);
        Assert.Equal(new[] { "building-b" }, adminLoginWithNewPin.BuildingIds);

        var residentLoginWithNewPin = await authentication.AuthenticateResidentAsync(
            "+201000000447", "735192", "building-a", "apt-12");
        Assert.NotNull(residentLoginWithNewPin);
        Assert.True(residentLoginWithNewPin.Success);
        Assert.False(residentLoginWithNewPin.MustChangePin);
        Assert.Equal("resident", residentLoginWithNewPin.Role);
        Assert.Equal(new[] { "building-a" }, residentLoginWithNewPin.BuildingIds);

        var oldTemporaryPinLogin = await authentication.AuthenticateAdminAsync(
            "+201000000447", result.TemporaryPin, "building-b");
        Assert.NotNull(oldTemporaryPinLogin);
        Assert.False(oldTemporaryPinLogin.Success);

        audit.Verify(x => x.LogAsync("identity.managed.pin.reset", null, null, "superadmin-actor", "superadmin",
            It.IsAny<object?>(), "info"), Times.Once);
    }

    [Fact]
    public async Task SetManagedAccountStatusAsync_AllowsAdminWithMultipleBuildingScopes()
    {
        var factory = new TestContextFactory();
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity { PersonId = personId, DisplayName = "Admin", PrimaryPhone = "+201000000446", CreatedAtUtc = now });
            db.UserAccounts.Add(new UserAccountEntity { AccountId = accountId, PersonId = personId, AccountStatus = 1, SecurityStamp = "before", CreatedAtUtc = now });
            db.RoleDefinitions.Add(new RoleDefinitionEntity { RoleKey = "admin", DisplayName = "Admin", IsActive = true });
            db.UserRoleAssignments.AddRange(
                new UserRoleAssignmentEntity { RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "admin", ScopeType = 1, ScopeBuildingId = "building-a", ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1) },
                new UserRoleAssignmentEntity { RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "admin", ScopeType = 1, ScopeBuildingId = "building-b", ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1) });
            await db.SaveChangesAsync();
        }
        var audit = CreateAuditMock();
        var result = await new AccountManagementService(factory, audit.Object).SetManagedAccountStatusAsync(accountId, false, "superadmin-actor");
        Assert.True(result.Success);
        await using var verify = await factory.CreateDbContextAsync();
        var account = await verify.UserAccounts.SingleAsync(x => x.AccountId == accountId);
        Assert.Equal((byte)0, account.AccountStatus);
        Assert.NotEqual("before", account.SecurityStamp);
    }

    [Fact]
    public async Task ResetManagedAccountPinAsync_ProvisionsPendingResidentWithIndividualCredentialAndActivatesIt()
    {
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId, DisplayName = "Pending Resident",
                PrimaryPhone = "+201000000777", CreatedAtUtc = now
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId, PersonId = personId, AccountStatus = 0,
                MustChangePin = true, SecurityStamp = "pending-before", CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "resident", DisplayName = "Resident", IsActive = true
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "resident",
                ScopeType = 2, ScopeBuildingId = "building-1", ScopeApartmentId = "apt-7",
                ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var audit = CreateAuditMock();
        var service = new AccountManagementService(factory, audit.Object);
        var prematureActivation = await service.SetManagedAccountStatusAsync(
            accountId, true, "superadmin-actor", identityVerified: true);
        Assert.False(prematureActivation.Success);
        var result = await service.ResetManagedAccountPinAsync(accountId, "superadmin-actor", hasher, identityVerified: true);

        Assert.True(result.Success);
        Assert.NotNull(result.TemporaryPin);
        Assert.Equal(6, result.TemporaryPin!.Length);
        await using var verify = await factory.CreateDbContextAsync();
        var account = await verify.UserAccounts.SingleAsync(x => x.AccountId == accountId);
        Assert.Equal((byte)1, account.AccountStatus);
        Assert.True(account.MustChangePin);
        Assert.NotEqual("pending-before", account.SecurityStamp);
        var credential = await verify.UserCredentials.SingleAsync(x => x.AccountId == accountId && x.RevokedAtUtc == null);
        Assert.True(hasher.VerifyPin(result.TemporaryPin, credential.CredentialHash));
        Assert.Equal(1, await verify.UserRoleAssignments.CountAsync(x =>
            x.AccountId == accountId && x.RoleKey == "resident" &&
            x.ScopeBuildingId == "building-1" && x.ScopeApartmentId == "apt-7"));
        audit.Verify(x => x.LogAsync("identity.resident.pin.reset", null, null, "superadmin-actor", "superadmin",
            It.IsAny<object?>(), "info"), Times.Once);
    }

    [Fact]
    public async Task SetManagedAccountStatusAsync_RequiresIdentityAttestationEvenWhenResidentCredentialExists()
    {
        var factory = new TestContextFactory();
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var hasher = new BcryptPasswordHasher();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity { PersonId = personId, DisplayName = "Resident", PrimaryPhone = "+201000000998", CreatedAtUtc = now });
            db.UserAccounts.Add(new UserAccountEntity { AccountId = accountId, PersonId = personId, AccountStatus = 0, MustChangePin = true, SecurityStamp = "pending", CreatedAtUtc = now });
            db.RoleDefinitions.Add(new RoleDefinitionEntity { RoleKey = "resident", DisplayName = "Resident", IsActive = true });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity { RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "resident", ScopeType = 2, ScopeBuildingId = "building-1", ScopeApartmentId = "apt-9", ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1) });
            db.UserCredentials.Add(new UserCredentialEntity { CredentialId = Guid.NewGuid(), AccountId = accountId, CredentialType = 0, CredentialHash = hasher.HashPin("654321"), HashAlgorithm = "BCrypt.Net-Next", CreatedAtUtc = now });
            await db.SaveChangesAsync();
        }
        var service = new AccountManagementService(factory, CreateAuditMock().Object);
        var unverified = await service.SetManagedAccountStatusAsync(accountId, true, "superadmin-actor");
        Assert.False(unverified.Success);
        await using (var pending = await factory.CreateDbContextAsync())
            Assert.Equal((byte)0, (await pending.UserAccounts.SingleAsync(x => x.AccountId == accountId)).AccountStatus);
        var verified = await service.SetManagedAccountStatusAsync(accountId, true, "superadmin-actor", identityVerified: true);
        Assert.True(verified.Success);
        await using var final = await factory.CreateDbContextAsync();
        Assert.Equal((byte)1, (await final.UserAccounts.SingleAsync(x => x.AccountId == accountId)).AccountStatus);
    }

    [Fact]
    public async Task ResetManagedAccountPinAsync_RejectsDuplicateResidentPhoneAndKeepsAccountPending()
    {
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var now = DateTime.UtcNow;
        var residentPersonId = Guid.NewGuid();
        var otherPersonId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.AddRange(
                new PersonEntity
                {
                    PersonId = residentPersonId, DisplayName = "Resident A",
                    PrimaryPhone = "+201000000888", CreatedAtUtc = now
                },
                new PersonEntity
                {
                    PersonId = otherPersonId, DisplayName = "Resident B",
                    PrimaryPhone = "01000000888", CreatedAtUtc = now
                });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId, PersonId = residentPersonId, AccountStatus = 0,
                MustChangePin = true, SecurityStamp = "pending", CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "resident", DisplayName = "Resident", IsActive = true
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "resident",
                ScopeType = 2, ScopeBuildingId = "building-1", ScopeApartmentId = "apt-8",
                ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var result = await new AccountManagementService(factory, CreateAuditMock().Object)
            .ResetManagedAccountPinAsync(accountId, "superadmin-actor", hasher, identityVerified: true);

        Assert.False(result.Success);
        Assert.Null(result.TemporaryPin);
        await using var verify = await factory.CreateDbContextAsync();
        var account = await verify.UserAccounts.SingleAsync(x => x.AccountId == accountId);
        Assert.Equal((byte)0, account.AccountStatus);
        Assert.Equal("pending", account.SecurityStamp);
        Assert.Empty(await verify.UserCredentials.Where(x => x.AccountId == accountId).ToListAsync());
    }

    [Fact]
    public async Task ResetManagedAccountPinAsync_IgnoresSuperAdminPhoneDuplicates()
    {
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var now = DateTime.UtcNow;
        var residentPersonId = Guid.NewGuid();
        var superAdminGmailPersonId = Guid.NewGuid();
        var superAdminHotmailPersonId = Guid.NewGuid();
        var residentAccountId = Guid.NewGuid();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.RoleDefinitions.AddRange(
                new RoleDefinitionEntity { RoleKey = "resident", DisplayName = "Resident", IsActive = true },
                new RoleDefinitionEntity { RoleKey = "superadmin", DisplayName = "Super Administrator", IsSystemRole = true, IsActive = true });
            db.Persons.AddRange(
                new PersonEntity { PersonId = residentPersonId, DisplayName = "Resident", PrimaryPhone = "+201119221000", CreatedAtUtc = now },
                new PersonEntity { PersonId = superAdminGmailPersonId, DisplayName = "Super Admin Gmail", PrimaryPhone = "+201119221000", CreatedAtUtc = now },
                new PersonEntity { PersonId = superAdminHotmailPersonId, DisplayName = "Super Admin Hotmail", PrimaryPhone = "01119221000", CreatedAtUtc = now });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = residentAccountId, PersonId = residentPersonId, AccountStatus = 0,
                MustChangePin = true, SecurityStamp = "pending", CreatedAtUtc = now
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = residentAccountId, RoleKey = "resident",
                ScopeType = 2, ScopeBuildingId = "building-1", ScopeApartmentId = "apt-1",
                ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
            });
            foreach (var personId in new[] { superAdminGmailPersonId, superAdminHotmailPersonId })
            {
                var adminAccountId = Guid.NewGuid();
                db.UserAccounts.Add(new UserAccountEntity
                {
                    AccountId = adminAccountId, PersonId = personId, AccountStatus = 1,
                    SecurityStamp = Guid.NewGuid().ToString("N"), CreatedAtUtc = now
                });
                db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
                {
                    RoleAssignmentId = Guid.NewGuid(), AccountId = adminAccountId, RoleKey = "superadmin",
                    ScopeType = 0, ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
                });
            }
            await db.SaveChangesAsync();
        }

        var result = await new AccountManagementService(factory, CreateAuditMock().Object)
            .ResetManagedAccountPinAsync(residentAccountId, "superadmin-actor", hasher, identityVerified: true);

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.TemporaryPin));
        await using var verify = await factory.CreateDbContextAsync();
        var account = await verify.UserAccounts.SingleAsync(x => x.AccountId == residentAccountId);
        Assert.Equal((byte)1, account.AccountStatus);
        Assert.True(account.MustChangePin);
        Assert.Single(await verify.UserCredentials.Where(x => x.AccountId == residentAccountId && x.RevokedAtUtc == null).ToListAsync());
    }
    [Fact]
    public async Task IdentityGovernance_CreateOccupancy_CreatesPendingAccountAndApartmentScopedResidentRole()
    {
        var factory = new TestContextFactory();
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId, DisplayName = "New Resident", PrimaryPhone = "+201000000881",
                PrimaryEmail = "resident@example.test", CreatedAtUtc = now
            });
            db.Buildings.Add(new BuildingEntity { Id = "building-1", BuildingNumber = "B1", Name = "Building One" });
            db.Apartments.Add(new ApartmentEntity
            {
                BuildingId = "building-1", Id = "apt-1", FloorId = "floor-1", Number = 1
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "resident", DisplayName = "Resident", IsActive = true
            });
            await db.SaveChangesAsync();
        }

        var audit = CreateAuditMock();
        var service = new IdentityGovernanceService(factory, audit.Object);
        var result = await service.CreateOccupancyAsync(
            personId, "building-1", "apt-1", 1, true, now.AddMinutes(-1), "Initial occupancy",
            Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.Success);
        await using var verify = await factory.CreateDbContextAsync();
        var account = await verify.UserAccounts.SingleAsync(x => x.PersonId == personId);
        Assert.Equal((byte)0, account.AccountStatus);
        Assert.True(account.MustChangePin);
        Assert.Equal("RESIDENT@EXAMPLE.TEST", account.NormalizedEmail);
        Assert.Empty(await verify.UserCredentials.Where(x => x.AccountId == account.AccountId).ToListAsync());
        var residentRole = await verify.UserRoleAssignments.SingleAsync(x => x.AccountId == account.AccountId);
        Assert.Equal("resident", residentRole.RoleKey);
        Assert.Equal((byte)2, residentRole.ScopeType);
        Assert.Equal("building-1", residentRole.ScopeBuildingId);
        Assert.Equal("apt-1", residentRole.ScopeApartmentId);
    }

    [Fact]
    public async Task IdentityGovernance_CreateOccupancy_AddsResidentRoleWithoutChangingExistingAdminAccount()
    {
        var factory = new TestContextFactory();
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId, DisplayName = "Resident Admin", PrimaryPhone = "+201000000882", CreatedAtUtc = now
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId, PersonId = personId, AccountStatus = 1,
                SecurityStamp = "existing-stamp", CreatedAtUtc = now
            });
            db.Buildings.Add(new BuildingEntity { Id = "building-1", BuildingNumber = "B1", Name = "Building One" });
            db.Apartments.Add(new ApartmentEntity
            {
                BuildingId = "building-1", Id = "apt-2", FloorId = "floor-1", Number = 2
            });
            db.RoleDefinitions.AddRange(
                new RoleDefinitionEntity { RoleKey = "resident", DisplayName = "Resident", IsActive = true },
                new RoleDefinitionEntity { RoleKey = "admin", DisplayName = "Admin", IsActive = true });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "admin",
                ScopeType = 1, ScopeBuildingId = "building-1", ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var result = await new IdentityGovernanceService(factory, CreateAuditMock().Object).CreateOccupancyAsync(
            personId, "building-1", "apt-2", 1, true, now.AddMinutes(-1), null,
            Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.Success);
        await using var verify = await factory.CreateDbContextAsync();
        var account = await verify.UserAccounts.SingleAsync(x => x.AccountId == accountId);
        Assert.Equal((byte)1, account.AccountStatus);
        Assert.Equal("existing-stamp", account.SecurityStamp);
        var assignments = await verify.UserRoleAssignments.Where(x => x.AccountId == accountId).ToListAsync();
        Assert.Contains(assignments, x => x.RoleKey == "admin" && x.ScopeType == 1);
        Assert.Contains(assignments, x => x.RoleKey == "resident" && x.ScopeType == 2 &&
            x.ScopeBuildingId == "building-1" && x.ScopeApartmentId == "apt-2");
    }

    [Fact]
    public async Task IdentityGovernance_CreatePendingAccount_ProvisionsResidentRolesForEveryActiveApartment()
    {
        var factory = new TestContextFactory();
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId, DisplayName = "Multi Apartment Resident", PrimaryPhone = "+201000000883", CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "resident", DisplayName = "Resident", IsActive = true
            });
            db.ApartmentOccupancies.AddRange(
                new ApartmentOccupancyEntity
                {
                    OccupancyId = Guid.NewGuid(), PersonId = personId, BuildingId = "building-1", ApartmentId = "apt-1",
                    OccupancyType = 1, IsPrimary = true, ValidFromUtc = now.AddDays(-2), RecordedAtUtc = now.AddDays(-2)
                },
                new ApartmentOccupancyEntity
                {
                    OccupancyId = Guid.NewGuid(), PersonId = personId, BuildingId = "building-2", ApartmentId = "apt-9",
                    OccupancyType = 1, IsPrimary = false, ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
                });
            await db.SaveChangesAsync();
        }

        var result = await new IdentityGovernanceService(factory, CreateAuditMock().Object)
            .CreatePendingAccountAsync(personId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.Success);
        await using var verify = await factory.CreateDbContextAsync();
        var account = await verify.UserAccounts.SingleAsync(x => x.PersonId == personId);
        Assert.Equal((byte)0, account.AccountStatus);
        Assert.True(account.MustChangePin);
        Assert.Empty(await verify.UserCredentials.Where(x => x.AccountId == account.AccountId).ToListAsync());
        var roles = await verify.UserRoleAssignments.Where(x => x.AccountId == account.AccountId).ToListAsync();
        Assert.Equal(2, roles.Count);
        Assert.Contains(roles, x => x.ScopeBuildingId == "building-1" && x.ScopeApartmentId == "apt-1");
        Assert.Contains(roles, x => x.ScopeBuildingId == "building-2" && x.ScopeApartmentId == "apt-9");
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
