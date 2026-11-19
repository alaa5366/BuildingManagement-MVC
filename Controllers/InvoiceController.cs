﻿using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
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

    /// <summary>Displays the QR code for the same immutable monthly invoice.</summary>
    [HttpGet]
    public async Task<IActionResult> Qr(string? aptId, string? month)
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

        var monthKey = string.IsNullOrWhiteSpace(month)
            ? await _invoice.GetCurrentPeriodKeyAsync(buildingId, HttpContext.RequestAborted)
            : month;
        var data = await _invoice.BuildInvoiceDataAsync(
            buildingId, targetApartmentId, monthKey, HttpContext.RequestAborted);
        if (data is null) return NotFound();

        byte[] qrPng;
        using (var generator = new QRCodeGenerator())
        {
            var qrData = generator.CreateQrCode(data.QrPayload, QRCodeGenerator.ECCLevel.M);
            qrPng = new PngByteQRCode(qrData).GetGraphic(12);
        }

        var role = isResident ? "resident" : isSuperAdmin ? "superadmin" : "admin";
        await _audit.LogAsync(
            action: "invoice_qr_view",
            buildingId: data.Building.Id,
            apartmentId: data.Apt.Id,
            userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
            userRole: role,
            metadata: new { aptNumber = data.Apt.Number, invoiceNo = data.InvoiceNo, monthKey },
            severity: "info");

        return File(qrPng, "image/png");
    }

    /// <summary>Opens a prefilled WhatsApp message so the building administrator can notify the resident about the issued invoice.</summary>
    [HttpGet]
    public async Task<IActionResult> WhatsApp(string aptId, string? month)
    {
        if (!User.IsInRole("admin") && !User.IsInRole("superadmin"))
            return Forbid();
        if (string.IsNullOrWhiteSpace(aptId))
            return NotFound();

        var buildingId = User.FindFirstValue("buildingId");
        if (string.IsNullOrWhiteSpace(buildingId))
        {
            if (!User.IsInRole("superadmin")) return NotFound();
            buildingId = await _invoice.FindBuildingIdByApartmentAsync(aptId, HttpContext.RequestAborted);
            if (string.IsNullOrWhiteSpace(buildingId)) return NotFound();
        }

        var monthKey = string.IsNullOrWhiteSpace(month)
            ? await _invoice.GetCurrentPeriodKeyAsync(buildingId, HttpContext.RequestAborted)
            : month;
        var data = await _invoice.BuildInvoiceDataAsync(
            buildingId, aptId, monthKey, HttpContext.RequestAborted);
        if (data is null) return NotFound();

        var phone = AuthHelpers.NormalizePhone(data.Apt.Phone);
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest("لا يوجد رقم هاتف مسجل لهذه الشقة.");

        var loginUrl = Url.Action("UnifiedLogin", "Account", values: null, protocol: Request.Scheme)!;
        var message =
            $"مرحباً {data.Apt.Owner}،\n" +
            $"فاتورة العمارة {data.Building.Name} â€” الشقة رقم {data.Apt.Number}\n" +
            $"الشهر: {data.MonthLabelText}\n" +
            $"رقم الفاتورة: {data.InvoiceNo}\n\n" +
            $"للاطلاع على الفاتورة PDF، سجّل الدخول بحساب الساكن من الرابط التالي:\n{loginUrl}\n\n" +
            "هذه رسالة إشعار فقط؛ لا تشارك بيانات الدخول مع أي شخص.";
        var waLink = WhatsAppTemplateService.BuildWaLink(phone, message);

        await _audit.LogAsync(
            action: "invoice_whatsapp_draft",
            buildingId: data.Building.Id,
            apartmentId: data.Apt.Id,
            userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
            userRole: User.IsInRole("superadmin") ? "superadmin" : "admin",
            metadata: new { aptNumber = data.Apt.Number, invoiceNo = data.InvoiceNo, monthKey, phoneSuffix = phone.Length > 4 ? phone[^4..] : phone },
            severity: "info");

        return Redirect(waLink);
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

        // The issued invoice number is immutable for this apartment and month;
        // using it as the download name makes reprints visibly traceable to the same invoice.
        var safeInvoiceNo = System.Text.RegularExpressions.Regex.Replace(
            data.InvoiceNo ?? string.Empty, @"[^A-Za-z0-9._-]", "_");
        if (string.IsNullOrWhiteSpace(safeInvoiceNo))
            safeInvoiceNo = $"invoice-{data.Building.BuildingNumber}-apt-{data.Apt.Number}-{monthKey}";
        var fileName = $"{safeInvoiceNo}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
}
