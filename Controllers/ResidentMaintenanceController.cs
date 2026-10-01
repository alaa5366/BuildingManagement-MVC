using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

// نفس شاشة js/views/resident/resident-maintenance.js (عرض فقط)
[Authorize(Roles = "resident")]
public class ResidentMaintenanceController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly MaintenanceService _maint;

    public ResidentMaintenanceController(BuildingsService buildings, MaintenanceService maint)
    {
        _buildings = buildings; _maint = maint;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        var log = _maint.GetLog(building).Where(m => m.Status != "cancelled").ToList();
        return View(log);
    }
}
