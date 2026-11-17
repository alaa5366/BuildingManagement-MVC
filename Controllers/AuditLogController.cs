using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Controllers;

/// <summary>
/// Shows and exports the audit log. The scope (all buildings, one building, one apartment) depends on the signed-in role.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>superadmin,admin,resident</c>.
/// </remarks>
[Authorize(Roles = "superadmin,admin,resident")]
public class AuditLogController : Controller
{
    private readonly AuditLogQueryService _audit;
    private readonly BuildingsService _buildings;
    private readonly ILogger<AuditLogController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogController"/> class.
    /// </summary>
    public AuditLogController(
        AuditLogQueryService audit,
        BuildingsService buildings,
        ILogger<AuditLogController> logger)
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

    /// <summary>
    /// Shows the main page of the audit log screen.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index(
    string? buildingId, string? apartmentId, string? userId,
    string? actionFilter, string? severity, string? from, string? to,
    int page = 1, int pageSize = 50)
    {
        var actionTimer = Stopwatch.StartNew();

        if ((IsResident && (string.IsNullOrWhiteSpace(CurrentBuildingId) || string.IsNullOrWhiteSpace(CurrentApartmentId))) ||
            (IsAdmin && string.IsNullOrWhiteSpace(CurrentBuildingId))) return Forbid();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        string? qBuildingId = null;
        string? qApartmentId = null;

        if (IsResident)
        {
            qBuildingId = CurrentBuildingId;
            qApartmentId = CurrentApartmentId;
            ViewBag.CanFilterByBuilding = false;
            ViewBag.CanFilterByApartment = false;
        }
        else if (IsAdmin)
        {
            qBuildingId = CurrentBuildingId;
            if (!string.IsNullOrWhiteSpace(apartmentId))
                qApartmentId = apartmentId;
            ViewBag.CanFilterByBuilding = false;
            ViewBag.CanFilterByApartment = true;
        }
        else if (IsSuperAdmin)
        {
            qBuildingId = string.IsNullOrWhiteSpace(buildingId) ? null : buildingId;
            qApartmentId = string.IsNullOrWhiteSpace(apartmentId) ? null : apartmentId;
            ViewBag.CanFilterByBuilding = true;
            ViewBag.CanFilterByApartment = true;
        }

        var queryTimer = Stopwatch.StartNew();
        var pagedResult = await _audit.QueryPageAsync(
            buildingId: qBuildingId,
            apartmentId: qApartmentId,
            userId: string.IsNullOrWhiteSpace(userId) ? null : userId,
            action: string.IsNullOrWhiteSpace(actionFilter) ? null : actionFilter,
            severity: string.IsNullOrWhiteSpace(severity) ? null : severity,
            from: ParseDate(from), to: ParseDate(to), page: page, pageSize: pageSize);
        queryTimer.Stop();
        page = pagedResult.CurrentPage;
        pageSize = pagedResult.PageSize;

        var buildingsTimer = Stopwatch.StartNew();
        ViewBag.Buildings = IsSuperAdmin
            ? await _buildings.GetSummariesAsync()
            : new List<Building>();
        buildingsTimer.Stop();

        _logger.LogInformation(
            "AUDIT PERF: Role={Role} QueryPage={QueryMs}ms Buildings={BuildingsMs}ms",
            IsSuperAdmin ? "superadmin" : IsAdmin ? "admin" : "resident",
            queryTimer.ElapsedMilliseconds,
            buildingsTimer.ElapsedMilliseconds);

        ViewBag.CurrentRole = IsSuperAdmin ? "superadmin" : (IsAdmin ? "admin" : "resident");
        ViewBag.KnownActions = AuditLogQueryService.KnownActions;
        ViewBag.SelectedAction = actionFilter;
        ViewBag.SelectedSeverity = severity;
        ViewBag.SelectedBuildingId = buildingId;
        ViewBag.SelectedApartmentId = apartmentId;
        ViewBag.SelectedUserId = userId;
        ViewBag.From = from;
        ViewBag.To = to;

        // ✅ معلومات الـ Pagination للـ Partial
        ViewBag.PaginationRequest = new PaginationRequest { Page = page, PageSize = pageSize };
        ViewBag.RouteValues = new Dictionary<string, string?>
        {
            ["buildingId"] = buildingId,
            ["apartmentId"] = apartmentId,
            ["userId"] = userId,
            ["actionFilter"] = actionFilter,
            ["severity"] = severity,
            ["from"] = from,
            ["to"] = to,
            ["pageSize"] = pageSize.ToString()
        };

        actionTimer.Stop();
        _logger.LogInformation(
            "AUDIT PERF: Index action completed in {ActionMs}ms; TotalItems={TotalItems}; PageSize={PageSize}",
            actionTimer.ElapsedMilliseconds, pagedResult.TotalItems, pagedResult.PageSize);

        return View(pagedResult);
    }

    /// <summary>
    /// Exports the CSV.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> ExportCsv(
    string? buildingId, string? apartmentId, string? userId,
    string? actionFilter, string? severity, string? from, string? to)
    {
        if ((IsResident && (string.IsNullOrWhiteSpace(CurrentBuildingId) || string.IsNullOrWhiteSpace(CurrentApartmentId))) ||
            (IsAdmin && string.IsNullOrWhiteSpace(CurrentBuildingId))) return Forbid();
        string? qBuildingId = null;
        string? qApartmentId = null;

        if (IsResident)
        {
            qBuildingId = CurrentBuildingId;
            qApartmentId = CurrentApartmentId;
        }
        else if (IsAdmin)
        {
            qBuildingId = CurrentBuildingId;
            if (!string.IsNullOrWhiteSpace(apartmentId)) qApartmentId = apartmentId;
        }
        else if (IsSuperAdmin)
        {
            qBuildingId = string.IsNullOrWhiteSpace(buildingId) ? null : buildingId;
            qApartmentId = string.IsNullOrWhiteSpace(apartmentId) ? null : apartmentId;
        }

        var entries = await _audit.QueryAsync(
            buildingId: qBuildingId,
            apartmentId: qApartmentId,
            userId: string.IsNullOrWhiteSpace(userId) ? null : userId,
            action: string.IsNullOrWhiteSpace(actionFilter) ? null : actionFilter,
            severity: string.IsNullOrWhiteSpace(severity) ? null : severity,
            from: ParseDate(from),
            to: ParseDate(to),
            limit: 50000);  // ← كل السجلات

        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        void Row(params string[] cells) => sb.AppendLine(string.Join(",", cells.Select(CsvExportSafety.Cell)));

        Row(Loc.T("Date"), Loc.T("Action"), Loc.T("Description"), Loc.T("Severity"),
            Loc.T("User"), Loc.T("Role"), Loc.T("Building"), Loc.T("Apartment"));

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
                e.ApartmentId ?? "");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"audit-log-{DateTime.UtcNow:yyyy-MM-dd-HHmm}.csv";
        return File(bytes, "text/csv", fileName);
    }

    private static DateTime? ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return DateTime.SpecifyKind(d, DateTimeKind.Utc);
        return null;
    }
}