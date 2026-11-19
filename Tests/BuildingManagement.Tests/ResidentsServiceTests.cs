using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class ResidentsServiceTests
{
    [Fact]
    public async Task CreateAsync_StoresPersonAccountCredentialRoleAndOccupancyInTargetSchema()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync();

        await context.Service.CreateAsync("b1", "a1", "Ali", "01001234567", "01001234567",
            "4321", "ali@example.test", true, true, "system");

        await using var db = context.CreateDb();
        var person = await db.Persons.SingleAsync();
        var account = await db.UserAccounts.SingleAsync();
        var credential = await db.UserCredentials.SingleAsync();
        var occupancy = await db.ApartmentOccupancies.SingleAsync();
        Assert.Equal("Ali", person.DisplayName);
        Assert.Equal(account.PersonId, person.PersonId);
        Assert.True(new BcryptPasswordHasher().VerifyPin("4321", credential.CredentialHash));
        Assert.Equal("resident", (await db.UserRoleAssignments.SingleAsync()).RoleKey);
        Assert.True(occupancy.IsPrimary);
        Assert.Equal("a1", occupancy.ApartmentId);
    }

    [Fact]
    public async Task UpdateAsync_ChangesPersonAndOccupancyWithoutLegacyResidentRows()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync();
        await context.Service.CreateAsync("b1", "a1", "Ali", "01001234567", "01001234567",
            "4321", null, true, true, "system");
        var resident = (await context.Service.GetByApartmentAsync("b1", "a1")).Single();

        await context.Service.UpdateAsync(resident.Id, "Ali Updated", "01111111111",
            "01111111111", "updated@example.test", false, false, "system");

        await using var db = context.CreateDb();
        var person = await db.Persons.SingleAsync();
        var occupancy = await db.ApartmentOccupancies.SingleAsync();
        Assert.Equal("Ali Updated", person.DisplayName);
        Assert.Equal(AuthHelpers.NormalizePhone("01111111111"), person.PrimaryPhone);
        Assert.False(occupancy.IsPrimary);
        Assert.Equal((byte)2, occupancy.OccupancyType);
    }

    [Fact]
    public async Task ChangePinAsync_ReplacesCredentialAndRotatesSecurityStamp()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync();
        await context.Service.CreateAsync("b1", "a1", "Ali", "01001234567", "01001234567",
            "4321", null, true, true, "system");
        var resident = (await context.Service.GetByApartmentAsync("b1", "a1")).Single();
        await using var beforeDb = context.CreateDb();
        var accountBefore = await beforeDb.UserAccounts.SingleAsync();
        var oldStamp = accountBefore.SecurityStamp;

        await context.Service.ChangePinAsync(resident.Id, "9876", "system");

        await using var db = context.CreateDb();
        var account = await db.UserAccounts.SingleAsync();
        var credentials = await db.UserCredentials.OrderBy(item => item.CreatedAtUtc).ToListAsync();
        Assert.NotEqual(oldStamp, account.SecurityStamp);
        Assert.True(credentials[0].RevokedAtUtc.HasValue);
        Assert.True(new BcryptPasswordHasher().VerifyPin("9876", credentials[1].CredentialHash));
    }

    [Fact]
    public async Task SetDisabledAsync_RevokesAndRestoresOnlyTheApartmentRoleAssignment()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync();
        await context.Service.CreateAsync("b1", "a1", "Ali", "01001234567", "01001234567",
            "4321", null, true, true, "system");
        var resident = (await context.Service.GetByApartmentAsync("b1", "a1")).Single();

        await context.Service.SetDisabledAsync(resident.Id, true, "unpaid", "system");
        Assert.True((await context.Service.GetByIdAsync(resident.Id))!.IsDisabled);
        await context.Service.SetDisabledAsync(resident.Id, false, null, "system");
        Assert.False((await context.Service.GetByIdAsync(resident.Id))!.IsDisabled);

        await using var db = context.CreateDb();
        Assert.Equal(2, await db.UserRoleAssignments.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_ClosesOccupancyAndRevokesRoleInsteadOfDeletingIdentity()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync();
        await context.Service.CreateAsync("b1", "a1", "Ali", "01001234567", "01001234567",
            "4321", null, true, true, "system");
        var resident = (await context.Service.GetByApartmentAsync("b1", "a1")).Single();

        await context.Service.DeleteAsync(resident.Id, "system");

        await using var db = context.CreateDb();
        Assert.Equal(0, await context.Service.CountAsync());
        Assert.Single(await db.Persons.ToListAsync());
        Assert.Single(await db.UserAccounts.ToListAsync());
        Assert.NotNull((await db.ApartmentOccupancies.SingleAsync()).ValidToUtc);
        Assert.NotNull((await db.UserRoleAssignments.SingleAsync()).RevokedAtUtc);
    }

    [Fact]
    public async Task GetByBuildingAsync_ReturnsOnlyResidentsInRequestedBuilding()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync("b1", "a1");
        await context.SeedApartmentAsync("b1", "a2");
        await context.SeedApartmentAsync("b2", "a3");
        await context.Service.CreateAsync("b1", "a1", "Ali", "01000000001", "01000000001", "4321", null, true, true, "system");
        await context.Service.CreateAsync("b1", "a2", "Sara", "01000000002", "01000000002", "4321", null, true, true, "system");
        await context.Service.CreateAsync("b2", "a3", "Omar", "01000000003", "01000000003", "4321", null, true, true, "system");

        var residents = await context.Service.GetByBuildingAsync("b1");

        Assert.Equal(2, residents.Count);
        Assert.All(residents, resident => Assert.Equal("b1", resident.BuildingId));
    }
}
