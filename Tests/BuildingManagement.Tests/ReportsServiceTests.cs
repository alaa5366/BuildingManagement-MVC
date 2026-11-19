using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class ReportsServiceTests
{
    [Fact]
    public void MonthLabel_ValidMonth_ReturnsLocalizedLabel()
    {
        var label = ReportsService.MonthLabel("2025-01");
        Assert.Contains("2025", label);
        Assert.NotEqual("2025-01", label);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("2025")]
    [InlineData("2025-13")]
    [InlineData("2025-00")]
    public void MonthLabel_InvalidMonth_ReturnsOriginal(string value) =>
        Assert.Equal(value, ReportsService.MonthLabel(value));

    [Fact]
    public void ShiftMonth_CrossesYearBoundaries()
    {
        Assert.Equal("2025-02", ReportsService.ShiftMonth("2025-01", 1));
        Assert.Equal("2024-12", ReportsService.ShiftMonth("2025-01", -1));
        Assert.Equal("2026-01", ReportsService.ShiftMonth("2025-12", 1));
    }

    [Fact]
    public void GetReportMonths_AllRange_UsesAvailableTargetPeriods()
    {
        var months = ReportsService.GetReportMonths(
            new[] { "2024-11", "2024-12", "2025-01" }, "all", "2025-01");
        Assert.Equal(new[] { "2024-11", "2024-12", "2025-01" }, months);
    }

    [Fact]
    public void GetReportMonths_YearRange_FiltersCurrentYear()
    {
        var months = ReportsService.GetReportMonths(
            new[] { "2024-11", "2025-01", "2025-02" }, "year", "2025-01");
        Assert.Equal(new[] { "2025-01", "2025-02" }, months);
    }

    [Theory]
    [InlineData("6m", 6)]
    [InlineData("12m", 12)]
    public void GetReportMonths_RollingRanges_ReturnExpectedCount(string range, int expected)
    {
        var months = ReportsService.GetReportMonths(Array.Empty<string>(), range, "2025-06");
        Assert.Equal(expected, months.Count);
        Assert.Equal("2025-06", months[^1]);
    }

    [Fact]
    public void GetReportMonths_AllRangeWithoutPeriods_FallsBackToCurrentMonth()
    {
        Assert.Equal(new[] { "2025-06" },
            ReportsService.GetReportMonths(Array.Empty<string>(), "all", "2025-06"));
    }
}
