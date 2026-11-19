using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  AdminsController	إدارة الأدمنز (SuperAdmin فقط)
// ============================================================
/// <summary>
/// Super admin screens for creating and managing admin accounts, their buildings and PINs.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>superadmin</c>.
/// </remarks>
[Authorize(Roles = "superadmin")]
[HasPermission(AdminPermissions.ManageAdmins)]
public class AdminsController : Controller
{
    private readonly AdminManagementService _admins;
    private readonly BuildingsService _buildings;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminsController"/> class.
    /// </summary>
    public AdminsController(AdminManagementService admins, BuildingsService buildings)
    {
        _admins = admins;
        _buildings = buildings;
    }

    private string CurrentUserId =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    /// <summary>
    /// Shows the main page of the admins screen.
    /// </summary>
    public IActionResult Index(string? buildingId, int page = 1, int pageSize = 50)
    {
        // The legacy Users table has been retired. Keep the old URL as a compatibility
        // entry point, but send administrators to the canonical UserAccounts inventory.
        // Do not fall back to recreating or querying the retired Users table.
        return RedirectToAction("Index", "AccountManagement");
    }

    /// <summary>
    /// Creates the admins create page.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Buildings = await _buildings.GetSummariesAsync();
        ViewBag.AllPermissions = AdminPermissions.All;
        return View();
    }

    /// <summary>
    /// Handles the create form submission of the admins screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string name, string phone, string buildingId, string pin, List<string> permissions)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone) ||
            string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(pin))
        {
            TempData["Error"] = Loc.T("All_Fields_Are_Required");
            ViewBag.Buildings = await _buildings.GetSummariesAsync();
            ViewBag.AllPermissions = AdminPermissions.All;
            return View();
        }

        try
        {
            await _admins.CreateAdminAsync(name, phone, buildingId, pin, permissions ?? new(), CurrentUserId);
            TempData["Message"] = Loc.T("Admin_Created_Successfully");
            return RedirectToAction(nameof(Index));
        }
        catch (System.Exception)
        {
            TempData["Error"] = Loc.T("Creation_Failed");
            ViewBag.Buildings = await _buildings.GetSummariesAsync();
            ViewBag.AllPermissions = AdminPermissions.All;
            return View();
        }
    }

    /// <summary>
    /// Edits the admins edit page.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var admin = await _admins.GetAdminAsync(id);
        if (admin == null) return NotFound();

        ViewBag.Buildings = await _buildings.GetSummariesAsync();
        ViewBag.AllPermissions = AdminPermissions.All;
        return View(admin);
    }

    /// <summary>
    /// Handles the edit form submission of the admins screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
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
        catch (System.Exception)
        {
            TempData["Error"] = Loc.T("Update_Failed");
            var admin = await _admins.GetAdminAsync(id);
            ViewBag.Buildings = await _buildings.GetSummariesAsync();
            ViewBag.AllPermissions = AdminPermissions.All;
            return View(admin);
        }
    }

    /// <summary>
    /// Changes the PIN.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePin(string id, string newPin)
    {
        try
        {
            await _admins.ChangePinAsync(id, newPin, CurrentUserId);
            TempData["Message"] = Loc.T("PIN_Changed");
        }
        catch (System.Exception)
        {
            TempData["Error"] = Loc.T("PIN_Change_Failed");
        }
        return RedirectToAction(nameof(Edit), new { id });
    }

    /// <summary>
    /// Deactivates an admin account so that it can no longer sign in.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string id)
    {
        try { await _admins.DeactivateAsync(id, CurrentUserId); TempData["Message"] = Loc.T("Disabled_4"); }
        catch (System.Exception) { TempData["Error"] = Loc.T("An_Unexpected_Error_Occurred"); }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Re-activates a previously deactivated admin account.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(string id)
    {
        try { await _admins.ActivateAsync(id, CurrentUserId); TempData["Message"] = Loc.T("Activated"); }
        catch (System.Exception) { TempData["Error"] = Loc.T("An_Unexpected_Error_Occurred"); }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Handles the delete form submission of the admins screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try { await _admins.DeleteAsync(id, CurrentUserId); TempData["Message"] = Loc.T("Deleted"); }
        catch (System.Exception) { TempData["Error"] = Loc.T("An_Unexpected_Error_Occurred"); }
        return RedirectToAction(nameof(Index));
    }
}
