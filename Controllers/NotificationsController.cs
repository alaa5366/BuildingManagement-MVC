﻿using BuildingManagementMvc.Models;
using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

/// <summary>NotificationsController coordinates the corresponding application controller actions.</summary>
[Authorize(Roles = "admin,resident")]
public class NotificationsController : Controller
{
    private readonly TargetNotificationsService _notifications;
    private readonly TargetWhatsAppService _whatsApp;
    private readonly IAuditLogger _auditLogger;

    public NotificationsController(
        TargetNotificationsService notifications,
        TargetWhatsAppService whatsApp,
        IAuditLogger auditLogger)
    {
        _notifications = notifications;
        _whatsApp = whatsApp;
        _auditLogger = auditLogger;
    }

    private string? BuildingId => User.FindFirstValue("buildingId");
    private string? ApartmentId => User.FindFirstValue("apartmentId");
    private string RecipientType => User.IsInRole("admin") ? "admin" : "resident";

    /// <summary>Displays the administration page and its current records.</summary>
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

    /// <summary>Prepares a notification for publication.</summary>
    [HttpGet]
    [AdminPermission(AdminPermissions.UseWhatsApp)]
    public async Task<IActionResult> Compose()
    {
        if (!User.IsInRole("admin") || string.IsNullOrWhiteSpace(BuildingId)) return Forbid();
        var model = await _whatsApp.GetIndexAsync(BuildingId, HttpContext.RequestAborted);
        if (model is null) return NotFound();
        return View(model);
    }

    /// <summary>Publishes the prepared notification.</summary>
    [HttpPost]
    [AdminPermission(AdminPermissions.UseWhatsApp)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(string aptId, string title, string body)
    {
        if (!User.IsInRole("admin") || string.IsNullOrWhiteSpace(BuildingId) ||
            User.HasClaim("impersonated", "true"))
            return Forbid();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200 ||
            string.IsNullOrWhiteSpace(body) || body.Length > 800 ||
            body.Contains("token=", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("/Account/QuickLogin", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("/Account/SetPin", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "العنوان مطلوب (حتى 200 حرف) ونص الإشعار مطلوب (حتى 800 حرف)، ولا يُسمح بإضافة روابط دخول أو رموز مؤقتة.";
            return RedirectToAction(nameof(Compose));
        }

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var result = await _whatsApp.GenerateAsync(
                BuildingId, aptId, "general", new Dictionary<string, string> { ["message"] = body.Trim() },
                baseUrl, HttpContext.RequestAborted);
            if (result is null) return NotFound();

            await _notifications.CreateResidentNotificationAsync(
                BuildingId, aptId, title.Trim(), body.Trim(), "whatsapp-draft", "ًں“²", HttpContext.RequestAborted);

            // wa.me only prepares a user-confirmed draft; record that exact outcome
            // without ever claiming that WhatsApp sent or delivered the message.
            await _auditLogger.LogAsync(
                action: "notification.whatsapp_draft_created",
                buildingId: BuildingId,
                apartmentId: aptId,
                userId: User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
                userRole: "admin",
                metadata: new
                {
                    channel = "whatsapp",
                    deliveryStatus = "draft_ready",
                    inAppStatus = "published",
                    apartmentNumber = result.ApartmentNumber,
                    template = "general"
                },
                severity: "info");

            return View("~/Views/WhatsApp/Result.cshtml", result);
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Compose));
    }

    /// <summary>Marks the specified notification as read.</summary>
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

    /// <summary>Marks all applicable notifications as read.</summary>
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
