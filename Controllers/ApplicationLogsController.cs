using System.Globalization;
using System.Text;
using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using BuildingManagementMvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BuildingManagementMvc.Controllers;

/// <summary>Provides an authorized, read-only interface for searching retained application logs.</summary>
[Authorize(Roles = "superadmin,admin")]
[AdminPermission(AdminPermissions.ViewApplicationLogs)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ApplicationLogsController : Controller
{
    private readonly ApplicationLogQueryService _logs;

    public ApplicationLogsController(ApplicationLogQueryService logs) => _logs = logs;

    /// <summary>Displays a filtered page of sanitized application log events.</summary>
    [HttpGet]
    [EnableRateLimiting("lookup")]
    public async Task<IActionResult> Index(
        string? from, string? to, string? level, string? search,
        string? correlationId, string? source, string? componentType, bool? hasException,
        int? httpStatusCode, double? minDurationMs, double? maxDurationMs,
        int page = 1, int pageSize = 25)
    {
        var query = new ApplicationLogQuery
        {
            FromUtc = ParseDate(from, false),
            ToUtc = ParseDate(to, true),
            Level = level,
            Search = search,
            CorrelationId = correlationId,
            Source = source,
            ComponentType = componentType,
            HasException = hasException,
            HttpStatusCode = httpStatusCode,
            MinDurationMs = minDurationMs,
            MaxDurationMs = maxDurationMs,
            Page = Math.Clamp(page, 1, 100),
            PageSize = Math.Clamp(pageSize, 10, 100)
        };

        ViewBag.CanViewAuditTrail = User.IsInRole("superadmin") ||
            User.HasClaim("perm", AdminPermissions.ViewAuditLogBuilding);

        if (query.HttpStatusCode is < 100 or > 599 ||
            query.MinDurationMs is < 0 or > 3600000 ||
            query.MaxDurationMs is < 0 or > 3600000 ||
            (query.MinDurationMs.HasValue && query.MaxDurationMs.HasValue && query.MinDurationMs.Value > query.MaxDurationMs.Value))
        {
            ModelState.AddModelError(nameof(httpStatusCode), "HTTP status and duration filters are outside the supported range.");
            return View(new ApplicationLogsViewModel
            {
                Query = query,
                Results = new PagedResult<ApplicationLogEntry> { CurrentPage = query.Page, PageSize = query.PageSize }
            });
        }

        if (query.FromUtc.HasValue && query.ToUtc.HasValue &&
            query.FromUtc.Value > query.ToUtc.Value)
        {
            ModelState.AddModelError(nameof(from), Loc.T("Invalid_Date_Range"));
            return View(new ApplicationLogsViewModel
            {
                Query = query,
                Results = new PagedResult<ApplicationLogEntry>
                {
                    CurrentPage = query.Page,
                    PageSize = query.PageSize
                }
            });
        }

        var results = await _logs.QueryPageAsync(query, HttpContext.RequestAborted);
        return View(new ApplicationLogsViewModel { Results = results, Query = query });
    }

    /// <summary>Exports the currently filtered page as a sanitized CSV file.</summary>
    [HttpGet]
    [EnableRateLimiting("lookup")]
    public async Task<IActionResult> ExportCsv(
        string? from, string? to, string? level, string? search,
        string? correlationId, string? source, string? componentType, bool? hasException,
        int? httpStatusCode, double? minDurationMs, double? maxDurationMs,
        int page = 1, int pageSize = 100)
    {
        var query = new ApplicationLogQuery
        {
            FromUtc = ParseDate(from, false),
            ToUtc = ParseDate(to, true),
            Level = level,
            Search = search,
            CorrelationId = correlationId,
            Source = source,
            ComponentType = componentType,
            HasException = hasException,
            HttpStatusCode = httpStatusCode,
            MinDurationMs = minDurationMs,
            MaxDurationMs = maxDurationMs,
            Page = Math.Clamp(page, 1, 100),
            PageSize = Math.Clamp(pageSize, 10, 100)
        };

        if (query.HttpStatusCode is < 100 or > 599 ||
            query.MinDurationMs is < 0 or > 3600000 ||
            query.MaxDurationMs is < 0 or > 3600000 ||
            (query.MinDurationMs.HasValue && query.MaxDurationMs.HasValue &&
             query.MinDurationMs.Value > query.MaxDurationMs.Value) ||
            (query.FromUtc.HasValue && query.ToUtc.HasValue && query.FromUtc.Value > query.ToUtc.Value))
            return BadRequest("Invalid log export filters.");

        var result = await _logs.QueryPageAsync(query, HttpContext.RequestAborted);
        var csv = new StringBuilder();
        csv.AppendLine("TimestampUtc,Level,Message,Exception,Details,SourceContext,CorrelationId,HttpMethod,RequestPath,HttpStatusCode,DurationMs");
        foreach (var item in result.Items)
        {
            var fields = new[]
            {
                item.Timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                item.Level,
                item.Message,
                item.Exception,
                item.Details,
                item.SourceContext,
                item.CorrelationId,
                item.HttpMethod,
                item.RequestPath,
                item.HttpStatusCode?.ToString(CultureInfo.InvariantCulture) ?? "",
                item.DurationMs?.ToString("0.###", CultureInfo.InvariantCulture) ?? ""
            };
            csv.AppendLine(string.Join(",", fields.Select(EscapeCsv)));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"application-logs-page-{result.CurrentPage}.csv");
    }

    private static string EscapeCsv(string? value)
    {
        value ??= "";
        var trimmed = value.TrimStart();
        if (trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static DateTimeOffset? ParseDate(string? value, bool endOfDay)
    {
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date))
            return null;

        var utc = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return endOfDay ? utc.AddDays(1).AddTicks(-1) : utc;
    }
}
