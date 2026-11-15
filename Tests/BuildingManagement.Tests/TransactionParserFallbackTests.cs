using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

/// <summary>
/// The integer fallback of the OCR amount parser must only accept a number that sits next to an
/// amount keyword or a currency, so dates and random numbers are not mistaken for the amount.
/// </summary>
public class TransactionParserFallbackTests
{
    private readonly TransactionParser _parser = new();

    [Theory]
    [InlineData("Transfer 250 EGP to account", 250)]
    [InlineData("المبلغ 3500 جنيه", 3500)]
    [InlineData("Amount: 1200", 1200)]
    public void Integer_amount_next_to_a_keyword_or_currency_is_extracted(string text, int expected)
    {
        var result = _parser.Parse(text);

        Assert.NotNull(result);
        Assert.Equal((decimal)expected, result!.Amount);
    }

    [Theory]
    [InlineData("Date 14 Nov 2026 Transaction Successful")]
    [InlineData("Title 1234 Reference")]
    [InlineData("Ref 123456789012")]
    public void Numbers_without_a_keyword_are_not_taken_as_the_amount(string text)
    {
        var result = _parser.Parse(text);

        Assert.NotNull(result);
        Assert.Equal(0m, result!.Amount);
    }
}
