using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class ReportsServiceTests
{
    private readonly WalletService _wallet = new();
    private readonly ReportsService _service;

    public ReportsServiceTests()
    {
        _service = new ReportsService(_wallet);
    }

    private static Building CreateBuilding()
    {
        var b = new Building { Name = "Test" };
        b.Apartments.Add(new Apartment { Id = "a1", Number = 1, MonthlyFee = 100 });
        b.Apartments.Add(new Apartment { Id = "a2", Number = 2, MonthlyFee = 100 });
        return b;
    }

    // ================================================================
    // MonthLabel
    // ================================================================

    [Fact]
    public void MonthLabel_January_ReturnsArabicLabel()
    {
        var label = ReportsService.MonthLabel("2025-01");
        Assert.Contains("2025", label);
        Assert.NotEqual("2025-01", label); // مش بيرجع المفتاح الخام
    }

    [Fact]
    public void MonthLabel_InvalidFormat_ReturnsOriginal()
    {
        Assert.Equal("invalid", ReportsService.MonthLabel("invalid"));
        Assert.Equal("2025", ReportsService.MonthLabel("2025"));
    }

    [Fact]
    public void MonthLabel_InvalidMonth_ReturnsOriginal()
    {
        Assert.Equal("2025-13", ReportsService.MonthLabel("2025-13"));
        Assert.Equal("2025-00", ReportsService.MonthLabel("2025-00"));
    }

    // ================================================================
    // ShiftMonth
    // ================================================================

    [Fact]
    public void ShiftMonth_Forward_ReturnsNextMonth()
    {
        Assert.Equal("2025-02", ReportsService.ShiftMonth("2025-01", 1));
        Assert.Equal("2025-03", ReportsService.ShiftMonth("2025-01", 2));
    }

    [Fact]
    public void ShiftMonth_Backward_ReturnsPreviousMonth()
    {
        Assert.Equal("2024-12", ReportsService.ShiftMonth("2025-01", -1));
        Assert.Equal("2024-11", ReportsService.ShiftMonth("2025-01", -2));
    }

    [Fact]
    public void ShiftMonth_CrossYear_WorksCorrectly()
    {
        Assert.Equal("2026-01", ReportsService.ShiftMonth("2025-12", 1));
        Assert.Equal("2024-12", ReportsService.ShiftMonth("2025-01", -1));
    }

    // ================================================================
    // GetReportMonths
    // ================================================================

    [Fact]
    public void GetReportMonths_AllRange_ReturnsAllMonths()
    {
        var b = CreateBuilding();
        _wallet.EnsureMonth(b, "2024-11");
        _wallet.EnsureMonth(b, "2024-12");
        _wallet.EnsureMonth(b, "2025-01");

        var months = _service.GetReportMonths(b, "all", "2025-01");

        Assert.Equal(3, months.Count);
    }

    [Fact]
    public void GetReportMonths_YearRange_FiltersByYear()
    {
        var b = CreateBuilding();
        _wallet.EnsureMonth(b, "2024-11");
        _wallet.EnsureMonth(b, "2025-01");
        _wallet.EnsureMonth(b, "2025-02");

        var months = _service.GetReportMonths(b, "year", "2025-01");

        Assert.Equal(2, months.Count);
        Assert.All(months, m => Assert.StartsWith("2025", m));
    }

    [Fact]
    public void GetReportMonths_6mRange_Returns6Months()
    {
        var b = CreateBuilding();

        var months = _service.GetReportMonths(b, "6m", "2025-06");

        Assert.Equal(6, months.Count);
    }

    [Fact]
    public void GetReportMonths_12mRange_Returns12Months()
    {
        var b = CreateBuilding();

        var months = _service.GetReportMonths(b, "12m", "2025-06");

        Assert.Equal(12, months.Count);
    }

    // ================================================================
    // BuildReport
    // ================================================================

    [Fact]
    public void BuildReport_WithDeposits_CalculatesTotals()
    {
        var b = CreateBuilding();
        var m = _wallet.EnsureMonth(b, "2025-01");
        m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "confirmed" });
        m.Deposits.Add(new Deposit { Id = "d2", AptId = "a2", Amount = 300, Status = "confirmed" });

        var report = _service.BuildReport(b, "6m", "2025-01");

        Assert.NotNull(report);
        Assert.Equal(800, report.TotalCollected); // 500 + 300
    }

    [Fact]
    public void BuildReport_WithExpenses_CalculatesTotal()
    {
        var b = CreateBuilding();
        var m = _wallet.EnsureMonth(b, "2025-01");
        m.Expenses.Add(new Expense { Id = "e1", Amount = 200 });
        m.Expenses.Add(new Expense { Id = "e2", Amount = 150 });

        var report = _service.BuildReport(b, "6m", "2025-01");

        Assert.Equal(350, report.TotalExpenses);
    }

    [Fact]
    public void BuildReport_WithRevenues_CalculatesTotal()
    {
        var b = CreateBuilding();
        var m = _wallet.EnsureMonth(b, "2025-01");
        m.Revenues.Add(new Revenue { Id = "r1", Amount = 100 });

        var report = _service.BuildReport(b, "6m", "2025-01");

        Assert.Equal(100, report.TotalRevenues);
    }

    [Fact]
    public void BuildReport_NetBalance_IsCalculated()
    {
        var b = CreateBuilding();
        var m = _wallet.EnsureMonth(b, "2025-01");
        m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "confirmed" });
        m.Expenses.Add(new Expense { Id = "e1", Amount = 200 });
        m.Revenues.Add(new Revenue { Id = "r1", Amount = 100 });

        var report = _service.BuildReport(b, "6m", "2025-01");

        // 500 - 200 + 100 = 400
        Assert.Equal(400, report.NetBalance);
    }

    [Fact]
    public void BuildReport_WithDebtors_IdentifiesThem()
    {
        var b = CreateBuilding();
        var m = _wallet.EnsureMonth(b, "2025-01");
        // a1 مدين (رصيد سالب): مفيش دفعات والرسم 100
        // a2 مدين برضه
        // المتوقع: كلاهما مدينين

        var report = _service.BuildReport(b, "6m", "2025-01");

        Assert.NotEmpty(report.Debtors);
    }

    [Fact]
    public void BuildReport_WithCreditors_IdentifiesThem()
    {
        var b = CreateBuilding();
        var m = _wallet.EnsureMonth(b, "2025-01");
        m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "confirmed" });

        var report = _service.BuildReport(b, "6m", "2025-01");

        Assert.NotEmpty(report.Creditors);
    }

    [Fact]
    public void BuildReport_MonthlyData_ContainsAllMonths()
    {
        var b = CreateBuilding();

        var report = _service.BuildReport(b, "6m", "2025-01");

        Assert.Equal(6, report.Monthly.Count);
    }
}