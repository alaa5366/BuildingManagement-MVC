using BuildingManagementMvc.Data;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class FinancialAllocationCalculatorTests
{
    [Fact]
    public void AllocateEqually_UsesLargestRemainderAndStableApartmentOrdering()
    {
        var result = FinancialAllocationCalculator.AllocateEqually(
            10.01m, new[] { "APT-C", "APT-A", "APT-B" });

        Assert.Equal(new[] { "APT-A", "APT-B", "APT-C" }, result.Select(x => x.ApartmentId));
        Assert.Equal(new[] { 3.34m, 3.34m, 3.33m }, result.Select(x => x.AllocatedAmount));
        Assert.Equal(10.01m, result.Sum(x => x.AllocatedAmount));
        Assert.Equal(new[] { 1, 2, 3 }, result.Select(x => x.RemainderRank));
    }

    [Fact]
    public void AllocateEqually_IsIndependentOfInputOrder()
    {
        var first = FinancialAllocationCalculator.AllocateEqually(1.00m, new[] { "B", "A", "C" });
        var second = FinancialAllocationCalculator.AllocateEqually(1.00m, new[] { "C", "B", "A" });
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.001)]
    public void AllocateEqually_RejectsInvalidAmounts(decimal amount)
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            FinancialAllocationCalculator.AllocateEqually(amount, new[] { "A" }));

    [Fact]
    public void AllocateEqually_RejectsEmptyOrDuplicateApartmentSet()
    {
        Assert.Throws<ArgumentException>(() => FinancialAllocationCalculator.AllocateEqually(1m, Array.Empty<string>()));
        Assert.Throws<ArgumentException>(() => FinancialAllocationCalculator.AllocateEqually(1m, new[] { "A", "A" }));
    }
}
