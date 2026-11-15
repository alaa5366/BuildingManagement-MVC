using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  QrAccessController	دخول بـ QR مؤقت (بدون PIN)
// ============================================================
/// <summary>
/// Anonymous entry point for scanning stable apartment/admin QR codes.
/// </summary>
/// <remarks>
/// Access: anonymous.
/// </remarks>
[AllowAnonymous]
public class QrAccessController : Controller
{
    private readonly QrAuthService _qrAuth;
    private readonly QrTokenStoreService _qrStore;
    private readonly BuildingsService _buildings;

    /// <summary>
    /// Initializes a new instance of the <see cref="QrAccessController"/> class.
    /// </summary>
    public QrAccessController(QrAuthService qrAuth, QrTokenStoreService qrStore, BuildingsService buildings)
    {
        _qrAuth = qrAuth; _qrStore = qrStore; _buildings = buildings;
    }

    /// <summary>
    /// Enters the QR access data.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public IActionResult Enter(string token)
    {
        var (valid, payload, reason) = _qrAuth.ValidateAccessToken(token);
        if (!valid || payload == null)
        {
            ViewBag.Error = MapReason(reason);
            return View("AccessDenied");
        }

        // GET must remain side-effect free: link-preview bots must not consume a single-use token.
        ViewBag.Token = token;
        return View("AccessConfirm");
    }
    /// <summary>
    /// يؤكد عملية دخول عبر رمز QR ويُسجّلها.
    /// </summary>
    /// <param name="token">بيانات طلب التأكيد.</param>
    /// <returns>نتيجة عملية التأكيد.</returns>
    [HttpPost]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(string token)
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
            ViewBag.Error = Loc.T("Could_Not_Determine_The_Apartment");
            return View("AccessDenied");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "qr-" + apartment.Id),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(apartment.Owner) ? Loc.T("Apartment_N", apartment.Number) : apartment.Owner),
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
        "expired" => Loc.T("The_Link_Has_Expired"),
        "already-used" => Loc.T("This_Link_Has_Already_Been_Used"),
        "not-found" => Loc.T("The_Link_Is_Invalid"),
        "invalid-signature" => Loc.T("The_Link_Is_Invalid"),
        _ => Loc.T("Could_Not_Log_In_With_This")
    };
}
