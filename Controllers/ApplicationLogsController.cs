using System.Globalization;
using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using BuildingManagementMvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin,admin")]
[AdminPermission(AdminPermissions.ViewApplicationLogs)]
public sealed class ApplicationLogsController : Controller
{
    private readonly ApplicationLogQueryService _logs;

    public ApplicationLogsController(ApplicationLogQueryService logs) => _logs = logs;

    [HttpGet]
    public async Task<IActionResult> Index(
        string? from, string? to, string? level, string? search,
        string? correlationId, string? source, bool? hasException,
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
            HasException = hasException,
            Page = Math.Clamp(page, 1, 100),
            PageSize = Math.Clamp(pageSize, 10, 100)
        };

        if (query.FromUtc.HasValue && query.ToUtc.HasValue &&
            query.FromUtc.Value > query.ToUtc.Value)
            ModelState.AddModelError(nameof(from), Loc.T("Invalid_Date_Range"));

        var results = await _logs.QueryPageAsync(query, HttpContext.RequestAborted);
        return View(new ApplicationLogsViewModel { Results = results, Query = query });
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
