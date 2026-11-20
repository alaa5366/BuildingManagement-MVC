using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
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
    public async Task SaveSettingsAsync_InvalidFrequency_IsRejected()
    {
        var settings = new ScheduledBackupSettings { Frequency = "hourly" };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SaveSettingsAsync(settings, "admin"));
    }

    [Theory]
    [InlineData("weekly", -1, null)]
    [InlineData("weekly", 7, null)]
    [InlineData("monthly", null, 0)]
    [InlineData("monthly", null, 32)]
    public async Task SaveSettingsAsync_InvalidScheduleDay_IsRejected(
        string frequency, int? dayOfWeek, int? dayOfMonth)
    {
        var settings = new ScheduledBackupSettings
        {
            Frequency = frequency,
            DayOfWeek = dayOfWeek,
            DayOfMonth = dayOfMonth
        };

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
        var saved = await db.BackupScheduleConfigurations.SingleAsync();
        Assert.Equal("weekly", saved.Frequency);
        Assert.Equal(3, saved.ScheduledHour);
        Assert.Equal(5, saved.MaxBackupsToKeep);
        Assert.Equal(new[] { "buildings" },
            System.Text.Json.JsonSerializer.Deserialize<List<string>>(saved.CollectionsJson));
    }

    [Fact]
    public async Task GetSettingsAsync_MapsLegacyCollectionsToTargetSchemaCollections()
    {
        var legacyCollections = new[]
        {
            "globalSettings", "scheduledBackupSettings", "users", "buildings",
            "buildingSettings", "buildingAdmins", "floors", "apartments", "residents",
            "financialCategories", "expenses", "revenues", "monthlyApartmentShares",
            "deposits", "depositEditHistory", "walletAdjustments", "userPermissions",
            "auditLog", "notifications", "polls", "pollOptions", "pollVotes",
            "maintenanceRecords", "dvrs", "cameras", "qrTokens", "qrUsages"
        };

        await using (var db = new AppDbContext(_options))
        {
            db.BackupScheduleConfigurations.Add(new BackupScheduleConfigurationEntity
            {
                ConfigurationId = Guid.NewGuid(),
                LegacySettingId = "default",
                IsEnabled = true,
                Frequency = "daily",
                ScheduledHour = 4,
                CollectionsJson = System.Text.Json.JsonSerializer.Serialize(legacyCollections),
                MaxBackupsToKeep = 7,
                ImportedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var settings = await _service.GetSettingsAsync();
        var knownCollections = BackupService.GetKnownCollections();

        Assert.NotEmpty(settings.Collections);
        Assert.All(settings.Collections, collection => Assert.Contains(collection, knownCollections.Keys));
        Assert.Contains("globalApplicationSettings", settings.Collections);
        Assert.Contains("backupScheduleConfigurations", settings.Collections);
        Assert.Contains("persons", settings.Collections);
        Assert.Contains("userAccounts", settings.Collections);
        Assert.Contains("paymentDeposits", settings.Collections);
        Assert.Contains("ledgerTransactions", settings.Collections);
        Assert.Contains("notificationMessages", settings.Collections);
        Assert.Contains("pollDefinitions", settings.Collections);
        Assert.Contains("maintenanceRequests", settings.Collections);
        Assert.Contains("mediaDevices", settings.Collections);
        Assert.Contains("qrCapabilities", settings.Collections);
        Assert.DoesNotContain("scheduledBackupSettings", settings.Collections);
        Assert.Equal(settings.Collections.Count, settings.Collections.Distinct(StringComparer.Ordinal).Count());
    }
}
