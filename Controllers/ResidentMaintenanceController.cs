using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  ResidentMaintenanceController	(صيانة الساكن (عرض فقط
// ============================================================
/// <summary>
/// Read-only view of maintenance records for residents.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>resident</c>.
/// </remarks>
[Authorize(Roles = "resident")]
public class ResidentMaintenanceController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly MaintenanceService _maint;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResidentMaintenanceController"/> class.
    /// </summary>
    public ResidentMaintenanceController(BuildingsService buildings, MaintenanceService maint)
    {
        _buildings = buildings; _maint = maint;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    /// <summary>
    /// Shows the main page of the resident maintenance screen.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        var log = _maint.GetLog(building).Where(m => m.Status != "cancelled").ToList();
        return View(log);
    }
}
