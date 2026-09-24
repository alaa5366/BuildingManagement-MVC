using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

// نفس شاشة js/views/admin/admin-wallet.js (أرصدة الشقق + الدفعات المعلقة)
[Authorize(Roles = "admin")]
public class WalletController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly WalletService _wallet;
    private readonly AuditLogService _audit;

    public WalletController(BuildingsService buildings, WalletService wallet, AuditLogService audit)
    {
        _buildings = buildings; _wallet = wallet; _audit = audit;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    [HttpGet]
    public async Task<IActionResult> Index(string? month)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;
        _wallet.GetOrCreateMonth(building, mk);

        // ✅ خريطة: floorId → floorOrder
        var floorOrderMap = building.Floors.ToDictionary(f => f.Id, f => f.Order);

        var balances = building.Apartments
            .OrderBy(a => floorOrderMap.GetValueOrDefault(a.FloorId, int.MaxValue))
            .ThenBy(a => a.Number)
            .Select(a => (Apt: a, Balance: _wallet.ComputeWalletBalance(building, a.Id, mk)))
            .ToList();

        var pending = _wallet.GetAllPendingDeposits(building, mk);
        var totals = _wallet.TotalsOf(building, mk);

        ViewBag.Building = building;
        ViewBag.Month = mk;
        ViewBag.Balances = balances;
        ViewBag.Pending = pending;
        ViewBag.Totals = totals;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmDeposit(string month, string depositId)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var d = _wallet.ConfirmDeposit(building, depositId, User.Identity?.Name ?? "admin", month);
        if (d != null)
        {
            _audit.Push(building, "deposit_confirm", $"تأكيد دفعة {d.Number} — {d.Amount:0.##} ج.م", "admin", User.Identity?.Name ?? "admin");
            await _buildings.SaveFullAsync(building);
        }

        return RedirectToAction("Index", new { month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelDeposit(string month, string depositId, string? reason)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var d = _wallet.CancelDeposit(building, depositId, User.Identity?.Name ?? "admin", reason, month);
        if (d != null)
        {
            _audit.Push(building, "deposit_cancel", $"إلغاء دفعة {d.Number}" + (string.IsNullOrWhiteSpace(reason) ? "" : $" — {reason}"), "admin", User.Identity?.Name ?? "admin");
            await _buildings.SaveFullAsync(building);
        }

        return RedirectToAction("Index", new { month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAdjustment(string month, string aptId, double amount, string? reason)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        if (amount != 0)
        {
            _wallet.AddWalletAdjustment(building, aptId, amount, reason, month, User.Identity?.Name ?? "admin");
            _audit.Push(building, "wallet_adjustment", $"{amount:0.##} ج.م — {reason}", "admin", User.Identity?.Name ?? "admin");
            await _buildings.SaveFullAsync(building);
        }

        return RedirectToAction("Index", new { month });
    }
}
