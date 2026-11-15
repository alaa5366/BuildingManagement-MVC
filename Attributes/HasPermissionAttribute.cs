using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BuildingManagementMvc.Attributes;

/// <summary>
/// Authorization filter that checks a single named permission for admins (super admins always pass). Redirects anonymous users to the login page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class HasPermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    /// <summary>
    /// Initializes a new instance of the <see cref="HasPermissionAttribute"/> class.
    /// </summary>
    public HasPermissionAttribute(string permission) => _permission = permission;

    /// <summary>
    /// Handles the authorization.
    /// </summary>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new RedirectToActionResult("LoginChoice", "Account", null);
            return;
        }

        var role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role == "superadmin") return;

        var hasPerm = false;
        foreach (var claim in user.FindAll("perm"))
        {
            if (claim.Value == _permission) { hasPerm = true; break; }
        }

        if (!hasPerm)
            context.Result = new ForbidResult();
    }
}