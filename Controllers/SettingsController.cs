using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  SettingsController	الإعدادات (Global + Building + Own)
// ============================================================
/// <summary>
/// Personal, building and global settings screens.
/// </summary>
/// <remarks>
/// Access: any signed-in user (roles are checked per action).
/// </remarks>
[Authorize]
public class SettingsController : Controller
{
    private readonly SettingsService _settings;
    private readonly BuildingsService _buildings;
    private readonly ILogger<SettingsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsController"/> class.
    /// </summary>
    public SettingsController(
        SettingsService settings,
        BuildingsService buildings,
        ILogger<SettingsController> logger)
    {
        _settings = settings;
        _buildings = buildings;
        _logger = logger;
    }

    private string CurrentUserId =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    private string CurrentRole =>
        User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

    private string? CurrentBuildingId => User.FindFirst("buildingId")?.Value;
    private string? CurrentApartmentId => User.FindFirst("apartmentId")?.Value;

    // ============================================================
    // Router — حسب الدور
    // ============================================================
    /// <summary>
    /// Shows the main page of the settings screen.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public IActionResult Index()
    {
        if (User.IsInRole("superadmin"))
            return RedirectToAction(nameof(Global));

        if (User.IsInRole("admin"))
            return RedirectToAction(nameof(Building));

        return RedirectToAction(nameof(Own));
    }

    // ============================================================
    // Global Settings (Super Admin)
    // ============================================================
    /// <summary>
    /// Shows the global settings page (super admin).
    /// </summary>
    /// <remarks>
    /// HTTP GET. Requires one of the roles <c>superadmin</c>. Requires the admin permission <c>AdminPermissions.ManageSettingsGlobal</c>.
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = "superadmin")]
    [HasPermission(AdminPermissions.ManageSettingsGlobal)]
    public async Task<IActionResult> Global()
    {
        var settings = await _settings.GetGlobalAsync();
        return View(settings);
    }

    /// <summary>
    /// Saves the global settings.
    /// </summary>
    /// <remarks>
    /// HTTP POST. Requires one of the roles <c>superadmin</c>. Requires the admin permission <c>AdminPermissions.ManageSettingsGlobal</c>.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "superadmin")]
    [HasPermission(AdminPermissions.ManageSettingsGlobal)]
    public async Task<IActionResult> Global(GlobalSettings model)
    {
        try
        {
            await _settings.SaveGlobalAsync(model, CurrentUserId);
            TempData["Message"] = Loc.T("Global_Settings_Saved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveGlobal failed");
            TempData["Error"] = Loc.T("Save_Failed_2");
        }
        return RedirectToAction(nameof(Global));
    }

    // ============================================================
    // Building Settings (Admin + Super Admin)
    // ============================================================
    /// <summary>
    /// Shows the settings page of a building.
    /// </summary>
    /// <remarks>
    /// HTTP GET. Requires one of the roles <c>superadmin,admin</c>.
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = "superadmin,admin")]
    [AdminPermission(AdminPermissions.ManageSettingsBuilding)]
    public async Task<IActionResult> Building(string? id)
    {
        string? buildingId;

        if (User.IsInRole("superadmin"))
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                ViewBag.Buildings = await _buildings.GetSummariesAsync();
                return View("ChooseBuilding");
            }
            buildingId = id;
        }
        else
        {
            buildingId = CurrentBuildingId;
        }

        if (string.IsNullOrWhiteSpace(buildingId))
            return Forbid();

        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        var settings = await _settings.GetBuildingAsync(buildingId);

        ViewBag.Building = building;
        ViewBag.BuildingId = buildingId;
        return View(settings);
    }

    /// <summary>
    /// Saves the settings of a building.
    /// </summary>
    /// <remarks>
    /// HTTP POST. Requires one of the roles <c>superadmin,admin</c>.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "superadmin,admin")]
    [AdminPermission(AdminPermissions.ManageSettingsBuilding)]
    public async Task<IActionResult> Building(string buildingId, BuildingSettings model)
    {
        if (!User.IsInRole("superadmin") && CurrentBuildingId != buildingId)
            return Forbid();

        try
        {
            await _settings.SaveBuildingAsync(buildingId, model, CurrentUserId, CurrentRole);
            TempData["Message"] = Loc.T("Building_Settings_Saved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveBuilding failed");
            TempData["Error"] = Loc.T("Save_Failed_2");
        }

        return RedirectToAction(nameof(Building), new { id = buildingId });
    }

    // ============================================================
    // Own Settings (Resident + Admin + Super Admin)
    // ============================================================
    /// <summary>
    /// Shows the signed-in user's own settings page.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Own()
    {
        try
        {
            var settings = await _settings.GetOwnAsync(
                CurrentUserId, CurrentRole, CurrentBuildingId, CurrentApartmentId);
            ViewBag.IsResident = CurrentRole == "resident";
            ViewBag.CanManageTwoFactor = (CurrentRole is "admin" or "superadmin")
                && !User.HasClaim("impersonated", "true");
            return View(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Own GET failed");
            TempData["Error"] = Loc.T("An_Unexpected_Error_Occurred");
            return View(new OwnSettings());
        }
    }

    /// <summary>
    /// Saves the signed-in user's own settings (profile, PIN change and preferences).
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Own(OwnSettings model)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState
                .Where(item => item.Value?.Errors.Count > 0)
                .Select(item => $"{item.Key}: {string.Join(", ", item.Value!.Errors.Select(error => error.ErrorMessage))}")
                .ToArray();
            _logger.LogWarning("Own settings validation failed for account {AccountId}: {ValidationErrors}",
                CurrentUserId, string.Join(" | ", validationErrors));
            TempData["Error"] = Loc.T("Please_Check_The_Entered_Data");
            return RedirectToAction(nameof(Own));
        }

        var pinChanged = !string.IsNullOrWhiteSpace(model.NewPin);
        if (pinChanged && (model.NewPin != model.ConfirmNewPin
            || model.NewPin!.Length is < 4 or > 12
            || model.NewPin.Any(character => character < '0' || character > '9')
            || string.IsNullOrWhiteSpace(model.CurrentPin)
            || model.CurrentPin.Length is < 4 or > 12
            || model.CurrentPin.Any(character => character < '0' || character > '9')))
        {
            TempData["Error"] = Loc.T("PIN_4_12_Digits");
            return RedirectToAction(nameof(Own));
        }

        var saveErrors = new List<string>();
        var saveSuccess = new List<string>();

        try
        {
            await _settings.SaveOwnAsync(CurrentUserId, model, CurrentUserId, CurrentRole,
                CurrentBuildingId, CurrentApartmentId);
            saveSuccess.Add(Loc.T("Preferences"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveOwn preferences failed");
            saveErrors.Add(Loc.T("Preferences_2"));
        }

        try
        {
            await _settings.UpdateOwnIdentityProfileAsync(
                CurrentUserId, CurrentRole, CurrentBuildingId, CurrentApartmentId,
                model, pinChanged ? model.NewPin : null, model.CurrentPin);
            saveSuccess.Add(Loc.T("Your_Details"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveOwn identity profile failed");
            saveErrors.Add(Loc.T("An_Unexpected_Error_Occurred"));
        }

        if (saveErrors.Count == 0)
            TempData["Message"] = Loc.T("Saved") + string.Join(Loc.T("And"), saveSuccess);
        else if (saveSuccess.Count > 0)
            TempData["Message"] = Loc.T("Saved_2") + string.Join(Loc.T("And"), saveSuccess)
                + Loc.T("But_Failed") + string.Join(" | ", saveErrors);
        else
            TempData["Error"] = Loc.T("Save_Failed_3") + string.Join(" | ", saveErrors);

        var currentLang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var newLang = string.IsNullOrWhiteSpace(model.Language) ? "ar" : model.Language.ToLowerInvariant();
        if (newLang is not ("ar" or "en" or "fr" or "de")) newLang = "ar";
        if (newLang != currentLang)
            return RedirectToAction("Set", "Language", new
            {
                lang = newLang,
                returnUrl = Url.Action(nameof(Own))
            });

        return RedirectToAction(nameof(Own));
    }
}
