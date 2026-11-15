using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BuildingManagement.Tests;

/// <summary>
/// Tests for <see cref="ResidentsService.SetApartmentPinAsync"/>, the method behind the set-PIN link
/// and the admin PIN reset. Uses the EF Core in-memory provider.
/// </summary>
public class ResidentsServiceSetPinTests
{
    private readonly AppDbContext _db;
    private readonly ResidentsService _service;

    public ResidentsServiceSetPinTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);

        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => new AppDbContext(options));

        _service = new ResidentsService(
            factory.Object,
            new Mock<IAuditLogger>().Object,
            NullLogger<ResidentsService>.Instance);
    }

    private static ResidentEntity Resident(string id, string building, string apt, string pin) => new()
    {
        Id = id,
        Uid = "u-" + id,
        BuildingId = building,
        ApartmentId = apt,
        Name = "Resident " + id,
        PinHash = SecretVerifier.Hash(pin)
    };

    [Fact]
    public async Task Creates_a_primary_resident_from_the_apartment_when_none_exists()
    {
        _db.Apartments.Add(new ApartmentEntity
        {
            BuildingId = "b1", Id = "a1", FloorId = "f1", Number = 3, Owner = "Sara", Phone = "01001234567"
        });
        await _db.SaveChangesAsync();

        await _service.SetApartmentPinAsync("b1", "a1", "4321", "setpin-link");

        _db.ChangeTracker.Clear();
        var resident = await _db.Residents.SingleAsync();
        Assert.True(resident.IsPrimary);
        Assert.Equal("Sara", resident.Name);
        Assert.NotEqual("4321", resident.PinHash);
        Assert.True(SecretVerifier.Verify("4321", resident.PinHash));
    }

    [Fact]
    public async Task Updates_every_resident_of_the_apartment_and_nobody_else()
    {
        _db.Residents.AddRange(
            Resident("r1", "b1", "a1", "1111"),
            Resident("r2", "b1", "a1", "2222"),
            Resident("r3", "b1", "a2", "3333"),      // شقة تانية في نفس العمارة
            Resident("r4", "b2", "a1", "4444"));     // نفس رقم الشقة في عمارة تانية
        await _db.SaveChangesAsync();

        await _service.SetApartmentPinAsync("b1", "a1", "9999", "admin");

        _db.ChangeTracker.Clear();
        var all = await _db.Residents.ToDictionaryAsync(r => r.Id);
        Assert.True(SecretVerifier.Verify("9999", all["r1"].PinHash));
        Assert.True(SecretVerifier.Verify("9999", all["r2"].PinHash));
        Assert.True(SecretVerifier.Verify("3333", all["r3"].PinHash));
        Assert.True(SecretVerifier.Verify("4444", all["r4"].PinHash));
    }

    [Fact]
    public async Task Unknown_apartment_throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SetApartmentPinAsync("b1", "missing", "1234", "admin"));
    }
}
