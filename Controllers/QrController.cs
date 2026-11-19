using BuildingManagementMvc.Attributes;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "admin")]
[AdminPermission(BuildingManagementMvc.Models.AdminPermissions.ManageResidents)]
public class QrController : Controller
{
    private readonly TargetQrService _targetQr;
    private readonly QrGeneratorService _qrGen;
    private readonly QrAuthService _qrAuth;
    private readonly QrTokenStoreService _qrStore;
    private readonly ILogger<QrController> _logger;

    public QrController(
        TargetQrService targetQr,
        QrGeneratorService qrGen,
        QrAuthService qrAuth,
        QrTokenStoreService qrStore,
        ILogger<QrController> logger)
    {
        _targetQr = targetQr;
        _qrGen = qrGen;
        _qrAuth = qrAuth;
        _qrStore = qrStore;
        _logger = logger;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await _targetQr.GetBuildingPageAsync(BuildingId, HttpContext.RequestAborted);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ApartmentBadge(string aptId)
    {
        var apartment = await _targetQr.GetApartmentAsync(
            BuildingId, aptId, requireActive: true, HttpContext.RequestAborted);
        if (apartment is null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var url = _qrGen.GetApartmentLoginUrl(
            baseUrl, apartment.BuildingId, apartment.FloorOrder, apartment.ApartmentNumber);
        var png = _qrGen.GeneratePng(url, 10);
        return File(png, "image/png",
            $"QR-{apartment.BuildingNumber}-apt-{apartment.ApartmentNumber}.png");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateAccessLink(string aptId, int minutes, bool oneTime)
    {
        if (minutes is < 1 or > 60)
        {
            ModelState.AddModelError(nameof(minutes), "مدة رابط الدخول يجب أن تكون من دقيقة إلى 60 دقيقة.");
            return BadRequest(ModelState);
        }

        var apartment = await _targetQr.GetApartmentAsync(
            BuildingId, aptId, requireActive: true, HttpContext.RequestAborted);
        if (apartment is null) return NotFound();

        var (token, payload, expiresAt) = _qrAuth.GenerateAccessToken(
            BuildingId, apartment.ApartmentId, minutes, oneTime, User.Identity?.Name ?? "admin");

        if (!await _qrStore.SaveTokenAsync(payload))
        {
            _logger.LogError(
                "QR access token persistence failed for building {BuildingId}, apartment {ApartmentId}.",
                BuildingId, apartment.ApartmentId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "qr-token-persistence-failed", message = "تعذر حفظ رابط الدخول. لم يتم إنشاء رابط صالح." });
        }

        Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.ReferrerPolicy = "no-referrer";

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        ViewBag.Link = $"{baseUrl}/QrAccess/Enter?token={token}";
        ViewBag.ExpiresAt = expiresAt;
        ViewBag.ApartmentNumber = apartment.ApartmentNumber;
        ViewBag.BuildingName = apartment.BuildingName;
        return View("AccessLinkResult");
    }
}
