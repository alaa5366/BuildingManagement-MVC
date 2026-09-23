using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

// نفس شاشة js/views/admin/admin-expenses.js
[Authorize(Roles = "admin")]
public class ExpensesController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly WalletService _wallet;
    private readonly ExpensesService _expenses;
    private readonly AuditLogService _audit;

    public ExpensesController(BuildingsService buildings, WalletService wallet, ExpensesService expenses, AuditLogService audit)
    {
        _buildings = buildings; _wallet = wallet; _expenses = expenses; _audit = audit;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    [HttpGet]
    public async Task<IActionResult> Index(string? month)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;
        var m = _wallet.GetOrCreateMonth(building, mk);

        ViewBag.Building = building;
        ViewBag.Month = mk;
        ViewBag.Categories = building.ExpenseCategories.Where(c => c.Active).OrderBy(c => c.Order).ToList();
        ViewBag.Total = m.Expenses.Sum(e => e.Amount);
        return View(m.Expenses.OrderByDescending(e => e.Date).ToList());
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
            _expenses.AddExpense(building, categoryId, note, amount, date, mk);
            _wallet.RecalculateDistribution(building, mk);
            _audit.Push(building, "expense_add", $"{amount:0.##} ج.م — {note}", "admin", User.Identity?.Name ?? "admin");
            await _buildings.SaveFullAsync(building);
            TempData["Message"] = "تمت إضافة المصروف";
        }

        return RedirectToAction("Index", new { month = mk });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string month, string id)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        _expenses.DeleteExpense(building, id, month);
        _wallet.RecalculateDistribution(building, month);
        _audit.Push(building, "expense_delete", "حذف مصروف", "admin", User.Identity?.Name ?? "admin");
        await _buildings.SaveFullAsync(building);

        return RedirectToAction("Index", new { month });
    }
}
