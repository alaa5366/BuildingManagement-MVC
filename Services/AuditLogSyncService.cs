using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class AuditLogSyncService
{
    private readonly FirestoreDb _fs;
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly ILogger<AuditLogSyncService> _log;

    private const string Collection = "auditLogs";

    public AuditLogSyncService(
        FirestoreContext fsCtx,
        IDbContextFactory<AppDbContext> sqlFactory,
        ILogger<AuditLogSyncService> log)
    {
        _fs = fsCtx.Db;
        _sqlFactory = sqlFactory;
        _log = log;
    }

    public async Task<AuditLogSyncReport> SyncAllToSqlAsync()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var report = new AuditLogSyncReport();

        try
        {
            var snapshot = await _fs.Collection(Collection)
                .OrderBy("createdAt")
                .Limit(10000)
                .GetSnapshotAsync();

            _log.LogInformation("Found {Count} auditLogs in Firestore", snapshot.Documents.Count);

            await using var db = await _sqlFactory.CreateDbContextAsync();

            var existingKeys = (await db.AuditLog
                .Where(x => x.SourceId != null)
                .Select(x => x.SourceId!)
                .ToListAsync())
                .ToHashSet();

            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var data = doc.ToDictionary();

                    var sourceId = doc.Id;
                    if (existingKeys.Contains(sourceId))
                    {
                        report.Skipped++;
                        continue;
                    }

                    var action = GetString(data, "action");
                    if (string.IsNullOrWhiteSpace(action))
                    {
                        report.Skipped++;
                        continue;
                    }

                    var buildingId = GetStringOrNull(data, "buildingId");
                    if (!string.IsNullOrWhiteSpace(buildingId))
                    {
                        var buildingExists = await db.Buildings.AnyAsync(b => b.Id == buildingId);
                        if (!buildingExists) buildingId = null;
                    }

                    var ts = ParseTs(data, "createdAt")
                          ?? ParseTs(data, "timestamp")
                          ?? DateTime.UtcNow;

                    db.AuditLog.Add(new AuditLogEntity
                    {
                        SourceId = sourceId,
                        BuildingId = buildingId,
                        Ts = ts,
                        Action = Cut(action, 100),
                        Label = Cut(GetString(data, "label"), 300),
                        Actor = Cut(GetString(data, "userId"), 200),
                        ActorRole = Cut(GetString(data, "userRole"), 20),
                        Details = GetString(data, "details"),
                        MonthKey = Cut(ts.ToString("yyyy-MM"), 7),
                        BuildingNumber = null,
                        AptNumber = null
                    });

                    report.Added++;
                    existingKeys.Add(sourceId);
                }
                catch (Exception ex)
                {
                    report.Failed++;
                    report.Errors.Add($"{doc.Id}: {ex.Message}");
                }
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            report.Errors.Add($"عام: {ex.Message}");
            _log.LogError(ex, "AuditLogSync failed");
        }

        sw.Stop();
        report.Elapsed = sw.Elapsed;
        return report;
    }

    private static string GetString(Dictionary<string, object> d, string key, string def = "")
        => d.TryGetValue(key, out var v) && v != null ? v.ToString() ?? def : def;

    private static string? GetStringOrNull(Dictionary<string, object> d, string key)
        => d.TryGetValue(key, out var v) && v != null ? v.ToString() : null;

    private static DateTime? ParseTs(Dictionary<string, object> d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return null;

        if (v is Google.Cloud.Firestore.Timestamp ts)
            return new DateTime(ts.ToDateTime().Ticks - ts.ToDateTime().Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

        if (DateTime.TryParse(v.ToString(), null,
                System.Globalization.DateTimeStyles.AdjustToUniversal |
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out var dt))
            return new DateTime(dt.Ticks - dt.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

        return null;
    }

    private static string Cut(string? s, int max)
    {
        s ??= "";
        return s.Length <= max ? s : s[..max];
    }
}

public class AuditLogSyncReport
{
    public int Added { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public TimeSpan Elapsed { get; set; }
    public List<string> Errors { get; set; } = new();
}