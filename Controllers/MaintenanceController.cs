using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

// نفس شاشة js/views/admin/admin-maintenance.js
[Authorize(Roles = "admin")]
public class MaintenanceController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly MaintenanceService _maint;
    private readonly AuditLogService _audit;
    private readonly NotificationsService _notify;

    public MaintenanceController(BuildingsService buildings, MaintenanceService maint, AuditLogService audit, NotificationsService notify)
    {
        _buildings = buildings; _maint = maint; _audit = audit; _notify = notify;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var allRecords = _maint.GetLog(building);
        var pagedRecords = BuildingManagementMvc.Models.PagedResult<BuildingManagementMvc.Models.MaintenanceRecord>
            .Create(allRecords, page, pageSize);

        ViewBag.Building = building;
        ViewBag.RouteValues = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString()
        };

        return View(pagedRecords);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string title, string? description, string date, double cost, string? vendor, string status)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(title))
        {
            var rec = _maint.Add(building, title.Trim(), description, date, cost, vendor, status, User.Identity?.Name ?? "admin");
            _audit.Push(building, "maint_add", rec.Title, "admin", User.Identity?.Name ?? "admin");
            await _buildings.SaveFullAsync(building);
            TempData["Message"] = "تمت إضافة سجل الصيانة";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string id, string title, string? description, string date, double cost, string? vendor, string status)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var rec = _maint.Update(building, id, title, description, date, cost, vendor, status);
        if (rec != null)
        {
            _audit.Push(building, "maint_update", rec.Title, "admin", User.Identity?.Name ?? "admin");
            await _buildings.SaveFullAsync(building);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        if (_maint.Delete(building, id))
        {
            _audit.Push(building, "maint_delete", id, "admin", User.Identity?.Name ?? "admin");
            await _buildings.SaveFullAsync(building);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToExpenses(string id, string? month)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;
        var expense = _maint.AddCostToExpenses(building, id, mk, User.Identity?.Name ?? "admin");
        if (expense != null)
        {
            _audit.Push(building, "maint_add_expense", $"{expense.Note} — {expense.Amount:0.##}", "admin", User.Identity?.Name ?? "admin");
            var catName = building.ExpenseCategories.FirstOrDefault(c => c.Id == expense.CategoryId)?.Name ?? expense.CategoryId;
            _notify.NotifyResidentsNewExpense(building, catName, expense.Amount);
            await _buildings.SaveFullAsync(building);
            TempData["Message"] = "تمت إضافة التكلفة للمصروفات";
        }

        return RedirectToAction("Index");
    }
}
