using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class WhatsAppTemplateTests
{
    [Fact]
    public void Welcome_message_has_setpin_link_and_never_contains_the_pin()
    {
        var svc = new WhatsAppTemplateService();
        var building = new Building { Name = "Nile Tower" };
        var apt = new Apartment { Id = "a1", Number = 3, Owner = "Sara", Phone = "+201001234567", Pin = "9876" };

        var text = svc.Render("welcome", building, apt, new Dictionary<string, string>
        {
            ["setpin_link"] = "https://example.test/Account/SetPin?token=abc",
            ["quick_link"] = "https://example.test/Account/QuickLogin?token=xyz",
            ["login_url"] = "https://example.test/Account/UnifiedLogin"
        });

        Assert.Contains("https://example.test/Account/SetPin?token=abc", text);
        Assert.DoesNotContain("9876", text);
        Assert.DoesNotContain("{pin}", text);
        Assert.DoesNotContain("{setpin_link}", text);
    }
}
