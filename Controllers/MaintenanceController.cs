using System.Security.Claims;
using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "admin")]
[AdminPermission(AdminPermissions.ManageMaintenance)]
public class MaintenanceController : Controller
{
    private readonly TargetMaintenanceService _maintenance;
    private readonly TargetFinanceReadService _targetFinance;
    private readonly TargetExpenseRevenueWorkflowService _financeWorkflow;
    private readonly TargetAccountIdentityResolver _identityResolver;
    private readonly TargetNotificationsService _notifications;
    private readonly IAuditLogger _audit;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(
        TargetMaintenanceService maintenance,
        TargetFinanceReadService targetFinance,
        TargetExpenseRevenueWorkflowService financeWorkflow,
        TargetAccountIdentityResolver identityResolver,
        TargetNotificationsService notifications,
        IAuditLogger audit,
        ILogger<MaintenanceController> logger)
    {
        _maintenance = maintenance;
        _targetFinance = targetFinance;
        _financeWorkflow = financeWorkflow;
        _identityResolver = identityResolver;
        _notifications = notifications;
        _audit = audit;
        _logger = logger;
    }

    private string BuildingId => User.FindFirstValue("buildingId") ?? "";

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        if (string.IsNullOrWhiteSpace(BuildingId)) return Forbid();
        var buildingName = await _maintenance.GetBuildingNameAsync(BuildingId, HttpContext.RequestAborted);
        if (buildingName is null) return NotFound();

        var records = await _maintenance.GetPageAsync(
            BuildingId, page, pageSize, HttpContext.RequestAborted);
        ViewBag.BuildingName = buildingName;
        ViewBag.RouteValues = new Dictionary<string, string?> { ["pageSize"] = records.PageSize.ToString() };
        return View(records);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        string title, string? description, string date, decimal cost, string? vendor, string status)
    {
        if (string.IsNullOrWhiteSpace(BuildingId)) return Forbid();
        try
        {
            var record = await _maintenance.AddAsync(
                BuildingId, title, description, date, cost, vendor, status,
                User.Identity?.Name ?? "admin", HttpContext.RequestAborted);
            await _audit.LogAsync(
                action: "maintenance.added",
                buildingId: BuildingId,
                userId: User.Identity?.Name ?? "admin",
                userRole: "admin",
                metadata: new { record.Id, record.Title, record.Cost, record.Date },
                severity: "info");
            TempData["Message"] = Loc.T("Maintenance_Record_Added");
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Target maintenance record creation failed for building {BuildingId}.", BuildingId);
            TempData["Error"] = "تعذر حفظ سجل الصيانة.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        string id, string title, string? description, string date,
        decimal cost, string? vendor, string status)
    {
        if (string.IsNullOrWhiteSpace(BuildingId)) return Forbid();
        try
        {
            var record = await _maintenance.UpdateAsync(
                BuildingId, id, title, description, date, cost, vendor, status,
                HttpContext.RequestAborted);
            if (record is null) return NotFound();

            await _audit.LogAsync(
                action: "maintenance.updated",
                buildingId: BuildingId,
                userId: User.Identity?.Name ?? "admin",
                userRole: "admin",
                metadata: new { record.Id, record.Title, record.Cost, record.Date, record.Status },
                severity: "info");
            TempData["Message"] = "تم تحديث سجل الصيانة.";
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Target maintenance update failed for {MaintenanceId}.", id);
            TempData["Error"] = "تعذر تحديث سجل الصيانة.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(BuildingId)) return Forbid();
        try
        {
            var deleted = await _maintenance.DeleteAsync(BuildingId, id, HttpContext.RequestAborted);
            if (!deleted) return NotFound();

            await _audit.LogAsync(
                action: "maintenance.deleted",
                buildingId: BuildingId,
                userId: User.Identity?.Name ?? "admin",
                userRole: "admin",
                metadata: new { maintenanceId = id },
                severity: "warning");
            TempData["Message"] = "تم حذف سجل الصيانة.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Target maintenance delete failed for {MaintenanceId}.", id);
            TempData["Error"] = "تعذر حذف سجل الصيانة.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToExpenses(string id)
    {
        if (string.IsNullOrWhiteSpace(BuildingId)) return Forbid();
        var record = await _maintenance.GetAsync(BuildingId, id, HttpContext.RequestAborted);
        if (record is null) return NotFound();
        if (record.AddedToExpense || record.Cost <= 0 ||
            string.Equals(record.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
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

        var actorAccountId = await _identityResolver.ResolveAsync(User, HttpContext.RequestAborted);
        if (actorAccountId is null) return Forbid();

        var expenseDate = record.Date;
        var monthKey = expenseDate.Length >= 7 ? expenseDate[..7] : "";
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

            if (!await _maintenance.MarkAddedToExpenseAsync(BuildingId, id, HttpContext.RequestAborted))
                throw new InvalidOperationException("The maintenance expense was posted but its source status could not be updated.");

            await _audit.LogAsync(
                action: "maintenance.posted_to_expenses",
                buildingId: BuildingId,
                userId: User.Identity?.Name ?? "admin",
                userRole: "admin",
                metadata: new { record.Id, record.Cost, monthKey, categoryId = category.Id },
                severity: "info");

            var apartmentIds = await _maintenance.GetActiveApartmentIdsAsync(
                BuildingId, HttpContext.RequestAborted);
            var buildingName = await _maintenance.GetBuildingNameAsync(BuildingId, HttpContext.RequestAborted) ?? "";
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
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to post maintenance cost {MaintenanceId} to target finance.", id);
            TempData["Error"] = "تعذر إضافة تكلفة الصيانة إلى المصروفات.";
        }

        return RedirectToAction(nameof(Index));
    }
}
