using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class CsvExportSafetyTests
{
    [Theory]
    [InlineData("=1+1")]
    [InlineData(" +cmd|' /C calc'!A0")]
    [InlineData("@SUM(A1:A2)")]
    [InlineData("-2+3")]
    [InlineData("\t=1+1")]
    public void Cell_PrefixesFormulaLikeValues(string input)
    {
        var cell = CsvExportSafety.Cell(input);
        Assert.StartsWith("\"'", cell);
    }

    [Fact]
    public void Cell_LeavesNegativeNumericValuesAsNumbers()
    {
        Assert.Equal("\"-125.50\"", CsvExportSafety.Cell("-125.50"));
    }

    [Fact]
    public void Cell_EscapesQuotes()
    {
        Assert.Equal("\"a\"\"b\"", CsvExportSafety.Cell("a\"b"));
    }
}
