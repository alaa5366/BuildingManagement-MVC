using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BuildingManagementMvc.Attributes;

/// <summary>
/// Authorization filter requiring an authenticated administrator account and an explicit permission claim.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AdminPermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    /// <summary>
    /// Gets the permission key enforced by this attribute.
    /// </summary>
    public string Permission => _permission;

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

        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.ChallengeResult();
            return;
        }

        // Roles identify the account class; they never grant permission by themselves.
        if (!user.IsInRole("admin") && !user.IsInRole("superadmin"))
        {
            context.Result = new Microsoft.AspNetCore.Mvc.ForbidResult();
            return;
        }

        if (user.HasClaim("perm", _permission)) return;

        context.Result = new ContentResult
        {
            StatusCode = 403,
            ContentType = "text/plain; charset=utf-8",
            Content = Loc.T("You_Don_T_Have_Permission_To")
        };
    }
}