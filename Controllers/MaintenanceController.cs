using BuildingManagementMvc.Attributes;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="MaintenanceController"/> class.
    /// </summary>
    public MaintenanceController(
        BuildingsService buildings,
        MaintenanceService maint,
        AuditLogService audit,
        NotificationsService notify,
        TargetNotificationsService targetNotifications,
        Microsoft.Extensions.Logging.ILogger<MaintenanceController> logger)
    {
        _buildings = buildings;
        _maint = maint;
        _audit = audit;
        _notify = notify;
        _targetNotifications = targetNotifications;
        _logger = logger;
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
    public async Task<IActionResult> AddToExpenses(string id, string? month)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;
        var expense = _maint.AddCostToExpenses(building, id, mk, User.Identity?.Name ?? "admin");
        if (expense != null)
        {
            _audit.Push(building, "maint_add_expense", $"{expense.Note} — {expense.Amount:0.##}", "admin", User.Identity?.Name ?? "admin");
            var catName = building.ExpenseCategories.FirstOrDefault(c => c.Id == expense.CategoryId)?.Name ?? expense.CategoryId;
            _notify.NotifyResidentsNewExpense(building, catName, expense.Amount);
            foreach (var apartment in building.Apartments.Where(item => !item.Closed))
            {
                try
                {
                    await _targetNotifications.CreateResidentDepositNoticeAsync(
                        building.Id, apartment.Id, "مصروف جديد",
                        $"{catName}: {expense.Amount:0.##} ج.م — {building.Name}",
                        HttpContext.RequestAborted);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Expense notification failed for building {BuildingId}, apartment {ApartmentId}.",
                        building.Id, apartment.Id);
                }
            }
            await _buildings.SaveFullAsync(building);
            TempData["Message"] = Loc.T("Cost_Added_To_Expenses");
        }

        return RedirectToAction("Index");
    }
}
