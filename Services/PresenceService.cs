using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Services;

// ✅ Phase 24 — Presence Service (متوافق مع نسخة JS)
public class PresenceService
{
    private readonly FirestoreDb _db;
    private readonly ILogger<PresenceService> _logger;

    private const string Collection = "presence";

    // ⏱️ نعتبر المستخدم "Online" لو آخر ظهور أقل من 2 دقيقة
    public static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(2);

    public PresenceService(FirestoreContext ctx, ILogger<PresenceService> logger)
    {
        _db = ctx.Db;
        _logger = logger;
    }

    private CollectionReference Col => _db.Collection(Collection);

    // ============================================================
    // Helper: نبني الـ Document ID بنفس صيغة JS
    // ============================================================
    public static string BuildDocId(string uid, string aptId, string buildingId)
    {
        return !string.IsNullOrEmpty(buildingId) && !string.IsNullOrEmpty(aptId)
            ? $"{buildingId}_{aptId}"
            : uid;
    }

    // ============================================================
    // 1. Heartbeat — يسجّل إن المستخدم نشط الآن
    // ============================================================
    public async Task HeartbeatAsync(string uid, string aptId, string buildingId)
    {
        if (string.IsNullOrEmpty(uid)) return;

        var docId = BuildDocId(uid, aptId, buildingId);

        try
        {
            var now = DateTime.UtcNow;
            var doc = new Dictionary<string, object>
            {
                ["aptId"] = aptId ?? "",
                ["buildingId"] = buildingId ?? "",
                ["lastSeen"] = now.ToString("o"),
                ["ts"] = new DateTimeOffset(now).ToUnixTimeMilliseconds()
            };

            await Col.Document(docId).SetAsync(doc, SetOptions.MergeAll);

            _logger.LogInformation(
                "[Presence] Heartbeat OK: docId={DocId}, uid={Uid}",
                docId, uid);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Presence] Heartbeat failed for docId={DocId}", docId);
        }
    }

    // ============================================================
    // 2. Get online count per building
    // ============================================================
    public async Task<int> GetOnlineCountAsync(string buildingId)
    {
        try
        {
            var cutoff = DateTime.UtcNow.Subtract(OnlineThreshold);

            var snap = await Col
                .WhereEqualTo("buildingId", buildingId)
                .GetSnapshotAsync();

            int count = 0;
            foreach (var doc in snap.Documents)
            {
                var data = doc.ToDictionary();
                if (data.TryGetValue("ts", out var tsRaw) && tsRaw != null)
                {
                    if (long.TryParse(tsRaw.ToString(), out var ts))
                    {
                        var lastSeen = DateTimeOffset.FromUnixTimeMilliseconds(ts).UtcDateTime;
                        if (lastSeen >= cutoff) count++;
                    }
                }
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Presence] GetOnlineCount failed");
            return 0;
        }
    }

    // ============================================================
    // 3. Get all presences for a building (map aptId → Presence)
    // ============================================================
    public async Task<Dictionary<string, Presence>> GetByBuildingAsync(string buildingId)
    {
        var result = new Dictionary<string, Presence>();

        try
        {
            var snap = await Col
                .WhereEqualTo("buildingId", buildingId)
                .GetSnapshotAsync();

            foreach (var doc in snap.Documents)
            {
                try
                {
                    var p = doc.ConvertTo<Presence>();
                    p.Uid = doc.Id;
                    if (!string.IsNullOrEmpty(p.AptId))
                        result[p.AptId] = p;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[Presence] ConvertTo failed for doc {Id}", doc.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Presence] GetByBuilding failed");
        }

        return result;
    }

    // ============================================================
    // 4. Helper: هل المستخدم أونلاين؟
    // ============================================================
    public static bool IsOnline(Presence? p)
    {
        if (p == null || p.Ts <= 0) return false;
        var lastSeen = DateTimeOffset.FromUnixTimeMilliseconds(p.Ts).UtcDateTime;
        return DateTime.UtcNow - lastSeen <= OnlineThreshold;
    }

    // ============================================================
    // 5. Helper: وصف "منذ آخر ظهور"
    // ============================================================
    public static string TimeAgo(Presence? p)
    {
        if (p == null || p.Ts <= 0) return "لم يسجل دخول";

        var lastSeen = DateTimeOffset.FromUnixTimeMilliseconds(p.Ts).UtcDateTime;
        var diff = DateTime.UtcNow - lastSeen;

        if (diff.TotalMinutes < 1) return "الآن";
        if (diff.TotalMinutes < 60) return $"منذ {(int)diff.TotalMinutes} دقيقة";
        if (diff.TotalHours < 24) return $"منذ {(int)diff.TotalHours} ساعة";
        if (diff.TotalDays < 7) return $"منذ {(int)diff.TotalDays} يوم";
        return lastSeen.ToString("yyyy-MM-dd");
    }

    // ============================================================
    // 6. Clear old presences (maintenance)
    // ============================================================
    public async Task<int> ClearOldAsync(int olderThanDays = 30)
    {
        int deleted = 0;
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
            var cutoffMs = new DateTimeOffset(cutoff).ToUnixTimeMilliseconds();

            var snap = await Col.GetSnapshotAsync();
            foreach (var doc in snap.Documents)
            {
                var data = doc.ToDictionary();
                if (data.TryGetValue("ts", out var tsRaw) && tsRaw != null)
                {
                    if (long.TryParse(tsRaw.ToString(), out var ts) && ts < cutoffMs)
                    {
                        await doc.Reference.DeleteAsync();
                        deleted++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Presence] ClearOld failed");
        }
        return deleted;
    }
}