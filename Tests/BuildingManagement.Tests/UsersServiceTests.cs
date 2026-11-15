using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BuildingManagement.Tests;

public class UsersServiceTests
{
    private readonly Mock<UserSqlStore> _mockStore;
    private readonly UsersService _service;

    public UsersServiceTests()
    {
        _mockStore = new Mock<UserSqlStore>(
            new Mock<Microsoft.EntityFrameworkCore.IDbContextFactory<AppDbContext>>().Object,
            new Mock<ILogger<UserSqlStore>>().Object
        );

        _service = new UsersService(
            _mockStore.Object,
            NullLogger<UsersService>.Instance
        );
    }

    // ================================================================
    // SetAsync — منطق التحويل والتشفير
    // ================================================================

    [Fact]
    public async Task SetAsync_NewUser_CreatesUserWithAllFields()
    {
        // Arrange
        _mockStore.Setup(s => s.GetByUidAsync("u1", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((UserEntity?)null);

        var data = new Dictionary<string, object>
        {
            ["email"] = "test@test.com",
            ["name"] = "Ali",
            ["phone"] = "01001234567",
            ["role"] = "admin",
            ["isActive"] = true
        };

        // Act
        await _service.SetAsync("u1", data);

        // Assert
        _mockStore.Verify(s => s.UpsertAsync(
            It.Is<AppUserDoc>(d =>
                d.Uid == "u1" &&
                d.Email == "test@test.com" &&
                d.Name == "Ali" &&
                d.Role == "admin" &&
                d.IsActive == true
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SetAsync_PinPlainText_HashesIt()
    {
        // Arrange
        _mockStore.Setup(s => s.GetByUidAsync("u1", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((UserEntity?)null);

        var data = new Dictionary<string, object> { ["pin"] = "1234" };

        // Act
        await _service.SetAsync("u1", data);

        // Assert
        _mockStore.Verify(s => s.UpsertAsync(
            It.Is<AppUserDoc>(d => d.Pin.StartsWith("$2a$") || d.Pin.StartsWith("$2b$")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SetAsync_PinAlreadyHashed_DoesNotRehash()
    {
        // Arrange
        var hashedPin = SecretVerifier.Hash("1234");
        _mockStore.Setup(s => s.GetByUidAsync("u1", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((UserEntity?)null);

        var data = new Dictionary<string, object> { ["pin"] = hashedPin };

        // Act
        await _service.SetAsync("u1", data);

        // Assert
        _mockStore.Verify(s => s.UpsertAsync(
            It.Is<AppUserDoc>(d => d.Pin == hashedPin), // نفس الـ hash، مفيش double-hash
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SetAsync_ExistingUser_PreservesExistingFields()
    {
        // Arrange
        var existingEntity = new UserEntity
        {
            Uid = "u1",
            Email = "old@test.com",
            Name = "Old Name",
            Role = "admin"
        };
        _mockStore.Setup(s => s.GetByUidAsync("u1", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(existingEntity);

        var data = new Dictionary<string, object> { ["name"] = "New Name" };

        // Act
        await _service.SetAsync("u1", data);

        // Assert
        _mockStore.Verify(s => s.UpsertAsync(
            It.Is<AppUserDoc>(d =>
                d.Email == "old@test.com" &&  // لسه زي ما هو
                d.Name == "New Name" &&       // اتحدث
                d.Role == "admin"              // لسه زي ما هو
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithPermissionsList_AssignsList()
    {
        // Arrange
        _mockStore.Setup(s => s.GetByUidAsync("u1", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((UserEntity?)null);

        var data = new Dictionary<string, object>
        {
            ["permissions"] = new List<string> { "buildings.manage", "wallet.manage" }
        };

        // Act
        await _service.SetAsync("u1", data);

        // Assert
        _mockStore.Verify(s => s.UpsertAsync(
            It.Is<AppUserDoc>(d =>
                d.Permissions.Count == 2 &&
                d.Permissions.Contains("buildings.manage")
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    // ================================================================
    // GetByUidAsync
    // ================================================================

    [Fact]
    public async Task GetByUidAsync_ExistingUser_ReturnsDoc()
    {
        // Arrange
        var entity = new UserEntity { Uid = "u1", Email = "test@test.com", Role = "admin" };
        _mockStore.Setup(s => s.GetByUidAsync("u1", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByUidAsync("u1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("u1", result.Uid);
        Assert.Equal("test@test.com", result.Email);
    }

    [Fact]
    public async Task GetByUidAsync_NonExistingUser_ReturnsNull()
    {
        // Arrange
        _mockStore.Setup(s => s.GetByUidAsync("ghost", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((UserEntity?)null);

        // Act
        var result = await _service.GetByUidAsync("ghost");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUidAsync_EmptyUid_ReturnsNull()
    {
        // Act
        var result = await _service.GetByUidAsync("");
        Assert.Null(result);

        // Assert: مفيش استدعاء للـ store
        _mockStore.Verify(s => s.GetByUidAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ================================================================
    // GetAllAsync / GetAdminsAsync / GetBuildingAdminsAsync
    // ================================================================

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsersAsDocs()
    {
        // Arrange
        var entities = new List<UserEntity>
        {
            new() { Uid = "u1", Email = "a@test.com", Role = "admin" },
            new() { Uid = "u2", Email = "b@test.com", Role = "superadmin" }
        };
        _mockStore.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(entities);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("u1", result[0].Uid);
        Assert.Equal("u2", result[1].Uid);
    }

    [Fact]
    public async Task GetAdminsAsync_ReturnsOnlyAdmins()
    {
        // Arrange
        var entities = new List<UserEntity>
        {
            new() { Uid = "u1", Role = "admin" },
            new() { Uid = "u2", Role = "superadmin" }
        };
        _mockStore.Setup(s => s.GetAdminsAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(entities);

        // Act
        var result = await _service.GetAdminsAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetBuildingAdminsAsync_ReturnsAdminsForBuilding()
    {
        // Arrange
        var entities = new List<UserEntity>
        {
            new() { Uid = "u1", Role = "admin" }
        };
        _mockStore.Setup(s => s.GetBuildingAdminsAsync("b1", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(entities);

        // Act
        var result = await _service.GetBuildingAdminsAsync("b1");

        // Assert
        Assert.Single(result);
    }

    // ================================================================
    // MirrorOneToSqlAsync
    // ================================================================

    [Fact]
    public async Task MirrorOneToSqlAsync_CallsStoreUpsert()
    {
        // Arrange
        var doc = new AppUserDoc { Uid = "u1", Email = "test@test.com" };
        _mockStore.Setup(s => s.UpsertAsync(doc, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);

        // Act
        var result = await _service.MirrorOneToSqlAsync(doc);

        // Assert
        Assert.True(result);
        _mockStore.Verify(s => s.UpsertAsync(doc, It.IsAny<CancellationToken>()), Times.Once);
    }
}