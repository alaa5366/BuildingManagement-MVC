using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "admin")]
public class AdminHomeController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly BuildingMapService _map;

    public AdminHomeController(BuildingsService buildings, BuildingMapService map)
    {
        _buildings = buildings;
        _map = map;
    }

    public async Task<IActionResult> Index()
    {
        var buildingId = User.FindFirstValue("buildingId");
        if (string.IsNullOrEmpty(buildingId)) return NotFound();

        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        var currentMonth = WalletService.CurrentMonthKey();
        var vm = await _map.BuildAsync(building, currentMonth);

        return View(vm);
    }
    // ============================================================
    // ✅ Phase 24.3 — Apartment Details (AJAX)
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> ApartmentDetails(string aptId)
    {
        var buildingId = User.FindFirstValue("buildingId");
        if (string.IsNullOrEmpty(buildingId)) return Unauthorized();

        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        var currentMonth = WalletService.CurrentMonthKey();
        var vm = await _map.GetApartmentDetailsAsync(building, aptId, currentMonth);
        if (vm == null) return NotFound();

        return PartialView("_ApartmentDetailsModal", vm);
    }
}