using BuildingManagementMvc.Models;
using System.Text.Json;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية للجزء الحسابي من js/features/invoice.js (من غير html2canvas/jsPDF)
/// <summary>
/// Builds invoice data (balances, shares, deposits) for an apartment and month.
/// </summary>
public class InvoiceService
{
    private readonly TargetWalletReadService _targetWallet;
    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceService"/> class.
    /// </summary>
    public InvoiceService(TargetWalletReadService targetWallet) => _targetWallet = targetWallet;

    /// <summary>
    /// Builds invoice data from the canonical ledger without creating or mutating legacy month aggregates.
    /// </summary>
    public async Task<InvoiceData> BuildInvoiceDataAsync(
        Building building, Apartment apt, string monthKey, CancellationToken cancellationToken = default)
    {
        var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);
        var wallet = await _targetWallet.GetApartmentAsync(
            building.Id, apt.Id, monthKey, monthKey, cancellationToken: cancellationToken);
        var previousMonth = ReportsService.ShiftMonth(monthKey, -1);
        var previousWallet = await _targetWallet.GetApartmentAsync(
            building.Id, apt.Id, toMonth: previousMonth, cancellationToken: cancellationToken);

        var balance = wallet.Balance;
        var previousBalance = previousWallet.Balance;
        var monthDeposits = wallet.Transactions
            .Where(item => item.Type == "deposit" && item.Status == "confirmed")
            .Sum(item => item.Amount);
        var monthExpenses = Math.Abs(wallet.Transactions
            .Where(item => item.Type == "expense")
            .Sum(item => item.Amount));
        var monthRevenues = wallet.Transactions
            .Where(item => item.Type == "revenue")
            .Sum(item => item.Amount);

        // Category labels remain sourced from the category catalog; the monetary
        // invoice totals above are ledger-derived and do not depend on legacy wallet calculations.
        building.Months.TryGetValue(monthKey, out var legacyMonth);
        var monthExpensesByCategory = legacyMonth?.Expenses ?? new List<Expense>();
        var openCount = building.Apartments.Count(a => !a.Closed);
        var catShares = building.ExpenseCategories.Select(cat =>
        {
            var total = monthExpensesByCategory.Where(e => e.CategoryId == cat.Id).Sum(e => e.Amount);
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
            MonthFee = apt.MonthlyFee,  
            CategoryShares = catShares,
            Payment = building.PaymentInfo,
            MonthKey = monthKey,
            MonthLabelText = ReportsService.MonthLabel(monthKey),
            QrPayload = qrPayload
        };
    }
}
