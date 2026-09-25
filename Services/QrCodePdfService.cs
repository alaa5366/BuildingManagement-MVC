using BuildingManagementMvc.Models;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BuildingManagementMvc.Services;

public class QrCodePdfService
{
    public byte[] GenerateQrCodesPdf(Building building, string baseUrl)
    {
        var document = Document.Create(container =>
        {
            // الصفحة الرئيسية
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Cairo"));

                page.Header().Column(col =>
                {
                    col.Item().Text($"📱 QR Codes — {building.Name}")
                        .FontSize(20).Bold().FontColor("#12433C");
                    col.Item().Text($"{building.BuildingNumber}")
                        .FontSize(12).FontColor("#666");
                    col.Item().PaddingTop(5).LineHorizontal(2).LineColor("#12433C");
                });

                page.Content().Column(col =>
                {
                    col.Spacing(15);

                    // QR الأدمن
                    col.Item().Text("👑 أدمن العمارة").FontSize(14).Bold();
                    var adminUrl = $"{baseUrl}/Account/LoginAdmin?bld={building.Id}";
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(120).Image(GenerateQrBytes(adminUrl));
                        row.RelativeItem().PaddingLeft(10).Column(c =>
                        {
                            c.Item().Text($"العمارة: {building.Name}").FontSize(11).Bold();
                            c.Item().Text($"الرقم: {building.BuildingNumber}").FontSize(10);
                            c.Item().Text($"الرابط:").FontSize(9).FontColor("#666");
                            c.Item().Text(adminUrl).FontSize(7).FontColor("#0066CC");
                        });
                    });

                    col.Item().PaddingTop(20).LineHorizontal(1).LineColor("#DDDDDD");

                    // QR الشقق
                    col.Item().Text($"🏠 الشقق ({building.Apartments.Count})").FontSize(14).Bold();

                    foreach (var floor in building.Floors.OrderBy(f => f.Order))
                    {
                        var apts = building.Apartments
                            .Where(a => a.FloorId == floor.Id)
                            .OrderBy(a => a.Number)
                            .ToList();

                        if (apts.Count == 0) continue;

                        col.Item().PaddingTop(10).Text($"🏢 {floor.Label}")
                            .FontSize(12).Bold().FontColor("#12433C");

                        // عرض 3 QRs في صف
                        for (int i = 0; i < apts.Count; i += 3)
                        {
                            var batch = apts.Skip(i).Take(3).ToList();
                            col.Item().Row(row =>
                            {
                                foreach (var apt in batch)
                                {
                                    var aptUrl = $"{baseUrl}/Account/LoginResident?apt={building.Id}_{apt.Id}";
                                    row.RelativeItem().Padding(5).Column(c =>
                                    {
                                        c.Item().AlignCenter().Width(120).Height(120)
                                            .Image(GenerateQrBytes(aptUrl));
                                        c.Item().AlignCenter().Text($"🏠 شقة {apt.Number}")
                                            .FontSize(10).Bold();
                                        if (!string.IsNullOrEmpty(apt.Owner))
                                            c.Item().AlignCenter().Text(apt.Owner)
                                                .FontSize(8).FontColor("#666");
                                        if (apt.Closed)
                                            c.Item().AlignCenter().Text("🔒 مغلقة")
                                                .FontSize(8).FontColor("#A0432A");
                                    });
                                }

                                // فراغات للمحاذاة
                                for (int j = batch.Count; j < 3; j++)
                                {
                                    row.RelativeItem();
                                }
                            });
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("نظام إدارة العمارات").FontSize(8).FontColor("#666");
                    text.Span(" | ").FontSize(8).FontColor("#666");
                    text.Span($"تم الإنشاء: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor("#666");
                });
            });
        });

        return document.GeneratePdf();
    }

    private byte[] GenerateQrBytes(string data)
    {
        using var generator = new QRCodeGenerator();
        var qrData = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
        return new PngByteQRCode(qrData).GetGraphic(10);
    }
}