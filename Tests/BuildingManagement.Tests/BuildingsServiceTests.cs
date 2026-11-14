using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildingManagement.Tests;

public class BuildingsServiceTests
{
    // كلاس Fake بيورث من BuildingSqlStore عشان نتحكم في البيانات اللي بترجع
    private class FakeBuildingStore : BuildingSqlStore
    {
        public List<Building> SummariesToReturn = new();
        public List<Building> SummariesWithCountsToReturn = new();
        public string? BuildingIdToReturn = null;
        public Building? BuildingToReturn = null;

        public FakeBuildingStore() : base(null!, null!) { }

        public override Task<List<Building>> GetSummariesAsync() => Task.FromResult(SummariesToReturn);
        public override Task<List<Building>> GetSummariesWithCountsAsync() => Task.FromResult(SummariesWithCountsToReturn);
        public override Task<string?> FindBuildingIdByApartmentAsync(string apartmentId) => Task.FromResult(BuildingIdToReturn);
        public override Task<Building?> GetByIdAsync(string id) => Task.FromResult(BuildingToReturn);
    }

    private readonly FakeBuildingStore _fakeStore;
    private readonly BuildingsService _service;

    public BuildingsServiceTests()
    {
        _fakeStore = new FakeBuildingStore();
        var mockLogger = NullLogger<BuildingsService>.Instance;
        _service = new BuildingsService(_fakeStore, mockLogger);
    }

    // ================================================================
    // GetSummariesAsync
    // ================================================================

    [Fact]
    public async Task GetSummariesAsync_ReturnsListOfSummaries()
    {
        _fakeStore.SummariesToReturn = new List<Building>
        {
            new() { Id = "b1", Name = "Tower 1", BuildingNumber = "BLD-001" },
            new() { Id = "b2", Name = "Tower 2", BuildingNumber = "BLD-002" }
        };

        var result = await _service.GetSummariesAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Tower 1", result[0].Name);
    }

    [Fact]
    public async Task GetSummariesAsync_WhenStoreReturnsEmpty_ReturnsEmptyList()
    {
        _fakeStore.SummariesToReturn = new List<Building>();

        var result = await _service.GetSummariesAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ================================================================
    // GetSummariesWithCountsAsync
    // ================================================================

    [Fact]
    public async Task GetSummariesWithCountsAsync_ReturnsBuildingsWithCounts()
    {
        _fakeStore.SummariesWithCountsToReturn = new List<Building>
        {
            new()
            {
                Id = "b1",
                Name = "Tower 1",
                Apartments = new List<Apartment> { new(), new(), new() },
                Floors = new List<Floor> { new(), new() }
            }
        };

        var result = await _service.GetSummariesWithCountsAsync();

        Assert.Single(result);
        Assert.Equal(3, result[0].Apartments.Count);
        Assert.Equal(2, result[0].Floors.Count);
    }

    // ================================================================
    // FindBuildingIdByApartmentAsync
    // ================================================================

    [Theory]
    [InlineData("apt-123", "bld-456")]
    [InlineData("apt-999", "bld-888")]
    public async Task FindBuildingIdByApartmentAsync_ReturnsId_WhenFound(string aptId, string expectedBldId)
    {
        _fakeStore.BuildingIdToReturn = expectedBldId;

        var result = await _service.FindBuildingIdByApartmentAsync(aptId);

        Assert.Equal(expectedBldId, result);
    }

    [Fact]
    public async Task FindBuildingIdByApartmentAsync_ReturnsNull_WhenNotFound()
    {
        _fakeStore.BuildingIdToReturn = null;

        var result = await _service.FindBuildingIdByApartmentAsync("ghost");

        Assert.Null(result);
    }

    // ================================================================
    // GetByIdAsync
    // ================================================================

    [Fact]
    public async Task GetByIdAsync_ReturnsBuilding_WhenExists()
    {
        _fakeStore.BuildingToReturn = new Building { Id = "b1", Name = "Tower" };

        var result = await _service.GetByIdAsync("b1");

        Assert.NotNull(result);
        Assert.Equal("b1", result.Id);
    }
}