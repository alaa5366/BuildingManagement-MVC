using BuildingManagementMvc.Attributes;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  QrController	إدارة QRs (Stable + Access Links)
// ============================================================
/// <summary>
/// Generation of QR codes and temporary login links for apartments and admins.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>admin</c>.
/// </remarks>
[Authorize(Roles = "admin")]
[AdminPermission(BuildingManagementMvc.Models.AdminPermissions.ManageResidents)]
public class QrController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly QrGeneratorService _qrGen;
    private readonly QrAuthService _qrAuth;
    private readonly QrTokenStoreService _qrStore;
    private readonly AuditLogService _audit;
    private readonly ILogger<QrController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="QrController"/> class.
    /// </summary>
    public QrController(BuildingsService buildings, QrGeneratorService qrGen, QrAuthService qrAuth,
        QrTokenStoreService qrStore, AuditLogService audit, ILogger<QrController> logger)
    {
        _buildings = buildings; _qrGen = qrGen; _qrAuth = qrAuth; _qrStore = qrStore; _audit = audit;
        _logger = logger;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    /// <summary>
    /// Shows the main page of the QR screen.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        return View(building);
    }

    // كود QR ثابت للشقة (يتلصق على الباب) — بيودّي لصفحة دخول الساكن
    // مع تعبئة العمارة/الدور/رقم الشقة تلقائيًا
    /// <summary>
    /// Renders a printable badge (with a QR code) for one apartment.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> ApartmentBadge(string aptId)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        var apt = building?.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (building == null || apt == null) return NotFound();
        var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var url = _qrGen.GetApartmentLoginUrl(baseUrl, building.Id, floor?.Order ?? 0, apt.Number);
        var png = _qrGen.GeneratePng(url, 10);

        return File(png, "image/png", $"QR-{building.BuildingNumber}-apt-{apt.Number}.png");
    }

    // رابط دخول مؤقت (بدون تليفون/PIN) — لما الساكن ينسى الـ PIN مثلًا
    /// <summary>
    /// Generates the access link.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateAccessLink(string aptId, int minutes, bool oneTime)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        var apt = building?.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (building == null || apt == null) return NotFound();

        // Keep temporary access capabilities short-lived and fail closed.
        // The token must be durably stored before a usable link is ever shown.
        if (minutes is < 1 or > 60)
        {
            ModelState.AddModelError(nameof(minutes), "مدة رابط الدخول يجب أن تكون من دقيقة إلى 60 دقيقة.");
            return BadRequest(ModelState);
        }

        var (token, payload, expiresAt) = _qrAuth.GenerateAccessToken(
            building.Id, apt.Id, minutes, oneTime, User.Identity?.Name ?? "admin");

        if (!await _qrStore.SaveTokenAsync(payload))
        {
            _logger.LogError("QR access token persistence failed for building {BuildingId}, apartment {ApartmentId}.",
                BuildingId, apt.Id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "qr-token-persistence-failed", message = "تعذر حفظ رابط الدخول. لم يتم إنشاء رابط صالح." });
        }

        _audit.Push(building, "qr_access_generate",
            $"رابط دخول مؤقت — شقة {apt.Number} ({minutes} دقيقة)",
            "admin", User.Identity?.Name ?? "admin", apt.Number.ToString());
        await _buildings.SaveFullAsync(building);

        Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.ReferrerPolicy = "no-referrer";

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        ViewBag.Link = $"{baseUrl}/QrAccess/Enter?token={token}";
        ViewBag.ExpiresAt = expiresAt;
        ViewBag.Apt = apt;
        ViewBag.Building = building;
        return View("AccessLinkResult");
    }
}
