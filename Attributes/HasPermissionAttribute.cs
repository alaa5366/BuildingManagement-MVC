using BuildingManagementMvc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace BuildingManagementMvc.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class HasPermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    public HasPermissionAttribute(string permission) => _permission = permission;

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