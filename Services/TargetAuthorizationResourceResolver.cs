using System.Security.Claims;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Resolves the target authorization resource from signed session claims.
/// A building scope is used only when the session identifies exactly one building;
/// otherwise the resolver fails closed for permissions that require a building.
/// </summary>
public static class TargetAuthorizationResourceResolver
{
    /// <summary>Resolves the narrowest session-bound scope supported by the permission.</summary>
    public static ResolvedAuthorizationResource Resolve(
        ClaimsPrincipal principal,
        IAuthorizationPermissionCatalog catalog,
        string permissionKey)
    {
        if (!catalog.TryGet(permissionKey, out var definition))
            return new ResolvedAuthorizationResource(AuthorizationScopeType.System);

        var buildingIds = principal.FindAll("buildingId")
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (buildingIds.Length == 1 &&
            definition.AllowedGrantScopes.Contains(AuthorizationScopeType.Building))
        {
            return new ResolvedAuthorizationResource(
                AuthorizationScopeType.Building,
                BuildingId: buildingIds[0]);
        }

        if (definition.AllowedGrantScopes.Contains(AuthorizationScopeType.System))
            return new ResolvedAuthorizationResource(AuthorizationScopeType.System);

        // No trustworthy session scope is available for this permission.
        return new ResolvedAuthorizationResource(
            AuthorizationScopeType.Building,
            BuildingId: null);
    }
}
