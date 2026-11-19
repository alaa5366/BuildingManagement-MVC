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

    public WhatsAppController(TargetWhatsAppService whatsApp) => _whatsApp = whatsApp;

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
