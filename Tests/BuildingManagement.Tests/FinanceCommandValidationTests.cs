using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class FinanceCommandValidationTests
{
    private static Building CreateBuilding() => new()
    {
        Id = "building-1",
        Apartments = [new Apartment { Id = "apt-1" }],
        ExpenseCategories = [new FinancialCategory { Id = "expense-1", Active = true }],
        RevenueCategories = [new FinancialCategory { Id = "revenue-1", Active = true }]
    };

    [Theory]
    [InlineData("2026-11", "2026-11-19", "2026-11-19")]
    [InlineData("2026-02", "2026-02-01", "2026-02-01")]
    public void ResolveTransactionDate_AcceptsExactDateWithinMonth(string month, string input, string expected)
        => Assert.Equal(expected, FinanceCommandValidation.ResolveTransactionDate(input, month));

    [Theory]
    [InlineData("2026-11", "2026-10-31")]
    [InlineData("2026-11", "19/11/2026")]
    [InlineData("2026-13", "2026-11-19")]
    [InlineData("2026-1", "2026-01-01")]
    public void ResolveTransactionDate_RejectsInvalidOrMismatchedDate(string month, string date)
        => Assert.Throws<ArgumentException>(() => FinanceCommandValidation.ResolveTransactionDate(date, month));

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.001")]
    public void AddExpense_RejectsInvalidAmount(string rawAmount)
    {
        var amount = decimal.Parse(rawAmount, System.Globalization.CultureInfo.InvariantCulture);
        var building = CreateBuilding();
        var service = new ExpensesService(new WalletService());
        Assert.Throws<ArgumentOutOfRangeException>(() => service.AddExpense(building, "expense-1", "test", amount, "2026-11-19", "2026-11"));
        Assert.Empty(building.Months);
    }

    [Fact]
    public void AddExpense_RejectsInactiveOrUnknownCategory()
    {
        var building = CreateBuilding();
        building.ExpenseCategories[0].Active = false;
        var service = new ExpensesService(new WalletService());
        Assert.Throws<ArgumentException>(() => service.AddExpense(building, "expense-1", "test", 10m, "2026-11-19", "2026-11"));
        Assert.Empty(building.Months);
    }

    [Fact]
    public void AddExpense_StoresValidatedTransaction()
    {
        var building = CreateBuilding();
        var service = new ExpensesService(new WalletService());
        var result = service.AddExpense(building, "expense-1", "  repair  ", 10.25m, "2026-11-19", "2026-11");
        Assert.Equal("repair", result.Note);
        Assert.Equal("2026-11-19", result.Date);
        Assert.Equal(10.25m, result.Amount);
    }

    [Fact]
    public void AddRevenue_RejectsDateOutsideSelectedMonth()
    {
        var building = CreateBuilding();
        var service = new RevenuesService(new WalletService());
        Assert.Throws<ArgumentException>(() => service.AddRevenue(building, "revenue-1", "test", 10m, "2026-10-31", "2026-11"));
        Assert.Empty(building.Months);
    }

    [Fact]
    public void AddWalletAdjustment_RequiresExistingApartmentReasonAndActor()
    {
        var service = new WalletService();
        var building = CreateBuilding();
        Assert.Throws<ArgumentException>(() => service.AddWalletAdjustment(building, "ghost", 5m, "reason", "2026-11", "admin"));
        Assert.Throws<ArgumentException>(() => service.AddWalletAdjustment(building, "apt-1", 5m, " ", "2026-11", "admin"));
        Assert.Throws<ArgumentException>(() => service.AddWalletAdjustment(building, "apt-1", 5m, "reason", "2026-11", " "));
        Assert.Empty(building.WalletAdjustments);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1.001")]
    public void AddWalletAdjustment_RejectsZeroOrSubCentAmount(string rawAmount)
    {
        var amount = decimal.Parse(rawAmount, System.Globalization.CultureInfo.InvariantCulture);
        var service = new WalletService();
        var building = CreateBuilding();
        Assert.Throws<ArgumentOutOfRangeException>(() => service.AddWalletAdjustment(building, "apt-1", amount, "reason", "2026-11", "admin"));
        Assert.Empty(building.WalletAdjustments);
    }

    [Fact]
    public void AddWalletAdjustment_AcceptsSignedCentAmountAndTrimsAuditFields()
    {
        var service = new WalletService();
        var building = CreateBuilding();
        var result = service.AddWalletAdjustment(building, "apt-1", -2.50m, "  correction  ", "2026-11", " admin ");
        Assert.Equal(-2.50m, result.Amount);
        Assert.Equal("correction", result.Reason);
        Assert.Equal("admin", result.CreatedBy);
    }
}
