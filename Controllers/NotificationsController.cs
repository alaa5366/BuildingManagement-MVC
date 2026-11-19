using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  NotificationsController	الإشعارات
// ============================================================
/// <summary>
/// In-app notification list for the signed-in user (mark as read, mark all as read).
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>admin,resident</c>.
/// </remarks>
[Authorize(Roles = "admin,resident")]
public class NotificationsController : Controller
{
    private readonly TargetNotificationsService _notifications;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationsController"/> class.
    /// </summary>
    public NotificationsController(TargetNotificationsService notifications)
    {
        _notifications = notifications;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    /// <summary>
    /// Shows the main page of the notifications screen.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        if (string.IsNullOrWhiteSpace(BuildingId) ||
            (User.IsInRole("resident") && string.IsNullOrWhiteSpace(User.FindFirstValue("apartmentId"))))
            return Forbid();
        var allNotifications = User.IsInRole("admin")
            ? await _notifications.GetForAdminAsync(BuildingId, 500, HttpContext.RequestAborted)
            : await _notifications.GetForResidentAsync(BuildingId,
                User.FindFirstValue("apartmentId")!, 500, HttpContext.RequestAborted);

        var paged = BuildingManagementMvc.Models.PagedResult<BuildingManagementMvc.Models.Notification>
            .Create(allNotifications, page, pageSize);

        ViewBag.RouteValues = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString()
        };

        return View(paged);
    }

    /// <summary>
    /// Marks the read.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(string id, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(BuildingId) ||
            (User.IsInRole("resident") && string.IsNullOrWhiteSpace(User.FindFirstValue("apartmentId"))))
            return Forbid();
        var recipientType = User.IsInRole("admin") ? "admin" : "resident";
        var recipientId = recipientType == "resident" ? User.FindFirstValue("apartmentId") : null;
        await _notifications.MarkReadAsync(BuildingId, id, recipientType, recipientId,
            HttpContext.RequestAborted);

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Index")! : returnUrl);
    }

    /// <summary>
    /// Marks the all read.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        if (string.IsNullOrWhiteSpace(BuildingId) ||
            (User.IsInRole("resident") && string.IsNullOrWhiteSpace(User.FindFirstValue("apartmentId"))))
            return Forbid();
        var recipientType = User.IsInRole("admin") ? "admin" : "resident";
        var recipientId = recipientType == "resident" ? User.FindFirstValue("apartmentId") : null;
        await _notifications.MarkAllReadAsync(BuildingId, recipientType, recipientId,
            HttpContext.RequestAborted);
        return RedirectToAction("Index");
    }
}
