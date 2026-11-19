using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class MaintenanceServiceTests
{
    private readonly MaintenanceService _service = new();

    private static Building CreateBuilding()
    {
        var b = new Building { Name = "Test" };
        b.Apartments.Add(new Apartment { Id = "a1", Number = 1, MonthlyFee = 100 });
        b.Apartments.Add(new Apartment { Id = "a2", Number = 2, MonthlyFee = 100 });
        return b;
    }

    // ================================================================
    // Add
    // ================================================================

    [Fact]
    public void Add_ValidData_CreatesRecord()
    {
        var b = CreateBuilding();
        var record = _service.Add(b, "Fix elevator", "Broken", "2025-01-15", 500, "ABC", "open", "admin");

        Assert.NotNull(record);
        Assert.Equal("Fix elevator", record.Title);
        Assert.Equal(500, record.Cost);
        Assert.Equal("open", record.Status);
        Assert.Equal("admin", record.CreatedBy);
        Assert.False(record.AddedToExpense);
        Assert.Single(b.MaintenanceLog);
    }

    [Fact]
    public void Add_NullStatus_DefaultsToOpen()
    {
        var b = CreateBuilding();
        var record = _service.Add(b, "Test", null, "2025-01-15", 100, null, null!, "admin");
        Assert.Equal("open", record.Status);
    }

    [Fact]
    public void Add_EmptyDescriptionAndVendor_DefaultsToEmptyStrings()
    {
        var b = CreateBuilding();
        var record = _service.Add(b, "Test", null, "2025-01-15", 100, null, "open", "admin");
        Assert.Equal("", record.Description);
        Assert.Equal("", record.Vendor);
    }

    // ================================================================
    // Update
    // ================================================================

    [Fact]
    public void Update_ExistingRecord_UpdatesFields()
    {
        var b = CreateBuilding();
        var record = _service.Add(b, "Old", "Old desc", "2025-01-01", 100, "V1", "open", "admin");

        var updated = _service.Update(b, record.Id, "New Title", "New desc", "2025-01-20", 200, "V2", "done");

        Assert.NotNull(updated);
        Assert.Equal("New Title", updated.Title);
        Assert.Equal(200, updated.Cost);
        Assert.Equal("done", updated.Status);
        Assert.Equal("V2", updated.Vendor);
    }

    [Fact]
    public void Update_NonExistingRecord_ReturnsNull()
    {
        var b = CreateBuilding();
        var result = _service.Update(b, "ghost", "T", "D", "2025-01-01", 100, "V", "open");
        Assert.Null(result);
    }

    // ================================================================
    // Delete
    // ================================================================

    [Fact]
    public void Delete_ExistingRecord_RemovesIt()
    {
        var b = CreateBuilding();
        var record = _service.Add(b, "Test", null, "2025-01-01", 100, null, "open", "admin");

        var result = _service.Delete(b, record.Id);

        Assert.True(result);
        Assert.Empty(b.MaintenanceLog);
    }

    [Fact]
    public void Delete_NonExistingRecord_ReturnsFalse()
    {
        var b = CreateBuilding();
        Assert.False(_service.Delete(b, "ghost"));
    }
}
