using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  AdminHomeController — لوحة تحكم الأدمن الرئيسية (Building Map)
// ============================================================
/// <summary>
/// Admin dashboard: building map, apartment details modal and key statistics.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>admin</c>.
/// </remarks>
[Authorize(Roles = "admin")]
public class AdminHomeController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly BuildingMapService _map;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminHomeController"/> class.
    /// </summary>
    public AdminHomeController(BuildingsService buildings, BuildingMapService map)
    {
        _buildings = buildings;
        _map = map;
    }

    /// <summary>
    /// Shows the main page of the admin home screen.
    /// </summary>
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
    /// <summary>
    /// Returns the apartment details modal (partial view) shown from the building map.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
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