using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManagementMvc.Attributes;

/// <summary>
/// Checks an explicit administrator permission. Target-schema identities are evaluated
/// against live role assignments and account overrides; legacy identities retain the
/// transitional permission-claim behavior.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class HasPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;

    /// <summary>Initializes the filter with a permission key.</summary>
    public HasPermissionAttribute(string permission) => _permission = permission;

    /// <inheritdoc />
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new RedirectToActionResult("LoginChoice", "Account", null);
            return;
        }

        var role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role != "admin" && role != "superadmin")
        {
            context.Result = new ForbidResult();
            return;
        }

        if (string.Equals(user.FindFirst("targetIdentityAccount")?.Value, "true", StringComparison.Ordinal))
        {
            var evaluator = context.HttpContext.RequestServices.GetRequiredService<AuthorizationEvaluator>();
            var result = await evaluator.EvaluateAsync(
                user,
                _permission,
                new ResolvedAuthorizationResource(AuthorizationScopeType.System),
                DateTime.UtcNow,
                context.HttpContext.RequestAborted);

            if (result.Decision == AuthorizationDecision.Allow)
                return;

            context.Result = new ForbidResult();
            return;
        }

        if (!user.HasClaim("perm", _permission))
            context.Result = new ForbidResult();
    }
}
