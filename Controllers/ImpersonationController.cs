using System.Security.Claims;
using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

public class ImpersonationController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly ImpersonationService _impersonation;
    private readonly ILogger<ImpersonationController> _logger;

    public ImpersonationController(
        BuildingsService buildings,
        ImpersonationService impersonation,
        ILogger<ImpersonationController> logger)
    {
        _buildings = buildings;
        _impersonation = impersonation;
        _logger = logger;
    }

    private string CurrentUid => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    private string CurrentName => User.FindFirst(ClaimTypes.Name)?.Value ?? "user";
    private string CurrentEmail => User.FindFirst(ClaimTypes.Email)?.Value ?? "";
    private string CurrentRole => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

    // ============================================================
    // POST: /Impersonation/Enter (Admin → Resident)
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin")]
    [AdminPermission(AdminPermissions.ManageResidents)]
    public async Task<IActionResult> Enter(string aptId)
    {
        var buildingId = User.FindFirst("buildingId")?.Value;
        if (string.IsNullOrEmpty(buildingId)) return Forbid();

        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return NotFound();

        // ✅ Push الحالة الحالية للـ Stack
        PushCurrentContext(buildingId);

        // ✅ Sign in as resident
        var claims = BuildResidentClaims(building, apt, CurrentUid);
        await SignInAsync(claims);

        // Audit
        await _impersonation.LogEnterAsync(building, apt, CurrentUid, CurrentName);
        _logger.LogWarning("[Impersonation] {Admin} → Apt {Apt} (depth={Depth})",
            CurrentUid, apt.Number, _impersonation.StackDepth(HttpContext));

        return RedirectToAction("Index", "ResidentHome");
    }

    // ============================================================
    // POST: /Impersonation/Exit
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "resident,admin")]
    public async Task<IActionResult> Exit()
    {
        if (!_impersonation.IsImpersonating(HttpContext))
            return Forbid();

        var stack = _impersonation.ReadBackupStack(HttpContext);

        if (stack.Count == 0)
        {
            // Stack فاضي → Sign out
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LoginChoice", "Account");
        }

        // ✅ Pop آخر طبقة
        var previous = _impersonation.PopBackup(HttpContext);
        if (previous == null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LoginChoice", "Account");
        }

        // ✅ Sign in as previous context
        var claims = BuildBackupClaims(previous);
        await SignInAsync(claims);

        // Audit
        await _impersonation.LogExitAsync(previous.BuildingId, previous.Uid, previous.Name);
        _logger.LogWarning("[Impersonation] Exit to {Role} (depth={Depth})",
            previous.Role, _impersonation.StackDepth(HttpContext));

        // ✅ رجّع حسب الدور
        return previous.Role switch
        {
            "superadmin" => RedirectToAction("Index", "SuperAdminHome"),
            "admin" => RedirectToAction("Index", "AdminHome"),
            _ => RedirectToAction("Index", "Home")
        };
    }

    // ============================================================
    // POST: /Impersonation/EnterResidentWallet (Admin → Resident Wallet)
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> EnterResidentWallet(string aptId, string? buildingId = null)
    {
        // ✅ SuperAdmin: buildingId من الـ parameter
        // ✅ Admin: buildingId من الـ claims
        var targetBuildingId = User.IsInRole("superadmin")
            ? buildingId
            : User.FindFirstValue("buildingId");

        if (string.IsNullOrEmpty(targetBuildingId))
            return NotFound();

        var building = await _buildings.GetByIdAsync(targetBuildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return NotFound();

        // ✅ Push الحالة الحالية
        PushCurrentContext(targetBuildingId, isSuperAdmin: User.IsInRole("superadmin"));

        // ✅ Sign in as resident
        var claims = BuildResidentClaims(building, apt, CurrentUid);
        await SignInAsync(claims);

        await _impersonation.LogEnterAsync(building, apt, CurrentUid, CurrentName);
        _logger.LogWarning("[Impersonation] {User} → Apt {Apt} (Wallet, depth={Depth})",
            CurrentUid, apt.Number, _impersonation.StackDepth(HttpContext));

        return RedirectToAction("Index", "ResidentWallet");
    }

    // ============================================================
    // POST: /Impersonation/EnterBuildingAdmin (SuperAdmin → Admin)
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> EnterBuildingAdmin(string buildingId)
    {
        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        // ✅ Push الحالة الحالية (SuperAdmin)
        PushCurrentContext(buildingId, isSuperAdmin: true);

        // ✅ Sign in as Admin
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, CurrentUid),
            new(ClaimTypes.Name, CurrentName),
            new(ClaimTypes.Email, CurrentEmail),
            new(ClaimTypes.Role, "admin"),
            new("buildingId", building.Id),
            new("impersonated", "true"),
            new("impersonatedBy", CurrentUid),
            new("originalRole", "superadmin")
        };

        foreach (var p in AdminPermissions.TemplateAdminSuper)
            claims.Add(new Claim("perm", p));

        await SignInAsync(claims);

        await _impersonation.LogEnterBuildingAdminAsync(building, CurrentUid, CurrentName);
        _logger.LogWarning("[Impersonation] SuperAdmin entered building {B} as admin (depth={Depth})",
            building.Id, _impersonation.StackDepth(HttpContext));

        return RedirectToAction("Index", "AdminHome");
    }

    // ============================================================
    // Helpers
    // ============================================================

    /// بنجهّز بيانات الطبقة الحالية ونحطها في الـ Stack
    private void PushCurrentContext(string buildingId, bool isSuperAdmin = false)
    {
        // ✅ نخزّن الحالة الحالية دايمًا (حتى لو impersonated)
        var role = isSuperAdmin ? "superadmin" : CurrentRole;
        var buildingIds = User.FindAll("buildingId").Select(c => c.Value).ToList();
        var perms = User.FindAll("perm").Select(c => c.Value).ToList();

        var backup = new AdminBackupData
        {
            Uid = CurrentUid,
            Name = CurrentName,
            Email = CurrentEmail,
            Role = isSuperAdmin ? "superadmin" : CurrentRole,
            BuildingIds = User.FindAll("buildingId").Select(c => c.Value).ToList(),
            Permissions = User.FindAll("perm").Select(c => c.Value).ToList(),
            BuildingId = buildingId,
            StartedAt = DateTime.UtcNow.ToString("o")
        };

        _impersonation.PushBackup(HttpContext, backup);
    }

    private List<Claim> BuildResidentClaims(Building building, Apartment apt, string impersonatedBy)
    {
        var name = string.IsNullOrWhiteSpace(apt.Owner)
            ? Loc.T("Apartment_N", apt.Number)
            : apt.Owner;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "imp-" + apt.Id),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, "resident"),
            new("buildingId", building.Id),
            new("apartmentId", apt.Id),
            new("apartmentNumber", apt.Number.ToString()),
            new("impersonated", "true"),
            new("impersonatedBy", impersonatedBy)
        };

        foreach (var p in AdminPermissions.ResidentBasic)
            claims.Add(new Claim("perm", p));

        return claims;
    }

    private List<Claim> BuildBackupClaims(AdminBackupData backup)
    {
        var isSuperAdmin = backup.Role == "superadmin";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, backup.Uid),
            new(ClaimTypes.Name, backup.Name),
            new(ClaimTypes.Email, backup.Email),
            new(ClaimTypes.Role, isSuperAdmin ? "superadmin" : backup.Role)
        };

        foreach (var bId in backup.BuildingIds)
            claims.Add(new Claim("buildingId", bId));

        foreach (var p in backup.Permissions)
            claims.Add(new Claim("perm", p));

        return claims;
    }

    private async Task SignInAsync(List<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}