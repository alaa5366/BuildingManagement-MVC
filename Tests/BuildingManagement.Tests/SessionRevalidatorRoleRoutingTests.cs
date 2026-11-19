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
    public void SuperAdminSecurityStamp_ChangesWhenCredentialOrAccountStateChanges()
    {
        var updatedAt = new DateTime(2026, 11, 19, 8, 0, 0, DateTimeKind.Utc);
        var original = SqlAuthService.CreateSuperAdminSecurityStamp("bcrypt-hash-a", updatedAt, true, false);
        var changedPassword = SqlAuthService.CreateSuperAdminSecurityStamp("bcrypt-hash-b", updatedAt, true, false);
        var changedStatus = SqlAuthService.CreateSuperAdminSecurityStamp("bcrypt-hash-a", updatedAt, false, true);

        Assert.NotEqual(original, changedPassword);
        Assert.NotEqual(original, changedStatus);
        Assert.DoesNotContain("bcrypt-hash-a", original, StringComparison.Ordinal);
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
