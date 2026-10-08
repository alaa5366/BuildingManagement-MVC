using BuildingManagementMvc.Data;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

// =====================================================================
//  OrphanedCleanupService
//  بيحذف السجلات اليتيمة (Orphaned) من Firestore
//  اللي بتشاور على حاجات مش موجودة في SQL
//
//  ⚠️ الـ Collections المدعومة فقط:
//    - qr-tokens
//    - presence
//    - dvrs
//    - cameras
//
//  ❌ ممنوع لمس: users, auditLogs, buildings, whatsappTemplates
// =====================================================================
public class OrphanedCleanupService
{
    private readonly FirestoreDb _fs;
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly ILogger<OrphanedCleanupService> _log;

    public OrphanedCleanupService(
        FirestoreContext fsCtx,
        IDbContextFactory<AppDbContext> sqlFactory,
        ILogger<OrphanedCleanupService> log)
    {
        _fs = fsCtx.Db;
        _sqlFactory = sqlFactory;
        _log = log;
    }

    // ============================================================
    // Main Entry Point
    // ============================================================
    public async Task<CleanupReport> RunAsync(
        string collectionName,
        bool dryRun = true,
        CancellationToken ct = default)
    {
        var report = new CleanupReport
        {
            CollectionName = collectionName,
            DryRun = dryRun,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            switch (collectionName.ToLowerInvariant())
            {
                case "qr-tokens":
                    await CleanupQrTokensAsync(report, dryRun, ct);
                    break;
                case "presence":
                    await CleanupPresenceAsync(report, dryRun, ct);
                    break;
                case "dvrs":
                    await CleanupDvrsAsync(report, dryRun, ct);
                    break;
                case "cameras":
                    await CleanupCamerasAsync(report, dryRun, ct);
                    break;
                default:
                    report.Errors.Add(Loc.T("Cleanup_Reason_CollectionNotSupported", collectionName));
                    break;
            }

            _log.LogInformation(
                "[Cleanup] {Collection}: found={Found}, deleted={Deleted}, dryRun={DryRun}",
                collectionName, report.ItemsFound, report.ItemsDeleted, dryRun);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[Cleanup] {Collection} FAILED", collectionName);
            report.Errors.Add(ex.Message);
        }

        report.FinishedAt = DateTime.UtcNow;
        return report;
    }

    // ============================================================
    // 1. qr-tokens
    // ============================================================
    private async Task CleanupQrTokensAsync(CleanupReport report, bool dryRun, CancellationToken ct)
    {
        var snap = await _fs.Collection("qr-tokens").GetSnapshotAsync(ct);
        await using var db = await _sqlFactory.CreateDbContextAsync(ct);

        var existingBuildings = (await db.Buildings.Select(b => b.Id).ToListAsync(ct)).ToHashSet();

        foreach (var doc in snap.Documents)
        {
            var d = doc.ToDictionary();
            var bld = S(d, "bld");

            bool isOrphaned =
                string.IsNullOrEmpty(bld) ||
                !existingBuildings.Contains(bld);

            if (isOrphaned)
            {
                report.ItemsFound++;
                report.Items.Add(new OrphanedItem
                {
                    Id = doc.Id,
                    Reason = string.IsNullOrEmpty(bld)
                        ? Loc.T("Cleanup_Reason_NoBld")
                        : Loc.T("Cleanup_Reason_BldNotFound", bld)
                });

                if (!dryRun)
                {
                    await doc.Reference.DeleteAsync(cancellationToken: ct);
                    report.ItemsDeleted++;
                }
            }
        }
    }

    // ============================================================
    // 2. presence
    // ============================================================
    private async Task CleanupPresenceAsync(CleanupReport report, bool dryRun, CancellationToken ct)
    {
        var snap = await _fs.Collection("presence").GetSnapshotAsync(ct);
        await using var db = await _sqlFactory.CreateDbContextAsync(ct);

        var existingBuildings = (await db.Buildings.Select(b => b.Id).ToListAsync(ct)).ToHashSet();

        foreach (var doc in snap.Documents)
        {
            var d = doc.ToDictionary();
            var bld = S(d, "buildingId");

            if (string.IsNullOrEmpty(bld)) continue;

            if (!existingBuildings.Contains(bld))
            {
                report.ItemsFound++;
                report.Items.Add(new OrphanedItem
                {
                    Id = doc.Id,
                    Reason = Loc.T("Cleanup_Reason_BldNotFound", bld)
                });

                if (!dryRun)
                {
                    await doc.Reference.DeleteAsync(cancellationToken: ct);
                    report.ItemsDeleted++;
                }
            }
        }
    }

    // ============================================================
    // 3. dvrs
    // ============================================================
    private async Task CleanupDvrsAsync(CleanupReport report, bool dryRun, CancellationToken ct)
    {
        var snap = await _fs.Collection("dvrs").GetSnapshotAsync(ct);
        await using var db = await _sqlFactory.CreateDbContextAsync(ct);

        var existingBuildings = (await db.Buildings.Select(b => b.Id).ToListAsync(ct)).ToHashSet();

        foreach (var doc in snap.Documents)
        {
            var d = doc.ToDictionary();
            var bld = S(d, "buildingId");

            if (string.IsNullOrEmpty(bld) || !existingBuildings.Contains(bld))
            {
                report.ItemsFound++;
                report.Items.Add(new OrphanedItem
                {
                    Id = doc.Id,
                    Reason = string.IsNullOrEmpty(bld)
                        ? Loc.T("Cleanup_Reason_NoBuildingId")
                        : Loc.T("Cleanup_Reason_BldNotFound", bld)
                });

                if (!dryRun)
                {
                    await doc.Reference.DeleteAsync(cancellationToken: ct);
                    report.ItemsDeleted++;
                }
            }
        }
    }

    // ============================================================
    // 4. cameras
    // ============================================================
    private async Task CleanupCamerasAsync(CleanupReport report, bool dryRun, CancellationToken ct)
    {
        var snap = await _fs.Collection("cameras").GetSnapshotAsync(ct);
        await using var db = await _sqlFactory.CreateDbContextAsync(ct);

        var existingDvrs = (await db.Dvrs.Select(d => d.Id).ToListAsync(ct)).ToHashSet();

        foreach (var doc in snap.Documents)
        {
            var d = doc.ToDictionary();
            var dvrId = S(d, "dvrId");

            if (string.IsNullOrEmpty(dvrId) || !existingDvrs.Contains(dvrId))
            {
                report.ItemsFound++;
                report.Items.Add(new OrphanedItem
                {
                    Id = doc.Id,
                    Reason = string.IsNullOrEmpty(dvrId)
                        ? Loc.T("Cleanup_Reason_NoDvrId")
                        : Loc.T("Cleanup_Reason_DvrNotFound", dvrId)
                });

                if (!dryRun)
                {
                    await doc.Reference.DeleteAsync(cancellationToken: ct);
                    report.ItemsDeleted++;
                }
            }
        }
    }

    // ============================================================
    // Helpers
    // ============================================================
    private static string S(Dictionary<string, object> d, string k)
        => d.TryGetValue(k, out var v) && v != null ? v.ToString() ?? "" : "";

    // ============================================================
    // Supported Collections
    // ============================================================
    public static List<(string Name, string LabelAr)> SupportedCollections() => new()
    {
        ("qr-tokens", Loc.T("Cleanup_Col_QrTokens")),
        ("presence",  Loc.T("Cleanup_Col_Presence")),
        ("dvrs",      Loc.T("Cleanup_Col_Dvrs")),
        ("cameras",   Loc.T("Cleanup_Col_Cameras"))
    };
}

// ============================================================
// Report Models
// ============================================================
public class CleanupReport
{
    public string CollectionName { get; set; } = "";
    public bool DryRun { get; set; }
    public int ItemsFound { get; set; }
    public int ItemsDeleted { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public TimeSpan Duration => FinishedAt - StartedAt;
    public List<OrphanedItem> Items { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class OrphanedItem
{
    public string Id { get; set; } = "";
    public string Reason { get; set; } = "";
}