using BuildingManagementMvc.Attributes;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  WhatsAppController	رسائل WhatsApp (من قوالب)
// ============================================================
/// <summary>
/// Builds WhatsApp message drafts (welcome, reminders, payment notices) from templates for apartments.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>admin</c>.
/// </remarks>
[Authorize(Roles = "admin")]
[AdminPermission(BuildingManagementMvc.Models.AdminPermissions.UseWhatsApp)]
public class WhatsAppController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly WhatsAppTemplateService _templates;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppController"/> class.
    /// </summary>
    public WhatsAppController(BuildingsService buildings, WhatsAppTemplateService templates)
    {
        _buildings = buildings; _templates = templates;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    /// <summary>
    /// Shows the main page of the whats app screen.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        ViewBag.Building = building;
        ViewBag.Templates = WhatsAppTemplateService.Templates;
        return View();
    }

    /// <summary>
    /// Handles the generate form submission of the whats app screen.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(string aptId, string templateId, string? amount, string? note, string? share, string? message)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        var apt = building?.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (building == null || apt == null) return NotFound();

        var extra = new Dictionary<string, string>
        {
            ["amount"] = amount ?? "",
            ["note"] = note ?? "",
            ["share"] = share ?? "",
            ["message"] = message ?? ""
        };

        var text = _templates.Render(templateId, building, apt, extra);
        var link = WhatsAppTemplateService.BuildWaLink(apt.Phone, text);

        ViewBag.Building = building;
        ViewBag.Apt = apt;
        ViewBag.Message = text;
        ViewBag.Link = link;
        return View("Result");
    }
}
