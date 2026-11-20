using System.Security.Claims;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class SessionRevalidatorRoleRoutingTests
{
    [Fact]
    public async Task ActiveBuildingAdminDatabaseRole_PreservesCanonicalAdminSession()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        await using (var db = new AppDbContext(options))
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId,
                DisplayName = "Building Admin",
                IsArchived = false,
                CreatedAtUtc = DateTime.UtcNow
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId,
                PersonId = personId,
                AccountStatus = 1,
                SecurityStamp = "building-admin-stamp",
                CreatedAtUtc = DateTime.UtcNow
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "building-admin",
                DisplayName = "Building Administrator",
                IsActive = true
            });
            db.RolePermissions.Add(new RolePermissionEntity
            {
                RoleKey = "building-admin",
                PermissionKey = "residents.manage",
                Effect = 1
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(),
                AccountId = accountId,
                RoleKey = "building-admin",
                ScopeType = 1,
                ScopeBuildingId = "building-1",
                ValidFromUtc = DateTime.UtcNow.AddMinutes(-1),
                GrantedAtUtc = DateTime.UtcNow.AddMinutes(-1)
            });
            await db.SaveChangesAsync();
        }

        var factory = new InMemoryFactory(options);
        var validator = new SessionRevalidator(
            factory,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<SessionRevalidator>.Instance);
        var principal = Principal(
            new Claim("targetIdentityAccount", "true"),
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.NameIdentifier, accountId.ToString("D")),
            new Claim("securityStamp", "building-admin-stamp"),
            new Claim("buildingId", "building-1"));

        var result = await validator.ValidateAsync(principal);

        Assert.True(result.Valid);
        Assert.Contains("residents.manage", result.Permissions!);
    }

    private sealed class InMemoryFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public InMemoryFactory(DbContextOptions<AppDbContext> options) => _options = options;
        public AppDbContext CreateDbContext() => new(_options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(_options));
    }

    private sealed class UnavailableFactory : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => throw new InvalidOperationException("Legacy lookup must not be reached.");
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<AppDbContext>(new InvalidOperationException("Legacy lookup must not be reached."));
    }

    private static SessionRevalidator CreateValidator() =>
        new(new UnavailableFactory(), new MemoryCache(new MemoryCacheOptions()),
            NullLogger<SessionRevalidator>.Instance);

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Cookies"));

    [Fact]
    public async Task AllowListedEmailWithResidentRole_DoesNotBypassTargetIdentityRequirement()
    {
        SuperAdminIdentityPolicy.Configure("allow-listed@example.test");
        var principal = Principal(
            new Claim(ClaimTypes.Role, "resident"),
            new Claim(ClaimTypes.Email, "allow-listed@example.test"),
            new Claim(ClaimTypes.NameIdentifier, "legacy-resident"));
        Assert.False((await CreateValidator().ValidateAsync(principal)).Valid);
    }

    [Fact]
    public async Task LegacySuperAdminRoleWithoutTargetMarker_IsRejected()
    {
        var principal = Principal(
            new Claim(ClaimTypes.Role, "superadmin"),
            new Claim(ClaimTypes.Email, "root@example.test"),
            new Claim(ClaimTypes.NameIdentifier, "legacy-superadmin"));
        Assert.False((await CreateValidator().ValidateAsync(principal)).Valid);
    }

    [Fact]
    public async Task OriginalSuperAdminClaimAlone_DoesNotPreserveLegacySession()
    {
        var principal = Principal(
            new Claim("originalRole", "superadmin"),
            new Claim(ClaimTypes.Role, "resident"),
            new Claim(ClaimTypes.NameIdentifier, "legacy-resident"));
        Assert.False((await CreateValidator().ValidateAsync(principal)).Valid);
    }

    [Fact]
    public void SuperAdminSecurityStamp_ChangesWhenCredentialOrAccountStateChanges()
    {
        var updatedAt = new DateTime(2026, 11, 19, 8, 0, 0, DateTimeKind.Utc);
        var original = SuperAdminIdentityPolicy.CreateSecurityStamp("bcrypt-hash-a", updatedAt, true, false);
        var changedPassword = SuperAdminIdentityPolicy.CreateSecurityStamp("bcrypt-hash-b", updatedAt, true, false);
        var changedStatus = SuperAdminIdentityPolicy.CreateSecurityStamp("bcrypt-hash-a", updatedAt, false, true);

        Assert.NotEqual(original, changedPassword);
        Assert.NotEqual(original, changedStatus);
        Assert.DoesNotContain("bcrypt-hash-a", original, StringComparison.Ordinal);
    }
}
