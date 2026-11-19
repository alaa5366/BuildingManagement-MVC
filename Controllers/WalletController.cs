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
    private readonly TargetDepositWorkflowService _depositWorkflow;
    private readonly TargetWalletReadService _targetWallet;
    private readonly TargetAccountIdentityResolver _identityResolver;
    private readonly TargetWalletAdjustmentWorkflowService _adjustmentWorkflow;

    /// <summary>
    /// Initializes a new instance of the <see cref="WalletController"/> class.
    /// </summary>
    public WalletController(
        BuildingsService buildings,
        TargetDepositWorkflowService depositWorkflow,
        TargetWalletReadService targetWallet,
        TargetAccountIdentityResolver identityResolver,
        TargetWalletAdjustmentWorkflowService adjustmentWorkflow)
    {
        _buildings = buildings;
        _depositWorkflow = depositWorkflow;
        _targetWallet = targetWallet;
        _identityResolver = identityResolver;
        _adjustmentWorkflow = adjustmentWorkflow;
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

        var mk = string.IsNullOrWhiteSpace(month) ? DateTime.UtcNow.ToString("yyyy-MM") : month;
        TargetWalletAdminSnapshot snapshot;
        try
        {
            snapshot = await _targetWallet.GetAdminSnapshotAsync(BuildingId, mk, HttpContext.RequestAborted);
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }

        var floorOrderMap = building.Floors.ToDictionary(f => f.Id, f => f.Order);
        var allBalances = building.Apartments
            .OrderBy(a => floorOrderMap.GetValueOrDefault(a.FloorId, int.MaxValue))
            .ThenBy(a => a.Number)
            .Select(a => (Apt: a, Balance: snapshot.Balances.GetValueOrDefault(a.Id)))
            .ToList();

        var pagedBalances = PagedResult<(Apartment Apt, decimal Balance)>
            .Create(allBalances, page, pageSize);

        var allPending = snapshot.PendingDeposits
            .Select(item =>
            {
                var apartment = building.Apartments.FirstOrDefault(a => a.Id == item.ApartmentId);
                return apartment is null ? ((Apartment Apt, Deposit Deposit, string MonthKey)?)null
                    : (apartment, item.Deposit, item.MonthKey);
            })
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .ToList();
        var pagedPending = PagedResult<(Apartment Apt, Deposit Deposit, string MonthKey)>
            .Create(allPending, 1, pageSize);

        var totals = snapshot.Totals;

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
        var mk = string.IsNullOrWhiteSpace(month) ? DateTime.UtcNow.ToString("yyyy-MM") : month;
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

    public async Task<IActionResult> AddAdjustment(
        string month, string aptId, decimal amount, string? reason, Guid commandId)
    {
        var mk = string.IsNullOrWhiteSpace(month) ? DateTime.UtcNow.ToString("yyyy-MM") : month;
        var actorAccountId = await _identityResolver.ResolveAsync(User, HttpContext.RequestAborted);
        if (actorAccountId is null)
        {
            TempData["Error"] = "تعذر تحديد حساب المسؤول في النظام المستهدف.";
            return RedirectToAction("Index", new { month = mk });
        }

        try
        {
            if (commandId == Guid.Empty) commandId = Guid.NewGuid();
            await _adjustmentWorkflow.AddAsync(
                BuildingId, aptId, mk, actorAccountId.Value, amount, reason ?? "",
                commandId.ToString("D"), HttpContext.RequestAborted);
            TempData["Message"] = "تم تسجيل التسوية في دفتر الأستاذ بنجاح.";
        }
        catch (ArgumentException ex) { TempData["Error"] = ex.Message; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (Exception) { TempData["Error"] = "تعذر تسجيل التسوية. لم يتم تعديل الرصيد."; }

        return RedirectToAction("Index", new { month = mk });
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
            var updated = await _depositWorkflow.UpdatePendingAsync(
                paymentDepositId, actorAccountId.Value, amount, note, reason ?? "",
                HttpContext.RequestAborted);
            TempData[updated ? "Message" : "Error"] = updated
                ? Loc.T("Payment_Updated_Successfully")
                : Loc.T("The_Payment_Does_Not_Exist");
        }
        catch (ArgumentException ex) { TempData["Error"] = ex.Message; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (Exception) { TempData["Error"] = Loc.T("Update_Failed_2"); }

        return RedirectToAction("Index", new { month = mk });
    }
}