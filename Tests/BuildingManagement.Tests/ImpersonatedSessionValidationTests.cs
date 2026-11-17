using System.Security.Claims;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildingManagement.Tests;

public class ImpersonatedSessionValidationTests
{
    private const string BuildingId = "building-1";
    private const string ApartmentId = "apartment-1";
    private const string ActorUid = "admin-1";

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;

        public AppDbContext CreateDbContext() => new(_options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private sealed class TestUserSqlStore : UserSqlStore
    {
        private readonly UserEntity? _user;

        public TestUserSqlStore(UserEntity? user) : base(null!, NullLogger<UserSqlStore>.Instance) => _user = user;

        public override Task<UserEntity?> GetByUidAsync(string uid, CancellationToken ct = default) =>
            Task.FromResult(uid == ActorUid ? _user : null);
    }

    private static async Task<(SessionRevalidator Validator, AppDbContext Db)> CreateAsync(
        bool apartmentExists = true,
        UserEntity? actor = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var factory = new TestDbContextFactory(options);
        var db = factory.CreateDbContext();
        db.Buildings.Add(new BuildingEntity { Id = BuildingId, Name = "Test", BuildingNumber = "T-1" });
        if (apartmentExists)
            db.Apartments.Add(new ApartmentEntity { BuildingId = BuildingId, Id = ApartmentId, FloorId = "floor-1" });
        await db.SaveChangesAsync();

        actor ??= new UserEntity
        {
            Uid = ActorUid,
            Role = "admin",
            IsActive = true,
            IsDisabled = false,
            BuildingAdmins = new List<BuildingAdminEntity>
            {
                new() { AdminUid = ActorUid, BuildingId = BuildingId }
            }
        };

        var store = new TestUserSqlStore(actor);
        var users = new UsersService(store, NullLogger<UsersService>.Instance);
        var validator = new SessionRevalidator(
            users,
            factory,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<SessionRevalidator>.Instance);
        return (validator, db);
    }

    private static ClaimsPrincipal CreateResidentImpersonation(bool includeActorClaims = true)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "imp-" + ApartmentId),
            new(ClaimTypes.Role, "resident"),
            new("impersonated", "true"),
            new("impersonatedBy", ActorUid),
            new("impersonationTargetBuildingId", BuildingId),
            new("impersonationTargetApartmentId", ApartmentId)
        };
        if (includeActorClaims)
        {
            claims.Add(new Claim("impersonationActorUid", ActorUid));
            claims.Add(new Claim("impersonationActorRole", "admin"));
            claims.Add(new Claim("impersonationActorPermissions", "[]"));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public async Task Active_admin_can_keep_resident_impersonation_in_assigned_building()
    {
        var (validator, db) = await CreateAsync();
        await using (db)
        {
            var result = await validator.ValidateAsync(CreateResidentImpersonation());
            Assert.True(result.Valid);
        }
    }

    [Fact]
    public async Task Disabled_initiating_admin_is_rejected()
    {
        var disabled = new UserEntity
        {
            Uid = ActorUid,
            Role = "admin",
            IsActive = true,
            IsDisabled = true,
            BuildingAdmins = new List<BuildingAdminEntity>
            {
                new() { AdminUid = ActorUid, BuildingId = BuildingId }
            }
        };
        var (validator, db) = await CreateAsync(actor: disabled);
        await using (db)
        {
            var result = await validator.ValidateAsync(CreateResidentImpersonation());
            Assert.False(result.Valid);
        }
    }

    [Fact]
    public async Task Changed_initiating_admin_permissions_revoke_impersonation()
    {
        var changed = new UserEntity
        {
            Uid = ActorUid,
            Role = "admin",
            IsActive = true,
            IsDisabled = false,
            BuildingAdmins = new List<BuildingAdminEntity>
            {
                new() { AdminUid = ActorUid, BuildingId = BuildingId }
            },
            Permissions = new List<UserPermissionEntity>
            {
                new() { Uid = ActorUid, Permission = "finance.manage" }
            }
        };
        var (validator, db) = await CreateAsync(actor: changed);
        await using (db)
        {
            var result = await validator.ValidateAsync(CreateResidentImpersonation());
            Assert.False(result.Valid);
        }
    }

    [Fact]
    public async Task Missing_initiating_identity_claims_are_rejected()
    {
        var (validator, db) = await CreateAsync();
        await using (db)
        {
            var result = await validator.ValidateAsync(CreateResidentImpersonation(includeActorClaims: false));
            Assert.False(result.Valid);
        }
    }

    [Fact]
    public async Task Deleted_target_apartment_is_rejected()
    {
        var (validator, db) = await CreateAsync(apartmentExists: false);
        await using (db)
        {
            var result = await validator.ValidateAsync(CreateResidentImpersonation());
            Assert.False(result.Valid);
        }
    }

    [Fact]
    public async Task Admin_removed_from_target_building_is_rejected()
    {
        var removed = new UserEntity
        {
            Uid = ActorUid,
            Role = "admin",
            IsActive = true,
            IsDisabled = false,
            BuildingAdmins = new List<BuildingAdminEntity>()
        };
        var (validator, db) = await CreateAsync(actor: removed);
        await using (db)
        {
            var result = await validator.ValidateAsync(CreateResidentImpersonation());
            Assert.False(result.Valid);
        }
    }
}
