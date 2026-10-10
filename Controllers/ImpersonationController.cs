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

    // ═══════════════════════════════════════════════════════════
    // POST: /Impersonation/EnterResidentWallet
    // ═══════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> EnterResidentWallet(string aptId, string? buildingId = null)
    {
        var targetBuildingId = User.IsInRole("superadmin")
            ? buildingId
            : User.FindFirstValue("buildingId");

        if (string.IsNullOrEmpty(targetBuildingId))
            return NotFound();

        var building = await _buildings.GetByIdAsync(targetBuildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return NotFound();

        PushCurrentContext(targetBuildingId, isSuperAdmin: User.IsInRole("superadmin"));

        var claims = BuildResidentClaims(building, apt, CurrentUid);
        await SignInAsync(claims);

        await _impersonation.LogEnterAsync(building, apt, CurrentUid, CurrentName);
        _logger.LogWarning("[Impersonation] {User} → Apt {Apt} (Wallet)", CurrentUid, apt.Number);

        return RedirectToAction("Index", "ResidentWallet");
    }

    // ═══════════════════════════════════════════════════════════
    // POST: /Impersonation/EnterResidentHome
    // ═══════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> EnterResidentHome(string aptId, string? buildingId = null)
    {
        var targetBuildingId = User.IsInRole("superadmin")
            ? buildingId
            : User.FindFirstValue("buildingId");

        if (string.IsNullOrEmpty(targetBuildingId))
            return NotFound();

        var building = await _buildings.GetByIdAsync(targetBuildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return NotFound();

        PushCurrentContext(targetBuildingId, isSuperAdmin: User.IsInRole("superadmin"));

        var claims = BuildResidentClaims(building, apt, CurrentUid);
        await SignInAsync(claims);

        await _impersonation.LogEnterAsync(building, apt, CurrentUid, CurrentName);
        _logger.LogWarning("[Impersonation] {User} → Apt {Apt} (Home)", CurrentUid, apt.Number);

        return RedirectToAction("Index", "ResidentHome");
    }

    // ═══════════════════════════════════════════════════════════
    // POST: /Impersonation/EnterBuildingAdmin
    // ═══════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> EnterBuildingAdmin(string buildingId)
    {
        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        PushCurrentContext(buildingId, isSuperAdmin: true);

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
        _logger.LogWarning("[Impersonation] SuperAdmin entered building {B} as admin", building.Id);

        return RedirectToAction("Index", "AdminHome");
    }

    // ═══════════════════════════════════════════════════════════
    // POST: /Impersonation/Exit
    // ═══════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "resident,admin,superadmin")]
    public async Task<IActionResult> Exit()
    {
        if (!_impersonation.IsImpersonating(HttpContext))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LoginChoice", "Account");
        }

        // ✅ اقرأ الـ Stack قبل
        var stackBefore = _impersonation.ReadBackupStack(HttpContext);
        _logger.LogWarning(
            "[Impersonation] Exit BEFORE. Count={Count}, roles=[{Roles}]",
            stackBefore.Count,
            string.Join(", ", stackBefore.Select(s => s.Role)));

        // ✅ Pop
        var previous = _impersonation.PopBackup(HttpContext);
        if (previous == null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LoginChoice", "Account");
        }

        // ✅ اقرأ الـ Stack بعد
        var stackAfter = _impersonation.ReadBackupStack(HttpContext);
        _logger.LogWarning(
            "[Impersonation] Exit AFTER. Count={Count}, roles=[{Roles}], poppedRole={Popped}",
            stackAfter.Count,
            string.Join(", ", stackAfter.Select(s => s.Role)),
            previous.Role);

        // ✅ لو لسه فيه طبقات في الـ Stack، يعني لسه في Impersonation
        var stillImpersonating = stackAfter.Count > 0;

        var claims = BuildBackupClaims(previous, stillImpersonating);
        await SignInAsync(claims);

        await _impersonation.LogExitAsync(previous.BuildingId, previous.Uid, previous.Name);
        _logger.LogWarning("[Impersonation] Exit to {Role} (stillImpersonating={Still})",
            previous.Role, stillImpersonating);

        return previous.Role switch
        {
            "superadmin" => RedirectToAction("Index", "SuperAdminHome"),
            "admin" => RedirectToAction("Index", "AdminHome"),
            _ => RedirectToAction("Index", "Home")
        };
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════

    private void PushCurrentContext(string buildingId, bool isSuperAdmin = false)
    {
        var stackBefore = _impersonation.ReadBackupStack(HttpContext);

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

        _logger.LogWarning(
            "[Impersonation] Push BEFORE. Count={Count}, roles=[{Roles}]. Adding: {NewRole} (isSuperAdmin={IsSuper})",
            stackBefore.Count,
            string.Join(", ", stackBefore.Select(s => s.Role)),
            backup.Role,
            isSuperAdmin);

        _impersonation.PushBackup(HttpContext, backup);

        var stackAfter = _impersonation.ReadBackupStack(HttpContext);
        _logger.LogWarning(
            "[Impersonation] Push AFTER. Count={Count}, roles=[{Roles}]",
            stackAfter.Count,
            string.Join(", ", stackAfter.Select(s => s.Role)));
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

    private List<Claim> BuildBackupClaims(AdminBackupData backup, bool stillImpersonating)
    {
        var isSuperAdmin = backup.Role == "superadmin";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, backup.Uid),
            new(ClaimTypes.Name, backup.Name),
            new(ClaimTypes.Email, backup.Email),
            new(ClaimTypes.Role, isSuperAdmin ? "superadmin" : backup.Role)
        };

        if (isSuperAdmin || SqlAuthService.IsSuperAdminEmail(backup.Email))
        {
            claims.Add(new Claim("originalRole", "superadmin"));
        }

        // ✅ لو لسه في Impersonation (Stack فيه طبقات)، ضيف الـ claim
        if (stillImpersonating)
        {
            claims.Add(new Claim("impersonated", "true"));
        }

        foreach (var bId in backup.BuildingIds)
            claims.Add(new Claim("buildingId", bId));

        foreach (var p in backup.Permissions)
            claims.Add(new Claim("perm", p));

        return claims;
    }

    private async Task SignInAsync(List<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}