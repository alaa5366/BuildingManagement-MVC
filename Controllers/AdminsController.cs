using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin")]
[HasPermission(AdminPermissions.ManageAdmins)]
public class AdminsController : Controller
{
    private readonly AdminManagementService _admins;
    private readonly BuildingsService _buildings;

    public AdminsController(AdminManagementService admins, BuildingsService buildings)
    {
        _admins = admins;
        _buildings = buildings;
    }

    private string CurrentUserId =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    public async Task<IActionResult> Index(string? buildingId, int page = 1, int pageSize = 50)
    {
        ViewBag.Buildings = await _buildings.GetAllAsync();

        var allAdmins = await _admins.GetAdminsAsync(buildingId);
        var paged = BuildingManagementMvc.Models.PagedResult<BuildingManagementMvc.Models.AppUserDoc>
            .Create(allAdmins, page, pageSize);

        ViewBag.SelectedBuildingId = buildingId;
        ViewBag.RouteValues = new Dictionary<string, string?>
        {
            ["buildingId"] = buildingId,
            ["pageSize"] = pageSize.ToString()
        };

        return View(paged);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Buildings = await _buildings.GetAllAsync();
        ViewBag.AllPermissions = AdminPermissions.All;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string name, string phone, string buildingId, string pin, List<string> permissions)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone) ||
            string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(pin))
        {
            TempData["Error"] = Loc.T("All_Fields_Are_Required");
            ViewBag.Buildings = await _buildings.GetAllAsync();
            ViewBag.AllPermissions = AdminPermissions.All;
            return View();
        }

        try
        {
            await _admins.CreateAdminAsync(name, phone, buildingId, pin, permissions ?? new(), CurrentUserId);
            TempData["Message"] = Loc.T("Admin_Created_Successfully");
            return RedirectToAction(nameof(Index));
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = Loc.T("Creation_Failed") + ex.Message;
            ViewBag.Buildings = await _buildings.GetAllAsync();
            ViewBag.AllPermissions = AdminPermissions.All;
            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var admin = await _admins.GetAdminAsync(id);
        if (admin == null) return NotFound();

        ViewBag.Buildings = await _buildings.GetAllAsync();
        ViewBag.AllPermissions = AdminPermissions.All;
        return View(admin);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id, string name, string phone, bool isActive, List<string> permissions)
    {
        try
        {
            await _admins.UpdateAdminAsync(id, name, phone, permissions ?? new(), isActive, CurrentUserId);
            TempData["Message"] = Loc.T("Updated");
            return RedirectToAction(nameof(Index));
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = Loc.T("Update_Failed") + ex.Message;
            var admin = await _admins.GetAdminAsync(id);
            ViewBag.Buildings = await _buildings.GetAllAsync();
            ViewBag.AllPermissions = AdminPermissions.All;
            return View(admin);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePin(string id, string newPin)
    {
        try
        {
            await _admins.ChangePinAsync(id, newPin, CurrentUserId);
            TempData["Message"] = Loc.T("PIN_Changed");
        }
        catch (System.Exception ex)
        {
            TempData["Error"] = Loc.T("PIN_Change_Failed") + ex.Message;
        }
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string id)
    {
        try { await _admins.DeactivateAsync(id, CurrentUserId); TempData["Message"] = Loc.T("Disabled_4"); }
        catch (System.Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(string id)
    {
        try { await _admins.ActivateAsync(id, CurrentUserId); TempData["Message"] = Loc.T("Activated"); }
        catch (System.Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try { await _admins.DeleteAsync(id, CurrentUserId); TempData["Message"] = Loc.T("Deleted"); }
        catch (System.Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}