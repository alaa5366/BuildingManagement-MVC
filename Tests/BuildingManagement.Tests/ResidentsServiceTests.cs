using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BuildingManagement.Tests;

public class ResidentsServiceTests
{
    private readonly AppDbContext _db;
    private readonly ResidentsService _service;
    private readonly Mock<IAuditLogger> _mockAudit;

    public ResidentsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // ✅ نستخدم نفس الـ context في الـ service والـ assertions
        _db = new AppDbContext(options);

        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        // ✅ كل استدعاء بيفتح context جديد لكن على نفس الـ DB
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => new AppDbContext(options));

        _mockAudit = new Mock<IAuditLogger>();

        _service = new ResidentsService(
            factory.Object,
            _mockAudit.Object,
            NullLogger<ResidentsService>.Instance
        );
    }

    // ================================================================
    // CreateAsync
    // ================================================================

    [Fact]
    public async Task CreateAsync_ValidData_CreatesResidentWithHashedPin()
    {
        await _service.CreateAsync("b1", "a1", "Ali", "01001234567", "01001234567", "1234", "ali@test.com", true, true, "admin");

        _db.ChangeTracker.Clear(); // ✅ نتأكد إننا بنقرأ من الـ DB
        var resident = await _db.Residents.FirstOrDefaultAsync();

        Assert.NotNull(resident);
        Assert.Equal("Ali", resident.Name);
        // ✅ نتوقع الناتج الفعلي من NormalizePhone
        Assert.Equal(AuthHelpers.NormalizePhone("01001234567"), resident.Phone);
        Assert.NotEqual("1234", resident.PinHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("1234", resident.PinHash));
        Assert.True(resident.IsPrimary);
        Assert.True(resident.IsOwner);
    }

    [Fact]
    public async Task CreateAsync_NormalizesPhoneNumber()
    {
        await _service.CreateAsync("b1", "a1", "Ali", "+201001234567", "+201001234567", "1234", null, true, true, "admin");

        _db.ChangeTracker.Clear();
        var resident = await _db.Residents.FirstOrDefaultAsync();

        Assert.NotNull(resident);
        // ✅ نتوقع الناتج الفعلي
        Assert.Equal(AuthHelpers.NormalizePhone("+201001234567"), resident.Phone);
    }

    // ================================================================
    // UpdateAsync
    // ================================================================

    [Fact]
    public async Task UpdateAsync_ExistingResident_UpdatesFields()
    {
        await _service.CreateAsync("b1", "a1", "Ali", "01001234567", "01001234567", "1234", null, true, true, "admin");

        _db.ChangeTracker.Clear();
        var resident = await _db.Residents.FirstAsync();
        var residentId = resident.Id;

        await _service.UpdateAsync(residentId, "Ali Updated", "01111111111", "01111111111", "new@test.com", false, false, "admin");

        _db.ChangeTracker.Clear();
        var updated = await _db.Residents.FindAsync(residentId);

        Assert.NotNull(updated);
        Assert.Equal("Ali Updated", updated.Name);
        Assert.Equal(AuthHelpers.NormalizePhone("01111111111"), updated.Phone);
        Assert.False(updated.IsPrimary);
        Assert.False(updated.IsOwner);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingResident_DoesNothing()
    {
        await _service.UpdateAsync("ghost", "Ali", "0100", "0100", null, true, true, "admin");

        _db.ChangeTracker.Clear();
        Assert.Empty(_db.Residents);
    }

    // ================================================================
    // ChangePinAsync
    // ================================================================

    [Fact]
    public async Task ChangePinAsync_UpdatesPinHash()
    {
        await _service.CreateAsync("b1", "a1", "Ali", "01001234567", "01001234567", "1234", null, true, true, "admin");

        _db.ChangeTracker.Clear();
        var resident = await _db.Residents.FirstAsync();
        var residentId = resident.Id;

        await _service.ChangePinAsync(residentId, "9999", "admin");

        _db.ChangeTracker.Clear();
        var updated = await _db.Residents.FindAsync(residentId);

        Assert.NotNull(updated);
        Assert.True(BCrypt.Net.BCrypt.Verify("9999", updated.PinHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("1234", updated.PinHash));
    }

    // ================================================================
    // SetDisabledAsync
    // ================================================================

    [Fact]
    public async Task SetDisabledAsync_DisablesResident()
    {
        await _service.CreateAsync("b1", "a1", "Ali", "01001234567", "01001234567", "1234", null, true, true, "admin");

        _db.ChangeTracker.Clear();
        var resident = await _db.Residents.FirstAsync();
        var residentId = resident.Id;

        await _service.SetDisabledAsync(residentId, true, "unpaid", "admin");

        _db.ChangeTracker.Clear();
        var updated = await _db.Residents.FindAsync(residentId);

        Assert.NotNull(updated);
        Assert.True(updated.IsDisabled);
        Assert.Equal("unpaid", updated.DisabledReason);
    }

    [Fact]
    public async Task SetDisabledAsync_EnablesResident_ClearsReason()
    {
        await _service.CreateAsync("b1", "a1", "Ali", "01001234567", "01001234567", "1234", null, true, true, "admin");

        _db.ChangeTracker.Clear();
        var resident = await _db.Residents.FirstAsync();
        var residentId = resident.Id;

        await _service.SetDisabledAsync(residentId, true, "unpaid", "admin");
        await _service.SetDisabledAsync(residentId, false, null, "admin");

        _db.ChangeTracker.Clear();
        var updated = await _db.Residents.FindAsync(residentId);

        Assert.NotNull(updated);
        Assert.False(updated.IsDisabled);
        Assert.Equal("", updated.DisabledReason);
    }

    // ================================================================
    // DeleteAsync
    // ================================================================

    [Fact]
    public async Task DeleteAsync_RemovesResident()
    {
        await _service.CreateAsync("b1", "a1", "Ali", "01001234567", "01001234567", "1234", null, true, true, "admin");

        _db.ChangeTracker.Clear();
        var resident = await _db.Residents.FirstAsync();
        var residentId = resident.Id;

        await _service.DeleteAsync(residentId, "admin");

        _db.ChangeTracker.Clear();
        Assert.Empty(_db.Residents);
    }

    // ================================================================
    // GetByBuildingAsync
    // ================================================================

    [Fact]
    public async Task GetByBuildingAsync_ReturnsResidentsForBuilding()
    {
        await _service.CreateAsync("b1", "a1", "Ali", "0100", "0100", "1234", null, true, true, "admin");
        await _service.CreateAsync("b1", "a2", "Sara", "0101", "0101", "1234", null, true, true, "admin");
        await _service.CreateAsync("b2", "a3", "Omar", "0102", "0102", "1234", null, true, true, "admin");

        var residents = await _service.GetByBuildingAsync("b1");

        Assert.Equal(2, residents.Count);
        Assert.All(residents, r => Assert.Equal("b1", r.BuildingId));
    }

    // ================================================================
    // SetApartmentPinAsync
    // ================================================================

    [Fact]
    public async Task SetApartmentPinAsync_NoExistingResident_CreatesPrimaryResident()
    {
        // Arrange
        var building = new BuildingEntity { Id = "b1", Name = "Test", BuildingNumber = "BLD-001" };
        var floor = new FloorEntity { BuildingId = "b1", Id = "f1", Label = "F1" };
        var apt = new ApartmentEntity { BuildingId = "b1", Id = "a1", FloorId = "f1", Number = 1, Owner = "Owner", Phone = "0100" };

        _db.Buildings.Add(building);
        _db.Floors.Add(floor);
        _db.Apartments.Add(apt);
        await _db.SaveChangesAsync();

        // Act
        await _service.SetApartmentPinAsync("b1", "a1", "1234", "admin");

        // Assert
        _db.ChangeTracker.Clear();
        var resident = await _db.Residents.FirstOrDefaultAsync();
        Assert.NotNull(resident);
        Assert.Equal("a1", resident.ApartmentId);
        Assert.True(resident.IsPrimary);
        Assert.True(BCrypt.Net.BCrypt.Verify("1234", resident.PinHash));
    }

    [Fact]
    public async Task SetApartmentPinAsync_ExistingResident_UpdatesPin()
    {
        // Arrange
        await _service.CreateAsync("b1", "a1", "Ali", "0100", "0100", "1234", null, true, true, "admin");

        // Act
        await _service.SetApartmentPinAsync("b1", "a1", "9999", "admin");

        // Assert
        _db.ChangeTracker.Clear();
        var resident = await _db.Residents.FirstAsync();
        Assert.True(BCrypt.Net.BCrypt.Verify("9999", resident.PinHash));
    }
}