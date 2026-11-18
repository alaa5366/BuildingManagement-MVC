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
            await db.SaveChangesAsync();
        }

        var result = await new IdentityAccountAuthenticationService(factory, hasher)
            .AuthenticateResidentAsync("locked-test@example.invalid", "wrong");

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("wrong-credentials", result.Error);
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
