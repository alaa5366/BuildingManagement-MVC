using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  AdminAptsController	إدارة الشقق للأدمن (فتح/إغلاق/تعطيل/تعديل)
// ============================================================

[Authorize(Roles = "admin")]
[AdminPermission(AdminPermissions.ManageResidents)]
public class AdminAptsController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly ResidentsService _residents;
    private readonly QrSecurityService _qrSecurity;
    private readonly QrGeneratorService _qrGen;
    private readonly WhatsAppTemplateService _waTemplates;
    private readonly WalletService _wallet;
    private readonly NotificationsService _notify;
    private readonly IAuditLogger _auditLogger;

    public AdminAptsController(
        BuildingsService buildings,
        ResidentsService residents,
        QrSecurityService qrSecurity,
        QrGeneratorService qrGen,
        WhatsAppTemplateService waTemplates,
        WalletService wallet,
        NotificationsService notify,
        IAuditLogger auditLogger)
    {
        _buildings = buildings;
        _residents = residents;
        _qrSecurity = qrSecurity;
        _qrGen = qrGen;
        _waTemplates = waTemplates;
        _wallet = wallet;
        _notify = notify;
        _auditLogger = auditLogger;
    }

    private string BuildingId => User.FindFirst("buildingId")!.Value;

    private string CurrentUserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    // ============================================================
    // 1. Index
    // ============================================================
    public async Task<IActionResult> Index()
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        return View(building);
    }

    // ============================================================
    // 2. GenerateQuickQr
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
    // 3. GenerateWelcome
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

        // رابط تعيين الـ PIN (مرة واحدة) — بدل ما نبعت الـ PIN نفسه
        var setPinToken = _qrSecurity.GenerateQuickToken(new QuickQrOptions
        {
            BuildingId = building.Id,
            AptId = apt.Id,
            AptNumber = apt.Number,
            FloorOrder = floor?.Order ?? 0,
            Validity = TimeSpan.FromHours(hours),
            OneTime = true,
            MaxUses = 1,
            Purpose = "setpin"
        });
        var setPinUrl = $"{baseUrl}/Account/SetPin?token={setPinToken}";

        var extra = new Dictionary<string, string>
        {
            ["setpin_link"] = setPinUrl,
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
    // 4. OpenApartment
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
            TempData["Error"] = Loc.T("The_Apartment_Is_Already_Open");
            return RedirectToAction("Index", "AdminHome");
        }

        apt.Closed = false;
        apt.OpenDate = DateTime.UtcNow.ToString("o");
        apt.CloseDate = null;

        var currentMonth = WalletService.CurrentMonthKey();
        _wallet.RecalculateDistribution(building, currentMonth);
        _wallet.RecalculateRevenueDistribution(building, currentMonth);

        _notify.NotifyResidentApartmentReopened(building, apt, null);

        await _buildings.SaveFullAsync(building);

        await _auditLogger.LogAsync(
            action: "apartment.opened",
            buildingId: building.Id,
            apartmentId: apt.Id,
            userId: CurrentUserId,
            userRole: "admin",
            metadata: new { aptNumber = apt.Number, aptOwner = apt.Owner },
            severity: "warning");

        TempData["Message"] = Loc.T("Apartment_N_Was_Opened_The_Resident", apt.Number);
        return RedirectToAction("Index", "AdminHome");
    }

    // ============================================================
    // 5. CloseApartment
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
            TempData["Error"] = Loc.T("The_Apartment_Is_Already_Closed");
            return RedirectToAction("Index", "AdminHome");
        }

        apt.Closed = true;
        apt.CloseDate = DateTime.UtcNow.ToString("o");

        if (!string.IsNullOrWhiteSpace(reason))
        {
            apt.Notes = string.IsNullOrWhiteSpace(apt.Notes)
                ? reason
                : $"{apt.Notes}\n{reason}";
        }

        var currentMonth = WalletService.CurrentMonthKey();
        _wallet.RecalculateDistribution(building, currentMonth);
        _wallet.RecalculateRevenueDistribution(building, currentMonth);

        _notify.NotifyResidentApartmentClosed(building, apt, reason);

        await _buildings.SaveFullAsync(building);

        await _auditLogger.LogAsync(
            action: "apartment.closed",
            buildingId: building.Id,
            apartmentId: apt.Id,
            userId: CurrentUserId,
            userRole: "admin",
            metadata: new { aptNumber = apt.Number, aptOwner = apt.Owner, reason = reason ?? "" },
            severity: "warning");

        TempData["Message"] = Loc.T("Apartment_N_Was_Closed", apt.Number);
        return RedirectToAction("Index", "AdminHome");
    }

    // ============================================================
    // 6. DisableApartment
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableApartment(string aptId, string? reason)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return NotFound();

        if (apt.Disabled)
        {
            TempData["Error"] = Loc.T("The_Apartment_Is_Already_Disabled");
            return RedirectToAction("Index", "AdminHome");
        }

        apt.Disabled = true;
        apt.DisabledReason = reason ?? "";

        _notify.NotifyResidentApartmentDisabled(building, apt, reason);

        await _buildings.SaveFullAsync(building);

        await _auditLogger.LogAsync(
            action: "apartment.disabled",
            buildingId: building.Id,
            apartmentId: apt.Id,
            userId: CurrentUserId,
            userRole: "admin",
            metadata: new { aptNumber = apt.Number, aptOwner = apt.Owner, reason = reason ?? "" },
            severity: "warning");

        TempData["Message"] = Loc.T("Apartment_N_Was_Disabled", apt.Number);
        return RedirectToAction("Index", "AdminHome");
    }

    // ============================================================
    // 7. EnableApartment
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableApartment(string aptId, string? reason)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return NotFound();

        if (!apt.Disabled)
        {
            TempData["Error"] = Loc.T("The_Apartment_Is_Already_Enabled");
            return RedirectToAction("Index", "AdminHome");
        }

        apt.Disabled = false;
        apt.DisabledReason = "";

        _notify.NotifyResidentApartmentEnabled(building, apt, reason);

        await _buildings.SaveFullAsync(building);

        await _auditLogger.LogAsync(
            action: "apartment.enabled",
            buildingId: building.Id,
            apartmentId: apt.Id,
            userId: CurrentUserId,
            userRole: "admin",
            metadata: new { aptNumber = apt.Number, aptOwner = apt.Owner, reason = reason ?? "" },
            severity: "info");

        TempData["Message"] = Loc.T("Apartment_N_Was_Enabled", apt.Number);
        return RedirectToAction("Index", "AdminHome");
    }

    // ============================================================
    // 8. ✅ صفحة تعديل الشقة (GET) — هذه هي الدالة المفقودة
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Edit(string aptId)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return NotFound();

        var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);

        ViewBag.Building = building;
        ViewBag.Floor = floor;

        return View(apt);
    }

    // ============================================================
    // 9. صفحة تعديل الشقة (POST)
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string aptId,
        string owner,
        string label,
        string phone,
        string? pin,
        double monthlyFee,
        string? notes,
        string? email)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return NotFound();

        if (string.IsNullOrWhiteSpace(owner))
        {
            TempData["Error"] = Loc.T("All_Fields_Are_Required");
            return RedirectToAction(nameof(Edit), new { aptId });
        }

        // الـ PIN اختياري: فاضي = يفضل زي ما هو، 4 أرقام = إعادة تعيين
        if (!string.IsNullOrWhiteSpace(pin) && (pin.Length != 4 || !pin.All(char.IsDigit)))
        {
            TempData["Error"] = Loc.T("PIN_Must_Be_Exactly_4_Digits");
            return RedirectToAction(nameof(Edit), new { aptId });
        }

        try
        {
            // 1. تحديث جدول الشقق
            await _buildings.UpdateApartmentAsync(
                BuildingId, aptId,
                owner, label ?? "", phone ?? "", "",
                monthlyFee, notes ?? "", email);

            // 2. ✅ تحديث جدول السكان باستخدام الدالة المركزية
            if (!string.IsNullOrWhiteSpace(pin))
                await _residents.SetApartmentPinAsync(BuildingId, aptId, pin, CurrentUserId);

            await _auditLogger.LogAsync(
                action: "apartment.updated",
                buildingId: building.Id,
                apartmentId: aptId,
                userId: CurrentUserId,
                userRole: "admin",
                metadata: new { aptNumber = apt.Number, aptOwner = owner },
                severity: "info");

            TempData["Message"] = Loc.T("Updated");
        }
        catch (Exception ex)
        {
            TempData["Error"] = Loc.T("Update_Failed") + ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { aptId });
    }
}