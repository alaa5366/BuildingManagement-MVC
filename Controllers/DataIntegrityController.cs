using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin")]
[HasPermission(AdminPermissions.ManageSync)]
public class DataIntegrityController : Controller
{
    private readonly DataIntegrityService _integrity;
    private readonly ReverseSyncService _reverseSync;
    private readonly ILogger<DataIntegrityController> _log;

    public DataIntegrityController(
        DataIntegrityService integrity,
        ReverseSyncService reverseSync,
        ILogger<DataIntegrityController> log)
    {
        _integrity = integrity;
        _reverseSync = reverseSync;
        _log = log;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var report = await _integrity.GetFullReportAsync();
            var buildings = await _integrity.CompareBuildingsAsync();
            var users = await _integrity.CompareUsersAsync();
            var dvrs = await _integrity.CompareDvrsAsync();
            var cameras = await _integrity.CompareCamerasAsync();

            ViewBag.Buildings = buildings;
            ViewBag.Users = users;
            ViewBag.Dvrs = dvrs;
            ViewBag.Cameras = cameras;

            return View(report);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "DataIntegrity failed");
            TempData["Error"] = "❌ " + ex.Message;
            return View(new DataIntegrityReport());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Refresh()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunReverseSync()
    {
        try
        {
            var report = await _reverseSync.SyncSqlToFirebaseAsync();

            TempData["Success"] = $"✅ Reverse Sync: " +
                $"{report.Added} مضاف، {report.Updated} محدّث، {report.Errors.Count} خطأ";

            if (report.Errors.Any())
            {
                TempData["Error"] = string.Join(" | ", report.Errors.Take(5));
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Reverse Sync failed");
            TempData["Error"] = "❌ " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}