using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class AuthHelpersTests
{
    // ================================================================
    // NormalizePhone
    // ================================================================

    [Theory]
    [InlineData("01001234567", "+201001234567")]       // 11 رقم يبدأ بـ 0
    [InlineData("+201001234567", "+201001234567")]     // E.164
    [InlineData("201001234567", "+201001234567")]      // من غير +
    [InlineData("00201001234567", "+201001234567")]    // 00 بدل +
    [InlineData("1001234567", "+201001234567")]        // 10 أرقام (من غير 0)
    public void NormalizePhone_EgyptianFormats_ReturnsE164(string input, string expected)
    {
        Assert.Equal(expected, AuthHelpers.NormalizePhone(input));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("abc", "")]
    [InlineData("---", "")]
    public void NormalizePhone_NullOrNoDigits_ReturnsEmpty(string? input, string expected)
    {
        Assert.Equal(expected, AuthHelpers.NormalizePhone(input));
    }

    [Fact]
    public void NormalizePhone_WithSpaces_StripsThem()
    {
        Assert.Equal("+201001234567", AuthHelpers.NormalizePhone("+20 100 123 4567"));
    }

    [Fact]
    public void NormalizePhone_WithDashes_StripsThem()
    {
        Assert.Equal("+201001234567", AuthHelpers.NormalizePhone("+20-100-123-4567"));
    }

    [Fact]
    public void NormalizePhone_AlreadyNormalized_ReturnsSame()
    {
        Assert.Equal("+201001234567", AuthHelpers.NormalizePhone("+201001234567"));
    }

    // ================================================================
    // ResidentInternalEmail
    // ================================================================

    [Fact]
    public void ResidentInternalEmail_BuildsCorrectFormat()
    {
        var email = AuthHelpers.ResidentInternalEmail("ABC123", 2, 5);
        Assert.Equal("res-abc123-2-5@building.local", email);
    }

    [Fact]
    public void ResidentInternalEmail_LowercasesBuildingId()
    {
        var email = AuthHelpers.ResidentInternalEmail("UPPERCASE", 0, 1);
        Assert.Equal("res-uppercase-0-1@building.local", email);
    }

    [Fact]
    public void ResidentInternalEmail_HandlesNegativeFloorOrder()
    {
        // البدروم = -1
        var email = AuthHelpers.ResidentInternalEmail("ABC", -1, 3);
        Assert.Equal("res-abc--1-3@building.local", email);
    }

    // ================================================================
    // ResidentPassword
    // ================================================================

    [Fact]
    public void ResidentPassword_BuildsCorrectFormat()
    {
        var pwd = AuthHelpers.ResidentPassword("ABC123", 5, "1234");
        // آخر 6 حروف من "ABC123" هي "ABC123" نفسها → lowercase → "abc123"
        Assert.Equal("res-abc123-5-1234", pwd);
    }

    [Fact]
    public void ResidentPassword_ShortBuildingId_UsesWholeId()
    {
        var pwd = AuthHelpers.ResidentPassword("AB", 1, "0000");
        Assert.Equal("res-ab-1-0000", pwd);
    }

    [Fact]
    public void ResidentPassword_LongBuildingId_UsesLast6()
    {
        var pwd = AuthHelpers.ResidentPassword("VERYLONGBUILDINGID", 1, "0000");
        // آخر 6 حروف من "VERYLONGBUILDINGID" هي "DINGID" → lowercase → "dingid"
        Assert.Equal("res-dingid-1-0000", pwd);
    }

    // ================================================================
    // AdminEmailForPhone
    // ================================================================

    [Fact]
    public void AdminEmailForPhone_BuildsCorrectFormat()
    {
        var email = AuthHelpers.AdminEmailForPhone("BLD-001", "01001234567");
        Assert.Equal("adm-ld-001-01001234567@building.local", email);

    }

    [Fact]
    public void AdminEmailForPhone_StripsNonDigitsFromPhone()
    {
        var email = AuthHelpers.AdminEmailForPhone("BLD-001", "+20 100 123 4567");
        // بياخد الأرقام بس من الرقم
        Assert.Equal("adm-ld-001-201001234567@building.local", email);
    }

    // ================================================================
    // AdminPasswordForPhone
    // ================================================================

    [Fact]
    public void AdminPasswordForPhone_BuildsCorrectFormat()
    {
        var pwd = AuthHelpers.AdminPasswordForPhone("01001234567", "1234");
        Assert.Equal("adm-01001234567-1234", pwd);
    }

    [Fact]
    public void AdminPasswordForPhone_StripsNonDigitsFromPhone()
    {
        var pwd = AuthHelpers.AdminPasswordForPhone("+20 100 123 4567", "1234");
        Assert.Equal("adm-201001234567-1234", pwd);
    }
}