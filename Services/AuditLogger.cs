using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Persists audit entries to the database. Implementation of <see cref="IAuditLogger"/>.
/// </summary>
public class AuditLogger : IAuditLogger
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly ILogger<AuditLogger> _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogger"/> class.
    /// </summary>
    public AuditLogger(IDbContextFactory<AppDbContext> sqlFactory, ILogger<AuditLogger> log)
    {
        _sqlFactory = sqlFactory;
        _log = log;
    }

    /// <summary>
    /// Logs the operation on the audit logger.
    /// </summary>
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

            var metadataJson = SerializeMetadataWithCorrelation(metadata);

            int? apartmentNumber = null;
            if (!string.IsNullOrWhiteSpace(effectiveBuildingId) && !string.IsNullOrWhiteSpace(apartmentId))
            {
                apartmentNumber = await db.Apartments.AsNoTracking()
                    .Where(a => a.BuildingId == effectiveBuildingId && a.Id == apartmentId)
                    .Select(a => (int?)a.Number).FirstOrDefaultAsync();
            }

            var severityValue = (severity ?? "info").Trim().ToLowerInvariant();
            var normalizedSeverity = new[] { "info", "warning", "error", "critical" }
                .Contains(severityValue) ? severityValue : "info";

            db.AuditLog.Add(new AuditLogEntity
            {
                BuildingId = effectiveBuildingId,
                ApartmentId = apartmentNumber.HasValue ? apartmentId : null,
                AptNumber = apartmentNumber,
                Severity = normalizedSeverity,
                Ts = DateTime.UtcNow,
                Action = Cut(action, 100),
                Label = Cut(action, 300),
                Actor = Cut(userId ?? "", 200),
                ActorRole = Cut(userRole ?? "", 20),
                Details = metadataJson ?? "",
                MonthKey = DateTime.UtcNow.ToString("yyyy-MM"),
                SourceId = null,
                BuildingNumber = null
            });

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "AuditLogger failed for {Action}", action);
        }
    }

    /// <summary>
    /// Returns the query.
    /// </summary>
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

        if (!string.IsNullOrWhiteSpace(apartmentId))
            query = query.Where(x => x.ApartmentId == apartmentId);

        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(x => x.Severity == severity);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(x => x.Action == action);

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(x => x.Actor == userId);

        if (from.HasValue)
            query = query.Where(x => x.Ts >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.Ts <= to.Value);

        limit = Math.Clamp(limit, 1, 50000);
        var entries = await query
            .OrderByDescending(x => x.Ts)
            .Take(limit)
            .ToListAsync();

        return entries.Select(ToDoc).ToList();
    }
    /// <summary>
    /// يسترجع صفحة من سجلات التدقيق حسب معايير التصفية.
    /// </summary>
    /// <param name="buildingId">معايير التصفية والترقيم.</param>
    /// <param name="apartmentId"></param>
    /// <param name="userId"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="action"></param>
    /// <param name="severity"></param>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    /// <returns>صفحة من سجلات التدقيق.</returns>
    public async Task<BuildingManagementMvc.Models.PagedResult<AuditEntryDoc>> QueryPageAsync(
        string? buildingId, string? apartmentId, string? userId, DateTime? from, DateTime? to,
        string? action, string? severity, int page, int pageSize)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var query = db.AuditLog.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(buildingId)) query = query.Where(x => x.BuildingId == buildingId);
        if (!string.IsNullOrWhiteSpace(apartmentId)) query = query.Where(x => x.ApartmentId == apartmentId);
        if (!string.IsNullOrWhiteSpace(userId)) query = query.Where(x => x.Actor == userId);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action);
        if (!string.IsNullOrWhiteSpace(severity)) query = query.Where(x => x.Severity == severity);
        if (from.HasValue) query = query.Where(x => x.Ts >= from.Value);
        if (to.HasValue) query = query.Where(x => x.Ts <= to.Value);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, totalPages);
        var rows = await query.OrderByDescending(x => x.Ts).ThenByDescending(x => x.AuditId)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new BuildingManagementMvc.Models.PagedResult<AuditEntryDoc>
        {
            Items = rows.Select(ToDoc).ToList(), CurrentPage = page, PageSize = pageSize, TotalItems = total
        };
    }

    private static AuditEntryDoc ToDoc(AuditLogEntity e) => new()
    {
        Id = e.SourceId ?? e.AuditId.ToString(),
        Action = e.Action,
        BuildingId = e.BuildingId,
        ApartmentId = e.ApartmentId ?? e.AptNumber?.ToString(),
        UserId = e.Actor,
        UserRole = e.ActorRole,
        Severity = string.IsNullOrWhiteSpace(e.Severity) ? "info" : e.Severity,
        Details = e.Details,
        CreatedAt = DateTime.SpecifyKind(e.Ts, DateTimeKind.Utc)
    };

    private static string? SerializeMetadataWithCorrelation(object? metadata)
    {
        var correlationId = Activity.Current?.GetTagItem(CorrelationIdMiddleware.ActivityTagName)?.ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
            return metadata == null ? null : JsonConvert.SerializeObject(metadata);

        var token = metadata == null ? new JObject() : JToken.FromObject(metadata);
        var envelope = token as JObject ?? new JObject { ["value"] = token };
        envelope["correlationId"] = correlationId;
        return envelope.ToString(Formatting.None);
    }

    private static string Cut(string? s, int max)
    {
        s ??= "";
        return s.Length <= max ? s : s[..max];
    }
}