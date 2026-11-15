using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BuildingManagementMvc.Attributes;

/// <summary>
/// Authorization filter that requires the signed-in admin to hold a specific permission. Super admins always pass; residents never do.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AdminPermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminPermissionAttribute"/> class.
    /// </summary>
    public AdminPermissionAttribute(string permission) => _permission = permission;

    /// <summary>
    /// Handles the authorization.
    /// </summary>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // مش مسجّل دخول => [Authorize] هو اللي بيتصرف
        if (user.Identity?.IsAuthenticated != true) return;
        if (!user.IsInRole("admin")) return;

        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        // الفحص شغال دايمًا. مفتاح طوارئ فقط: Auth:EnforceAdminPermissions=false
        if (config.GetValue<bool?>("Auth:EnforceAdminPermissions") == false) return;

        if (user.HasClaim("perm", _permission)) return;

        context.Result = new ContentResult
        {
            StatusCode = 403,
            ContentType = "text/plain; charset=utf-8",
            Content = Loc.T("You_Don_T_Have_Permission_To")
        };
    }
}