using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "admin")]
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

    private string AdminUid => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    private string AdminName => User.FindFirst(ClaimTypes.Name)?.Value ?? "admin";

    // ============================================================
    // POST: /Impersonation/Enter
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enter(string aptId)
    {
        var buildingId = User.FindFirst("buildingId")?.Value;
        if (string.IsNullOrEmpty(buildingId)) return Forbid();

        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return NotFound();

        // 1. احفظ بيانات الأدمن (لو لسه ما اتحفظتش)
        if (!_impersonation.IsImpersonating(HttpContext))
        {
            var backup = new AdminBackupData
            {
                Uid = AdminUid,
                Name = AdminName,
                Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "",
                Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "admin",
                BuildingIds = User.FindAll("buildingId").Select(c => c.Value).ToList(),
                Permissions = User.FindAll("perm").Select(c => c.Value).ToList(),
                BuildingId = buildingId,
                StartedAt = DateTime.UtcNow.ToString("o")
            };

            _impersonation.SaveBackup(HttpContext, backup);
        }

        // 2. سجّل الدخول كساكن
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "imp-" + apt.Id),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(apt.Owner) ? $"شقة {apt.Number}" : apt.Owner),
            new(ClaimTypes.Role, "resident"),
            new("buildingId", building.Id),
            new("apartmentId", apt.Id),
            new("apartmentNumber", apt.Number.ToString()),
            new("impersonated", "true")
        };

        // صلاحيات الساكن
        foreach (var p in AdminPermissions.ResidentBasic)
            claims.Add(new Claim("perm", p));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        // 3. Audit
        await _impersonation.LogEnterAsync(building, apt, AdminUid, AdminName);

        _logger.LogWarning("[Impersonation] Admin {Admin} → Apt {Apt}", AdminUid, apt.Number);

        return RedirectToAction("Index", "ResidentHome");
    }

    // ============================================================
    // POST: /Impersonation/Exit
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Exit()
    {
        var backup = _impersonation.ReadBackup(HttpContext);
        if (backup == null)
        {
            // مفيش backup — بس نعمل logout ونرجع لصفحة الدخول
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LoginChoice", "Account");
        }

        // 1. ابني Claims الأدمن تاني
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, backup.Uid),
            new(ClaimTypes.Name, backup.Name),
            new(ClaimTypes.Email, backup.Email),
            new(ClaimTypes.Role, backup.Role)
        };

        foreach (var bId in backup.BuildingIds)
            claims.Add(new Claim("buildingId", bId));

        foreach (var p in backup.Permissions)
            claims.Add(new Claim("perm", p));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        // 2. امسح backup
        _impersonation.ClearBackup(HttpContext);

        // 3. Audit
        await _impersonation.LogExitAsync(backup.BuildingId, backup.Uid, backup.Name);

        _logger.LogWarning("[Impersonation] Admin {Admin} returned from impersonation", backup.Uid);

        return RedirectToAction("Index", "AdminHome");
    }
}