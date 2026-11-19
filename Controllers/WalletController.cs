using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  WalletController	المحفظة (أرصدة + دفعات معلقة + تعديلات)
// ============================================================
/// <summary>
/// Admin wallet management: apartment balances, confirming/cancelling/editing payments, manual adjustments and WhatsApp messages.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>admin</c>.
/// </remarks>
[Authorize(Roles = "admin")]
[AdminPermission(BuildingManagementMvc.Models.AdminPermissions.ManageWallet)]
public class WalletController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly WalletService _wallet;
    private readonly AuditLogService _audit;
    private readonly NotificationsService _notify;

    /// <summary>
    /// Initializes a new instance of the <see cref="WalletController"/> class.
    /// </summary>
    public WalletController(
        BuildingsService buildings,
        WalletService wallet,
        AuditLogService audit,
        NotificationsService notify)
    {
        _buildings = buildings;
        _wallet = wallet;
        _audit = audit;
        _notify = notify;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    /// <summary>
    /// Shows the main page of the wallet screen.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
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
        var pagedBalances = PagedResult<(Apartment Apt, decimal Balance)>
            .Create(allBalances, page, pageSize);

        // ✅ Pagination للدفعات المعلقة (من كل الشهور)
        var allPending = _wallet.GetAllPendingDepositsCrossMonth(building);
        var pagedPending = PagedResult<(Apartment Apt, Deposit Deposit, string MonthKey)>
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

    // ============================================================
    // ✅ تأكيد دفعة
    // ============================================================
    /// <summary>
    /// Confirms the deposit.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmDeposit(string month, string depositId)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var d = _wallet.ConfirmDeposit(building, depositId, User.Identity?.Name ?? "admin", month);
        if (d != null)
        {
            // ✅ 1. أعد حساب carryOver من الشهر ده
            _wallet.RecomputeCarryOverFrom(building, month);

            // ✅ 2. Audit
            _audit.Push(building, "deposit_confirm",
                $"تأكيد دفعة {d.Number} — {d.Amount:0.##} ج.م",
                "admin", User.Identity?.Name ?? "admin");

            // ✅ 3. إشعار الساكن
            var apt = building.Apartments.FirstOrDefault(a => a.Id == d.AptId);
            if (apt != null)
            {
                _notify.NotifyResidentDepositConfirmed(building, d, apt);
            }

            await _buildings.SaveFullAsync(building);
            TempData["Message"] = Loc.T("Deposit_Confirmed_Successfully");
        }
        else TempData["Error"] = "تعذر تأكيد الدفعة: قد لا تكون معلّقة أو أنها غير موجودة.";

        return RedirectToAction("Index", new { month });
    }

    // ============================================================
    // ✅ إلغاء دفعة
    // ============================================================
    /// <summary>
    /// Cancels the deposit.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelDeposit(string month, string depositId, string? reason)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var d = _wallet.CancelDeposit(building, depositId, User.Identity?.Name ?? "admin", reason, month);
        if (d != null)
        {
            // ✅ 1. أعد حساب carryOver من الشهر ده
            _wallet.RecomputeCarryOverFrom(building, month);

            // ✅ 2. Audit
            _audit.Push(building, "deposit_cancel",
                $"إلغاء دفعة {d.Number}" + (string.IsNullOrWhiteSpace(reason) ? "" : $" — {reason}"),
                "admin", User.Identity?.Name ?? "admin");

            // ✅ 3. إشعار الساكن
            var apt = building.Apartments.FirstOrDefault(a => a.Id == d.AptId);
            if (apt != null)
            {
                _notify.NotifyResidentDepositCancelled(building, d, apt, reason);
            }

            await _buildings.SaveFullAsync(building);
            TempData["Message"] = Loc.T("Deposit_Cancelled_Successfully");
        }
        else TempData["Error"] = "تعذر إلغاء الدفعة: قد لا تكون معلّقة أو أنها غير موجودة.";

        return RedirectToAction("Index", new { month });
    }

    // ============================================================
    // ✅ إضافة تسوية يدوية
    // ============================================================
    /// <summary>
    /// Adds the adjustment.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAdjustment(string month, string aptId, decimal amount, string? reason)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        try
        {
            _wallet.AddWalletAdjustment(building, aptId, amount, reason, month, User.Identity?.Name ?? "admin");
            _audit.Push(building, "wallet_adjustment", $"{amount:0.##} ج.م — {reason}", "admin", User.Identity?.Name ?? "admin");
            await _buildings.SaveFullAsync(building);
            TempData["Message"] = "تم تسجيل التسوية بنجاح.";
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["Error"] = "تعذر تسجيل التسوية.";
        }

        return RedirectToAction("Index", new { month });
    }

    // ============================================================
    // ✅ Phase 21 — تعديل دفعة معلقة (المبلغ + الملاحظة بس)
    // ============================================================
    /// <summary>
    /// Updates the deposit.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDeposit(
        string month,
        string depositId,
        decimal amount,
        string? note,
        string? reason)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;

        try
        {
            var m = _wallet.GetOrCreateMonth(building, mk);
            var dep = m.Deposits.FirstOrDefault(d => d.Id == depositId);
            if (dep == null)
            {
                TempData["Error"] = Loc.T("The_Payment_Does_Not_Exist");
                return RedirectToAction("Index", new { month = mk });
            }

            if (dep.Status != "pending")
            {
                TempData["Error"] = Loc.T("A_Payment_Cannot_Be_Edited_When") +
                    (dep.Status == "confirmed" ? Loc.T("Confirmed_2") : Loc.T("Cancelled_2"));
                return RedirectToAction("Index", new { month = mk });
            }

            var oldAmount = dep.Amount;
            var aptId = dep.AptId;

            // ✅ نستدعي Service
            var updated = _wallet.UpdatePendingDeposit(
                building,
                depositId,
                mk,
                amount,
                note ?? "",
                reason ?? "",
                User.Identity?.Name ?? "admin");

            if (updated != null)
            {
                // ✅ Audit
                _audit.Push(
                    building,
                    "deposit_update",
                    $"تعديل دفعة {updated.Number}: {oldAmount:0.##} ← {updated.Amount:0.##} ج.م" +
                        (string.IsNullOrWhiteSpace(reason) ? "" : Loc.T("Reason_N", reason)),
                    "admin",
                    User.Identity?.Name ?? "admin");

                // ✅ إشعار الساكن
                var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
                if (apt != null)
                {
                    _notify.NotifyResidentDepositUpdated(
                        building, updated, apt, oldAmount, reason);
                }

                await _buildings.SaveFullAsync(building);
                TempData["Message"] = Loc.T("Payment_Updated_Successfully");
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["Error"] = Loc.T("Update_Failed_2");
        }

        return RedirectToAction("Index", new { month = mk });
    }
}