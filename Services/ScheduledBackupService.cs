using System.Security.Cryptography;
using System.Text.Json;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Scheduled backup orchestration using BackupScheduleConfigurations, BackupJobs and BackupArtifacts.</summary>
public sealed class ScheduledBackupService
{
    private const string SettingsId = "default";
    private const byte ScheduledBackupJobType = 1;
    private const byte BackupSucceeded = 1;
    private const byte BackupFailed = 2;
    private const string BackupEncryptionKeyVersion = "backup-file-protector-v1";

    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly BackupService _backup;
    private readonly IAuditLogger _audit;
    private readonly ILogger<ScheduledBackupService> _logger;

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

    /// <summary> Gets or previews data through GetBackupsFolder. </summary>
    public static string GetBackupsFolder(IWebHostEnvironment env)
    {
        var projectRoot = string.IsNullOrEmpty(env.WebRootPath)
            ? env.ContentRootPath
            : Path.GetFullPath(Path.Combine(env.WebRootPath, ".."));
        var path = Path.Combine(projectRoot, "Backups");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }

    /// <summary> Gets or previews data through GetSettingsAsync. </summary>
    public async Task<ScheduledBackupSettings> GetSettingsAsync()
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var entity = await db.BackupScheduleConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.LegacySettingId == SettingsId);
        if (entity is null) return new ScheduledBackupSettings { Id = SettingsId };
        return MapSettings(entity);
    }

    /// <summary> Creates or saves data through SaveSettingsAsync. </summary>
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
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal).ToList();
        var knownCollections = BackupService.GetKnownCollections();
        if (collections.Any(value => !knownCollections.ContainsKey(value)))
            throw new ArgumentException("One or more backup collections are invalid.", nameof(settings));
        if (settings.Enabled && collections.Count == 0)
            throw new ArgumentException("At least one backup collection is required when scheduling is enabled.", nameof(settings));

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var entity = await db.BackupScheduleConfigurations
            .SingleOrDefaultAsync(item => item.LegacySettingId == SettingsId);
        var now = DateTime.UtcNow;
        if (entity is null)
        {
            entity = new BackupScheduleConfigurationEntity
            {
                ConfigurationId = Guid.NewGuid(),
                LegacySettingId = SettingsId,
                ImportedAtUtc = now
            };
            db.BackupScheduleConfigurations.Add(entity);
        }

        entity.IsEnabled = settings.Enabled;
        entity.Frequency = frequency;
        entity.ScheduledHour = settings.Hour;
        entity.DayOfWeek = settings.DayOfWeek;
        entity.DayOfMonth = settings.DayOfMonth;
        entity.CollectionsJson = JsonSerializer.Serialize(collections);
        entity.MaxBackupsToKeep = settings.MaxBackupsToKeep;
        entity.UpdatedAtUtc = now;
        entity.UpdatedByReference = userId;
        await db.SaveChangesAsync();

        await _audit.LogAsync(action: "backup.settings_update", userId: userId, userRole: "superadmin",
            metadata: new { enabled = settings.Enabled, frequency, hour = settings.Hour, collections },
            severity: "warning");
    }

    /// <summary> Executes or evaluates the scheduled operation ShouldRunAsync. </summary>
    public async Task<bool> ShouldRunAsync()
    {
        var settings = await GetSettingsAsync();
        if (!settings.Enabled || settings.Hour is < 0 or > 23 || settings.MaxBackupsToKeep is < 1 or > 30)
            return false;
        if (string.IsNullOrEmpty(settings.LastRunAt)) return true;
        if (!DateTime.TryParse(settings.LastRunAt, out var lastRun)) return true;
        var now = DateTime.UtcNow;
        if (lastRun.Date == now.Date) return false;
        var target = new DateTime(now.Year, now.Month, now.Day, settings.Hour, 0, 0, DateTimeKind.Utc);
        if (now < target) return false;
        return settings.Frequency switch
        {
            "daily" => true,
            "weekly" => settings.DayOfWeek is >= 0 and <= 6 && (int)now.DayOfWeek == settings.DayOfWeek,
            "monthly" => settings.DayOfMonth is >= 1 and <= 31 && now.Day == settings.DayOfMonth,
            _ => false
        };
    }

    /// <summary> Executes or evaluates the scheduled operation RunScheduledBackupAsync. </summary>
    public async Task RunScheduledBackupAsync(IWebHostEnvironment env, bool forceRun = false)
    {
        var settings = await GetSettingsAsync();
        if (!settings.Enabled && !forceRun) return;
        if (settings.Collections.Count == 0) return;

        var startedAt = DateTime.UtcNow;
        _logger.LogInformation("[ScheduledBackup] Running target-schema backup...");
        try
        {
            var collections = settings.Collections.Distinct(StringComparer.Ordinal).ToList();
            var knownCollections = BackupService.GetKnownCollections();
            if (collections.Any(value => !knownCollections.ContainsKey(value)))
                throw new InvalidOperationException("Scheduled backup contains an unknown collection.");

            var (bytes, result) = await _backup.CreateBackupAsync(collections, "system-scheduler");
            if (!result.Success)
                throw new InvalidOperationException($"Scheduled backup was incomplete: {string.Join("; ", result.Errors)}");

            var protectedBytes = BackupFileProtector.Protect(bytes);
            var folder = GetBackupsFolder(env);
            var fileName = $"scheduled-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.zip";
            var fullPath = Path.Combine(folder, fileName);
            await File.WriteAllBytesAsync(fullPath, protectedBytes);

            var completedAt = DateTime.UtcNow;
            var artifactId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var metadata = JsonSerializer.Serialize(new BackupHistoryMetadata
            {
                Collections = result.Collections,
                TotalDocuments = result.TotalDocuments,
                DurationSeconds = result.Duration.TotalSeconds,
                Status = "success",
                Source = "scheduled"
            });
            await using (var db = await _sqlFactory.CreateDbContextAsync())
            {
                db.BackupArtifacts.Add(new BackupArtifactEntity
                {
                    BackupArtifactId = artifactId,
                    StorageReference = fileName,
                    Sha256 = SHA256.HashData(protectedBytes),
                    SizeBytes = protectedBytes.LongLength,
                    CreatedAtUtc = completedAt,
                    EncryptionKeyVersion = BackupEncryptionKeyVersion
                });
                db.BackupJobs.Add(new BackupJobEntity
                {
                    BackupJobId = jobId,
                    JobType = ScheduledBackupJobType,
                    Status = BackupSucceeded,
                    StartedAtUtc = startedAt,
                    CompletedAtUtc = completedAt,
                    RequestedByReference = "system-scheduler",
                    DatabaseReference = "configured-database",
                    BackupArtifactId = artifactId,
                    MetadataJson = metadata
                });
                await db.SaveChangesAsync();
            }

            settings.LastRunAt = completedAt.ToString("o");
            settings.LastRunStatus = "success";
            await SaveSettingsInternalAsync(settings);
            await RotateBackupsAsync(env, settings.MaxBackupsToKeep);
            await _audit.LogAsync(action: "backup.scheduled_run", userId: "system-scheduler", userRole: "system",
                metadata: new { fileName, fileSize = protectedBytes.LongLength }, severity: "info");
            _logger.LogInformation("[ScheduledBackup] Completed: {FileName}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ScheduledBackup] Failed");
            var completedAt = DateTime.UtcNow;
            await using (var db = await _sqlFactory.CreateDbContextAsync())
            {
                db.BackupJobs.Add(new BackupJobEntity
                {
                    BackupJobId = Guid.NewGuid(),
                    JobType = ScheduledBackupJobType,
                    Status = BackupFailed,
                    StartedAtUtc = startedAt,
                    CompletedAtUtc = completedAt,
                    RequestedByReference = "system-scheduler",
                    DatabaseReference = "configured-database",
                    SafeErrorCode = "scheduled-backup-failed",
                    MetadataJson = JsonSerializer.Serialize(new BackupHistoryMetadata
                    {
                        Collections = settings.Collections,
                        Status = "failed",
                        Source = "scheduled",
                        ErrorMessage = "Backup generation failed."
                    })
                });
                await db.SaveChangesAsync();
            }
            settings.LastRunAt = completedAt.ToString("o");
            settings.LastRunStatus = "failed";
            await SaveSettingsInternalAsync(settings);
            await _audit.LogAsync(action: "backup.scheduled_failed", userId: "system-scheduler",
                userRole: "system", metadata: new { error = "scheduled-backup-failed" }, severity: "critical");
        }
    }

    private async Task SaveSettingsInternalAsync(ScheduledBackupSettings settings)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var entity = await db.BackupScheduleConfigurations
            .SingleOrDefaultAsync(item => item.LegacySettingId == SettingsId);
        if (entity is null) return;
        entity.LastRunAtUtc = string.IsNullOrEmpty(settings.LastRunAt) ? null : DateTime.Parse(settings.LastRunAt).ToUniversalTime();
        entity.LastRunStatus = settings.LastRunStatus;
        await db.SaveChangesAsync();
    }

    /// <summary> Gets or previews data through GetHistoryAsync. </summary>
    public async Task<List<BackupHistoryEntry>> GetHistoryAsync(IWebHostEnvironment env, int limit = 50)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var jobs = await db.BackupJobs.AsNoTracking()
            .Where(item => item.JobType == ScheduledBackupJobType)
            .OrderByDescending(item => item.StartedAtUtc).Take(Math.Clamp(limit, 1, 500)).ToListAsync();
        var artifactIds = jobs.Where(item => item.BackupArtifactId.HasValue)
            .Select(item => item.BackupArtifactId!.Value).Distinct().ToList();
        var artifacts = await db.BackupArtifacts.AsNoTracking()
            .Where(item => artifactIds.Contains(item.BackupArtifactId)).ToDictionaryAsync(item => item.BackupArtifactId);
        var folder = GetBackupsFolder(env);

        return jobs.Select(job =>
        {
            artifacts.TryGetValue(job.BackupArtifactId ?? Guid.Empty, out var artifact);
            var metadata = string.IsNullOrWhiteSpace(job.MetadataJson)
                ? new BackupHistoryMetadata()
                : JsonSerializer.Deserialize<BackupHistoryMetadata>(job.MetadataJson) ?? new BackupHistoryMetadata();
            var fileName = artifact?.StorageReference ?? "";
            return new BackupHistoryEntry
            {
                Id = job.BackupJobId.ToString("N"),
                FileName = fileName,
                FileSize = artifact?.SizeBytes ?? 0,
                Collections = metadata.Collections ?? new List<string>(),
                TotalDocuments = metadata.TotalDocuments,
                CreatedAt = job.CompletedAtUtc ?? job.StartedAtUtc,
                DurationSeconds = metadata.DurationSeconds,
                Status = job.Status == BackupSucceeded ? "success" : "failed",
                Source = metadata.Source ?? "scheduled",
                ErrorMessage = metadata.ErrorMessage ?? job.SafeErrorCode,
                FileExists = !string.IsNullOrEmpty(fileName) && File.Exists(Path.Combine(folder, fileName))
            };
        }).ToList();
    }

    private async Task RotateBackupsAsync(IWebHostEnvironment env, int keep)
    {
        keep = Math.Clamp(keep, 1, 30);
        var folder = GetBackupsFolder(env);
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var artifacts = await db.BackupArtifacts.Where(item =>
                item.StorageReference.StartsWith("scheduled-") && item.RetainUntilUtc == null)
            .OrderByDescending(item => item.CreatedAtUtc).ToListAsync();
        foreach (var artifact in artifacts.Skip(keep))
        {
            var path = Path.Combine(folder, Path.GetFileName(artifact.StorageReference));
            try
            {
                if (File.Exists(path)) File.Delete(path);
                artifact.RetainUntilUtc = DateTime.UtcNow;
                db.BackupRetentionEvents.Add(new BackupRetentionEventEntity
                {
                    RetentionEventId = Guid.NewGuid(), BackupArtifactId = artifact.BackupArtifactId,
                    EventType = 1, OccurredAtUtc = DateTime.UtcNow, ActorReference = "system-scheduler",
                    Reason = "retention-policy"
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to rotate backup artifact {ArtifactId}", artifact.BackupArtifactId);
            }
        }
        await db.SaveChangesAsync();
    }

    /// <summary> Gets or previews data through GetBackupFilePathAsync. </summary>
    public Task<string?> GetBackupFilePathAsync(IWebHostEnvironment env, string fileName)
    {
        if (string.IsNullOrEmpty(fileName) || fileName.Contains("..") ||
            fileName.Contains("/") || fileName.Contains("\\"))
            return Task.FromResult<string?>(null);
        var path = Path.Combine(GetBackupsFolder(env), fileName);
        return Task.FromResult(File.Exists(path) ? path : null);
    }

    /// <summary> Removes or revokes data through DeleteBackupAsync. </summary>
    public async Task<bool> DeleteBackupAsync(IWebHostEnvironment env, string fileName, string userId)
    {
        try
        {
            var path = await GetBackupFilePathAsync(env, fileName);
            if (path is not null && File.Exists(path)) File.Delete(path);
            await using var db = await _sqlFactory.CreateDbContextAsync();
            var artifact = await db.BackupArtifacts.SingleOrDefaultAsync(item =>
                item.StorageReference == fileName && item.RetainUntilUtc == null);
            if (artifact is not null)
            {
                artifact.RetainUntilUtc = DateTime.UtcNow;
                db.BackupRetentionEvents.Add(new BackupRetentionEventEntity
                {
                    RetentionEventId = Guid.NewGuid(), BackupArtifactId = artifact.BackupArtifactId,
                    EventType = 2, OccurredAtUtc = DateTime.UtcNow, ActorReference = userId,
                    Reason = "manual-delete"
                });
                await db.SaveChangesAsync();
            }
            await _audit.LogAsync(action: "backup.scheduled_delete", userId: userId, userRole: "superadmin",
                metadata: new { fileName }, severity: "warning");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteBackup failed: {FileName}", fileName);
            return false;
        }
    }

    /// <summary> Gets or previews data through ListBackupFiles. </summary>
    public List<(string FileName, long FileSize, DateTime CreatedAt)> ListBackupFiles(IWebHostEnvironment env)
    {
        var folder = GetBackupsFolder(env);
        return Directory.GetFiles(folder, "*.zip").Select(file => new FileInfo(file))
            .Select(file => (FileName: file.Name, FileSize: file.Length, CreatedAt: file.CreationTimeUtc))
            .OrderByDescending(item => item.CreatedAt).ToList();
    }

    private static ScheduledBackupSettings MapSettings(BackupScheduleConfigurationEntity entity) => new()
    {
        Id = entity.LegacySettingId,
        Enabled = entity.IsEnabled,
        Frequency = entity.Frequency,
        Hour = entity.ScheduledHour,
        DayOfWeek = entity.DayOfWeek,
        DayOfMonth = entity.DayOfMonth,
        Collections = JsonSerializer.Deserialize<List<string>>(entity.CollectionsJson) ?? new(),
        MaxBackupsToKeep = entity.MaxBackupsToKeep,
        LastRunAt = entity.LastRunAtUtc?.ToString("o"),
        LastRunStatus = entity.LastRunStatus,
        UpdatedAt = entity.UpdatedAtUtc?.ToString("o"),
        UpdatedBy = entity.UpdatedByReference
    };

    private sealed class BackupHistoryMetadata
    {
        public List<string>? Collections { get; set; } = new();
        public int TotalDocuments { get; set; }
        public double DurationSeconds { get; set; }
        public string? Status { get; set; }
        public string? Source { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
