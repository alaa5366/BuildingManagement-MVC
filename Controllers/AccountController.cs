﻿using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;
// ============================================================
// AccountController نقطة الدخول الوحيدة للنظام(UnifiedLogin, QuickLogin, SetPin, Logout)
// ============================================================
/// <summary>
/// Handles sign-in for residents, admins and super admins: unified login, account selection, QR quick login, set-PIN links, language-neutral lookups for the login page, and sign-out.
/// </summary>
/// <remarks>
/// Access: anonymous.
/// </remarks>
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IdentityAccountAuthenticationService _identityAuth;
    private readonly BuildingsService _buildings;
    private readonly LoginThrottle _throttle;
    private readonly TwoFactorService _twoFactor;
    private readonly QrGeneratorService _qrGenerator;
    private readonly TargetQrService _targetQr;
    private readonly ILogger<AccountController> _log;
    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();
    // مفتاح قفل موحّد لكل طرق الدخول: الرقم بعد التطبيع (0100.. = +20100.. = 20100..) أو الإيميل
    private static string AcctKey(string? id)
    {
        var s = (id ?? "").Trim();
        if (s.Contains('@')) return "acct:" + s.ToLowerInvariant();
        var p = AuthHelpers.NormalizePhone(s);
        return "acct:" + (string.IsNullOrEmpty(p) ? s.ToLowerInvariant() : p);
    }

    private static string LockedMsg => Loc.T("Too_Many_Failed_Login_Attempts_Try");

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountController"/> class.
    /// </summary>
    public AccountController(
        IdentityAccountAuthenticationService identityAuth,
        BuildingsService buildings,
        LoginThrottle throttle,
        TwoFactorService twoFactor,
        QrGeneratorService qrGenerator,
        TargetQrService targetQr,
        ILogger<AccountController> logger)
    {
        _identityAuth = identityAuth;
        _buildings = buildings;
        _throttle = throttle;
        _twoFactor = twoFactor;
        _qrGenerator = qrGenerator;
        _targetQr = targetQr;
        _log = logger;
    }

    /// <summary>
    /// Signs in the choice.
    /// </summary>
    public IActionResult LoginChoice() => RedirectToAction("UnifiedLogin");

    // ═══════════════════════════════════════════════════════════
    // 1. Resident Login
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Signs in the resident.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> LoginResident()
    {
        ViewBag.Buildings = await _buildings.GetSummariesAsync();
        return View(new ResidentLoginVm());
    }

    /// <summary>
    /// Signs in the resident.
    /// </summary>
    /// <remarks>
    /// HTTP POST. Rate limited.
    /// </remarks>
    [HttpPost]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginResident(ResidentLoginVm vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Buildings = await _buildings.GetSummariesAsync();
            return View(vm);
        }

        var throttleKey = AcctKey(vm.Phone);
        if (_throttle.IsLocked(throttleKey, ClientIp))
        {
            ViewBag.Error = LockedMsg;
            ViewBag.Buildings = await _buildings.GetSummariesAsync();
            return View(vm);
        }

        // Authenticate against Persons/UserAccounts/UserCredentials and constrain the
        // resulting occupancy to the exact building/apartment selected by the resident.
        var building = await _buildings.GetByIdAsync(vm.BuildingId);
        var floor = building?.Floors.FirstOrDefault(item => item.Id == vm.FloorId);
        var apartment = building?.Apartments.FirstOrDefault(item =>
            item.Id == vm.AptId && item.FloorId == vm.FloorId);
        var res = building == null || floor == null || apartment == null
            ? AuthResult.Fail("wrong-credentials")
            : await _identityAuth.AuthenticateResidentAsync(
                vm.Phone, vm.Pin, vm.BuildingId, vm.AptId);

        if (res == null || !res.Success ||
            !string.Equals(res.Role, "resident", StringComparison.Ordinal) ||
            !res.BuildingIds.Contains(vm.BuildingId, StringComparer.Ordinal) ||
            !string.Equals(res.ApartmentId, vm.AptId, StringComparison.Ordinal))
        {
            _throttle.RegisterFailure(throttleKey, ClientIp);
            ViewBag.Error = Loc.T("Invalid_Login_Details");
            ViewBag.Buildings = await _buildings.GetSummariesAsync();
            return View(vm);
        }

        _throttle.Reset(throttleKey);
        if (res.MustChangePin)
            return StartPinChangeChallenge(res, vm.Phone, vm.BuildingId, vm.AptId);
        await SignInCookieAsync(res);
        return RedirectToAction("Index", "ResidentHome");
    }

    // ═══════════════════════════════════════════════════════════
    // 2. Admin Login
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Signs in the admin.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> LoginAdmin()
    {
        ViewBag.Buildings = await _buildings.GetSummariesAsync();
        return View(new AdminLoginVm());
    }

    /// <summary>
    /// Signs in the admin.
    /// </summary>
    /// <remarks>
    /// HTTP POST. Rate limited.
    /// </remarks>
    [HttpPost]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginAdmin(AdminLoginVm vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Buildings = await _buildings.GetSummariesAsync();
            return View(vm);
        }

        var throttleKey = AcctKey(vm.Phone);
        if (_throttle.IsLocked(throttleKey, ClientIp))
        {
            ViewBag.Error = LockedMsg;
            ViewBag.Buildings = await _buildings.GetSummariesAsync();
            return View(vm);
        }

        var res = await _identityAuth.AuthenticateAdminAsync(vm.Phone, vm.Pin, vm.BuildingId)
            ?? AuthResult.Fail("wrong-credentials");

        if (!res.Success)
        {
            _throttle.RegisterFailure(throttleKey, ClientIp);
            ViewBag.Error = MapError(res.Error, res.Reason);
            ViewBag.Buildings = await _buildings.GetSummariesAsync();
            return View(vm);
        }

        if (res.UsesTargetIdentity && res.MustChangePin)
            return StartPinChangeChallenge(res, vm.Phone, vm.BuildingId);

        return await CompleteSignInAsync(res, throttleKey);
    }

    // ═══════════════════════════════════════════════════════════
    // 3. SuperAdmin Login
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Signs in the super admin.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public IActionResult LoginSuperAdmin() => View(new SuperAdminLoginVm());

    /// <summary>
    /// Signs in the super admin.
    /// </summary>
    /// <remarks>
    /// HTTP POST. Rate limited.
    /// </remarks>
    [HttpPost]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginSuperAdmin(SuperAdminLoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var throttleKey = AcctKey(vm.Email);
        if (_throttle.IsLocked(throttleKey, ClientIp))
        {
            ViewBag.Error = LockedMsg;
            return View(vm);
        }

        var res = await _identityAuth.AuthenticateSuperAdminAsync(vm.Email, vm.Password);

        if (!res.Success)
        {
            _throttle.RegisterFailure(throttleKey, ClientIp);
            ViewBag.Error = MapError(res.Error, res.Reason);
            return View(vm);
        }

        return await CompleteSignInAsync(res, throttleKey);
    }

    // ═══════════════════════════════════════════════════════════
    // 4. Unified Login
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Shows the unified login page: one form for an e-mail or phone number plus a password or PIN.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public IActionResult UnifiedLogin(string? returnUrl = null)
    {
        var pending = GetPendingTwoFactorSignIn();
        if (pending?.RequiresEnrollment == true)
            return RedirectToAction(nameof(TwoFactorSetup));

        ViewBag.ReturnUrl = returnUrl;
        ViewBag.Error = TempData["Error"];
        ViewBag.TwoFactorStep = pending != null;
        return View(new UnifiedLoginVm());
    }

    /// <summary>
    /// Authenticates the submitted identifier and credential. Signs the user in directly, or redirects to account selection when several accounts match.
    /// </summary>
    /// <remarks>
    /// HTTP POST. Rate limited.
    /// </remarks>
    [HttpPost]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnifiedLogin(UnifiedLoginVm vm)
    {
        // Once primary credentials have been accepted, every POST in this session must
        // complete the pending second factor; it cannot fall back to primary login.
        var pendingSignIn = GetPendingTwoFactorSignIn();
        if (pendingSignIn != null)
            return pendingSignIn.RequiresEnrollment
                ? RedirectToAction(nameof(TwoFactorSetup))
                : await CompleteTwoFactorVerificationAsync(vm.TwoFactorCode);

        if (!ModelState.IsValid) return View(vm);

        var identifier = vm.Identifier.Trim();
        // Credentials are opaque secrets: never trim or normalize passwords/PINs.
        var credential = vm.Credential;

        var throttleKey = AcctKey(identifier);
        if (_throttle.IsLocked(throttleKey, ClientIp))
        {
            ViewBag.Error = LockedMsg;
            return View(vm);
        }

        // Allow-listed SuperAdmin accounts authenticate exclusively against the target identity schema.
        if (SuperAdminIdentityPolicy.IsEmailAllowed(identifier))
        {
            var superAdminResult = await _identityAuth.AuthenticateSuperAdminAsync(identifier, credential);
            if (!superAdminResult.Success)
            {
                _throttle.RegisterFailure(throttleKey, ClientIp);
                ViewBag.Error = Loc.T("Invalid_Login_Details");
                return View(vm);
            }

            return await CompleteSignInAsync(superAdminResult, throttleKey);
        }

        var authenticatedContexts = await AuthenticatePhonePinContextsAsync(identifier, credential);
        if (authenticatedContexts.Count == 0)
        {
            _throttle.RegisterFailure(throttleKey, ClientIp);
            ViewBag.Error = Loc.T("Invalid_Login_Details");
            return View(vm);
        }

        _throttle.Reset(throttleKey);
        if (authenticatedContexts.Any(result => result.MustChangePin))
        {
            var pinChangeResult = authenticatedContexts.FirstOrDefault(result => result.Role == "resident")
                ?? authenticatedContexts[0];
            return StartPinChangeChallenge(
                pinChangeResult, identifier,
                pinChangeRole: authenticatedContexts.Count > 1 ? "multi" : pinChangeResult.Role);
        }

        if (authenticatedContexts.Count > 1)
            return await StartRoleContextSelectionAsync(authenticatedContexts, throttleKey);

        return await CompleteSignInAsync(authenticatedContexts[0], throttleKey);
    }

    // ═══════════════════════════════════════════════════════════
    // 5. ChooseContext
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Shows the account selection page for the accounts found by the unified login.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public IActionResult ChooseContext(string token)
    {
        var (contexts, fullName, _) = GetContextsFromSession(token);

        if (contexts == null)
        {
            TempData["Error"] = Loc.T("The_Session_Has_Expired_Please_Try");
            return RedirectToAction("UnifiedLogin");
        }

        var vm = new ChooseContextVm
        {
            FullName = fullName ?? Loc.T("User"),
            Contexts = contexts,
            SessionToken = token
        };

        return View("LoginChoice", vm);
    }

    /// <summary>
    /// Signs in with the account the user selected on the account selection page.
    /// </summary>
    /// <remarks>
    /// HTTP POST. Rate limited.
    /// </remarks>
    [HttpPost]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChooseContext(string sessionToken, string contextId)
    {
        var (contexts, _, throttleKey) = GetContextsFromSession(sessionToken);

        if (contexts == null)
        {
            TempData["Error"] = Loc.T("The_Session_Has_Expired");
            return RedirectToAction("UnifiedLogin");
        }

        var selected = contexts.FirstOrDefault(c => c.Id == contextId);
        if (selected == null)
        {
            TempData["Error"] = Loc.T("The_Selected_Context_Does_Not_Exist");
            return RedirectToAction("ChooseContext", new { token = sessionToken });
        }

        // Legacy account-context tokens are never used to issue a target-identity session.
        HttpContext.Session.Remove("ctx_" + sessionToken);
        TempData["Error"] = Loc.T("The_Session_Has_Expired_Please_Try");
        return RedirectToAction(nameof(UnifiedLogin));
    }

    /// <summary>Displays the short-lived, session-bound role and scope choices after successful phone/PIN authentication.</summary>
    [HttpGet]
    public async Task<IActionResult> SelectRoleContext()
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        var token = HttpContext.Session.GetString("role_ctx_token") ?? "";
        var pending = GetPendingRoleContexts(token);
        if (pending == null)
        {
            TempData["Error"] = Loc.T("The_Session_Has_Expired_Please_Try");
            return RedirectToAction(nameof(UnifiedLogin));
        }

        var vm = new RoleContextSelectionVm
        {
            FullName = pending.Contexts.FirstOrDefault()?.Name ?? Loc.T("User"),
            SessionToken = token,
            Contexts = await BuildRoleContextOptionsAsync(pending.Contexts)
        };
        return View(vm);
    }

    /// <summary>Completes sign-in for the selected role and scope after rechecking its current authorization.</summary>
    [HttpPost]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectRoleContext(string sessionToken, string contextId)
    {
        var pending = GetPendingRoleContexts(sessionToken);
        if (pending == null)
        {
            TempData["Error"] = Loc.T("The_Session_Has_Expired_Please_Try");
            return RedirectToAction(nameof(UnifiedLogin));
        }

        var selected = pending.Contexts.FirstOrDefault(result =>
            string.Equals(GetRoleContextId(result), contextId, StringComparison.Ordinal));
        if (selected == null)
        {
            TempData["Error"] = Loc.T("The_Selected_Context_Does_Not_Exist");
            return RedirectToAction(nameof(SelectRoleContext));
        }

        if (!await _identityAuth.IsRoleContextStillAuthorizedAsync(selected, HttpContext.RequestAborted))
        {
            HttpContext.Session.Remove("role_ctx_" + sessionToken);
            TempData["Error"] = Loc.T("Invalid_Login_Details");
            return RedirectToAction(nameof(UnifiedLogin));
        }

        HttpContext.Session.Remove("role_ctx_" + sessionToken);
        HttpContext.Session.Remove("role_ctx_token");
        return await CompleteSignInAsync(selected, pending.ThrottleKey);
    }

    private sealed class PendingRoleContexts
    {
        public List<AuthResult> Contexts { get; set; } = new();
        public string? ThrottleKey { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }

    private async Task<List<AuthResult>> AuthenticatePhonePinContextsAsync(string identifier, string credential)
    {
        var resident = await _identityAuth.AuthenticateResidentAsync(
            identifier, credential, cancellationToken: HttpContext.RequestAborted);
        var admin = await _identityAuth.AuthenticateAdminAsync(
            identifier, credential, cancellationToken: HttpContext.RequestAborted);

        var results = new List<AuthResult>();
        if (resident?.Success == true)
        {
            var scopes = resident.ResidentScopes.Count > 0
                ? resident.ResidentScopes
                : new List<ResidentLoginScope>
                {
                    new() { BuildingId = resident.BuildingIds.FirstOrDefault() ?? "", ApartmentId = resident.ApartmentId ?? "" }
                };
            foreach (var scope in scopes.Where(scope =>
                !string.IsNullOrWhiteSpace(scope.BuildingId) && !string.IsNullOrWhiteSpace(scope.ApartmentId)))
            {
                results.Add(CloneAuthResult(resident, "resident",
                    new List<string> { scope.BuildingId }, scope.ApartmentId));
            }
        }

        if (admin?.Success == true && admin.BuildingIds.Count > 0)
        {
            foreach (var buildingId in admin.BuildingIds.Distinct(StringComparer.Ordinal))
            {
                // Re-authenticate in the individual building scope so permission overrides
                // are evaluated for this building rather than intersected across buildings.
                var scopedAdmin = await _identityAuth.AuthenticateAdminAsync(
                    identifier, credential, buildingId, HttpContext.RequestAborted);
                if (scopedAdmin?.Success == true)
                    results.Add(scopedAdmin);
            }
        }

        return results
            .GroupBy(GetRoleContextId, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    private static AuthResult CloneAuthResult(
        AuthResult source, string role, List<string> buildingIds, string? apartmentId) => new()
    {
        Success = source.Success,
        Uid = source.Uid,
        Role = role,
        Email = source.Email,
        Name = source.Name,
        Phone = source.Phone,
        UsesTargetIdentity = source.UsesTargetIdentity,
        MustChangePin = source.MustChangePin,
        SecurityStamp = source.SecurityStamp,
        BuildingIds = buildingIds,
        Permissions = source.Permissions.ToList(),
        ApartmentId = apartmentId,
        ApartmentNumber = source.ApartmentNumber
    };

    private async Task<IActionResult> StartRoleContextSelectionAsync(
        List<AuthResult> contexts, string throttleKey)
    {
        var token = Guid.NewGuid().ToString("N");
        var pending = new PendingRoleContexts
        {
            Contexts = contexts,
            ThrottleKey = throttleKey,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
        };
        HttpContext.Session.Clear();
        HttpContext.Session.SetString("role_ctx_token", token);
        HttpContext.Session.SetString(
            "role_ctx_" + token, System.Text.Json.JsonSerializer.Serialize(pending));
        return RedirectToAction(nameof(SelectRoleContext));
    }

    private PendingRoleContexts? GetPendingRoleContexts(string token)
    {
        if (!Guid.TryParseExact(token, "N", out _) ||
            !string.Equals(HttpContext.Session.GetString("role_ctx_token"), token, StringComparison.Ordinal))
            return null;
        var json = HttpContext.Session.GetString("role_ctx_" + token);
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var pending = System.Text.Json.JsonSerializer.Deserialize<PendingRoleContexts>(json);
            if (pending == null || pending.ExpiresAt <= DateTimeOffset.UtcNow ||
                pending.Contexts.Count < 2 || pending.Contexts.Any(result =>
                    !result.Success || !result.UsesTargetIdentity || result.Uid == null))
            {
                HttpContext.Session.Remove("role_ctx_" + token);
                return null;
            }
            return pending;
        }
        catch (System.Text.Json.JsonException)
        {
            HttpContext.Session.Remove("role_ctx_" + token);
            return null;
        }
    }

    private static string GetRoleContextId(AuthResult result) =>
        result.Role == "resident"
            ? "resident:" + (result.BuildingIds.FirstOrDefault() ?? "") + ":" + (result.ApartmentId ?? "")
            : result.Role == "admin"
                ? "admin:" + (result.BuildingIds.FirstOrDefault() ?? "")
                : result.Role ?? "";

    private async Task<List<RoleContextOptionVm>> BuildRoleContextOptionsAsync(List<AuthResult> contexts)
    {
        var options = new List<RoleContextOptionVm>();
        foreach (var context in contexts)
        {
            if (context.Role == "resident")
            {
                var buildingId = context.BuildingIds.FirstOrDefault() ?? "";
                var building = await _buildings.GetByIdAsync(buildingId);
                var apartment = building?.Apartments.FirstOrDefault(item => item.Id == context.ApartmentId);
                var buildingLabel = building == null
                    ? Loc.T("Building")
                    : $"{building.Name} ({building.BuildingNumber})";
                var apartmentLabel = apartment == null
                    ? context.ApartmentId ?? ""
                    : Loc.T("Apartment_N", apartment.Number);
                options.Add(new RoleContextOptionVm
                {
                    Id = GetRoleContextId(context),
                    Role = "resident",
                    GroupTitle = buildingLabel,
                    Title = apartmentLabel,
                    Description = "دخول الساكن لهذه الشقة"
                });
            }
            else if (context.Role == "admin")
            {
                var buildingId = context.BuildingIds.SingleOrDefault();
                if (string.IsNullOrWhiteSpace(buildingId)) continue;
                var building = await _buildings.GetByIdAsync(buildingId);
                var buildingLabel = building == null
                    ? Loc.T("Building")
                    : $"{building.Name} ({building.BuildingNumber})";
                options.Add(new RoleContextOptionVm
                {
                    Id = GetRoleContextId(context),
                    Role = "admin",
                    GroupTitle = buildingLabel,
                    Title = "مدير العمارة",
                    Description = $"إدارة وصلاحيات عمارة {buildingLabel}"
                });
            }
        }
        return options;
    }

    private IActionResult? EnsureAdministratorTwoFactorAccess()
    {
        if (User.Identity?.IsAuthenticated != true) return Challenge();
        if (!User.IsInRole("admin") && !User.IsInRole("superadmin")) return Forbid();
        if (User.HasClaim("impersonated", "true")) return Forbid();
        return null;
    }

    private sealed class PendingTwoFactorSignIn
    {
        public AuthResult Result { get; set; } = new();
        public string? ThrottleKey { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public bool RequiresEnrollment { get; set; }
    }

    private PendingTwoFactorSignIn? GetPendingTwoFactorSignIn()
    {
        var json = HttpContext.Session.GetString("2fa_pending");
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var pending = System.Text.Json.JsonSerializer.Deserialize<PendingTwoFactorSignIn>(json);
            if (pending == null || pending.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                ClearPendingTwoFactorState();
                return null;
            }
            return pending;
        }
        catch (System.Text.Json.JsonException)
        {
            ClearPendingTwoFactorState();
            return null;
        }
    }

    private IActionResult StartPinChangeChallenge(
        AuthResult result, string identifier, string? buildingId = null,
        string? apartmentId = null, string? pinChangeRole = null)
    {
        HttpContext.Session.Clear();
        HttpContext.Session.SetString("resident_pin_change_account", result.Uid!);
        HttpContext.Session.SetString("resident_pin_change_stamp", result.SecurityStamp ?? "");
        HttpContext.Session.SetString("resident_pin_change_identifier", identifier);
        HttpContext.Session.SetString("resident_pin_change_role", pinChangeRole ?? result.Role ?? "resident");
        if (!string.IsNullOrWhiteSpace(buildingId))
            HttpContext.Session.SetString("resident_pin_change_building", buildingId);
        if (!string.IsNullOrWhiteSpace(apartmentId))
            HttpContext.Session.SetString("resident_pin_change_apartment", apartmentId);
        HttpContext.Session.SetString("resident_pin_change_expires",
            DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        return RedirectToAction(nameof(ChangePin));
    }

    /// <summary>Changes the PIN for the authenticated account.</summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ChangePin()
    {
        if (!HasValidPinChangeChallenge())
            return RedirectToAction(nameof(UnifiedLogin));
        return View(new ResidentPinChangeVm());
    }

    /// <summary>Changes the PIN for the authenticated account.</summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePin(ResidentPinChangeVm vm)
    {
        if (!HasValidPinChangeChallenge())
            return RedirectToAction(nameof(UnifiedLogin));

        if (!ModelState.IsValid ||
            vm.NewPin.Length < 4 || vm.NewPin.Length > 12 ||
            vm.NewPin.Any(character => character < '0' || character > '9') ||
            vm.NewPin == "1234" ||
            !string.Equals(vm.NewPin, vm.ConfirmNewPin, StringComparison.Ordinal))
        {
            ViewBag.Error = Loc.T("Choose_New_PIN_And_Confirm");
            return View(vm);
        }

        var accountText = HttpContext.Session.GetString("resident_pin_change_account")!;
        var stamp = HttpContext.Session.GetString("resident_pin_change_stamp")!;
        var identifier = HttpContext.Session.GetString("resident_pin_change_identifier")!;
        var buildingId = HttpContext.Session.GetString("resident_pin_change_building");
        var apartmentId = HttpContext.Session.GetString("resident_pin_change_apartment");
        if (!Guid.TryParse(accountText, out var accountId) ||
            !await _identityAuth.ChangeResidentPinAsync(accountId, stamp, vm.NewPin))
        {
            HttpContext.Session.Clear();
            TempData["Error"] = Loc.T("The_Session_Has_Expired_Please_Try");
            return RedirectToAction(nameof(UnifiedLogin));
        }

        var pinChangeRole = HttpContext.Session.GetString("resident_pin_change_role") ?? "resident";
        if (pinChangeRole == "multi")
        {
            var contexts = await AuthenticatePhonePinContextsAsync(identifier, vm.NewPin);
            if (contexts.Count == 0 || contexts.Any(result => result.MustChangePin))
            {
                HttpContext.Session.Clear();
                TempData["Error"] = Loc.T("Invalid_Login_Details");
                return RedirectToAction(nameof(UnifiedLogin));
            }
            if (contexts.Count > 1)
                return await StartRoleContextSelectionAsync(contexts, AcctKey(identifier));
            if (contexts.Count == 1)
                return await CompleteSignInAsync(contexts[0], AcctKey(identifier));
        }

        var result = pinChangeRole == "admin"
            ? await _identityAuth.AuthenticateAdminAsync(identifier, vm.NewPin, buildingId, HttpContext.RequestAborted)
            : await _identityAuth.AuthenticateResidentAsync(identifier, vm.NewPin, buildingId, apartmentId);
        if (result == null || !result.Success || result.MustChangePin)
        {
            HttpContext.Session.Clear();
            TempData["Error"] = Loc.T("Invalid_Login_Details");
            return RedirectToAction(nameof(UnifiedLogin));
        }

        return await CompleteSignInAsync(result, AcctKey(identifier));
    }

    private bool HasValidPinChangeChallenge()
    {
        var account = HttpContext.Session.GetString("resident_pin_change_account");
        var stamp = HttpContext.Session.GetString("resident_pin_change_stamp");
        var expiresText = HttpContext.Session.GetString("resident_pin_change_expires");
        return !string.IsNullOrWhiteSpace(account) &&
            !string.IsNullOrWhiteSpace(stamp) &&
            long.TryParse(expiresText, out var expires) &&
            expires > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private async Task<IActionResult> CompleteSignInAsync(AuthResult result, string? throttleKey)
    {
        if (AdministratorMfaPolicy.RequiresMfa(result.Role))
        {
            if (!result.UsesTargetIdentity || !Guid.TryParse(result.Uid, out _))
            {
                _log.LogError("Refusing privileged sign-in without a canonical target identity for role {Role}", result.Role);
                TempData["Error"] = Loc.T("Invalid_Login_Details");
                return RedirectToAction(nameof(UnifiedLogin));
            }

            var mfaEnabled = await _twoFactor.IsEnabledAsync(result.Uid!);
            HttpContext.Session.Clear();
            var pending = new PendingTwoFactorSignIn
            {
                Result = result,
                ThrottleKey = throttleKey ?? AcctKey(result.Email ?? result.Uid),
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(mfaEnabled ? 5 : 10),
                RequiresEnrollment = !mfaEnabled
            };
            HttpContext.Session.SetString("2fa_pending", System.Text.Json.JsonSerializer.Serialize(pending));

            if (!mfaEnabled)
            {
                HttpContext.Session.SetString("2fa_setup_secret", TwoFactorService.CreateSecret());
                HttpContext.Session.SetString(
                    "2fa_setup_expires",
                    pending.ExpiresAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture));
                return RedirectToAction(nameof(TwoFactorSetup));
            }

            return RedirectToAction(nameof(UnifiedLogin));
        }

        if (!string.IsNullOrWhiteSpace(throttleKey)) _throttle.Reset(throttleKey);
        await SignInCookieAsync(result);
        return RedirectBasedOnRole(result.Role!);
    }

    /// <summary>Legacy route retained for compatibility; the challenge is rendered by UnifiedLogin.</summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult VerifyTwoFactor() => RedirectToAction(nameof(UnifiedLogin));

    /// <summary>Legacy POST route retained for compatibility with the unified login challenge.</summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> VerifyTwoFactor(TwoFactorVerifyVm vm)
        => CompleteTwoFactorVerificationAsync(vm.Code);

    /// <summary>Clears an unfinished second-factor challenge and returns to the normal login form.</summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult CancelTwoFactor()
    {
        HttpContext.Session.Remove("2fa_pending");
        HttpContext.Session.Remove("2fa_setup_secret");
        HttpContext.Session.Remove("2fa_setup_expires");
        return RedirectToAction(nameof(UnifiedLogin));
    }

    private async Task<IActionResult> CompleteTwoFactorVerificationAsync(string? code)
    {
        var pending = GetPendingTwoFactorSignIn();
        if (pending == null) return RedirectToAction(nameof(UnifiedLogin));
        if (pending.RequiresEnrollment) return RedirectToAction(nameof(TwoFactorSetup));

        var throttleKey = pending.ThrottleKey ?? AcctKey(pending.Result.Email ?? pending.Result.Uid);
        ViewBag.TwoFactorStep = true;

        if (_throttle.IsLocked(throttleKey, ClientIp))
        {
            ViewBag.Error = LockedMsg;
            return View("UnifiedLogin", new UnifiedLoginVm());
        }

        if (string.IsNullOrWhiteSpace(code)
            || !await _twoFactor.VerifyAndConsumeAsync(pending.Result.Uid!, code.Trim()))
        {
            _log.LogWarning("Two-factor verification failed for account {Uid}", pending.Result.Uid);
            _throttle.RegisterFailure(throttleKey, ClientIp);
            ViewBag.Error = Loc.T("Mfa_Invalid_Code_Try_Again");
            return View("UnifiedLogin", new UnifiedLoginVm());
        }

        if (pending.Result.UsesTargetIdentity &&
            (pending.Result.Role is "admin" or "resident" or "superadmin") &&
            !await _identityAuth.IsRoleContextStillAuthorizedAsync(pending.Result, HttpContext.RequestAborted))
        {
            HttpContext.Session.Remove("2fa_pending");
            TempData["Error"] = Loc.T("Invalid_Login_Details");
            return RedirectToAction(nameof(UnifiedLogin));
        }

        _log.LogInformation("Two-factor verification succeeded for account {Uid}", pending.Result.Uid);
        HttpContext.Session.Remove("2fa_pending");
        _throttle.Reset(throttleKey);
        await SignInCookieAsync(pending.Result, twoFactorVerified: true);
        return RedirectBasedOnRole(pending.Result.Role!);
    }

    /// <summary>Displays two-factor authentication settings for the signed-in administrator.</summary>
    [HttpGet]
    public IActionResult TwoFactorSettings()
    {
        var access = EnsureAdministratorTwoFactorAccess();
        if (access != null) return access;
        return RedirectToAction("Own", "Settings");
    }

    /// <summary>Starts a time-limited two-factor setup session and generates a new authenticator secret.</summary>
    [HttpPost]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BeginTwoFactorSetup()
    {
        var access = EnsureAdministratorTwoFactorAccess();
        if (access != null) return access;
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (await _twoFactor.IsEnabledAsync(uid))
        {
            TempData["Error"] = "Two-factor authentication is already enabled.";
            return RedirectToAction("Own", "Settings");
        }
        HttpContext.Session.SetString("2fa_setup_secret", TwoFactorService.CreateSecret());
        HttpContext.Session.SetString("2fa_setup_expires", DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString());
        return RedirectToAction(nameof(TwoFactorSetup));
    }

    /// <summary>Displays the authenticator provisioning details for the current setup session.</summary>
    [HttpGet]
    public async Task<IActionResult> TwoFactorSetup()
    {
        var pending = GetPendingTwoFactorSignIn();
        if (pending?.RequiresEnrollment == true)
        {
            var secret = HttpContext.Session.GetString("2fa_setup_secret");
            var expiresText = HttpContext.Session.GetString("2fa_setup_expires");
            if (!AdministratorMfaPolicy.RequiresMfa(pending.Result.Role) ||
                !pending.Result.UsesTargetIdentity ||
                string.IsNullOrWhiteSpace(secret) ||
                !long.TryParse(expiresText, out var expires) ||
                expires <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                ClearPendingTwoFactorState();
                TempData["Error"] = Loc.T("Mfa_Enrollment_Expired_Sign_In_Again");
                return RedirectToAction(nameof(UnifiedLogin));
            }

            if (!await _identityAuth.IsRoleContextStillAuthorizedAsync(pending.Result, HttpContext.RequestAborted))
            {
                ClearPendingTwoFactorState();
                TempData["Error"] = Loc.T("Invalid_Login_Details");
                return RedirectToAction(nameof(UnifiedLogin));
            }

            return RenderTwoFactorSetup(secret, pending.Result.Email ?? pending.Result.Name ?? "administrator", mandatory: true);
        }

        var access = EnsureAdministratorTwoFactorAccess();
        if (access != null) return access;
        var setupSecret = HttpContext.Session.GetString("2fa_setup_secret");
        var setupExpiresText = HttpContext.Session.GetString("2fa_setup_expires");
        if (string.IsNullOrWhiteSpace(setupSecret)
            || !long.TryParse(setupExpiresText, out var setupExpires)
            || setupExpires <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            HttpContext.Session.Remove("2fa_setup_secret");
            HttpContext.Session.Remove("2fa_setup_expires");
            return RedirectToAction("Own", "Settings");
        }

        var account = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(account)) account = User.FindFirstValue(ClaimTypes.Name) ?? "administrator";
        return RenderTwoFactorSetup(setupSecret, account, mandatory: false);
    }

    private IActionResult RenderTwoFactorSetup(string secret, string account, bool mandatory, TwoFactorVerifyVm? model = null)
    {
        var uri = TwoFactorService.CreateProvisioningUri(secret, account);
        ViewBag.Secret = secret;
        ViewBag.QrDataUri = "data:image/png;base64," + Convert.ToBase64String(_qrGenerator.GeneratePng(uri));
        ViewBag.MandatoryEnrollment = mandatory;
        return View("TwoFactorSetup", model ?? new TwoFactorVerifyVm());
    }

    private void ClearPendingTwoFactorState()
    {
        HttpContext.Session.Remove("2fa_pending");
        HttpContext.Session.Remove("2fa_setup_secret");
        HttpContext.Session.Remove("2fa_setup_expires");
    }

    private async Task<IActionResult> CompleteMandatoryTwoFactorEnrollmentAsync(TwoFactorVerifyVm vm)
    {
        var pending = GetPendingTwoFactorSignIn();
        var secret = HttpContext.Session.GetString("2fa_setup_secret");
        var expiresText = HttpContext.Session.GetString("2fa_setup_expires");
        if (pending?.RequiresEnrollment != true ||
            string.IsNullOrWhiteSpace(secret) ||
            !long.TryParse(expiresText, out var expires) ||
            expires <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            ClearPendingTwoFactorState();
            TempData["Error"] = Loc.T("Mfa_Enrollment_Expired_Sign_In_Again");
            return RedirectToAction(nameof(UnifiedLogin));
        }

        if (!AdministratorMfaPolicy.RequiresMfa(pending.Result.Role) ||
            !pending.Result.UsesTargetIdentity ||
            !await _identityAuth.IsRoleContextStillAuthorizedAsync(pending.Result, HttpContext.RequestAborted))
        {
            ClearPendingTwoFactorState();
            TempData["Error"] = Loc.T("Invalid_Login_Details");
            return RedirectToAction(nameof(UnifiedLogin));
        }

        var throttleKey = pending.ThrottleKey ?? AcctKey(pending.Result.Email ?? pending.Result.Uid);
        if (_throttle.IsLocked(throttleKey, ClientIp))
        {
            ViewBag.Error = LockedMsg;
            return RenderTwoFactorSetup(secret, pending.Result.Email ?? pending.Result.Name ?? "administrator", mandatory: true, vm);
        }

        var codes = await _twoFactor.EnableAsync(pending.Result.Uid!, secret, vm.Code, HttpContext.RequestAborted);
        if (codes == null)
        {
            _throttle.RegisterFailure(throttleKey, ClientIp);
            ViewBag.Error = Loc.T("Mfa_Invalid_Code_Try_Again");
            return RenderTwoFactorSetup(secret, pending.Result.Email ?? pending.Result.Name ?? "administrator", mandatory: true, vm);
        }

        _throttle.Reset(throttleKey);
        TempData["TwoFactorRecoveryCodes"] = System.Text.Json.JsonSerializer.Serialize(codes);
        await SignInCookieAsync(pending.Result, twoFactorVerified: true);
        return RedirectToAction(nameof(TwoFactorRecoveryCodes));
    }

    /// <summary>Validates the first authenticator code and enables two-factor authentication.</summary>
    /// <param name="vm">The verification code submitted by the administrator.</param>
    [HttpPost]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableTwoFactor(TwoFactorVerifyVm vm)
    {
        if (GetPendingTwoFactorSignIn()?.RequiresEnrollment == true)
            return await CompleteMandatoryTwoFactorEnrollmentAsync(vm);

        var access = EnsureAdministratorTwoFactorAccess();
        if (access != null) return access;
        var secret = HttpContext.Session.GetString("2fa_setup_secret");
        var expiresText = HttpContext.Session.GetString("2fa_setup_expires");
        if (string.IsNullOrWhiteSpace(secret)
            || !long.TryParse(expiresText, out var expires)
            || expires <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            TempData["Error"] = "The setup session has expired. Start again.";
            return RedirectToAction("Own", "Settings");
        }

        var codes = await _twoFactor.EnableAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, secret, vm.Code);
        if (codes == null)
        {
            ViewBag.Error = Loc.T("Mfa_Invalid_Code_Try_Again");
            var uri = TwoFactorService.CreateProvisioningUri(secret, User.FindFirstValue(ClaimTypes.Email) ?? "administrator");
            ViewBag.Secret = secret;
            ViewBag.QrDataUri = "data:image/png;base64," + Convert.ToBase64String(_qrGenerator.GeneratePng(uri));
            return View("TwoFactorSetup", vm);
        }

        HttpContext.Session.Remove("2fa_setup_secret");
        HttpContext.Session.Remove("2fa_setup_expires");
        await RefreshTwoFactorVerifiedCookieAsync();
        TempData["TwoFactorRecoveryCodes"] = System.Text.Json.JsonSerializer.Serialize(codes);
        return RedirectToAction(nameof(TwoFactorRecoveryCodes));
    }

    /// <summary>Displays the one-time recovery codes generated when two-factor authentication was enabled.</summary>
    [HttpGet]
    public IActionResult TwoFactorRecoveryCodes()
    {
        var access = EnsureAdministratorTwoFactorAccess();
        if (access != null) return access;
        var json = TempData["TwoFactorRecoveryCodes"] as string;
        if (string.IsNullOrWhiteSpace(json)) return RedirectToAction("Own", "Settings");
        try
        {
            ViewBag.Codes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            return View();
        }
        catch (System.Text.Json.JsonException)
        {
            return RedirectToAction("Own", "Settings");
        }
    }

    /// <summary>Disables two-factor authentication after validating a current authenticator or recovery code.</summary>
    /// <param name="vm">The verification code submitted by the administrator.</param>
    [HttpPost]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableTwoFactor(TwoFactorVerifyVm vm)
    {
        var access = EnsureAdministratorTwoFactorAccess();
        if (access != null) return access;
        TempData["Error"] = Loc.T("Mfa_Disable_Not_Allowed");
        return RedirectToAction("Own", "Settings");
    }

    // ═══════════════════════════════════════════════════════════
    // 6. Logout
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Handles the logout form submission of the account screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST. Rate limited.
    /// </remarks>
    [HttpPost]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("LoginChoice");
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════
    private string StoreContextsInSession(List<UserContext> contexts, string fullName, string throttleKey)
    {
        var token = Guid.NewGuid().ToString("N");
        var data = System.Text.Json.JsonSerializer.Serialize(new
        {
            Contexts = contexts,
            FullName = fullName,
            ThrottleKey = throttleKey
        });
        HttpContext.Session.SetString("ctx_" + token, data);
        return token;
    }

    private (List<UserContext>?, string?, string?) GetContextsFromSession(string token)
    {
        var json = HttpContext.Session.GetString("ctx_" + token);
        if (string.IsNullOrEmpty(json)) return (null, null, null);

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var contextsJson = root.GetProperty("Contexts").GetRawText();
            var contexts = System.Text.Json.JsonSerializer.Deserialize<List<UserContext>>(contextsJson);

            var fullName = root.TryGetProperty("FullName", out var fn) ? fn.GetString() : null;
            var throttleKey = root.TryGetProperty("ThrottleKey", out var tk) ? tk.GetString() : null;

            return (contexts, fullName, throttleKey);
        }
        catch
        {
            return (null, null, null);
        }
    }

    private IActionResult RedirectBasedOnRole(string role) => role switch
    {
        "superadmin" => RedirectToAction("Index", "SuperAdminHome"),
        "admin" => RedirectToAction("Index", "AdminHome"),
        "resident" => RedirectToAction("Index", "ResidentHome"),
        _ => RedirectToAction("UnifiedLogin")
    };

    private async Task RefreshTwoFactorVerifiedCookieAsync()
    {
        var claims = User.Claims
            .Where(c => c.Type is not ("twoFactorVerified" or "mfaVerifiedAtUtc"))
            .ToList();
        claims.Add(new Claim("twoFactorVerified", "true"));
        claims.Add(new Claim(
            "mfaVerifiedAtUtc",
            DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12),
            AllowRefresh = false
        };
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);
    }

    private async Task SignInCookieAsync(AuthResult res, bool twoFactorVerified = false)
    {

        HttpContext.Session.Clear();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, res.Uid!),
            new(ClaimTypes.Name, res.Name ?? ""),
            new(ClaimTypes.Email, res.Email ?? ""),
            new(ClaimTypes.Role, res.Role!),
        };

        if (res.UsesTargetIdentity)
        {
            claims.Add(new Claim("targetIdentityAccount", "true"));
            claims.Add(new Claim("securityStamp", res.SecurityStamp ?? ""));
        }
        else if (res.Role == "superadmin")
        {
            claims.Add(new Claim("legacySuperAdminSecurityStamp", res.SecurityStamp ?? ""));
        }

        foreach (var bId in res.BuildingIds)
            claims.Add(new Claim("buildingId", bId));

        if (res.ApartmentId != null) claims.Add(new Claim("apartmentId", res.ApartmentId));
        if (res.ApartmentNumber != null) claims.Add(new Claim("apartmentNumber", res.ApartmentNumber.Value.ToString()));

        if (res.Role is "admin" or "superadmin")
        {
            claims.Add(new Claim("twoFactorVerified", twoFactorVerified ? "true" : "false"));
            if (twoFactorVerified)
                claims.Add(new Claim("mfaVerifiedAtUtc", DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture)));
        }

        if (res.Role is "admin" or "superadmin")
        {
            // Permission claims are sourced from persisted grants for both administrator roles.
            // A role name or configured SuperAdmin allow-list never manufactures permissions.
            foreach (var permission in res.Permissions.Distinct(StringComparer.Ordinal))
                claims.Add(new Claim("perm", permission));
        }
        else if (res.Role == "resident")
        {
            foreach (var p in BuildingManagementMvc.Models.AdminPermissions.ResidentBasic)
                claims.Add(new Claim("perm", p));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // الأدمن والسوبر أدمن: جلسة 12 ساعة ومش بتتمدد (الساكن زي ما هو: 7 أيام متجددة)
        var props = new AuthenticationProperties();
        if (res.Role == "admin" || res.Role == "superadmin")
        {
            props.ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12);
            props.AllowRefresh = false;
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            props);
    }

    private static string MapError(string? error, string? reason = null) => error switch
    {
        "building-not-found" => Loc.T("The_Building_Does_Not_Exist"),
        "building-required" => Loc.T("Building_Required"),
        "floor-required" => Loc.T("Floor_Required"),
        "floor-not-found" => Loc.T("Floor_Not_Found"),
        "apartment-required" => Loc.T("Apartment_Required"),
        "phone-required" => Loc.T("Phone_Required"),
        "pin-required" => Loc.T("PIN_Required"),
        "apartment-not-found" => Loc.T("The_Apartment_Does_Not_Exist"),
        "no-admin" => Loc.T("No_Admin_Has_Been_Registered_Yet"),
        // رسالة واحدة لكل أخطاء بيانات الدخول (منع كشف الحسابات)
        "wrong-credentials" or "wrong-wa" or "wrong-pin" or "not-in-building"
            => Loc.T("Incorrect_Phone_Number_Or_PIN"),
        "not-superadmin" => Loc.T("This_Email_Is_Not_Authorized_For_2"),
        "pin-not-set" => Loc.T("PIN_Not_Set_Contact_Admin"),
        "account-disabled" => Loc.T("This_Account_Is_Disabled") + (string.IsNullOrWhiteSpace(reason) ? "" : Loc.T("Reason_N", reason)),
        "account-inactive" => Loc.T("This_Account_Is_Inactive"),
        null => Loc.T("An_Unexpected_Error_Occurred"),
        _ => Loc.T("Could_Not_Log_In_2") + error
    };

    // ═══════════════════════════════════════════════════════════
    // AJAX: Get Floors / Apartments
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Gets the floors.
    /// </summary>
    /// <remarks>
    /// HTTP GET. Anonymous access. Rate limited.
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting("lookup")]
    public async Task<IActionResult> GetFloors(string buildingId)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            return Json(new List<object>());

        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null)
            return Json(new List<object>());

        var floors = building.Floors
            .OrderBy(f => f.Order)
            .Select(f => new
            {
                id = f.Id,
                order = f.Order,
                label = Loc.T("Floor_Label_With_Order", f.Label, f.Order)
            })
            .ToList();

        return Json(floors);
    }

    /// <summary>
    /// Gets the apartments.
    /// </summary>
    /// <remarks>
    /// HTTP GET. Anonymous access. Rate limited.
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting("lookup")]
    public async Task<IActionResult> GetApartments(string buildingId, string floorId)
    {
        if (string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(floorId))
            return Json(new List<object>());

        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null)
            return Json(new List<object>());

        var apts = building.Apartments
            .Where(a => a.FloorId == floorId && !a.Closed)
            .OrderBy(a => a.Number)
            .Select(a => new
            {
                id = a.Id,
                number = a.Number,
                label = Loc.T("Apartment_N", a.Number) + (string.IsNullOrWhiteSpace(a.Label) ? "" : $" â€” {a.Label}")
            })
            .ToList();

        return Json(apts);
    }

    // ═══════════════════════════════════════════════════════════
    // Quick Login (QR السريع)
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Shows the confirmation page of a quick-login link. It does not consume the token, so link previews cannot burn it.
    /// </summary>
    /// <remarks>
    /// HTTP GET. Anonymous access.
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> QuickLogin(
        string token,
        [FromServices] QrSecurityService qrSecurity)
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("LoginChoice");

        // âœ… GET مابيستهلكش التوكن â€” بيعرض صفحة تأكيد بس (معاينة الرابط في واتساب/الأنتي فايروس ماتحرقوش)
        var ctx = await ResolveQuickLoginAsync(token, qrSecurity);
        if (ctx.Error != null)
        {
            ViewBag.Error = ctx.Error;
            return View("QuickLoginError");
        }

        ViewBag.Token = token;
        ViewBag.AptNumber = ctx.Apartment!.ApartmentNumber;
        ViewBag.BuildingName = ctx.Apartment!.BuildingName;
        return View("QuickLoginConfirm");
    }

    /// <summary>
    /// Consumes the quick-login token (atomically) and signs the resident in.
    /// </summary>
    /// <remarks>
    /// HTTP POST. Anonymous access. Rate limited.
    /// </remarks>
    [HttpPost]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> QuickLoginConfirm(
        string token,
        [FromServices] QrSecurityService qrSecurity)
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("LoginChoice");

        var ctx = await ResolveQuickLoginAsync(token, qrSecurity);
        if (ctx.Error != null)
        {
            ViewBag.Error = ctx.Error;
            return View("QuickLoginError");
        }

        var payload = ctx.Payload!;
        var apartment = ctx.Apartment!;

        // Consume before resolving the account. A stale token cannot be revived if it
        // was revoked/reissued between the preview GET and this confirmation POST.
        var consumed = await qrSecurity.TryConsumeQuickTokenAsync(payload);
        if (!consumed)
        {
            ViewBag.Error = Loc.T("This_QR_Has_Already_Been_Used");
            return View("QuickLoginError");
        }

        // QR sign-in must use Persons/UserAccounts/ApartmentOccupancies and scoped
        // resident grants only. Never authenticate through the transitional Residents table.
        var isRecovery = string.Equals(payload.Purpose, "recovery", StringComparison.OrdinalIgnoreCase);
        var result = await _identityAuth.AuthenticateResidentWithQuickQrAsync(
            payload.BuildingId, payload.AptId, requirePinChange: isRecovery,
            cancellationToken: HttpContext.RequestAborted);
        if (!result.Success)
        {
            ViewBag.Error = Loc.T("Invalid_Token");
            return View("QuickLoginError");
        }

        result.ApartmentNumber = apartment.ApartmentNumber;
        if (result.MustChangePin)
        {
            if (string.IsNullOrWhiteSpace(result.Phone))
            {
                ViewBag.Error = "تعذر بدء تغيير الرقم السري؛ راجع رقم الهاتف الأساسي المسجل للحساب مع الإدارة.";
                return View("QuickLoginError");
            }
            return StartPinChangeChallenge(result, result.Phone, payload.BuildingId, payload.AptId);
        }

        await SignInCookieAsync(result);
        _log.LogInformation("[QuickLogin] Target resident account logged in for apartment {Apt}",
            apartment.ApartmentNumber);
        return RedirectToAction("Index", "ResidentHome");
    }

    // ═══════════════════════════════════════════════════════════
    // رابط تعيين الـ PIN (بيتبعت في رسالة الترحيب بدل الـ PIN نفسه)
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Sets the PIN.
    /// </summary>
    /// <remarks>
    /// HTTP GET. Anonymous access.
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> SetPin(string token, [FromServices] QrSecurityService qrSecurity)
    {
        var ctx = await ResolveSetPinAsync(token, qrSecurity);
        if (ctx.Error != null)
        {
            ViewBag.Error = ctx.Error;
            return View("QuickLoginError");
        }

        ViewBag.Token = token;
        ViewBag.AptNumber = ctx.Apartment!.ApartmentNumber;
        ViewBag.BuildingName = ctx.Apartment!.BuildingName;
        return View("SetPin");
    }

    /// <summary>
    /// Sets the PIN.
    /// </summary>
    /// <remarks>
    /// HTTP POST. Anonymous access. Rate limited.
    /// </remarks>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPin(
        string token, string newPin, string confirmPin,
        [FromServices] QrSecurityService qrSecurity)
    {
        var ctx = await ResolveSetPinAsync(token, qrSecurity);
        if (ctx.Error != null)
        {
            ViewBag.Error = ctx.Error;
            return View("QuickLoginError");
        }

        ViewBag.Token = token;
        ViewBag.AptNumber = ctx.Apartment!.ApartmentNumber;
        ViewBag.BuildingName = ctx.Apartment!.BuildingName;

        if (string.IsNullOrEmpty(newPin) || newPin.Length != 4 || !newPin.All(char.IsDigit))
        {
            ViewBag.FormError = Loc.T("PIN_Must_Be_Exactly_4_Digits");
            return View("SetPin");
        }

        if (newPin != confirmPin)
        {
            ViewBag.FormError = Loc.T("The_New_PIN_And_Its_Confirmation_2");
            return View("SetPin");
        }

        // استهلاك atomic: الرابط يتستخدم مرة واحدة بس
        if (!await qrSecurity.TryConsumeQuickTokenAsync(ctx.Payload!))
        {
            ViewBag.Error = Loc.T("This_QR_Has_Already_Been_Used");
            return View("QuickLoginError");
        }

        try
        {
            var affectedAccounts = await _identityAuth.ResetApartmentResidentPinsAsync(
                ctx.Payload!.BuildingId, ctx.Payload.AptId, newPin, "setpin-link", HttpContext.RequestAborted);
            if (affectedAccounts == 0)
            {
                ViewBag.Error = "رابط الشقة المشترك لا يغيّر PIN الحسابات الشخصية. اطلب من إدارة العمارة إصدار PIN مستقل من مركز إدارة الحسابات.";
                return View("QuickLoginError");
            }
            _log.LogInformation("[SetPin] PIN set via link for apartment {Apt} in building {B}",
                ctx.Apartment.ApartmentNumber, ctx.Payload.BuildingId);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[SetPin] failed to set PIN");
            ViewBag.Error = Loc.T("An_Unexpected_Error_Occurred");
            return View("QuickLoginError");
        }

        ViewBag.Done = true;
        return View("SetPin");
    }

    private async Task<QuickLoginContext> ResolveSetPinAsync(string token, QrSecurityService qrSecurity)
    {
        if (string.IsNullOrEmpty(token))
            return new QuickLoginContext { Error = Loc.T("Invalid_Token") };

        var validation = await qrSecurity.ValidateQuickToken(token);
        if (!validation.IsSuccess)
            return new QuickLoginContext { Error = validation.Error };

        var payload = validation.Data!;
        if (!string.Equals(payload.Purpose, "setpin", StringComparison.OrdinalIgnoreCase))
            return new QuickLoginContext { Error = Loc.T("Invalid_Token") };

        var apartment = await _targetQr.GetApartmentAsync(
            payload.BuildingId, payload.AptId, requireActive: true, HttpContext.RequestAborted);
        if (apartment is null)
            return new QuickLoginContext { Error = Loc.T("The_Apartment_Does_Not_Exist") };

        return new QuickLoginContext { Payload = payload, Apartment = apartment };
    }

    private sealed class QuickLoginContext
    {
        public string? Error { get; init; }
        public QuickTokenPayload? Payload { get; init; }
        public QrApartmentInfo? Apartment { get; init; }
        public TargetResidentAccount? Resident { get; init; }
    }

    private async Task<QuickLoginContext> ResolveQuickLoginAsync(string token, QrSecurityService qrSecurity)
    {
        var validation = await qrSecurity.ValidateQuickToken(token);
        if (!validation.IsSuccess)
            return new QuickLoginContext { Error = validation.Error };

        var payload = validation.Data!;

        // توكن تعيين الـ PIN مايعملش دخول
        if (!string.Equals(payload.Purpose, "login", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(payload.Purpose, "recovery", StringComparison.OrdinalIgnoreCase))
            return new QuickLoginContext { Error = Loc.T("Invalid_Token") };

        var apartment = await _targetQr.GetApartmentAsync(
            payload.BuildingId, payload.AptId, requireActive: true, HttpContext.RequestAborted);
        if (apartment is null)
            return new QuickLoginContext { Error = Loc.T("The_Apartment_Does_Not_Exist") };

        var residents = await _targetQr.GetResidentAccountsForApartmentAsync(
            payload.BuildingId, payload.AptId, HttpContext.RequestAborted);
        // Recovery must resolve to the explicitly designated primary target account;
        // it must never fall back to another occupant's identity.
        var resident = string.Equals(payload.Purpose, "recovery", StringComparison.OrdinalIgnoreCase)
            ? residents.FirstOrDefault(item => item.IsPrimary)
            : residents.FirstOrDefault(item => item.IsPrimary) ?? residents.FirstOrDefault();
        if (resident == null)
            return new QuickLoginContext { Error = Loc.T("The_Apartment_Does_Not_Exist") };

        return new QuickLoginContext
        {
            Payload = payload,
            Apartment = apartment,
            Resident = resident
        };
    }
}
