using System.Reflection;
using System.Security.Claims;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class SessionRevalidatorRoleRoutingTests
{
    private static bool IsSuperAdminPrincipal(ClaimsPrincipal principal)
    {
        var method = typeof(SessionRevalidator).GetMethod(
            "IsSuperAdminPrincipal",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);
        return Assert.IsType<bool>(method!.Invoke(null, new object[] { principal }));
    }

    [Fact]
    public void AllowListedEmailWithResidentRole_DoesNotRouteToSuperAdminValidation()
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Role, "resident"),
                new Claim(ClaimTypes.Email, "allow-listed@example.test")
            },
            "Cookies");

        Assert.False(IsSuperAdminPrincipal(new ClaimsPrincipal(identity)));
    }

    [Fact]
    public void SuperAdminRole_RoutesToSuperAdminValidation()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Role, "superadmin") },
            "Cookies");

        Assert.True(IsSuperAdminPrincipal(new ClaimsPrincipal(identity)));
    }

    [Fact]
    public void OriginalSuperAdminClaim_RoutesToSuperAdminValidation()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("originalRole", "superadmin") },
            "Cookies");

        Assert.True(IsSuperAdminPrincipal(new ClaimsPrincipal(identity)));
    }
}
