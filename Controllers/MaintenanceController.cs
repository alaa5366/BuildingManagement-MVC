using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  MaintenanceController	سجل الصيانة
// ============================================================
/// <summary>
/// Admin maintenance log: create, update status, delete and convert a record's cost into an expense.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>admin</c>.
/// </remarks>
[Authorize(Roles = "admin")]
[AdminPermission(BuildingManagementMvc.Models.AdminPermissions.ManageMaintenance)]
public class MaintenanceController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly MaintenanceService _maint;
    private readonly AuditLogService _audit;
    private readonly NotificationsService _notify;
    private readonly TargetNotificationsService _targetNotifications;
    private readonly Microsoft.Extensions.Logging.ILogger<MaintenanceController> _logger;
    private readonly TargetExpenseRevenueWorkflowService _financeWorkflow;
    private readonly TargetFinanceReadService _targetFinance;
    private readonly TargetAccountIdentityResolver _identityResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaintenanceController"/> class.
    /// </summary>
    public MaintenanceController(
        BuildingsService buildings,
        MaintenanceService maint,
        AuditLogService audit,
        NotificationsService notify,
        TargetNotificationsService targetNotifications,
        Microsoft.Extensions.Logging.ILogger<MaintenanceController> logger,
        TargetExpenseRevenueWorkflowService financeWorkflow,
        TargetFinanceReadService targetFinance,
        TargetAccountIdentityResolver identityResolver)
    {
        _buildings = buildings;
        _maint = maint;
        _audit = audit;
        _notify = notify;
        _targetNotifications = targetNotifications;
        _logger = logger;
        _financeWorkflow = financeWorkflow;
        _targetFinance = targetFinance;
        _identityResolver = identityResolver;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    /// <summary>
    /// Shows the main page of the maintenance screen.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var allRecords = _maint.GetLog(building);
        var pagedRecords = BuildingManagementMvc.Models.PagedResult<BuildingManagementMvc.Models.MaintenanceRecord>
            .Create(allRecords, page, pageSize);

        ViewBag.Building = building;
        ViewBag.RouteValues = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString()
        };

        return View(pagedRecords);
    }

    /// <summary>
    /// Handles the add form submission of the maintenance screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string title, string? description, string date, decimal cost, string? vendor, string status)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        if (string.IsNullOrWhiteSpace(title) || cost < 0 ||
            !DateTime.TryParse(date, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal, out _) ||
            (!string.IsNullOrWhiteSpace(status) &&
             !new[] { "open", "inprogress", "done", "cancelled" }.Contains(status.Trim().ToLowerInvariant())))
        {
            TempData["Error"] = "يرجى إدخال عنوان وتاريخ صحيحين وتكلفة غير سالبة.";
            return RedirectToAction("Index");
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            var rec = _maint.Add(building, title.Trim(), description, date, cost, vendor, status, User.Identity?.Name ?? "admin");
            _audit.Push(building, "maint_add", rec.Title, "admin", User.Identity?.Name ?? "admin");
            await _buildings.SaveFullAsync(building);
            TempData["Message"] = Loc.T("Maintenance_Record_Added");
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Handles the update form submission of the maintenance screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string id, string title, string? description, string date, decimal cost, string? vendor, string status)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        if (string.IsNullOrWhiteSpace(title) || cost < 0 ||
            !DateTime.TryParse(date, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal, out _) ||
            !new[] { "open", "inprogress", "done", "cancelled" }.Contains((status ?? "").Trim().ToLowerInvariant()))
        {
            TempData["Error"] = "يرجى إدخال عنوان وتاريخ صحيحين وتكلفة غير سالبة.";
            return RedirectToAction("Index");
        }
        status = Request.Form["status"].ToString() ?? "Pending";
        var rec = _maint.Update(building, id, title, description, date, cost, vendor, status);
        if (rec != null)
        {
            _audit.Push(building, "maint_update", rec.Title, "admin", User.Identity?.Name ?? "admin");
            await _buildings.SaveFullAsync(building);
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Handles the delete form submission of the maintenance screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        if (_maint.Delete(building, id))
        {
            _audit.Push(building, "maint_delete", id, "admin", User.Identity?.Name ?? "admin");
            await _buildings.SaveFullAsync(building);
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Adds the to expenses.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToExpenses(string id, string? month, Guid commandId)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;
        var record = building.MaintenanceLog.FirstOrDefault(item => item.Id == id);
        if (record is null || record.AddedToExpense || record.Cost <= 0)
        {
            TempData["Error"] = "سجل الصيانة غير موجود أو تمت إضافته للمصروفات بالفعل.";
            return RedirectToAction("Index");
        }

        var actorAccountId = await _identityResolver.ResolveAsync(User, HttpContext.RequestAborted);
        if (actorAccountId is null)
        {
            TempData["Error"] = "تعذر تحديد حساب المسؤول في النظام المستهدف.";
            return RedirectToAction("Index");
        }

        try
        {
            var financeMonth = await _targetFinance.GetMonthAsync(
                BuildingId, mk, "expense", HttpContext.RequestAborted);
            var category = financeMonth.Categories.FirstOrDefault(item => item.Active &&
                    (item.Name.Contains("صيانة", StringComparison.OrdinalIgnoreCase) ||
                     item.Name.Contains("maintenance", StringComparison.OrdinalIgnoreCase)))
                ?? financeMonth.Categories.FirstOrDefault(item => item.Active && item.Id == "other")
                ?? financeMonth.Categories.FirstOrDefault(item => item.Active);
            if (category is null)
            {
                TempData["Error"] = "لا توجد فئة مصروفات نشطة لتسجيل تكلفة الصيانة.";
                return RedirectToAction("Index");
            }

            if (commandId == Guid.Empty)
            {
                TempData["Error"] = "انتهت صلاحية النموذج. حدّث الصفحة وحاول مرة أخرى.";
                return RedirectToAction("Index");
            }

            var expenseDate = string.IsNullOrWhiteSpace(record.Date)
                ? DateTime.UtcNow.ToString("yyyy-MM-dd")
                : record.Date;
            var note = "🔧 " + record.Title +
                (string.IsNullOrWhiteSpace(record.Vendor) ? "" : $" — {record.Vendor}");
            await _financeWorkflow.AddExpenseAsync(
                BuildingId, mk, category.Id, note, record.Cost, expenseDate,
                commandId, actorAccountId.Value, HttpContext.RequestAborted);

            record.AddedToExpense = true;
            record.AddedToExpenseAt = DateTime.UtcNow.ToString("O");
            await _buildings.SaveFullAsync(building);

            foreach (var apartment in building.Apartments.Where(item => !item.Closed))
            {
                try
                {
                    await _targetNotifications.CreateResidentDepositNoticeAsync(
                        building.Id, apartment.Id, "مصروف صيانة جديد",
                        $"{category.Name}: {record.Cost:0.##} — {building.Name}",
                        HttpContext.RequestAborted, "expense_new", "💸");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Maintenance expense notification failed for building {BuildingId}, apartment {ApartmentId}.",
                        building.Id, apartment.Id);
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

        return RedirectToAction("Index");
    }
}
