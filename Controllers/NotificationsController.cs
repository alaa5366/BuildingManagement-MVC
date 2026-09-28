using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

// نفس منطق core/notifications.js (إشعارات داخل التطبيق للأدمن والساكن)
[Authorize(Roles = "admin,resident")]
public class NotificationsController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly NotificationsService _notify;

    public NotificationsController(BuildingsService buildings, NotificationsService notify)
    {
        _buildings = buildings; _notify = notify;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var allNotifications = User.IsInRole("admin")
            ? _notify.GetForAdmin(building, 500)
            : _notify.GetForResident(building, User.FindFirstValue("apartmentId")!, 500);

        var paged = BuildingManagementMvc.Models.PagedResult<BuildingManagementMvc.Models.Notification>
            .Create(allNotifications, page, pageSize);

        ViewBag.RouteValues = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString()
        };

        return View(paged);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(string id, string? returnUrl)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building != null && _notify.MarkRead(building, id))
            await _buildings.SaveFullAsync(building);

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Index")! : returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building != null)
        {
            if (User.IsInRole("admin")) _notify.MarkAllReadForAdmin(building);
            else _notify.MarkAllReadForResident(building, User.FindFirstValue("apartmentId")!);
            await _buildings.SaveFullAsync(building);
        }
        return RedirectToAction("Index");
    }
}
