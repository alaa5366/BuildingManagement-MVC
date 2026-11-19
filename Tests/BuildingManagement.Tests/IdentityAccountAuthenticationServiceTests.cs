using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

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
                ValidFromUtc = now.AddMinutes(-1), GrantedAtUtc = now.AddMinutes(-1)
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
        var result = await service.AuthenticateResidentAsync("identity-smoke@example.invalid", "Synthetic-Test-Password-42!");

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
            db.Persons.Add(new PersonEntity { PersonId = personId, DisplayName = "Synthetic", CreatedAtUtc = now });
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
                ValidFromUtc = now.AddMinutes(-1), GrantedAtUtc = now.AddMinutes(-1)
            });
            await db.SaveChangesAsync();
        }

        var result = await new IdentityAccountAuthenticationService(factory, hasher)
            .AuthenticateResidentAsync("locked-test@example.invalid", "wrong");

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
                ValidFromUtc = now.AddDays(-1),
                GrantedAtUtc = now.AddDays(-1)
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

    private sealed class TestContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        public AppDbContext CreateDbContext() => new(_options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(_options));
    }
}
