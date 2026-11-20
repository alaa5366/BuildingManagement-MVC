using System.Security.Claims;
using System.Text.Json;
using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  ImpersonationController	الانتحال (أدمن → ساكن / SuperAdmin → Admin)
// ============================================================
/// <summary>
/// Lets super admins and admins enter a building or a resident's session and return to their own account. Every transition is audited.
/// </summary>
public class ImpersonationController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly ImpersonationService _impersonation;
    private readonly ILogger<ImpersonationController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImpersonationController"/> class.
    /// </summary>
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
    /// <summary>
    /// Enters the resident wallet.
    /// </summary>
    /// <remarks>
    /// HTTP POST. Requires one of the roles <c>admin,superadmin</c>.
    /// </remarks>
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

        if (!PushCurrentContext(targetBuildingId, isSuperAdmin: User.IsInRole("superadmin")))
            return Conflict("Maximum impersonation depth reached. Exit an existing impersonation session first.");

        var claims = BuildResidentClaims(building, apt, CurrentUid);
        await SignInAsync(claims);

        await _impersonation.LogEnterAsync(building, apt, CurrentUid, CurrentName);
        _logger.LogWarning("[Impersonation] {User} → Apt {Apt} (Wallet)", CurrentUid, apt.Number);

        return RedirectToAction("Index", "ResidentWallet");
    }

    // ═══════════════════════════════════════════════════════════
    // POST: /Impersonation/EnterResidentHome
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Enters the resident home.
    /// </summary>
    /// <remarks>
    /// HTTP POST. Requires one of the roles <c>admin,superadmin</c>.
    /// </remarks>
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

        if (!PushCurrentContext(targetBuildingId, isSuperAdmin: User.IsInRole("superadmin")))
            return Conflict("Maximum impersonation depth reached. Exit an existing impersonation session first.");

        var claims = BuildResidentClaims(building, apt, CurrentUid);
        await SignInAsync(claims);

        await _impersonation.LogEnterAsync(building, apt, CurrentUid, CurrentName);
        _logger.LogWarning("[Impersonation] {User} → Apt {Apt} (Home)", CurrentUid, apt.Number);

        return RedirectToAction("Index", "ResidentHome");
    }

    // ═══════════════════════════════════════════════════════════
    // POST: /Impersonation/EnterBuildingAdmin
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Enters the building admin.
    /// </summary>
    /// <remarks>
    /// HTTP POST. Requires one of the roles <c>superadmin</c>.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> EnterBuildingAdmin(string buildingId)
    {
        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        if (!PushCurrentContext(buildingId, isSuperAdmin: true))
            return Conflict("Maximum impersonation depth reached. Exit an existing impersonation session first.");

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

        AddImpersonationActorClaims(claims, building.Id, apartmentId: null);

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
    /// <summary>
    /// Handles the exit form submission of the impersonation screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST. Requires one of the roles <c>resident,admin,superadmin</c>.
    /// </remarks>
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
        _logger.LogDebug(
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
        _logger.LogDebug(
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

    private bool PushCurrentContext(string buildingId, bool isSuperAdmin = false)
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
            ImpersonationActorUid = User.FindFirst("impersonationActorUid")?.Value ?? "",
            ImpersonationActorRole = User.FindFirst("impersonationActorRole")?.Value ?? "",
            ImpersonationActorEmail = User.FindFirst("impersonationActorEmail")?.Value ?? "",
            ImpersonationActorPermissions = User.FindFirst("impersonationActorPermissions")?.Value ?? "",
            ImpersonationActorTwoFactorVerified = User.FindFirst("impersonationActorTwoFactorVerified")?.Value
                ?? (User.HasClaim("twoFactorVerified", "true") ? "true" : "false"),
            ImpersonationActorSecurityStamp = User.FindFirst("impersonationActorSecurityStamp")?.Value
                ?? User.FindFirst("legacySuperAdminSecurityStamp")?.Value
                ?? User.FindFirst("securityStamp")?.Value ?? "",
            TwoFactorVerified = User.HasClaim("twoFactorVerified", "true")
                || User.HasClaim("impersonationActorTwoFactorVerified", "true"),
            StartedAt = DateTime.UtcNow.ToString("o")
        };

        _logger.LogDebug(
            "[Impersonation] Push BEFORE. Count={Count}, roles=[{Roles}]. Adding: {NewRole} (isSuperAdmin={IsSuper})",
            stackBefore.Count,
            string.Join(", ", stackBefore.Select(s => s.Role)),
            backup.Role,
            isSuperAdmin);

        var pushed = _impersonation.PushBackup(HttpContext, backup);
        if (!pushed)
            return false;

        var stackAfter = _impersonation.ReadBackupStack(HttpContext);
        _logger.LogDebug(
            "[Impersonation] Push AFTER. Count={Count}, roles=[{Roles}]",
            stackAfter.Count,
            string.Join(", ", stackAfter.Select(s => s.Role)));
        return true;
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

        AddImpersonationActorClaims(claims, building.Id, apt.Id);

        foreach (var p in AdminPermissions.ResidentBasic)
            claims.Add(new Claim("perm", p));

        return claims;
    }

    private void AddImpersonationActorClaims(List<Claim> claims, string buildingId, string? apartmentId)
    {
        // Nested impersonation keeps the original initiating identity; it must not
        // replace it with the synthetic/intermediate principal.
        var actorUid = User.HasClaim("impersonated", "true")
            ? User.FindFirst("impersonationActorUid")?.Value
            : CurrentUid;
        var actorRole = User.HasClaim("impersonated", "true")
            ? User.FindFirst("impersonationActorRole")?.Value
            : CurrentRole;
        var actorEmail = User.HasClaim("impersonated", "true")
            ? User.FindFirst("impersonationActorEmail")?.Value
            : CurrentEmail;
        var actorPermissions = User.HasClaim("impersonated", "true")
            ? User.FindFirst("impersonationActorPermissions")?.Value
            : JsonSerializer.Serialize(User.FindAll("perm").Select(c => c.Value).Distinct().OrderBy(p => p).ToList());
        var actorTwoFactorVerified = User.HasClaim("impersonated", "true")
            ? User.FindFirst("impersonationActorTwoFactorVerified")?.Value
            : (User.HasClaim("twoFactorVerified", "true") ? "true" : "false");
        var actorSecurityStamp = User.HasClaim("impersonated", "true")
            ? User.FindFirst("impersonationActorSecurityStamp")?.Value
            : User.FindFirst("legacySuperAdminSecurityStamp")?.Value
                ?? User.FindFirst("securityStamp")?.Value;

        if (!string.IsNullOrWhiteSpace(actorUid))
            claims.Add(new Claim("impersonationActorUid", actorUid));
        if (!string.IsNullOrWhiteSpace(actorRole))
            claims.Add(new Claim("impersonationActorRole", actorRole));
        if (!string.IsNullOrWhiteSpace(actorEmail))
            claims.Add(new Claim("impersonationActorEmail", actorEmail));
        if (!string.IsNullOrWhiteSpace(actorPermissions))
            claims.Add(new Claim("impersonationActorPermissions", actorPermissions));
        if (!string.IsNullOrWhiteSpace(actorTwoFactorVerified))
            claims.Add(new Claim("impersonationActorTwoFactorVerified", actorTwoFactorVerified));
        if (!string.IsNullOrWhiteSpace(actorSecurityStamp))
            claims.Add(new Claim("impersonationActorSecurityStamp", actorSecurityStamp));

        claims.Add(new Claim("impersonationTargetBuildingId", buildingId));
        if (!string.IsNullOrWhiteSpace(apartmentId))
            claims.Add(new Claim("impersonationTargetApartmentId", apartmentId));
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

        if (backup.Role is "admin" or "superadmin")
            claims.Add(new Claim("twoFactorVerified", backup.TwoFactorVerified ? "true" : "false"));

        if (isSuperAdmin)
        {
            claims.Add(new Claim("originalRole", "superadmin"));
        }
        if (isSuperAdmin && !string.IsNullOrWhiteSpace(backup.ImpersonationActorSecurityStamp))
            claims.Add(new Claim("legacySuperAdminSecurityStamp", backup.ImpersonationActorSecurityStamp));

        // ✅ لو لسه في Impersonation (Stack فيه طبقات)، ضيف الـ claim
        if (stillImpersonating)
        {
            claims.Add(new Claim("impersonated", "true"));
            if (!string.IsNullOrWhiteSpace(backup.ImpersonationActorUid))
            {
                claims.Add(new Claim("impersonationActorUid", backup.ImpersonationActorUid));
                claims.Add(new Claim("impersonatedBy", backup.ImpersonationActorUid));
            }
            if (!string.IsNullOrWhiteSpace(backup.ImpersonationActorRole))
                claims.Add(new Claim("impersonationActorRole", backup.ImpersonationActorRole));
            if (!string.IsNullOrWhiteSpace(backup.ImpersonationActorEmail))
                claims.Add(new Claim("impersonationActorEmail", backup.ImpersonationActorEmail));
            if (!string.IsNullOrWhiteSpace(backup.ImpersonationActorPermissions))
                claims.Add(new Claim("impersonationActorPermissions", backup.ImpersonationActorPermissions));
            if (!string.IsNullOrWhiteSpace(backup.ImpersonationActorTwoFactorVerified))
                claims.Add(new Claim("impersonationActorTwoFactorVerified", backup.ImpersonationActorTwoFactorVerified));
            if (!string.IsNullOrWhiteSpace(backup.ImpersonationActorSecurityStamp))
                claims.Add(new Claim("impersonationActorSecurityStamp", backup.ImpersonationActorSecurityStamp));
            if (!string.IsNullOrWhiteSpace(backup.BuildingId))
                claims.Add(new Claim("impersonationTargetBuildingId", backup.BuildingId));
        }

        var restoredBuildingIds = backup.BuildingIds
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);
        if (backup.Role == "admin" && !string.IsNullOrWhiteSpace(backup.BuildingId))
            restoredBuildingIds.Add(backup.BuildingId);

        foreach (var bId in restoredBuildingIds)
            claims.Add(new Claim("buildingId", bId));

        foreach (var p in backup.Permissions)
            claims.Add(new Claim("perm", p));

        return claims;
    }

    private async Task SignInAsync(List<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // جلسة الانتحال لها نفس عمر كوكي الرجوع (ساعتين) ومابتتجددش —
        // عشان مايحصلش إن الكوكي الأصلي يموت والشخص يفضل عالق كساكن
        var props = new AuthenticationProperties();
        if (claims.Any(c => c.Type == "impersonated" && c.Value == "true"))
        {
            props.IsPersistent = false;
            props.AllowRefresh = false;
            props.ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2);
        }
        else if (claims.Any(c => c.Type == ClaimTypes.Role && (c.Value == "admin" || c.Value == "superadmin")))
        {
            // الرجوع لحساب الأدمن الأصلي: نفس حد الـ 12 ساعة
            props.ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12);
            props.AllowRefresh = false;
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            props);
    }
}
