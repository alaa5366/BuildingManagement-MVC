using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin")]
[HasPermission(AdminPermissions.ManageAdmins)]
public class PermissionsController : Controller
{
    private readonly AdminManagementService _admins;
    private readonly BuildingsService _buildings;

    public PermissionsController(AdminManagementService admins, BuildingsService buildings)
    {
        _admins = admins;
        _buildings = buildings;
    }

    private string CurrentUserId =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    public async Task<IActionResult> Index(string? buildingId)
    {
        var admins = await _admins.GetAdminsAsync();
        ViewBag.AllPermissions = AdminPermissions.All;
        ViewBag.GroupedPermissions = GetGroupedPermissions();
        ViewBag.Buildings = await _buildings.GetAllAsync();
        ViewBag.SelectedBuildingId = buildingId;
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
        List<string> perms = template switch
        {
            "full" => AdminPermissions.TemplateAdminFull,
            "super" => AdminPermissions.TemplateAdminSuper,      // ← جديد
            "financial" => AdminPermissions.TemplateAdminFinancial,
            "maintenance" => AdminPermissions.TemplateAdminMaintenance,
            _ => new List<string>()
        };

        await _admins.SyncPermissionsAsync(adminId, perms, CurrentUserId);

        var label = template switch
        {
            "full" => "كامل (12)",
            "super" => "Super (22)",
            "financial" => "مالي (7)",
            "maintenance" => "صيانة (4)",
            _ => template
        };

        TempData["Message"] = $"✅ تم تطبيق قالب {label} — {perms.Count} صلاحية.";
        return RedirectToAction(nameof(Index));
    }
    private static Dictionary<AdminPermissions.PermissionCategory, List<string>> GetGroupedPermissions()
    {
        var result = new Dictionary<AdminPermissions.PermissionCategory, List<string>>();

        foreach (var p in AdminPermissions.All)
        {
            var cat = AdminPermissions.CategoryOf(p);
            if (!result.ContainsKey(cat))
                result[cat] = new List<string>();
            result[cat].Add(p);
        }

        return result;
    }
}