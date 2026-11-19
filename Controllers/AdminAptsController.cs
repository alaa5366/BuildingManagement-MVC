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

/// <summary>
/// Admin screens for managing the apartments of the current building: listing, editing, opening/closing, enabling/disabling, welcome messages and quick-login QR codes.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>admin</c>.
/// </remarks>
[Authorize(Roles = "admin")]
[AdminPermission(AdminPermissions.ManageResidents)]
public class AdminAptsController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly ResidentsService _residents;
    private readonly QrSecurityService _qrSecurity;
    private readonly TargetQrService _targetQr;
    private readonly QrGeneratorService _qrGen;
    private readonly TargetWhatsAppService _targetWhatsApp;
    private readonly TargetNotificationsService _targetNotifications;
    private readonly Microsoft.Extensions.Logging.ILogger<AdminAptsController> _logger;
    private readonly IAuditLogger _auditLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminAptsController"/> class.
    /// </summary>
    public AdminAptsController(
        BuildingsService buildings,
        ResidentsService residents,
        QrSecurityService qrSecurity,
        TargetQrService targetQr,
        QrGeneratorService qrGen,
        TargetWhatsAppService targetWhatsApp,
        IAuditLogger auditLogger,
        TargetNotificationsService targetNotifications,
        Microsoft.Extensions.Logging.ILogger<AdminAptsController> logger)
    {
        _buildings = buildings;
        _residents = residents;
        _qrSecurity = qrSecurity;
        _targetQr = targetQr;
        _qrGen = qrGen;
        _targetWhatsApp = targetWhatsApp;
        _auditLogger = auditLogger;
        _targetNotifications = targetNotifications;
        _logger = logger;
    }

    private string BuildingId => User.FindFirst("buildingId")!.Value;

    private string CurrentUserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    // ============================================================
    // 1. Index
    // ============================================================
    /// <summary>
    /// Shows the main page of the admin apartments screen.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        return View(building);
    }

    // ============================================================
    // 2. GenerateQuickQr
    // ============================================================
    /// <summary>
    /// Generates the quick QR.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateQuickQr(
        string aptId, int validityMinutes, bool oneTime, int maxUses)
    {
        if (validityMinutes is < 1 or > 60 || (!oneTime && maxUses is < 1 or > 100))
            return BadRequest(new { error = "invalid-qr-options" });

        var apt = await _targetQr.GetApartmentAsync(
            BuildingId, aptId, requireActive: true, HttpContext.RequestAborted);
        if (apt is null) return NotFound();

        var options = new QuickQrOptions
        {
            BuildingId = apt.BuildingId,
            AptId = apt.ApartmentId,
            AptNumber = apt.ApartmentNumber,
            FloorOrder = apt.FloorOrder,
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
    // 3. GenerateRecoveryQr
    // ============================================================
    /// <summary>
    /// Creates a five-minute, single-use resident login QR for an administrator-approved recovery.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateRecoveryQr(string aptId)
    {
        var apt = await _targetQr.GetApartmentAsync(
            BuildingId, aptId, requireActive: true, HttpContext.RequestAborted);
        if (apt is null) return NotFound();

        var phone = AuthHelpers.NormalizePhone(apt.Phone);
        if (string.IsNullOrWhiteSpace(phone))
        {
            TempData["Error"] = "لا يوجد رقم هاتف مسجل للشقة.";
            return RedirectToAction(nameof(Index));
        }

        // Never send a recovery credential to a phone shared by another active apartment.
        var buildingPage = await _targetQr.GetBuildingPageAsync(
            BuildingId, HttpContext.RequestAborted);
        var duplicatePhone = buildingPage?.Apartments.Any(x =>
            x.ApartmentId != apt.ApartmentId &&
            string.Equals(AuthHelpers.NormalizePhone(x.Phone), phone, StringComparison.Ordinal)) == true;
        if (duplicatePhone)
        {
            TempData["Error"] = "رقم الهاتف مكرر في شقة أخرى؛ راجع بيانات الهاتف قبل إصدار رمز الدخول.";
            return RedirectToAction(nameof(Index));
        }

        var token = _qrSecurity.GenerateQuickToken(new QuickQrOptions
        {
            BuildingId = apt.BuildingId,
            AptId = apt.ApartmentId,
            AptNumber = apt.ApartmentNumber,
            FloorOrder = apt.FloorOrder,
            Validity = TimeSpan.FromMinutes(5),
            OneTime = true,
            MaxUses = 1,
            Purpose = "login"
        });

        var quickUrl = $"{Request.Scheme}://{Request.Host}/Account/QuickLogin?token={Uri.EscapeDataString(token)}";
        var message =
            $"مرحباً، هذا رابط دخول مؤقت لحساب شقتك رقم {apt.ApartmentNumber} في {apt.BuildingName}.\n" +
            "صالح لمدة 5 دقائق ويُستخدم مرة واحدة فقط. افتح الرابط ثم أكد الدخول.\n" +
            "إذا لم تطلب هذا الرابط، تجاهل الرسالة ولا تشاركها مع أي شخص.\n" +
            quickUrl;
        var waLink = WhatsAppTemplateService.BuildWaLink(phone, message);
        var qrBase64 = Convert.ToBase64String(_qrGen.GeneratePng(quickUrl, 12));

        await _auditLogger.LogAsync(
            action: "resident.recovery_qr_issued",
            buildingId: apt.BuildingId,
            apartmentId: apt.ApartmentId,
            userId: CurrentUserId,
            userRole: "admin",
            metadata: new { aptNumber = apt.ApartmentNumber, phoneSuffix = phone.Length > 4 ? phone[^4..] : phone, expiresInMinutes = 5, oneTime = true },
            severity: "warning");

        ViewBag.BuildingName = apt.BuildingName;
        ViewBag.ApartmentNumber = apt.ApartmentNumber;
        ViewBag.ApartmentPhone = apt.Phone;
        ViewBag.QuickUrl = quickUrl;
        ViewBag.WhatsAppLink = waLink;
        ViewBag.QrBase64 = qrBase64;
        ViewBag.ExpiresInMinutes = 5;
        return View("RecoveryQr");
    }

    // ============================================================
    // 3. GenerateWelcome
    // ============================================================
    /// <summary>
    /// Generates the welcome.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateWelcome(string aptId, int validityHours)
    {
        var apt = await _targetQr.GetApartmentAsync(
            BuildingId, aptId, requireActive: true, HttpContext.RequestAborted);
        if (apt is null) return NotFound();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var hours = validityHours <= 0 ? 24 : validityHours;
        if (hours > 72)
        {
            TempData["Error"] = "مدة رابط الترحيب يجب ألا تتجاوز 72 ساعة.";
            return RedirectToAction(nameof(Index));
        }

        var options = new QuickQrOptions
        {
            BuildingId = apt.BuildingId,
            AptId = apt.ApartmentId,
            AptNumber = apt.ApartmentNumber,
            FloorOrder = apt.FloorOrder,
            Validity = TimeSpan.FromHours(hours),
            OneTime = false,
            MaxUses = 5
        };

        var token = _qrSecurity.GenerateQuickToken(options);
        var quickUrl = $"{baseUrl}/Account/QuickLogin?token={token}";

        // رابط تعيين الـ PIN (مرة واحدة) — بدل ما نبعت الـ PIN نفسه
        var setPinToken = _qrSecurity.GenerateQuickToken(new QuickQrOptions
        {
            BuildingId = apt.BuildingId,
            AptId = apt.ApartmentId,
            AptNumber = apt.ApartmentNumber,
            FloorOrder = apt.FloorOrder,
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

        WhatsAppResultViewModel welcomeDraft;
        try
        {
            welcomeDraft = await _targetWhatsApp.GenerateAsync(
                BuildingId, apt.ApartmentId, "welcome", extra, baseUrl, HttpContext.RequestAborted)
                ?? throw new InvalidOperationException("تعذر العثور على بيانات الشقة في النظام المستهدف.");
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        var messageText = welcomeDraft.Message;
        var waLink = welcomeDraft.Link;

        var qrPng = _qrGen.GeneratePng(quickUrl, 10);
        var qrBase64 = Convert.ToBase64String(qrPng);

        ViewBag.BuildingName = apt.BuildingName;
        ViewBag.BuildingNumber = apt.BuildingNumber;
        ViewBag.ApartmentNumber = apt.ApartmentNumber;
        ViewBag.ApartmentPhone = apt.Phone;
        ViewBag.FloorLabel = apt.FloorLabel;
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
    /// <summary>
    /// Opens the apartment.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
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

        await _buildings.SaveFullAsync(building);
        await TryNotifyResidentAsync(apt.Id, "تم إعادة تفعيل شقتك",
            $"تم إعادة تفعيل شقتك رقم {apt.Number} في {building.Name}.",
            "apartment_reopened", "🔓");

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
    /// <summary>
    /// Closes the apartment.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
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

        await _buildings.SaveFullAsync(building);
        await TryNotifyResidentAsync(apt.Id, "تم إغلاق شقتك",
            $"تم إغلاق شقتك رقم {apt.Number} في {building.Name}. {reason}".Trim(),
            "apartment_closed", "🔒");

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
    /// <summary>
    /// Disables the apartment.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
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

        await _buildings.SaveFullAsync(building);
        await TryNotifyResidentAsync(apt.Id, "تم تعطيل حساب الشقة",
            $"تم تعطيل الشقة رقم {apt.Number} في {building.Name}. {reason}".Trim(),
            "apartment_disabled", "🚫");

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
    /// <summary>
    /// Enables the apartment.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
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

        await _buildings.SaveFullAsync(building);
        await TryNotifyResidentAsync(apt.Id, "تم إعادة تفعيل حساب الشقة",
            $"تم إعادة تفعيل الشقة رقم {apt.Number} في {building.Name}. {reason}".Trim(),
            "apartment_enabled", "✅");

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
    /// <summary>
    /// Edits the admin apartments edit page.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
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
    /// <summary>
    /// Handles the edit form submission of the admin apartments screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string aptId,
        string owner,
        string label,
        string phone,
        string? pin,
        decimal monthlyFee,
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
        catch (Exception)
        {
            TempData["Error"] = Loc.T("Update_Failed");
        }

        return RedirectToAction(nameof(Edit), new { aptId });
    }

    private async Task TryNotifyResidentAsync(string apartmentId, string title, string body, string type, string icon)
    {
        try
        {
            await _targetNotifications.CreateResidentDepositNoticeAsync(
                BuildingId, apartmentId, title, body, HttpContext.RequestAborted, type, icon);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apartment notification failed for building {BuildingId}, apartment {ApartmentId}.",
                BuildingId, apartmentId);
        }
    }
}