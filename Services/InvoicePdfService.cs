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
                            c.Item().Text(Loc.T("Building_Number_N", d.Building.BuildingNumber)).FontSize(9).FontColor("#FFFFFF");
                        });
                        row.ConstantItem(160).Column(c =>
                        {
                            c.Item().AlignRight().Text(Loc.T("Wallet_Invoice")).FontSize(9).FontColor("#FFFFFF");
                            c.Item().AlignRight().Text(d.MonthLabelText).FontSize(14).Bold().FontColor("#FFFFFF");
                        });
                    });

                    // -------- Meta --------
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(Loc.T("Invoice_Number_N", d.InvoiceNo)).FontSize(9);
                        row.RelativeItem().AlignRight().Text(Loc.T("Issue_Date_N", d.IssueDate)).FontSize(9);
                    });

                    // -------- Apartment info --------
                    col.Item().Border(1).BorderColor("#DDDDDD").Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(c => { c.Item().Text(Loc.T("Apartment_Number")).FontSize(8).FontColor("#666666"); c.Item().Text(Loc.T("Apartment_N", d.Apt.Number)).Bold(); });
                        row.RelativeItem().Column(c => { c.Item().Text(Loc.T("Role")).FontSize(8).FontColor("#666666"); c.Item().Text(d.Floor?.Label ?? "—").Bold(); });
                        row.RelativeItem().Column(c => { c.Item().Text(Loc.T("Resident_Name")).FontSize(8).FontColor("#666666"); c.Item().Text(string.IsNullOrWhiteSpace(d.Apt.Owner) ? "—" : d.Apt.Owner).Bold(); });
                        row.RelativeItem().Column(c => { c.Item().Text(Loc.T("WhatsApp_Number")).FontSize(8).FontColor("#666666"); c.Item().Text(string.IsNullOrWhiteSpace(d.Apt.Phone) ? "—" : d.Apt.Phone).Bold(); });
                    });

                    // -------- Wallet summary --------
                    col.Item().Border(1).BorderColor("#DDDDDD").Padding(10).Column(c =>
                    {
                        c.Item().Text(Loc.T("Wallet_Summary")).Bold().FontSize(12);

                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text(Loc.T("Previous_Balance"));
                            r.RelativeItem().AlignRight().Text(Loc.T("N_EGP_2", d.PreviousBalance));
                        });
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text(Loc.T("Deposits_This_Month"));
                            r.RelativeItem().AlignRight().Text(Loc.T("N_EGP_3", d.MonthDeposits)).FontColor("#2E7D5B");
                        });
                        if (d.MonthRevenuesShare > 0)
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text(Loc.T("Your_Share_Of_Revenues"));
                                r.RelativeItem().AlignRight().Text(Loc.T("N_EGP_3", d.MonthRevenuesShare)).FontColor("#1F6B2E");
                            });
                        }
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text(Loc.T("Your_Share_Of_Expenses"));
                            r.RelativeItem().AlignRight().Text(Loc.T("N_EGP_4", d.MonthExpensesShare)).FontColor("#A0432A");
                        });
                        c.Item().PaddingTop(6).BorderTop(1).BorderColor("#B9853B").PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text(Loc.T("Current_Balance_2")).Bold();
                            r.RelativeItem().AlignRight().Text(Loc.T("N_EGP_2", d.Balance)).Bold().FontSize(14)
                                .FontColor(d.Balance >= 0 ? "#2E7D5B" : "#A0432A");
                        });
                    });

                    // -------- Category breakdown --------
                    if (d.CategoryShares.Count > 0)
                    {
                        col.Item().Border(1).BorderColor("#DDDDDD").Padding(10).Column(c =>
                        {
                            c.Item().Text(Loc.T("Expense_Breakdown")).Bold().FontSize(12);
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
                            c.Item().Text(Loc.T("Transfer_Details_For_Payment")).Bold();
                            if (!string.IsNullOrWhiteSpace(d.Payment.Label)) c.Item().Text(Loc.T("Method_N", d.Payment.Label)).FontSize(9);
                            if (!string.IsNullOrWhiteSpace(d.Payment.AccountNumber)) c.Item().Text(Loc.T("Account_Number_N", d.Payment.AccountNumber)).FontSize(9);
                            if (!string.IsNullOrWhiteSpace(d.Payment.Phone)) c.Item().Text(Loc.T("Phone_Number_N", d.Payment.Phone)).FontSize(9);
                            if (!string.IsNullOrWhiteSpace(d.Payment.Notes)) c.Item().Text(Loc.T("Note_N", d.Payment.Notes)).FontSize(9);
                        });
                    }

                    // -------- Footer + QR --------
                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(Loc.T("N_Building_Management_System", d.Building.Name)).Bold().FontSize(10);
                            c.Item().Text(Loc.T("This_Is_An_Electronic_Invoice_Issued")).FontSize(8).FontColor("#666666");
                            c.Item().Text(Loc.T("Invoice_Number_N", d.InvoiceNo)).FontSize(8);
                        });
                        row.ConstantItem(90).Column(c =>
                        {
                            c.Item().Width(80).Height(80).Image(qrPng);
                            c.Item().AlignCenter().Text(Loc.T("Scan_To_Verify")).FontSize(7);
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}