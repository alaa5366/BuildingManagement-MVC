
using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;
// =====================================================================
//  AdminResidentsController â€” ط¥ط¯ط§ط±ط© ط§ظ„ط³ط§ظƒظ†ظٹظ†
//  ظ…طھط§ط­ ظ„ظ„ط£ط¯ظ…ظ† (ظپظٹ ط¹ظ…ط§ط±طھظ‡ ط¨ط³) ظˆط§ظ„ظ€ SuperAdmin (ظƒظ„ ط§ظ„ط¹ظ…ط§ط±ط§طھ)
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

    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ
    // Index â€” ظ‚ط§ط¦ظ…ط© ط§ظ„ط³ط§ظƒظ†ظٹظ†
    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ

    /// <summary>
    /// Shows the main page of the admin residents screen.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index(string? buildingId, string? search)
    {
        // SuperAdmin: ظٹط®طھط§ط± ط§ظ„ط¹ظ…ط§ط±ط©
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

        // ظپظ„طھط±ط© ط¨ط§ظ„ط¨ط­ط«
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

    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ
    // Details â€” طھظپط§طµظٹظ„ ط³ط§ظƒظ†
    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ

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

    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ
    // Create (GET)
    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ

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
        ViewBag.RegistrationPersonId = Guid.NewGuid();
        ViewBag.ExistingPeople = await _residents.GetPeopleForRegistrationAsync(bid!, IsSuperAdmin);

        return View();
    }

    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ
    // Create (POST)
    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ

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
        string? email,
        Guid? personId = null,
        Guid? registrationPersonId = null,
        bool isPrimary = false,
        bool isOwner = false)
    {
        if (!CanAccessBuilding(buildingId))
            return Forbid();

        try
        {
            await _residents.CreateAsync(
                buildingId, apartmentId,
                name, phone, whatsapp, email,
                isPrimary, isOwner,
                CurrentUserId, personId, createLoginAccount: true, registrationPersonId: registrationPersonId, allowCrossBuildingReuse: IsSuperAdmin);

            TempData["Message"] = Loc.T("Resident_Created_Successfully");
            return RedirectToAction(nameof(Index), new { buildingId });
        }
        catch (Exception)
        {
            TempData["Error"] = Loc.T("Creation_Failed");

            var building = await _buildings.GetByIdAsync(buildingId);
            ViewBag.Building = building;
            ViewBag.ApartmentId = apartmentId;
            ViewBag.RegistrationPersonId = registrationPersonId ?? Guid.NewGuid();
            ViewBag.ExistingPeople = await _residents.GetPeopleForRegistrationAsync(buildingId, IsSuperAdmin);
            return View();
        }
    }

    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ
    // Edit (GET)
    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ

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

    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ
    // Edit (POST)
    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ

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

    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ
    // ChangePin
    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ

    /// <summary>Moves the resident to another apartment within the same building.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(string id, string targetApartmentId)
    {
        var resident = await _residents.GetByIdAsync(id);
        if (resident == null) return NotFound();

        if (!CanAccessBuilding(resident.BuildingId))
            return Forbid();

        try
        {
            var newOccupancyId = await _residents.MoveAsync(id, targetApartmentId, CurrentUserId);
            TempData["Message"] = Loc.T("Resident_Updated_Successfully");
            return RedirectToAction(nameof(Edit), new { id = newOccupancyId.ToString("D") });
        }
        catch (InvalidOperationException)
        {
            TempData["Error"] = Loc.T("Update_Failed");
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (ArgumentException)
        {
            TempData["Error"] = Loc.T("Update_Failed");
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    /// <summary>Handles the legacy direct PIN-change route, which redirects to verified account enrollment.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePin(string id)
    {
        var resident = await _residents.GetByIdAsync(id);
        if (resident == null) return NotFound();

        if (!CanAccessBuilding(resident.BuildingId))
            return Forbid();

        TempData["Error"] = "Direct PIN changes from the resident profile are disabled. Use the verified individual account enrollment workflow.";

        return RedirectToAction(nameof(Edit), new { id });
    }

    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ
    // Disable / Enable
    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ

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

    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ
    // Delete
    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ

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

    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ
    // Migration (SuperAdmin only)
    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ

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

    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ
    // Migration (SuperAdmin only)
    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ

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
            var result = await _residents.MigrateFromApartmentsAsync(buildingId, isDryRun, CurrentUserId);

            // âœ… ط±ط³ط§ظ„ط© ظ†ط¬ط§ط­
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

            // âœ… ط§ط±ط¬ط¹ ظ„طµظپط­ط© Sync
            return RedirectToAction("Index", "Sync");
        }
        catch (Exception)
        {
            TempData["Error"] = Loc.T("Migration_Failed");
            return RedirectToAction("Index", "Sync");
        }
    }

    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ
    // Helpers
    // â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گ

    private bool CanAccessBuilding(string buildingId)
    {
        if (IsSuperAdmin) return true;
        return CurrentBuildingId == buildingId;
    }
}
