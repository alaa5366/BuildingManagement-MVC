using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "admin")]
[AdminPermission(BuildingManagementMvc.Models.AdminPermissions.ManageMaintenance)]
public class MaintenanceController : Controller
{
    private readonly TargetMaintenanceService _maintenance;
    private readonly TargetNotificationsService _notifications;
    private readonly ILogger<MaintenanceController> _logger;
    private readonly TargetExpenseRevenueWorkflowService _financeWorkflow;
    private readonly TargetFinanceReadService _targetFinance;
    private readonly TargetAccountIdentityResolver _identityResolver;

    public MaintenanceController(
        TargetMaintenanceService maintenance,
        TargetNotificationsService notifications,
        ILogger<MaintenanceController> logger,
        TargetExpenseRevenueWorkflowService financeWorkflow,
        TargetFinanceReadService targetFinance,
        TargetAccountIdentityResolver identityResolver)
    {
        _maintenance = maintenance;
        _notifications = notifications;
        _logger = logger;
        _financeWorkflow = financeWorkflow;
        _targetFinance = targetFinance;
        _identityResolver = identityResolver;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        var buildingName = await _maintenance.GetBuildingNameAsync(BuildingId, HttpContext.RequestAborted);
        if (buildingName is null) return NotFound();
        var paged = await _maintenance.GetPageAsync(BuildingId, page, pageSize, HttpContext.RequestAborted);
        ViewBag.BuildingName = buildingName;
        ViewBag.RouteValues = new Dictionary<string, string?> { ["pageSize"] = paged.PageSize.ToString() };
        return View(paged);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string title, string? description, string date, decimal cost, string? vendor, string status)
    {
        try
        {
            var record = await _maintenance.AddAsync(BuildingId, title, description, date, cost, vendor,
                status, User.Identity?.Name ?? "admin", HttpContext.RequestAborted);
            _logger.LogInformation("Maintenance record {MaintenanceId} created for building {BuildingId}.",
                record.Id, BuildingId);
            TempData["Message"] = Loc.T("Maintenance_Record_Added");
        }
        catch (ArgumentException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string id, string title, string? description, string date, decimal cost, string? vendor, string status)
    {
        try
        {
            var record = await _maintenance.UpdateAsync(BuildingId, id, title, description, date,
                cost, vendor, status, HttpContext.RequestAborted);
            if (record == null) return NotFound();
            _logger.LogInformation("Maintenance record {MaintenanceId} updated for building {BuildingId}.", id, BuildingId);
        }
        catch (ArgumentException ex) { TempData["Error"] = ex.Message; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            if (await _maintenance.DeleteAsync(BuildingId, id, HttpContext.RequestAborted))
                _logger.LogInformation("Maintenance record {MaintenanceId} deleted for building {BuildingId}.", id, BuildingId);
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToExpenses(string id)
    {
        var record = await _maintenance.GetAsync(BuildingId, id, HttpContext.RequestAborted);
        if (record is null || record.AddedToExpense || record.Cost <= 0)
        {
            TempData["Error"] = "سجل الصيانة غير موجود أو تمت إضافته للمصروفات بالفعل.";
            return RedirectToAction(nameof(Index));
        }

        var sourceCommand = id.StartsWith("mnt-", StringComparison.OrdinalIgnoreCase) ? id[4..] : id;
        if (!Guid.TryParse(sourceCommand, out var commandId) || commandId == Guid.Empty)
        {
            TempData["Error"] = "معرّف سجل الصيانة غير صالح لتسجيل العملية المالية.";
            return RedirectToAction(nameof(Index));
        }

        if (!DateOnly.TryParseExact(record.Date, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out _))
        {
            TempData["Error"] = "تاريخ سجل الصيانة غير صالح لتسجيل المصروف.";
            return RedirectToAction(nameof(Index));
        }
        var expenseDate = record.Date;
        var monthKey = expenseDate[..7];
        var actorAccountId = await _identityResolver.ResolveAsync(User, HttpContext.RequestAborted);
        if (actorAccountId is null)
        {
            TempData["Error"] = "تعذر تحديد حساب المسؤول في النظام المستهدف.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var financeMonth = await _targetFinance.GetMonthAsync(
                BuildingId, monthKey, "expense", HttpContext.RequestAborted);
            var category = financeMonth.Categories.FirstOrDefault(item => item.Active &&
                    (item.Name.Contains("صيانة", StringComparison.OrdinalIgnoreCase) ||
                     item.Name.Contains("maintenance", StringComparison.OrdinalIgnoreCase)))
                ?? financeMonth.Categories.FirstOrDefault(item => item.Active && item.Id == "other")
                ?? financeMonth.Categories.FirstOrDefault(item => item.Active);
            if (category is null)
            {
                TempData["Error"] = "لا توجد فئة مصروفات نشطة لتسجيل تكلفة الصيانة.";
                return RedirectToAction(nameof(Index));
            }

            var note = "🔧 " + record.Title +
                (string.IsNullOrWhiteSpace(record.Vendor) ? "" : $" — {record.Vendor}");
            await _financeWorkflow.AddExpenseAsync(
                BuildingId, monthKey, category.Id, note, record.Cost, expenseDate,
                commandId, actorAccountId.Value, HttpContext.RequestAborted);

            await _maintenance.MarkAddedToExpenseAsync(BuildingId, id, HttpContext.RequestAborted);

            var buildingName = await _maintenance.GetBuildingNameAsync(BuildingId, HttpContext.RequestAborted) ?? "";
            var apartmentIds = await _maintenance.GetActiveApartmentIdsAsync(BuildingId, HttpContext.RequestAborted);
            foreach (var apartmentId in apartmentIds)
            {
                try
                {
                    await _notifications.CreateResidentDepositNoticeAsync(
                        BuildingId, apartmentId, "مصروف صيانة جديد",
                        $"{category.Name}: {record.Cost:0.##} — {buildingName}",
                        HttpContext.RequestAborted, "expense_new", "💸");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Maintenance expense notification failed for building {BuildingId}, apartment {ApartmentId}.",
                        BuildingId, apartmentId);
                }
            }
            TempData["Message"] = Loc.T("Cost_Added_To_Expenses");
        }
        catch (ArgumentException ex) { TempData["Error"] = ex.Message; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to post maintenance cost {MaintenanceId} to target finance.", id);
            TempData["Error"] = "تعذر إضافة تكلفة الصيانة إلى المصروفات.";
        }

        return RedirectToAction(nameof(Index));
    }
}
