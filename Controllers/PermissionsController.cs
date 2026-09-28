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
public class PermissionsController : Controller
{
    private readonly AdminManagementService _admins;

    public PermissionsController(AdminManagementService admins) => _admins = admins;

    private string CurrentUserId =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    public async Task<IActionResult> Index()
    {
        var admins = await _admins.GetAdminsAsync();
        ViewBag.AllPermissions = AdminPermissions.All;
        return View(admins);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string adminId, string permission, bool enabled)
    {
        var admin = await _admins.GetAdminAsync(adminId);
        if (admin == null) return NotFound();

        var perms = admin.Permissions ?? new List<string>();
        if (enabled && !perms.Contains(permission)) perms.Add(permission);
        if (!enabled && perms.Contains(permission)) perms.Remove(permission);

        await _admins.SyncPermissionsAsync(adminId, perms, CurrentUserId);
        return Json(new { success = true, count = perms.Count });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyTemplate(string adminId, string template)
    {
        var perms = template switch
        {
            "full" => AdminPermissions.TemplateAdminFull,
            "financial" => AdminPermissions.TemplateAdminFinancial,
            "maintenance" => AdminPermissions.TemplateAdminMaintenance,
            _ => new List<string>()
        };

        await _admins.SyncPermissionsAsync(adminId, perms, CurrentUserId);
        TempData["Message"] = "تم تطبيق القالب.";
        return RedirectToAction(nameof(Index));
    }
}