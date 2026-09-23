using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/core/expenses.js
public class ExpensesService
{
    private readonly WalletService _wallet;
    public ExpensesService(WalletService wallet) => _wallet = wallet;

    public Expense AddExpense(Building building, string categoryId, string note, double amount, string? date, string monthKey)
    {
        var m = _wallet.GetOrCreateMonth(building, monthKey);
        var expense = new Expense
        {
            Id = Guid.NewGuid().ToString("N"),
            CategoryId = categoryId,
            Note = note ?? "",
            Amount = amount,
            Date = string.IsNullOrWhiteSpace(date) ? DateTime.UtcNow.ToString("yyyy-MM-dd") : date
        };
        m.Expenses.Add(expense);
        return expense;
    }

    public bool DeleteExpense(Building building, string expenseId, string monthKey)
    {
        if (!building.Months.TryGetValue(monthKey, out var m)) return false;
        var before = m.Expenses.Count;
        m.Expenses = m.Expenses.Where(e => e.Id != expenseId).ToList();
        return before != m.Expenses.Count;
    }

    public double TotalExpensesOfMonth(Building building, string monthKey) =>
        building.Months.TryGetValue(monthKey, out var m) ? m.Expenses.Sum(e => e.Amount) : 0;
}
