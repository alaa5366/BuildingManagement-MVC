using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Data;
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
    private readonly TargetDepositWorkflowService _depositWorkflow;
    private readonly TargetWalletReadService _targetWallet;
    private readonly TargetAccountIdentityResolver _identityResolver;
    private readonly AuditLogService _audit;
    private readonly NotificationsService _notify;

    /// <summary>
    /// Initializes a new instance of the <see cref="WalletController"/> class.
    /// </summary>
    public WalletController(
        BuildingsService buildings,
        WalletService wallet,
        TargetDepositWorkflowService depositWorkflow,
        TargetWalletReadService targetWallet,
        TargetAccountIdentityResolver identityResolver,
        AuditLogService audit,
        NotificationsService notify)
    {
        _buildings = buildings;
        _wallet = wallet;
        _depositWorkflow = depositWorkflow;
        _targetWallet = targetWallet;
        _identityResolver = identityResolver;
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
        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;
        if (!Guid.TryParse(depositId, out var paymentDepositId))
        {
            TempData["Error"] = "معرّف الدفعة غير صالح.";
            return RedirectToAction("Index", new { month = mk });
        }

        var actorAccountId = await _identityResolver.ResolveAsync(User, HttpContext.RequestAborted);
        if (actorAccountId is null)
        {
            TempData["Error"] = "تعذر تحديد حساب المسؤول في النظام المستهدف.";
            return RedirectToAction("Index", new { month = mk });
        }

        try
        {
            var transactionId = await _depositWorkflow.ConfirmAsync(
                paymentDepositId, actorAccountId.Value,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                HttpContext.RequestAborted);
            if (transactionId is null)
            {
                TempData["Error"] = "الدفعة غير موجودة.";
                return RedirectToAction("Index", new { month = mk });
            }

            // The target workflow owns the state transition, ledger posting and audit event.
            TempData["Message"] = Loc.T("Deposit_Confirmed_Successfully");
        }
        catch (ArgumentException ex) { TempData["Error"] = ex.Message; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (Exception) { TempData["Error"] = "تعذر تأكيد الدفعة. لم يتم تعديل الرصيد."; }

        return RedirectToAction("Index", new { month = mk });
    }

    public async Task<IActionResult> CancelDeposit(string month, string depositId, string? reason)
    {
        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;
        if (!Guid.TryParse(depositId, out var paymentDepositId))
        {
            TempData["Error"] = "معرّف الدفعة غير صالح.";
            return RedirectToAction("Index", new { month = mk });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "سبب الإلغاء مطلوب لحفظ سجل العملية.";
            return RedirectToAction("Index", new { month = mk });
        }

        var actorAccountId = await _identityResolver.ResolveAsync(User, HttpContext.RequestAborted);
        if (actorAccountId is null)
        {
            TempData["Error"] = "تعذر تحديد حساب المسؤول في النظام المستهدف.";
            return RedirectToAction("Index", new { month = mk });
        }

        try
        {
            var cancelled = await _depositWorkflow.CancelAsync(
                paymentDepositId, actorAccountId.Value, reason,
                HttpContext.RequestAborted);
            TempData[cancelled ? "Message" : "Error"] = cancelled
                ? Loc.T("Deposit_Cancelled_Successfully")
                : "الدفعة غير موجودة.";
        }
        catch (ArgumentException ex) { TempData["Error"] = ex.Message; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (Exception) { TempData["Error"] = "تعذر إلغاء الدفعة. لم يتم تعديل الرصيد."; }

        return RedirectToAction("Index", new { month = mk });
    }

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