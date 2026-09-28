using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

// نفس شاشة js/views/admin/admin-revenues.js
[Authorize(Roles = "admin")]
public class RevenuesController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly WalletService _wallet;
    private readonly RevenuesService _revenues;
    private readonly AuditLogService _audit;

    public RevenuesController(BuildingsService buildings, WalletService wallet, RevenuesService revenues, AuditLogService audit)
    {
        _buildings = buildings; _wallet = wallet; _revenues = revenues; _audit = audit;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    [HttpGet]
    [HttpGet]
    public async Task<IActionResult> Index(string? month, int page = 1, int pageSize = 50)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;
        var m = _wallet.GetOrCreateMonth(building, mk);

        var allRevenues = m.Revenues.OrderByDescending(r => r.Date).ToList();
        var pagedRevenues = PagedResult<BuildingManagementMvc.Models.Revenue>
            .Create(allRevenues, page, pageSize);

        ViewBag.Building = building;
        ViewBag.Month = mk;
        ViewBag.Categories = building.RevenueCategories.Where(c => c.Active).OrderBy(c => c.Order).ToList();
        ViewBag.Total = m.Revenues.Sum(r => r.Amount);
        ViewBag.RouteValues = new Dictionary<string, string?>
        {
            ["month"] = mk,
            ["pageSize"] = pageSize.ToString()
        };

        return View(pagedRevenues);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string month, string categoryId, string note, double amount, string date)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;

        if (amount > 0 && !string.IsNullOrWhiteSpace(categoryId))
        {
            _revenues.AddRevenue(building, categoryId, note, amount, date, mk);
            _wallet.RecalculateRevenueDistribution(building, mk);
            _audit.Push(building, "revenue_add", $"{amount:0.##} ج.م — {note}", "admin", User.Identity?.Name ?? "admin");
            await _buildings.SaveFullAsync(building);
            TempData["Message"] = "تمت إضافة الإيراد";
        }

        return RedirectToAction("Index", new { month = mk });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string month, string id)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        _revenues.DeleteRevenue(building, id, month);
        _wallet.RecalculateRevenueDistribution(building, month);
        _audit.Push(building, "revenue_delete", "حذف إيراد", "admin", User.Identity?.Name ?? "admin");
        await _buildings.SaveFullAsync(building);

        return RedirectToAction("Index", new { month });
    }
}
