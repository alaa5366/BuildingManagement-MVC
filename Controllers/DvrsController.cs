using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin,admin")]
[AdminPermission(AdminPermissions.ManageDvrs)]
public class DvrsController : Controller
{
    private readonly DvrService _dvrs;
    private readonly BuildingsService _buildings;
    private readonly MediaMtxService _mediamtx;
    private readonly ILogger<DvrsController> _log;

    public DvrsController(
        DvrService dvrs,
        BuildingsService buildings,
        MediaMtxService mediamtx,
        ILogger<DvrsController> log)
    {
        _dvrs = dvrs;
        _buildings = buildings;
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
    public async Task<IActionResult> Index(string? buildingId)
    {
        if (IsSuperAdmin && string.IsNullOrEmpty(buildingId))
        {
            var allBuildings = await _buildings.GetAllAsync();
            return View("ChooseBuilding", allBuildings);
        }

        var bid = IsSuperAdmin ? buildingId! : CurrentBuildingId!;
        if (!await CanManageAsync(bid)) return Forbid();

        var building = await _buildings.GetByIdAsync(bid);
        if (building == null) return NotFound();

        var dvrs = await _dvrs.GetByBuildingAsync(bid);

        ViewBag.Building = building;
        ViewBag.BuildingId = bid;
        return View(dvrs);
    }

    // ============================================================
    // Create
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Create(string buildingId)
    {
        if (!await CanManageAsync(buildingId)) return Forbid();
        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        ViewBag.Building = building;
        ViewBag.BuildingId = buildingId;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string buildingId, string name, string ip, int port,
        string brand, string username, string password)
    {
        if (!await CanManageAsync(buildingId)) return Forbid();
        if (!ModelState.IsValid)
        {
            ViewBag.BuildingId = buildingId;
            ViewBag.Building = await _buildings.GetByIdAsync(buildingId);
            return View();
        }

        var id = await _dvrs.CreateAsync(buildingId, name, ip, port, brand, username, password);
        TempData["Success"] = "تم إضافة DVR بنجاح";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================================
    // Edit
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var dvr = await _dvrs.GetByIdAsync(id);
        if (dvr == null) return NotFound();
        if (!await CanManageAsync(dvr.BuildingId)) return Forbid();

        ViewBag.Building = await _buildings.GetByIdAsync(dvr.BuildingId);
        return View(dvr);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id, string name, string ip, int port,
        string brand, string username, string? newPassword, bool isActive)
    {
        var dvr = await _dvrs.GetByIdAsync(id);
        if (dvr == null) return NotFound();
        if (!await CanManageAsync(dvr.BuildingId)) return Forbid();

        await _dvrs.UpdateAsync(id, name, ip, port, brand, username, newPassword, isActive);
        TempData["Success"] = "تم تعديل DVR";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================================
    // Details + Cameras
    // ============================================================
    public async Task<IActionResult> Details(string id)
    {
        var dvr = await _dvrs.GetByIdAsync(id);
        if (dvr == null) return NotFound();
        if (!await CanManageAsync(dvr.BuildingId)) return Forbid();

        ViewBag.Building = await _buildings.GetByIdAsync(dvr.BuildingId);
        return View(dvr);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCamera(
        string dvrId, string name, int channel, string? rtspPath)
    {
        var dvr = await _dvrs.GetByIdAsync(dvrId);
        if (dvr == null) return NotFound();
        if (!await CanManageAsync(dvr.BuildingId)) return Forbid();

        // سجّل الـ path في MediaMTX
        var password = await _dvrs.GetDecryptedPasswordAsync(dvrId);
        var rtspUrl = MediaMtxService.BuildRtspUrl(
            dvr.IpAddress, dvr.Port, dvr.Username, password,
            string.IsNullOrEmpty(rtspPath)
                ? MediaMtxService.GetDefaultRtspPath(dvr.Brand, channel)
                : rtspPath);

        var camId = await _dvrs.AddCameraAsync(dvrId, name, channel, rtspPath ?? "");
        var pathName = $"cam_{camId}";
        await _mediamtx.AddOrUpdatePathAsync(pathName, rtspUrl);

        TempData["Success"] = "تم إضافة الكاميرا";
        return RedirectToAction(nameof(Details), new { id = dvrId });
    }

    // ============================================================
    // Delete Camera
    // ============================================================
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCamera(string cameraId, string dvrId)
    {
        var dvr = await _dvrs.GetByIdAsync(dvrId);
        if (dvr == null) return NotFound();
        if (!await CanManageAsync(dvr.BuildingId)) return Forbid();

        // امسح من SQL + Firestore + MediaMTX
        await _dvrs.DeleteCameraAsync(cameraId);
        await _mediamtx.DeletePathAsync($"cam_{cameraId}");

        TempData["Success"] = "تم حذف الكاميرا";
        return RedirectToAction(nameof(Details), new { id = dvrId });
    }

    // ============================================================
    // Delete DVR
    // ============================================================
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var dvr = await _dvrs.GetByIdAsync(id);
        if (dvr == null) return NotFound();
        if (!await CanManageAsync(dvr.BuildingId)) return Forbid();

        await _dvrs.DeleteAsync(id);
        TempData["Success"] = "تم حذف DVR";
        return RedirectToAction(nameof(Index), new { buildingId = dvr.BuildingId });
    }
}