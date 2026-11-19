using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "admin,resident")]
public class NotificationsController : Controller
{
    private readonly TargetNotificationsService _notifications;

    public NotificationsController(TargetNotificationsService notifications)
    {
        _notifications = notifications;
    }

    private string? BuildingId => User.FindFirstValue("buildingId");
    private string? ApartmentId => User.FindFirstValue("apartmentId");
    private string RecipientType => User.IsInRole("admin") ? "admin" : "resident";

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        if (string.IsNullOrWhiteSpace(BuildingId) ||
            (RecipientType == "resident" && string.IsNullOrWhiteSpace(ApartmentId)))
            return Forbid();

        var paged = RecipientType == "admin"
            ? await _notifications.GetPageForAdminAsync(
                BuildingId, page, pageSize, HttpContext.RequestAborted)
            : await _notifications.GetPageForResidentAsync(
                BuildingId, ApartmentId!, page, pageSize, HttpContext.RequestAborted);
        ViewBag.RouteValues = new Dictionary<string, string?>
        {
            ["pageSize"] = paged.PageSize.ToString()
        };
        return View(paged);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(string id, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(BuildingId) ||
            (RecipientType == "resident" && string.IsNullOrWhiteSpace(ApartmentId)))
            return Forbid();

        await _notifications.MarkReadAsync(
            BuildingId, id, RecipientType,
            RecipientType == "resident" ? ApartmentId : null,
            HttpContext.RequestAborted);

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl)
            ? Url.Action(nameof(Index))!
            : returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        if (string.IsNullOrWhiteSpace(BuildingId) ||
            (RecipientType == "resident" && string.IsNullOrWhiteSpace(ApartmentId)))
            return Forbid();

        await _notifications.MarkAllReadAsync(
            BuildingId, RecipientType,
            RecipientType == "resident" ? ApartmentId : null,
            HttpContext.RequestAborted);

        return RedirectToAction(nameof(Index));
    }
}
