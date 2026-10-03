using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "resident")]
public class ResidentHomeController : Controller
{
    private readonly BuildingsService _buildings;
    public ResidentHomeController(BuildingsService buildings) => _buildings = buildings;

    public async Task<IActionResult> Index()
    {
        var buildingId = User.FindFirstValue("buildingId");
        var aptId = User.FindFirstValue("apartmentId");
        var aptNumber = User.FindFirstValue("apartmentNumber");

        var building = buildingId == null ? null : await _buildings.GetByIdAsync(buildingId);

        Apartment? apt = null;
        if (building != null)
        {
            // ✅ جرّب بالـ Id الأول (أدق)
            if (!string.IsNullOrWhiteSpace(aptId))
                apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);

            // ✅ Fallback للرقم (للتوافق)
            if (apt == null && !string.IsNullOrWhiteSpace(aptNumber))
                apt = building.Apartments.FirstOrDefault(a => a.Number.ToString() == aptNumber);
        }

        ViewBag.Building = building;
        return View(apt);
    }

    [HttpGet]
    public IActionResult Debug()
    {
        var claims = string.Join("\n", User.Claims.Select(c => $"{c.Type} = {c.Value}"));
        var isAuth = User.Identity?.IsAuthenticated ?? false;
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "(none)";

        var text = $@"IsAuthenticated: {isAuth}
Role: {role}

=== Claims ===
{claims}";

        return Content(text, "text/plain", System.Text.Encoding.UTF8);
    }
}