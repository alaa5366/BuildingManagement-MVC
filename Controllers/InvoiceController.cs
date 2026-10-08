using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

// نفس شاشة js/features/invoice.js — توليد فاتورة PDF فيها QR للتحقق
[Authorize(Roles = "superadmin,admin,resident")]
public class InvoiceController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly InvoiceService _invoice;
    private readonly InvoicePdfService _pdf;
    private readonly AuditLogService _audit;

    public InvoiceController(BuildingsService buildings, InvoiceService invoice,
        InvoicePdfService pdf, AuditLogService audit)
    {
        _buildings = buildings;
        _invoice = invoice;
        _pdf = pdf;
        _audit = audit;
    }

    // aptId اختياري: الأدمن يحدده لأي شقة في عمارته، الساكن بياخد شقته هو بس دايمًا
    // SuperAdmin: لازم يحدد aptId + buildingId (عن طريق Impersonation أو مباشرة)
    [HttpGet]
    public async Task<IActionResult> Download(string? aptId, string? month)
    {
        // ✅ نحدد buildingId من الـ claims
        var buildingId = User.FindFirstValue("buildingId");

        // ✅ لو مش موجود (SuperAdmin من غير Impersonation) → نستخدم aptId للبحث
        if (string.IsNullOrEmpty(buildingId) && !string.IsNullOrEmpty(aptId))
        {
            // SuperAdmin: نبحث عن الشقة في كل العمارات
            var allBuildings = await _buildings.GetAllAsync();
            Building? foundBuilding = null;
            Apartment? foundApt = null;

            foreach (var b in allBuildings)
            {
                var apt = b.Apartments.FirstOrDefault(a => a.Id == aptId);
                if (apt != null)
                {
                    foundBuilding = b;
                    foundApt = apt;
                    break;
                }
            }

            if (foundBuilding == null || foundApt == null) return NotFound();

            return await GenerateInvoiceAsync(foundBuilding, foundApt, month, "superadmin");
        }

        // الأدمن أو الساكن
        if (string.IsNullOrEmpty(buildingId)) return NotFound();

        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        var isResident = User.IsInRole("resident");
        var targetAptId = isResident ? User.FindFirstValue("apartmentId") : aptId;

        if (string.IsNullOrEmpty(targetAptId)) return NotFound();

        var targetApt = building.Apartments.FirstOrDefault(a => a.Id == targetAptId);
        if (targetApt == null) return NotFound();

        var role = isResident ? "resident" : "admin";
        return await GenerateInvoiceAsync(building, targetApt, month, role);
    }

    private async Task<IActionResult> GenerateInvoiceAsync(Building building, Apartment apt,
        string? month, string role)
    {
        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;

        var data = _invoice.BuildInvoiceData(building, apt, mk);
        var pdfBytes = _pdf.Generate(data);

        _audit.Push(building, "invoice_download",
            $"شقة {apt.Number} — {data.InvoiceNo}",
            role,
            User.Identity?.Name ?? role,
            apt.Number.ToString());

        await _buildings.SaveFullAsync(building);

        var fileName = $"invoice-{building.BuildingNumber}-apt-{apt.Number}-{mk}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
}