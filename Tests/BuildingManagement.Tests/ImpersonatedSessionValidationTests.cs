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

    private sealed class UnavailableDbContextFactory : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => throw new InvalidOperationException("Database unavailable");

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<AppDbContext>(new InvalidOperationException("Database unavailable"));
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
        db.Floors.Add(new FloorEntity { BuildingId = BuildingId, Id = "floor-1", Label = "Floor 1" });
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

        db.Users.Add(new UserEntity
        {
            Uid = actor.Uid,
            Email = actor.Email,
            Role = actor.Role,
            IsActive = actor.IsActive,
            IsDisabled = actor.IsDisabled,
            TwoFactorEnabled = actor.TwoFactorEnabled
        });
        await db.SaveChangesAsync();

        var store = new TestUserSqlStore(actor);
        var users = new UsersService(store, NullLogger<UsersService>.Instance);
        var validator = new SessionRevalidator(
            users,
            factory,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<SessionRevalidator>.Instance);
        return (validator, db);
    }

    private static ClaimsPrincipal CreateResidentImpersonation(
        bool includeActorClaims = true,
        string permissionsJson = "[]",
        string? actorTwoFactorVerified = null)
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
            claims.Add(new Claim("impersonationActorPermissions", permissionsJson));
            if (actorTwoFactorVerified != null)
                claims.Add(new Claim("impersonationActorTwoFactorVerified", actorTwoFactorVerified));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public async Task Super_admin_impersonation_requires_allow_list_and_active_database_account()
    {
        SqlAuthService.ConfigureSuperAdmins("root@example.test");
        var (validator, db) = await CreateAsync();
        await using (db)
        {
            db.Users.Add(new UserEntity
            {
                Uid = "super-1",
                Email = "root@example.test",
                Role = "superadmin",
                IsActive = true,
                IsDisabled = false
            });
            await db.SaveChangesAsync();

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "super-1"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim(ClaimTypes.Email, "root@example.test"),
                new Claim("impersonated", "true"),
                new Claim("impersonatedBy", "super-1"),
                new Claim("impersonationActorUid", "super-1"),
                new Claim("impersonationActorRole", "superadmin"),
                new Claim("impersonationActorEmail", "root@example.test"),
                new Claim("impersonationActorSecurityStamp", SqlAuthService.CreateSuperAdminSecurityStamp("", null, true, false)),
                new Claim("impersonationTargetBuildingId", BuildingId)
            }, "test"));

            Assert.True((await validator.ValidateAsync(principal)).Valid);

            var super = await db.Users.SingleAsync(u => u.Uid == "super-1");
            super.IsDisabled = true;
            await db.SaveChangesAsync();

            // Use a fresh cache to ensure this assertion tests the database state, not the 30-second cache.
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("superadmin-disabled-" + Guid.NewGuid().ToString("N"))
                .Options;
            var factory = new TestDbContextFactory(options);
            await using (var seed = factory.CreateDbContext())
            {
                seed.Buildings.Add(new BuildingEntity { Id = BuildingId, Name = "Test", BuildingNumber = "T-1" });
                seed.Users.Add(new UserEntity
                {
                    Uid = "super-1",
                    Email = "root@example.test",
                    Role = "superadmin",
                    IsActive = true,
                    IsDisabled = true
                });
                await seed.SaveChangesAsync();
            }
            var disabledValidator = new SessionRevalidator(
                new UsersService(new TestUserSqlStore(null), NullLogger<UsersService>.Instance),
                factory,
                new MemoryCache(new MemoryCacheOptions()),
                NullLogger<SessionRevalidator>.Instance);
            Assert.False((await disabledValidator.ValidateAsync(principal)).Valid);
        }
    }


    [Fact]
    public async Task Super_admin_session_requires_2FA_proof_after_enrollment()
    {
        SqlAuthService.ConfigureSuperAdmins("root@example.test");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("superadmin-2fa-" + Guid.NewGuid().ToString("N"))
            .Options;
        var factory = new TestDbContextFactory(options);
        await using (var seed = factory.CreateDbContext())
        {
            seed.Users.Add(new UserEntity
            {
                Uid = "super-2fa",
                Email = "root@example.test",
                Role = "superadmin",
                IsActive = true,
                IsDisabled = false,
                TwoFactorEnabled = true
            });
            await seed.SaveChangesAsync();
        }

        var validator = new SessionRevalidator(
            new UsersService(new TestUserSqlStore(null), NullLogger<UsersService>.Instance),
            factory,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<SessionRevalidator>.Instance);

        ClaimsPrincipal Principal(string verified) => new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "super-2fa"),
            new Claim(ClaimTypes.Email, "root@example.test"),
            new Claim(ClaimTypes.Role, "superadmin"),
            new Claim("legacySuperAdminSecurityStamp", SqlAuthService.CreateSuperAdminSecurityStamp("", null, true, false)),
            new Claim("twoFactorVerified", verified)
        }, "test"));

        Assert.False((await validator.ValidateAsync(Principal("false"))).Valid);
        Assert.True((await validator.ValidateAsync(Principal("true"))).Valid);
    }


    [Fact]
    public async Task Super_admin_password_rotation_revokes_existing_session()
    {
        SqlAuthService.ConfigureSuperAdmins("root@example.test");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("superadmin-password-rotation-" + Guid.NewGuid().ToString("N"))
            .Options;
        var factory = new TestDbContextFactory(options);
        var createdAt = new DateTime(2026, 11, 19, 7, 0, 0, DateTimeKind.Utc);
        const string oldCredentialHash = "bcrypt-hash-before";
        await using (var seed = factory.CreateDbContext())
        {
            seed.Users.Add(new UserEntity
            {
                Uid = "super-rotation", Email = "root@example.test", Role = "superadmin",
                IsActive = true, IsDisabled = false, Pin = oldCredentialHash, CreatedAt = createdAt
            });
            await seed.SaveChangesAsync();
        }

        var validator = new SessionRevalidator(
            new UsersService(new TestUserSqlStore(null), NullLogger<UsersService>.Instance),
            factory,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<SessionRevalidator>.Instance);
        var originalStamp = SqlAuthService.CreateSuperAdminSecurityStamp(oldCredentialHash, createdAt, true, false);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "super-rotation"),
            new Claim(ClaimTypes.Email, "root@example.test"),
            new Claim(ClaimTypes.Role, "superadmin"),
            new Claim("legacySuperAdminSecurityStamp", originalStamp)
        }, "test"));

        Assert.True((await validator.ValidateAsync(principal)).Valid);

        await using (var update = factory.CreateDbContext())
        {
            var account = await update.Users.SingleAsync(x => x.Uid == "super-rotation");
            account.Pin = "bcrypt-hash-after";
            account.UpdatedAt = createdAt.AddSeconds(1);
            await update.SaveChangesAsync();
        }

        Assert.False((await validator.ValidateAsync(principal)).Valid);
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
    public async Task Database_outage_fails_closed_for_impersonated_session()
    {
        var actor = new UserEntity
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
        var users = new UsersService(
            new TestUserSqlStore(actor),
            NullLogger<UsersService>.Instance);
        var validator = new SessionRevalidator(
            users,
            new UnavailableDbContextFactory(),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<SessionRevalidator>.Instance);

        var result = await validator.ValidateAsync(CreateResidentImpersonation());
        Assert.False(result.Valid);
    }


    [Fact]
    public async Task Admin_impersonation_is_rejected_when_actor_has_2FA_but_claim_is_missing()
    {
        var enabled = new UserEntity
        {
            Uid = ActorUid,
            Role = "admin",
            IsActive = true,
            IsDisabled = false,
            TwoFactorEnabled = true,
            BuildingAdmins = new List<BuildingAdminEntity>
            {
                new() { AdminUid = ActorUid, BuildingId = BuildingId }
            }
        };
        var (validator, db) = await CreateAsync(actor: enabled);
        await using (db)
        {
            Assert.False((await validator.ValidateAsync(CreateResidentImpersonation())).Valid);
        }
    }

    [Fact]
    public async Task Admin_impersonation_is_valid_when_actor_2FA_claim_is_verified()
    {
        var enabled = new UserEntity
        {
            Uid = ActorUid,
            Role = "admin",
            IsActive = true,
            IsDisabled = false,
            TwoFactorEnabled = true,
            BuildingAdmins = new List<BuildingAdminEntity>
            {
                new() { AdminUid = ActorUid, BuildingId = BuildingId }
            }
        };
        var (validator, db) = await CreateAsync(actor: enabled);
        await using (db)
        {
            Assert.True((await validator.ValidateAsync(
                CreateResidentImpersonation(actorTwoFactorVerified: "true"))).Valid);
        }
    }

    [Fact]
    public async Task Existing_admin_cookie_without_2FA_proof_is_rejected_after_enrollment()
    {
        var enabled = new UserEntity
        {
            Uid = ActorUid,
            Role = "admin",
            IsActive = true,
            IsDisabled = false,
            TwoFactorEnabled = true,
            BuildingAdmins = new List<BuildingAdminEntity>
            {
                new() { AdminUid = ActorUid, BuildingId = BuildingId }
            }
        };
        var (validator, db) = await CreateAsync(actor: enabled);
        await using (db)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ActorUid),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim(ClaimTypes.Email, "admin@example.test"),
                new Claim("buildingId", BuildingId),
                new Claim("twoFactorVerified", "false")
            }, "test"));
            Assert.False((await validator.ValidateAsync(principal)).Valid);
        }
    }

    [Fact]
    public async Task Admin_cookie_with_verified_2FA_is_valid_after_enrollment()
    {
        var enabled = new UserEntity
        {
            Uid = ActorUid,
            Role = "admin",
            IsActive = true,
            IsDisabled = false,
            TwoFactorEnabled = true,
            BuildingAdmins = new List<BuildingAdminEntity>
            {
                new() { AdminUid = ActorUid, BuildingId = BuildingId }
            }
        };
        var (validator, db) = await CreateAsync(actor: enabled);
        await using (db)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ActorUid),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim(ClaimTypes.Email, "admin@example.test"),
                new Claim("buildingId", BuildingId),
                new Claim("twoFactorVerified", "true")
            }, "test"));
            Assert.True((await validator.ValidateAsync(principal)).Valid);
        }
    }

    [Fact]
    public async Task Malformed_initiating_permission_snapshot_is_rejected()
    {
        var (validator, db) = await CreateAsync();
        await using (db)
        {
            var result = await validator.ValidateAsync(
                CreateResidentImpersonation(permissionsJson: "not-json"));
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
