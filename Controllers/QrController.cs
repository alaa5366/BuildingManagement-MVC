using BuildingManagementMvc.Attributes;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

// نفس شاشات js/features/qr-generator.js و js/views/admin/modals/qr-access-modal.js
[Authorize(Roles = "admin")]
[AdminPermission(BuildingManagementMvc.Models.AdminPermissions.ManageResidents)]
public class QrController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly QrGeneratorService _qrGen;
    private readonly QrAuthService _qrAuth;
    private readonly QrTokenStoreService _qrStore;
    private readonly AuditLogService _audit;

    public QrController(BuildingsService buildings, QrGeneratorService qrGen, QrAuthService qrAuth,
        QrTokenStoreService qrStore, AuditLogService audit)
    {
        _buildings = buildings; _qrGen = qrGen; _qrAuth = qrAuth; _qrStore = qrStore; _audit = audit;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        return View(building);
    }

    // كود QR ثابت للشقة (يتلصق على الباب) — بيودّي لصفحة دخول الساكن
    // مع تعبئة العمارة/الدور/رقم الشقة تلقائيًا
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
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateAccessLink(string aptId, int minutes, bool oneTime)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        var apt = building?.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (building == null || apt == null) return NotFound();

        var (token, payload, expiresAt) = _qrAuth.GenerateAccessToken(building.Id, apt.Id, minutes <= 0 ? 15 : minutes, oneTime, User.Identity?.Name ?? "admin");
        await _qrStore.SaveTokenAsync(payload);

        _audit.Push(building, "qr_access_generate", $"رابط دخول مؤقت — شقة {apt.Number} ({minutes} دقيقة)", "admin", User.Identity?.Name ?? "admin", apt.Number.ToString());
        await _buildings.SaveFullAsync(building);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        ViewBag.Link = $"{baseUrl}/QrAccess/Enter?token={token}";
        ViewBag.ExpiresAt = expiresAt;
        ViewBag.Apt = apt;
        ViewBag.Building = building;
        return View("AccessLinkResult");
    }
}
