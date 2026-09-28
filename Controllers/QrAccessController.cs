using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

// نفس منطق التحقق من توكن QR في js/features/qr-token-store.js — بيسمح
// للساكن يدخل مباشرة من غير تليفون/PIN لو معاه رابط/QR صالح من الأدمن
[AllowAnonymous]
public class QrAccessController : Controller
{
    private readonly QrAuthService _qrAuth;
    private readonly QrTokenStoreService _qrStore;
    private readonly BuildingsService _buildings;

    public QrAccessController(QrAuthService qrAuth, QrTokenStoreService qrStore, BuildingsService buildings)
    {
        _qrAuth = qrAuth; _qrStore = qrStore; _buildings = buildings;
    }

    [HttpGet]
    public async Task<IActionResult> Enter(string token)
    {
        var (valid, payload, reason) = _qrAuth.ValidateAccessToken(token);
        if (!valid || payload == null)
        {
            ViewBag.Error = MapReason(reason);
            return View("AccessDenied");
        }

        var (allowed, storeReason, bld, apt) = await _qrStore.CheckAndUseTokenAsync(payload.Uid);
        if (!allowed)
        {
            ViewBag.Error = MapReason(storeReason);
            return View("AccessDenied");
        }

        var building = await _buildings.GetByIdAsync(bld!);
        var apartment = building?.Apartments.FirstOrDefault(a => a.Id == apt);
        if (building == null || apartment == null)
        {
            ViewBag.Error = "تعذّر تحديد الشقة";
            return View("AccessDenied");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "qr-" + apartment.Id),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(apartment.Owner) ? $"شقة {apartment.Number}" : apartment.Owner),
            new(ClaimTypes.Role, "resident"),
            new("buildingId", building.Id),
            new("apartmentId", apartment.Id),
            new("apartmentNumber", apartment.Number.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "ResidentHome");
    }

    private static string MapReason(string? reason) => reason switch
    {
        "expired" => "⏰ انتهت صلاحية الرابط",
        "already-used" => "🚫 الرابط ده اتستخدم قبل كده",
        "not-found" => "الرابط غير صالح",
        "invalid-signature" => "الرابط غير صالح",
        _ => "تعذّر الدخول بهذا الرابط"
    };
}
