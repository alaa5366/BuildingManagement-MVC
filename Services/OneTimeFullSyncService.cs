using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

// =====================================================================
//  OneTimeFullSyncService
//  يزامن من Firestore → SQL الجداول الناقصة:
//    - qr-tokens  → QrTokens
//    - presence   → Presence
//    - qr_usage   → QrUsages
// =====================================================================
public class OneTimeFullSyncService
{
    private readonly FirestoreDb _fs;
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly ILogger<OneTimeFullSyncService> _log;

    public OneTimeFullSyncService(
        FirestoreContext fsCtx,
        IDbContextFactory<AppDbContext> sqlFactory,
        ILogger<OneTimeFullSyncService> log)
    {
        _fs = fsCtx.Db;
        _sqlFactory = sqlFactory;
        _log = log;
    }

    public async Task<OneTimeSyncReport> RunAllAsync(CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var report = new OneTimeSyncReport();

        report.QrTokens = await SyncQrTokensAsync(ct);
        report.Presence = await SyncPresenceAsync(ct);
        report.QrUsages = await SyncQrUsagesAsync(ct);

        sw.Stop();
        report.Elapsed = sw.Elapsed;

        _log.LogInformation(
            "[OneTimeSync] Done: QrTokens=+{QrTokens}, Presence=+{Presence}, QrUsages=+{QrUsages} in {Sec}s",
            report.QrTokens, report.Presence, report.QrUsages, sw.Elapsed.TotalSeconds);

        return report;
    }

    // ============================================================
    // 1. qr-tokens → QrTokens
    // ============================================================
    private async Task<int> SyncQrTokensAsync(CancellationToken ct)
    {
        var skipped = new List<string>();
        var alreadyExists = 0;
        var orphaned = 0;

        try
        {
            var snap = await _fs.Collection("qr-tokens").GetSnapshotAsync(ct);
            _log.LogInformation("[OneTimeSync] qr-tokens: {N} docs in Firestore", snap.Documents.Count);

            if (snap.Documents.Count == 0) return 0;

            await using var db = await _sqlFactory.CreateDbContextAsync(ct);
            var existingIds = await db.QrTokens.Select(x => x.Uid).ToListAsync(ct);
            var existing = new HashSet<string>(existingIds);

            // ✅ جيب الـ Buildings مرة واحدة (بدل ما تسأل في كل iteration)
            var existingBuildings = (await db.Buildings.Select(b => b.Id).ToListAsync(ct)).ToHashSet();

            var toAdd = new List<QrTokenEntity>();

            foreach (var doc in snap.Documents)
            {
                if (existing.Contains(doc.Id))
                {
                    alreadyExists++;
                    continue;
                }

                var d = doc.ToDictionary();
                var bld = S(d, "bld");
                var apt = S(d, "apt");

                if (string.IsNullOrEmpty(bld) || string.IsNullOrEmpty(apt))
                {
                    orphaned++;
                    skipped.Add($"{doc.Id}: bld='{bld}' أو apt='{apt}' فاضي");
                    continue;
                }

                if (!existingBuildings.Contains(bld))
                {
                    orphaned++;
                    skipped.Add($"{doc.Id}: العمارة '{bld}' مش موجودة في SQL");
                    continue;
                }

                var aptExists = await db.Apartments
                    .AnyAsync(a => a.BuildingId == bld && a.Id == apt, ct);
                if (!aptExists)
                {
                    orphaned++;
                    skipped.Add($"{doc.Id}: الشقة '{apt}' في عمارة '{bld}' مش موجودة في SQL");
                    continue;
                }

                var ts = L(d, "ts");
                var issuedAt = ts > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(ts).UtcDateTime
                    : DateTime.UtcNow;

                var exp = L(d, "exp");
                var expiresAt = exp > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(exp).UtcDateTime
                    : DateTime.UtcNow.AddHours(24);

                toAdd.Add(new QrTokenEntity
                {
                    Uid = BuildingSqlMapper.Cut(doc.Id, 128),
                    BuildingId = bld,
                    ApartmentId = apt,
                    UseType = S(d, "use", "single"),
                    IssuedAt = issuedAt,
                    ExpiresAt = expiresAt,
                    CreatedBy = S(d, "by"),
                    IsUsed = B(d, "used") ?? false,
                    UsedAt = ParseTs(S(d, "usedAt")),
                    UsedBy = S(d, "usedBy"),
                    CreatedAt = ParseTs(S(d, "createdAt")) ?? DateTime.UtcNow
                });
            }

            if (toAdd.Count > 0)
            {
                db.QrTokens.AddRange(toAdd);
                await db.SaveChangesAsync(ct);
            }

            // ✅ ملخص مختصر (Info) — بيظهر دايماً
            _log.LogInformation(
                "[OneTimeSync] qr-tokens: +{Added} added, {Existing} existed, {Orphaned} orphaned",
                toAdd.Count, alreadyExists, orphaned);

            // ✅ Warning بس للسجلات اليتيمة (مش الموجودة)
            if (skipped.Count > 0)
            {
                _log.LogWarning("[OneTimeSync] qr-tokens orphaned:\n{List}",
                    string.Join("\n", skipped));
            }

            return toAdd.Count;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[OneTimeSync] qr-tokens FAILED");
            return 0;
        }
    }

    // ============================================================
    // 2. presence → Presence
    // ============================================================
    private async Task<int> SyncPresenceAsync(CancellationToken ct)
    {
        try
        {
            var snap = await _fs.Collection("presence").GetSnapshotAsync(ct);
            _log.LogInformation("[OneTimeSync] presence: {N} docs in Firestore", snap.Documents.Count);

            if (snap.Documents.Count == 0) return 0;

            await using var db = await _sqlFactory.CreateDbContextAsync(ct);
            var existingIds = await db.Presence.Select(x => x.Uid).ToListAsync(ct);
            var existing = new HashSet<string>(existingIds);

            var toAdd = new List<PresenceEntity>();

            foreach (var doc in snap.Documents)
            {
                if (existing.Contains(doc.Id)) continue;

                var d = doc.ToDictionary();
                var ts = L(d, "ts");
                var lastSeen = ts > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(ts).UtcDateTime
                    : ParseTs(S(d, "lastSeen")) ?? DateTime.UtcNow;

                toAdd.Add(new PresenceEntity
                {
                    Uid = BuildingSqlMapper.Cut(doc.Id, 128),
                    BuildingId = N(S(d, "buildingId")),
                    ApartmentId = N(S(d, "aptId")),
                    LastSeen = lastSeen
                });
            }

            if (toAdd.Count > 0)
            {
                db.Presence.AddRange(toAdd);
                await db.SaveChangesAsync(ct);
            }

            _log.LogInformation("[OneTimeSync] presence: +{N} added", toAdd.Count);
            return toAdd.Count;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[OneTimeSync] presence FAILED");
            return 0;
        }
    }

    // ============================================================
    // 3. qr_usage → QrUsages
    // ============================================================
    private async Task<int> SyncQrUsagesAsync(CancellationToken ct)
    {
        try
        {
            var snap = await _fs.Collection("qr_usage").GetSnapshotAsync(ct);
            _log.LogInformation("[OneTimeSync] qr_usage: {N} docs in Firestore", snap.Documents.Count);

            if (snap.Documents.Count == 0) return 0;

            await using var db = await _sqlFactory.CreateDbContextAsync(ct);
            var existingIds = await db.QrUsages.Select(x => x.Id).ToListAsync(ct);
            var existing = new HashSet<string>(existingIds);

            var toAdd = new List<QrUsageEntity>();

            foreach (var doc in snap.Documents)
            {
                if (existing.Contains(doc.Id)) continue;

                var d = doc.ToDictionary();

                toAdd.Add(new QrUsageEntity
                {
                    Id = BuildingSqlMapper.Cut(doc.Id, 128),
                    TokenId = BuildingSqlMapper.Cut(S(d, "tokenId"), 128),
                    UseCount = (int)L(d, "useCount"),
                    FirstUsedAt = S(d, "firstUsedAt"),
                    LastUsedAt = S(d, "lastUsedAt"),
                    DeviceFingerprint = N(S(d, "deviceFingerprint")),
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (toAdd.Count > 0)
            {
                db.QrUsages.AddRange(toAdd);
                await db.SaveChangesAsync(ct);
            }

            _log.LogInformation("[OneTimeSync] qr_usage: +{N} added", toAdd.Count);
            return toAdd.Count;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[OneTimeSync] qr_usage FAILED");
            return 0;
        }
    }

    // ============================================================
    // Helpers
    // ============================================================
    private static string S(Dictionary<string, object> d, string k, string def = "")
        => d.TryGetValue(k, out var v) && v != null ? v.ToString() ?? def : def;

    private static string? N(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static long L(Dictionary<string, object> d, string k)
    {
        if (!d.TryGetValue(k, out var v) || v == null) return 0;
        if (v is long l) return l;
        if (v is int i) return i;
        if (v is double db) return (long)db;
        return long.TryParse(v.ToString(), out var n) ? n : 0;
    }

    private static bool? B(Dictionary<string, object> d, string k)
    {
        if (!d.TryGetValue(k, out var v) || v == null) return null;
        if (v is bool b) return b;
        return bool.TryParse(v.ToString(), out var r) ? r : null;
    }

    private static DateTime? ParseTs(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParse(s, null,
                System.Globalization.DateTimeStyles.AdjustToUniversal |
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out var dt))
            return new DateTime(dt.Ticks - dt.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        return null;
    }
}

// ============================================================
// Report
// ============================================================
public class OneTimeSyncReport
{
    public int QrTokens { get; set; }
    public int Presence { get; set; }
    public int QrUsages { get; set; }
    public TimeSpan Elapsed { get; set; }
}