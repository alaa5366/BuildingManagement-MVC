using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Builds financial reports exclusively from the canonical target finance schema.</summary>
public sealed class ReportsService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TargetWalletReadService _wallet;
    private readonly TargetFinanceReadService _finance;

    public ReportsService(IDbContextFactory<AppDbContext> factory, TargetWalletReadService wallet, TargetFinanceReadService finance)
    {
        _factory = factory;
        _wallet = wallet;
        _finance = finance;
    }

    private static string[] MonthNamesAr =>
    [
        Loc.T("January"), Loc.T("February"), Loc.T("March"), Loc.T("April"),
        Loc.T("May"), Loc.T("June"), Loc.T("July"), Loc.T("August"),
        Loc.T("September"), Loc.T("October"), Loc.T("November"), Loc.T("December")
    ];

    public static string MonthLabel(string key)
    {
        var parts = key.Split('-');
        if (parts.Length != 2 || !int.TryParse(parts[1], out var month) || month < 1 || month > 12) return key;
        return $"{MonthNamesAr[month - 1]} {parts[0]}";
    }

    public static string ShiftMonth(string key, int delta)
    {
        if (!DateOnly.TryParseExact(key + "-01", "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date))
            throw new ArgumentException("Month must use yyyy-MM format.", nameof(key));
        return date.AddMonths(delta).ToString("yyyy-MM");
    }

    public static List<string> GetReportMonths(IEnumerable<string> availableKeys, string range, string currentMonth)
    {
        var allKeys = availableKeys.Distinct(StringComparer.Ordinal).OrderBy(key => key, StringComparer.Ordinal).ToList();
        if (range == "all") return allKeys.Count > 0 ? allKeys : [currentMonth];
        if (range == "year")
        {
            var year = currentMonth[..4];
            var filtered = allKeys.Where(key => key.StartsWith(year, StringComparison.Ordinal)).ToList();
            return filtered.Count > 0 ? filtered : [currentMonth];
        }

        var count = range == "12m" ? 12 : 6;
        var keys = new List<string>(count);
        var key = currentMonth;
        for (var index = 0; index < count; index++)
        {
            keys.Insert(0, key);
            key = ShiftMonth(key, -1);
        }
        return keys;
    }

    public async Task<ReportsVm> BuildReportAsync(string buildingId, string range, string currentMonth, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId)) throw new ArgumentException("Building is required.", nameof(buildingId));
        if (range is not ("6m" or "12m" or "year" or "all")) range = "6m";

        var building = await _wallet.GetBuildingShellAsync(buildingId, null, cancellationToken)
            ?? throw new InvalidOperationException("Building was not found in the target schema.");

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var availableKeys = await db.FinancialPeriods.AsNoTracking()
            .Where(period => period.BuildingId == buildingId)
            .Select(period => period.PeriodKey).Distinct().ToListAsync(cancellationToken);
        var months = GetReportMonths(availableKeys, range, currentMonth);

        var currencyCode = await db.BuildingSettings.AsNoTracking()
            .Where(settings => settings.BuildingId == buildingId)
            .Select(settings => settings.Currency).SingleOrDefaultAsync(cancellationToken) ?? "EGP";
        currencyCode = currencyCode.ToUpperInvariant();

        var collectedRows = await (
            from deposit in db.PaymentDeposits.AsNoTracking()
            join period in db.FinancialPeriods.AsNoTracking() on deposit.PeriodId equals period.PeriodId
            where deposit.BuildingId == buildingId && period.BuildingId == buildingId &&
                months.Contains(period.PeriodKey) && deposit.CurrencyCode == currencyCode &&
                deposit.Status == PaymentDepositStatus.Confirmed
            group deposit by period.PeriodKey into monthDeposits
            select new { MonthKey = monthDeposits.Key, Total = monthDeposits.Sum(item => item.Amount) }
        ).ToListAsync(cancellationToken);
        var collectedByMonth = collectedRows.ToDictionary(item => item.MonthKey, item => item.Total, StringComparer.Ordinal);

        var monthly = new List<MonthlyPoint>(months.Count);
        var categoryTotals = new Dictionary<string, CategoryPoint>(StringComparer.Ordinal);
        decimal totalCollected = 0, totalExpenses = 0, totalRevenues = 0;

        foreach (var month in months)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var expenseTask = _finance.GetMonthAsync(buildingId, month, "expense", cancellationToken);
            var revenueTask = _finance.GetMonthAsync(buildingId, month, "revenue", cancellationToken);
            await Task.WhenAll(expenseTask, revenueTask);
            var expenseSnapshot = await expenseTask;
            var revenueSnapshot = await revenueTask;
            var collected = collectedByMonth.GetValueOrDefault(month);
            var expenses = expenseSnapshot.Total;
            var revenues = revenueSnapshot.Total;

            totalCollected += collected;
            totalExpenses += expenses;
            totalRevenues += revenues;
            monthly.Add(new MonthlyPoint
            {
                Month = month, Label = MonthLabel(month), Collected = collected,
                Revenues = revenues, Expenses = expenses
            });

            foreach (var category in expenseSnapshot.Categories)
            {
                if (!categoryTotals.ContainsKey(category.Id))
                    categoryTotals[category.Id] = new CategoryPoint { Name = category.Name, Color = category.Color, Total = 0 };
            }
            foreach (var expense in expenseSnapshot.Expenses)
            {
                var categoryId = string.IsNullOrWhiteSpace(expense.CategoryId) ? "__uncategorized" : expense.CategoryId;
                if (!categoryTotals.TryGetValue(categoryId, out var point))
                {
                    point = new CategoryPoint { Name = "—", Color = "#888888", Total = 0 };
                    categoryTotals[categoryId] = point;
                }
                point.Total += expense.Amount;
            }
        }

        var currentWallet = await _wallet.GetAdminSnapshotAsync(buildingId, currentMonth, cancellationToken);
        var allBalances = building.Apartments.Select(apartment => new TopEntry
        {
            Apt = apartment,
            Balance = currentWallet.Balances.GetValueOrDefault(apartment.Id)
        }).ToList();
        var wallet = building.Apartments.Where(apartment => !apartment.Closed)
            .Select(apartment => new WalletPoint
            {
                Number = apartment.Number,
                Label = Loc.T("Apartment_N", apartment.Number),
                Balance = currentWallet.Balances.GetValueOrDefault(apartment.Id)
            }).OrderBy(item => item.Number).ToList();

        var netBalance = totalCollected + totalRevenues - totalExpenses;
        return new ReportsVm
        {
            Building = building, Range = range,
            TotalCollected = totalCollected, TotalExpenses = totalExpenses, TotalRevenues = totalRevenues,
            NetBalance = netBalance, AvgMonthly = months.Count > 0 ? netBalance / months.Count : 0,
            Monthly = monthly, Categories = categoryTotals.Values.Where(item => item.Total > 0).ToList(),
            Wallet = wallet,
            Debtors = allBalances.Where(item => item.Balance < 0).OrderBy(item => item.Balance).Take(5).ToList(),
            Creditors = allBalances.Where(item => item.Balance > 0).OrderByDescending(item => item.Balance).Take(5).ToList()
        };
    }
}
