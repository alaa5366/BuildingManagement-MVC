using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BuildingManagement.Tests;

/// <summary>
/// Tests that a restore really writes to the database: rows are saved, "overwrite" updates rows in
/// place (without deleting related rows), identity columns are regenerated, and a restore file that is
/// not the preview's temporary file is rejected and left untouched.
/// </summary>
public class BackupRestorePersistenceTests
{
    private readonly BackupService _service;
    private readonly DbContextOptions<AppDbContext> _options;

    public BackupRestorePersistenceTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => new AppDbContext(_options));

        _service = new BackupService(factory.Object, new Mock<IAuditLogger>().Object, NullLogger<BackupService>.Instance);
    }

    private AppDbContext NewDb() => new(_options);

    // نعمل ملف باك أب بنفس اسم وشكل الملف المؤقت اللي بتعمله خطوة المعاينة
    private async Task<string> WriteBackupAsync(params string[] tables)
    {
        var (bytes, result) = await _service.CreateBackupAsync(tables.ToList(), "admin");
        Assert.True(result.Success, "Backup failed: " + string.Join(", ", result.Errors));

        var path = Path.Combine(Path.GetTempPath(), $"restore-{Guid.NewGuid():N}.zip");
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }

    [Fact]
    public async Task Missing_rows_are_inserted_and_saved()
    {
        await using (var db = NewDb())
        {
            db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "Nile Tower", BuildingNumber = "BLD-001" });
            await db.SaveChangesAsync();
        }

        var path = await WriteBackupAsync("buildings");

        await using (var db = NewDb())
        {
            db.Buildings.RemoveRange(db.Buildings);
            await db.SaveChangesAsync();
        }

        var result = await _service.RestoreAsync(path, new() { "buildings" }, overwrite: false, "admin");

        Assert.True(result.Success, string.Join(", ", result.Errors));
        Assert.Equal(1, result.Restored);

        await using var check = NewDb();
        var restored = await check.Buildings.SingleAsync();
        Assert.Equal("Nile Tower", restored.Name);
    }

    [Fact]
    public async Task Without_overwrite_existing_rows_are_kept_as_they_are()
    {
        await using (var db = NewDb())
        {
            db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "Old", BuildingNumber = "BLD-001" });
            await db.SaveChangesAsync();
        }

        var path = await WriteBackupAsync("buildings");

        await using (var db = NewDb())
        {
            (await db.Buildings.SingleAsync()).Name = "Changed";
            await db.SaveChangesAsync();
        }

        var result = await _service.RestoreAsync(path, new() { "buildings" }, overwrite: false, "admin");

        Assert.Equal(1, result.Skipped);
        await using var check = NewDb();
        Assert.Equal("Changed", (await check.Buildings.SingleAsync()).Name);
    }

    [Fact]
    public async Task Overwrite_updates_the_row_in_place_and_keeps_its_children()
    {
        await using (var db = NewDb())
        {
            db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "Old", BuildingNumber = "BLD-001" });
            db.Floors.Add(new FloorEntity { BuildingId = "b1", Id = "f1", Label = "Ground" });
            await db.SaveChangesAsync();
        }

        var path = await WriteBackupAsync("buildings");        // الأدوار مش جوه الباك أب

        await using (var db = NewDb())
        {
            (await db.Buildings.SingleAsync()).Name = "Changed";
            await db.SaveChangesAsync();
        }

        var result = await _service.RestoreAsync(path, new() { "buildings" }, overwrite: true, "admin");

        Assert.True(result.Success, string.Join(", ", result.Errors));
        Assert.Equal(1, result.Restored);

        await using var check = NewDb();
        Assert.Equal("Old", (await check.Buildings.SingleAsync()).Name);
        Assert.Equal(1, await check.Floors.CountAsync());      // الدور لسه موجود
    }

    [Fact]
    public async Task Audit_log_rows_get_new_identity_values()
    {
        await using (var db = NewDb())
        {
            db.AuditLog.Add(new AuditLogEntity { Action = "test", Label = "x", Ts = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        var path = await WriteBackupAsync("auditLog");

        var result = await _service.RestoreAsync(path, new() { "auditLog" }, overwrite: false, "admin");

        Assert.True(result.Success, string.Join(", ", result.Errors));
        await using var check = NewDb();
        Assert.Equal(2, await check.AuditLog.CountAsync());     // الأصلي + المستعاد (append-only)
    }

    [Fact]
    public async Task RestoreWithMalformedLaterCollection_DoesNotPersistEarlierCollection_InNonRelationalProvider()
    {
        await using (var db = NewDb())
        {
            db.Buildings.Add(new BuildingEntity { Id = "rollback-b1", Name = "Original", BuildingNumber = "BLD-992" });
            await db.SaveChangesAsync();
        }

        var (backupBytes, _) = await _service.CreateBackupAsync(new() { "buildings" }, "admin");
        using var source = new MemoryStream(backupBytes);
        using var output = new MemoryStream();
        using (var inputZip = new ZipArchive(source, ZipArchiveMode.Read, true))
        using (var outputZip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            foreach (var entry in inputZip.Entries)
            {
                var target = outputZip.CreateEntry(entry.FullName);
                using var from = entry.Open();
                using var to = target.Open();
                await from.CopyToAsync(to);
            }
            using var malformed = outputZip.CreateEntry("apartments.json").Open();
            var badJson = Encoding.UTF8.GetBytes("{");
            await malformed.WriteAsync(badJson);
        }

        await using (var db = NewDb())
        {
            db.Buildings.RemoveRange(db.Buildings);
            await db.SaveChangesAsync();
        }

        var path = Path.Combine(Path.GetTempPath(), $"restore-{Guid.NewGuid():N}.zip");
        await File.WriteAllBytesAsync(path, output.ToArray());
        var result = await _service.RestoreAsync(path, new() { "buildings", "apartments" }, overwrite: true, "admin");

        Assert.False(result.Success);
        await using var check = NewDb();
        Assert.Empty(await check.Buildings.ToListAsync());
    }

    [Fact]
    public async Task A_file_that_is_not_the_preview_temp_file_is_rejected_and_not_deleted()
    {
        var path = Path.Combine(Path.GetTempPath(), $"not-a-backup-{Guid.NewGuid():N}.zip");
        await File.WriteAllBytesAsync(path, new byte[] { 1, 2, 3 });

        try
        {
            var result = await _service.RestoreAsync(path, new() { "buildings" }, overwrite: true, "admin");

            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            Assert.True(File.Exists(path));                      // مامسحناش الملف
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("C:\\Windows\\win.ini")]
    [InlineData("/etc/passwd")]
    [InlineData("..\\..\\secret.zip")]
    public async Task Arbitrary_paths_are_rejected(string path)
    {
        var result = await _service.RestoreAsync(path, new() { "buildings" }, overwrite: true, "admin");

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }
}
