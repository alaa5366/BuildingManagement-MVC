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
    public async Task AdminPermission_RequiresExplicitGrantForEveryAdministratorRole(string role)
    {
        var context = CreateContext(role);
        await await new AdminPermissionAttribute("backup.manage").OnAuthorizationAsync(context);
        var denied = Assert.IsType<ContentResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, denied.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(denied.Content));
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("superadmin")]
    public async Task AdminPermission_AllowsExplicitGrant(string role)
    {
        var context = CreateContext(role, "backup.manage");
        await new AdminPermissionAttribute("backup.manage").OnAuthorizationAsync(context);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task AdminPermission_RejectsResidentEvenWhenPermissionClaimExists()
    {
        var context = CreateContext("resident", "backup.manage");
        await new AdminPermissionAttribute("backup.manage").OnAuthorizationAsync(context);
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task AdminPermission_ChallengesAnonymousPrincipal()
    {
        var context = CreateContext(null);
        await new AdminPermissionAttribute("backup.manage").OnAuthorizationAsync(context);
        Assert.IsType<ChallengeResult>(context.Result);
    }

    [Fact]
    public async Task HasPermission_DoesNotBypassSuperAdminRole()
    {
        var context = CreateContext("superadmin");
        await new HasPermissionAttribute("secret.access").OnAuthorizationAsync(context);
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task HasPermission_AllowsExplicitSuperAdminGrant()
    {
        var context = CreateContext("superadmin", "secret.access");
        await new HasPermissionAttribute("secret.access").OnAuthorizationAsync(context);
        Assert.Null(context.Result);
    }
}
