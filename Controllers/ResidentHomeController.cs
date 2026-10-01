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
        var aptNumber = User.FindFirstValue("apartmentNumber");
        var building = buildingId == null ? null : await _buildings.GetByIdAsync(buildingId);
        var apt = building?.Apartments.FirstOrDefault(a => a.Number.ToString() == aptNumber);

        ViewBag.Building = building;
        return View(apt);
    }
    [HttpGet]
    public IActionResult Debug()
    {
        var claims = string.Join("\n", User.Claims.Select(c => $"{c.Type} = {c.Value}"));
        var isAuth = User.Identity?.IsAuthenticated ?? false;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "(none)";

        var text = $@"IsAuthenticated: {isAuth}
Role: {role}

=== Claims ===
{claims}";

        return Content(text, "text/plain", System.Text.Encoding.UTF8);
    }
}
