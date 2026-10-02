using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin,admin,resident")]
public class AuditLogController : Controller
{
    private readonly AuditLogQueryService _audit;
    private readonly BuildingsService _buildings;

    public AuditLogController(AuditLogQueryService audit, BuildingsService buildings)
    {
        _audit = audit;
        _buildings = buildings;
    }

    private string? CurrentBuildingId => User.FindFirst("buildingId")?.Value;
    private string? CurrentApartmentId => User.FindFirst("apartmentId")?.Value;
    private bool IsSuperAdmin => User.IsInRole("superadmin");
    private bool IsAdmin => User.IsInRole("admin");
    private bool IsResident => User.IsInRole("resident");

    [HttpGet]
    [HttpGet]
    public async Task<IActionResult> Index(
    string? buildingId, string? apartmentId, string? userId,
    string? actionFilter, string? severity, string? from, string? to,
    int page = 1, int pageSize = 50)
    {
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

        // ✅ نجيب كل السجلات (بدون pagination على Firestore)
        var entries = await _audit.QueryAsync(
            buildingId: qBuildingId,
            apartmentId: qApartmentId,
            userId: string.IsNullOrWhiteSpace(userId) ? null : userId,
            action: string.IsNullOrWhiteSpace(actionFilter) ? null : actionFilter,
            severity: string.IsNullOrWhiteSpace(severity) ? null : severity,
            from: ParseDate(from),
            to: ParseDate(to),
            limit: 10000);  // ← نجيب كل السجلات، ثم نعمل pagination في الـ memory

        // ✅ Pagination في الـ memory
        var pagedResult = PagedResult<BuildingManagementMvc.Services.AuditEntryDoc>.Create(
            entries, page, pageSize);

        ViewBag.Buildings = IsSuperAdmin
            ? await _buildings.GetAllAsync()
            : new List<Building>();

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

        return View(pagedResult);
    }

    [HttpGet]
    [HttpGet]
    public async Task<IActionResult> ExportCsv(
    string? buildingId, string? apartmentId, string? userId,
    string? actionFilter, string? severity, string? from, string? to)
    {
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
        void Row(params string[] cells) => sb.AppendLine(string.Join(",",
            cells.Select(c => $"\"{(c ?? "").Replace("\"", "\"\"")}\"")));

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