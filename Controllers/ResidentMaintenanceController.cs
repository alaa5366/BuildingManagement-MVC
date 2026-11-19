using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

/// <summary>ResidentMaintenanceController coordinates the corresponding application functionality.</summary>
[Authorize(Roles = "resident")]
public class ResidentMaintenanceController : Controller
{
    private readonly TargetMaintenanceService _maintenance;

    public ResidentMaintenanceController(TargetMaintenanceService maintenance) => _maintenance = maintenance;

    private string BuildingId => User.FindFirstValue("buildingId")!;

    /// <summary>Displays the current records and associated management actions.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var log = await _maintenance.GetResidentLogAsync(BuildingId, HttpContext.RequestAborted);
        return View(log);
    }
}
