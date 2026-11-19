using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class ApartmentAccessGrantValidatorTests
{
    private static readonly DateTime StartUtc = new(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(ApartmentAccessGrantType.Emergency)]
    [InlineData(ApartmentAccessGrantType.Temporary)]
    public void ValidateGrantRequest_RequiresExpiryForEmergencyAndTemporary(
        ApartmentAccessGrantType grantType)
    {
        var request = Request(grantType, validToUtc: null);

        Assert.Throws<ArgumentException>(() => ApartmentAccessGrantValidator.ValidateGrantRequest(request));
    }

    [Fact]
    public void ValidateGrantRequest_AllowsUnboundedFamilyGrant()
    {
        var request = Request(ApartmentAccessGrantType.Family, validToUtc: null);

        ApartmentAccessGrantValidator.ValidateGrantRequest(request);
    }

    [Fact]
    public void ValidateGrantRequest_RejectsNonUtcDates()
    {
        var request = Request(ApartmentAccessGrantType.Tenant, validToUtc: StartUtc.AddDays(1)) with
        {
            ValidFromUtc = DateTime.SpecifyKind(StartUtc, DateTimeKind.Local)
        };

        Assert.Throws<ArgumentException>(() => ApartmentAccessGrantValidator.ValidateGrantRequest(request));
    }

    [Fact]
    public void ValidateGrantRequest_RejectsUnknownGrantType()
    {
        var request = Request((ApartmentAccessGrantType)99, validToUtc: StartUtc.AddDays(1));

        Assert.Throws<ArgumentException>(() => ApartmentAccessGrantValidator.ValidateGrantRequest(request));
    }

    [Fact]
    public void ValidateRevokeRequest_RequiresReason()
    {
        var request = new RevokeApartmentAccessRequest(Guid.NewGuid(), Guid.NewGuid(), "  ");

        Assert.Throws<ArgumentException>(() => ApartmentAccessGrantValidator.ValidateRevokeRequest(request));
    }

    private static GrantApartmentAccessRequest Request(
        ApartmentAccessGrantType grantType,
        DateTime? validToUtc) => new(
            "building-1",
            "apartment-1",
            Guid.NewGuid(),
            grantType,
            StartUtc,
            validToUtc,
            "approved request",
            Guid.NewGuid());
}
