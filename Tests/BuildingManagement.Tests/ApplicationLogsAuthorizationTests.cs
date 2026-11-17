using System.Reflection;
using System.Security.Claims;
using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Controllers;
using BuildingManagementMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class ApplicationLogsAuthorizationTests
{
    [Fact]
    public void Controller_RequiresAdminOrSuperAdminAndDedicatedPermission()
    {
        var authorize = typeof(ApplicationLogsController).GetCustomAttribute<AuthorizeAttribute>();
        var permission = typeof(ApplicationLogsController).GetCustomAttribute<AdminPermissionAttribute>();

        Assert.Equal("superadmin,admin", authorize?.Roles);
        Assert.NotNull(permission);
        Assert.Contains(AdminPermissions.ViewApplicationLogs, AdminPermissions.All);
        Assert.DoesNotContain(AdminPermissions.ViewApplicationLogs, AdminPermissions.TemplateAdminFull);
        Assert.DoesNotContain(AdminPermissions.ViewApplicationLogs, AdminPermissions.TemplateAdminSuper);
    }

    [Fact]
    public void AdminWithoutPermission_IsForbidden()
    {
        var context = CreateContext(new Claim(ClaimTypes.Role, "admin"));
        var filterContext = CreateFilterContext(context);

        new AdminPermissionAttribute(AdminPermissions.ViewApplicationLogs).OnAuthorization(filterContext);

        var result = Assert.IsType<ContentResult>(filterContext.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public void AdminWithPermission_IsAllowed()
    {
        var context = CreateContext(
            new Claim(ClaimTypes.Role, "admin"),
            new Claim("perm", AdminPermissions.ViewApplicationLogs));
        var filterContext = CreateFilterContext(context);

        new AdminPermissionAttribute(AdminPermissions.ViewApplicationLogs).OnAuthorization(filterContext);

        Assert.Null(filterContext.Result);
    }

    [Fact]
    public void SuperAdmin_BypassesAdminPermissionMatrix()
    {
        var context = CreateContext(new Claim(ClaimTypes.Role, "superadmin"));
        var filterContext = CreateFilterContext(context);

        new AdminPermissionAttribute(AdminPermissions.ViewApplicationLogs).OnAuthorization(filterContext);

        Assert.Null(filterContext.Result);
    }

    private static DefaultHttpContext CreateContext(params Claim[] claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
        context.RequestServices = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .BuildServiceProvider();
        return context;
    }

    private static AuthorizationFilterContext CreateFilterContext(HttpContext httpContext)
    {
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }
}
