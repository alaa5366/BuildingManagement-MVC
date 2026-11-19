using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace BuildingManagementMvc.Services;

/// <summary>Persists immutable business audit events and provides bounded, indexed queries.</summary>
public class AuditLogger : IAuditLogger
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly ILogger<AuditLogger> _log;

    public AuditLogger(IDbContextFactory<AppDbContext> sqlFactory, ILogger<AuditLogger> log)
    {
        _sqlFactory = sqlFactory;
        _log = log;
    }

    /// <summary>Writes an audit event asynchronously.</summary>
    public async Task LogAsync(
        string action, string? buildingId = null, string? apartmentId = null,
        string? userId = null, string? userRole = null, object? metadata = null,
        string severity = "info")
    {
        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();
            var effectiveBuildingId = buildingId;
            if (!string.IsNullOrWhiteSpace(buildingId) &&
                !await db.Buildings.AnyAsync(x => x.Id == buildingId))
                effectiveBuildingId = null;

            var severityValue = (severity ?? "info").Trim().ToLowerInvariant();
            var outcome = severityValue switch
            {
                "warning" => (byte)1,
                "error" => (byte)2,
                "critical" => (byte)3,
                _ => (byte)0
            };

            var correlationId = Activity.Current?
                .GetTagItem(CorrelationIdMiddleware.ActivityTagName)?.ToString();
            var details = SerializeSafeMetadata(metadata, userRole, correlationId);
            db.AuditEvents.Add(new AuditEventEntity
            {
                AuditEventId = Guid.NewGuid(),
                OccurredAtUtc = DateTime.UtcNow,
                ActorAccountId = Guid.TryParse(userId, out var accountId) ? accountId : null,
                ActorReference = Cut(userId, 160),
                BuildingId = effectiveBuildingId,
                EventCategory = "Business",
                EventType = Cut(action, 120) ?? string.Empty,
                ResourceType = string.IsNullOrWhiteSpace(apartmentId) ? null : "Apartment",
                ResourceReference = Cut(apartmentId, 200),
                CorrelationId = Cut(correlationId, 100),
                Outcome = outcome,
                SafeDetailsJson = details
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "AuditLogger failed for {Action}", action);
        }
    }

    /// <summary>Queries audit events using the supplied filters.</summary>
    public async Task<List<AuditEntryDoc>> QueryAsync(
        string? buildingId = null, string? apartmentId = null, string? userId = null,
        DateTime? from = null, DateTime? to = null, string? action = null,
        string? severity = null, int limit = 100, string? correlationId = null)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var query = ApplyFilters(db.AuditEvents.AsNoTracking(), buildingId, apartmentId,
            userId, from, to, action, severity, correlationId);
        var rows = await query.OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.AuditEventId)
            .Take(Math.Clamp(limit, 1, 50000)).ToListAsync();
        return rows.Select(ToDoc).ToList();
    }

    /// <summary>Retrieves a filtered page of audit events.</summary>
    public async Task<BuildingManagementMvc.Models.PagedResult<AuditEntryDoc>> QueryPageAsync(
        string? buildingId, string? apartmentId, string? userId, DateTime? from, DateTime? to,
        string? action, string? severity, int page, int pageSize, string? correlationId = null)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var query = ApplyFilters(db.AuditEvents.AsNoTracking(), buildingId, apartmentId,
            userId, from, to, action, severity, correlationId);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var countTimer = Stopwatch.StartNew();
        var total = await query.CountAsync();
        countTimer.Stop();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, totalPages);
        var pageTimer = Stopwatch.StartNew();
        var rows = await query.OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.AuditEventId)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        pageTimer.Stop();
        if (countTimer.ElapsedMilliseconds >= 250 || pageTimer.ElapsedMilliseconds >= 250)
        {
            _log.LogWarning(
                "AUDIT PERF: Count={CountMs}ms PageQuery={PageMs}ms Rows={RowCount} Total={Total}",
                countTimer.ElapsedMilliseconds,
                pageTimer.ElapsedMilliseconds,
                rows.Count,
                total);
        }

        return new BuildingManagementMvc.Models.PagedResult<AuditEntryDoc>
        {
            Items = rows.Select(ToDoc).ToList(),
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    private static IQueryable<AuditEventEntity> ApplyFilters(
        IQueryable<AuditEventEntity> query, string? buildingId, string? apartmentId,
        string? userId, DateTime? from, DateTime? to, string? action, string? severity, string? correlationId = null)
    {
        if (!string.IsNullOrWhiteSpace(buildingId))
            query = query.Where(x => x.BuildingId == buildingId);
        if (!string.IsNullOrWhiteSpace(apartmentId))
            query = query.Where(x => x.ResourceType == "Apartment" && x.ResourceReference == apartmentId);
        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(x => x.ActorReference == userId);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(x => x.EventType == action);
        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(x => x.CorrelationId != null && x.CorrelationId == correlationId);
        if (!string.IsNullOrWhiteSpace(severity))
        {
            var outcome = severity.Trim().ToLowerInvariant() switch
            {
                "info" => (byte)0,
                "warning" => (byte)1,
                "error" => (byte)2,
                "critical" => (byte)3,
                _ => byte.MaxValue
            };
            query = query.Where(x => x.Outcome == outcome);
        }
        if (from.HasValue) query = query.Where(x => x.OccurredAtUtc >= from.Value);
        if (to.HasValue) query = query.Where(x => x.OccurredAtUtc <= to.Value);
        return query;
    }

    private static AuditEntryDoc ToDoc(AuditEventEntity e)
    {
        Dictionary<string, object>? metadata = null;
        if (!string.IsNullOrWhiteSpace(e.SafeDetailsJson))
        {
            try
            {
                metadata = JObject.Parse(e.SafeDetailsJson).ToObject<Dictionary<string, object>>();
            }
            catch (JsonException)
            {
                metadata = new Dictionary<string, object> { ["details"] = e.SafeDetailsJson };
            }
        }
        return new AuditEntryDoc
        {
            Id = e.AuditEventId.ToString("N"),
            Action = e.EventType,
            BuildingId = e.BuildingId,
            ApartmentId = e.ResourceType == "Apartment" ? e.ResourceReference : null,
            UserId = e.ActorReference,
            CorrelationId = e.CorrelationId,
            UserRole = ReadActorRole(e.SafeDetailsJson),
            Severity = e.Outcome switch { 1 => "warning", 2 => "error", 3 => "critical", _ => "info" },
            Metadata = metadata,
            Details = e.SafeDetailsJson,
            CreatedAt = DateTime.SpecifyKind(e.OccurredAtUtc, DateTimeKind.Utc),
            Timestamp = DateTime.SpecifyKind(e.OccurredAtUtc, DateTimeKind.Utc)
        };
    }

    private static string? SerializeSafeMetadata(object? metadata, string? actorRole, string? correlationId)
    {
        if (metadata == null && string.IsNullOrWhiteSpace(actorRole) &&
            string.IsNullOrWhiteSpace(correlationId)) return null;
        var token = metadata == null ? new JObject() : JToken.FromObject(metadata);
        var envelope = token as JObject ?? new JObject { ["value"] = token };
        envelope["actorRole"] = actorRole;
        envelope["correlationId"] = correlationId;
        RedactSensitiveValues(envelope);
        return envelope.ToString(Formatting.None);
    }

    private static string? ReadActorRole(string? details)
    {
        if (string.IsNullOrWhiteSpace(details)) return null;
        try { return JObject.Parse(details)["actorRole"]?.Value<string>(); }
        catch (JsonException) { return null; }
    }

    private static void RedactSensitiveValues(JToken token)
    {
        if (token is JObject obj)
        {
            foreach (var property in obj.Properties().ToList())
            {
                var key = property.Name;
                if (key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("credential", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("pin", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("recovery", StringComparison.OrdinalIgnoreCase))
                    property.Value = "[REDACTED]";
                else
                    RedactSensitiveValues(property.Value);
            }
        }
        else if (token is JArray array)
        {
            foreach (var item in array) RedactSensitiveValues(item);
        }
    }

    private static string? Cut(string? value, int max) =>
        string.IsNullOrEmpty(value) ? value : value.Length <= max ? value : value[..max];
}
