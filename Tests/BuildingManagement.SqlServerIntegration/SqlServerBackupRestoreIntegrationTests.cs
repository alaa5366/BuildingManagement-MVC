using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace BuildingManagement.SqlServerIntegration;

/// <summary>
/// Real SQL Server integration checks. Set BM_SQLSERVER_TEST_CONNECTION to an isolated,
/// migration-updated test database; otherwise xUnit reports these tests as skipped.
/// </summary>
[Trait("Category", "SqlServerIntegration")]
public sealed class SqlServerBackupRestoreIntegrationTests
{
    private sealed class SqlServerContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public SqlServerContextFactory(string connectionString)
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString, options => options.CommandTimeout(60))
                .Options;
        }

        public AppDbContext CreateDbContext() => new(_options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private static SqlServerContextFactory CreateFactory()
    {
        var connectionString = Environment.GetEnvironmentVariable("BM_SQLSERVER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Set BM_SQLSERVER_TEST_CONNECTION to an isolated, migration-updated SQL Server database.");
        return new SqlServerContextFactory(connectionString);
    }

    private static BackupService CreateService(IDbContextFactory<AppDbContext> factory)
    {
        var audit = new Mock<IAuditLogger>();
        audit.Setup(item => item.LogAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return new BackupService(factory, audit.Object, NullLogger<BackupService>.Instance);
    }

    private static string CreateRestorePath(byte[] bytes)
    {
        var path = Path.Combine(Path.GetTempPath(), $"restore-{Guid.NewGuid():N}.zip");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static async Task DeleteBuildingGraphAsync(IDbContextFactory<AppDbContext> factory, string buildingId)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.Apartments.RemoveRange(db.Apartments.Where(item => item.BuildingId == buildingId));
        db.Floors.RemoveRange(db.Floors.Where(item => item.BuildingId == buildingId));
        db.Buildings.RemoveRange(db.Buildings.Where(item => item.Id == buildingId));
        await db.SaveChangesAsync();
    }

    [SqlServerFact]
    public async Task SqlServer_backup_and_restore_round_trip_persists_a_missing_building()
    {
        var factory = CreateFactory();
        var service = CreateService(factory);
        var buildingId = $"qa-{Guid.NewGuid():N}";
        var buildingNumber = $"QA-{Guid.NewGuid():N}";

        try
        {
            await using (var db = await factory.CreateDbContextAsync())
            {
                db.Buildings.Add(new BuildingEntity
                {
                    Id = buildingId,
                    BuildingNumber = buildingNumber,
                    Name = "SQL restore integration fixture"
                });
                await db.SaveChangesAsync();
            }

            var (bytes, backup) = await service.CreateBackupAsync(new() { "buildings" }, "qa-integration");
            Assert.True(backup.Success, string.Join("; ", backup.Errors));
            Assert.True(bytes.Length > 0);

            await DeleteBuildingGraphAsync(factory, buildingId);
            var path = CreateRestorePath(bytes);
            var restore = await service.RestoreAsync(path, new() { "buildings" }, overwrite: false, "qa-integration");

            Assert.True(restore.Success, string.Join("; ", restore.Errors));
            Assert.Equal(1, restore.Restored);
            await using var verify = await factory.CreateDbContextAsync();
            var restored = await verify.Buildings.SingleAsync(item => item.Id == buildingId);
            Assert.Equal("SQL restore integration fixture", restored.Name);
        }
        finally
        {
            await DeleteBuildingGraphAsync(factory, buildingId);
        }
    }

    [SqlServerFact]
    public async Task SqlServer_restore_rolls_back_earlier_collections_when_a_later_foreign_key_fails()
    {
        var factory = CreateFactory();
        var service = CreateService(factory);
        var buildingId = $"qa-{Guid.NewGuid():N}";
        var floorId = $"floor-{Guid.NewGuid():N}";
        var apartmentId = $"apt-{Guid.NewGuid():N}";

        try
        {
            await using (var db = await factory.CreateDbContextAsync())
            {
                db.Buildings.Add(new BuildingEntity
                {
                    Id = buildingId,
                    BuildingNumber = $"QA-{Guid.NewGuid():N}",
                    Name = "SQL rollback integration fixture"
                });
                db.Floors.Add(new FloorEntity { BuildingId = buildingId, Id = floorId, Label = "QA floor" });
                db.Apartments.Add(new ApartmentEntity
                {
                    BuildingId = buildingId, Id = apartmentId, FloorId = floorId, Number = 1
                });
                await db.SaveChangesAsync();
            }

            var (bytes, backup) = await service.CreateBackupAsync(
                new() { "buildings", "floors", "apartments" }, "qa-integration");
            Assert.True(backup.Success, string.Join("; ", backup.Errors));

            var corrupted = await ReplaceApartmentBuildingIdAsync(bytes, "missing-" + Guid.NewGuid().ToString("N"));
            await DeleteBuildingGraphAsync(factory, buildingId);

            var path = CreateRestorePath(corrupted);
            var restore = await service.RestoreAsync(
                path, new() { "buildings", "floors", "apartments" }, overwrite: false, "qa-integration");

            Assert.False(restore.Success);
            Assert.Equal(0, restore.Restored);
            Assert.Equal(0, restore.Skipped);
            Assert.Contains(restore.Details, item => item.Contains("Rolled back", StringComparison.OrdinalIgnoreCase));
            Assert.All(
                restore.Details.Where(item => item.Contains("restored", StringComparison.OrdinalIgnoreCase)),
                item => Assert.StartsWith("Not committed:", item, StringComparison.Ordinal));

            await using var verify = await factory.CreateDbContextAsync();
            Assert.False(await verify.Buildings.AnyAsync(item => item.Id == buildingId));
            Assert.False(await verify.Floors.AnyAsync(item => item.BuildingId == buildingId && item.Id == floorId));
            Assert.False(await verify.Apartments.AnyAsync(item => item.BuildingId == buildingId && item.Id == apartmentId));
        }
        finally
        {
            await DeleteBuildingGraphAsync(factory, buildingId);
        }
    }

    private static async Task<byte[]> ReplaceApartmentBuildingIdAsync(byte[] bytes, string replacementBuildingId)
    {
        using var input = new MemoryStream(bytes);
        using var source = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
        using var output = new MemoryStream();
        using (var target = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in source.Entries)
            {
                var targetEntry = target.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                await using var targetStream = targetEntry.Open();
                await using var sourceStream = entry.Open();
                if (string.Equals(entry.FullName, "apartments.json", StringComparison.Ordinal))
                {
                    using var reader = new StreamReader(sourceStream, Encoding.UTF8);
                    var json = await reader.ReadToEndAsync();
                    var rows = JsonNode.Parse(json)!.AsArray();
                    Assert.NotEmpty(rows);
                    rows[0]!["data"]!["BuildingId"] = replacementBuildingId;
                    var changed = Encoding.UTF8.GetBytes(rows.ToJsonString());
                    await targetStream.WriteAsync(changed);
                }
                else
                {
                    await sourceStream.CopyToAsync(targetStream);
                }
            }
        }
        return output.ToArray();
    }
}
