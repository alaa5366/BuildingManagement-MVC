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
    private readonly SqlAuthService _sqlAuth;
    private readonly IdentityAccountAuthenticationService _identityAuth;
    private readonly BuildingsService _buildings;
    private readonly ResidentsService _residents;
    private readonly UsersService _users;
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
        SqlAuthService sqlAuth,
        IdentityAccountAuthenticationService identityAuth,
        BuildingsService buildings,
        UsersService users,
        ResidentsService residents,
        LoginThrottle throttle,
        TwoFactorService twoFactor,
        QrGeneratorService qrGenerator,
        TargetQrService targetQr,
        ILogger<AccountController> logger)
    {
        _sqlAuth = sqlAuth;
        _identityAuth = identityAuth;
        _buildings = buildings;
        _users = users;
        _residents = residents;
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

        var res = await _identityAuth.AuthenticateAdminAsync(vm.Phone, vm.Pin, vm.BuildingId);
        res ??= await _sqlAuth.SignInAdminAsync(vm.BuildingId, vm.Phone, vm.Pin);

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

        var res = await _sqlAuth.SignInSuperAdminAsync(vm.Email, vm.Password);

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
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.Error = TempData["Error"];
        ViewBag.TwoFactorStep = GetPendingTwoFactorSignIn() != null;
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
        if (GetPendingTwoFactorSignIn() != null)
            return await CompleteTwoFactorVerificationAsync(vm.TwoFactorCode);

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

        var identityResult = await _identityAuth.AuthenticateResidentAsync(identifier, credential);
        if (identityResult != null)
        {
            if (!identityResult.Success)
            {
                _throttle.RegisterFailure(throttleKey, ClientIp);
                ViewBag.Error = Loc.T("Invalid_Login_Details");
                return View(vm);
            }

            _throttle.Reset(throttleKey);
            if (identityResult.MustChangePin)
                return StartPinChangeChallenge(identityResult, identifier);
            await SignInCookieAsync(identityResult);
            return RedirectBasedOnRole(identityResult.Role!);
        }

        var contexts = await _sqlAuth.FindAllContextsAsync(identifier, credential);

        if (contexts.Count == 0)
        {
            _throttle.RegisterFailure(throttleKey, ClientIp);
            ViewBag.Error = Loc.T("Invalid_Login_Details");
            return View(vm);
        }

        if (contexts.Count == 1)
        {
            var res = await _sqlAuth.SignInFromContextAsync(contexts[0]);
            if (!res.Success)
            {
                ViewBag.Error = MapError(res.Error, res.Reason);
                return View(vm);
            }

            return await CompleteSignInAsync(res, throttleKey);
        }

        // ✅ تحسين رسالة الترحيب
        var fullName = contexts.Count > 1
            ? Loc.T("Accounts_Linked_To_Number", identifier)
            : (contexts.FirstOrDefault()?.Name ?? identifier);

        var token = StoreContextsInSession(contexts, fullName, throttleKey);

        return RedirectToAction("ChooseContext", new { token });
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

        var res = await _sqlAuth.SignInFromContextAsync(selected);
        if (!res.Success)
        {
            TempData["Error"] = MapError(res.Error, res.Reason);
            return RedirectToAction("ChooseContext", new { token = sessionToken });
        }

        HttpContext.Session.Remove("ctx_" + sessionToken);
        return await CompleteSignInAsync(res, throttleKey);
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
                HttpContext.Session.Remove("2fa_pending");
                return null;
            }
            return pending;
        }
        catch (System.Text.Json.JsonException)
        {
            HttpContext.Session.Remove("2fa_pending");
            return null;
        }
    }

    private IActionResult StartPinChangeChallenge(AuthResult result, string identifier, string? buildingId = null, string? apartmentId = null)
    {
        HttpContext.Session.Clear();
        HttpContext.Session.SetString("resident_pin_change_account", result.Uid!);
        HttpContext.Session.SetString("resident_pin_change_stamp", result.SecurityStamp ?? "");
        HttpContext.Session.SetString("resident_pin_change_identifier", identifier);
        HttpContext.Session.SetString("resident_pin_change_role", result.Role ?? "resident");
        if (!string.IsNullOrWhiteSpace(buildingId))
            HttpContext.Session.SetString("resident_pin_change_building", buildingId);
        if (!string.IsNullOrWhiteSpace(apartmentId))
            HttpContext.Session.SetString("resident_pin_change_apartment", apartmentId);
        HttpContext.Session.SetString("resident_pin_change_expires",
            DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        return RedirectToAction(nameof(ChangePin));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ChangePin()
    {
        if (!HasValidPinChangeChallenge())
            return RedirectToAction(nameof(UnifiedLogin));
        return View(new ResidentPinChangeVm());
    }

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
        var result = pinChangeRole == "admin"
            ? await _identityAuth.AuthenticateAdminAsync(identifier, vm.NewPin, buildingId ?? "", HttpContext.RequestAborted)
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
        if ((result.Role is "admin" or "superadmin")
            && await _twoFactor.IsEnabledAsync(result.Uid!))
        {
            var pending = new PendingTwoFactorSignIn
            {
                Result = result,
                ThrottleKey = throttleKey ?? AcctKey(result.Email ?? result.Uid),
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
            };
            HttpContext.Session.SetString("2fa_pending", System.Text.Json.JsonSerializer.Serialize(pending));
            return RedirectToAction(nameof(UnifiedLogin));
        }

        if (!string.IsNullOrWhiteSpace(throttleKey)) _throttle.Reset(throttleKey);
        return await CompleteSignInAsync(result, AcctKey(identifier));
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
        return RedirectToAction(nameof(UnifiedLogin));
    }

    private async Task<IActionResult> CompleteTwoFactorVerificationAsync(string? code)
    {
        var pending = GetPendingTwoFactorSignIn();
        if (pending == null) return RedirectToAction(nameof(UnifiedLogin));

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
            ViewBag.Error = "The verification code is invalid or has already been used.";
            return View("UnifiedLogin", new UnifiedLoginVm());
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
    public IActionResult TwoFactorSetup()
    {
        var access = EnsureAdministratorTwoFactorAccess();
        if (access != null) return access;
        var secret = HttpContext.Session.GetString("2fa_setup_secret");
        var expiresText = HttpContext.Session.GetString("2fa_setup_expires");
        if (string.IsNullOrWhiteSpace(secret)
            || !long.TryParse(expiresText, out var expires)
            || expires <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            HttpContext.Session.Remove("2fa_setup_secret");
            HttpContext.Session.Remove("2fa_setup_expires");
            return RedirectToAction("Own", "Settings");
        }

        var account = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(account)) account = User.FindFirstValue(ClaimTypes.Name) ?? "administrator";
        var uri = TwoFactorService.CreateProvisioningUri(secret, account);
        ViewBag.Secret = secret;
        ViewBag.QrDataUri = "data:image/png;base64," + Convert.ToBase64String(_qrGenerator.GeneratePng(uri));
        return View(new TwoFactorVerifyVm());
    }

    /// <summary>Validates the first authenticator code and enables two-factor authentication.</summary>
    /// <param name="vm">The verification code submitted by the administrator.</param>
    [HttpPost]
    [EnableRateLimiting("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableTwoFactor(TwoFactorVerifyVm vm)
    {
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
            ViewBag.Error = "The code could not be verified. Check your authenticator app and try again.";
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
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _twoFactor.DisableAsync(uid, vm.Code))
        {
            TempData["Error"] = "The verification code is invalid. Two-factor authentication remains enabled.";
            return RedirectToAction("Own", "Settings");
        }
        TempData["Message"] = "Two-factor authentication has been disabled.";
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
                label = Loc.T("Apartment_N", a.Number) + (string.IsNullOrWhiteSpace(a.Label) ? "" : $" — {a.Label}")
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

        // ✅ GET مابيستهلكش التوكن — بيعرض صفحة تأكيد بس (معاينة الرابط في واتساب/الأنتي فايروس ماتحرقوش)
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
            await _residents.SetApartmentPinAsync(ctx.Payload!.BuildingId, ctx.Payload.AptId, newPin, "setpin-link");
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

        var residents = await _residents.GetByApartmentAsync(payload.BuildingId, payload.AptId);
        // Recovery must resolve to the explicitly designated primary resident;
        // it must never fall back to another occupant's identity.
        var resident = string.Equals(payload.Purpose, "recovery", StringComparison.OrdinalIgnoreCase)
            ? residents.FirstOrDefault(r => r.IsPrimary && !r.IsDisabled && r.IsActive)
            : residents.FirstOrDefault(r => r.IsPrimary && !r.IsDisabled && r.IsActive)
                ?? residents.FirstOrDefault(r => !r.IsDisabled && r.IsActive);
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
