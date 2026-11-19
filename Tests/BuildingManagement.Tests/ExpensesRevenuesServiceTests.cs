using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class ExpensesServiceTests
{
    private readonly WalletService _wallet = new();
    private readonly ExpensesService _service;

    public ExpensesServiceTests()
    {
        _service = new ExpensesService(_wallet);
    }

    private static Building CreateBuilding()
    {
        return new Building { Name = "Test", ExpenseCategories = [new FinancialCategory { Id = "cat1" }, new FinancialCategory { Id = "cat2" }] };
    }

    // ================================================================
    // AddExpense
    // ================================================================

    [Fact]
    public void AddExpense_ValidData_AddsToMonth()
    {
        var b = CreateBuilding();
        var expense = _service.AddExpense(b, "cat1", "Test note", 500, "2025-01-15", "2025-01");

        Assert.NotNull(expense);
        Assert.Equal(500, expense.Amount);
        Assert.Equal("cat1", expense.CategoryId);
        Assert.Equal("Test note", expense.Note);
        Assert.Equal("2025-01-15", expense.Date);
        Assert.Single(b.Months["2025-01"].Expenses);
    }

    [Fact]
    public void AddExpense_NullDate_UsesToday()
    {
        var b = CreateBuilding();
        var month = WalletService.CurrentMonthKey();
        var expense = _service.AddExpense(b, "cat1", null!, 500, null, month);
        Assert.NotNull(expense.Date);
        Assert.NotEmpty(expense.Date);
        Assert.Matches(@"\d{4}-\d{2}-\d{2}", expense.Date);
    }

    [Fact]
    public void AddExpense_EmptyDate_UsesToday()
    {
        var b = CreateBuilding();
        var month = WalletService.CurrentMonthKey();
        var expense = _service.AddExpense(b, "cat1", "", 500, null, month);
        Assert.NotNull(expense.Date);
        Assert.NotEmpty(expense.Date);
    }

    [Fact]
    public void AddExpense_NullNote_StoresEmpty()
    {
        var b = CreateBuilding();
        var expense = _service.AddExpense(b, "cat1", "", 500, null, "2025-01");
        Assert.Equal("", expense.Note);
    }

    [Fact]
    public void AddExpense_MultipleExpenses_AllAdded()
    {
        var b = CreateBuilding();
        _service.AddExpense(b, "cat1", "A", 500, "2025-01-15", "2025-01");
        _service.AddExpense(b, "cat2", "B", 300, "2025-01-20", "2025-01");

        Assert.Equal(2, b.Months["2025-01"].Expenses.Count);
    }

    [Fact]
    public void AddExpense_DifferentMonths_SeparateCollections()
    {
        var b = CreateBuilding();
        _service.AddExpense(b, "cat1", "A", 500, "2025-01-15", "2025-01");
        _service.AddExpense(b, "cat1", "B", 300, "2025-02-10", "2025-02");

        Assert.Single(b.Months["2025-01"].Expenses);
        Assert.Single(b.Months["2025-02"].Expenses);
    }

    // ================================================================
    // DeleteExpense
    // ================================================================

    [Fact]
    public void DeleteExpense_ExistingExpense_Removes()
    {
        var b = CreateBuilding();
        var expense = _service.AddExpense(b, "cat1", "Test", 500, "2025-01-15", "2025-01");

        var deleted = _service.DeleteExpense(b, expense.Id, "2025-01");

        Assert.True(deleted);
        Assert.Empty(b.Months["2025-01"].Expenses);
    }

    [Fact]
    public void DeleteExpense_NonExistingExpense_ReturnsFalse()
    {
        var b = CreateBuilding();
        _service.AddExpense(b, "cat1", "Test", 500, "2025-01-15", "2025-01");

        var deleted = _service.DeleteExpense(b, "ghost", "2025-01");

        Assert.False(deleted);
        Assert.Single(b.Months["2025-01"].Expenses);
    }

    [Fact]
    public void DeleteExpense_NonExistingMonth_ReturnsFalse()
    {
        var b = CreateBuilding();
        Assert.False(_service.DeleteExpense(b, "any", "2099-01"));
    }

    [Fact]
    public void DeleteExpense_OnlyRemovesTargetedExpense()
    {
        var b = CreateBuilding();
        var e1 = _service.AddExpense(b, "cat1", "A", 500, "2025-01-15", "2025-01");
        var e2 = _service.AddExpense(b, "cat2", "B", 300, "2025-01-20", "2025-01");

        _service.DeleteExpense(b, e1.Id, "2025-01");

        Assert.Single(b.Months["2025-01"].Expenses);
        Assert.Equal(e2.Id, b.Months["2025-01"].Expenses[0].Id);
    }

    // ================================================================
    // TotalExpensesOfMonth
    // ================================================================

    [Fact]
    public void TotalExpensesOfMonth_SumsAllExpenses()
    {
        var b = CreateBuilding();
        _service.AddExpense(b, "cat1", "A", 500, "2025-01-15", "2025-01");
        _service.AddExpense(b, "cat2", "B", 300, "2025-01-20", "2025-01");

        Assert.Equal(800, _service.TotalExpensesOfMonth(b, "2025-01"));
    }

    [Fact]
    public void TotalExpensesOfMonth_EmptyMonth_ReturnsZero()
    {
        var b = CreateBuilding();
        Assert.Equal(0, _service.TotalExpensesOfMonth(b, "2025-01"));
    }

    [Fact]
    public void TotalExpensesOfMonth_DifferentMonths_CalculatedSeparately()
    {
        var b = CreateBuilding();
        _service.AddExpense(b, "cat1", "A", 500, "2025-01-15", "2025-01");
        _service.AddExpense(b, "cat1", "B", 300, "2025-02-10", "2025-02");

        Assert.Equal(500, _service.TotalExpensesOfMonth(b, "2025-01"));
        Assert.Equal(300, _service.TotalExpensesOfMonth(b, "2025-02"));
    }
}

public class RevenuesServiceTests
{
    private readonly WalletService _wallet = new();
    private readonly RevenuesService _service;

    public RevenuesServiceTests()
    {
        _service = new RevenuesService(_wallet);
    }

    private static Building CreateBuilding()
    {
        return new Building { Name = "Test", RevenueCategories = [new FinancialCategory { Id = "cat1" }, new FinancialCategory { Id = "cat2" }] };
    }

    // ================================================================
    // AddRevenue
    // ================================================================

    [Fact]
    public void AddRevenue_ValidData_AddsToMonth()
    {
        var b = CreateBuilding();
        var revenue = _service.AddRevenue(b, "cat1", "Test note", 500, "2025-01-15", "2025-01");

        Assert.NotNull(revenue);
        Assert.Equal(500, revenue.Amount);
        Assert.Equal("cat1", revenue.CategoryId);
        Assert.Equal("Test note", revenue.Note);
        Assert.Equal("2025-01-15", revenue.Date);
        Assert.Single(b.Months["2025-01"].Revenues);
    }

    [Fact]
    public void AddRevenue_NullDate_UsesToday()
    {
        var b = CreateBuilding();
        var month = WalletService.CurrentMonthKey();
        var revenue = _service.AddRevenue(b, "cat1", null!, 500, null, month);
        Assert.NotNull(revenue.Date);
        Assert.Matches(@"\d{4}-\d{2}-\d{2}", revenue.Date);
    }

    [Fact]
    public void AddRevenue_NullNote_StoresEmpty()
    {
        var b = CreateBuilding();
        var month = WalletService.CurrentMonthKey();
        var revenue = _service.AddRevenue(b, "cat1", "", 500, null, month);
        Assert.Equal("", revenue.Note);
    }

    // ================================================================
    // DeleteRevenue
    // ================================================================

    [Fact]
    public void DeleteRevenue_ExistingRevenue_Removes()
    {
        var b = CreateBuilding();
        var revenue = _service.AddRevenue(b, "cat1", "Test", 500, "2025-01-15", "2025-01");

        var deleted = _service.DeleteRevenue(b, revenue.Id, "2025-01");

        Assert.True(deleted);
        Assert.Empty(b.Months["2025-01"].Revenues);
    }

    [Fact]
    public void DeleteRevenue_NonExistingRevenue_ReturnsFalse()
    {
        var b = CreateBuilding();
        Assert.False(_service.DeleteRevenue(b, "ghost", "2025-01"));
    }

    [Fact]
    public void DeleteRevenue_NonExistingMonth_ReturnsFalse()
    {
        var b = CreateBuilding();
        Assert.False(_service.DeleteRevenue(b, "any", "2099-01"));
    }

    // ================================================================
    // TotalRevenuesOfMonth
    // ================================================================

    [Fact]
    public void TotalRevenuesOfMonth_SumsAllRevenues()
    {
        var b = CreateBuilding();
        _service.AddRevenue(b, "cat1", "A", 500, "2025-01-15", "2025-01");
        _service.AddRevenue(b, "cat2", "B", 300, "2025-01-20", "2025-01");

        Assert.Equal(800, _service.TotalRevenuesOfMonth(b, "2025-01"));
    }

    [Fact]
    public void TotalRevenuesOfMonth_EmptyMonth_ReturnsZero()
    {
        var b = CreateBuilding();
        Assert.Equal(0, _service.TotalRevenuesOfMonth(b, "2025-01"));
    }
}