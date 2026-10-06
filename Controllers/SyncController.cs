using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin")]
[HasPermission(AdminPermissions.ManageSync)]
public class SyncController : Controller
{
    private readonly DbSyncService _sync;
    private readonly BuildingsService _buildings;
    private readonly StorageSettingsService _storageSettings;
    private readonly ReverseSyncService _reverseSync;
    private readonly DvrService _dvrs;
    private readonly AuditLogSyncService _auditLogSync;

    public SyncController(
        DbSyncService sync,
        BuildingsService buildings,
        StorageSettingsService storageSettings,
        ReverseSyncService reverseSync,
        DvrService dvrs,
        AuditLogSyncService auditLogSync)
    {
        _sync = sync;
        _buildings = buildings;
        _storageSettings = storageSettings;
        _reverseSync = reverseSync;
        _dvrs = dvrs;
        _auditLogSync = auditLogSync;
    }

    private string CurrentUserId =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    public async Task<IActionResult> Index()
    {
        ViewBag.CurrentMode = await _storageSettings.GetModeAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncAdmins(bool autoFix)
    {
        var result = await _sync.SyncAdminsAsync(autoFix, CurrentUserId);
        ViewBag.Result = result;
        ViewBag.SyncType = Loc.T("Admins_2");
        return View("Result");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncResidents(bool autoFix)
    {
        var result = await _sync.SyncResidentsAsync(autoFix, CurrentUserId);
        ViewBag.Result = result;
        ViewBag.SyncType = Loc.T("Residents");
        return View("Result");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncPasswords()
    {
        var result = await _sync.SyncPasswordsAsync(CurrentUserId);
        ViewBag.Result = result;
        ViewBag.SyncType = Loc.T("Passwords");
        return View("Result");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncSql()
    {
        try
        {
            var report = await _buildings.SyncAllToSqlAsync();
            ViewBag.Report = report;
            ViewBag.Provider = _buildings.StorageProvider;
            ViewBag.MirrorEnabled = _buildings.SqlMirrorEnabled;
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
        }
        return View("SqlResult");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReverseSync()
    {
        try
        {
            var report = await _reverseSync.SyncSqlToFirebaseAsync();
            ViewBag.Report = report;
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
        }
        return View("ReverseSyncResult");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStorageMode(string mode)
    {
        try
        {
            await _storageSettings.SetModeAsync(mode, CurrentUserId);
            TempData["Success"] = $"تم تحويل وضع التخزين إلى: {mode}";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetStorageMode()
    {
        var mode = await _storageSettings.GetModeAsync();
        return Json(new { mode });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedUsersToSql()
    {
        try
        {
            var usersService = HttpContext.RequestServices.GetRequiredService<UsersService>();
            var users = await usersService.GetAllFromFirestoreAsync();
            int ok = 0, fail = 0;
            var errors = new List<string>();

            foreach (var u in users)
            {
                try
                {
                    if (await usersService.MirrorOneToSqlAsync(u)) ok++;
                    else fail++;
                }
                catch (Exception ex)
                {
                    fail++;
                    errors.Add($"{u.Uid}: {ex.Message}");
                }
            }

            TempData["Success"] = $"تم نسخ {ok} مستخدم، فشل {fail}.";
            if (errors.Any())
                TempData["Error"] = string.Join(" | ", errors.Take(5));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncDvrs()
    {
        try
        {
            var report = await _dvrs.SyncAllToSqlAsync();

            if (report.Errors.Count > 0)
            {
                TempData["Error"] = $"تم النسخ مع {report.Errors.Count} أخطاء. " +
                                    $"DVRs: +{report.DvrsAdded} / ~{report.DvrsUpdated} | " +
                                    $"Cameras: +{report.CamerasAdded} / ~{report.CamerasUpdated}";
            }
            else
            {
                TempData["Success"] = $"✅ تم النسخ بنجاح في {report.Elapsed.TotalSeconds:0.0} ثانية — " +
                                      $"DVRs: {report.DvrsTotal} | Cameras: {report.CamerasTotal}";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"❌ خطأ: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncAuditLog()
    {
        try
        {
            var report = await _auditLogSync.SyncAllToSqlAsync();

            if (report.Errors.Count > 0)
            {
                TempData["Error"] = $"AuditLog: +{report.Added} / تخطي {report.Skipped} / فشل {report.Failed}";
            }
            else
            {
                TempData["Success"] = $"✅ AuditLog: +{report.Added}، تخطي {report.Skipped} في {report.Elapsed.TotalSeconds:0.0} ثانية";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"❌ خطأ: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncEverything()
    {
        var results = new List<string>();
        var errors = new List<string>();

        try
        {
            var buildingReport = await _buildings.SyncAllToSqlAsync();
            results.Add($"🏢 Buildings: {buildingReport.Buildings}");

            if (buildingReport.Errors.Any())
                errors.AddRange(buildingReport.Errors.Take(10));

            var usersService = HttpContext.RequestServices.GetRequiredService<UsersService>();
            var users = await usersService.GetAllFromFirestoreAsync();
            int userOk = 0, userFail = 0;
            foreach (var u in users)
            {
                try
                {
                    if (await usersService.MirrorOneToSqlAsync(u)) userOk++;
                    else userFail++;
                }
                catch (Exception ex)
                {
                    userFail++;
                    errors.Add($"User {u.Uid}: {ex.Message}");
                }
            }
            results.Add($"👥 Users: {userOk} (فشل {userFail})");

            var dvrReport = await _dvrs.SyncAllToSqlAsync();
            results.Add($"📹 DVRs: {dvrReport.DvrsTotal} | Cameras: {dvrReport.CamerasTotal}");

            if (dvrReport.Errors.Any())
                errors.AddRange(dvrReport.Errors.Take(10));

            var auditReport = await _auditLogSync.SyncAllToSqlAsync();
            results.Add($"📋 AuditLog: +{auditReport.Added}");

            if (auditReport.Errors.Any())
                errors.AddRange(auditReport.Errors.Take(10));

            TempData["Success"] = "✅ مزامنة شاملة نجحت: " + string.Join(" | ", results);
            if (errors.Any())
                TempData["Error"] = $"⚠️ {errors.Count} أخطاء: " + string.Join(" | ", errors.Take(5));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"❌ فشل المزامنة الشاملة: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}