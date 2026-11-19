using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  DvrsController	إدارة أجهزة DVR والكاميرات
// ============================================================
/// <summary>
/// Management of DVR devices and the cameras attached to them (connection settings are stored encrypted).
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>superadmin,admin</c>.
/// </remarks>
[Authorize(Roles = "superadmin,admin")]
[AdminPermission(AdminPermissions.ManageDvrs)]
public class DvrsController : Controller
{
    private readonly DvrService _dvrs;
    private readonly MediaMtxService _mediamtx;
    private readonly ILogger<DvrsController> _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="DvrsController"/> class.
    /// </summary>
    public DvrsController(
        DvrService dvrs,
        MediaMtxService mediamtx,
        ILogger<DvrsController> log)
    {
        _dvrs = dvrs;
        _mediamtx = mediamtx;
        _log = log;
    }

    private string? CurrentBuildingId => User.FindFirst("buildingId")?.Value;
    private bool IsSuperAdmin => User.IsInRole("superadmin");

    private async Task<bool> CanManageAsync(string buildingId)
    {
        if (IsSuperAdmin) return true;
        return CurrentBuildingId == buildingId;
    }

    // ============================================================
    // Index — كل DVRs العمارة
    // ============================================================
    /// <summary>
    /// Shows the main page of the dvrs screen.
    /// </summary>
    public async Task<IActionResult> Index(string? buildingId)
    {
        if (IsSuperAdmin && string.IsNullOrEmpty(buildingId))
        {
            var allBuildings = await _dvrs.GetBuildingSummariesAsync(HttpContext.RequestAborted);
            return View("ChooseBuilding", allBuildings);
        }

        var bid = IsSuperAdmin ? buildingId! : CurrentBuildingId!;
        if (!await CanManageAsync(bid)) return Forbid();

        var building = await _dvrs.GetBuildingSummaryAsync(bid, HttpContext.RequestAborted);
        if (building == null) return NotFound();

        var dvrs = await _dvrs.GetByBuildingAsync(bid);

        ViewBag.BuildingName = building.Name;
        ViewBag.BuildingNumber = building.BuildingNumber;
        ViewBag.BuildingId = bid;
        return View(dvrs);
    }

    // ============================================================
    // Create
    // ============================================================
    /// <summary>
    /// Creates the dvrs create page.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Create(string buildingId)
    {
        if (!await CanManageAsync(buildingId)) return Forbid();
        var building = await _dvrs.GetBuildingSummaryAsync(buildingId, HttpContext.RequestAborted);
        if (building == null) return NotFound();

        ViewBag.BuildingName = building.Name;
        ViewBag.BuildingNumber = building.BuildingNumber;
        ViewBag.BuildingId = buildingId;
        return View();
    }

    /// <summary>
    /// Handles the create form submission of the dvrs screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string buildingId, string name, string ip, int port,
        string brand, string username, string password)
    {
        if (!await CanManageAsync(buildingId)) return Forbid();
        if (!ModelState.IsValid || !DvrNetworkPolicy.IsAllowedEndpoint(ip, port))
        {
            ModelState.AddModelError(string.Empty, "يجب إدخال عنوان IP خاص بالشبكة الداخلية ومنفذ صحيح.");
            ViewBag.BuildingId = buildingId;
            var building = await _dvrs.GetBuildingSummaryAsync(buildingId, HttpContext.RequestAborted);
            ViewBag.BuildingName = building?.Name;
            ViewBag.BuildingNumber = building?.BuildingNumber;
            return View();
        }

        var id = await _dvrs.CreateAsync(buildingId, name, ip, port, brand, username, password);
        TempData["Success"] = Loc.T("Dvr_Added_Successfully");
        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================================
    // Edit
    // ============================================================
    /// <summary>
    /// Edits the dvrs edit page.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var dvr = await _dvrs.GetByIdAsync(id);
        if (dvr == null) return NotFound();
        if (!await CanManageAsync(dvr.BuildingId)) return Forbid();

        var building = await _dvrs.GetBuildingSummaryAsync(dvr.BuildingId, HttpContext.RequestAborted);
        ViewBag.BuildingName = building?.Name;
        ViewBag.BuildingNumber = building?.BuildingNumber;
        return View(dvr);
    }

    /// <summary>
    /// Handles the edit form submission of the dvrs screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id, string name, string ip, int port,
        string brand, string username, string? newPassword, bool isActive)
    {
        var dvr = await _dvrs.GetByIdAsync(id);
        if (dvr == null) return NotFound();
        if (!await CanManageAsync(dvr.BuildingId)) return Forbid();
        if (!DvrNetworkPolicy.IsAllowedEndpoint(ip, port))
        {
            TempData["Error"] = "يجب إدخال عنوان IP خاص بالشبكة الداخلية ومنفذ صحيح.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        await _dvrs.UpdateAsync(id, name, ip, port, brand, username, newPassword, isActive);
        TempData["Success"] = Loc.T("Dvr_Updated_Successfully");
        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================================
    // Details + Cameras
    // ============================================================
    /// <summary>
    /// Shows the details page of the dvrs screen.
    /// </summary>
    public async Task<IActionResult> Details(string id)
    {
        var dvr = await _dvrs.GetByIdAsync(id);
        if (dvr == null) return NotFound();
        if (!await CanManageAsync(dvr.BuildingId)) return Forbid();

        var building = await _dvrs.GetBuildingSummaryAsync(dvr.BuildingId, HttpContext.RequestAborted);
        ViewBag.BuildingName = building?.Name;
        ViewBag.BuildingNumber = building?.BuildingNumber;
        return View(dvr);
    }

    /// <summary>
    /// Adds the camera.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCamera(
        string dvrId, string name, int channel, string? rtspPath)
    {
        var dvr = await _dvrs.GetByIdAsync(dvrId);
        if (dvr == null) return NotFound();
        if (!await CanManageAsync(dvr.BuildingId)) return Forbid();

        if (channel is < 1 or > 256 || !DvrNetworkPolicy.IsSafeRelativeRtspPath(rtspPath))
        {
            TempData["Error"] = "رقم القناة أو مسار RTSP غير صالح.";
            return RedirectToAction(nameof(Details), new { id = dvrId });
        }

        // سجّل الـ path في MediaMTX
        var password = await _dvrs.GetDecryptedPasswordAsync(dvrId);
        var rtspUrl = MediaMtxService.BuildRtspUrl(
            dvr.IpAddress, dvr.Port, dvr.Username, password,
            string.IsNullOrEmpty(rtspPath)
                ? MediaMtxService.GetDefaultRtspPath(dvr.Brand, channel)
                : rtspPath);

        var camId = await _dvrs.AddCameraAsync(dvrId, name, channel, rtspPath ?? "");
        var pathName = $"cam_{camId}";
        var registered = await _mediamtx.AddOrUpdatePathAsync(pathName, rtspUrl);
        if (!registered)
        {
            await _dvrs.DeleteCameraAsync(camId, dvrId);
            TempData["Error"] = "تعذر تسجيل البث في MediaMTX؛ لم يتم الاحتفاظ بالكاميرا.";
            return RedirectToAction(nameof(Details), new { id = dvrId });
        }

        TempData["Success"] = Loc.T("Camera_Added_Successfully");
        return RedirectToAction(nameof(Details), new { id = dvrId });
    }

    // ============================================================
    // Delete Camera
    // ============================================================
    /// <summary>
    /// Deletes the camera.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCamera(string cameraId, string dvrId)
    {
        var dvr = await _dvrs.GetByIdAsync(dvrId);
        if (dvr == null) return NotFound();
        if (!await CanManageAsync(dvr.BuildingId)) return Forbid();

        // A caller may not delete a camera belonging to a different DVR/building.
        if (!dvr.Cameras.Any(camera => camera.Id == cameraId)) return NotFound();

        // Remove the MediaMTX path first. If the media server is unavailable, retain the SQL record for retry.
        if (!await _mediamtx.DeletePathAsync($"cam_{cameraId}"))
        {
            TempData["Error"] = "تعذر حذف مسار البث من MediaMTX؛ لم يتم حذف سجل الكاميرا.";
            return RedirectToAction(nameof(Details), new { id = dvrId });
        }
        if (!await _dvrs.DeleteCameraAsync(cameraId, dvrId)) return NotFound();

        TempData["Success"] = Loc.T("Camera_Deleted_Successfully");
        return RedirectToAction(nameof(Details), new { id = dvrId });
    }

    // ============================================================
    // Delete DVR
    // ============================================================
    /// <summary>
    /// Handles the delete form submission of the dvrs screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var dvr = await _dvrs.GetByIdAsync(id);
        if (dvr == null) return NotFound();
        if (!await CanManageAsync(dvr.BuildingId)) return Forbid();

        foreach (var camera in dvr.Cameras)
        {
            if (!await _mediamtx.DeletePathAsync($"cam_{camera.Id}"))
            {
                TempData["Error"] = "تعذر حذف جميع مسارات البث من MediaMTX؛ لم يتم حذف جهاز DVR.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        await _dvrs.DeleteAsync(id);
        TempData["Success"] = Loc.T("Dvr_Deleted_Successfully");
        return RedirectToAction(nameof(Index), new { buildingId = dvr.BuildingId });
    }
}
