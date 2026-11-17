using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  CamerasController	عرض الكاميرات المباشر (HLS via MediaMTX)
// ============================================================
/// <summary>
/// Live camera viewing for residents/admins and camera management for a building.
/// </summary>
/// <remarks>
/// Access: any signed-in user (roles are checked per action).
/// </remarks>
[Authorize]
[AdminPermission(BuildingManagementMvc.Models.AdminPermissions.ViewCameras)]
public class CamerasController : Controller
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly BuildingsService _buildings;
    private readonly IConfiguration _config;
    private readonly ILogger<CamerasController> _log;
    private readonly IWebHostEnvironment _env;

    /// <summary>
    /// Initializes a new instance of the <see cref="CamerasController"/> class.
    /// </summary>
    public CamerasController(
        IDbContextFactory<AppDbContext> sqlFactory,
        BuildingsService buildings,
        IConfiguration config,
        ILogger<CamerasController> log,
        IWebHostEnvironment env)
    {
        _sqlFactory = sqlFactory;
        _buildings = buildings;
        _config = config;
        _log = log;
        _env = env;
    }

    // ============================================================
    // Live View
    // ============================================================
    /// <summary>
    /// Shows the live camera wall of a building.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
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
    /// <summary>
    /// Gets the HLS URL.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
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

        // MediaMTX paths are registered as cam_{cameraId} by DvrsController.AddCamera.
        var streamName = $"cam_{cam.Id}";

        // ✅ HLS URL من MediaMTX
        var hlsBase = _config["MediaMtx:HlsBaseUrl"];
        if (string.IsNullOrWhiteSpace(hlsBase))
        {
            if (!_env.IsDevelopment())
            {
                _log.LogError("MediaMtx:HlsBaseUrl is not configured for a non-development environment.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { error = "camera-stream-not-configured" });
            }
            hlsBase = "http://localhost:8888";
        }
        if (!Uri.TryCreate(hlsBase, UriKind.Absolute, out var hlsUri) ||
            (hlsUri.Scheme != Uri.UriSchemeHttp && hlsUri.Scheme != Uri.UriSchemeHttps) ||
            (!_env.IsDevelopment() && hlsUri.IsLoopback))
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "invalid-camera-stream-configuration" });

        var hlsUrl = $"{hlsBase.TrimEnd('/')}/{streamName.TrimStart('/')}/index.m3u8";

        return Json(new
        {
            cameraId,
            name = cam.Name,
            hlsUrl
        });
    }
}