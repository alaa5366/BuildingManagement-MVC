using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  InvoiceController	توليد فواتير PDF
// ============================================================
/// <summary>
/// Generates apartment wallet invoices as PDF files.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>superadmin,admin,resident</c>.
/// </remarks>
[Authorize(Roles = "superadmin,admin,resident")]
public class InvoiceController : Controller
{
    private readonly InvoiceService _invoice;
    private readonly InvoicePdfService _pdf;
    private readonly IAuditLogger _audit;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceController"/> class.
    /// </summary>
    public InvoiceController(InvoiceService invoice, InvoicePdfService pdf, IAuditLogger audit)
    {
        _invoice = invoice;
        _pdf = pdf;
        _audit = audit;
    }

    // aptId اختياري: الأدمن يحدده لأي شقة في عمارته، الساكن بياخد شقته هو بس دايمًا
    // SuperAdmin: لازم يحدد aptId + buildingId (عن طريق Impersonation أو مباشرة)
    /// <summary>
    /// Downloads the invoice data.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Download(string? aptId, string? month)
    {
        var buildingId = User.FindFirstValue("buildingId");
        var isResident = User.IsInRole("resident");
        var isSuperAdmin = User.IsInRole("superadmin");

        if (string.IsNullOrWhiteSpace(buildingId))
        {
            if (!isSuperAdmin || string.IsNullOrWhiteSpace(aptId)) return NotFound();
            buildingId = await _invoice.FindBuildingIdByApartmentAsync(aptId, HttpContext.RequestAborted);
            if (string.IsNullOrWhiteSpace(buildingId)) return NotFound();
        }

        var targetApartmentId = isResident ? User.FindFirstValue("apartmentId") : aptId;
        if (string.IsNullOrWhiteSpace(targetApartmentId)) return NotFound();

        var role = isResident ? "resident" : isSuperAdmin ? "superadmin" : "admin";
        return await GenerateInvoiceAsync(buildingId, targetApartmentId, month, role);
    }

    private async Task<IActionResult> GenerateInvoiceAsync(
        string buildingId, string apartmentId, string? month, string role)
    {
        var monthKey = string.IsNullOrWhiteSpace(month)
            ? await _invoice.GetCurrentPeriodKeyAsync(buildingId, HttpContext.RequestAborted)
            : month;
        var data = await _invoice.BuildInvoiceDataAsync(
            buildingId, apartmentId, monthKey, HttpContext.RequestAborted);
        if (data is null) return NotFound();

        var pdfBytes = _pdf.Generate(data);
        await _audit.LogAsync(
            action: "invoice_download",
            buildingId: data.Building.Id,
            apartmentId: data.Apt.Id,
            userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
            userRole: role,
            metadata: new
            {
                aptNumber = data.Apt.Number,
                invoiceNo = data.InvoiceNo,
                monthKey,
                actor = User.Identity?.Name ?? role
            },
            severity: "info");

        var fileName = $"invoice-{data.Building.BuildingNumber}-apt-{data.Apt.Number}-{monthKey}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
}