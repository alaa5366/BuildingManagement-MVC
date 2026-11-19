using System.Text.Json;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Builds invoice data from target financial periods, allocations and immutable ledger entries.</summary>
public sealed class InvoiceService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TargetWalletReadService _wallet;

    public InvoiceService(IDbContextFactory<AppDbContext> factory, TargetWalletReadService wallet)
    {
        _factory = factory;
        _wallet = wallet;
    }

    public async Task<string?> FindBuildingIdByApartmentAsync(string apartmentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apartmentId)) return null;
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var matches = await db.Apartments.AsNoTracking()
            .Where(item => item.Id == apartmentId)
            .Select(item => item.BuildingId)
            .Take(2)
            .ToListAsync(cancellationToken);
        return matches.Count == 1 ? matches[0] : null;
    }

    public async Task<InvoiceData?> BuildInvoiceDataAsync(
        string buildingId, string apartmentId, string monthKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId)) throw new ArgumentException("Building is required.", nameof(buildingId));
        if (!DateOnly.TryParseExact(monthKey + "-01", "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
            throw new ArgumentException("Month must use yyyy-MM format.", nameof(monthKey));

        var building = await _wallet.GetBuildingShellAsync(buildingId, apartmentId, cancellationToken);
        var apartment = building?.Apartments.SingleOrDefault(item => item.Id == apartmentId);
        if (building is null || apartment is null) return null;
        var floor = building.Floors.FirstOrDefault(item => item.Id == apartment.FloorId);

        var previousMonth = ReportsService.ShiftMonth(monthKey, -1);
        var currentSnapshot = await _wallet.GetApartmentAsync(buildingId, apartmentId, monthKey, monthKey,
            cancellationToken: cancellationToken);
        var previousSnapshot = await _wallet.GetApartmentAsync(buildingId, apartmentId, null, previousMonth,
            cancellationToken: cancellationToken);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var periodIds = await db.FinancialPeriods.AsNoTracking()
            .Where(period => period.BuildingId == buildingId && period.PeriodKey == monthKey)
            .Select(period => period.PeriodId).ToListAsync(cancellationToken);

        var currencyCode = await db.BuildingSettings.AsNoTracking()
            .Where(settings => settings.BuildingId == buildingId)
            .Select(settings => settings.Currency).SingleOrDefaultAsync(cancellationToken) ?? "EGP";
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3 ||
            currencyCode.Any(character => !char.IsAsciiLetter(character)))
            throw new InvalidOperationException("Building currency configuration is invalid.");
        currencyCode = currencyCode.ToUpperInvariant();

        var monthDeposits = await db.PaymentDeposits.AsNoTracking()
            .Where(deposit => deposit.BuildingId == buildingId && periodIds.Contains(deposit.PeriodId) &&
                deposit.ApartmentId == apartmentId && deposit.CurrencyCode == currencyCode &&
                deposit.Status == PaymentDepositStatus.Confirmed)
            .SumAsync(deposit => (decimal?)deposit.Amount, cancellationToken) ?? 0m;

        var activeExpenseRows = await db.Expenses.AsNoTracking()
            .Where(item => item.BuildingId == buildingId && item.MonthKey == monthKey)
            .ToListAsync(cancellationToken);
        var activeRevenueRows = await db.Revenues.AsNoTracking()
            .Where(item => item.BuildingId == buildingId && item.MonthKey == monthKey)
            .ToListAsync(cancellationToken);

        var expenseTransactions = await db.LedgerTransactions.AsNoTracking()
            .Where(item => item.BuildingId == buildingId && periodIds.Contains(item.PeriodId) &&
                item.SourceSystem == "application" && item.SourceType == "Expense" &&
                item.Kind == LedgerTransactionKind.Expense && item.Status == LedgerTransactionStatus.Posted)
            .ToListAsync(cancellationToken);
        var revenueTransactions = await db.LedgerTransactions.AsNoTracking()
            .Where(item => item.BuildingId == buildingId && periodIds.Contains(item.PeriodId) &&
                item.SourceSystem == "application" && item.SourceType == "Revenue" &&
                item.Kind == LedgerTransactionKind.Revenue && item.Status == LedgerTransactionStatus.Posted)
            .ToListAsync(cancellationToken);

        var originalIds = expenseTransactions.Concat(revenueTransactions)
            .Select(item => item.LedgerTransactionId).ToList();
        var reversedIds = await db.LedgerTransactions.AsNoTracking()
            .Where(item => item.BuildingId == buildingId && item.Kind == LedgerTransactionKind.Reversal &&
                item.Status == LedgerTransactionStatus.Posted && item.ReversesTransactionId.HasValue &&
                originalIds.Contains(item.ReversesTransactionId.Value))
            .Select(item => item.ReversesTransactionId!.Value).ToListAsync(cancellationToken);
        var reversedSet = reversedIds.ToHashSet();
        expenseTransactions = expenseTransactions.Where(item => !reversedSet.Contains(item.LedgerTransactionId)).ToList();
        revenueTransactions = revenueTransactions.Where(item => !reversedSet.Contains(item.LedgerTransactionId)).ToList();

        var transactionIds = expenseTransactions.Concat(revenueTransactions)
            .Select(item => item.LedgerTransactionId).ToList();
        var batches = await db.FinancialAllocationBatches.AsNoTracking()
            .Where(batch => batch.BuildingId == buildingId && transactionIds.Contains(batch.SourceTransactionId) &&
                batch.Status == FinancialAllocationStatus.Completed)
            .ToListAsync(cancellationToken);
        var batchIds = batches.Select(batch => batch.AllocationBatchId).ToList();
        var lines = await db.FinancialAllocationLines.AsNoTracking()
            .Where(line => line.BuildingId == buildingId && batchIds.Contains(line.AllocationBatchId))
            .ToListAsync(cancellationToken);
        var apartmentLines = lines.Where(line => line.ApartmentId == apartmentId).ToList();
        var batchByTransaction = batches.ToDictionary(batch => batch.SourceTransactionId);

        decimal expenseShare = 0, revenueShare = 0;
        var categoryShares = new List<CategoryShare>();
        var categories = await db.FinancialCategories.AsNoTracking()
            .Where(category => category.BuildingId == buildingId && category.Kind == "expense")
            .ToListAsync(cancellationToken);
        var categoryById = categories.ToDictionary(category => category.Id, StringComparer.Ordinal);
        var expenseRowsById = activeExpenseRows.ToDictionary(item => NormalizeSourceKey(item.Id), StringComparer.OrdinalIgnoreCase);
        var categoryTotals = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var categoryApartmentShares = new Dictionary<string, decimal>(StringComparer.Ordinal);

        foreach (var transaction in expenseTransactions)
        {
            var sourceId = NormalizeSourceKey(transaction.SourceKey);
            if (!expenseRowsById.TryGetValue(sourceId, out var expense) ||
                !batchByTransaction.TryGetValue(transaction.LedgerTransactionId, out var batch))
                continue;
            var share = apartmentLines.Where(line => line.AllocationBatchId == batch.AllocationBatchId)
                .Sum(line => line.AllocatedAmount);
            expenseShare += share;
            var categoryId = expense.CategoryId ?? "";
            categoryTotals[categoryId] = categoryTotals.GetValueOrDefault(categoryId) + expense.Amount;
            categoryApartmentShares[categoryId] = categoryApartmentShares.GetValueOrDefault(categoryId) + share;
        }

        foreach (var transaction in revenueTransactions)
        {
            if (!batchByTransaction.TryGetValue(transaction.LedgerTransactionId, out var batch)) continue;
            revenueShare += apartmentLines.Where(line => line.AllocationBatchId == batch.AllocationBatchId)
                .Sum(line => line.AllocatedAmount);
        }

        var openCount = building.Apartments.Count(item => !item.Closed && !item.Disabled);
        foreach (var categoryTotal in categoryTotals)
        {
            categoryById.TryGetValue(categoryTotal.Key, out var category);
            categoryShares.Add(new CategoryShare
            {
                Name = category?.Name ?? "—",
                ColorHex = category?.Color ?? "#888888",
                Total = categoryTotal.Value,
                AptCount = openCount,
                Share = categoryApartmentShares.GetValueOrDefault(categoryTotal.Key)
            });
        }

        var random = Random.Shared.Next(0, 1000).ToString("D3");
        var invoiceNo = $"INV-{building.BuildingNumber}-{apartment.Number}-{monthKey.Replace("-", "")}-{random}";
        var issueDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        var qrPayload = JsonSerializer.Serialize(new
        {
            v = "3.2",
            no = invoiceNo,
            bld = building.BuildingNumber,
            apt = apartment.Number,
            month = monthKey,
            balance = currentSnapshot.Balance
        });

        return new InvoiceData
        {
            InvoiceNo = invoiceNo,
            IssueDate = issueDate,
            Building = building,
            Apt = apartment,
            Floor = floor,
            Balance = currentSnapshot.Balance,
            PreviousBalance = previousSnapshot.Balance,
            MonthDeposits = monthDeposits,
            MonthExpensesShare = expenseShare,
            MonthRevenuesShare = revenueShare,
            MonthFee = apartment.MonthlyFee,
            CategoryShares = categoryShares.OrderBy(item => item.Name, StringComparer.CurrentCulture).ToList(),
            Payment = building.PaymentInfo,
            MonthKey = monthKey,
            MonthLabelText = ReportsService.MonthLabel(monthKey),
            QrPayload = qrPayload
        };
    }

    private static string NormalizeSourceKey(string? key) =>
        Guid.TryParse(key, out var id) ? id.ToString("N") : key ?? "";
}
