using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "resident")]
public class ResidentHomeController : Controller
{
    private readonly BuildingsService _buildings;
    public ResidentHomeController(BuildingsService buildings) => _buildings = buildings;

    public async Task<IActionResult> Index()
    {
        var buildingId = User.FindFirstValue("buildingId");
        var aptNumber = User.FindFirstValue("apartmentNumber");
        var building = buildingId == null ? null : await _buildings.GetByIdAsync(buildingId);
        var apt = building?.Apartments.FirstOrDefault(a => a.Number.ToString() == aptNumber);

        ViewBag.Building = building;
        return View(apt);
    }
}
