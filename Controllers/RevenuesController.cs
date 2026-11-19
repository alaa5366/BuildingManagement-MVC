using System.Security.Claims;
using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "admin")]
[AdminPermission(AdminPermissions.ManageRevenues)]
public class RevenuesController : Controller
{
    private readonly TargetWalletReadService _targetWallet;
    private readonly TargetFinanceReadService _financeRead;
    private readonly TargetWalletReadService _targetWallet;
    private readonly TargetExpenseRevenueWorkflowService _financeWorkflow;
    private readonly TargetAccountIdentityResolver _identityResolver;
    private readonly ILogger<RevenuesController> _logger;

    public RevenuesController(
        TargetWalletReadService targetWallet,
        TargetFinanceReadService financeRead,
        TargetWalletReadService targetWallet,
        TargetExpenseRevenueWorkflowService financeWorkflow,
        TargetAccountIdentityResolver identityResolver,
        ILogger<RevenuesController> logger)
    {
        _targetWallet = targetWallet;
        _financeRead = financeRead;
        _targetWallet = targetWallet;
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
        var monthKey = string.IsNullOrWhiteSpace(month)
            ? await _targetWallet.GetCurrentPeriodKeyAsync(BuildingId, cancellationToken: HttpContext.RequestAborted)
            : month;
        var snapshot = await _financeRead.GetMonthAsync(BuildingId, monthKey, "revenue", HttpContext.RequestAborted);
        var revenues = PagedResult<Revenue>.Create(snapshot.Revenues.ToList(), page, pageSize);
        ViewBag.Building = building;
        ViewBag.Month = monthKey;
        ViewBag.Categories = snapshot.Categories.ToList();
        ViewBag.Total = snapshot.Total;
        ViewBag.RouteValues = new Dictionary<string, string?> { ["month"] = monthKey, ["pageSize"] = pageSize.ToString() };
        return View(revenues);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string month, string categoryId, string note, decimal amount,
        string date, Guid commandId)
    {
        var buildingId = BuildingId;
        var monthKey = string.IsNullOrWhiteSpace(month)
            ? await _targetWallet.GetCurrentPeriodKeyAsync(BuildingId, cancellationToken: HttpContext.RequestAborted)
            : month;
        try
        {
            var actorId = await _identityResolver.ResolveAsync(User, HttpContext.RequestAborted);
            if (actorId is null) return Forbid();
            if (commandId == Guid.Empty) throw new ArgumentException("Invalid submission token.");
            await _financeWorkflow.AddRevenueAsync(buildingId, monthKey, categoryId, note, amount,
                date, commandId, actorId.Value, HttpContext.RequestAborted);
            TempData["Message"] = Loc.T("Revenue_Added");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Target finance revenue posting failed for building {BuildingId}", buildingId);
            TempData["Error"] = Loc.T("Failed_To_Add_Revenue");
        }
        return RedirectToAction(nameof(Index), new { month = monthKey });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string month, string id)
    {
        var buildingId = BuildingId;
        var monthKey = string.IsNullOrWhiteSpace(month)
            ? await _targetWallet.GetCurrentPeriodKeyAsync(BuildingId, cancellationToken: HttpContext.RequestAborted)
            : month;
        try
        {
            var actorId = await _identityResolver.ResolveAsync(User, HttpContext.RequestAborted);
            if (actorId is null) return Forbid();
            await _financeWorkflow.ReverseAsync(true, buildingId, id,
                "Reversed by building administrator", Guid.NewGuid(), actorId.Value, HttpContext.RequestAborted);
            TempData["Message"] = "تم عكس قيد الإيراد دون حذف سجل المصدر.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Target finance revenue reversal failed for {RevenueId}", id);
            TempData["Error"] = Loc.T("Failed_To_Delete_Revenue");
        }
        return RedirectToAction(nameof(Index), new { month = monthKey });
    }
}
