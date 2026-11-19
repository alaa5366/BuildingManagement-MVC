using BuildingManagementMvc.Services;
using BuildingManagementMvc.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Controllers;

public sealed class PhoneCorrectionController : Controller
{
    private readonly PersonPhoneCorrectionService _service;
    private readonly BuildingsService _buildings;
    public PhoneCorrectionController(PersonPhoneCorrectionService service, BuildingsService buildings)
    {
        _service = service;
        _buildings = buildings;
    }

    [HttpGet]
    [AllowAnonymous]
    [ActionName("Request")]
    public async Task<IActionResult> GetRequestPage()
    {
        ViewBag.Buildings = await _buildings.GetSummariesAsync();
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    [ActionName("Request")]
    public async Task<IActionResult> SubmitRequest(string previousPhone, string requestedPhone, string buildingId, string apartmentId)
    {
        await _service.SubmitAsync(previousPhone, requestedPhone, buildingId, apartmentId);
        ViewBag.Message = "إذا كانت البيانات صحيحة وتنطبق عليها شروط المراجعة، فسيظهر الطلب لإدارة العمارة للتحقق من هويتك. لا يتم تغيير الرقم تلقائيًا.";
        ViewBag.Buildings = await _buildings.GetSummariesAsync();
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "admin,superadmin")]
    [AdminPermission(AdminPermissions.ManageResidents)]
    public async Task<IActionResult> Index()
    {
        var superAdmin = User.IsInRole("superadmin");
        var buildingIds = User.FindAll("buildingId").Select(c => c.Value).Distinct().ToList();
        var items = new List<PhoneCorrectionQueueItem>();
        if (superAdmin)
            items = await _service.GetPendingAsync(null, true);
        else
            foreach (var buildingId in buildingIds)
                items.AddRange(await _service.GetPendingAsync(buildingId, false));
        return View(items.OrderBy(x => x.RequestedAtUtc).ToList());
    }

    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    [AdminPermission(AdminPermissions.ManageResidents)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(Guid requestId, bool approve, string? reason)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(reviewerId)) return Forbid();
        var superAdmin = User.IsInRole("superadmin");
        var buildingId = HttpContext.Request.Form["buildingId"].FirstOrDefault();
        var authorizedBuildings = User.FindAll("buildingId").Select(claim => claim.Value).ToHashSet(StringComparer.Ordinal);
        if (!superAdmin && (string.IsNullOrWhiteSpace(buildingId) || !authorizedBuildings.Contains(buildingId))) return Forbid();
        var success = await _service.ReviewAsync(requestId, reviewerId, approve, reason ?? "", superAdmin, buildingId);
        TempData[success ? "Message" : "Error"] = success
            ? "تم تسجيل قرار مراجعة طلب تغيير الهاتف."
            : "تعذر تنفيذ القرار. تأكد من صلاحياتك وعدم وجود تعارض في الرقم.";
        return RedirectToAction(nameof(Index));
    }
}