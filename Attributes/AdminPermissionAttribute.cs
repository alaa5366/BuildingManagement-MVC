using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManagementMvc.Attributes;

/// <summary>
/// Authorization filter requiring an authenticated administrator and an explicit permission.
/// Target-schema identities are evaluated against current role assignments and overrides;
/// transitional legacy identities continue to use their persisted permission claims.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AdminPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;

    /// <summary>Gets the permission enforced by this attribute.</summary>
    public string Permission => _permission;

    /// <summary>Initializes a permission filter.</summary>
    public AdminPermissionAttribute(string permission) => _permission = permission;

    /// <inheritdoc />
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        // Roles identify the account class; they never grant permission by themselves.
        if (!user.IsInRole("admin") && !user.IsInRole("superadmin"))
        {
            context.Result = new ForbidResult();
            return;
        }

        // Target identities must be checked against live target-schema grants on every
        // request. Never trust the permission claims copied into an older cookie.
        if (string.Equals(user.FindFirst("targetIdentityAccount")?.Value, "true", StringComparison.Ordinal))
        {
            var services = context.HttpContext.RequestServices;
            var evaluator = services.GetRequiredService<AuthorizationEvaluator>();
            var catalog = services.GetRequiredService<IAuthorizationPermissionCatalog>();
            var resource = TargetAuthorizationResourceResolver.Resolve(user, catalog, _permission);
            var decision = await evaluator.EvaluateAsync(
                user,
                _permission,
                resource,
                DateTime.UtcNow,
                context.HttpContext.RequestAborted);

            if (decision.Decision == AuthorizationDecision.Allow)
                return;

            context.Result = new ContentResult
            {
                StatusCode = StatusCodes.Status403Forbidden,
                ContentType = "text/plain; charset=utf-8",
                Content = decision.Decision == AuthorizationDecision.RequiresStepUp
                    ? "Additional authentication is required."
                    : Loc.T("You_Don_T_Have_Permission_To")
            };
            return;
        }

        // Transitional legacy accounts retain the existing claim-based policy.
        if (user.HasClaim("perm", _permission))
            return;

        context.Result = new ContentResult
        {
            StatusCode = StatusCodes.Status403Forbidden,
            ContentType = "text/plain; charset=utf-8",
            Content = Loc.T("You_Don_T_Have_Permission_To")
        };
    }
}
