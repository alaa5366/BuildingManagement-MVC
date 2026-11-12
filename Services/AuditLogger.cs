using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace BuildingManagementMvc.Services;

public class AuditLogger : IAuditLogger
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly ILogger<AuditLogger> _log;

    public AuditLogger(IDbContextFactory<AppDbContext> sqlFactory, ILogger<AuditLogger> log)
    {
        _sqlFactory = sqlFactory;
        _log = log;
    }

    public async Task LogAsync(
        string action,
        string? buildingId = null,
        string? apartmentId = null,
        string? userId = null,
        string? userRole = null,
        object? metadata = null,
        string severity = "info")
    {
        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();

            string? effectiveBuildingId = buildingId;
            if (!string.IsNullOrWhiteSpace(buildingId))
            {
                var exists = await db.Buildings.AnyAsync(b => b.Id == buildingId);
                if (!exists) effectiveBuildingId = null;
            }

            var metadataJson = metadata == null
                ? null
                : JsonConvert.SerializeObject(metadata);

            db.AuditLog.Add(new AuditLogEntity
            {
                BuildingId = effectiveBuildingId,
                Ts = DateTime.UtcNow,
                Action = Cut(action, 100),
                Label = Cut(action, 300),
                Actor = Cut(userId ?? "", 200),
                ActorRole = Cut(userRole ?? "", 20),
                Details = metadataJson ?? "",
                MonthKey = DateTime.UtcNow.ToString("yyyy-MM"),
                SourceId = null,
                BuildingNumber = null,
                AptNumber = null
            });

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "AuditLogger failed for {Action}", action);
        }
    }

    public async Task<List<AuditEntryDoc>> QueryAsync(
        string? buildingId = null,
        string? apartmentId = null,
        string? userId = null,
        DateTime? from = null,
        DateTime? to = null,
        string? action = null,
        string? severity = null,
        int limit = 100)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var query = db.AuditLog.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(buildingId))
            query = query.Where(x => x.BuildingId == buildingId);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(x => x.Action == action);

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(x => x.Actor == userId);

        if (from.HasValue)
            query = query.Where(x => x.Ts >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.Ts <= to.Value);

        var entries = await query
            .OrderByDescending(x => x.Ts)
            .Take(limit)
            .ToListAsync();

        return entries.Select(ToDoc).ToList();
    }

    private static AuditEntryDoc ToDoc(AuditLogEntity e) => new()
    {
        Id = e.SourceId ?? e.AuditId.ToString(),
        Action = e.Action,
        BuildingId = e.BuildingId,
        ApartmentId = e.AptNumber?.ToString(),
        UserId = e.Actor,
        UserRole = e.ActorRole,
        Severity = "info",
        Details = e.Details,
        CreatedAt = DateTime.SpecifyKind(e.Ts, DateTimeKind.Utc)
    };

    private static string Cut(string? s, int max)
    {
        s ??= "";
        return s.Length <= max ? s : s[..max];
    }
}