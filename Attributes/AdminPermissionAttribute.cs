using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BuildingManagementMvc.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AdminPermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    public AdminPermissionAttribute(string permission) => _permission = permission;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // مش مسجّل دخول => [Authorize] هو اللي بيتصرف
        if (user.Identity?.IsAuthenticated != true) return;
        if (!user.IsInRole("admin")) return;

        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        if (!config.GetValue<bool>("Auth:EnforceAdminPermissions")) return;

        if (user.HasClaim("perm", _permission)) return;

        context.Result = new ContentResult
        {
            StatusCode = 403,
            ContentType = "text/plain; charset=utf-8",
            Content = Loc.T("You_Don_T_Have_Permission_To")
        };
    }
}