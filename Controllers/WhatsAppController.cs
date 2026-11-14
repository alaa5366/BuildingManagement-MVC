using BuildingManagementMvc.Attributes;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  WhatsAppController	رسائل WhatsApp (من قوالب)
// ============================================================
[Authorize(Roles = "admin")]
[AdminPermission(BuildingManagementMvc.Models.AdminPermissions.UseWhatsApp)]
public class WhatsAppController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly WhatsAppTemplateService _templates;

    public WhatsAppController(BuildingsService buildings, WhatsAppTemplateService templates)
    {
        _buildings = buildings; _templates = templates;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        ViewBag.Building = building;
        ViewBag.Templates = WhatsAppTemplateService.Templates;
        return View();
    }

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
