using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
    [HttpGet]
    public async Task<IActionResult> Index(string? month, int page = 1, int pageSize = 50)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;
        _wallet.GetOrCreateMonth(building, mk);

        var floorOrderMap = building.Floors.ToDictionary(f => f.Id, f => f.Order);

        // ✅ كل الأرصدة
        var allBalances = building.Apartments
            .OrderBy(a => floorOrderMap.GetValueOrDefault(a.FloorId, int.MaxValue))
            .ThenBy(a => a.Number)
            .Select(a => (Apt: a, Balance: _wallet.ComputeWalletBalance(building, a.Id, mk)))
            .ToList();

        // ✅ Pagination للشقق
        var pagedBalances = PagedResult<(BuildingManagementMvc.Models.Apartment Apt, double Balance)>
            .Create(allBalances, page, pageSize);

        // ✅ Pagination للدفعات المعلقة
        var allPending = _wallet.GetAllPendingDeposits(building, mk);
        var pagedPending = PagedResult<(BuildingManagementMvc.Models.Apartment Apt, BuildingManagementMvc.Models.Deposit Deposit)>
            .Create(allPending, 1, pageSize);

        var totals = _wallet.TotalsOf(building, mk);

        ViewBag.Building = building;
        ViewBag.Month = mk;
        ViewBag.Balances = pagedBalances;
        ViewBag.Pending = pagedPending;
        ViewBag.Totals = totals;
        ViewBag.RouteValues = new Dictionary<string, string?>
        {
            ["month"] = mk,
            ["pageSize"] = pageSize.ToString()
        };

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
