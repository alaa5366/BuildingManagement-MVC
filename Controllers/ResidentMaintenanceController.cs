using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "resident")]
public class ResidentMaintenanceController : Controller
{
    private readonly TargetMaintenanceService _maintenance;

    public ResidentMaintenanceController(TargetMaintenanceService maintenance) => _maintenance = maintenance;

    private string BuildingId => User.FindFirstValue("buildingId")!;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var log = await _maintenance.GetResidentLogAsync(BuildingId, HttpContext.RequestAborted);
        return View(log);
    }
}
