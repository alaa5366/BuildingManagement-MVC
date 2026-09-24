using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/features/reports.js
public class ReportsService
{
    private readonly WalletService _wallet;
    public ReportsService(WalletService wallet) => _wallet = wallet;

    private static readonly string[] MonthNamesAr =
    {
        "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
        "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
    };

    public static string MonthLabel(string key)
    {
        var parts = key.Split('-');
        if (parts.Length != 2 || !int.TryParse(parts[1], out var m) || m < 1 || m > 12) return key;
        return $"{MonthNamesAr[m - 1]} {parts[0]}";
    }

    public static string ShiftMonth(string key, int delta)
    {
        var parts = key.Split('-');
        var y = int.Parse(parts[0]);
        var m = int.Parse(parts[1]) + delta;
        while (m < 1) { m += 12; y--; }
        while (m > 12) { m -= 12; y++; }
        return $"{y}-{m:D2}";
    }

    public List<string> GetReportMonths(Building building, string range, string currentMonth)
    {
        var allKeys = building.Months.Keys.OrderBy(k => k).ToList();

        if (range == "all") return allKeys.Count > 0 ? allKeys : new List<string> { currentMonth };

        if (range == "year")
        {
            var year = currentMonth.Split('-')[0];
            var filtered = allKeys.Where(k => k.StartsWith(year)).ToList();
            return filtered.Count > 0 ? filtered : new List<string> { currentMonth };
        }

        var count = range == "12m" ? 12 : 6;
        var keys = new List<string>();
        var k = currentMonth;
        for (var i = 0; i < count; i++) { keys.Insert(0, k); k = ShiftMonth(k, -1); }
        return keys;
    }

    public ReportsVm BuildReport(Building building, string range, string currentMonth)
    {
        var months = GetReportMonths(building, range, currentMonth);

        double totalCollected = 0, totalExpenses = 0, totalRevenues = 0;
        var monthly = new List<MonthlyPoint>();

        foreach (var mk in months)
        {
            double collected = 0;
            foreach (var a in building.Apartments) collected += _wallet.TotalConfirmedDeposits(building, a.Id, mk);

            double expenses = 0, revenues = 0;
            if (building.Months.TryGetValue(mk, out var m))
            {
                expenses = m.Expenses.Sum(e => e.Amount);
                revenues = m.Revenues.Sum(r => r.Amount);
            }

            totalCollected += collected; totalExpenses += expenses; totalRevenues += revenues;
            monthly.Add(new MonthlyPoint { Month = mk, Label = MonthLabel(mk), Collected = collected, Revenues = revenues, Expenses = expenses });
        }

        var netBalance = totalCollected - totalExpenses + totalRevenues;
        var avgMonthly = months.Count > 0 ? netBalance / months.Count : 0;

        // بنود المصروفات
        var catTotals = new Dictionary<string, CategoryPoint>();
        foreach (var c in building.ExpenseCategories)
            catTotals[c.Id] = new CategoryPoint { Name = c.Name, Color = c.Color, Total = 0 };

        foreach (var mk in months)
        {
            if (!building.Months.TryGetValue(mk, out var m)) continue;
            foreach (var e in m.Expenses)
            {
                if (!catTotals.TryGetValue(e.CategoryId, out var cp))
                {
                    cp = new CategoryPoint { Name = "—", Color = "#888888", Total = 0 };
                    catTotals[e.CategoryId] = cp;
                }
                cp.Total += e.Amount;
            }
        }

        var categories = catTotals.Values.Where(c => c.Total > 0).ToList();

        // توزيع المحفظة (الشقق المفتوحة فقط، الشهر الحالي)
        var wallet = building.Apartments.Where(a => !a.Closed)
            .Select(a => new WalletPoint { Number = a.Number, Label = $"شقة {a.Number}", Balance = _wallet.ComputeWalletBalance(building, a.Id, currentMonth) })
            .OrderBy(w => w.Number).ToList();

        // أعلى مدينين ودائنين
        var allBalances = building.Apartments
            .Select(a => new TopEntry { Apt = a, Balance = _wallet.ComputeWalletBalance(building, a.Id, currentMonth) })
            .ToList();

        var debtors = allBalances.Where(x => x.Balance < 0).OrderBy(x => x.Balance).Take(5).ToList();
        var creditors = allBalances.Where(x => x.Balance > 0).OrderByDescending(x => x.Balance).Take(5).ToList();

        return new ReportsVm
        {
            Building = building,
            Range = range,
            TotalCollected = totalCollected,
            TotalExpenses = totalExpenses,
            TotalRevenues = totalRevenues,
            NetBalance = netBalance,
            AvgMonthly = avgMonthly,
            Monthly = monthly,
            Categories = categories,
            Wallet = wallet,
            Debtors = debtors,
            Creditors = creditors
        };
    }
}
