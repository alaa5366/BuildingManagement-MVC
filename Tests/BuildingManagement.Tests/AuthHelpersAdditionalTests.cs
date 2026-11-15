using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class AuthHelpersAdditionalTests
{
    [Fact]
    public void ResidentPassword_WithLongId_UsesLast6Characters()
    {
        var pwd = AuthHelpers.ResidentPassword("BUILDING-ABC-123456", 5, "1234");
        // آخر 6 حروف: "123456"
        Assert.Equal("res-123456-5-1234", pwd);
    }

    [Fact]
    public void ResidentPassword_WithShortId_UsesWholeId()
    {
        var pwd = AuthHelpers.ResidentPassword("ABC", 1, "0000");
        Assert.Equal("res-abc-1-0000", pwd);
    }

    [Fact]
    public void AdminEmailForPhone_WithDifferentPhones_ProducesDifferentEmails()
    {
        var email1 = AuthHelpers.AdminEmailForPhone("BLD-001", "01001234567");
        var email2 = AuthHelpers.AdminEmailForPhone("BLD-001", "01009876543");
        Assert.NotEqual(email1, email2);
    }

    [Fact]
    public void AdminEmailForPhone_WithDifferentBuildings_ProducesDifferentEmails()
    {
        var email1 = AuthHelpers.AdminEmailForPhone("BLD-001", "01001234567");
        var email2 = AuthHelpers.AdminEmailForPhone("BLD-002", "01001234567");
        Assert.NotEqual(email1, email2);
    }

    [Fact]
    public void NormalizePhone_WithVariousPrefixes_HandlesAll()
    {
        // +20
        Assert.Equal("+201001234567", AuthHelpers.NormalizePhone("+201001234567"));
        // 0020
        Assert.Equal("+201001234567", AuthHelpers.NormalizePhone("00201001234567"));
        // 0
        Assert.Equal("+201001234567", AuthHelpers.NormalizePhone("01001234567"));
        // Just digits
        Assert.Equal("+201001234567", AuthHelpers.NormalizePhone("201001234567"));
    }

    [Fact]
    public void ResidentInternalEmail_IncludesFloorAndAptNumber()
    {
        var email = AuthHelpers.ResidentInternalEmail("BLD-001", 3, 7);
        Assert.Contains("-3-7@", email);
    }
}