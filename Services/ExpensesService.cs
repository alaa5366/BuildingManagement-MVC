using BuildingManagementMvc.Models;
using System.Globalization;

namespace BuildingManagementMvc.Services;

// طھط±ط¬ظ…ط© ط­ط±ظپظٹط© ظ„ظ€ js/core/expenses.js
/// <summary>
/// Business logic for monthly expenses.
/// </summary>
public class ExpensesService
{
    private readonly WalletService _wallet;
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpensesService"/> class.
    /// </summary>
    public ExpensesService(WalletService wallet) => _wallet = wallet;

    /// <summary>
    /// Adds the expense.
    /// </summary>
    public Expense AddExpense(Building building, string categoryId, string note, decimal amount, string? date, string monthKey)
    {
        ArgumentNullException.ThrowIfNull(building);
        FinanceCommandValidation.ValidateMonthKey(monthKey);
        FinanceCommandValidation.ValidatePositiveMoney(amount, nameof(amount));
        FinanceCommandValidation.ValidateActiveCategory(building.ExpenseCategories, categoryId, nameof(categoryId));
        var transactionDate = FinanceCommandValidation.ResolveTransactionDate(date, monthKey);
        var m = _wallet.GetOrCreateMonth(building, monthKey);
        var expense = new Expense
        {
            Id = Guid.NewGuid().ToString("N"),
            CategoryId = categoryId,
            Note = note?.Trim() ?? "",
            Amount = amount,
            Date = transactionDate
        };
        m.Expenses.Add(expense);
        return expense;
    }

    /// <summary>
    /// Deletes the expense.
    /// </summary>
    public bool DeleteExpense(Building building, string expenseId, string monthKey)
    {
        if (!building.Months.TryGetValue(monthKey, out var m)) return false;
        var before = m.Expenses.Count;
        m.Expenses = m.Expenses.Where(e => e.Id != expenseId).ToList();
        return before != m.Expenses.Count;
    }

    /// <summary>
    /// Returns the total expenses of month.
    /// </summary>
    public decimal TotalExpensesOfMonth(Building building, string monthKey) =>
        building.Months.TryGetValue(monthKey, out var m) ? m.Expenses.Sum(e => e.Amount) : 0;
}
