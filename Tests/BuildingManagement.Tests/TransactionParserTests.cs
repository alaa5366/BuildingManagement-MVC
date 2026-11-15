using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class TransactionParserTests
{
    private readonly TransactionParser _parser = new();

    // ================================================================
    // Parse - Null / Empty / Invalid
    // ================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrNull_ReturnsNull(string? text)
    {
        Assert.Null(_parser.Parse(text!));
    }

    [Fact]
    public void Parse_NoAmountFound_ReturnsDataWithZeroAmount()
    {
        var result = _parser.Parse("Some random text without numbers");
        Assert.NotNull(result);
        Assert.Equal(0, result.Amount);
        Assert.False(result.IsValid);
    }

    // ================================================================
    // Parse - Amount
    // ================================================================

    [Theory]
    [InlineData("Amount: 1,678.50 EGP", 1678.50)]
    [InlineData("Amount: 500 EGP", 500)]
    [InlineData("Amount: 1,234,567.89", 1234567.89)]
    [InlineData("المبلغ: 250.75 ج.م", 250.75)]
    public void Parse_AmountFound_ExtractsCorrectly(string text, double expected)
    {
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.Equal(expected, result.Amount);
    }

    [Fact]
    public void Parse_MultipleNumbers_PrefersCommaSeparatedFormat()
    {
        var text = "Reference: 123456789012 Amount: 1,500.00";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.Equal(1500, result.Amount);
    }

    // ================================================================
    // Parse - Reference
    // ================================================================

    [Fact]
    public void Parse_ReferenceWithKeyword_ExtractsReference()
    {
        var text = "Reference: 123456789012";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.Equal("123456789012", result.Reference);
    }

    [Fact]
    public void Parse_TwelveDigitNumber_ExtractsAsReference()
    {
        var text = "Some text 707714830636 some more text";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.Equal("707714830636", result.Reference);
    }

    [Fact]
    public void Parse_ShortNumber_NotExtractedAsReference()
    {
        var text = "Amount: 12345";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.Equal("", result.Reference);
    }

    // ================================================================
    // Parse - Date
    // ================================================================

    [Fact]
    public void Parse_FullDateFormat_ExtractsDate()
    {
        var text = "01 Sep 2026 09:23 PM";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.Equal("01 Sep 2026 09:23 PM", result.Date);
    }

    [Fact]
    public void Parse_NoDate_DateIsEmpty()
    {
        var text = "Transaction without date";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.Equal("", result.Date);
    }

    // ================================================================
    // Parse - Status
    // ================================================================

    [Fact]
    public void Parse_TransactionSuccessful_SetsIsSuccessful()
    {
        var text = "Transaction Successful";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
        Assert.False(result.IsFailed);
    }

    [Fact]
    public void Parse_TransactionDetailsWithoutSuccessful_SetsIsFailed()
    {
        var text = "Transaction Details";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.False(result.IsSuccessful);
        Assert.True(result.IsFailed);
    }

    [Fact]
    public void Parse_TransactionSuccessfulTakesPrecedence_IsFailedFalse()
    {
        var text = "Transaction Successful Transaction Details";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
        Assert.False(result.IsFailed);
    }

    // ================================================================
    // Parse - Account
    // ================================================================

    [Fact]
    public void Parse_BankMisrAccount_ExtractsAccount()
    {
        var text = "Account: 8020220000001299";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.Equal("8020220000001299", result.RecipientAccount);
    }

    [Fact]
    public void Parse_EGAccount_ExtractsDigits()
    {
        var text = "IBAN: EG380019000500000000263180002";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.Equal("0019000500000000263180002", result.RecipientAccount);
    }

    [Fact]
    public void Parse_NoAccount_AccountIsEmpty()
    {
        var text = "Just amount: 500";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.Equal("", result.RecipientAccount);
    }

    // ================================================================
    // Parse - From
    // ================================================================

    [Fact]
    public void Parse_FromField_ExtractsFrom()
    {
        var text = "From\nAhmed Ali";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.Equal("Ahmed Ali", result.From);
    }

    [Fact]
    public void Parse_FromFieldOnSameLine_ExtractsFrom()
    {
        var text = "From: Sara Mohamed";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        // Regex بيشتغل على "From\s*\n?\s*" — يعني نفس السطر برضه شغال
        Assert.Equal(": Sara Mohamed", result.From);
    }

    // ================================================================
    // IsValid
    // ================================================================

    [Fact]
    public void Parse_ValidTransaction_IsValidTrue()
    {
        var text = "Amount: 500\nReference: 123456789012";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Parse_MissingAmount_IsValidFalse()
    {
        var text = "Reference: 123456789012";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Parse_MissingReference_IsValidFalse()
    {
        var text = "Amount: 500";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.False(result.IsValid);
    }

    // ================================================================
    // Parse - Full real-world example
    // ================================================================

    [Fact]
    public void Parse_FullRealWorldText_ExtractsEverything()
    {
        var text = @"
Transaction Successful
Amount: 1,678.50 EGP
Reference: 707714830636
Date: 01 Sep 2026 09:23 PM
Account: 8020220000001299
From
Ahmed Ali
";
        var result = _parser.Parse(text);

        Assert.NotNull(result);
        Assert.Equal(1678.50, result.Amount);
        Assert.Equal("707714830636", result.Reference);
        Assert.Equal("01 Sep 2026 09:23 PM", result.Date);
        Assert.True(result.IsSuccessful);
        Assert.Equal("8020220000001299", result.RecipientAccount);
        Assert.Equal("Ahmed Ali", result.From);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Parse_RawTextIsPreserved()
    {
        var text = "Amount: 500";
        var result = _parser.Parse(text);
        Assert.NotNull(result);
        Assert.Equal(text, result.RawText);
    }
}