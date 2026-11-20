using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class ResidentsServiceSetPinTests
{
    [Fact]
    public async Task ApartmentWidePinIsRejectedWithoutMutatingAnyAccountOrCredential()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync();
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            context.Service.SetApartmentPinAsync("b1", "a1", "4321", "admin"));
        await using var db = context.CreateDb();
        Assert.Empty(await db.Persons.ToListAsync());
        Assert.Empty(await db.UserAccounts.ToListAsync());
        Assert.Empty(await db.UserCredentials.ToListAsync());
        Assert.Empty(await db.ApartmentOccupancies.ToListAsync());
    }

    [Fact]
    public async Task UnknownApartmentAlsoCannotCreateSharedCredential()
    {
        await using var context = new ResidentsServiceTestContext();
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            context.Service.SetApartmentPinAsync("b1", "missing", "4321", "admin"));
        await using var db = context.CreateDb();
        Assert.Empty(await db.UserCredentials.ToListAsync());
    }
}
