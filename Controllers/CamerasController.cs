using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

[Authorize]
public class CamerasController : Controller
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly BuildingsService _buildings;
    private readonly IConfiguration _config;
    private readonly ILogger<CamerasController> _log;

    public CamerasController(
        IDbContextFactory<AppDbContext> sqlFactory,
        BuildingsService buildings,
        IConfiguration config,
        ILogger<CamerasController> log)
    {
        _sqlFactory = sqlFactory;
        _buildings = buildings;
        _config = config;
        _log = log;
    }

    // ============================================================
    // Live View
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Live(string? buildingId)
    {
        // SuperAdmin بدون buildingId → صفحة اختيار العمارة
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
    // Get HLS URL — ✅ HLS مباشر من MediaMTX
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

        // ✅ استخرج اسم الـ stream من RTSP Path
        var streamName = cam.RtspPath?.Trim() ?? "";

        // لو الـ RtspPath كامل (rtsp://...)، استخرج اسم المسار
        if (streamName.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(streamName);
                streamName = uri.AbsolutePath.TrimStart('/');
            }
            catch
            {
                streamName = $"cam{cam.Channel}";
            }
        }

        // لو فاضي، استخدم "cam{channel}"
        if (string.IsNullOrWhiteSpace(streamName))
            streamName = $"cam{cam.Channel}";

        // ✅ HLS URL من MediaMTX
        var hlsBase = _config["MediaMtx:HlsBaseUrl"] ?? "http://localhost:8888";
        var hlsUrl = $"{hlsBase.TrimEnd('/')}/{streamName}/index.m3u8";

        return Json(new
        {
            cameraId,
            name = cam.Name,
            hlsUrl
        });
    }
}