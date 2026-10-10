using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly SqlAuthService _sqlAuth;
    private readonly BuildingsService _buildings;
    private readonly ResidentsService _residents;   
    private readonly UsersService _users;
    private readonly LoginThrottle _throttle;
    private readonly ILogger<AccountController> _log;
    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();
    private static string LockedMsg => Loc.T("Too_Many_Failed_Login_Attempts_Try");

    public AccountController(
        SqlAuthService sqlAuth,
        BuildingsService buildings,
        UsersService users,
        ResidentsService residents,
        LoginThrottle throttle,
        ILogger<AccountController> log)
    {
        _sqlAuth = sqlAuth;
        _buildings = buildings;
        _users = users;
        _residents = residents;
        _throttle = throttle;
        _log = log;
    }

    public IActionResult LoginChoice() => RedirectToAction("UnifiedLogin");

    // ═══════════════════════════════════════════════════════════
    // 1. Resident Login
    // ═══════════════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> LoginResident()
    {
        ViewBag.Buildings = await _buildings.GetAllAsync();
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

        var throttleKey = $"res:{vm.BuildingId}:{vm.Whatsapp}";
        if (_throttle.IsLocked(throttleKey, ClientIp))
        {
            ViewBag.Error = LockedMsg;
            ViewBag.Buildings = await _buildings.GetAllAsync();
            return View(vm);
        }

        var res = await _sqlAuth.SignInResidentAsync(
            vm.BuildingId, vm.FloorId, vm.AptId, vm.Whatsapp, vm.Pin);

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

    // ═══════════════════════════════════════════════════════════
    // 2. Admin Login
    // ═══════════════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> LoginAdmin()
    {
        ViewBag.Buildings = await _buildings.GetAllAsync();
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

        var res = await _sqlAuth.SignInAdminAsync(vm.BuildingId, vm.Phone, vm.Pin);

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

    // ═══════════════════════════════════════════════════════════
    // 3. SuperAdmin Login
    // ═══════════════════════════════════════════════════════════
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

        var res = await _sqlAuth.SignInSuperAdminAsync(vm.Email, vm.Password);

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

    // ═══════════════════════════════════════════════════════════
    // 4. Google Sign-In (لـ Super Admin فقط)
    // ═══════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoogleSignIn(
        string idToken,
        string? email,
        [FromServices] FirebaseAdminService fbAdmin,
        [FromServices] ILogger<AccountController> logger)
    {
        try
        {
            var throttleKey = "google:" + (ClientIp ?? "unknown");
            if (_throttle.IsLocked(throttleKey, ClientIp))
                return Json(new { success = false, error = LockedMsg });

            var verified = await fbAdmin.VerifyIdTokenAsync(idToken);

            if (verified == null)
            {
                _throttle.RegisterFailure(throttleKey, ClientIp);
                return Json(new { success = false, error = "Invalid ID Token" });
            }

            if (!verified.EmailVerified)
            {
                _throttle.RegisterFailure(throttleKey, ClientIp);
                return Json(new { success = false, error = "Email not verified" });
            }

            if (!SqlAuthService.IsSuperAdminEmail(verified.Email))
            {
                _throttle.RegisterFailure(throttleKey, ClientIp);
                return Json(new
                {
                    success = false,
                    error = Loc.T("This_Email_Is_Not_Authorized_For")
                });
            }

            var result = AuthResult.Ok(
                uid: verified.Uid,
                role: "superadmin",
                email: verified.Email,
                name: verified.Email.Split('@')[0],
                buildingIds: new List<string>());

            await SignInCookieAsync(result);

            _log.LogInformation("[GoogleSignIn] SuperAdmin logged in: {Email}", verified.Email);

            return Json(new { success = true, redirectUrl = "/SuperAdminHome" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GoogleSignIn failed");
            return Json(new { success = false, error = Loc.T("Login_Error_2") });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 5. Unified Login
    // ═══════════════════════════════════════════════════════════
    [HttpGet]
    public IActionResult UnifiedLogin(
        [FromServices] IConfiguration config,
        string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.FirebaseApiKey = config["Firebase:WebApiKey"] ?? "";
        ViewBag.FirebaseProjectId = config["Firebase:ProjectId"] ?? "";
        ViewBag.FirebaseAuthDomain = $"{config["Firebase:ProjectId"]}.firebaseapp.com";

        return View(new UnifiedLoginVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnifiedLogin(UnifiedLoginVm vm)
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

        var contexts = await _sqlAuth.FindAllContextsAsync(identifier, credential);

        if (contexts.Count == 0)
        {
            _throttle.RegisterFailure(throttleKey, ClientIp);
            ViewBag.Error = Loc.T("Invalid_Login_Details");
            return View(vm);
        }

        _throttle.Reset(throttleKey);

        if (contexts.Count == 1)
        {
            var res = await _sqlAuth.SignInFromContextAsync(contexts[0]);
            if (!res.Success)
            {
                ViewBag.Error = MapError(res.Error, res.Reason);
                return View(vm);
            }

            await SignInCookieAsync(res);
            return RedirectBasedOnRole(res.Role!);
        }

        var fullName = contexts.FirstOrDefault(c => !string.IsNullOrEmpty(c.Name))?.Name ?? identifier;
        var token = StoreContextsInSession(contexts, fullName);

        return RedirectToAction("ChooseContext", new { token });
    }

    // ═══════════════════════════════════════════════════════════
    // 6. ChooseContext
    // ═══════════════════════════════════════════════════════════
    [HttpGet]
    public IActionResult ChooseContext(string token)
    {
        var (contexts, fullName) = GetContextsFromSession(token);

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChooseContext(string sessionToken, string contextId)
    {
        var (contexts, _) = GetContextsFromSession(sessionToken);

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
        await SignInCookieAsync(res);
        return RedirectBasedOnRole(res.Role!);
    }

    // ═══════════════════════════════════════════════════════════
    // 7. Logout
    // ═══════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("LoginChoice");
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════
    private string StoreContextsInSession(List<UserContext> contexts, string fullName)
    {
        var token = Guid.NewGuid().ToString("N");
        var data = System.Text.Json.JsonSerializer.Serialize(new
        {
            Contexts = contexts,
            FullName = fullName
        });
        HttpContext.Session.SetString("ctx_" + token, data);
        return token;
    }

    private (List<UserContext>?, string?) GetContextsFromSession(string token)
    {
        var json = HttpContext.Session.GetString("ctx_" + token);
        if (string.IsNullOrEmpty(json)) return (null, null);

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var contextsJson = root.GetProperty("Contexts").GetRawText();
            var contexts = System.Text.Json.JsonSerializer.Deserialize<List<UserContext>>(contextsJson);

            var fullName = root.TryGetProperty("FullName", out var fn) ? fn.GetString() : null;

            return (contexts, fullName);
        }
        catch
        {
            return (null, null);
        }
    }

    private IActionResult RedirectBasedOnRole(string role) => role switch
    {
        "superadmin" => RedirectToAction("Index", "SuperAdminHome"),
        "admin" => RedirectToAction("Index", "AdminHome"),
        "resident" => RedirectToAction("Index", "ResidentHome"),
        _ => RedirectToAction("UnifiedLogin")
    };

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
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
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
        "wrong-credentials" => Loc.T("Incorrect_Phone_Number_Or_PIN"),
        "wrong-wa" => Loc.T("The_WhatsApp_Number_Does_Not_Match"),
        "wrong-pin" => Loc.T("Incorrect_PIN"),
        "not-in-building" => Loc.T("You_Are_Not_In_This_Building"),
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
    [HttpGet]
    [AllowAnonymous]
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
                label = $"{f.Label} (رقم {f.Order})"
            })
            .ToList();

        return Json(floors);
    }

    [HttpGet]
    [AllowAnonymous]
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
                label = $"شقة {a.Number}" + (string.IsNullOrWhiteSpace(a.Label) ? "" : $" — {a.Label}")
            })
            .ToList();

        return Json(apts);
    }
    // ═══════════════════════════════════════════════════════════
    // Quick Login (QR السريع)
    // ═══════════════════════════════════════════════════════════
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> QuickLogin(
        string token,
        [FromServices] QrSecurityService qrSecurity)
    {
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("LoginChoice");

        // 1. تحقق من الـ Token
        var validation = await qrSecurity.ValidateQuickToken(token);
        if (!validation.IsSuccess)
        {
            ViewBag.Error = validation.Error;
            return View("QuickLoginError");
        }

        var payload = validation.Data!;

        // 2. جيب الـ Building
        var building = await _buildings.GetByIdAsync(payload.BuildingId);
        if (building == null)
        {
            ViewBag.Error = Loc.T("The_Building_Does_Not_Exist");
            return View("QuickLoginError");
        }

        // 3. جيب الشقة
        var apt = building.Apartments.FirstOrDefault(a => a.Id == payload.AptId);
        if (apt == null)
        {
            ViewBag.Error = Loc.T("The_Apartment_Does_Not_Exist");
            return View("QuickLoginError");
        }

        // 4. جيب الساكن من الـ Residents table
        var residents = await _residents.GetByApartmentAsync(payload.BuildingId, payload.AptId);
        var resident = residents.FirstOrDefault(r => r.IsPrimary && !r.IsDisabled)
                    ?? residents.FirstOrDefault(r => !r.IsDisabled);

        if (resident == null)
        {
            ViewBag.Error = Loc.T("The_Apartment_Does_Not_Exist");
            return View("QuickLoginError");
        }

        // 5. Sign In Cookie
        var result = AuthResult.Ok(
            uid: resident.Uid,
            role: "resident",
            email: resident.Email ?? "",
            name: string.IsNullOrWhiteSpace(resident.Name)
                ? $"ساكن شقة {apt.Number}" : resident.Name,
            buildingIds: new List<string> { building.Id },
            aptId: resident.ApartmentId,
            aptNumber: apt.Number);

        await SignInCookieAsync(result);

        // 6. Mark Token as used
        try
        {
            await qrSecurity.MarkQuickTokenUsed(payload.TokenId);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[QuickLogin] Failed to mark token as used");
        }

        _log.LogInformation("[QuickLogin] Resident logged in: {Name} (apt {Apt})",
            resident.Name, apt.Number);

        return RedirectToAction("Index", "ResidentHome");
    }
}