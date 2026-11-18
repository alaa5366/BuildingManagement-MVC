using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class LegacyIdentityMappingValidatorTests
{
    [Fact]
    public void ValidateSource_RejectsUnsupportedTableAndBlankKey()
    {
        Assert.Throws<ArgumentException>(() => LegacyIdentityMappingValidator.ValidateSource("Apartments", "apt-1"));
        Assert.Throws<ArgumentException>(() => LegacyIdentityMappingValidator.ValidateSource("Users", "  "));
    }

    [Fact]
    public void ValidateReviewRequest_RequiresEvidenceForResolvedDisposition()
    {
        var request = Review(
            LegacyIdentityMappingStatus.Linked,
            Guid.NewGuid(),
            Guid.NewGuid(),
            matchMethod: "manual-review",
            evidence: null);

        Assert.Throws<ArgumentException>(() => LegacyIdentityMappingValidator.ValidateReviewRequest(request));
    }

    [Fact]
    public void ValidateSourceSpecificFields_RequiresAccountForUserSource()
    {
        var request = Review(LegacyIdentityMappingStatus.Linked, Guid.NewGuid(), accountId: null);

        Assert.Throws<ArgumentException>(() =>
            LegacyIdentityMappingValidator.ValidateSourceSpecificFields("Users", request));
    }

    [Fact]
    public void ValidateSourceSpecificFields_RejectsLoginAccountForResidentSource()
    {
        var request = Review(LegacyIdentityMappingStatus.Linked, Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            LegacyIdentityMappingValidator.ValidateSourceSpecificFields("Residents", request));
    }

    [Fact]
    public void ValidateAccountPersonPair_RejectsCrossLinkedTargets()
    {
        Assert.Throws<InvalidOperationException>(() =>
            LegacyIdentityMappingValidator.ValidateAccountPersonPair(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void ValidateAccountPersonPair_AcceptsAccountOwnedBySelectedPerson()
    {
        var personId = Guid.NewGuid();

        LegacyIdentityMappingValidator.ValidateAccountPersonPair(personId, personId);
    }

    [Fact]
    public void ValidateReviewRequest_RejectsPendingAsFinalDisposition()
    {
        var request = Review(LegacyIdentityMappingStatus.Pending, null, null);

        Assert.Throws<ArgumentException>(() => LegacyIdentityMappingValidator.ValidateReviewRequest(request));
    }

    private static ReviewLegacyIdentityMappingRequest Review(
        LegacyIdentityMappingStatus status,
        Guid? personId,
        Guid? accountId,
        string? matchMethod = "manual-review",
        string? evidence = "case:reviewed-document") => new(
            Guid.NewGuid(),
            status,
            personId,
            accountId,
            matchMethod,
            evidence,
            "Reviewer verified source records.");
}
