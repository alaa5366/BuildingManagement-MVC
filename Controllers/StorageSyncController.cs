using BuildingManagementMvc.Data;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

// صفحة سوبر أدمن: حالة التخزين المزدوج (Firestore + SQL) ومزامنة كاملة يدوية
[Authorize(Roles = "superadmin")]
public class StorageSyncController : Controller
{
    private readonly BuildingsService _buildings;

    public StorageSyncController(BuildingsService buildings) => _buildings = buildings;

    [HttpGet]
    public IActionResult Index()
    {
        FillHealth();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run()
    {
        FillHealth();
        try
        {
            ViewBag.Report = await _buildings.SyncAllToSqlAsync();
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
        }
        return View("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetCounters()
    {
        SqlMirrorHealth.Reset();
        return RedirectToAction(nameof(Index));
    }

    private void FillHealth()
    {
        ViewBag.Provider = _buildings.StorageProvider;
        ViewBag.MirrorEnabled = _buildings.SqlMirrorEnabled;
        ViewBag.Failures = SqlMirrorHealth.FailureCount;
        ViewBag.LastError = SqlMirrorHealth.LastError;
        ViewBag.LastErrorAt = SqlMirrorHealth.LastErrorAt;
        ViewBag.LastSuccessAt = SqlMirrorHealth.LastSuccessAt;
    }
}
