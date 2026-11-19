using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

public sealed class PhoneCorrectionController : Controller
{
    private readonly PersonPhoneCorrectionService _service;
    public PhoneCorrectionController(PersonPhoneCorrectionService service) => _service = service;

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Request() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Request(string previousPhone, string requestedPhone, string buildingId, string apartmentId)
    {
        var accepted = await _service.SubmitAsync(previousPhone, requestedPhone, buildingId, apartmentId);
        ViewBag.Message = accepted
            ? "تم استلام الطلب. ستراجعه إدارة العمارة بعد التحقق من الهوية."
            : "تعذر إرسال الطلب. راجع البيانات أو تواصل مع إدارة العمارة للتحقق يدويًا.";
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "admin,superadmin")]
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(Guid requestId, bool approve, string? reason)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var reviewerId))
            return Forbid();
        var superAdmin = User.IsInRole("superadmin");
        var buildingId = Request.Form["buildingId"].FirstOrDefault();
        var success = await _service.ReviewAsync(requestId, reviewerId, approve, reason ?? "", superAdmin, buildingId);
        TempData[success ? "Message" : "Error"] = success
            ? "تم تسجيل قرار مراجعة طلب تغيير الهاتف."
            : "تعذر تنفيذ القرار. تأكد من صلاحياتك وعدم وجود تعارض في الرقم.";
        return RedirectToAction(nameof(Index));
    }
}