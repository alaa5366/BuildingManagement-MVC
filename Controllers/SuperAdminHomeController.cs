using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin")]
public class SuperAdminHomeController : Controller
{
    private readonly BuildingsService _buildings;
    public SuperAdminHomeController(BuildingsService buildings) => _buildings = buildings;

    public async Task<IActionResult> Index()
    {
        var buildings = await _buildings.GetAllAsync();
        return View(buildings);
    }
}
