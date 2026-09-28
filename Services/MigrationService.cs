using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Services;

// خدمة تحويل building.AuditLog القديم لـ auditLogs collection
public class MigrationService
{
    private readonly FirestoreDb _db;
    private readonly BuildingsService _buildings;
    private readonly AuditLogger _auditLogger;

    private const string AuditCollection = "auditLogs";
    private const string BuildingsCollection = "buildings";

    public MigrationService(FirestoreContext ctx, BuildingsService buildings, AuditLogger auditLogger)
    {
        _db = ctx.Db;
        _buildings = buildings;
        _auditLogger = auditLogger;
    }

    // ============================================================
    // Migration كامل — كل العمارات
    // ============================================================
    public async Task<MigrationResult> MigrateAllAsync(bool dryRun, string userId)
    {
        var sw = Stopwatch.StartNew();
        var result = new MigrationResult();

        var buildingsSnapshot = await _db.Collection(BuildingsCollection).GetSnapshotAsync();
        result.BuildingsScanned = buildingsSnapshot.Documents.Count;

        foreach (var buildingDoc in buildingsSnapshot.Documents)
        {
            try
            {
                var data = buildingDoc.ToDictionary();
                if (!data.TryGetValue("auditLog", out var auditLogObj) || auditLogObj == null)
                    continue;

                if (!(auditLogObj is IEnumerable<object> auditLogList))
                    continue;

                var entries = auditLogList.ToList();
                result.EntriesFound += entries.Count;

                foreach (var entryObj in entries)
                {
                    try
                    {
                        var oldEntry = await ParseAndMigrateEntry(
                            buildingDoc.Id, entryObj, dryRun, userId);
                        if (oldEntry == "migrated") result.EntriesMigrated++;
                        else if (oldEntry == "skipped") result.EntriesSkipped++;
                        else if (oldEntry == "failed") result.EntriesFailed++;
                    }
                    catch (Exception ex)
                    {
                        result.EntriesFailed++;
                        result.Errors.Add($"Building {buildingDoc.Id}: {ex.Message}");
                    }
                }

                // ✅ بعد Migration ناجح — نمسح auditLog من العمارة
                if (!dryRun && result.EntriesFailed == 0)
                {
                    await _db.Collection(BuildingsCollection).Document(buildingDoc.Id)
                        .UpdateAsync("auditLog", FieldValue.Delete);
                    result.Details.Add($"✅ {buildingDoc.Id}: auditLog deleted after migration");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Building {buildingDoc.Id} failed: {ex.Message}");
            }
        }

        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }

    // ============================================================
    // Migration عمارة واحدة
    // ============================================================
    public async Task<MigrationResult> MigrateBuildingAsync(string buildingId, bool dryRun, string userId)
    {
        var sw = Stopwatch.StartNew();
        var result = new MigrationResult { BuildingsScanned = 1 };

        var buildingDoc = await _db.Collection(BuildingsCollection).Document(buildingId).GetSnapshotAsync();
        if (!buildingDoc.Exists)
        {
            result.Errors.Add("العمارة غير موجودة");
            return result;
        }

        var data = buildingDoc.ToDictionary();
        if (!data.TryGetValue("auditLog", out var auditLogObj) || auditLogObj == null)
        {
            result.Errors.Add("لا يوجد auditLog قديم في العمارة");
            return result;
        }

        if (!(auditLogObj is IEnumerable<object> auditLogList))
        {
            result.Errors.Add("auditLog ليس قائمة");
            return result;
        }

        var entries = auditLogList.ToList();
        result.EntriesFound = entries.Count;

        foreach (var entryObj in entries)
        {
            try
            {
                var status = await ParseAndMigrateEntry(buildingId, entryObj, dryRun, userId);
                if (status == "migrated") result.EntriesMigrated++;
                else if (status == "skipped") result.EntriesSkipped++;
                else if (status == "failed") result.EntriesFailed++;
            }
            catch (Exception ex)
            {
                result.EntriesFailed++;
                result.Errors.Add(ex.Message);
            }
        }

        if (!dryRun && result.EntriesFailed == 0 && entries.Count > 0)
        {
            await _db.Collection(BuildingsCollection).Document(buildingId)
                .UpdateAsync("auditLog", FieldValue.Delete);
            result.Details.Add($"✅ auditLog deleted after migration");
        }

        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }

    // ============================================================
    // Helper: تحويل entry من Dictionary (من Firebase) لـ AuditEntryDoc
    // ============================================================
    private async Task<string> ParseAndMigrateEntry(
        string buildingId, object entryObj, bool dryRun, string userId)
    {
        if (!(entryObj is IDictionary<string, object> dict))
            return "skipped";

        var action = dict.TryGetValue("action", out var a) ? a?.ToString() ?? "" : "";
        var details = dict.TryGetValue("details", out var d) ? d?.ToString() ?? "" : "";
        var actor = dict.TryGetValue("actor", out var ac) ? ac?.ToString() ?? "" : "";
        var actorRole = dict.TryGetValue("actorRole", out var ar) ? ar?.ToString() ?? "" : "";
        var tsStr = dict.TryGetValue("ts", out var ts) ? ts?.ToString() ?? "" : "";

        if (string.IsNullOrWhiteSpace(action))
            return "skipped";

        // نتأكد إن السجل مش موجود قبل كده (بناءً على action + timestamp + buildingId)
        DateTime createdUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(tsStr))
        {
            if (DateTime.TryParse(tsStr, out var parsedTs))
                createdUtc = parsedTs;
        }

        if (createdUtc.Kind == DateTimeKind.Unspecified)
            createdUtc = DateTime.SpecifyKind(createdUtc, DateTimeKind.Utc);
        else if (createdUtc.Kind == DateTimeKind.Local)
            createdUtc = createdUtc.ToUniversalTime();

        // ✅ Dry-run: مش بنكتب في Firebase
        if (dryRun)
            return "migrated";

        // ✅ بنكتب في auditLogs
        var entry = new AuditEntryDoc
        {
            Action = action,
            BuildingId = buildingId,
            UserId = actor,
            UserRole = actorRole,
            Severity = "info",
            Details = details,
            CreatedAt = Timestamp.FromDateTime(createdUtc),  
            Metadata = new Dictionary<string, object>
            {
                ["migratedFrom"] = "building.AuditLog",
                ["migratedBy"] = userId,
                ["migratedAt"] = DateTime.UtcNow.ToString("o"),
                ["originalTs"] = tsStr,
                ["originalLabel"] = dict.TryGetValue("label", out var l) ? l?.ToString() ?? "" : ""
            }
        };

        try
        {
            await _db.Collection(AuditCollection).AddAsync(entry);
            return "migrated";
        }
        catch
        {
            return "failed";
        }
    }
}