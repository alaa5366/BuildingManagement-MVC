using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

/// <summary>Builds financial reports from target periods, allocation records and posted ledger entries.</summary>
public sealed class ReportsService
{
    private readonly TargetWalletReadService _wallet;
    private readonly TargetFinanceReadService _finance;

    public ReportsService(TargetWalletReadService wallet, TargetFinanceReadService finance)
    {
        _wallet = wallet;
        _finance = finance;
    }

    private static string[] MonthNamesAr =>
    [
        Loc.T("January"), Loc.T("February"), Loc.T("March"), Loc.T("April"),
        Loc.T("May"), Loc.T("June"), Loc.T("July"), Loc.T("August"),
        Loc.T("September"), Loc.T("October"), Loc.T("November"), Loc.T("December")
    ];

    /// <summary>Formats a financial period as a localized month label.</summary>
    public static string MonthLabel(string key)
    {
        var parts = key.Split('-');
        if (parts.Length != 2 || !int.TryParse(parts[1], out var month) || month is < 1 or > 12)
            return key;
        return $"{MonthNamesAr[month - 1]} {parts[0]}";
    }

    /// <summary>Moves a financial period by the specified number of months.</summary>
    public static string ShiftMonth(string key, int delta)
    {
        if (!DateOnly.TryParseExact(key + "-01", "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var date))
            throw new ArgumentException("Month must use yyyy-MM format.", nameof(key));
        return date.AddMonths(delta).ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Gets the key for the current financial reporting period.</summary>
    public Task<string> GetCurrentPeriodKeyAsync(string buildingId, CancellationToken cancellationToken = default) =>
        _wallet.GetCurrentPeriodKeyAsync(buildingId, cancellationToken: cancellationToken);

    /// <summary>Builds a report for the specified building and reporting period.</summary>
    public async Task<ReportsVm> BuildReportAsync(
        string buildingId,
        string range,
        string currentMonth,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            throw new ArgumentException("Building is required.", nameof(buildingId));
        var building = await _wallet.GetBuildingShellAsync(
            buildingId, null, cancellationToken)
            ?? throw new KeyNotFoundException("The requested building was not found in the target store.");

        var normalizedRange = range is "all" or "year" or "12m" or "6m" ? range : "6m";
        var allKeys = (await _wallet.GetFinancialPeriodKeysAsync(building.Id, cancellationToken))
            .Where(key => string.CompareOrdinal(key, currentMonth) <= 0)
            .TakeLast(600)
            .ToList();
        var months = SelectMonths(allKeys, normalizedRange, currentMonth);
        var periodTotals = await _wallet.GetPeriodTotalsAsync(building.Id, months, cancellationToken);

        var categorySnapshot = await _finance.GetMonthAsync(
            building.Id, currentMonth, "expense", cancellationToken);
        var categoryById = categorySnapshot.Categories
            .ToDictionary(category => category.Id, StringComparer.Ordinal);
        var categoryTotals = await _finance.GetExpenseCategoryTotalsAsync(
            building.Id, months, cancellationToken);

        var monthly = months.Select(month =>
        {
            var totals = periodTotals.GetValueOrDefault(month) ?? new TargetWalletPeriodTotals(0m, 0m, 0m);
            return new MonthlyPoint
            {
                Month = month,
                Label = MonthLabel(month),
                Collected = totals.Collected,
                Revenues = totals.Revenues,
                Expenses = totals.Expenses
            };
        }).ToList();

        var totalCollected = monthly.Sum(item => item.Collected);
        var totalExpenses = monthly.Sum(item => item.Expenses);
        var totalRevenues = monthly.Sum(item => item.Revenues);
        var netBalance = totalCollected - totalExpenses + totalRevenues;
        var averageMonthly = months.Count > 0 ? netBalance / months.Count : 0m;

        var categories = categoryTotals.Select(item =>
        {
            categoryById.TryGetValue(item.Key, out var category);
            return new CategoryPoint
            {
                Name = category?.Name ?? "â€”",
                Color = category?.Color ?? "#888888",
                Total = item.Value
            };
        }).Where(item => item.Total > 0)
          .OrderByDescending(item => item.Total)
          .ToList();

        var currentSnapshot = await _wallet.GetAdminSnapshotAsync(
            building.Id, currentMonth, cancellationToken);
        var wallet = building.Apartments.Where(apartment => !apartment.Closed && !apartment.Disabled)
            .Select(apartment => new WalletPoint
            {
                ApartmentId = apartment.Id,
                Owner = apartment.Owner,
                Number = apartment.Number,
                Label = Loc.T("Apartment_N", apartment.Number),
                Balance = currentSnapshot.Balances.GetValueOrDefault(apartment.Id)
            })
            .OrderBy(item => item.Number)
            .ToList();

        var allBalances = building.Apartments.Select(apartment => new TopEntry
        {
            Apt = apartment,
            Balance = currentSnapshot.Balances.GetValueOrDefault(apartment.Id)
        }).ToList();
        var debtors = allBalances.Where(item => item.Balance < 0)
            .OrderBy(item => item.Balance).Take(5).ToList();
        var creditors = allBalances.Where(item => item.Balance > 0)
            .OrderByDescending(item => item.Balance).Take(5).ToList();

        return new ReportsVm
        {
            Building = building,
            Range = normalizedRange,
            TotalCollected = totalCollected,
            TotalExpenses = totalExpenses,
            TotalRevenues = totalRevenues,
            NetBalance = netBalance,
            AvgMonthly = averageMonthly,
            Monthly = monthly,
            Categories = categories,
            Wallet = wallet,
            Debtors = debtors,
            Creditors = creditors
        };
    }

    /// <summary>Gets the available reporting months for the requested building.</summary>
    public static List<string> GetReportMonths(
        IReadOnlyList<string> allKeys, string range, string currentMonth) =>
        SelectMonths(allKeys, range, currentMonth);

    private static List<string> SelectMonths(
        IReadOnlyList<string> allKeys, string range, string currentMonth)
    {
        if (range == "all")
            return allKeys.Count > 0 ? allKeys.ToList() : new List<string> { currentMonth };
        if (range == "year")
        {
            var year = currentMonth[..4];
            var filtered = allKeys.Where(key => key.StartsWith(year, StringComparison.Ordinal))
                .OrderBy(key => key, StringComparer.Ordinal).ToList();
            return filtered.Count > 0 ? filtered : new List<string> { currentMonth };
        }

        var count = range == "12m" ? 12 : 6;
        var months = new List<string>(count);
        var month = currentMonth;
        for (var index = 0; index < count; index++)
        {
            months.Insert(0, month);
            month = ShiftMonth(month, -1);
        }
        return months;
    }
}
