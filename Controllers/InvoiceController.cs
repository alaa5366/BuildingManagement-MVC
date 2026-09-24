using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

// نفس شاشة js/features/invoice.js — توليد فاتورة PDF فيها QR للتحقق
[Authorize(Roles = "admin,resident")]
public class InvoiceController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly InvoiceService _invoice;
    private readonly InvoicePdfService _pdf;
    private readonly AuditLogService _audit;

    public InvoiceController(BuildingsService buildings, InvoiceService invoice, InvoicePdfService pdf, AuditLogService audit)
    {
        _buildings = buildings; _invoice = invoice; _pdf = pdf; _audit = audit;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    // aptId اختياري: الأدمن يحدده لأي شقة في عمارته، الساكن بياخد شقته هو بس دايمًا
    [HttpGet]
    public async Task<IActionResult> Download(string? aptId, string? month)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var isResident = User.IsInRole("resident");
        var targetAptId = isResident ? User.FindFirstValue("apartmentId") : aptId;

        var apt = building.Apartments.FirstOrDefault(a => a.Id == targetAptId);
        if (apt == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;

        var data = _invoice.BuildInvoiceData(building, apt, mk);
        var pdfBytes = _pdf.Generate(data);

        var role = isResident ? "resident" : "admin";
        _audit.Push(building, "invoice_download", $"شقة {apt.Number} — {data.InvoiceNo}", role, User.Identity?.Name ?? role, apt.Number.ToString());
        await _buildings.SaveFullAsync(building);

        var fileName = $"invoice-{building.BuildingNumber}-apt-{apt.Number}-{mk}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
}
