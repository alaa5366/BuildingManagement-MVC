using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class ResidentsServiceTests
{
    private static Task<Guid> Create(ResidentsServiceTestContext c, string apartment = "a1", string name = "Ali", string phone = "01001234567", Guid? personId = null, bool createLogin = true)
        => c.Service.CreateAsync("b1", apartment, name, phone, phone, null, true, true, "system", personId, createLogin);

    [Fact]
    public async Task CreateAsync_CreatesPersonOccupancyAndPendingAccountWithoutCredential()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync();
        var occupancyId = await Create(context);
        await using var db = context.CreateDb();
        var person = await db.Persons.SingleAsync();
        var account = await db.UserAccounts.SingleAsync();
        Assert.Equal(account.PersonId, person.PersonId);
        Assert.Equal((byte)0, account.AccountStatus);
        Assert.True(account.MustChangePin);
        Assert.Empty(await db.UserCredentials.ToListAsync());
        Assert.Equal("resident", (await db.UserRoleAssignments.SingleAsync()).RoleKey);
        Assert.Equal(occupancyId, (await db.ApartmentOccupancies.SingleAsync()).OccupancyId);
    }

    [Fact]
    public async Task SamePersonCanOccupyAnotherApartmentWithOneAccountAndAnotherScope()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync("b1", "a1");
        await context.SeedApartmentAsync("b1", "a2");
        await Create(context);
        await using var before = context.CreateDb();
        var personId = await before.Persons.Select(x => x.PersonId).SingleAsync();
        await Create(context, "a2", "Ali", "01001234567", personId);
        await using var db = context.CreateDb();
        Assert.Single(await db.UserAccounts.ToListAsync());
        Assert.Equal(2, await db.ApartmentOccupancies.CountAsync());
        Assert.Equal(2, await db.UserRoleAssignments.CountAsync());
    }

    [Fact]
    public async Task MoveAsync_PreservesPersonAndAccountAndTransfersOnlyApartmentScope()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync("b1", "a1");
        await context.SeedApartmentAsync("b1", "a2");
        var oldOccupancyId = await Create(context);
        var newOccupancyId = await context.Service.MoveAsync(oldOccupancyId.ToString("D"), "a2", "system");
        var repeated = await context.Service.MoveAsync(newOccupancyId.ToString("D"), "a2", "system");
        Assert.Equal(newOccupancyId, repeated);
        await using var db = context.CreateDb();
        Assert.Single(await db.Persons.ToListAsync());
        Assert.Single(await db.UserAccounts.ToListAsync());
        Assert.Equal((byte)0, (await db.UserAccounts.SingleAsync()).AccountStatus);
        Assert.Equal(2, await db.ApartmentOccupancies.CountAsync());
        Assert.NotNull((await db.ApartmentOccupancies.SingleAsync(x => x.OccupancyId == oldOccupancyId)).ValidToUtc);
        Assert.Null((await db.ApartmentOccupancies.SingleAsync(x => x.OccupancyId == newOccupancyId)).ValidToUtc);
        Assert.Equal(2, await db.UserRoleAssignments.CountAsync());
        Assert.Single(await db.UserRoleAssignments.Where(x => x.RevokedAtUtc == null && x.ScopeApartmentId == "a2").ToListAsync());
        Assert.Single(await db.UserRoleAssignments.Where(x => x.RevokedAtUtc != null && x.ScopeApartmentId == "a1").ToListAsync());
    }

    [Fact]
    public async Task MoveAsync_RejectsTargetApartmentInAnotherBuildingWithoutChangingOccupancy()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync("b1", "a1");
        await context.SeedApartmentAsync("b2", "a2");
        var occupancyId = await Create(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Service.MoveAsync(occupancyId.ToString("D"), "a2", "system"));

        await using var db = context.CreateDb();
        var occupancy = await db.ApartmentOccupancies.SingleAsync();
        Assert.Equal(occupancyId, occupancy.OccupancyId);
        Assert.Equal("b1", occupancy.BuildingId);
        Assert.Equal("a1", occupancy.ApartmentId);
        Assert.Null(occupancy.ValidToUtc);
        Assert.Single(await db.UserRoleAssignments.Where(x => x.RevokedAtUtc == null).ToListAsync());
    }

    [Fact]
    public async Task RepeatedRegistrationForSamePersonAndApartmentIsIdempotent()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync();
        var first = await Create(context);
        await using var db1 = context.CreateDb();
        var personId = await db1.Persons.Select(x => x.PersonId).SingleAsync();
        var second = await Create(context, "a1", "Ali", "01001234567", personId);
        Assert.Equal(first, second);
        await using var db = context.CreateDb();
        Assert.Single(await db.Persons.ToListAsync());
        Assert.Single(await db.UserAccounts.ToListAsync());
        Assert.Single(await db.ApartmentOccupancies.ToListAsync());
        Assert.Single(await db.UserRoleAssignments.ToListAsync());
    }

    [Fact]
    public async Task RepeatedNewPersonRegistrationWithSameRequestTokenIsIdempotent()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync();
        var requestPersonId = Guid.NewGuid();
        var first = await context.Service.CreateAsync("b1", "a1", "Token Resident", "01001234567", "01001234567", null,
            true, true, "system", registrationPersonId: requestPersonId);
        var second = await context.Service.CreateAsync("b1", "a1", "Token Resident", "01001234567", "01001234567", null,
            true, true, "system", registrationPersonId: requestPersonId);
        Assert.Equal(first, second);
        await using var db = context.CreateDb();
        Assert.Single(await db.Persons.ToListAsync());
        Assert.Single(await db.UserAccounts.ToListAsync());
        Assert.Single(await db.ApartmentOccupancies.ToListAsync());
        Assert.Single(await db.UserRoleAssignments.ToListAsync());
    }

    [Fact]
    public async Task DuplicatePhoneDoesNotMergeDifferentPeople()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync("b1", "a1");
        await context.SeedApartmentAsync("b1", "a2");
        await Create(context, "a1", "Person One", "01001234567");
        await Create(context, "a2", "Person Two", "01001234567");
        await using var db = context.CreateDb();
        Assert.Equal(2, await db.Persons.CountAsync());
        Assert.Equal(2, await db.UserAccounts.CountAsync());
        Assert.Empty(await db.UserCredentials.ToListAsync());
    }

    [Fact]
    public async Task MissingPhoneStillCreatesPendingAccountWithoutCredential()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync();
        await context.Service.CreateAsync("b1", "a1", "No Phone Resident", "", "", null, true, false, "system");
        await using var db = context.CreateDb();
        Assert.Null((await db.Persons.SingleAsync()).PrimaryPhone);
        Assert.Equal((byte)0, (await db.UserAccounts.SingleAsync()).AccountStatus);
        Assert.Empty(await db.UserCredentials.ToListAsync());
    }

    [Fact]
    public async Task ReturningPersonReusesAccountButRequiresNewEnrollment()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync("b1", "a1");
        await context.SeedApartmentAsync("b1", "a2");
        var firstOccupancy = await Create(context);
        await using var before = context.CreateDb();
        var personId = await before.Persons.Select(x => x.PersonId).SingleAsync();
        await context.Service.DeleteAsync(firstOccupancy.ToString("D"), "system");
        await context.Service.CreateAsync("b1", "a2", "Ali", "01001234567", "01001234567", null,
            true, true, "system", personId);
        await using var db = context.CreateDb();
        Assert.Single(await db.Persons.ToListAsync());
        var account = await db.UserAccounts.SingleAsync();
        Assert.Equal((byte)0, account.AccountStatus);
        Assert.True(account.MustChangePin);
        Assert.Empty(await db.UserCredentials.ToListAsync());
        Assert.Equal(2, await db.ApartmentOccupancies.CountAsync());
    }

    [Fact]
    public async Task LoginCanBeOmittedWhenOccupancyIsCreated()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync();
        await Create(context, createLogin: false);
        await using var db = context.CreateDb();
        Assert.Single(await db.Persons.ToListAsync());
        Assert.Single(await db.ApartmentOccupancies.ToListAsync());
        Assert.Empty(await db.UserAccounts.ToListAsync());
        Assert.Empty(await db.UserRoleAssignments.ToListAsync());
        Assert.Single(await context.Service.GetByApartmentAsync("b1", "a1"));
    }

    [Fact]
    public async Task UpdateAsync_ChangesPersonAndOccupancyWithoutLegacyResidentRows()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync();
        await Create(context);
        var resident = (await context.Service.GetByApartmentAsync("b1", "a1")).Single();
        await context.Service.UpdateAsync(resident.Id, "Ali Updated", "01111111111", "01111111111", "updated@example.test", false, false, "system");
        await using var db = context.CreateDb();
        Assert.Equal("Ali Updated", (await db.Persons.SingleAsync()).DisplayName);
        Assert.False((await db.ApartmentOccupancies.SingleAsync()).IsPrimary);
    }

    [Fact]
    public async Task SetDisabledAsync_RevokesAndRestoresOnlyTheApartmentRoleAssignment()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync();
        await Create(context);
        var resident = (await context.Service.GetByApartmentAsync("b1", "a1")).Single();
        await context.Service.SetDisabledAsync(resident.Id, true, "review", "system");
        await context.Service.SetDisabledAsync(resident.Id, false, null, "system");
        await using var db = context.CreateDb();
        Assert.Equal(2, await db.UserRoleAssignments.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_ClosesOccupancyAndRetainsIdentityAndAccount()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync();
        await Create(context);
        var resident = (await context.Service.GetByApartmentAsync("b1", "a1")).Single();
        await context.Service.DeleteAsync(resident.Id, "system");
        await using var db = context.CreateDb();
        Assert.Equal(0, await context.Service.CountAsync());
        Assert.Single(await db.Persons.ToListAsync());
        var account = await db.UserAccounts.SingleAsync();
        Assert.Equal((byte)3, account.AccountStatus);
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
        await Create(context, "a1", "Ali", "01000000001");
        await Create(context, "a2", "Sara", "01000000002");
        await context.Service.CreateAsync("b2", "a3", "Omar", "01000000003", "01000000003", null, true, true, "system");
        var residents = await context.Service.GetByBuildingAsync("b1");
        Assert.Equal(2, residents.Count);
        Assert.All(residents, resident => Assert.Equal("b1", resident.BuildingId));
    }
}
