using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BuildingManagement.Tests;

public class ScheduledBackupServiceTests
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly ScheduledBackupService _service;

    public ScheduledBackupServiceTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(_options));

        var audit = new Mock<IAuditLogger>();
        var backup = new BackupService(factory.Object, audit.Object, NullLogger<BackupService>.Instance);
        _service = new ScheduledBackupService(
            factory.Object, backup, audit.Object, NullLogger<ScheduledBackupService>.Instance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(31)]
    public async Task SaveSettingsAsync_InvalidRetention_IsRejected(int keep)
    {
        var settings = new ScheduledBackupSettings { MaxBackupsToKeep = keep };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.SaveSettingsAsync(settings, "admin"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public async Task SaveSettingsAsync_InvalidHour_IsRejected(int hour)
    {
        var settings = new ScheduledBackupSettings { Hour = hour };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.SaveSettingsAsync(settings, "admin"));
    }

    [Fact]
    public async Task SaveSettingsAsync_UnknownCollection_IsRejected()
    {
        var settings = new ScheduledBackupSettings
        {
            Enabled = true,
            Collections = new List<string> { "buildings", "not-a-collection" }
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SaveSettingsAsync(settings, "admin"));
    }

    [Fact]
    public async Task SaveSettingsAsync_NormalizesFrequencyAndDeduplicatesCollections()
    {
        var settings = new ScheduledBackupSettings
        {
            Enabled = true,
            Frequency = "WEEKLY",
            Hour = 3,
            DayOfWeek = 2,
            MaxBackupsToKeep = 5,
            Collections = new List<string> { "buildings", "buildings" }
        };

        await _service.SaveSettingsAsync(settings, "admin");

        await using var db = new AppDbContext(_options);
        var saved = await db.ScheduledBackupSettings.SingleAsync();
        Assert.Equal("weekly", saved.Frequency);
        Assert.Equal(3, saved.Hour);
        Assert.Equal(5, saved.MaxBackupsToKeep);
        Assert.Equal(new[] { "buildings" },
            System.Text.Json.JsonSerializer.Deserialize<List<string>>(saved.CollectionsJson));
    }
}
