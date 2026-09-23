using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/core/revenues.js
public class RevenuesService
{
    private readonly WalletService _wallet;
    public RevenuesService(WalletService wallet) => _wallet = wallet;

    public Revenue AddRevenue(Building building, string categoryId, string note, double amount, string? date, string monthKey)
    {
        var m = _wallet.GetOrCreateMonth(building, monthKey);
        var revenue = new Revenue
        {
            Id = Guid.NewGuid().ToString("N"),
            CategoryId = categoryId,
            Note = note ?? "",
            Amount = amount,
            Date = string.IsNullOrWhiteSpace(date) ? DateTime.UtcNow.ToString("yyyy-MM-dd") : date
        };
        m.Revenues.Add(revenue);
        return revenue;
    }

    public bool DeleteRevenue(Building building, string revenueId, string monthKey)
    {
        if (!building.Months.TryGetValue(monthKey, out var m)) return false;
        var before = m.Revenues.Count;
        m.Revenues = m.Revenues.Where(r => r.Id != revenueId).ToList();
        return before != m.Revenues.Count;
    }

    public double TotalRevenuesOfMonth(Building building, string monthKey) =>
        building.Months.TryGetValue(monthKey, out var m) ? m.Revenues.Sum(r => r.Amount) : 0;
}
