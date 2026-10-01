using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Services;

public class ScheduledBackupService
{
    private readonly FirestoreDb _db;
    private readonly BackupService _backup;
    private readonly IAuditLogger _audit;
    private readonly ILogger<ScheduledBackupService> _logger;

    private const string SettingsCollection = "system_settings";
    private const string HistoryCollection = "backup_history";
    private const string SettingsDocId = "scheduled_backup";

    public static string GetBackupsFolder(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "Backups");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }

    public ScheduledBackupService(
        FirestoreContext ctx,
        BackupService backup,
        IAuditLogger audit,
        ILogger<ScheduledBackupService> logger)
    {
        _db = ctx.Db;
        _backup = backup;
        _audit = audit;
        _logger = logger;
    }

    // ============================================================
    // 1. الإعدادات
    // ============================================================
    public async Task<ScheduledBackupSettings> GetSettingsAsync()
    {
        var doc = await _db.Collection(SettingsCollection).Document(SettingsDocId).GetSnapshotAsync();
        if (!doc.Exists)
            return new ScheduledBackupSettings();

        return doc.ConvertTo<ScheduledBackupSettings>();
    }

    public async Task SaveSettingsAsync(ScheduledBackupSettings settings, string userId)
    {
        settings.UpdatedAt = DateTime.UtcNow.ToString("o");
        settings.UpdatedBy = userId;

        await _db.Collection(SettingsCollection).Document(SettingsDocId)
            .SetAsync(settings, SetOptions.Overwrite);

        await _audit.LogAsync(
            action: "backup.settings_update",
            userId: userId,
            userRole: "superadmin",
            metadata: new
            {
                enabled = settings.Enabled,
                frequency = settings.Frequency,
                hour = settings.Hour,
                collections = settings.Collections,
                maxBackupsToKeep = settings.MaxBackupsToKeep
            },
            severity: "warning");
    }

    private async Task SaveSettingsInternalAsync(ScheduledBackupSettings settings)
    {
        await _db.Collection(SettingsCollection).Document(SettingsDocId)
            .SetAsync(settings, SetOptions.Overwrite);
    }

    // ============================================================
    // 2. هل حان الوقت؟
    // ============================================================
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

    // ============================================================
    // 3. تنفيذ النسخة المجدولة
    // ============================================================
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

            await _db.Collection(HistoryCollection).AddAsync(new BackupHistoryEntry
            {
                FileName = fileName,
                FileSize = bytes.Length,
                Collections = result.Collections,
                TotalDocuments = result.TotalDocuments,
                CreatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow),
                DurationSeconds = result.Duration.TotalSeconds,
                Status = "success",
                Source = "scheduled"
            });

            settings.LastRunAt = DateTime.UtcNow.ToString("o");
            settings.LastRunStatus = "success";
            await SaveSettingsInternalAsync(settings);

            _logger.LogInformation($"[ScheduledBackup] Done: {fileName}");

            await RotateBackupsAsync(env, settings.MaxBackupsToKeep);

            await _audit.LogAsync(
                action: "backup.scheduled_run",
                userId: "system-scheduler",
                userRole: "system",
                metadata: new { fileName, fileSize = bytes.Length, totalDocuments = result.TotalDocuments },
                severity: "info");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ScheduledBackup] Failed");

            settings.LastRunAt = DateTime.UtcNow.ToString("o");
            settings.LastRunStatus = "failed";
            await SaveSettingsInternalAsync(settings);

            await _db.Collection(HistoryCollection).AddAsync(new BackupHistoryEntry
            {
                FileName = "(failed)",
                CreatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow),
                Status = "failed",
                Source = "scheduled",
                ErrorMessage = ex.Message
            });

            await _audit.LogAsync(
                action: "backup.scheduled_failed",
                userId: "system-scheduler",
                userRole: "system",
                metadata: new { error = ex.Message },
                severity: "critical");
        }
    }

    // ============================================================
    // 4. History
    // ============================================================
    public async Task<List<BackupHistoryEntry>> GetHistoryAsync(IWebHostEnvironment env, int limit = 50)
    {
        try
        {
            var snapshot = await _db.Collection(HistoryCollection)
                .OrderByDescending("createdAt")
                .Limit(limit)
                .GetSnapshotAsync();

            var list = new List<BackupHistoryEntry>();
            var folder = GetBackupsFolder(env);

            foreach (var doc in snapshot.Documents)
            {
                var entry = doc.ConvertTo<BackupHistoryEntry>();
                entry.Id = doc.Id;

                // ✅ تحديد هل الملف موجود فعلاً
                if (!string.IsNullOrEmpty(entry.FileName) && entry.FileName.EndsWith(".zip"))
                {
                    var path = Path.Combine(folder, entry.FileName);
                    entry.FileExists = File.Exists(path);
                }

                list.Add(entry);
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BackupHistory] Get failed");
            return new List<BackupHistoryEntry>();
        }
    }

    // ============================================================
    // 5. Rotation
    // ============================================================
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
            try
            {
                file.Delete();
                _logger.LogInformation($"[ScheduledBackup] Deleted old: {file.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"[ScheduledBackup] Failed to delete: {file.Name}");
            }
        }

        await Task.CompletedTask;
    }

    // ============================================================
    // 6. عمليات يدوية
    // ============================================================
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
            // 1. امسح الملف الفعلي لو موجود
            var path = await GetBackupFilePathAsync(env, fileName);
            var fileDeleted = false;
            if (path != null && File.Exists(path))
            {
                File.Delete(path);
                fileDeleted = true;
            }

            // 2. امسح السجل من Firestore
            var historyRef = _db.Collection(HistoryCollection);
            var historySnapshot = await historyRef.WhereEqualTo("fileName", fileName).GetSnapshotAsync();

            foreach (var doc in historySnapshot.Documents)
            {
                await doc.Reference.DeleteAsync();
            }

            // 3. سجل في Audit Log
            await _audit.LogAsync(
                action: "backup.scheduled_delete",
                userId: userId,
                userRole: "superadmin",
                metadata: new
                {
                    fileName,
                    fileDeleted,
                    historyDeleted = historySnapshot.Documents.Count
                },
                severity: "warning");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[ScheduledBackup] DeleteBackup failed: {fileName}");
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