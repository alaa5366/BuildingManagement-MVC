using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  BuildingsController	إدارة العمارات (SuperAdmin)
// ============================================================
[Authorize]
public class BuildingsController : Controller
{
    private readonly BuildingsService _service;
    private readonly QrCodePdfService _qrPdf;

    public BuildingsController(BuildingsService service, QrCodePdfService qrPdf)
    {
        _service = service;
        _qrPdf = qrPdf;
    }

    // ═══════════════════════════════════════════════════════════
    // Index — SuperAdmin فقط
    // ═══════════════════════════════════════════════════════════
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        var allBuildings = await _service.GetSummariesAsync();
        var paged = BuildingManagementMvc.Models.PagedResult<BuildingManagementMvc.Models.Building>
            .Create(allBuildings, page, pageSize);

        ViewBag.RouteValues = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString()
        };

        return View(paged);
    }

    // ═══════════════════════════════════════════════════════════
    // Create — SuperAdmin فقط
    // ═══════════════════════════════════════════════════════════
    [HttpGet]
    [Authorize(Roles = "superadmin")]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Create(
        string name,
        string? buildingNumber,
        string adminPin,
        string adminWhatsapp,
        string? logoUrl,
        bool addDefaultExpenseCategories = false,
        bool addDefaultRevenueCategories = false)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 100)
        {
            ModelState.AddModelError("", Loc.T("Building_Name_Is_Required_2_To"));
            return View();
        }

        if (string.IsNullOrWhiteSpace(adminPin) || adminPin.Length != 4 || !adminPin.All(char.IsDigit))
        {
            ModelState.AddModelError("", Loc.T("Admin_PIN_Must_Be_Exactly_4"));
            return View();
        }

        if (string.IsNullOrWhiteSpace(adminWhatsapp))
        {
            ModelState.AddModelError("", Loc.T("Admin_WhatsApp_Number_Is_Required"));
            return View();
        }

        var phoneClean = adminWhatsapp.Replace("+", "").Trim();
        if (!phoneClean.All(char.IsDigit) || phoneClean.Length < 7)
        {
            ModelState.AddModelError("", Loc.T("Admin_WhatsApp_Number_Is_Invalid_Digits"));
            return View();
        }

        if (!string.IsNullOrWhiteSpace(buildingNumber))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(buildingNumber.Trim(), @"^BLD-\d{3}$"))
            {
                ModelState.AddModelError("", Loc.T("Building_Number_Must_Be_In_The"));
                return View();
            }
        }

        var (id, number) = await _service.CreateAsync(
            name.Trim(),
            adminPin.Trim(),
            adminWhatsapp.Trim(),
            logoUrl?.Trim(),
            buildingNumber?.Trim(),
            addDefaultExpenseCategories,
            addDefaultRevenueCategories
        );

        TempData["Message"] = Loc.T("Building_N_Created_Successfully", number);
        return RedirectToAction("Details", new { id });
    }

    // ═══════════════════════════════════════════════════════════
    // Details — SuperAdmin + Admin
    // ═══════════════════════════════════════════════════════════
    [Authorize(Roles = "superadmin,admin")]
    public async Task<IActionResult> Details(string id)
    {
        var building = await _service.GetByIdAsync(id);
        if (building == null) return NotFound();
        return View(building);
    }

    // ═══════════════════════════════════════════════════════════
    // QrCodes — SuperAdmin فقط
    // ═══════════════════════════════════════════════════════════
    [HttpGet]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> QrCodes(string id)
    {
        var building = await _service.GetByIdAsync(id);
        if (building == null) return NotFound();
        return View(building);
    }

    // ═══════════════════════════════════════════════════════════
    // QrCodesPdf — SuperAdmin فقط
    // ═══════════════════════════════════════════════════════════
    [HttpGet]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> QrCodesPdf(string id)
    {
        var building = await _service.GetByIdAsync(id);
        if (building == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var pdfBytes = _qrPdf.GenerateQrCodesPdf(building, baseUrl);
        var fileName = $"QR-Codes-{building.BuildingNumber}-{DateTime.Now:yyyyMMdd}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    // ═══════════════════════════════════════════════════════════
    // Delete — SuperAdmin فقط
    // ═══════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return RedirectToAction("Index");
    }

    // ═══════════════════════════════════════════════════════════
    // AddFloor — SuperAdmin فقط
    // ═══════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> AddFloor(string buildingId, string label, string aptPhones, string aptPins)
    {
        var phones = (aptPhones ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var pins = (aptPins ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var count = Math.Min(phones.Length, pins.Length);
        var apts = new List<(string, string)>();
        for (var i = 0; i < count; i++) apts.Add((phones[i], pins[i]));

        if (apts.Count == 0)
        {
            TempData["Error"] = Loc.T("You_Must_Enter_A_WhatsApp_Number");
            return RedirectToAction("Details", new { id = buildingId });
        }

        await _service.AddFloorAsync(buildingId, label, apts);
        TempData["Message"] = Loc.T("Floor_Added_Successfully");
        return RedirectToAction("Details", new { id = buildingId });
    }

    // ═══════════════════════════════════════════════════════════
    // AddApartment — SuperAdmin فقط
    // ═══════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> AddApartment(string buildingId, string floorId, string phone, string pin)
    {
        if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
        {
            TempData["Error"] = Loc.T("WhatsApp_Number_And_PIN_At_Least");
            return RedirectToAction("Details", new { id = buildingId });
        }

        await _service.AddApartmentAsync(buildingId, floorId, phone, pin);
        TempData["Message"] = Loc.T("Apartment_Added_Successfully");
        return RedirectToAction("Details", new { id = buildingId });
    }

    // ═══════════════════════════════════════════════════════════
    // GenerateQuickQr — SuperAdmin + Admin ✅
    // ═══════════════════════════════════════════════════════════
    [Authorize(Roles = "superadmin,admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateQuickQr(
        string buildingId, string aptId,
        int validityMinutes, bool oneTime, int maxUses,
        [FromServices] QrSecurityService qrSecurity)
    {
        var building = await _service.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return NotFound();

        var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);

        var options = new QuickQrOptions
        {
            BuildingId = buildingId,
            AptId = aptId,
            AptNumber = apt.Number,
            FloorOrder = floor?.Order ?? 0,
            Validity = TimeSpan.FromMinutes(validityMinutes),
            OneTime = oneTime,
            MaxUses = oneTime ? 1 : maxUses
        };

        var token = qrSecurity.GenerateQuickToken(options);
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
}