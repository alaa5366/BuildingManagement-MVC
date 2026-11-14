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
    private readonly IAuditLogger _audit;

    public InvoiceController(BuildingsService buildings, InvoiceService invoice,
        InvoicePdfService pdf, IAuditLogger audit)
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
            // SuperAdmin: نجيب العمارة من الشقة بدل ما نحمّل كل العمارات
            var foundBuildingId = await _buildings.FindBuildingIdByApartmentAsync(aptId);
            if (string.IsNullOrEmpty(foundBuildingId)) return NotFound();

            var foundBuilding = await _buildings.GetByIdAsync(foundBuildingId);
            var foundApt = foundBuilding?.Apartments.FirstOrDefault(a => a.Id == aptId);

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

        // تسجيل في جدول الـ Audit مباشرة (من غير ما نحفظ العمارة كلها في طلب GET)
        await _audit.LogAsync(
            action: "invoice_download",
            buildingId: building.Id,
            apartmentId: apt.Id,
            userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
            userRole: role,
            metadata: new { aptNumber = apt.Number, invoiceNo = data.InvoiceNo, actor = User.Identity?.Name ?? role },
            severity: "info");

        var fileName = $"invoice-{building.BuildingNumber}-apt-{apt.Number}-{mk}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
}