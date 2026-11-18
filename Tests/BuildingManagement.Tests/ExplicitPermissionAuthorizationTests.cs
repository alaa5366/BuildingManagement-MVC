using System.Security.Claims;
using BuildingManagementMvc.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace BuildingManagement.Tests;

public sealed class ExplicitPermissionAuthorizationTests
{
    private static AuthorizationFilterContext CreateContext(string? role, params string[] permissions)
    {
        var claims = new List<Claim>();
        if (role != null) claims.Add(new Claim(ClaimTypes.Role, role));
        claims.AddRange(permissions.Select(p => new Claim("perm", p)));
        var identity = new ClaimsIdentity(claims, role == null ? null : "Test");
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("superadmin")]
    public void AdminPermission_RequiresExplicitGrantForEveryAdministratorRole(string role)
    {
        var context = CreateContext(role);
        new AdminPermissionAttribute("backup.manage").OnAuthorization(context);
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("superadmin")]
    public void AdminPermission_AllowsExplicitGrant(string role)
    {
        var context = CreateContext(role, "backup.manage");
        new AdminPermissionAttribute("backup.manage").OnAuthorization(context);
        Assert.Null(context.Result);
    }

    [Fact]
    public void AdminPermission_RejectsResidentEvenWhenPermissionClaimExists()
    {
        var context = CreateContext("resident", "backup.manage");
        new AdminPermissionAttribute("backup.manage").OnAuthorization(context);
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public void AdminPermission_ChallengesAnonymousPrincipal()
    {
        var context = CreateContext(null);
        new AdminPermissionAttribute("backup.manage").OnAuthorization(context);
        Assert.IsType<ChallengeResult>(context.Result);
    }

    [Fact]
    public void HasPermission_DoesNotBypassSuperAdminRole()
    {
        var context = CreateContext("superadmin");
        new HasPermissionAttribute("secret.access").OnAuthorization(context);
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public void HasPermission_AllowsExplicitSuperAdminGrant()
    {
        var context = CreateContext("superadmin", "secret.access");
        new HasPermissionAttribute("secret.access").OnAuthorization(context);
        Assert.Null(context.Result);
    }
}
