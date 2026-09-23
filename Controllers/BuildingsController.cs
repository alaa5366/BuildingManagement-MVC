using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

// إدارة العمارات — نفس شاشات sa-buildings.js و sa-building-view.js و structure.js
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
    public async Task<IActionResult> Create(string name, string adminPin)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("", "اسم العمارة مطلوب");
            return View();
        }

        var (id, number) = await _service.CreateAsync(name.Trim(), (adminPin ?? "").Trim());
        TempData["Message"] = $"تم إنشاء العمارة {number} بنجاح";
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

    // إضافة دور جديد + شققه (كل شقة برقم واتساب و PIN مفصولين بفاصلة)
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
