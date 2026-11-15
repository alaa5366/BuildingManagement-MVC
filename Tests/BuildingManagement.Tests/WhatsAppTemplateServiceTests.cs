using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class WhatsAppTemplateServiceTests
{
    private readonly WhatsAppTemplateService _service = new();

    private static Building CreateBuilding()
    {
        var b = new Building
        {
            Name = "Test Tower",
            PaymentInfo = new PaymentInfo { Label = "Instapay", AccountNumber = "123456", Phone = "01001234567" }
        };
        return b;
    }

    private static Apartment CreateApartment()
    {
        return new Apartment { Id = "a1", Number = 5, Owner = "Ali", Phone = "01001234567", FloorId = "f1" };
    }

    // ================================================================
    // Render — القوالب الأساسية
    // ================================================================

    [Fact]
    public void Render_Reminder_ReplacesBasicPlaceholders()
    {
        var b = CreateBuilding();
        var apt = CreateApartment();
        var extra = new Dictionary<string, string>
        {
            ["amount"] = "500",
            ["month"] = "يناير 2025"
        };

        var result = _service.Render("reminder", b, apt, extra);

        Assert.Contains("مرحباً Ali", result);
        Assert.Contains("Test Tower", result);
        Assert.Contains("الشقة: 5", result);
        Assert.Contains("المبلغ المطلوب: 500", result);
        Assert.Contains("يناير 2025", result);
        Assert.Contains("Instapay", result);
        Assert.DoesNotContain("{owner}", result);
        Assert.DoesNotContain("{amount}", result);
    }

    [Fact]
    public void Render_Confirm_ReplacesPlaceholders()
    {
        var b = CreateBuilding();
        var apt = CreateApartment();
        var extra = new Dictionary<string, string>
        {
            ["amount"] = "500",
            ["month"] = "يناير 2025"
        };

        var result = _service.Render("confirm", b, apt, extra);

        Assert.Contains("تم تأكيد دفعتكم", result);
        Assert.Contains("500", result);
        Assert.DoesNotContain("{amount}", result);
    }

    [Fact]
    public void Render_Expense_ReplacesPlaceholders()
    {
        var b = CreateBuilding();
        var apt = CreateApartment();
        var extra = new Dictionary<string, string>
        {
            ["note"] = "صيانة الأسانسير",
            ["amount"] = "1000",
            ["share"] = "250",
            ["month"] = "يناير 2025"
        };

        var result = _service.Render("expense", b, apt, extra);

        Assert.Contains("صيانة الأسانسير", result);
        Assert.Contains("1000", result);
        Assert.Contains("250", result);
    }

    [Fact]
    public void Render_Revenue_ReplacesPlaceholders()
    {
        var b = CreateBuilding();
        var apt = CreateApartment();
        var extra = new Dictionary<string, string>
        {
            ["note"] = "إيجار السطح",
            ["amount"] = "500",
            ["share"] = "125",
            ["month"] = "يناير 2025"
        };

        var result = _service.Render("revenue", b, apt, extra);

        Assert.Contains("إيجار السطح", result);
        Assert.Contains("+125", result);
    }

    [Fact]
    public void Render_General_ReplacesMessage()
    {
        var b = CreateBuilding();
        var apt = CreateApartment();
        var extra = new Dictionary<string, string> { ["message"] = "اجتماع يوم الجمعة" };

        var result = _service.Render("general", b, apt, extra);

        Assert.Contains("اجتماع يوم الجمعة", result);
        Assert.Contains("Test Tower", result);
    }

    [Fact]
    public void Render_InvalidTemplate_ReturnsEmptyString()
    {
        var b = CreateBuilding();
        var apt = CreateApartment();
        Assert.Equal("", _service.Render("invalid-template", b, apt, new Dictionary<string, string>()));
    }

    // ================================================================
    // Render — حالات خاصة
    // ================================================================

    [Fact]
    public void Render_NoOwner_FallsBackToApartmentNumber()
    {
        var b = CreateBuilding();
        var apt = new Apartment { Number = 10, Owner = "" }; // مفيش مالك
        var extra = new Dictionary<string, string>();

        var result = _service.Render("general", b, apt, extra);

        Assert.Contains("ساكن شقة 10", result);
    }

    [Fact]
    public void Render_NoFloorLabel_ShowsDash()
    {
        var b = CreateBuilding();
        var apt = CreateApartment();
        b.Floors = new List<Floor>(); // مفيش أدوار

        var result = _service.Render("welcome", b, apt, new Dictionary<string, string>());

        Assert.Contains("الدور: —", result);
    }

    // ================================================================
    // BuildWaLink
    // ================================================================

    [Fact]
    public void BuildWaLink_FormatsPhoneCorrectly()
    {
        var link = WhatsAppTemplateService.BuildWaLink("+201001234567", "Hello");

        Assert.StartsWith("https://wa.me/201001234567?text=", link);
        Assert.Contains("Hello", link);
    }

    [Fact]
    public void BuildWaLink_EscapesSpecialCharacters()
    {
        var link = WhatsAppTemplateService.BuildWaLink("0100", "Hello World & Test");
        Assert.Contains("Hello%20World%20%26%20Test", link);
    }

    // ================================================================
    // BuildLoginUrl
    // ================================================================

    [Fact]
    public void BuildLoginUrl_AppendsUnifiedLoginPath()
    {
        var url = WhatsAppTemplateService.BuildLoginUrl("https://example.com");
        Assert.Equal("https://example.com/Account/UnifiedLogin", url);
    }

    [Fact]
    public void BuildLoginUrl_HandlesTrailingSlash()
    {
        var url = WhatsAppTemplateService.BuildLoginUrl("https://example.com/");
        Assert.Equal("https://example.com/Account/UnifiedLogin", url);
    }
}