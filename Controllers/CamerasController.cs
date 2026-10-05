using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

[Authorize] // أي حد مسجل
public class CamerasController : Controller
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly BuildingsService _buildings;
    private readonly ILogger<CamerasController> _log;

    public CamerasController(
        IDbContextFactory<AppDbContext> sqlFactory,
        BuildingsService buildings,
        ILogger<CamerasController> log)
    {
        _sqlFactory = sqlFactory;
        _buildings = buildings;
        _log = log;
    }

    // ============================================================
    // Live view للعمارة
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Live(string? buildingId)
    {
        // ✅ لو SuperAdmin ومفيش buildingId → يروح لصفحة اختيار العمارة
        if (User.IsInRole("superadmin") && string.IsNullOrEmpty(buildingId))
        {
            return RedirectToAction("Index", "Dvrs");
        }

        if (string.IsNullOrEmpty(buildingId))
            return NotFound();

        // التحقق من الصلاحية
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var userBuildingId = User.FindFirst("buildingId")?.Value;

        if (role != "superadmin" && userBuildingId != buildingId)
        {
            if (role != "resident" || userBuildingId != buildingId)
                return Forbid();
        }

        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var cameras = await db.Cameras
            .Include(c => c.Dvr)
            .Where(c => c.BuildingId == buildingId && c.IsActive && c.Dvr!.IsActive)
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.Building = building;
        ViewBag.BuildingId = buildingId;
        return View(cameras);
    }

    // ============================================================
    // HLS URL — يرجع الـ HLS للكاميرا
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> GetHlsUrl(string cameraId)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var cam = await db.Cameras
            .Include(c => c.Dvr)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cameraId);

        if (cam == null || !cam.IsActive || cam.Dvr == null || !cam.Dvr.IsActive)
            return NotFound();

        // التحقق من الصلاحية
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var userBuildingId = User.FindFirst("buildingId")?.Value;
        if (role != "superadmin" && userBuildingId != cam.BuildingId)
            return Forbid();

        var pathName = $"cam_{cameraId}";
        var hlsUrl = $"/hls/{pathName}/index.m3u8";

        return Json(new
        {
            cameraId,
            name = cam.Name,
            hlsUrl
        });
    }
}