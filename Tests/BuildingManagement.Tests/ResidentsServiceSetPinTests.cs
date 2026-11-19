using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class ResidentsServiceSetPinTests
{
    [Fact]
    public async Task Creates_primary_target_account_from_apartment_when_none_exists()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync("b1", "a1", "Sara", "01001234567");

        await context.Service.SetApartmentPinAsync("b1", "a1", "4321", "setpin-link");

        await using var db = context.CreateDb();
        var person = await db.Persons.SingleAsync();
        var account = await db.UserAccounts.SingleAsync();
        var credential = await db.UserCredentials.SingleAsync();
        var occupancy = await db.ApartmentOccupancies.SingleAsync();
        Assert.True(occupancy.IsPrimary);
        Assert.Equal("Sara", person.DisplayName);
        Assert.True(new BcryptPasswordHasher().VerifyPin("4321", credential.CredentialHash));
        Assert.Equal(account.AccountId, (await db.UserRoleAssignments.SingleAsync()).AccountId);
    }

    [Fact]
    public async Task Updates_every_target_account_in_the_apartment_and_nobody_else()
    {
        await using var context = new ResidentsServiceTestContext();
        await context.SeedApartmentAsync("b1", "a1");
        await context.SeedApartmentAsync("b1", "a2");
        await context.SeedApartmentAsync("b2", "a1");
        await context.Service.CreateAsync("b1", "a1", "One", "01000000001", "01000000001", "1111", null, true, true, "system");
        await context.Service.CreateAsync("b1", "a1", "Two", "01000000002", "01000000002", "2222", null, false, false, "system");
        await context.Service.CreateAsync("b1", "a2", "Three", "01000000003", "01000000003", "3333", null, true, true, "system");
        await context.Service.CreateAsync("b2", "a1", "Four", "01000000004", "01000000004", "4444", null, true, true, "system");

        await context.Service.SetApartmentPinAsync("b1", "a1", "9999", "admin");

        await using var db = context.CreateDb();
        var credentials = await db.UserCredentials.Where(item => item.RevokedAtUtc == null)
            .ToListAsync();
        var accountByPhone = await (
            from person in db.Persons
            join account in db.UserAccounts on person.PersonId equals account.PersonId
            select new { person.PrimaryPhone, account.AccountId }).ToDictionaryAsync(item => item.PrimaryPhone!, item => item.AccountId);
        foreach (var phone in new[] { "01000000001", "01000000002" })
        {
            var accountId = accountByPhone[AuthHelpers.NormalizePhone(phone)];
            var credential = credentials.Single(item => item.AccountId == accountId);
            Assert.True(new BcryptPasswordHasher().VerifyPin("9999", credential.CredentialHash));
        }
        foreach (var (phone, originalPin) in new[] { ("01000000003", "3333"), ("01000000004", "4444") })
        {
            var accountId = accountByPhone[AuthHelpers.NormalizePhone(phone)];
            var credential = credentials.Single(item => item.AccountId == accountId);
            Assert.True(new BcryptPasswordHasher().VerifyPin(originalPin, credential.CredentialHash));
        }
    }

    [Fact]
    public async Task Unknown_apartment_throws_without_creating_legacy_resident_rows()
    {
        await using var context = new ResidentsServiceTestContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Service.SetApartmentPinAsync("b1", "missing", "4321", "admin"));
        await using var db = context.CreateDb();
    }
}
