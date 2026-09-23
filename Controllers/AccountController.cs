using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly AuthService _auth;
    private readonly BuildingsService _buildings;

    public AccountController(AuthService auth, BuildingsService buildings)
    {
        _auth = auth;
        _buildings = buildings;
    }

    public IActionResult LoginChoice() => View();

    [HttpGet]
    public IActionResult LoginSuperAdmin() => View(new SuperAdminLoginVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginSuperAdmin(SuperAdminLoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var res = await _auth.SignInSuperAdminAsync(vm.Email, vm.Password);
        if (!res.Success)
        {
            ViewBag.Error = MapError(res.Error, res.Reason);
            return View(vm);
        }

        await SignInCookieAsync(res);
        return RedirectToAction("Index", "SuperAdminHome");
    }

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

        var res = await _auth.SignInAdminAsync(vm.BuildingId, vm.Phone, vm.Pin);
        if (!res.Success)
        {
            ViewBag.Error = MapError(res.Error, res.Reason);
            ViewBag.Buildings = await _buildings.GetAllAsync();
            return View(vm);
        }

        await SignInCookieAsync(res);
        return RedirectToAction("Index", "AdminHome");
    }

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

        var res = await _auth.SignInResidentAsync(vm.BuildingId, vm.FloorOrder, vm.AptNumber, vm.Whatsapp, vm.Pin);
        if (!res.Success)
        {
            ViewBag.Error = MapError(res.Error, res.Reason);
            ViewBag.Buildings = await _buildings.GetAllAsync();
            return View(vm);
        }

        await SignInCookieAsync(res);
        return RedirectToAction("Index", "ResidentHome");
    }

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

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    private static string MapError(string? error, string? reason = null) => error switch
    {
        "building-not-found" => "العمارة غير موجودة",
        "no-admin" => "لا يوجد أي أدمن مسجل بعد",
        "wrong-credentials" => "رقم الهاتف أو الـ PIN غير صحيح",
        "wrong-wa" => "رقم الواتساب غير مطابق لبيانات الشقة",
        "wrong-pin" => "الـ PIN غير صحيح",
        "apt-not-found" => "الشقة غير موجودة",
        "not-superadmin" => "هذا البريد غير مصرح له بصلاحية Super Admin",
        "account-disabled" => "🚫 هذا الحساب معطّل" + (string.IsNullOrWhiteSpace(reason) ? "" : $" — السبب: {reason}"),
        "EMAIL_NOT_FOUND" or "INVALID_PASSWORD" or "INVALID_LOGIN_CREDENTIALS" => "بيانات الدخول غير صحيحة",
        null => "حدث خطأ غير متوقع",
        _ => "تعذّر تسجيل الدخول: " + error
    };
}
