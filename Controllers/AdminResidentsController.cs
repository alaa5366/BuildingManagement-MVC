// =====================================================================
//  AdminResidentsController — إدارة الساكنين
//  متاح للأدمن (في عمارته بس) والـ SuperAdmin (كل العمارات)
// =====================================================================
using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "admin,superadmin")]
public class AdminResidentsController : Controller
{
    private readonly ResidentsService _residents;
    private readonly BuildingsService _buildings;

    public AdminResidentsController(
        ResidentsService residents,
        BuildingsService buildings)
    {
        _residents = residents;
        _buildings = buildings;
    }

    private string CurrentUserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    private string? CurrentBuildingId =>
        User.FindFirst("buildingId")?.Value;

    private bool IsSuperAdmin => User.IsInRole("superadmin");

    // ═══════════════════════════════════════════════════════════
    // Index — قائمة الساكنين
    // ═══════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Index(string? buildingId, string? search)
    {
        // SuperAdmin: يختار العمارة
        if (IsSuperAdmin && string.IsNullOrWhiteSpace(buildingId))
        {
            var allBuildings = await _buildings.GetAllAsync();
            return View("ChooseBuilding", allBuildings);
        }

        var bid = IsSuperAdmin ? buildingId! : CurrentBuildingId!;
        if (string.IsNullOrWhiteSpace(bid))
            return Forbid();

        var building = await _buildings.GetByIdAsync(bid);
        if (building == null) return NotFound();

        var residents = await _residents.GetByBuildingAsync(bid);

        // فلترة بالبحث
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            residents = residents
                .Where(r =>
                    r.Name.ToLowerInvariant().Contains(s) ||
                    r.Phone.Contains(s) ||
                    r.Whatsapp.Contains(s))
                .ToList();
        }

        ViewBag.Building = building;
        ViewBag.BuildingId = bid;
        ViewBag.Search = search;
        ViewBag.CanManage = true;

        return View(residents);
    }

    // ═══════════════════════════════════════════════════════════
    // Details — تفاصيل ساكن
    // ═══════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var resident = await _residents.GetByIdAsync(id);
        if (resident == null) return NotFound();

        if (!CanAccessBuilding(resident.BuildingId))
            return Forbid();

        var building = await _buildings.GetByIdAsync(resident.BuildingId);
        var apt = building?.Apartments.FirstOrDefault(a => a.Id == resident.ApartmentId);

        ViewBag.Building = building;
        ViewBag.Apartment = apt;

        return View(resident);
    }

    // ═══════════════════════════════════════════════════════════
    // Create (GET)
    // ═══════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Create(string? buildingId, string? apartmentId)
    {
        var bid = IsSuperAdmin ? buildingId : CurrentBuildingId;
        if (string.IsNullOrWhiteSpace(bid)) return Forbid();

        var building = await _buildings.GetByIdAsync(bid!);
        if (building == null) return NotFound();

        ViewBag.Building = building;
        ViewBag.ApartmentId = apartmentId;

        return View();
    }

    // ═══════════════════════════════════════════════════════════
    // Create (POST)
    // ═══════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string buildingId,
        string apartmentId,
        string name,
        string phone,
        string whatsapp,
        string pin,
        string? email,
        bool isPrimary = true,
        bool isOwner = true)
    {
        if (!CanAccessBuilding(buildingId))
            return Forbid();

        try
        {
            await _residents.CreateAsync(
                buildingId, apartmentId,
                name, phone, whatsapp, pin, email,
                isPrimary, isOwner,
                CurrentUserId);

            TempData["Message"] = Loc.T("Resident_Created_Successfully");
            return RedirectToAction(nameof(Index), new { buildingId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = Loc.T("Creation_Failed") + ex.Message;

            var building = await _buildings.GetByIdAsync(buildingId);
            ViewBag.Building = building;
            ViewBag.ApartmentId = apartmentId;
            return View();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Edit (GET)
    // ═══════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var resident = await _residents.GetByIdAsync(id);
        if (resident == null) return NotFound();

        if (!CanAccessBuilding(resident.BuildingId))
            return Forbid();

        var building = await _buildings.GetByIdAsync(resident.BuildingId);
        var apt = building?.Apartments.FirstOrDefault(a => a.Id == resident.ApartmentId);

        ViewBag.Building = building;
        ViewBag.Apartment = apt;

        return View(resident);
    }

    // ═══════════════════════════════════════════════════════════
    // Edit (POST)
    // ═══════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id,
        string name,
        string phone,
        string whatsapp,
        string? email,
        bool isPrimary,
        bool isOwner)
    {
        var resident = await _residents.GetByIdAsync(id);
        if (resident == null) return NotFound();

        if (!CanAccessBuilding(resident.BuildingId))
            return Forbid();

        try
        {
            await _residents.UpdateAsync(
                id, name, phone, whatsapp, email,
                isPrimary, isOwner, CurrentUserId);

            TempData["Message"] = Loc.T("Resident_Updated_Successfully");
            return RedirectToAction(nameof(Index), new { buildingId = resident.BuildingId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = Loc.T("Update_Failed") + ex.Message;

            var building = await _buildings.GetByIdAsync(resident.BuildingId);
            var apt = building?.Apartments.FirstOrDefault(a => a.Id == resident.ApartmentId);

            ViewBag.Building = building;
            ViewBag.Apartment = apt;
            return View(resident);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ChangePin
    // ═══════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePin(string id, string newPin)
    {
        var resident = await _residents.GetByIdAsync(id);
        if (resident == null) return NotFound();

        if (!CanAccessBuilding(resident.BuildingId))
            return Forbid();

        try
        {
            await _residents.ChangePinAsync(id, newPin, CurrentUserId);
            TempData["Message"] = Loc.T("PIN_Changed");
        }
        catch (Exception ex)
        {
            TempData["Error"] = Loc.T("PIN_Change_Failed") + ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    // ═══════════════════════════════════════════════════════════
    // Disable / Enable
    // ═══════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable(string id, string? reason)
    {
        var resident = await _residents.GetByIdAsync(id);
        if (resident == null) return NotFound();

        if (!CanAccessBuilding(resident.BuildingId))
            return Forbid();

        await _residents.SetDisabledAsync(id, true, reason, CurrentUserId);
        TempData["Message"] = Loc.T("Resident_Disabled_Successfully");

        return RedirectToAction(nameof(Index), new { buildingId = resident.BuildingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable(string id, string? reason)
    {
        var resident = await _residents.GetByIdAsync(id);
        if (resident == null) return NotFound();

        if (!CanAccessBuilding(resident.BuildingId))
            return Forbid();

        await _residents.SetDisabledAsync(id, false, reason, CurrentUserId);
        TempData["Message"] = Loc.T("Resident_Enabled_Successfully");

        return RedirectToAction(nameof(Index), new { buildingId = resident.BuildingId });
    }

    // ═══════════════════════════════════════════════════════════
    // Delete
    // ═══════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var resident = await _residents.GetByIdAsync(id);
        if (resident == null) return NotFound();

        if (!CanAccessBuilding(resident.BuildingId))
            return Forbid();

        await _residents.DeleteAsync(id, CurrentUserId);
        TempData["Message"] = Loc.T("Resident_Deleted_Successfully");

        return RedirectToAction(nameof(Index), new { buildingId = resident.BuildingId });
    }

    // ═══════════════════════════════════════════════════════════
    // Migration (SuperAdmin only)
    // ═══════════════════════════════════════════════════════════

    [HttpGet]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Migrate()
    {
        var buildings = await _buildings.GetAllAsync();
        ViewBag.Buildings = buildings;
        ViewBag.TotalResidents = await _residents.CountAsync();

        return View();
    }

    // ═══════════════════════════════════════════════════════════
    // Migration (SuperAdmin only)
    // ═══════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> RunMigration(string? buildingId, string? dryRun)
    {
        var isDryRun = !string.IsNullOrEmpty(dryRun)
            && dryRun.Equals("true", StringComparison.OrdinalIgnoreCase);

        try
        {
            var result = await _residents.MigrateFromApartmentsAsync(buildingId, isDryRun);

            // ✅ رسالة نجاح
            if (isDryRun)
            {
                TempData["Success"] = $"🔍 Dry Run: {result.EntriesMigrated} ساكن هيترحّلوا";
            }
            else
            {
                TempData["Success"] = $"✅ تم ترحيل {result.EntriesMigrated} ساكن بنجاح";
            }

            if (result.EntriesFailed > 0)
            {
                TempData["Error"] = $"⚠️ فشل: {result.EntriesFailed} ساكن";
            }

            // ✅ ارجع لصفحة Sync
            return RedirectToAction("Index", "Sync");
        }
        catch (Exception ex)
        {
            TempData["Error"] = Loc.T("Migration_Failed") + ex.Message;
            return RedirectToAction("Index", "Sync");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════

    private bool CanAccessBuilding(string buildingId)
    {
        if (IsSuperAdmin) return true;
        return CurrentBuildingId == buildingId;
    }
}