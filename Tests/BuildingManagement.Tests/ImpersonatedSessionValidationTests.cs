using System.Security.Claims;
using System.Text.Json;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildingManagement.Tests;

[Collection("SuperAdmin configuration")]
public sealed class ImpersonatedSessionValidationTests
{
    private const string BuildingId = "building-1";
    private const string ApartmentId = "apartment-1";

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public TestDbContextFactory() => _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        public AppDbContext CreateDbContext() => new(_options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }

    private static SessionRevalidator CreateValidator(IDbContextFactory<AppDbContext> factory) =>
        new(factory, new MemoryCache(new MemoryCacheOptions()),
            NullLogger<SessionRevalidator>.Instance);

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test"));

    private static async Task<(Guid AccountId, Guid PersonId, string SecurityStamp)> AddAdminAsync(
        AppDbContext db, string buildingId = BuildingId, bool twoFactorEnabled = false)
    {
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var stamp = Guid.NewGuid().ToString("N");
        db.Persons.Add(new PersonEntity
        {
            PersonId = personId, DisplayName = "Target Admin",
            PrimaryPhone = "+201000000111", CreatedAtUtc = now
        });
        db.UserAccounts.Add(new UserAccountEntity
        {
            AccountId = accountId, PersonId = personId, AccountStatus = 1,
            SecurityStamp = stamp, CreatedAtUtc = now
        });
        db.RoleDefinitions.Add(new RoleDefinitionEntity
        {
            RoleKey = "admin", DisplayName = "Administrator", IsActive = true
        });
        db.RolePermissions.Add(new RolePermissionEntity
        {
            RoleKey = "admin", PermissionKey = "residents.manage", Effect = 1
        });
        db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
        {
            RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "admin",
            ScopeType = 1, ScopeBuildingId = buildingId,
            ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
        });
        if (twoFactorEnabled)
        {
            db.UserTwoFactorSettings.Add(new UserTwoFactorSettingEntity
            {
                AccountId = accountId, IsEnabled = true,
                SecretCiphertext = new byte[] { 1, 2, 3 }, KeyVersion = "aes-gcm-v1",
                EnrolledAtUtc = now.AddDays(-1)
            });
        }
        await db.SaveChangesAsync();
        return (accountId, personId, stamp);
    }

    private static ClaimsPrincipal AdminPrincipal(Guid accountId, string stamp, bool twoFactorVerified = false) =>
        Principal(
            new Claim(ClaimTypes.NameIdentifier, accountId.ToString("D")),
            new Claim(ClaimTypes.Role, "admin"),
            new Claim("targetIdentityAccount", "true"),
            new Claim("securityStamp", stamp),
            new Claim("buildingId", BuildingId),
            new Claim("twoFactorVerified", twoFactorVerified ? "true" : "false"));

    [Fact]
    public async Task Target_admin_session_uses_current_role_scope_stamp_and_2FA_policy()
    {
        var factory = new TestDbContextFactory();
        await using var db = factory.CreateDbContext();
        var identity = await AddAdminAsync(db, twoFactorEnabled: true);
        var validator = CreateValidator(factory);

        Assert.False((await validator.ValidateAsync(AdminPrincipal(identity.AccountId, identity.SecurityStamp))).Valid);
        var verified = await validator.ValidateAsync(AdminPrincipal(identity.AccountId, identity.SecurityStamp, true));
        Assert.True(verified.Valid);
        Assert.Contains("residents.manage", verified.Permissions!);

        var assignment = await db.UserRoleAssignments.SingleAsync(x => x.AccountId == identity.AccountId);
        assignment.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Assert.False((await validator.ValidateAsync(AdminPrincipal(identity.AccountId, identity.SecurityStamp, true))).Valid);
    }

    [Fact]
    public async Task Super_admin_session_requires_allow_list_target_assignment_and_current_stamp()
    {
        SuperAdminIdentityPolicy.Configure("root@example.test");
        var factory = new TestDbContextFactory();
        await using var db = factory.CreateDbContext();
        var now = DateTime.UtcNow;
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var stamp = Guid.NewGuid().ToString("N");
        db.Persons.Add(new PersonEntity { PersonId = personId, DisplayName = "Root", CreatedAtUtc = now });
        db.UserAccounts.Add(new UserAccountEntity
        {
            AccountId = accountId, PersonId = personId, AccountStatus = 1,
            NormalizedEmail = "ROOT@EXAMPLE.TEST", SecurityStamp = stamp, CreatedAtUtc = now
        });
        db.RoleDefinitions.Add(new RoleDefinitionEntity
        {
            RoleKey = "superadmin", DisplayName = "Super Administrator", IsSystemRole = true, IsActive = true
        });
        db.RolePermissions.Add(new RolePermissionEntity
        {
            RoleKey = "superadmin", PermissionKey = "buildings.manage", Effect = 1
        });
        db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
        {
            RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "superadmin",
            ScopeType = 0, ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var principal = Principal(
            new Claim(ClaimTypes.NameIdentifier, accountId.ToString("D")),
            new Claim(ClaimTypes.Role, "superadmin"),
            new Claim(ClaimTypes.Email, "root@example.test"),
            new Claim("targetIdentityAccount", "true"),
            new Claim("securityStamp", stamp));
        var validator = CreateValidator(factory);
        Assert.True((await validator.ValidateAsync(principal)).Valid);

        var account = await db.UserAccounts.SingleAsync(x => x.AccountId == accountId);
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync();
        Assert.False((await validator.ValidateAsync(principal)).Valid);
    }

    [Fact]
    public async Task Admin_impersonation_is_validated_against_target_identity_and_target_scope()
    {
        var factory = new TestDbContextFactory();
        await using var db = factory.CreateDbContext();
        db.Buildings.Add(new BuildingEntity { Id = BuildingId, Name = "Test", BuildingNumber = "T-1" });
        db.Floors.Add(new FloorEntity { BuildingId = BuildingId, Id = "floor-1", Label = "Floor 1" });
        db.Apartments.Add(new ApartmentEntity { BuildingId = BuildingId, Id = ApartmentId, FloorId = "floor-1" });
        var identity = await AddAdminAsync(db);
        var validator = CreateValidator(factory);
        var permissions = JsonSerializer.Serialize(new[] { "residents.manage" });
        var principal = Principal(
            new Claim(ClaimTypes.NameIdentifier, "impersonated-resident"),
            new Claim(ClaimTypes.Role, "resident"),
            new Claim("impersonated", "true"),
            new Claim("impersonatedBy", identity.AccountId.ToString("D")),
            new Claim("impersonationActorUid", identity.AccountId.ToString("D")),
            new Claim("impersonationActorRole", "admin"),
            new Claim("impersonationActorPermissions", permissions),
            new Claim("impersonationActorSecurityStamp", identity.SecurityStamp),
            new Claim("impersonationActorTwoFactorVerified", "true"),
            new Claim("impersonationTargetBuildingId", BuildingId),
            new Claim("impersonationTargetApartmentId", ApartmentId));

        Assert.True((await validator.ValidateAsync(principal)).Valid);

        var assignment = await db.UserRoleAssignments.SingleAsync(x => x.AccountId == identity.AccountId);
        assignment.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        validator.InvalidateAdmin(identity.AccountId.ToString("D"));
        Assert.False((await validator.ValidateAsync(principal)).Valid);
    }

    [Fact]
    public async Task Legacy_cookie_without_target_identity_marker_is_rejected_without_legacy_lookup()
    {
        var factory = new TestDbContextFactory();
        var validator = CreateValidator(factory);
        var legacyPrincipal = Principal(
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Role, "admin"),
            new Claim("buildingId", BuildingId));
        Assert.False((await validator.ValidateAsync(legacyPrincipal)).Valid);
    }
}
