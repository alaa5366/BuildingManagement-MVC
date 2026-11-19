using System.Security.Claims;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class TargetAuthorizationResourceResolverTests
{
    private readonly DefaultAuthorizationPermissionCatalog _catalog = new();

    [Fact]
    public void Resolve_UsesTheSingleBuildingBoundToTheSession()
    {
        var principal = Principal("building-17");

        var resource = TargetAuthorizationResourceResolver.Resolve(
            principal, _catalog, "residents.manage");

        Assert.Equal(AuthorizationScopeType.Building, resource.ScopeType);
        Assert.Equal("building-17", resource.BuildingId);
        Assert.Null(resource.ApartmentId);
    }

    [Fact]
    public void Resolve_UsesSystemScopeForSystemOnlyPermission()
    {
        var principal = Principal("building-17");

        var resource = TargetAuthorizationResourceResolver.Resolve(
            principal, _catalog, "admins.manage");

        Assert.Equal(AuthorizationScopeType.System, resource.ScopeType);
    }

    [Fact]
    public void Resolve_FailsClosedWhenBuildingScopeIsAmbiguous()
    {
        var principal = Principal("building-17", "building-29");

        var resource = TargetAuthorizationResourceResolver.Resolve(
            principal, _catalog, "apartment.access.grant.manage");

        Assert.Equal(AuthorizationScopeType.Building, resource.ScopeType);
        Assert.Null(resource.BuildingId);
    }

    [Fact]
    public void Resolve_UsesSystemScopeWhenNoBuildingClaimExistsAndSystemGrantIsSupported()
    {
        var principal = Principal();

        var resource = TargetAuthorizationResourceResolver.Resolve(
            principal, _catalog, "residents.manage");

        Assert.Equal(AuthorizationScopeType.System, resource.ScopeType);
    }

    private static ClaimsPrincipal Principal(params string[] buildingIds)
    {
        var claims = buildingIds.Select(value => new Claim("buildingId", value)).ToList();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}
