using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Tracks and reports which users are online.
/// </summary>
public class PresenceService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly ILogger<PresenceService> _logger;

    /// <summary>
    /// The online threshold field.
    /// </summary>
    public static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Initializes a new instance of the <see cref="PresenceService"/> class.
    /// </summary>
    public PresenceService(IDbContextFactory<AppDbContext> sqlFactory, ILogger<PresenceService> logger)
    {
        _sqlFactory = sqlFactory;
        _logger = logger;
    }

    /// <summary>
    /// Builds the document ID.
    /// </summary>
    public static string BuildDocId(string uid, string aptId, string buildingId)
    {
        return !string.IsNullOrEmpty(buildingId) && !string.IsNullOrEmpty(aptId)
            ? $"{buildingId}_{aptId}"
            : uid;
    }

    /// <summary>
    /// Stores the last-seen time of a user.
    /// </summary>
    public async Task HeartbeatAsync(string uid, string aptId, string buildingId)
    {
        if (string.IsNullOrEmpty(uid)) return;

        var docId = BuildDocId(uid, aptId, buildingId);

        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();
            var existing = await db.Presence.FirstOrDefaultAsync(p => p.Uid == docId);

            if (existing == null)
            {
                db.Presence.Add(new PresenceEntity
                {
                    Uid = docId,
                    ApartmentId = string.IsNullOrWhiteSpace(aptId) ? null : aptId,
                    BuildingId = string.IsNullOrWhiteSpace(buildingId) ? null : buildingId,
                    LastSeen = DateTime.UtcNow
                });
            }
            else
            {
                existing.LastSeen = DateTime.UtcNow;
                existing.BuildingId = buildingId ?? existing.BuildingId;
                existing.ApartmentId = aptId ?? existing.ApartmentId;
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Presence] Heartbeat failed for {DocId}", docId);
        }
    }

    /// <summary>
    /// Gets the online count.
    /// </summary>
    public async Task<int> GetOnlineCountAsync(string buildingId)
    {
        try
        {
            var cutoff = DateTime.UtcNow.Subtract(OnlineThreshold);
            await using var db = await _sqlFactory.CreateDbContextAsync();
            return await db.Presence
                .CountAsync(p => p.BuildingId == buildingId && p.LastSeen >= cutoff);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Presence] GetOnlineCount failed");
            return 0;
        }
    }

    /// <summary>
    /// Gets the by building.
    /// </summary>
    public async Task<Dictionary<string, Presence>> GetByBuildingAsync(string buildingId)
    {
        var result = new Dictionary<string, Presence>();

        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();
            var list = await db.Presence.AsNoTracking()
                .Where(p => p.BuildingId == buildingId)
                .ToListAsync();

            foreach (var p in list)
            {
                var aptId = p.ApartmentId ?? "";
                if (string.IsNullOrEmpty(aptId)) continue;

                result[aptId] = new Presence
                {
                    Uid = p.Uid,
                    AptId = aptId,
                    BuildingId = p.BuildingId ?? "",
                    LastSeen = p.LastSeen.ToString("o"),
                    Ts = new DateTimeOffset(p.LastSeen, TimeSpan.Zero).ToUnixTimeMilliseconds()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Presence] GetByBuilding failed");
        }

        return result;
    }

    /// <summary>
    /// Determines whether the value is online.
    /// </summary>
    public static bool IsOnline(Presence? p)
    {
        if (p == null || p.Ts <= 0) return false;
        var lastSeen = DateTimeOffset.FromUnixTimeMilliseconds(p.Ts).UtcDateTime;
        return DateTime.UtcNow - lastSeen <= OnlineThreshold;
    }

    /// <summary>
    /// Determines whether the value is recent.
    /// </summary>
    public static bool IsRecent(Presence? p)
    {
        if (p == null || p.Ts <= 0) return false;
        var lastSeen = DateTimeOffset.FromUnixTimeMilliseconds(p.Ts).UtcDateTime;
        return DateTime.UtcNow - lastSeen < TimeSpan.FromHours(24);
    }

    /// <summary>
    /// Returns the time ago.
    /// </summary>
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

    /// <summary>
    /// Clears the old.
    /// </summary>
    public async Task<int> ClearOldAsync(int olderThanDays = 30)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
            await using var db = await _sqlFactory.CreateDbContextAsync();
            return await db.Presence
                .Where(p => p.LastSeen < cutoff)
                .ExecuteDeleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Presence] ClearOld failed");
            return 0;
        }
    }
}