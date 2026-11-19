using BuildingManagementMvc.Data.Entities;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class ApartmentAccessPermissionProfileCatalogTests
{
    [Theory]
    [InlineData(ApartmentAccessGrantType.Family, "apartment.access.family.v1")]
    [InlineData(ApartmentAccessGrantType.Tenant, "apartment.access.tenant.v1")]
    [InlineData(ApartmentAccessGrantType.Caretaker, "apartment.access.caretaker.v1")]
    [InlineData(ApartmentAccessGrantType.Emergency, "apartment.access.emergency.v1")]
    [InlineData(ApartmentAccessGrantType.Temporary, "apartment.access.temporary.v1")]
    public void IsValidFor_AcceptsOnlyTheVersionedProfileAssignedToGrantType(
        ApartmentAccessGrantType grantType,
        string permissionSetKey)
    {
        Assert.Equal(permissionSetKey, ApartmentAccessPermissionProfileCatalog.For(grantType));
        Assert.True(ApartmentAccessPermissionProfileCatalog.IsValidFor(grantType, permissionSetKey));
    }

    [Fact]
    public void IsValidFor_RejectsUnknownOrMismatchedProfiles()
    {
        Assert.False(ApartmentAccessPermissionProfileCatalog.IsValidFor(
            ApartmentAccessGrantType.Family,
            "apartment.access.admin.v1"));
        Assert.False(ApartmentAccessPermissionProfileCatalog.IsValidFor(
            ApartmentAccessGrantType.Family,
            ApartmentAccessPermissionProfileCatalog.Tenant));
        Assert.False(ApartmentAccessPermissionProfileCatalog.IsValidFor(
            (ApartmentAccessGrantType)255,
            ApartmentAccessPermissionProfileCatalog.Family));
    }
}
