using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Controllers;

/// <summary>
/// Shows and exports the audit log. The scope (all buildings, one building, one apartment) depends on the signed-in role.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>superadmin,admin,resident</c>.
/// </remarks>
[Authorize(Roles = "superadmin,admin,resident")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuditLogController : Controller
{
    private readonly AuditLogQueryService _audit;
    private readonly BuildingsService _buildings;
    private readonly ILogger<AuditLogController>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogController"/> class.
    /// </summary>
    public AuditLogController(
        AuditLogQueryService audit,
        BuildingsService buildings,
        ILogger<AuditLogController>? logger = null)
    {
        _audit = audit;
        _buildings = buildings;
        _logger = logger;
    }

    private string? CurrentBuildingId => User.FindFirst("buildingId")?.Value;
    private string? CurrentApartmentId => User.FindFirst("apartmentId")?.Value;
    private bool IsSuperAdmin => User.IsInRole("superadmin");
    private bool IsAdmin => User.IsInRole("admin");
    private bool IsResident => User.IsInRole("resident");

    private bool HasAuditLogPermission => HasPermissionForUser(User);

    /// <summary>
    /// Determines whether the specified user can access audit-log data based on their role and permission claim.
    /// </summary>
    /// <param name="user">The authenticated user whose audit-log access is being evaluated.</param>
    /// <returns><see langword="true"/> when the user has the role-scoped audit-log permission; otherwise, <see langword="false"/>.</returns>
    public static bool HasPermissionForUser(System.Security.Claims.ClaimsPrincipal user) =>
        user.IsInRole("superadmin") ||
        (user.IsInRole("admin") && user.HasClaim("perm", AdminPermissions.ViewAuditLogBuilding)) ||
        (user.IsInRole("resident") && user.HasClaim("perm", AdminPermissions.ViewAuditLogOwn));

    /// <summary>
    /// Shows the main page of the audit log screen.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    [EnableRateLimiting("lookup")]
    public async Task<IActionResult> Index(
    string? buildingId, string? apartmentId, string? userId,
    string? actionFilter, string? severity, string? from, string? to,
    int page = 1, int pageSize = 50, string? correlationId = null)
    {
        var actionTimer = Stopwatch.StartNew();
        if (!HasAuditLogPermission) return Forbid();
        if ((IsResident && (string.IsNullOrWhiteSpace(CurrentBuildingId) ||
                            string.IsNullOrWhiteSpace(CurrentApartmentId) ||
                            string.IsNullOrWhiteSpace(User.FindFirstValue(ClaimTypes.NameIdentifier)))) ||
            (IsAdmin && string.IsNullOrWhiteSpace(CurrentBuildingId))) return Forbid();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        correlationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        if (correlationId?.Length > 100) correlationId = correlationId[..100];
        string? qBuildingId = null;
        string? qApartmentId = null;
        string? qUserId = null;

        if (IsResident)
        {
            qBuildingId = CurrentBuildingId;
            qApartmentId = CurrentApartmentId;
            qUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.CanFilterByBuilding = false;
            ViewBag.CanFilterByApartment = false;
            ViewBag.CanFilterByUser = false;
        }
        else if (IsAdmin)
        {
            qBuildingId = CurrentBuildingId;
            if (!string.IsNullOrWhiteSpace(apartmentId))
                qApartmentId = apartmentId;
            qUserId = string.IsNullOrWhiteSpace(userId) ? null : userId;
            ViewBag.CanFilterByBuilding = false;
            ViewBag.CanFilterByApartment = true;
            ViewBag.CanFilterByUser = true;
        }
        else if (IsSuperAdmin)
        {
            qBuildingId = string.IsNullOrWhiteSpace(buildingId) ? null : buildingId;
            qApartmentId = string.IsNullOrWhiteSpace(apartmentId) ? null : apartmentId;
            qUserId = string.IsNullOrWhiteSpace(userId) ? null : userId;
            ViewBag.CanFilterByBuilding = true;
            ViewBag.CanFilterByApartment = true;
            ViewBag.CanFilterByUser = true;
        }

        var queryTimer = Stopwatch.StartNew();
        var pagedResult = await _audit.QueryPageAsync(
            buildingId: qBuildingId,
            apartmentId: qApartmentId,
            userId: qUserId,
            action: string.IsNullOrWhiteSpace(actionFilter) ? null : actionFilter,
            severity: string.IsNullOrWhiteSpace(severity) ? null : severity,
            from: ParseDate(from, endOfDay: false), to: ParseDate(to, endOfDay: true), page: page, pageSize: pageSize, correlationId: correlationId);
        queryTimer.Stop();
        page = pagedResult.CurrentPage;
        pageSize = pagedResult.PageSize;

        var buildingsTimer = Stopwatch.StartNew();
        ViewBag.Buildings = IsSuperAdmin
            ? await _buildings.GetSummariesAsync()
            : new List<Building>();
        buildingsTimer.Stop();

        ViewBag.CurrentRole = IsSuperAdmin ? "superadmin" : (IsAdmin ? "admin" : "resident");
        ViewBag.KnownActions = AuditLogQueryService.KnownActions;
        ViewBag.SelectedAction = actionFilter;
        ViewBag.SelectedSeverity = severity;
        ViewBag.SelectedCorrelationId = correlationId;
        ViewBag.SelectedBuildingId = buildingId;
        ViewBag.SelectedApartmentId = apartmentId;
        ViewBag.SelectedUserId = IsResident ? null : userId;
        ViewBag.From = from;
        ViewBag.To = to;

        // ✅ معلومات الـ Pagination للـ Partial
        ViewBag.PaginationRequest = new PaginationRequest { Page = page, PageSize = pageSize };
        ViewBag.RouteValues = new Dictionary<string, string?>
        {
            ["buildingId"] = buildingId,
            ["apartmentId"] = apartmentId,
            ["userId"] = IsResident ? null : userId,
            ["actionFilter"] = actionFilter,
            ["severity"] = severity,
            ["correlationId"] = correlationId,
            ["from"] = from,
            ["to"] = to,
            ["pageSize"] = pageSize.ToString()
        };

        actionTimer.Stop();
        _logger?.LogInformation(
            "AUDIT PERF: Role={Role} QueryPage={QueryMs}ms Buildings={BuildingsMs}ms Action={ActionMs}ms TotalItems={TotalItems} PageSize={PageSize}",
            IsSuperAdmin ? "superadmin" : IsAdmin ? "admin" : "resident",
            queryTimer.ElapsedMilliseconds,
            buildingsTimer.ElapsedMilliseconds,
            actionTimer.ElapsedMilliseconds,
            pagedResult.TotalItems,
            pagedResult.PageSize);

        return View(pagedResult);
    }

    /// <summary>
    /// Exports the CSV.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    [EnableRateLimiting("lookup")]
    public async Task<IActionResult> ExportCsv(
    string? buildingId, string? apartmentId, string? userId,
    string? actionFilter, string? severity, string? from, string? to, string? correlationId = null)
    {
        if (!HasAuditLogPermission) return Forbid();
        correlationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        if (correlationId?.Length > 100) correlationId = correlationId[..100];
        if ((IsResident && (string.IsNullOrWhiteSpace(CurrentBuildingId) ||
                            string.IsNullOrWhiteSpace(CurrentApartmentId) ||
                            string.IsNullOrWhiteSpace(User.FindFirstValue(ClaimTypes.NameIdentifier)))) ||
            (IsAdmin && string.IsNullOrWhiteSpace(CurrentBuildingId))) return Forbid();
        string? qBuildingId = null;
        string? qApartmentId = null;
        string? qUserId = null;

        if (IsResident)
        {
            qBuildingId = CurrentBuildingId;
            qApartmentId = CurrentApartmentId;
            qUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        else if (IsAdmin)
        {
            qBuildingId = CurrentBuildingId;
            if (!string.IsNullOrWhiteSpace(apartmentId)) qApartmentId = apartmentId;
            qUserId = string.IsNullOrWhiteSpace(userId) ? null : userId;
        }
        else if (IsSuperAdmin)
        {
            qBuildingId = string.IsNullOrWhiteSpace(buildingId) ? null : buildingId;
            qApartmentId = string.IsNullOrWhiteSpace(apartmentId) ? null : apartmentId;
            qUserId = string.IsNullOrWhiteSpace(userId) ? null : userId;
        }

        var entries = await _audit.QueryAsync(
            buildingId: qBuildingId,
            apartmentId: qApartmentId,
            userId: qUserId,
            action: string.IsNullOrWhiteSpace(actionFilter) ? null : actionFilter,
            severity: string.IsNullOrWhiteSpace(severity) ? null : severity,
            from: ParseDate(from, endOfDay: false),
            to: ParseDate(to, endOfDay: true),
            limit: 50000,
            correlationId: correlationId);  // ← كل السجلات

        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        void Row(params string[] cells) => sb.AppendLine(string.Join(",", cells.Select(CsvExportSafety.Cell)));

        Row(Loc.T("Date"), Loc.T("Action"), Loc.T("Description"), Loc.T("Severity"),
            Loc.T("User"), Loc.T("Role"), Loc.T("Building"), Loc.T("Apartment"), "Correlation ID");

        foreach (var e in entries)
        {
            Row(
                e.EffectiveDate.ToString("yyyy-MM-dd HH:mm:ss"),
                e.Action,
                AuditLogQueryService.LabelAr(e.Action),
                e.Severity,
                e.UserId ?? "",
                e.UserRole ?? "",
                e.BuildingId ?? "",
                e.ApartmentId ?? "",
                e.CorrelationId ?? "");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"audit-log-{DateTime.UtcNow:yyyy-MM-dd-HHmm}.csv";
        return File(bytes, "text/csv", fileName);
    }

    private static DateTime? ParseDate(string? value, bool endOfDay)
    {
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date))
            return null;

        var utc = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        return endOfDay ? utc.AddDays(1).AddTicks(-1) : utc;
    }
}