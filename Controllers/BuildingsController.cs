using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin")]
public class BuildingsController : Controller
{
    private readonly BuildingsService _service;
    public BuildingsController(BuildingsService service) => _service = service;

    public async Task<IActionResult> Index()
    {
        var buildings = await _service.GetAllAsync();
        return View(buildings);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string name,
        string? buildingNumber,
        string adminPin,
        string adminWhatsapp,
        string? logoUrl,
        bool addDefaultExpenseCategories = false,
        bool addDefaultRevenueCategories = false)
    {
        // ========== Validation ==========
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 100)
        {
            ModelState.AddModelError("", "اسم العمارة مطلوب (من 2 إلى 100 حرف)");
            return View();
        }

        if (string.IsNullOrWhiteSpace(adminPin) || adminPin.Length != 4 || !adminPin.All(char.IsDigit))
        {
            ModelState.AddModelError("", "PIN الأدمن لازم يكون 4 أرقام بالظبط");
            return View();
        }

        if (string.IsNullOrWhiteSpace(adminWhatsapp))
        {
            ModelState.AddModelError("", "رقم واتساب الأدمن مطلوب");
            return View();
        }

        var phoneClean = adminWhatsapp.Replace("+", "").Trim();
        if (!phoneClean.All(char.IsDigit) || phoneClean.Length < 7)
        {
            ModelState.AddModelError("", "رقم واتساب الأدمن غير صحيح (أرقام فقط، 7 خانات على الأقل)");
            return View();
        }

        if (!string.IsNullOrWhiteSpace(buildingNumber))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(buildingNumber.Trim(), @"^BLD-\d{3}$"))
            {
                ModelState.AddModelError("", "رقم العمارة لازم يكون بصيغة BLD-XXX (مثال: BLD-001)");
                return View();
            }
        }

        // ========== إنشاء العمارة ==========
        var (id, number) = await _service.CreateAsync(
            name.Trim(),
            adminPin.Trim(),
            adminWhatsapp.Trim(),
            logoUrl?.Trim(),
            buildingNumber?.Trim(),
            addDefaultExpenseCategories,
            addDefaultRevenueCategories
        );

        TempData["Message"] = $"✅ تم إنشاء العمارة {number} بنجاح";
        return RedirectToAction("Details", new { id });
    }

    public async Task<IActionResult> Details(string id)
    {
        var building = await _service.GetByIdAsync(id);
        if (building == null) return NotFound();
        return View(building);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return RedirectToAction("Index");
    }

    // إضافة دور جديد + شققه
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFloor(string buildingId, string label, string aptPhones, string aptPins)
    {
        var phones = (aptPhones ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var pins = (aptPins ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var count = Math.Min(phones.Length, pins.Length);
        var apts = new List<(string, string)>();
        for (var i = 0; i < count; i++) apts.Add((phones[i], pins[i]));

        if (apts.Count == 0)
        {
            TempData["Error"] = "لازم تدخل رقم واتساب و PIN لكل شقة (مفصولين بفاصلة)";
            return RedirectToAction("Details", new { id = buildingId });
        }

        await _service.AddFloorAsync(buildingId, label, apts);
        TempData["Message"] = "تمت إضافة الدور بنجاح";
        return RedirectToAction("Details", new { id = buildingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddApartment(string buildingId, string floorId, string phone, string pin)
    {
        if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
        {
            TempData["Error"] = "رقم الواتساب و PIN (4 أرقام على الأقل) مطلوبين";
            return RedirectToAction("Details", new { id = buildingId });
        }

        await _service.AddApartmentAsync(buildingId, floorId, phone, pin);
        TempData["Message"] = "تمت إضافة الشقة بنجاح";
        return RedirectToAction("Details", new { id = buildingId });
    }
}