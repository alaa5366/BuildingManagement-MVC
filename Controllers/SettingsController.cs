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

[Authorize]
public class SettingsController : Controller
{
    private readonly SettingsService _settings;
    private readonly BuildingsService _buildings;
    private readonly UsersService _users;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        SettingsService settings,
        BuildingsService buildings,
        UsersService users,
        ILogger<SettingsController> logger)
    {
        _settings = settings;
        _buildings = buildings;
        _users = users;
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
    [HttpGet]
    [Authorize(Roles = "superadmin")]
    [HasPermission(AdminPermissions.ManageSettingsGlobal)]
    public async Task<IActionResult> Global()
    {
        var settings = await _settings.GetGlobalAsync();

        settings.FirebaseProjectId = HttpContext.RequestServices
            .GetService<IConfiguration>()?["Firebase:ProjectId"] ?? "";

        return View(settings);
    }

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
            TempData["Error"] = Loc.T("Save_Failed_2") + ex.Message;
        }
        return RedirectToAction(nameof(Global));
    }

    // ============================================================
    // Building Settings (Admin + Super Admin)
    // ============================================================
    [HttpGet]
    [Authorize(Roles = "superadmin,admin")]
    public async Task<IActionResult> Building(string? id)
    {
        string? buildingId;

        if (User.IsInRole("superadmin"))
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                ViewBag.Buildings = await _buildings.GetAllAsync();
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "superadmin,admin")]
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
            TempData["Error"] = Loc.T("Save_Failed_2") + ex.Message;
        }

        return RedirectToAction(nameof(Building), new { id = buildingId });
    }

    // ============================================================
    // Own Settings (Resident + Admin + Super Admin)
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Own()
    {
        try
        {
            var settings = await _settings.GetOwnAsync(
                CurrentUserId, CurrentRole, CurrentBuildingId, CurrentApartmentId);

            // ✅ جلب بيانات المستخدم الحالية حسب الدور
            if (CurrentRole == "resident" &&
                !string.IsNullOrEmpty(CurrentBuildingId) &&
                !string.IsNullOrEmpty(CurrentApartmentId))
            {
                var building = await _buildings.GetByIdAsync(CurrentBuildingId);
                var apt = building?.Apartments.FirstOrDefault(a => a.Id == CurrentApartmentId);

                if (apt != null)
                {
                    ViewBag.OwnerName = apt.Owner;
                    ViewBag.AptLabel = apt.Label;
                    ViewBag.CurrentPhone = apt.Phone;
                    ViewBag.CurrentWhatsapp = apt.Phone;
                    ViewBag.AptNumber = apt.Number;
                }
                ViewBag.IsResident = true;
            }
            else if (CurrentRole == "admin")
            {
                var user = await _users.GetByUidAsync(CurrentUserId);
                if (user != null)
                {
                    ViewBag.OwnerName = user.Name;
                    ViewBag.CurrentPhone = user.Phone;
                    ViewBag.CurrentWhatsapp = string.IsNullOrEmpty(user.Whatsapp)
                        ? user.Phone
                        : user.Whatsapp;
                }
                ViewBag.IsResident = false;
            }
            else if (CurrentRole == "superadmin")
            {
                var user = await _users.GetByUidAsync(CurrentUserId);
                if (user != null)
                {
                    ViewBag.OwnerName = user.Name;
                    ViewBag.CurrentPhone = user.Phone;
                }
                ViewBag.IsResident = false;
            }

            return View(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Own GET failed");
            TempData["Error"] = Loc.T("Error_2") + ex.Message;
            return View(new OwnSettings());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Own(
    OwnSettings model,
    string? OwnerName,
    string? AptLabel,
    string? NewPhone,
    string? NewWhatsapp,
    string? NewPin,
    string? ConfirmNewPin,
    string? CurrentPin)
    {
        var saveErrors = new List<string>();
        var saveSuccess = new List<string>();

        // ============================================================
        // 1. حفظ الإعدادات العادية (اللغة + التفضيلات)
        // ============================================================
        try
        {
            await _settings.SaveOwnAsync(
                CurrentUserId, model, CurrentUserId, CurrentRole,
                CurrentBuildingId, CurrentApartmentId);
            saveSuccess.Add(Loc.T("Preferences"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveOwn (settings) failed");
            saveErrors.Add(Loc.T("Preferences_2") + ex.Message);
        }

        // ============================================================
        // 2. تحديث بيانات المستخدم (الاسم/الموبايل/PIN)
        // ============================================================
        try
        {
            var pinChanged = !string.IsNullOrWhiteSpace(NewPin);

            if (pinChanged)
            {
                if (NewPin != ConfirmNewPin)
                    throw new InvalidOperationException(Loc.T("The_New_PIN_And_Its_Confirmation_2"));

                if (NewPin!.Length != 4 || !NewPin.All(char.IsDigit))
                    throw new InvalidOperationException(Loc.T("PIN_Must_Be_Exactly_4_Digits"));

                if (string.IsNullOrWhiteSpace(CurrentPin))
                    throw new InvalidOperationException(Loc.T("You_Must_Enter_The_Current_PIN"));
            }

            if (CurrentRole == "resident" &&
                !string.IsNullOrEmpty(CurrentBuildingId) &&
                !string.IsNullOrEmpty(CurrentApartmentId))
            {
                await _settings.UpdateResidentProfileAsync(
                    CurrentBuildingId, CurrentApartmentId,
                    OwnerName ?? "", AptLabel ?? "", NewPhone ?? "",
                    pinChanged ? NewPin : null, CurrentPin ?? "",
                    CurrentUserId, CurrentRole);
                saveSuccess.Add(Loc.T("Your_Details"));
            }
            else if (CurrentRole == "admin")
            {
                await _settings.UpdateAdminProfileAsync(
                    CurrentUserId, OwnerName ?? "", NewPhone ?? "", NewWhatsapp ?? "",
                    pinChanged ? NewPin : null, CurrentPin ?? "", CurrentRole);
                saveSuccess.Add(Loc.T("Your_Details"));
            }
            else if (CurrentRole == "superadmin")
            {
                await _settings.UpdateSuperAdminNameAsync(CurrentUserId, OwnerName ?? "");
                saveSuccess.Add(Loc.T("Your_Details"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveOwn (profile) failed");
            saveErrors.Add(ex.Message);
        }

        // ============================================================
        // 3. رسالة موحّدة
        // ============================================================
        if (saveErrors.Count == 0)
        {
            TempData["Message"] = Loc.T("Saved") + string.Join(Loc.T("And"), saveSuccess);
        }
        else if (saveSuccess.Count > 0)
        {
            TempData["Message"] = Loc.T("Saved_2") + string.Join(Loc.T("And"), saveSuccess) +
                                 Loc.T("But_Failed") + string.Join(" | ", saveErrors);
        }
        else
        {
            TempData["Error"] = Loc.T("Save_Failed_3") + string.Join(" | ", saveErrors);
        }

        // ============================================================
        // ✅ 4. مزامنة اللغة: لو اتغيرت، نحدّث الكوكي عبر LanguageController
        // ============================================================
        var currentLang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var newLang = string.IsNullOrWhiteSpace(model.Language) ? "ar" : model.Language.ToLower();
        if (newLang != "ar" && newLang != "en" && newLang != "fr" && newLang != "de") newLang = "ar";

        if (newLang != currentLang)
        {
            return RedirectToAction("Set", "Language", new
            {
                lang = newLang,
                returnUrl = Url.Action(nameof(Own))
            });
        }

        return RedirectToAction(nameof(Own));
    }
}