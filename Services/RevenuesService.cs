using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// طھط±ط¬ظ…ط© ط­ط±ظپظٹط© ظ„ظ€ js/core/revenues.js
/// <summary>
/// Business logic for monthly revenues.
/// </summary>
public class RevenuesService
{
    private readonly WalletService _wallet;
    /// <summary>
    /// Initializes a new instance of the <see cref="RevenuesService"/> class.
    /// </summary>
    public RevenuesService(WalletService wallet) => _wallet = wallet;

    /// <summary>
    /// Adds the revenue.
    /// </summary>
    public Revenue AddRevenue(Building building, string categoryId, string note, decimal amount, string? date, string monthKey)
    {
        ArgumentNullException.ThrowIfNull(building);
        FinanceCommandValidation.ValidateMonthKey(monthKey);
        FinanceCommandValidation.ValidatePositiveMoney(amount, nameof(amount));
        FinanceCommandValidation.ValidateActiveCategory(building.RevenueCategories, categoryId, nameof(categoryId));
        var transactionDate = FinanceCommandValidation.ResolveTransactionDate(date, monthKey);
        var m = _wallet.GetOrCreateMonth(building, monthKey);
        var revenue = new Revenue
        {
            Id = Guid.NewGuid().ToString("N"),
            CategoryId = categoryId,
            Note = note?.Trim() ?? "",
            Amount = amount,
            Date = transactionDate
        };
        m.Revenues.Add(revenue);
        return revenue;
    }

    /// <summary>
    /// Deletes the revenue.
    /// </summary>
    public bool DeleteRevenue(Building building, string revenueId, string monthKey)
    {
        if (!building.Months.TryGetValue(monthKey, out var m)) return false;
        var before = m.Revenues.Count;
        m.Revenues = m.Revenues.Where(r => r.Id != revenueId).ToList();
        return before != m.Revenues.Count;
    }

    /// <summary>
    /// Returns the total revenues of month.
    /// </summary>
    public decimal TotalRevenuesOfMonth(Building building, string monthKey) =>
        building.Months.TryGetValue(monthKey, out var m) ? m.Revenues.Sum(r => r.Amount) : 0;
}
