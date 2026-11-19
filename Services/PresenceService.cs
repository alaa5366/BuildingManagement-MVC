using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Tracks online users using durable presence sessions and append-only heartbeat observations.
/// </summary>
public class PresenceService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly ILogger<PresenceService> _logger;

    public static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(2);

    public PresenceService(IDbContextFactory<AppDbContext> sqlFactory, ILogger<PresenceService> logger)
    {
        _sqlFactory = sqlFactory;
        _logger = logger;
    }

    public static string BuildDocId(string uid, string aptId, string buildingId) =>
        !string.IsNullOrEmpty(buildingId) && !string.IsNullOrEmpty(aptId)
            ? $"{buildingId}_{aptId}"
            : uid;

    /// <summary>Records a heartbeat without deleting historical session evidence.</summary>
    public async Task HeartbeatAsync(string uid, string aptId, string buildingId)
    {
        if (string.IsNullOrWhiteSpace(uid)) return;

        var now = DateTime.UtcNow;
        var docId = BuildDocId(uid, aptId, buildingId);
        var totalTimer = Stopwatch.StartNew();
        long contextCreateMs = 0;
        long transactionBeginMs = 0;
        long sessionQueryMs = 0;
        long saveCommitMs = 0;
        var stage = "ContextCreate";
        try
        {
            var stageTimer = Stopwatch.StartNew();
            await using var db = await _sqlFactory.CreateDbContextAsync();
            stageTimer.Stop();
            contextCreateMs = stageTimer.ElapsedMilliseconds;

            stage = "BeginTransaction";
            stageTimer.Restart();
            await using var tx = await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);
            stageTimer.Stop();
            transactionBeginMs = stageTimer.ElapsedMilliseconds;

            stage = "FindSession";
            stageTimer.Restart();
            var session = await db.PresenceSessions
                .Where(x => x.LegacyUid == docId && x.Status == 0)
                .OrderByDescending(x => x.LastSeenAtUtc)
                .FirstOrDefaultAsync();
            stageTimer.Stop();
            sessionQueryMs = stageTimer.ElapsedMilliseconds;

            if (session == null)
            {
                session = new PresenceSessionEntity
                {
                    PresenceSessionId = Guid.NewGuid(),
                    LegacyUid = docId,
                    BuildingId = string.IsNullOrWhiteSpace(buildingId) ? null : buildingId,
                    ApartmentId = string.IsNullOrWhiteSpace(aptId) ? null : aptId,
                    StartedAtUtc = now,
                    LastSeenAtUtc = now,
                    ExpiresAtUtc = now.Add(OnlineThreshold),
                    Status = 0
                };
                db.PresenceSessions.Add(session);
            }
            else
            {
                session.LastSeenAtUtc = now;
                session.ExpiresAtUtc = now.Add(OnlineThreshold);
                session.BuildingId = string.IsNullOrWhiteSpace(buildingId) ? session.BuildingId : buildingId;
                session.ApartmentId = string.IsNullOrWhiteSpace(aptId) ? session.ApartmentId : aptId;
            }

            db.PresenceHeartbeats.Add(new PresenceHeartbeatEntity
            {
                PresenceSessionId = session.PresenceSessionId,
                ObservedAtUtc = now,
                Signal = 0
            });

            stage = "SaveAndCommit";
            stageTimer.Restart();
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            stageTimer.Stop();
            saveCommitMs = stageTimer.ElapsedMilliseconds;

            totalTimer.Stop();
            if (totalTimer.ElapsedMilliseconds >= 100)
            {
                _logger.LogWarning(
                    "[Presence PERF] Heartbeat service slow: Total={TotalMs}ms ContextCreate={ContextMs}ms TransactionBegin={TransactionMs}ms SessionQuery={QueryMs}ms SaveCommit={SaveCommitMs}ms",
                    totalTimer.ElapsedMilliseconds,
                    contextCreateMs,
                    transactionBeginMs,
                    sessionQueryMs,
                    saveCommitMs);
            }
        }
        catch (Exception ex)
        {
            totalTimer.Stop();
            _logger.LogWarning(
                ex,
                "[Presence] Heartbeat failed at {Stage}; Total={TotalMs}ms ContextCreate={ContextMs}ms TransactionBegin={TransactionMs}ms SessionQuery={QueryMs}ms SaveCommit={SaveCommitMs}ms",
                stage,
                totalTimer.ElapsedMilliseconds,
                contextCreateMs,
                transactionBeginMs,
                sessionQueryMs,
                saveCommitMs);
        }
    }

    public async Task<int> GetOnlineCountAsync(string buildingId)
    {
        try
        {
            var cutoff = DateTime.UtcNow.Subtract(OnlineThreshold);
            await using var db = await _sqlFactory.CreateDbContextAsync();
            return await db.PresenceSessions
                .Where(x => x.BuildingId == buildingId && x.Status == 0 &&
                    x.EndedAtUtc == null && x.LastSeenAtUtc >= cutoff)
                .Select(x => x.ApartmentId ?? x.LegacyUid)
                .Distinct()
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Presence] GetOnlineCount failed");
            return 0;
        }
    }

    public async Task<Dictionary<string, Presence>> GetByBuildingAsync(string buildingId)
    {
        var result = new Dictionary<string, Presence>();
        try
        {
            var cutoff = DateTime.UtcNow.Subtract(OnlineThreshold);
            await using var db = await _sqlFactory.CreateDbContextAsync();
            var list = await db.PresenceSessions.AsNoTracking()
                .Where(x => x.BuildingId == buildingId && x.ApartmentId != null &&
                    x.EndedAtUtc == null && x.Status == 0)
                .GroupBy(x => x.ApartmentId!)
                .Select(g => g.OrderByDescending(x => x.LastSeenAtUtc).First())
                .ToListAsync();

            foreach (var session in list)
            {
                var aptId = session.ApartmentId!;
                result[aptId] = new Presence
                {
                    Uid = session.LegacyUid ?? session.PresenceSessionId.ToString("N"),
                    AptId = aptId,
                    BuildingId = session.BuildingId ?? "",
                    LastSeen = session.LastSeenAtUtc.ToString("o"),
                    Ts = new DateTimeOffset(session.LastSeenAtUtc, TimeSpan.Zero).ToUnixTimeMilliseconds()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Presence] GetByBuilding failed");
        }
        return result;
    }

    public static bool IsOnline(Presence? p)
    {
        if (p == null || p.Ts <= 0) return false;
        var lastSeen = DateTimeOffset.FromUnixTimeMilliseconds(p.Ts).UtcDateTime;
        return DateTime.UtcNow - lastSeen <= OnlineThreshold;
    }

    public static bool IsRecent(Presence? p)
    {
        if (p == null || p.Ts <= 0) return false;
        var lastSeen = DateTimeOffset.FromUnixTimeMilliseconds(p.Ts).UtcDateTime;
        return DateTime.UtcNow - lastSeen < TimeSpan.FromHours(24);
    }

    public static string TimeAgo(Presence? p)
    {
        if (p == null || p.Ts <= 0) return Loc.T("Never_Logged_In");
        var lastSeen = DateTimeOffset.FromUnixTimeMilliseconds(p.Ts).UtcDateTime;
        var diff = DateTime.UtcNow - lastSeen;
        if (diff.TotalMinutes < 1) return Loc.T("Now");
        if (diff.TotalMinutes < 60) return Loc.T("N_Minutes_Ago", (int)diff.TotalMinutes);
        if (diff.TotalHours < 24) return Loc.T("N_Hours_Ago", (int)diff.TotalHours);
        if (diff.TotalDays < 7) return Loc.T("N_Days_Ago", (int)diff.TotalDays);
        return lastSeen.ToString("yyyy-MM-dd");
    }

    /// <summary>Closes stale sessions while retaining their heartbeat history.</summary>
    public async Task<int> ClearOldAsync(int olderThanDays = 30)
    {
        if (olderThanDays < 1) throw new ArgumentOutOfRangeException(nameof(olderThanDays));
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
            await using var db = await _sqlFactory.CreateDbContextAsync();
            return await db.PresenceSessions
                .Where(x => x.Status == 0 && x.LastSeenAtUtc < cutoff)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, (byte)1)
                    .SetProperty(x => x.EndedAtUtc, x => x.LastSeenAtUtc));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Presence] ClearOld failed");
            return 0;
        }
    }
}
