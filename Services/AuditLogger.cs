using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;

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
        var skip = (long)(page - 1) * pageSize;
        var requiredRows = skip + pageSize;

        var orderedQuery = query.OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.AuditEventId);
        List<AuditEventEntity> rows;
        if (db.Database.IsSqlServer() && skip <= 10000)
        {
            // Parameterized TOP/OFFSET plans were measured at ~25 seconds on this
            // SQL Server workload. Literal TOP chunks of 100 rows stay fast. Use
            // keyset continuation for deeper pages so no query needs a large TOP
            // value or OFFSET/FETCH; every user value remains a SQL parameter.
            async Task<List<AuditEventEntity>> FetchSqlPage(
                int take, DateTime? afterUtc, Guid? afterAuditEventId)
            {
                var sql = new StringBuilder(
                    $"SELECT TOP ({take.ToString(CultureInfo.InvariantCulture)}) * FROM dbo.AuditEvents");
                var predicates = new List<string>();
                var parameters = new List<SqlParameter>();

                void AddTextFilter(string column, string parameterName, string? value, int maxLength)
                {
                    if (string.IsNullOrWhiteSpace(value)) return;
                    predicates.Add($"[{column}] = {parameterName}");
                    parameters.Add(new SqlParameter(parameterName, SqlDbType.NVarChar, maxLength) { Value = value });
                }

                AddTextFilter("BuildingId", "@buildingId", buildingId, 64);
                if (!string.IsNullOrWhiteSpace(apartmentId))
                {
                    predicates.Add("[ResourceType] = N'Apartment'");
                    AddTextFilter("ResourceReference", "@apartmentId", apartmentId, 200);
                }
                AddTextFilter("ActorReference", "@userId", userId, 160);
                AddTextFilter("EventType", "@action", action, 120);
                AddTextFilter("CorrelationId", "@correlationId", correlationId, 100);

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
                    predicates.Add("[Outcome] = @outcome");
                    parameters.Add(new SqlParameter("@outcome", SqlDbType.TinyInt) { Value = outcome });
                }

                if (from.HasValue)
                {
                    predicates.Add("[OccurredAtUtc] >= @fromUtc");
                    parameters.Add(new SqlParameter("@fromUtc", SqlDbType.DateTime2) { Scale = 3, Value = from.Value });
                }
                if (to.HasValue)
                {
                    predicates.Add("[OccurredAtUtc] <= @toUtc");
                    parameters.Add(new SqlParameter("@toUtc", SqlDbType.DateTime2) { Scale = 3, Value = to.Value });
                }

                if (afterUtc.HasValue && afterAuditEventId.HasValue)
                {
                    predicates.Add("([OccurredAtUtc] < @afterUtc OR ([OccurredAtUtc] = @afterUtc AND [AuditEventId] < @afterAuditEventId))");
                    parameters.Add(new SqlParameter("@afterUtc", SqlDbType.DateTime2) { Scale = 3, Value = afterUtc.Value });
                    parameters.Add(new SqlParameter("@afterAuditEventId", SqlDbType.UniqueIdentifier) { Value = afterAuditEventId.Value });
                }

                if (predicates.Count > 0)
                    sql.Append(" WHERE ").Append(string.Join(" AND ", predicates));
                sql.Append(" ORDER BY [OccurredAtUtc] DESC, [AuditEventId] DESC;");

                return await db.AuditEvents
                    .FromSqlRaw(sql.ToString(), parameters.Cast<object>().ToArray())
                    .AsNoTracking()
                    .ToListAsync();
            }

            if (requiredRows <= 100)
            {
                var orderedRows = await FetchSqlPage((int)requiredRows, null, null);
                rows = orderedRows.Skip((int)skip).Take(pageSize).ToList();
            }
            else
            {
                var remaining = skip;
                DateTime? cursorUtc = null;
                Guid? cursorAuditEventId = null;

                while (remaining > 0)
                {
                    var chunkSize = (int)Math.Min(100, remaining);
                    var boundaryRows = await FetchSqlPage(chunkSize, cursorUtc, cursorAuditEventId);
                    if (boundaryRows.Count == 0)
                        break;

                    var boundary = boundaryRows[^1];
                    cursorUtc = boundary.OccurredAtUtc;
                    cursorAuditEventId = boundary.AuditEventId;
                    remaining -= boundaryRows.Count;

                    if (boundaryRows.Count < chunkSize)
                        break;
                }

                rows = remaining == 0
                    ? await FetchSqlPage(pageSize, cursorUtc, cursorAuditEventId)
                    : new List<AuditEventEntity>();
            }
        }
        else if (requiredRows <= 10000)
        {
            var orderedRows = await orderedQuery.Take((int)requiredRows).ToListAsync();
            rows = orderedRows.Skip((int)skip).Take(pageSize).ToList();
        }
        else
        {
            rows = await orderedQuery.Skip((int)Math.Min(skip, int.MaxValue))
                .Take(pageSize).ToListAsync();
        }
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

        var items = rows.Select(ToDoc).ToList();
        await EnrichDepositReceiptLinksAsync(db, items);

        return new BuildingManagementMvc.Models.PagedResult<AuditEntryDoc>
        {
            Items = items,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    private static async Task EnrichDepositReceiptLinksAsync(
        AppDbContext db,
        List<AuditEntryDoc> entries)
    {
        var references = new List<(AuditEntryDoc Entry, Guid DepositId)>();
        foreach (var entry in entries)
        {
            if (!entry.Action.StartsWith("PaymentDeposit", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(entry.Details))
                continue;

            try
            {
                var details = JObject.Parse(entry.Details);
                if (Guid.TryParse(details["paymentDepositId"]?.ToString(), out var depositId))
                    references.Add((entry, depositId));
            }
            catch (JsonException)
            {
                // Malformed historical metadata must not break the audit page.
            }
        }

        var depositIds = references.Select(item => item.DepositId).Distinct().ToList();
        if (depositIds.Count == 0)
            return;

        var deposits = await db.PaymentDeposits.AsNoTracking()
            .Where(deposit => depositIds.Contains(deposit.PaymentDepositId))
            .Select(deposit => new
            {
                deposit.PaymentDepositId,
                deposit.BuildingId,
                deposit.ApartmentId,
                deposit.ReceiptMetadataJson
            })
            .ToListAsync();

        foreach (var reference in references)
        {
            var deposit = deposits.FirstOrDefault(item =>
                item.PaymentDepositId == reference.DepositId &&
                item.BuildingId == reference.Entry.BuildingId &&
                item.ApartmentId == reference.Entry.ApartmentId);
            if (deposit is null || string.IsNullOrWhiteSpace(deposit.ReceiptMetadataJson))
                continue;

            try
            {
                var receipt = JObject.Parse(deposit.ReceiptMetadataJson);
                var url = receipt["Url"]?.Value<string>();
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                    uri.Scheme != Uri.UriSchemeHttps)
                    continue;

                reference.Entry.Metadata ??= new Dictionary<string, object>();
                reference.Entry.Metadata["receiptUrl"] = uri.AbsoluteUri;
            }
            catch (JsonException)
            {
                // A damaged receipt must not hide or fail its audit event.
            }
        }
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
