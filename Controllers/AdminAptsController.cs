using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "admin")]
public class AdminAptsController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly QrSecurityService _qrSecurity;
    private readonly QrGeneratorService _qrGen;
    private readonly WhatsAppTemplateService _waTemplates;
    private readonly WalletService _wallet;
    private readonly NotificationsService _notify;
    private readonly IAuditLogger _auditLogger;

    public AdminAptsController(
        BuildingsService buildings,
        QrSecurityService qrSecurity,
        QrGeneratorService qrGen,
        WhatsAppTemplateService waTemplates,
        WalletService wallet,
        NotificationsService notify,
        IAuditLogger auditLogger)
    {
        _buildings = buildings;
        _qrSecurity = qrSecurity;
        _qrGen = qrGen;
        _waTemplates = waTemplates;
        _wallet = wallet;
        _notify = notify;
        _auditLogger = auditLogger;
    }

    private string BuildingId => User.FindFirst("buildingId")!.Value;

    private string CurrentUserId =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    // ============================================================
    // عرض الشقق
    // ============================================================
    public async Task<IActionResult> Index()
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        return View(building);
    }

    // ============================================================
    // توليد QR سريع
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateQuickQr(
        string aptId, int validityMinutes, bool oneTime, int maxUses)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return NotFound();

        var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);

        var options = new QuickQrOptions
        {
            BuildingId = building.Id,
            AptId = aptId,
            AptNumber = apt.Number,
            FloorOrder = floor?.Order ?? 0,
            Validity = TimeSpan.FromMinutes(validityMinutes),
            OneTime = oneTime,
            MaxUses = oneTime ? 1 : maxUses
        };

        var token = _qrSecurity.GenerateQuickToken(options);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var quickUrl = $"{baseUrl}/Account/QuickLogin?token={token}";

        return Json(new
        {
            success = true,
            token,
            url = quickUrl,
            expiresIn = validityMinutes,
            oneTime,
            maxUses = options.MaxUses
        });
    }

    // ============================================================
    // رسالة ترحيب + QR
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateWelcome(string aptId, int validityHours)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return NotFound();

        var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var hours = validityHours <= 0 ? 24 : validityHours;

        var options = new QuickQrOptions
        {
            BuildingId = building.Id,
            AptId = apt.Id,
            AptNumber = apt.Number,
            FloorOrder = floor?.Order ?? 0,
            Validity = TimeSpan.FromHours(hours),
            OneTime = false,
            MaxUses = 5
        };

        var token = _qrSecurity.GenerateQuickToken(options);
        var quickUrl = $"{baseUrl}/Account/QuickLogin?token={token}";

        var extra = new Dictionary<string, string>
        {
            ["quick_link"] = quickUrl,
            ["login_url"] = WhatsAppTemplateService.BuildLoginUrl(baseUrl)
        };

        var messageText = _waTemplates.Render("welcome", building, apt, extra);
        var waLink = WhatsAppTemplateService.BuildWaLink(apt.Phone, messageText);

        var qrPng = _qrGen.GeneratePng(quickUrl, 10);
        var qrBase64 = Convert.ToBase64String(qrPng);

        ViewBag.Building = building;
        ViewBag.Apt = apt;
        ViewBag.Floor = floor;
        ViewBag.QuickUrl = quickUrl;
        ViewBag.MessageText = messageText;
        ViewBag.WhatsAppLink = waLink;
        ViewBag.QrBase64 = qrBase64;
        ViewBag.ValidityHours = hours;

        return View("WelcomeMessage");
    }

    // ============================================================
    // ✅ فتح شقة مغلقة
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenApartment(string aptId)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return NotFound();

        if (!apt.Closed)
        {
            TempData["Error"] = "الشقة مفتوحة بالفعل";
            return RedirectToAction("Index", "AdminHome");
        }

        // 1. افتح الشقة
        apt.Closed = false;
        apt.OpenDate = DateTime.UtcNow.ToString("o");
        apt.CloseDate = null;

        // 2. إعادة حساب التوزيع للشهر الحالي
        var currentMonth = WalletService.CurrentMonthKey();
        _wallet.RecalculateDistribution(building, currentMonth);
        _wallet.RecalculateRevenueDistribution(building, currentMonth);

        // 3. إشعار للساكن
        _notify.NotifyResidentApartmentReopened(building, apt, null);

        // 4. حفظ
        await _buildings.SaveFullAsync(building);

        // 5. Audit — في auditLogs collection ✅
        await _auditLogger.LogAsync(
            action: "apartment.opened",
            buildingId: building.Id,
            apartmentId: apt.Id,
            userId: CurrentUserId,
            userRole: "admin",
            metadata: new
            {
                aptNumber = apt.Number,
                aptOwner = apt.Owner
            },
            severity: "warning");

        TempData["Message"] = $"✅ تم فتح شقة {apt.Number} — الساكن هيتنبّه";
        return RedirectToAction("Index", "AdminHome");
    }

    // ============================================================
    // ✅ إغلاق شقة
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseApartment(string aptId, string? reason)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return NotFound();

        if (apt.Closed)
        {
            TempData["Error"] = "الشقة مغلقة بالفعل";
            return RedirectToAction("Index", "AdminHome");
        }

        // 1. أغلق الشقة
        apt.Closed = true;
        apt.CloseDate = DateTime.UtcNow.ToString("o");

        // 2. سجّل السبب في الـ Notes
        if (!string.IsNullOrWhiteSpace(reason))
        {
            apt.Notes = string.IsNullOrWhiteSpace(apt.Notes)
                ? reason
                : $"{apt.Notes}\n{reason}";
        }

        // 3. إعادة حساب التوزيع
        var currentMonth = WalletService.CurrentMonthKey();
        _wallet.RecalculateDistribution(building, currentMonth);
        _wallet.RecalculateRevenueDistribution(building, currentMonth);

        // 4. إشعار للساكن
        _notify.NotifyResidentApartmentClosed(building, apt, reason);

        // 5. حفظ
        await _buildings.SaveFullAsync(building);

        // 6. Audit — في auditLogs collection ✅
        await _auditLogger.LogAsync(
            action: "apartment.closed",
            buildingId: building.Id,
            apartmentId: apt.Id,
            userId: CurrentUserId,
            userRole: "admin",
            metadata: new
            {
                aptNumber = apt.Number,
                aptOwner = apt.Owner,
                reason = reason ?? ""
            },
            severity: "warning");

        TempData["Message"] = $"✅ تم إغلاق شقة {apt.Number}";
        return RedirectToAction("Index", "AdminHome");
    }
}