
using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;
// =====================================================================
//  AdminResidentsController — إدارة الساكنين
//  متاح للأدمن (في عمارته بس) والـ SuperAdmin (كل العمارات)
// =====================================================================
/// <summary>
/// Admin screens for managing the residents of the current building (create, edit, enable/disable, delete, PIN reset).
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>admin,superadmin</c>.
/// </remarks>
[Authorize(Roles = "admin,superadmin")]
[AdminPermission(AdminPermissions.ManageResidents)]
public class AdminResidentsController : Controller
{
    private readonly ResidentsService _residents;
    private readonly BuildingsService _buildings;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminResidentsController"/> class.
    /// </summary>
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

    /// <summary>
    /// Shows the main page of the admin residents screen.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index(string? buildingId, string? search)
    {
        // SuperAdmin: يختار العمارة
        if (IsSuperAdmin && string.IsNullOrWhiteSpace(buildingId))
        {
            var allBuildings = await _buildings.GetSummariesAsync();
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

    /// <summary>
    /// Shows the details page of the admin residents screen.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
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

    /// <summary>
    /// Creates the admin residents create page.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
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

    /// <summary>
    /// Handles the create form submission of the admin residents screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
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
        catch (Exception)
        {
            TempData["Error"] = Loc.T("Creation_Failed");

            var building = await _buildings.GetByIdAsync(buildingId);
            ViewBag.Building = building;
            ViewBag.ApartmentId = apartmentId;
            return View();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Edit (GET)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Edits the admin residents edit page.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
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

    /// <summary>
    /// Handles the edit form submission of the admin residents screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
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
        catch (Exception)
        {
            TempData["Error"] = Loc.T("Update_Failed");

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

    /// <summary>
    /// Changes the PIN.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
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
        catch (Exception)
        {
            TempData["Error"] = Loc.T("PIN_Change_Failed");
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    // ═══════════════════════════════════════════════════════════
    // Disable / Enable
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Handles the disable form submission of the admin residents screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
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

    /// <summary>
    /// Handles the enable form submission of the admin residents screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
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

    /// <summary>
    /// Handles the delete form submission of the admin residents screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
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

    /// <summary>
    /// Migrates the admin residents data.
    /// </summary>
    /// <remarks>
    /// HTTP GET. Requires one of the roles <c>superadmin</c>.
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Migrate()
    {
        var buildings = await _buildings.GetSummariesWithCountsAsync();
        ViewBag.Buildings = buildings;
        ViewBag.TotalResidents = await _residents.CountAsync();

        return View();
    }

    // ═══════════════════════════════════════════════════════════
    // Migration (SuperAdmin only)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Runs the migration.
    /// </summary>
    /// <remarks>
    /// HTTP POST. Requires one of the roles <c>superadmin</c>.
    /// </remarks>
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
                TempData["Success"] = Loc.T("Migration_DryRun_N", result.EntriesMigrated);
            }
            else
            {
                TempData["Success"] = Loc.T("Migration_Residents_Done_N", result.EntriesMigrated);
            }

            if (result.EntriesFailed > 0)
            {
                TempData["Error"] = Loc.T("Migration_Residents_Failed_N", result.EntriesFailed);
            }

            // ✅ ارجع لصفحة Sync
            return RedirectToAction("Index", "Sync");
        }
        catch (Exception)
        {
            TempData["Error"] = Loc.T("Migration_Failed");
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
