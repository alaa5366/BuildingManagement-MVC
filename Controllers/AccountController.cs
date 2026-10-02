using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly AuthService _auth;
    private readonly BuildingsService _buildings;
    private readonly UsersService _users;   



    private readonly LoginThrottle _throttle;
    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();
    private static string LockedMsg => Loc.T("Too_Many_Failed_Login_Attempts_Try");

    public AccountController(AuthService auth, BuildingsService buildings, UsersService users,
        LoginThrottle throttle)
    {
        _auth = auth;
        _buildings = buildings;
        _users = users;
        _throttle = throttle;
    }

    public IActionResult LoginChoice() => RedirectToAction("UnifiedLogin");

    // ============================================================
    // Super Admin Login
    // ============================================================
    [HttpGet]
    public IActionResult LoginSuperAdmin() => View(new SuperAdminLoginVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginSuperAdmin(SuperAdminLoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var throttleKey = "sa:" + vm.Email;
        if (_throttle.IsLocked(throttleKey, ClientIp))
        {
            ViewBag.Error = LockedMsg;
            return View(vm);
        }

        var res = await _auth.SignInSuperAdminAsync(vm.Email, vm.Password);
        if (!res.Success)
        {
            _throttle.RegisterFailure(throttleKey, ClientIp);
            ViewBag.Error = MapError(res.Error, res.Reason);
            return View(vm);
        }

        _throttle.Reset(throttleKey);
        await SignInCookieAsync(res);
        return RedirectToAction("Index", "SuperAdminHome");
    }

    // ============================================================
    // Admin Login — يدعم QR (مشفّر)
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> LoginAdmin(string? bld, string? sig,
        [FromServices] QrSecurityService qrSecurity)
    {
        ViewBag.Buildings = await _buildings.GetAllAsync();

        if (!string.IsNullOrEmpty(bld))
        {
            // ✅ التحقق من التوقيع
            if (!string.IsNullOrEmpty(sig))
            {
                var path = "/Account/LoginAdmin";
                var query = $"bld={bld}";
                if (!qrSecurity.VerifyStableQr(path, query, sig))
                {
                    ViewBag.Error = Loc.T("This_Link_Has_Been_Modified_Or");
                    return View(new AdminLoginVm());
                }
            }

            var building = await _buildings.GetByIdAsync(bld);
            if (building != null)
            {
                ViewBag.QrBuildingId = building.Id;
                ViewBag.QrBuildingName = building.Name;
                ViewBag.QrBuildingNumber = building.BuildingNumber;
            }
        }

        return View(new AdminLoginVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginAdmin(AdminLoginVm vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Buildings = await _buildings.GetAllAsync();
            return View(vm);
        }

        var throttleKey = $"adm:{vm.BuildingId}:{vm.Phone}";
        if (_throttle.IsLocked(throttleKey, ClientIp))
        {
            ViewBag.Error = LockedMsg;
            ViewBag.Buildings = await _buildings.GetAllAsync();
            return View(vm);
        }

        var res = await _auth.SignInAdminAsync(vm.BuildingId, vm.Phone, vm.Pin);
        if (!res.Success)
        {
            _throttle.RegisterFailure(throttleKey, ClientIp);
            ViewBag.Error = MapError(res.Error, res.Reason);
            ViewBag.Buildings = await _buildings.GetAllAsync();
            return View(vm);
        }

        _throttle.Reset(throttleKey);
        await SignInCookieAsync(res);
        return RedirectToAction("Index", "AdminHome");
    }

    // ============================================================
    // Resident Login — يدعم QR (مشفّر)
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> LoginResident(string? apt, string? sig,
        [FromServices] QrSecurityService qrSecurity)
    {
        ViewBag.Buildings = await _buildings.GetAllAsync();

        if (!string.IsNullOrEmpty(apt) && apt.Contains('_'))
        {
            // ✅ التحقق من التوقيع
            if (!string.IsNullOrEmpty(sig))
            {
                var path = "/Account/LoginResident";
                var query = $"apt={apt}";
                if (!qrSecurity.VerifyStableQr(path, query, sig))
                {
                    ViewBag.Error = Loc.T("This_Link_Has_Been_Modified_Or");
                    return View(new ResidentLoginVm());
                }
            }

            var parts = apt.Split('_', 2);
            var buildingId = parts[0];
            var aptId = parts[1];

            var building = await _buildings.GetByIdAsync(buildingId);
            if (building != null)
            {
                var aptEntity = building.Apartments.FirstOrDefault(a => a.Id == aptId);
                if (aptEntity != null)
                {
                    var floor = building.Floors.FirstOrDefault(f => f.Id == aptEntity.FloorId);

                    ViewBag.QrBuildingId = building.Id;
                    ViewBag.QrBuildingName = building.Name;
                    ViewBag.QrFloorOrder = floor?.Order ?? 0;
                    ViewBag.QrFloorLabel = floor?.Label ?? "";
                    ViewBag.QrAptNumber = aptEntity.Number;
                    ViewBag.QrAptLabel = aptEntity.Label;
                }
            }
        }

        return View(new ResidentLoginVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginResident(ResidentLoginVm vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Buildings = await _buildings.GetAllAsync();
            return View(vm);
        }

        var throttleKey = $"res:{vm.BuildingId}:{vm.FloorOrder}:{vm.AptNumber}";
        if (_throttle.IsLocked(throttleKey, ClientIp))
        {
            ViewBag.Error = LockedMsg;
            ViewBag.Buildings = await _buildings.GetAllAsync();
            return View(vm);
        }

        var res = await _auth.SignInResidentAsync(vm.BuildingId, vm.FloorOrder, vm.AptNumber, vm.Whatsapp, vm.Pin);
        if (!res.Success)
        {
            _throttle.RegisterFailure(throttleKey, ClientIp);
            ViewBag.Error = MapError(res.Error, res.Reason);
            ViewBag.Buildings = await _buildings.GetAllAsync();
            return View(vm);
        }

        _throttle.Reset(throttleKey);
        await SignInCookieAsync(res);
        return RedirectToAction("Index", "ResidentHome");
    }

    // ============================================================
    // الدخول الموحد
    // ============================================================
    [HttpGet]
    public IActionResult UnifiedLogin([FromServices] IConfiguration config, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        // ✅ Firebase Config للـ JS
        var firebaseConfig = new
        {
            apiKey = config["Firebase:WebApiKey"],
            authDomain = $"{config["Firebase:ProjectId"]}.firebaseapp.com",
            projectId = config["Firebase:ProjectId"]
        };

        ViewBag.FirebaseConfig = System.Text.Json.JsonSerializer.Serialize(firebaseConfig);

        return View(new UnifiedLoginVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnifiedLogin(UnifiedLoginVm vm,
        [FromServices] UnifiedAuthService unifiedAuth)
    {
        if (!ModelState.IsValid) return View(vm);

        var identifier = vm.Identifier.Trim();
        var credential = vm.Credential.Trim();

        var throttleKey = "uni:" + identifier;
        if (_throttle.IsLocked(throttleKey, ClientIp))
        {
            ViewBag.Error = LockedMsg;
            return View(vm);
        }

        var contexts = await unifiedAuth.FindAllContextsAsync(identifier, credential);

        if (contexts.Count == 0)
        {
            _throttle.RegisterFailure(throttleKey, ClientIp);
            ViewBag.Error = Loc.T("Invalid_Login_Details");
            return View(vm);
        }

        _throttle.Reset(throttleKey);

        // ✅ لو سياق واحد → دخول مباشر
        if (contexts.Count == 1)
        {
            var res = await unifiedAuth.SignInFromContextAsync(contexts[0]);
            if (!res.Success)
            {
                ViewBag.Error = MapError(res.Error, res.Reason);
                return View(vm);
            }

            await SignInCookieAsync(res);
            return RedirectBasedOnRole(res.Role!);
        }

        // ✅ لو أكتر من سياق → صفحة الاختيار
        var fullName = contexts.FirstOrDefault(c => !string.IsNullOrEmpty(c.Name))?.Name ?? identifier;
        var token = unifiedAuth.StoreContexts(contexts, fullName);

        return RedirectToAction("ChooseContext", new { token });
    }

    // ============================================================
    // صفحة اختيار السياق
    // ============================================================
    [HttpGet]
    public IActionResult ChooseContext(string token,
        [FromServices] UnifiedAuthService unifiedAuth)
    {
        var (contexts, fullName) = unifiedAuth.GetStoredContexts(token);

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

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChooseContext(string sessionToken, string contextId,
        [FromServices] UnifiedAuthService unifiedAuth)
    {
        var (contexts, _) = unifiedAuth.GetStoredContexts(sessionToken);

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

        var res = await unifiedAuth.SignInFromContextAsync(selected);
        if (!res.Success)
        {
            TempData["Error"] = MapError(res.Error, res.Reason);
            return RedirectToAction("ChooseContext", new { token = sessionToken });
        }

        unifiedAuth.RemoveStoredContexts(sessionToken);
        await SignInCookieAsync(res);
        return RedirectBasedOnRole(res.Role!);
    }

    // ============================================================
    // Helper
    // ============================================================
    private IActionResult RedirectBasedOnRole(string role) => role switch
    {
        "superadmin" => RedirectToAction("Index", "SuperAdminHome"),
        "admin" => RedirectToAction("Index", "AdminHome"),
        "resident" => RedirectToAction("Index", "ResidentHome"),
        _ => RedirectToAction("UnifiedLogin")
    };

    // ============================================================
    // Quick Login (QR السريع)
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> QuickLogin(string token,
        [FromServices] QrSecurityService qrSecurity)
    {
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("LoginChoice");

        var validation = await qrSecurity.ValidateQuickToken(token);
        if (!validation.IsSuccess)
        {
            ViewBag.Error = validation.Error;
            return View("QuickLoginError");
        }

        var payload = validation.Data!;

        var building = await _buildings.GetByIdAsync(payload.BuildingId);
        if (building == null)
        {
            ViewBag.Error = Loc.T("The_Building_Does_Not_Exist");
            return View("QuickLoginError");
        }

        var apt = building.Apartments.FirstOrDefault(a => a.Id == payload.AptId);
        if (apt == null)
        {
            ViewBag.Error = Loc.T("The_Apartment_Does_Not_Exist");
            return View("QuickLoginError");
        }

        // ✅ دخول مباشر
        var res = await _auth.SignInResidentAsync(
            payload.BuildingId,
            payload.FloorOrder,
            payload.AptNumber,
            apt.Phone,
            apt.Pin
        );

        if (!res.Success)
        {
            ViewBag.Error = MapError(res.Error, res.Reason);
            return View("QuickLoginError");
        }

        await qrSecurity.MarkQuickTokenUsed(payload.TokenId);
        await SignInCookieAsync(res);
        return RedirectToAction("Index", "ResidentHome");
    }
    // ============================================================
    // Google Sign-In (للـ Super Admin فقط)
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoogleSignIn(string idToken, string? email,
        [FromServices] FirebaseAdminService fbAdmin,
        [FromServices] ILogger<AccountController> logger)
    {
        try
        {
            var throttleKey = "google:" + (ClientIp ?? "unknown");
            if (_throttle.IsLocked(throttleKey, ClientIp))
                return Json(new { success = false, error = LockedMsg });

            // ✅ الإيميل لازم ييجي من الـ ID Token بعد التحقق منه من Firebase،
            // مش من الـ request (الـ email اللي في الـ form بنتجاهله).
            var verified = await fbAdmin.VerifyIdTokenAsync(idToken);

            if (verified == null || !verified.EmailVerified || !AuthService.IsSuperAdminEmail(verified.Email))
            {
                _throttle.RegisterFailure(throttleKey, ClientIp);
                return Json(new { success = false, error = Loc.T("This_Email_Is_Not_Authorized_For") });
            }

            var result = AuthResult.Ok(
                uid: verified.Uid,
                role: "superadmin",
                email: verified.Email,
                name: verified.Email.Split('@')[0],
                buildingIds: new List<string>()
            );

            await SignInCookieAsync(result);

            return Json(new { success = true, redirectUrl = "/SuperAdminHome" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GoogleSignIn failed");
            return Json(new { success = false, error = Loc.T("Login_Error_2") });
        }
    }

    // ============================================================
    // Logout
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("LoginChoice");
    }

    private async Task SignInCookieAsync(AuthResult res)
    {
        var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, res.Uid!),
        new(ClaimTypes.Name, res.Name ?? ""),
        new(ClaimTypes.Email, res.Email ?? ""),
        new(ClaimTypes.Role, res.Role!),
    };

        foreach (var bId in res.BuildingIds)
            claims.Add(new Claim("buildingId", bId));

        if (res.ApartmentId != null) claims.Add(new Claim("apartmentId", res.ApartmentId));
        if (res.ApartmentNumber != null) claims.Add(new Claim("apartmentNumber", res.ApartmentNumber.Value.ToString()));

        // ✅ المرحلة 15 — إضافة الصلاحيات في الـ Claims
        if (res.Role == "superadmin")
        {
            foreach (var p in BuildingManagementMvc.Models.AdminPermissions.SuperAdminAll)
                claims.Add(new Claim("perm", p));
        }
        else if (res.Role == "admin")
        {
            var user = await _users.GetByUidAsync(res.Uid!);
            if (user?.Permissions != null)
            {
                foreach (var p in user.Permissions)
                    claims.Add(new Claim("perm", p));
            }
        }
        else if (res.Role == "resident")
        {
            foreach (var p in BuildingManagementMvc.Models.AdminPermissions.ResidentBasic)
                claims.Add(new Claim("perm", p));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    private static string MapError(string? error, string? reason = null) => error switch
    {
        "building-not-found" => Loc.T("The_Building_Does_Not_Exist"),
        "no-admin" => Loc.T("No_Admin_Has_Been_Registered_Yet"),
        "wrong-credentials" => Loc.T("Incorrect_Phone_Number_Or_PIN"),
        "wrong-wa" => Loc.T("The_WhatsApp_Number_Does_Not_Match"),
        "wrong-pin" => Loc.T("Incorrect_PIN"),
        "apt-not-found" => Loc.T("The_Apartment_Does_Not_Exist"),
        "not-superadmin" => Loc.T("This_Email_Is_Not_Authorized_For_2"),
        "account-disabled" => Loc.T("This_Account_Is_Disabled") + (string.IsNullOrWhiteSpace(reason) ? "" : Loc.T("Reason_N", reason)),
        "EMAIL_NOT_FOUND" or "INVALID_PASSWORD" or "INVALID_LOGIN_CREDENTIALS" => Loc.T("Invalid_Login_Details_2"),
        null => Loc.T("An_Unexpected_Error_Occurred"),
        _ => Loc.T("Could_Not_Log_In_2") + error
    };
}