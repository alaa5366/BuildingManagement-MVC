using BuildingManagementMvc.Attributes;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "admin")]
[AdminPermission(BuildingManagementMvc.Models.AdminPermissions.UseWhatsApp)]
public class WhatsAppController : Controller
{
    private readonly TargetWhatsAppService _whatsApp;
    private readonly TargetNotificationsService _notifications;

    public WhatsAppController(TargetWhatsAppService whatsApp, TargetNotificationsService notifications)
    {
        _whatsApp = whatsApp;
        _notifications = notifications;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await _whatsApp.GetIndexAsync(BuildingId, HttpContext.RequestAborted);
        if (model is null) return NotFound();
        ViewBag.Templates = WhatsAppTemplateService.Templates;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(
        string aptId, string templateId, string? amount, string? note,
        string? share, string? message)
    {
        var extra = new Dictionary<string, string>
        {
            ["amount"] = amount ?? "",
            ["note"] = note ?? "",
            ["share"] = share ?? "",
            ["message"] = message ?? ""
        };

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var result = await _whatsApp.GenerateAsync(
                BuildingId, aptId, templateId, extra, baseUrl, HttpContext.RequestAborted);
            if (result is null) return NotFound();

            // Publish the same content to the resident's in-app inbox. WhatsApp remains
            // a user-confirmed wa.me draft; the browser cannot truthfully confirm delivery.
            var templateName = WhatsAppTemplateService.Templates[templateId].Name;
            var notificationBody = result.Message.Length <= 1000
                ? result.Message
                : result.Message[..997] + "...";
            await _notifications.CreateResidentNotificationAsync(
                BuildingId, aptId, templateName, notificationBody,
                "whatsapp-draft", "📲", HttpContext.RequestAborted);

            return View("Result", result);
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
