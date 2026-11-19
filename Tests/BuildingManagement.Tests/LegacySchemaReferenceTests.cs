using System.Text.RegularExpressions;
using Xunit;

namespace BuildingManagement.Tests;

/// <summary>Guards the application model against reintroducing tables retired by the target-schema cutover.</summary>
public sealed class LegacySchemaReferenceTests
{
    [Fact]
    public void AppDbContext_DoesNotMapRetiredTablesOrExposeLegacyDbSets()
    {
        var root = FindRepositoryRoot();
        Assert.NotNull(root);
        var source = File.ReadAllText(Path.Combine(root!, "Data", "AppDbContext.cs"));

        var retiredTables = new[]
        {
            "Users", "UserPermissions", "BuildingAdmins", "Expenses", "Revenues",
            "MonthlyApartmentShares", "Deposits", "DepositEditHistory", "WalletAdjustments",
            "AuditLog", "Notifications", "PollVotes", "MaintenanceRecords", "QrTokens",
            "QrUsages", "Presence", "GlobalSettings", "ScheduledBackupSettings",
            "BackupHistory", "SyncPendingChanges", "Residents", "Dvrs", "Cameras"
        };

        foreach (var table in retiredTables)
        {
            Assert.DoesNotMatch(
                new Regex($@"DbSet\s*<[^>]+>\s+{Regex.Escape(table)}\b", RegexOptions.IgnoreCase),
                source);
            Assert.DoesNotMatch(
                new Regex($@"ToTable\s*\(\s*""{Regex.Escape(table)}""", RegexOptions.IgnoreCase),
                source);
        }
    }

    [Fact]
    public void LegacySqlAuthenticationAndStoreImplementationsAreNotPresent()
    {
        var root = FindRepositoryRoot();
        Assert.NotNull(root);
        foreach (var relativePath in new[]
        {
            Path.Combine("Services", "SqlAuthService.cs"),
            Path.Combine("Services", "UsersService.cs"),
            Path.Combine("Data", "UserSqlStore.cs"),
            Path.Combine("Data", "UserSqlMapper.cs"),
            Path.Combine("Data", "ResidentSqlStore.cs")
        })
        {
            Assert.False(File.Exists(Path.Combine(root!, relativePath)), relativePath);
        }
    }

    private static string? FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "BuildingManagementMVC.csproj")))
            directory = directory.Parent;
        return directory?.FullName;
    }
}
