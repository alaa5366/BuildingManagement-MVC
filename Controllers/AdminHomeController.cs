using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "admin")]
public class AdminHomeController : Controller
{
    private readonly BuildingsService _buildings;
    public AdminHomeController(BuildingsService buildings) => _buildings = buildings;

    public async Task<IActionResult> Index()
    {
        var buildingId = User.FindFirstValue("buildingId");
        var building = buildingId == null ? null : await _buildings.GetByIdAsync(buildingId);
        return View(building);
    }
}
