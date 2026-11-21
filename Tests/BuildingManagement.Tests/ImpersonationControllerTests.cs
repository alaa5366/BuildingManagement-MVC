using System.Reflection;
using System.Security.Claims;
using BuildingManagementMvc.Controllers;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class ImpersonationControllerTests
{
    private static List<Claim> Restore(AdminBackupData backup, bool stillImpersonating)
    {
        var controller = new ImpersonationController(null!, null!, NullLogger<ImpersonationController>.Instance);
        var method = typeof(ImpersonationController).GetMethod(
            "BuildBackupClaims",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        return (List<Claim>)method.Invoke(controller, new object[] { backup, stillImpersonating })!;
    }

    [Fact]
    public void Returning_from_resident_to_impersonated_admin_restores_building_scope_and_original_actor()
    {
        const string buildingId = "building-a";
        const string superAdminId = "11111111-1111-1111-1111-111111111111";
        const string adminPermission = "residents.manage";

        var claims = Restore(new AdminBackupData
        {
            Uid = superAdminId,
            Name = "Root",
            Email = "root@example.test",
            Role = "admin",
            BuildingId = buildingId,
            BuildingIds = new List<string>(),
            Permissions = new List<string> { adminPermission },
            ImpersonationActorUid = superAdminId,
            ImpersonationActorRole = "superadmin",
            ImpersonationActorEmail = "root@example.test",
            ImpersonationActorPermissions = "[]",
            ImpersonationActorTwoFactorVerified = "true",
            ImpersonationActorSecurityStamp = "root-stamp",
            TwoFactorVerified = true
        }, stillImpersonating: true);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        Assert.Equal("admin", principal.FindFirst(ClaimTypes.Role)?.Value);
        Assert.Equal("true", principal.FindFirst("targetIdentityAccount")?.Value);
        Assert.Equal("root-stamp", principal.FindFirst("securityStamp")?.Value);
        Assert.Contains(principal.FindAll("buildingId"), claim => claim.Value == buildingId);
        Assert.Equal(adminPermission, principal.FindFirst("perm")?.Value);
        Assert.Equal("true", principal.FindFirst("impersonated")?.Value);
        Assert.Equal(superAdminId, principal.FindFirst("impersonationActorUid")?.Value);
        Assert.Equal("superadmin", principal.FindFirst("impersonationActorRole")?.Value);
        Assert.Equal(buildingId, principal.FindFirst("impersonationTargetBuildingId")?.Value);
        Assert.Equal("true", principal.FindFirst("twoFactorVerified")?.Value);
    }

    [Fact]
    public void Returning_from_admin_to_original_superadmin_clears_impersonation_context()
    {
        const string superAdminId = "22222222-2222-2222-2222-222222222222";

        var claims = Restore(new AdminBackupData
        {
            Uid = superAdminId,
            Name = "Root",
            Email = "root@example.test",
            Role = "superadmin",
            BuildingId = "building-a",
            ImpersonationActorSecurityStamp = "original-superadmin-stamp",
            TwoFactorVerified = true
        }, stillImpersonating: false);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        Assert.Equal("superadmin", principal.FindFirst(ClaimTypes.Role)?.Value);
        Assert.Equal("true", principal.FindFirst("targetIdentityAccount")?.Value);
        Assert.Equal("original-superadmin-stamp", principal.FindFirst("securityStamp")?.Value);
        Assert.Null(principal.FindFirst("impersonated"));
        Assert.Null(principal.FindFirst("impersonationActorUid"));
        Assert.Null(principal.FindFirst("impersonationTargetBuildingId"));
    }

    [Fact]
    public void Returning_to_original_admin_restores_canonical_identity_and_security_stamp()
    {
        var claims = Restore(new AdminBackupData
        {
            Uid = "44444444-4444-4444-4444-444444444444",
            Name = "Building Admin",
            Email = "admin@example.test",
            Role = "admin",
            BuildingId = "building-a",
            BuildingIds = new List<string> { "building-a" },
            Permissions = new List<string> { "residents.manage" },
            ImpersonationActorSecurityStamp = "admin-security-stamp",
            TwoFactorVerified = true
        }, stillImpersonating: false);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        Assert.Equal("admin", principal.FindFirst(ClaimTypes.Role)?.Value);
        Assert.Equal("true", principal.FindFirst("targetIdentityAccount")?.Value);
        Assert.Equal("admin-security-stamp", principal.FindFirst("securityStamp")?.Value);
        Assert.Null(principal.FindFirst("impersonated"));
        Assert.Contains(principal.FindAll("buildingId"), claim => claim.Value == "building-a");
        Assert.Contains(principal.FindAll("perm"), claim => claim.Value == "residents.manage");
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("superadmin")]
    public void Entering_nested_impersonation_captures_current_identity_security_stamp(string role)
    {
        const string accountId = "33333333-3333-3333-3333-333333333333";
        const string stamp = "current-account-security-stamp";
        var controller = new ImpersonationController(null!, null!, NullLogger<ImpersonationController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, accountId),
                        new Claim(ClaimTypes.Name, "Operator"),
                        new Claim(ClaimTypes.Email, "operator@example.test"),
                        new Claim(ClaimTypes.Role, role),
                        new Claim("securityStamp", stamp),
                        new Claim("twoFactorVerified", "true")
                    }, "test"))
                }
            }
        };
        var claims = new List<Claim>();
        var method = typeof(ImpersonationController).GetMethod(
            "AddImpersonationActorClaims",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        method.Invoke(controller, new object?[] { claims, "building-a", "apartment-a" });

        Assert.Equal(stamp, claims.Single(claim => claim.Type == "impersonationActorSecurityStamp").Value);
    }

    [Fact]
    public void Backup_stack_refuses_a_sixth_layer_without_losing_existing_return_contexts()
    {
        var service = new ImpersonationService(
            null!,
            NullLogger<ImpersonationService>.Instance,
            DataProtectionProvider.Create("ImpersonationControllerTests"));
        var context = new DefaultHttpContext();

        for (var index = 0; index < 5; index++)
            Assert.True(service.PushBackup(context, new AdminBackupData { Role = $"role-{index}" }));

        Assert.False(service.PushBackup(context, new AdminBackupData { Role = "overflow" }));
        Assert.Equal(5, service.StackDepth(context));
        Assert.Equal("role-4", service.PeekBackup(context)?.Role);
    }
}
