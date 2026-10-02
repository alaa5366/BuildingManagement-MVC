using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin")]
[HasPermission(AdminPermissions.ManageSync)]
public class SyncController : Controller
{
    private readonly DbSyncService _sync;

    public SyncController(DbSyncService sync) => _sync = sync;

    private string CurrentUserId =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    public IActionResult Index() => View();

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
}