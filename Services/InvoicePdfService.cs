using BuildingManagementMvc.Models;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BuildingManagementMvc.Services;

public class InvoicePdfService
{
    public byte[] Generate(InvoiceData d)
    {
        byte[] qrPng;
        using (var generator = new QRCodeGenerator())
        {
            var qrData = generator.CreateQrCode(d.QrPayload, QRCodeGenerator.ECCLevel.M);
            qrPng = new PngByteQRCode(qrData).GetGraphic(10);
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                // ✅ السطر الأهم
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Cairo"));

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    // -------- Header --------
                    col.Item().Background("#12433C").Padding(14).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(d.Building.Name).FontSize(16).Bold().FontColor("#FFFFFF");
                            c.Item().Text($"رقم العمارة: {d.Building.BuildingNumber}").FontSize(9).FontColor("#FFFFFF");
                        });
                        row.ConstantItem(160).Column(c =>
                        {
                            c.Item().AlignRight().Text("فاتورة محفظة").FontSize(9).FontColor("#FFFFFF");
                            c.Item().AlignRight().Text(d.MonthLabelText).FontSize(14).Bold().FontColor("#FFFFFF");
                        });
                    });

                    // -------- Meta --------
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"رقم الفاتورة: {d.InvoiceNo}").FontSize(9);
                        row.RelativeItem().AlignRight().Text($"تاريخ الإصدار: {d.IssueDate}").FontSize(9);
                    });

                    // -------- Apartment info --------
                    col.Item().Border(1).BorderColor("#DDDDDD").Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(c => { c.Item().Text("رقم الشقة").FontSize(8).FontColor("#666666"); c.Item().Text($"شقة {d.Apt.Number}").Bold(); });
                        row.RelativeItem().Column(c => { c.Item().Text("الدور").FontSize(8).FontColor("#666666"); c.Item().Text(d.Floor?.Label ?? "—").Bold(); });
                        row.RelativeItem().Column(c => { c.Item().Text("اسم الساكن").FontSize(8).FontColor("#666666"); c.Item().Text(string.IsNullOrWhiteSpace(d.Apt.Owner) ? "—" : d.Apt.Owner).Bold(); });
                        row.RelativeItem().Column(c => { c.Item().Text("رقم الواتساب").FontSize(8).FontColor("#666666"); c.Item().Text(string.IsNullOrWhiteSpace(d.Apt.Phone) ? "—" : d.Apt.Phone).Bold(); });
                    });

                    // -------- Wallet summary --------
                    col.Item().Border(1).BorderColor("#DDDDDD").Padding(10).Column(c =>
                    {
                        c.Item().Text("ملخص المحفظة").Bold().FontSize(12);

                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text("الرصيد السابق");
                            r.RelativeItem().AlignRight().Text($"{d.PreviousBalance:0.##} ج.م");
                        });
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("+ إيداعات هذا الشهر");
                            r.RelativeItem().AlignRight().Text($"+{d.MonthDeposits:0.##} ج.م").FontColor("#2E7D5B");
                        });
                        if (d.MonthRevenuesShare > 0)
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("+ نصيبك من الإيرادات");
                                r.RelativeItem().AlignRight().Text($"+{d.MonthRevenuesShare:0.##} ج.م").FontColor("#1F6B2E");
                            });
                        }
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("- نصيبك من المصروفات");
                            r.RelativeItem().AlignRight().Text($"-{d.MonthExpensesShare:0.##} ج.م").FontColor("#A0432A");
                        });
                        c.Item().PaddingTop(6).BorderTop(1).BorderColor("#B9853B").PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text("= الرصيد الحالي").Bold();
                            r.RelativeItem().AlignRight().Text($"{d.Balance:0.##} ج.م").Bold().FontSize(14)
                                .FontColor(d.Balance >= 0 ? "#2E7D5B" : "#A0432A");
                        });
                    });

                    // -------- Category breakdown --------
                    if (d.CategoryShares.Count > 0)
                    {
                        col.Item().Border(1).BorderColor("#DDDDDD").Padding(10).Column(c =>
                        {
                            c.Item().Text("تفصيل المصروفات").Bold().FontSize(12);
                            c.Item().PaddingTop(6).Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    cd.RelativeColumn(2);
                                    cd.RelativeColumn(1);
                                    cd.RelativeColumn(1);
                                    cd.RelativeColumn(1);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Text("البند").Bold().FontSize(9);
                                    h.Cell().Text("الإجمالي").Bold().FontSize(9);
                                    h.Cell().Text("عدد الشقق").Bold().FontSize(9);
                                    h.Cell().Text("نصيبك").Bold().FontSize(9);
                                });

                                foreach (var cs in d.CategoryShares)
                                {
                                    table.Cell().Text(cs.Name).FontSize(9);
                                    table.Cell().Text($"{cs.Total:0.##}").FontSize(9);
                                    table.Cell().Text(cs.AptCount.ToString()).FontSize(9);
                                    table.Cell().Text($"{cs.Share:0.##}").FontSize(9).FontColor("#A0432A");
                                }
                            });
                        });
                    }

                    // -------- Payment info --------
                    if (!string.IsNullOrWhiteSpace(d.Payment.AccountNumber) || !string.IsNullOrWhiteSpace(d.Payment.Phone))
                    {
                        col.Item().Background("#DCEAE5").Padding(10).Column(c =>
                        {
                            c.Item().Text("بيانات التحويل للدفع").Bold();
                            if (!string.IsNullOrWhiteSpace(d.Payment.Label)) c.Item().Text($"الطريقة: {d.Payment.Label}").FontSize(9);
                            if (!string.IsNullOrWhiteSpace(d.Payment.AccountNumber)) c.Item().Text($"رقم الحساب: {d.Payment.AccountNumber}").FontSize(9);
                            if (!string.IsNullOrWhiteSpace(d.Payment.Phone)) c.Item().Text($"رقم الهاتف: {d.Payment.Phone}").FontSize(9);
                            if (!string.IsNullOrWhiteSpace(d.Payment.Notes)) c.Item().Text($"ملاحظة: {d.Payment.Notes}").FontSize(9);
                        });
                    }

                    // -------- Footer + QR --------
                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"{d.Building.Name} — نظام إدارة العمارات").Bold().FontSize(10);
                            c.Item().Text("هذه فاتورة إلكترونية صادرة من نظام إدارة العمارات").FontSize(8).FontColor("#666666");
                            c.Item().Text($"رقم الفاتورة: {d.InvoiceNo}").FontSize(8);
                        });
                        row.ConstantItem(90).Column(c =>
                        {
                            c.Item().Width(80).Height(80).Image(qrPng);
                            c.Item().AlignCenter().Text("امسح للتحقق").FontSize(7);
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}