using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace BuildingManagement.Tests;

[Collection("SuperAdmin configuration")]
public sealed class IdentityAccountAuthenticationServiceTests
{
    [Fact]
    public async Task AuthenticateResidentAsync_UsesTargetIdentityTablesAndReturnsTargetAccountPrincipal()
    {
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId,
                DisplayName = "Synthetic Identity Resident",
                PrimaryEmail = "identity-smoke@example.invalid",
                PrimaryPhone = "+201000000041",
                IsArchived = false,
                CreatedAtUtc = now
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId,
                PersonId = personId,
                AccountStatus = 1,
                NormalizedEmail = "IDENTITY-SMOKE@EXAMPLE.INVALID",
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity { RoleKey = "resident", DisplayName = "Resident", IsActive = true });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "resident",
                ScopeType = 2, ScopeBuildingId = "building-1", ScopeApartmentId = "apt-1",
                ValidFromUtc = now.AddMinutes(-1), GrantedAtUtc = now.AddMinutes(-1)
            });
            db.ApartmentOccupancies.Add(new ApartmentOccupancyEntity
            {
                OccupancyId = Guid.NewGuid(), BuildingId = "building-1", ApartmentId = "apt-1",
                PersonId = personId, OccupancyType = 1, IsPrimary = true,
                ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
            });
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = Guid.NewGuid(), AccountId = accountId, CredentialType = 0,
                CredentialHash = hasher.Hash("Synthetic-Test-Password-42!"), HashAlgorithm = "BCrypt.Net-Next",
                CreatedAtUtc = now.AddMinutes(-1)
            });
            await db.SaveChangesAsync();
        }

        var service = new IdentityAccountAuthenticationService(factory, hasher);
        var result = await service.AuthenticateResidentAsync("+201000000041", "Synthetic-Test-Password-42!");

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.UsesTargetIdentity);
        Assert.Equal(accountId.ToString("D"), result.Uid);
        Assert.Equal("resident", result.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.SecurityStamp));
    }

    [Fact]
    public async Task AuthenticateResidentAsync_DoesNotFallBackWhenTargetAccountIdentifierMatchesButCredentialIsWrong()
    {
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity { PersonId = personId, DisplayName = "Synthetic", PrimaryPhone = "+201000000042", CreatedAtUtc = now });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId, PersonId = personId, AccountStatus = 1,
                NormalizedEmail = "LOCKED-TEST@EXAMPLE.INVALID", SecurityStamp = "stamp", CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "resident", DisplayName = "Resident", IsActive = true
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "resident",
                ScopeType = 2, ScopeBuildingId = "building-1", ScopeApartmentId = "apt-1",
                ValidFromUtc = now.AddMinutes(-1), GrantedAtUtc = now.AddMinutes(-1)
            });
            await db.SaveChangesAsync();
        }

        var result = await new IdentityAccountAuthenticationService(factory, hasher)
            .AuthenticateResidentAsync("+201000000042", "wrong");

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("wrong-credentials", result.Error);
    }


    [Fact]
    public async Task AuthenticateResidentAsync_DoesNotTreatNonPasswordCredentialTypesAsLoginSecrets()
    {
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId,
                DisplayName = "Credential Type Isolation",
                PrimaryPhone = "+201000000099",
                CreatedAtUtc = now
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId,
                PersonId = personId,
                AccountStatus = 1,
                SecurityStamp = "credential-type-stamp",
                CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "resident", DisplayName = "Resident", IsActive = true
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "resident",
                ScopeType = 2, ScopeBuildingId = "building-1", ScopeApartmentId = "apt-1",
                ValidFromUtc = now.AddMinutes(-1), GrantedAtUtc = now.AddMinutes(-1)
            });
            db.ApartmentOccupancies.Add(new ApartmentOccupancyEntity
            {
                OccupancyId = Guid.NewGuid(), BuildingId = "building-1", ApartmentId = "apt-1",
                PersonId = personId, OccupancyType = 1, IsPrimary = true,
                ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
            });
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = Guid.NewGuid(), AccountId = accountId, CredentialType = 1,
                CredentialHash = hasher.Hash("Not-A-Password-Credential-42!"),
                HashAlgorithm = "BCrypt.Net-Next", CreatedAtUtc = now.AddMinutes(-1)
            });
            await db.SaveChangesAsync();
        }

        var result = await new IdentityAccountAuthenticationService(factory, hasher)
            .AuthenticateResidentAsync("+201000000099", "Not-A-Password-Credential-42!");

        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task AuthenticateResidentAsync_UsesAnOlderValidCredentialWhenNewestCredentialIsExpired()
    {
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId,
                DisplayName = "Synthetic Credential Rotation",
                PrimaryPhone = "+201000000001",
                CreatedAtUtc = now
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId,
                PersonId = personId,
                AccountStatus = 1,
                SecurityStamp = "credential-rotation-stamp",
                CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "resident", DisplayName = "Resident", IsActive = true
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(),
                AccountId = accountId,
                RoleKey = "resident",
                ScopeType = 2,
                ScopeBuildingId = "building-1",
                ScopeApartmentId = "apt-1",
                ValidFromUtc = now.AddDays(-1),
                GrantedAtUtc = now.AddDays(-1)
            });
            db.ApartmentOccupancies.Add(new ApartmentOccupancyEntity
            {
                OccupancyId = Guid.NewGuid(), BuildingId = "building-1", ApartmentId = "apt-1",
                PersonId = personId, OccupancyType = 1, IsPrimary = true,
                ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
            });
            db.UserCredentials.AddRange(
                new UserCredentialEntity
                {
                    CredentialId = Guid.NewGuid(),
                    AccountId = accountId,
                    CredentialType = 0,
                    CredentialHash = hasher.Hash("Still-Valid-Password-42!"),
                    HashAlgorithm = "BCrypt.Net-Next",
                    CreatedAtUtc = now.AddDays(-2)
                },
                new UserCredentialEntity
                {
                    CredentialId = Guid.NewGuid(),
                    AccountId = accountId,
                    CredentialType = 0,
                    CredentialHash = hasher.Hash("Expired-Password-42!"),
                    HashAlgorithm = "BCrypt.Net-Next",
                    CreatedAtUtc = now.AddDays(-1),
                    ExpiresAtUtc = now.AddMinutes(-1)
                });
            await db.SaveChangesAsync();
        }

        var result = await new IdentityAccountAuthenticationService(factory, hasher)
            .AuthenticateResidentAsync("+201000000001", "Still-Valid-Password-42!");

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(accountId.ToString("D"), result.Uid);
    }

    [Fact]
    public async Task AuthenticateResidentAsync_RejectsResidentRoleScopedToAnotherApartment()
    {
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId, DisplayName = "Wrong Scope Resident",
                PrimaryPhone = "+201000000098", CreatedAtUtc = now
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId, PersonId = personId, AccountStatus = 1,
                SecurityStamp = "wrong-scope-stamp", CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "resident", DisplayName = "Resident", IsActive = true
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "resident",
                ScopeType = 2, ScopeBuildingId = "building-1", ScopeApartmentId = "apt-other",
                ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
            });
            db.ApartmentOccupancies.Add(new ApartmentOccupancyEntity
            {
                OccupancyId = Guid.NewGuid(), BuildingId = "building-1", ApartmentId = "apt-1",
                PersonId = personId, OccupancyType = 1, IsPrimary = true,
                ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
            });
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = Guid.NewGuid(), AccountId = accountId, CredentialType = 0,
                CredentialHash = hasher.Hash("Scope-Test-Password-42!"),
                HashAlgorithm = "BCrypt.Net-Next", CreatedAtUtc = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var result = await new IdentityAccountAuthenticationService(factory, hasher)
            .AuthenticateResidentAsync("+201000000098", "Scope-Test-Password-42!", "building-1", "apt-1");

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("wrong-credentials", result.Error);
    }

    [Fact]
    public async Task AuthenticateResidentAsync_DoesNotAutoProvisionWithSharedDefaultPin()
    {
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var personId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId,
                DisplayName = "First Login Resident",
                PrimaryPhone = "01000000077",
                IsArchived = false,
                CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "resident",
                DisplayName = "Resident",
                IsActive = true
            });
            db.ApartmentOccupancies.Add(new ApartmentOccupancyEntity
            {
                OccupancyId = Guid.NewGuid(),
                BuildingId = "building-1",
                ApartmentId = "apt-77",
                PersonId = personId,
                OccupancyType = 1,
                IsPrimary = true,
                ValidFromUtc = now.AddDays(-1),
                RecordedAtUtc = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var service = new IdentityAccountAuthenticationService(factory, hasher);
        var initial = await service.AuthenticateResidentAsync("+201000000077", "1234");

        Assert.NotNull(initial);
        Assert.False(initial.Success);
        Assert.Equal("wrong-credentials", initial.Error);

        await using var verify = await factory.CreateDbContextAsync();
        Assert.Empty(await verify.UserAccounts.ToListAsync());
        Assert.Empty(await verify.UserCredentials.ToListAsync());
    }

    [Fact]
    public async Task AuthenticateResidentAsync_DoesNotProvisionWhenPhoneIsAmbiguousAcrossPeople()
    {
        var factory = new TestContextFactory();
        var now = DateTime.UtcNow;
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.AddRange(
                new PersonEntity
                {
                    PersonId = Guid.NewGuid(), DisplayName = "Duplicate Phone A",
                    PrimaryPhone = "+201000000088", CreatedAtUtc = now
                },
                new PersonEntity
                {
                    PersonId = Guid.NewGuid(), DisplayName = "Duplicate Phone B",
                    PrimaryPhone = "01000000088", IsArchived = false, CreatedAtUtc = now
                });
            await db.SaveChangesAsync();
        }

        var result = await new IdentityAccountAuthenticationService(factory, new BcryptPasswordHasher())
            .AuthenticateResidentAsync("+201000000088", "1234");

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("wrong-credentials", result.Error);
        await using var verify = await factory.CreateDbContextAsync();
        Assert.Empty(await verify.UserAccounts.ToListAsync());
        Assert.Empty(await verify.UserCredentials.ToListAsync());
    }

    [Fact]
    public async Task AuthenticateResidentAsync_DoesNotAcceptEmailAsLoginIdentifier()
    {
        var factory = new TestContextFactory();
        var result = await new IdentityAccountAuthenticationService(factory, new BcryptPasswordHasher())
            .AuthenticateResidentAsync("resident@example.invalid", "123456");
        Assert.Null(result);
    }
    [Theory]
    [InlineData("01000000077", "+201000000077")]
    [InlineData("٠١٠٠٠٠٠٠٠٧٧", "+201000000077")]
    [InlineData("+20 10-0000-0077", "+201000000077")]
    [InlineData("00201000000077", "+201000000077")]
    public void NormalizePhone_ConvertsEquivalentEgyptianNumberFormatsToOneCanonicalValue(
        string input, string expected)
    {
        Assert.Equal(expected, AuthHelpers.NormalizePhone(input));
    }


    [Fact]
    public async Task AuthenticateAdminAsync_UsesTargetMobilePinAndSelectedBuildingScope()
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
                PersonId = personId, DisplayName = "Target Admin",
                PrimaryPhone = "+201000000555", PrimaryEmail = "admin@example.invalid", CreatedAtUtc = now
            });
            // A separate Resident-only record may share the contact phone.
            db.Persons.Add(new PersonEntity
            {
                PersonId = Guid.NewGuid(), DisplayName = "Resident Contact Record",
                PrimaryPhone = "01000000555", CreatedAtUtc = now
            });            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId, PersonId = personId, AccountStatus = 1,
                SecurityStamp = "admin-stamp", CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity { RoleKey = "building-admin", DisplayName = "Building Administrator", IsActive = true });
            db.RoleDefinitions.Add(new RoleDefinitionEntity { RoleKey = "resident", DisplayName = "Resident", IsActive = true });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "resident",
                ScopeType = 1, ScopeBuildingId = "building-b", ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
            });            db.RolePermissions.Add(new RolePermissionEntity { RoleKey = "building-admin", PermissionKey = "residents.manage", Effect = 1 });
            db.UserRoleAssignments.AddRange(
                new UserRoleAssignmentEntity { RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "building-admin", ScopeType = 1, ScopeBuildingId = "building-a", ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1) },
                new UserRoleAssignmentEntity { RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "building-admin", ScopeType = 1, ScopeBuildingId = "building-b", ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1) });
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = Guid.NewGuid(), AccountId = accountId, CredentialType = 0,
                CredentialHash = hasher.HashPin("583921"), HashAlgorithm = "BCrypt.Net-Next", CreatedAtUtc = now
            });
            await db.SaveChangesAsync();
        }

        var service = new IdentityAccountAuthenticationService(factory, hasher);
        var result = await service.AuthenticateAdminAsync("01000000555", "583921", "building-b");

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.UsesTargetIdentity);
        Assert.Equal("admin", result.Role);
        Assert.Equal(accountId.ToString("D"), result.Uid);
        Assert.Equal(new[] { "building-b" }, result.BuildingIds);
        Assert.Contains("residents.manage", result.Permissions);

        var wrongPin = await service.AuthenticateAdminAsync("+201000000555", "000000", "building-b");
        Assert.NotNull(wrongPin);
        Assert.False(wrongPin.Success);

        var wrongBuilding = await service.AuthenticateAdminAsync("+201000000555", "583921", "building-c");
        Assert.Null(wrongBuilding);
    }

    [Fact]
    public async Task AuthenticateSuperAdminAsync_AuthenticatesTwoIndependentTargetAccounts()
    {
        SuperAdminIdentityPolicy.Configure("alaa5366@hotmail.com,alaa5366@gmail.com");
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var now = DateTime.UtcNow;
        var hotmailAccountId = Guid.NewGuid();
        var gmailAccountId = Guid.NewGuid();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "superadmin", DisplayName = "Super Administrator",
                IsSystemRole = true, IsActive = true
            });
            db.RolePermissions.Add(new RolePermissionEntity
            {
                RoleKey = "superadmin", PermissionKey = "buildings.manage", Effect = 1
            });

            var accounts = new[]
            {
                (Id: hotmailAccountId, Email: "ALAA5366@HOTMAIL.COM", Password: "Hotmail-Unique-Test-Password-42!"),
                (Id: gmailAccountId, Email: "ALAA5366@GMAIL.COM", Password: "Gmail-Unique-Test-Password-73!")
            };
            foreach (var item in accounts)
            {
                var personId = Guid.NewGuid();
                db.Persons.Add(new PersonEntity
                {
                    PersonId = personId, DisplayName = "Super Admin",
                    PrimaryEmail = item.Email, IsArchived = false, CreatedAtUtc = now
                });
                db.UserAccounts.Add(new UserAccountEntity
                {
                    AccountId = item.Id, PersonId = personId, AccountStatus = 1,
                    NormalizedEmail = item.Email, SecurityStamp = Guid.NewGuid().ToString("N"),
                    CreatedAtUtc = now
                });
                db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
                {
                    RoleAssignmentId = Guid.NewGuid(), AccountId = item.Id,
                    RoleKey = "superadmin", ScopeType = 0, ValidFromUtc = now.AddMinutes(-1),
                    GrantedAtUtc = now.AddMinutes(-1)
                });
                db.UserCredentials.Add(new UserCredentialEntity
                {
                    CredentialId = Guid.NewGuid(), AccountId = item.Id, CredentialType = 0,
                    CredentialHash = hasher.Hash(item.Password), HashAlgorithm = "BCrypt.Net-Next",
                    CreatedAtUtc = now.AddMinutes(-1)
                });
            }
            await db.SaveChangesAsync();
        }

        var service = new IdentityAccountAuthenticationService(factory, hasher);
        var hotmail = await service.AuthenticateSuperAdminAsync(
            "alaa5366@hotmail.com", "Hotmail-Unique-Test-Password-42!");
        var gmail = await service.AuthenticateSuperAdminAsync(
            "alaa5366@gmail.com", "Gmail-Unique-Test-Password-73!");
        var crossed = await service.AuthenticateSuperAdminAsync(
            "alaa5366@gmail.com", "Hotmail-Unique-Test-Password-42!");

        Assert.True(hotmail.Success);
        Assert.True(gmail.Success);
        Assert.True(hotmail.UsesTargetIdentity);
        Assert.True(gmail.UsesTargetIdentity);
        Assert.Equal(hotmailAccountId.ToString("D"), hotmail.Uid);
        Assert.Equal(gmailAccountId.ToString("D"), gmail.Uid);
        Assert.Contains("buildings.manage", hotmail.Permissions);
        Assert.False(crossed.Success);
    }

    [Fact]
    public async Task ResetApartmentResidentPinsAsync_DoesNotChangeAnyIndividualResidentCredential()
    {
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var credentialId = Guid.NewGuid();
        var oldHash = hasher.HashPin("246810");
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId, DisplayName = "Individual Resident",
                PrimaryPhone = "+201000000909", CreatedAtUtc = now
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId, PersonId = personId, AccountStatus = 1,
                SecurityStamp = "before", CreatedAtUtc = now
            });
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = credentialId, AccountId = accountId, CredentialType = 0,
                CredentialHash = oldHash, HashAlgorithm = "BCrypt.Net-Next", CreatedAtUtc = now
            });
            await db.SaveChangesAsync();
        }

        var service = new IdentityAccountAuthenticationService(factory, hasher);
        var affected = await service.ResetApartmentResidentPinsAsync(
            "building-1", "apt-1", "9876", "qr-shared-link");

        Assert.Equal(0, affected);
        await using var verify = await factory.CreateDbContextAsync();
        var credential = await verify.UserCredentials.SingleAsync(x => x.CredentialId == credentialId);
        Assert.Null(credential.RevokedAtUtc);
        Assert.Equal(oldHash, credential.CredentialHash);
        Assert.Equal("before", (await verify.UserAccounts.SingleAsync(x => x.AccountId == accountId)).SecurityStamp);
    }

    [Fact]
    public async Task AuthenticateResidentAsync_ReturnsEveryActiveApartmentScopeAuthorizedForTheAccount()
    {
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var scopes = new[]
        {
            (Building: "building-one", Apartment: "apartment-one"),
            (Building: "building-two", Apartment: "apartment-two")
        };

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId, DisplayName = "Multi Scope Resident",
                PrimaryPhone = "+201000000733", CreatedAtUtc = now
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId, PersonId = personId, AccountStatus = 1,
                SecurityStamp = "multi-scope-stamp", CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "resident", DisplayName = "Resident", IsActive = true
            });
            foreach (var scope in scopes)
            {
                db.ApartmentOccupancies.Add(new ApartmentOccupancyEntity
                {
                    OccupancyId = Guid.NewGuid(), BuildingId = scope.Building,
                    ApartmentId = scope.Apartment, PersonId = personId, OccupancyType = 1,
                    IsPrimary = scope.Building == "building-one",
                    ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
                });
                db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
                {
                    RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "resident",
                    ScopeType = 2, ScopeBuildingId = scope.Building, ScopeApartmentId = scope.Apartment,
                    ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
                });
            }
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = Guid.NewGuid(), AccountId = accountId, CredentialType = 0,
                CredentialHash = hasher.Hash("Multi-Scope-Resident-42!"),
                HashAlgorithm = "BCrypt.Net-Next", CreatedAtUtc = now
            });
            await db.SaveChangesAsync();
        }

        var result = await new IdentityAccountAuthenticationService(factory, hasher)
            .AuthenticateResidentAsync("+201000000733", "Multi-Scope-Resident-42!");

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(2, result.ResidentScopes.Count);
        Assert.Contains(result.ResidentScopes, scope =>
            scope.BuildingId == "building-one" && scope.ApartmentId == "apartment-one");
        Assert.Contains(result.ResidentScopes, scope =>
            scope.BuildingId == "building-two" && scope.ApartmentId == "apartment-two");
    }

    [Fact]
    public async Task IsRoleContextStillAuthorizedAsync_RejectsRevokedOrExpiredBuildingAdminScope()
    {
        var factory = new TestContextFactory();
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId, DisplayName = "Scoped Building Admin",
                PrimaryPhone = "+201000000744", CreatedAtUtc = now
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId, PersonId = personId, AccountStatus = 1,
                SecurityStamp = "scope-validation-stamp", CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "building-admin", DisplayName = "Building Administrator", IsActive = true
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = assignmentId, AccountId = accountId, RoleKey = "building-admin",
                ScopeType = 1, ScopeBuildingId = "authorized-building",
                ValidFromUtc = now.AddMinutes(-1), GrantedAtUtc = now.AddMinutes(-1)
            });
            await db.SaveChangesAsync();
        }

        var service = new IdentityAccountAuthenticationService(factory, new BcryptPasswordHasher());
        var context = new AuthResult
        {
            Success = true, UsesTargetIdentity = true, Uid = accountId.ToString("D"),
            Role = "admin", SecurityStamp = "scope-validation-stamp",
            BuildingIds = new List<string> { "authorized-building" }
        };

        Assert.True(await service.IsRoleContextStillAuthorizedAsync(context));

        await using (var db = await factory.CreateDbContextAsync())
        {
            var assignment = await db.UserRoleAssignments.SingleAsync(item =>
                item.RoleAssignmentId == assignmentId);
            assignment.ValidToUtc = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        Assert.False(await service.IsRoleContextStillAuthorizedAsync(context));
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
