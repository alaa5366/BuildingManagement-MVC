using System.Text.Json;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية للجزء الحسابي من js/features/invoice.js (من غير html2canvas/jsPDF)
public class InvoiceService
{
    private readonly WalletService _wallet;
    public InvoiceService(WalletService wallet) => _wallet = wallet;

    public InvoiceData BuildInvoiceData(Building building, Apartment apt, string monthKey)
    {
        var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);
        var m = _wallet.GetOrCreateMonth(building, monthKey);

        var balance = _wallet.ComputeWalletBalance(building, apt.Id, monthKey);
        var previousBalance = 0.0; // نفس computePreviousBalance في الأصل (بترجع 0 دايمًا حاليًا)
        var monthDeposits = _wallet.TotalConfirmedDeposits(building, apt.Id, monthKey);
        var monthExpenses = m.Distribution.TryGetValue(apt.Id, out var dist) ? dist : 0;
        var monthRevenues = m.RevenueDistribution.TryGetValue(apt.Id, out var rev) ? rev : 0;

        var openCount = building.Apartments.Count(a => !a.Closed);
        var catShares = building.ExpenseCategories.Select(cat =>
        {
            var total = m.Expenses.Where(e => e.CategoryId == cat.Id).Sum(e => e.Amount);
            var share = openCount > 0 ? total / openCount : 0;
            return new CategoryShare { Name = cat.Name, ColorHex = cat.Color, Total = total, AptCount = openCount, Share = share };
        }).Where(x => x.Total > 0).ToList();

        var rnd = Random.Shared.Next(0, 1000).ToString("D3");
        var invoiceNo = $"INV-{building.BuildingNumber}-{apt.Number}-{monthKey.Replace("-", "")}-{rnd}";
        var issueDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        var qrPayload = JsonSerializer.Serialize(new
        {
            v = "3.2",
            no = invoiceNo,
            bld = building.BuildingNumber,
            apt = apt.Number,
            month = monthKey,
            balance
        });

        return new InvoiceData
        {
            InvoiceNo = invoiceNo,
            IssueDate = issueDate,
            Building = building,
            Apt = apt,
            Floor = floor,
            Balance = balance,
            PreviousBalance = previousBalance,
            MonthDeposits = monthDeposits,
            MonthExpensesShare = monthExpenses,
            MonthRevenuesShare = monthRevenues,
            CategoryShares = catShares,
            Payment = building.PaymentInfo,
            MonthKey = monthKey,
            MonthLabelText = ReportsService.MonthLabel(monthKey),
            QrPayload = qrPayload
        };
    }
}
