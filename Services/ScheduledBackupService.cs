using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class ScheduledBackupService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly BackupService _backup;
    private readonly IAuditLogger _audit;
    private readonly ILogger<ScheduledBackupService> _logger;

    private const string SettingsId = "default";

    public static string GetBackupsFolder(IWebHostEnvironment env)
    {
        var projectRoot = string.IsNullOrEmpty(env.WebRootPath)
            ? env.ContentRootPath
            : Path.GetFullPath(Path.Combine(env.WebRootPath, ".."));

        var path = Path.Combine(projectRoot, "Backups");

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return path;
    }

    public ScheduledBackupService(
        IDbContextFactory<AppDbContext> sqlFactory,
        BackupService backup,
        IAuditLogger audit,
        ILogger<ScheduledBackupService> logger)
    {
        _sqlFactory = sqlFactory;
        _backup = backup;
        _audit = audit;
        _logger = logger;
    }

    public async Task<ScheduledBackupSettings> GetSettingsAsync()
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var entity = await db.ScheduledBackupSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == SettingsId);

        if (entity == null) return new ScheduledBackupSettings();

        return new ScheduledBackupSettings
        {
            Id = entity.Id,
            Enabled = entity.Enabled,
            Frequency = entity.Frequency,
            Hour = entity.Hour,
            DayOfWeek = entity.DayOfWeek,
            DayOfMonth = entity.DayOfMonth,
            Collections = System.Text.Json.JsonSerializer.Deserialize<List<string>>(entity.CollectionsJson) ?? new(),
            MaxBackupsToKeep = entity.MaxBackupsToKeep,
            LastRunAt = entity.LastRunAt?.ToString("o"),
            LastRunStatus = entity.LastRunStatus,
            UpdatedAt = entity.UpdatedAt?.ToString("o"),
            UpdatedBy = entity.UpdatedBy
        };
    }

    public async Task SaveSettingsAsync(ScheduledBackupSettings settings, string userId)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var entity = await db.ScheduledBackupSettings.FirstOrDefaultAsync(x => x.Id == SettingsId);
        if (entity == null)
        {
            entity = new Data.Entities.ScheduledBackupSettingsEntity { Id = SettingsId };
            db.ScheduledBackupSettings.Add(entity);
        }

        entity.Enabled = settings.Enabled;
        entity.Frequency = settings.Frequency ?? "daily";
        entity.Hour = settings.Hour;
        entity.DayOfWeek = settings.DayOfWeek;
        entity.DayOfMonth = settings.DayOfMonth;
        entity.CollectionsJson = System.Text.Json.JsonSerializer.Serialize(settings.Collections ?? new());
        entity.MaxBackupsToKeep = settings.MaxBackupsToKeep;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;

        await db.SaveChangesAsync();

        await _audit.LogAsync(
            action: "backup.settings_update",
            userId: userId,
            userRole: "superadmin",
            metadata: new
            {
                enabled = settings.Enabled,
                frequency = settings.Frequency,
                hour = settings.Hour,
                collections = settings.Collections
            },
            severity: "warning");
    }

    public async Task<bool> ShouldRunAsync()
    {
        var settings = await GetSettingsAsync();
        if (!settings.Enabled) return false;
        if (string.IsNullOrEmpty(settings.LastRunAt)) return true;
        if (!DateTime.TryParse(settings.LastRunAt, out var lastRun)) return true;

        var now = DateTime.UtcNow;

        switch (settings.Frequency)
        {
            case "daily":
                var todayTarget = new DateTime(now.Year, now.Month, now.Day, settings.Hour, 0, 0, DateTimeKind.Utc);
                if (lastRun.Date == now.Date) return false;
                return now >= todayTarget;

            case "weekly":
                if (!settings.DayOfWeek.HasValue) return false;
                if ((int)now.DayOfWeek != settings.DayOfWeek.Value) return false;
                var weekTarget = new DateTime(now.Year, now.Month, now.Day, settings.Hour, 0, 0, DateTimeKind.Utc);
                if (lastRun.Date == now.Date) return false;
                return now >= weekTarget;

            case "monthly":
                if (!settings.DayOfMonth.HasValue) return false;
                if (now.Day != settings.DayOfMonth.Value) return false;
                var monthTarget = new DateTime(now.Year, now.Month, now.Day, settings.Hour, 0, 0, DateTimeKind.Utc);
                if (lastRun.Date == now.Date) return false;
                return now >= monthTarget;

            default:
                return false;
        }
    }

    public async Task RunScheduledBackupAsync(IWebHostEnvironment env, bool forceRun = false)
    {
        var settings = await GetSettingsAsync();
        if (!settings.Enabled && !forceRun) return;
        if (settings.Collections.Count == 0) return;

        _logger.LogInformation("[ScheduledBackup] Running...");

        try
        {
            var (bytes, result) = await _backup.CreateBackupAsync(settings.Collections, "system-scheduler");

            var folder = GetBackupsFolder(env);
            var fileName = $"scheduled-{DateTime.UtcNow:yyyy-MM-dd-HHmm}.zip";
            var fullPath = Path.Combine(folder, fileName);
            await File.WriteAllBytesAsync(fullPath, bytes);

            await using (var db = await _sqlFactory.CreateDbContextAsync())
            {
                db.BackupHistory.Add(new Data.Entities.BackupHistoryEntity
                {
                    Id = Guid.NewGuid().ToString("N"),
                    FileName = fileName,
                    FileSize = bytes.Length,
                    CollectionsJson = System.Text.Json.JsonSerializer.Serialize(result.Collections),
                    TotalDocuments = result.TotalDocuments,
                    CreatedAt = DateTime.UtcNow,
                    DurationSeconds = result.Duration.TotalSeconds,
                    Status = "success",
                    Source = "scheduled"
                });
                await db.SaveChangesAsync();
            }

            settings.LastRunAt = DateTime.UtcNow.ToString("o");
            settings.LastRunStatus = "success";
            await SaveSettingsInternalAsync(settings);

            _logger.LogInformation($"[ScheduledBackup] Done: {fileName}");

            await RotateBackupsAsync(env, settings.MaxBackupsToKeep);

            await _audit.LogAsync(
                action: "backup.scheduled_run",
                userId: "system-scheduler",
                userRole: "system",
                metadata: new { fileName, fileSize = bytes.Length },
                severity: "info");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ScheduledBackup] Failed");

            settings.LastRunAt = DateTime.UtcNow.ToString("o");
            settings.LastRunStatus = "failed";
            await SaveSettingsInternalAsync(settings);

            await _audit.LogAsync(
                action: "backup.scheduled_failed",
                userId: "system-scheduler",
                userRole: "system",
                metadata: new { error = ex.Message },
                severity: "critical");
        }
    }

    private async Task SaveSettingsInternalAsync(ScheduledBackupSettings settings)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var entity = await db.ScheduledBackupSettings.FirstOrDefaultAsync(x => x.Id == SettingsId);
        if (entity != null)
        {
            entity.LastRunAt = string.IsNullOrEmpty(settings.LastRunAt) ? null : DateTime.Parse(settings.LastRunAt);
            entity.LastRunStatus = settings.LastRunStatus;
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<BackupHistoryEntry>> GetHistoryAsync(IWebHostEnvironment env, int limit = 50)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var list = await db.BackupHistory.AsNoTracking()
            .OrderByDescending(h => h.CreatedAt)
            .Take(limit)
            .ToListAsync();

        var folder = GetBackupsFolder(env);

        return list.Select(e => new BackupHistoryEntry
        {
            Id = e.Id,
            FileName = e.FileName,
            FileSize = e.FileSize,
            Collections = System.Text.Json.JsonSerializer.Deserialize<List<string>>(e.CollectionsJson) ?? new(),
            TotalDocuments = e.TotalDocuments,
            CreatedAt = e.CreatedAt,
            DurationSeconds = e.DurationSeconds,
            Status = e.Status,
            Source = e.Source,
            ErrorMessage = e.ErrorMessage,
            FileExists = File.Exists(Path.Combine(folder, e.FileName))
        }).ToList();
    }

    private async Task RotateBackupsAsync(IWebHostEnvironment env, int keep)
    {
        var folder = GetBackupsFolder(env);
        var files = Directory.GetFiles(folder, "scheduled-*.zip")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToList();

        if (files.Count <= keep) return;

        foreach (var file in files.Skip(keep))
        {
            try { file.Delete(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete {File}", file.Name); }
        }

        await Task.CompletedTask;
    }

    public Task<string?> GetBackupFilePathAsync(IWebHostEnvironment env, string fileName)
    {
        if (string.IsNullOrEmpty(fileName) || fileName.Contains("..") ||
            fileName.Contains("/") || fileName.Contains("\\"))
            return Task.FromResult<string?>(null);

        var path = Path.Combine(GetBackupsFolder(env), fileName);
        return Task.FromResult(File.Exists(path) ? path : null);
    }

    public async Task<bool> DeleteBackupAsync(IWebHostEnvironment env, string fileName, string userId)
    {
        try
        {
            var path = await GetBackupFilePathAsync(env, fileName);
            if (path != null && File.Exists(path)) File.Delete(path);

            await using var db = await _sqlFactory.CreateDbContextAsync();
            await db.BackupHistory.Where(h => h.FileName == fileName).ExecuteDeleteAsync();

            await _audit.LogAsync(
                action: "backup.scheduled_delete",
                userId: userId,
                userRole: "superadmin",
                metadata: new { fileName },
                severity: "warning");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteBackup failed: {FileName}", fileName);
            return false;
        }
    }

    public List<(string FileName, long FileSize, DateTime CreatedAt)> ListBackupFiles(IWebHostEnvironment env)
    {
        var folder = GetBackupsFolder(env);
        return Directory.GetFiles(folder, "*.zip")
            .Select(f => new FileInfo(f))
            .Select(f => (FileName: f.Name, FileSize: f.Length, CreatedAt: f.CreationTimeUtc))
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
    }
}