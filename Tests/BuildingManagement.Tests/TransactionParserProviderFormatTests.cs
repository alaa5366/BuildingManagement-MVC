using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

/// <summary>Representative synthetic OCR text fixtures for common Egyptian wallet and bank-transfer layouts.</summary>
public sealed class TransactionParserProviderFormatTests
{
    private readonly TransactionParser _parser = new();

    [Theory]
    [InlineData("InstaPay\nAmount: EGP 1,250.75\nReference Number: IP123456789\nTransaction Successful", 1250.75, "SUCCESS")]
    [InlineData("Vodafone Cash\nالمبلغ المحول: ٣٥٠٫٥٠ جنيه مصري\nرقم العملية: 123456789012\nتم التحويل بنجاح", 350.50, "SUCCESS")]
    [InlineData("Bank transfer receipt\nTransfer Amount: 2.450,00 EGP\nTransaction ID: TRX-20261121001\nCompleted", 2450.00, "SUCCESS")]
    [InlineData("Orange Cash\nAmount: 500 EGP\nReference: 998877665544\nProcessing", 500, "PENDING")]
    [InlineData("Wallet transfer\nAmount: 750.00 LE\nReference: 778899001122\nTransaction failed", 750, "FAILED")]
    public void Parses_common_provider_layouts_and_statuses(string text, decimal expectedAmount, string expectedStatus)
    {
        var result = _parser.Parse(text);

        Assert.NotNull(result);
        Assert.Equal(expectedAmount, result.Amount);
        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedStatus == "SUCCESS", result.IsSuccessful);
        Assert.Equal(expectedStatus == "FAILED", result.IsFailed);
        Assert.Equal(expectedStatus == "PENDING", result.IsPending);
    }

    [Fact]
    public void Failure_status_takes_precedence_over_generic_success_words()
    {
        var result = _parser.Parse("Payment status\nSuccess message unavailable\nTransfer failed\nAmount: 400 EGP");

        Assert.NotNull(result);
        Assert.Equal("FAILED", result.Status);
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void Arabic_indic_digits_and_decimal_separator_are_normalized()
    {
        var result = _parser.Parse("المبلغ: ١٬٢٣٤٫٥٠ جنيه\nتمت العملية بنجاح");

        Assert.NotNull(result);
        Assert.Equal(1234.50m, result.Amount);
        Assert.Equal("SUCCESS", result.Status);
    }

    [Fact]
    public void Account_numbers_and_dates_are_not_mistaken_for_integer_amounts()
    {
        var result = _parser.Parse("Date: 21/11/2026\nAccount: 1234567890123456\nReference: 123456789012");

        Assert.NotNull(result);
        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public void Unknown_status_is_not_promoted_to_success()
    {
        var result = _parser.Parse("Bank transfer\nAmount: 220 EGP\nReference: 123456789012");

        Assert.NotNull(result);
        Assert.Equal("UNKNOWN", result.Status);
        Assert.False(result.IsSuccessful);
        Assert.False(result.IsFailed);
    }
}
