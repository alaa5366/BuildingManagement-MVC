using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Creates scheduled backups, writes them encrypted to disk and applies the retention policy.
/// </summary>
public class ScheduledBackupService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly BackupService _backup;
    private readonly IAuditLogger _audit;
    private readonly ILogger<ScheduledBackupService> _logger;

    private const string SettingsId = "default";

    /// <summary>
    /// Gets the backups folder.
    /// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledBackupService"/> class.
    /// </summary>
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

    /// <summary>
    /// Gets the settings.
    /// </summary>
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

    /// <summary>
    /// Saves the settings.
    /// </summary>
    public async Task SaveSettingsAsync(ScheduledBackupSettings settings, string userId)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var frequency = (settings.Frequency ?? "daily").Trim().ToLowerInvariant();
        if (frequency is not ("daily" or "weekly" or "monthly"))
            throw new ArgumentException("Backup frequency is invalid.", nameof(settings));
        if (settings.Hour is < 0 or > 23)
            throw new ArgumentOutOfRangeException(nameof(settings.Hour), "Backup hour must be between 0 and 23.");
        if (frequency == "weekly" && (settings.DayOfWeek is null or < 0 or > 6))
            throw new ArgumentOutOfRangeException(nameof(settings.DayOfWeek), "Weekly backup day must be between 0 and 6.");
        if (frequency == "monthly" && (settings.DayOfMonth is null or < 1 or > 31))
            throw new ArgumentOutOfRangeException(nameof(settings.DayOfMonth), "Monthly backup day must be between 1 and 31.");
        if (settings.MaxBackupsToKeep is < 1 or > 30)
            throw new ArgumentOutOfRangeException(nameof(settings.MaxBackupsToKeep), "Backup retention must be between 1 and 30.");

        var collections = (settings.Collections ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var knownCollections = BackupService.GetKnownCollections();
        if (collections.Any(x => !knownCollections.ContainsKey(x)))
            throw new ArgumentException("One or more backup collections are invalid.", nameof(settings));
        if (settings.Enabled && collections.Count == 0)
            throw new ArgumentException("At least one backup collection is required when scheduling is enabled.", nameof(settings));

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var entity = await db.ScheduledBackupSettings.FirstOrDefaultAsync(x => x.Id == SettingsId);
        if (entity == null)
        {
            entity = new Data.Entities.ScheduledBackupSettingsEntity { Id = SettingsId };
            db.ScheduledBackupSettings.Add(entity);
        }

        entity.Enabled = settings.Enabled;
        entity.Frequency = frequency;
        entity.Hour = settings.Hour;
        entity.DayOfWeek = settings.DayOfWeek;
        entity.DayOfMonth = settings.DayOfMonth;
        entity.CollectionsJson = System.Text.Json.JsonSerializer.Serialize(collections);
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
                collections
            },
            severity: "warning");
    }

    /// <summary>
    /// Determines whether the value should run.
    /// </summary>
    public async Task<bool> ShouldRunAsync()
    {
        var settings = await GetSettingsAsync();
        if (!settings.Enabled || settings.Hour is < 0 or > 23 ||
            settings.MaxBackupsToKeep is < 1 or > 30)
            return false;
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
                if (settings.DayOfWeek is null or < 0 or > 6) return false;
                if ((int)now.DayOfWeek != settings.DayOfWeek.Value) return false;
                var weekTarget = new DateTime(now.Year, now.Month, now.Day, settings.Hour, 0, 0, DateTimeKind.Utc);
                if (lastRun.Date == now.Date) return false;
                return now >= weekTarget;

            case "monthly":
                if (settings.DayOfMonth is null or < 1 or > 31) return false;
                if (now.Day != settings.DayOfMonth.Value) return false;
                var monthTarget = new DateTime(now.Year, now.Month, now.Day, settings.Hour, 0, 0, DateTimeKind.Utc);
                if (lastRun.Date == now.Date) return false;
                return now >= monthTarget;

            default:
                return false;
        }
    }

    /// <summary>
    /// Runs the scheduled backup.
    /// </summary>
    public async Task RunScheduledBackupAsync(IWebHostEnvironment env, bool forceRun = false)
    {
        var settings = await GetSettingsAsync();
        if (!settings.Enabled && !forceRun) return;
        if (settings.Collections.Count == 0) return;

        _logger.LogInformation("[ScheduledBackup] Running...");

        try
        {
            var collections = settings.Collections.Distinct(StringComparer.Ordinal).ToList();
            var knownCollections = BackupService.GetKnownCollections();
            if (collections.Any(x => !knownCollections.ContainsKey(x)))
                throw new InvalidOperationException("Scheduled backup contains an unknown collection.");

            var (bytes, result) = await _backup.CreateBackupAsync(collections, "system-scheduler");

            var folder = GetBackupsFolder(env);
            var fileName = $"scheduled-{DateTime.UtcNow:yyyy-MM-dd-HHmm}.zip";
            var fullPath = Path.Combine(folder, fileName);
            await File.WriteAllBytesAsync(fullPath, BackupFileProtector.Protect(bytes));   // مشفّر على القرص

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

    /// <summary>
    /// Gets the history.
    /// </summary>
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
        // Defensive clamp protects existing persisted settings as well as newly validated input.
        keep = Math.Clamp(keep, 1, 30);
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

    /// <summary>
    /// Gets the backup file path.
    /// </summary>
    public Task<string?> GetBackupFilePathAsync(IWebHostEnvironment env, string fileName)
    {
        if (string.IsNullOrEmpty(fileName) || fileName.Contains("..") ||
            fileName.Contains("/") || fileName.Contains("\\"))
            return Task.FromResult<string?>(null);

        var path = Path.Combine(GetBackupsFolder(env), fileName);
        return Task.FromResult(File.Exists(path) ? path : null);
    }

    /// <summary>
    /// Deletes the backup.
    /// </summary>
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

    /// <summary>
    /// Lists the backup files.
    /// </summary>
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