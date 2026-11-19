using System.Security.Claims;
using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "admin")]
[AdminPermission(AdminPermissions.ManageExpenses)]
public class ExpensesController : Controller
{
    private readonly TargetWalletReadService _targetWallet;
    private readonly TargetFinanceReadService _financeRead;
    private readonly TargetExpenseRevenueWorkflowService _financeWorkflow;
    private readonly TargetAccountIdentityResolver _identityResolver;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(
        TargetWalletReadService targetWallet,
        TargetFinanceReadService financeRead,
        TargetExpenseRevenueWorkflowService financeWorkflow,
        TargetAccountIdentityResolver identityResolver,
        ILogger<ExpensesController> logger)
    {
        _targetWallet = targetWallet;
        _financeRead = financeRead;
        _financeWorkflow = financeWorkflow;
        _identityResolver = identityResolver;
        _logger = logger;
    }

    private string BuildingId => User.FindFirstValue("buildingId") ?? "";

    [HttpGet]
    public async Task<IActionResult> Index(string? month, int page = 1, int pageSize = 50)
    {
        if (string.IsNullOrWhiteSpace(BuildingId)) return Forbid();
        var building = await _targetWallet.GetBuildingShellAsync(
            BuildingId, cancellationToken: HttpContext.RequestAborted);
        if (building == null) return NotFound();
        var monthKey = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;
        var snapshot = await _financeRead.GetMonthAsync(BuildingId, monthKey, "expense", HttpContext.RequestAborted);
        var expenses = PagedResult<Expense>.Create(snapshot.Expenses.ToList(), page, pageSize);
        ViewBag.Building = building;
        ViewBag.Month = monthKey;
        ViewBag.Categories = snapshot.Categories.ToList();
        ViewBag.Total = snapshot.Total;
        ViewBag.RouteValues = new Dictionary<string, string?> { ["month"] = monthKey, ["pageSize"] = pageSize.ToString() };
        return View(expenses);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string month, string categoryId, string note, decimal amount,
        string date, Guid commandId)
    {
        var buildingId = BuildingId;
        var monthKey = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;
        try
        {
            var actorId = await _identityResolver.ResolveAsync(User, HttpContext.RequestAborted);
            if (actorId is null) return Forbid();
            if (commandId == Guid.Empty) throw new ArgumentException("Invalid submission token.");
            await _financeWorkflow.AddExpenseAsync(buildingId, monthKey, categoryId, note, amount,
                date, commandId, actorId.Value, HttpContext.RequestAborted);
            TempData["Message"] = Loc.T("Expense_Added");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Target finance expense posting failed for building {BuildingId}", buildingId);
            TempData["Error"] = Loc.T("Failed_To_Add_Expense");
        }
        return RedirectToAction(nameof(Index), new { month = monthKey });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string month, string id)
    {
        var buildingId = BuildingId;
        var monthKey = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;
        try
        {
            var actorId = await _identityResolver.ResolveAsync(User, HttpContext.RequestAborted);
            if (actorId is null) return Forbid();
            await _financeWorkflow.ReverseAsync(false, buildingId, id,
                "Reversed by building administrator", Guid.NewGuid(), actorId.Value, HttpContext.RequestAborted);
            TempData["Message"] = "تم عكس قيد المصروف دون حذف سجل المصدر.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Target finance expense reversal failed for {ExpenseId}", id);
            TempData["Error"] = Loc.T("Failed_To_Delete_Expense");
        }
        return RedirectToAction(nameof(Index), new { month = monthKey });
    }
}
