using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BuildingManagement.Tests;

public class BackupServiceTests
{
    private readonly BackupService _service;
    private readonly Mock<IAuditLogger> _mockAudit;
    private readonly AppDbContext _db;

    public BackupServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);

        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => new AppDbContext(options));

        _mockAudit = new Mock<IAuditLogger>();
        _service = new BackupService(factory.Object, _mockAudit.Object, NullLogger<BackupService>.Instance);
    }

    // ================================================================
    // GetStatsAsync
    // ================================================================

    [Fact]
    public async Task GetStatsAsync_EmptyDb_ReturnsStatsForAllTables()
    {
        var stats = await _service.GetStatsAsync();

        Assert.NotNull(stats);
        Assert.NotEmpty(stats);
        // لازم يكون فيه stats لكل جدول في KnownTables
        Assert.Contains(stats, s => s.Collection == "buildings");
        Assert.Contains(stats, s => s.Collection == "users");
        Assert.All(stats, s => Assert.Equal(0, s.DocCount));
    }

    [Fact]
    public async Task GetStatsAsync_WithData_ReturnsCorrectCounts()
    {
        // Arrange: نضيف بيانات
        _db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "Test", BuildingNumber = "BLD-001" });
        _db.Users.Add(new UserEntity { Uid = "u1", Email = "a@test.com", Role = "admin" });
        await _db.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        var buildingsStat = stats.FirstOrDefault(s => s.Collection == "buildings");
        Assert.NotNull(buildingsStat);
        Assert.Equal(1, buildingsStat.DocCount);

        var usersStat = stats.FirstOrDefault(s => s.Collection == "users");
        Assert.NotNull(usersStat);
        Assert.Equal(1, usersStat.DocCount);
    }

    // ================================================================
    // CreateBackupAsync
    // ================================================================

    [Fact]
    public async Task CreateBackupAsync_EmptyDb_ReturnsValidZip()
    {
        var (bytes, result) = await _service.CreateBackupAsync(new() { "buildings" }, "admin");

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);
        Assert.True(result.Success);
        Assert.Equal(1, result.TotalCollections);
        Assert.Equal(0, result.TotalDocuments);

        // الزيبي لازم يكون صالح
        using var ms = new MemoryStream(bytes);
        using var zip = new System.IO.Compression.ZipArchive(ms);
        Assert.Contains(zip.Entries, e => e.Name == "buildings.json");
        Assert.Contains(zip.Entries, e => e.Name == "README.md");
    }

    [Fact]
    public async Task CreateBackupAsync_WithData_IncludesAllRows()
    {
        // Arrange
        _db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "T1", BuildingNumber = "BLD-001" });
        _db.Buildings.Add(new BuildingEntity { Id = "b2", Name = "T2", BuildingNumber = "BLD-002" });
        await _db.SaveChangesAsync();

        // Act
        var (bytes, result) = await _service.CreateBackupAsync(new() { "buildings" }, "admin");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.TotalDocuments);

        // نتأكد إن الـ JSON فيه البيانات
        using var ms = new MemoryStream(bytes);
        using var zip = new System.IO.Compression.ZipArchive(ms);
        var entry = zip.GetEntry("buildings.json");
        Assert.NotNull(entry);

        using var reader = new StreamReader(entry.Open());
        var json = await reader.ReadToEndAsync();
        Assert.Contains("BLD-001", json);
        Assert.Contains("BLD-002", json);
    }

    [Fact]
    public async Task CreateBackupAsync_MultipleTables_BacksUpAll()
    {
        _db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "T", BuildingNumber = "BLD-001" });
        _db.Users.Add(new UserEntity { Uid = "u1", Email = "a@test.com", Role = "admin" });
        await _db.SaveChangesAsync();

        var (bytes, result) = await _service.CreateBackupAsync(new() { "buildings", "users" }, "admin");

        Assert.True(result.Success);
        Assert.Equal(2, result.TotalCollections);
        Assert.Equal(2, result.TotalDocuments);

        using var ms = new MemoryStream(bytes);
        using var zip = new System.IO.Compression.ZipArchive(ms);
        Assert.Contains(zip.Entries, e => e.Name == "buildings.json");
        Assert.Contains(zip.Entries, e => e.Name == "users.json");
    }

    [Fact]
    public async Task CreateBackupAsync_UnknownTable_SkipsIt()
    {
        var (_, result) = await _service.CreateBackupAsync(new() { "unknown_table" }, "admin");

        Assert.True(result.Success);
        Assert.Equal(0, result.TotalCollections);
    }

    // ================================================================
    // PreviewRestoreAsync
    // ================================================================

    [Fact]
    public async Task PreviewRestoreAsync_ValidZip_ReturnsPreview()
    {
        // Arrange: نعمل backup الأول
        _db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "T", BuildingNumber = "BLD-001" });
        await _db.SaveChangesAsync();

        var (bytes, _) = await _service.CreateBackupAsync(new() { "buildings" }, "admin");
        using var ms = new MemoryStream(bytes);

        // Act
        var preview = await _service.PreviewRestoreAsync(ms, "test.zip", "admin");

        // Assert
        Assert.True(preview.Success);
        Assert.Single(preview.Collections);
        Assert.Equal("buildings", preview.Collections[0].Collection);
        Assert.Equal(1, preview.Collections[0].DocCount);
        Assert.NotNull(preview.TempFilePath);
        Assert.True(File.Exists(preview.TempFilePath));

        // تنظيف
        if (preview.TempFilePath != null) File.Delete(preview.TempFilePath);
    }

    [Fact]
    public async Task PreviewRestoreAsync_InvalidZip_ReturnsError()
    {
        using var ms = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        var preview = await _service.PreviewRestoreAsync(ms, "bad.zip", "admin");

        Assert.False(preview.Success);
        Assert.NotEmpty(preview.Errors);
    }

    // ================================================================
    // RestoreAsync — الحالي (skeleton، مش بيعمل restore فعلي)
    // ================================================================

    [Fact]
    public async Task RestoreAsync_ValidZip_RestoresBuildings()
    {
        // Arrange
        _db.Buildings.Add(new BuildingManagementMvc.Data.Entities.BuildingEntity
        {
            Id = "b1",
            Name = "T",
            BuildingNumber = "BLD-001"
        });
        await _db.SaveChangesAsync();

        var (bytes, backupResult) = await _service.CreateBackupAsync(new() { "buildings" }, "admin");
        Assert.True(backupResult.Success, "Backup failed: " + string.Join(", ", backupResult.Errors));
        Assert.Equal(1, backupResult.TotalCollections);

        var tempPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid():N}.zip");
        await File.WriteAllBytesAsync(tempPath, bytes);

        try
        {
            // Act
            var result = await _service.RestoreAsync(tempPath, new() { "buildings" }, overwrite: false, "admin");

            // Assert — الـ Restore لازم يشتغل
            Assert.True(result.Success, "Restore failed: " + string.Join(", ", result.Errors));
            Assert.Equal(1, result.TotalCollections);
            Assert.Contains(result.Details, d => d.Contains("buildings"));
            // الـ b1 موجود بالفعل، فمفروض يتم تخطيه
            Assert.Equal(0, result.Restored);
            Assert.Equal(1, result.Skipped);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    // ================================================================
    // KnownTables
    // ================================================================

    [Fact]
    public void GetKnownCollections_ReturnsAllKnownTables()
    {
        var collections = BackupService.GetKnownCollections();

        Assert.Contains("buildings", collections.Keys);
        Assert.Contains("users", collections.Keys);
        Assert.Contains("residents", collections.Keys);
        Assert.Contains("auditLog", collections.Keys);
    }
}