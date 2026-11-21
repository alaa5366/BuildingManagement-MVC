using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class AdminManagementServiceTests
{
    [Fact]
    public async Task CreateAdminAsync_ReusesResidentAccountAndPreservesResidentAccessForSharedPhone()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var factory = new TestFactory(options);
        var hasher = new BcryptPasswordHasher();
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using (var db = new AppDbContext(options))
        {
            db.Buildings.Add(new BuildingEntity
            {
                Id = "building-1", Name = "Test Building", BuildingNumber = "1"
            });
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId, DisplayName = "Resident and Admin",
                PrimaryPhone = "01000000123", Whatsapp = "01000000123",
                CreatedAtUtc = now
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId, PersonId = personId, AccountStatus = 0,
                MustChangePin = true, SecurityStamp = "pending-resident", CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "resident", DisplayName = "Resident", IsSystemRole = true, IsActive = true
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "resident",
                ScopeType = 2, ScopeBuildingId = "building-1", ScopeApartmentId = "apt-12",
                ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
            });
            db.ApartmentOccupancies.AddRange(
                new ApartmentOccupancyEntity
                {
                    OccupancyId = Guid.NewGuid(), BuildingId = "building-1", ApartmentId = "apt-12",
                    PersonId = personId, OccupancyType = 1, IsPrimary = true,
                    ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
                },
                new ApartmentOccupancyEntity
                {
                    OccupancyId = Guid.NewGuid(), BuildingId = "building-1", ApartmentId = "apt-13",
                    PersonId = personId, OccupancyType = 2, IsPrimary = false,
                    ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
                });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "resident",
                ScopeType = 2, ScopeBuildingId = "building-1", ScopeApartmentId = "apt-13",
                ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
            });

            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "superadmin", DisplayName = "Super Admin", IsSystemRole = true, IsActive = true
            });
            var superAdminPeople = new[]
            {
                new PersonEntity
                {
                    PersonId = Guid.NewGuid(), DisplayName = "Super Admin One",
                    PrimaryPhone = "01000000123", CreatedAtUtc = now
                },
                new PersonEntity
                {
                    PersonId = Guid.NewGuid(), DisplayName = "Super Admin Two",
                    PrimaryPhone = "+201000000123", CreatedAtUtc = now
                }
            };
            db.Persons.AddRange(superAdminPeople);
            var superAdminAccounts = superAdminPeople.Select((person, index) => new UserAccountEntity
            {
                AccountId = Guid.NewGuid(), PersonId = person.PersonId, AccountStatus = 1,
                MustChangePin = false, SecurityStamp = $"superadmin-{index}", CreatedAtUtc = now,
                NormalizedEmail = $"SUPERADMIN{index}@EXAMPLE.TEST"
            }).ToList();
            db.UserAccounts.AddRange(superAdminAccounts);
            db.UserRoleAssignments.AddRange(superAdminAccounts.Select(account => new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = account.AccountId, RoleKey = "superadmin",
                ScopeType = 0, ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
            }));
            await db.SaveChangesAsync();
        }

        var service = new AdminManagementService(factory, hasher);
        var returnedAccountId = await service.CreateAdminAsync(
            "Resident and Admin", "01000000123", "building-1", "583921",
            new List<string> { AdminPermissions.ManageResidents }, "superadmin-test");

        Assert.Equal(accountId.ToString("D"), returnedAccountId);

        await using (var verify = new AppDbContext(options))
        {
            Assert.Equal(3, await verify.Persons.CountAsync());
            Assert.Equal(3, await verify.UserAccounts.CountAsync());
            var account = await verify.UserAccounts.SingleAsync(item => item.AccountId == accountId);
            Assert.Equal((byte)1, account.AccountStatus);
            Assert.False(account.MustChangePin);
            Assert.Equal(1, await verify.UserCredentials.CountAsync(x =>
                x.AccountId == accountId && x.RevokedAtUtc == null));
            Assert.Contains(await verify.UserRoleAssignments.Where(x =>
                x.AccountId == accountId && x.RevokedAtUtc == null).Select(x => x.RoleKey).ToListAsync(),
                role => role == "resident");
            Assert.Contains(await verify.UserRoleAssignments.Where(x =>
                x.AccountId == accountId && x.RevokedAtUtc == null).Select(x => x.RoleKey).ToListAsync(),
                role => role == "building-admin");
        }

        var authentication = new IdentityAccountAuthenticationService(factory, hasher);
        var residentLogin = await authentication.AuthenticateResidentAsync(
            "01000000123", "583921", "building-1", "apt-12");
        var secondApartmentLogin = await authentication.AuthenticateResidentAsync(
            "01000000123", "583921", "building-1", "apt-13");
        var adminLogin = await authentication.AuthenticateAdminAsync(
            "01000000123", "583921", "building-1");

        Assert.NotNull(residentLogin);
        Assert.True(residentLogin.Success);
        Assert.Equal(accountId.ToString("D"), residentLogin.Uid);
        Assert.Equal("resident", residentLogin.Role);
        Assert.NotNull(secondApartmentLogin);
        Assert.True(secondApartmentLogin.Success);
        Assert.Equal(accountId.ToString("D"), secondApartmentLogin.Uid);
        Assert.Equal("apt-13", secondApartmentLogin.ApartmentId);
        Assert.NotNull(adminLogin);
        Assert.True(adminLogin.Success);
        Assert.Equal(accountId.ToString("D"), adminLogin.Uid);
        Assert.Equal("admin", adminLogin.Role);

        await service.DeactivateAsync(accountId.ToString("D"), "superadmin-test");
        var residentAfterAdminDeactivation = await authentication.AuthenticateResidentAsync(
            "01000000123", "583921", "building-1", "apt-12");
        var adminAfterDeactivation = await authentication.AuthenticateAdminAsync(
            "01000000123", "583921", "building-1");
        var disabledAdmin = await service.GetAdminAsync(accountId.ToString("D"));

        Assert.NotNull(residentAfterAdminDeactivation);
        Assert.True(residentAfterAdminDeactivation.Success);
        Assert.Null(adminAfterDeactivation);
        Assert.NotNull(disabledAdmin);
        Assert.False(disabledAdmin.IsActive);

        await service.ActivateAsync(accountId.ToString("D"), "superadmin-test");
        var adminAfterReactivation = await authentication.AuthenticateAdminAsync(
            "01000000123", "583921", "building-1");
        Assert.NotNull(adminAfterReactivation);
        Assert.True(adminAfterReactivation.Success);
    }

    private sealed class TestFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public TestFactory(DbContextOptions<AppDbContext> options) => _options = options;
        public AppDbContext CreateDbContext() => new(_options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(_options));
    }
}
