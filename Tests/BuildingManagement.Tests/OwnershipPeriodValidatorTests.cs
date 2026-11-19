using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class OwnershipPeriodValidatorTests
{
    private static readonly DateTime NowUtc = new(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ValidateRequest_AllowsIncompleteOwnershipBelowOneHundredPercent()
    {
        var request = Request(OwnershipCompletenessStatus.Incomplete, 65m);

        OwnershipPeriodValidator.ValidateRequest(request, NowUtc);
    }

    [Fact]
    public void ValidateRequest_RequiresVerifiedCompleteSharesToEqualOneHundredPercent()
    {
        var request = Request(
            OwnershipCompletenessStatus.CompleteVerified,
            99m,
            verifiedAt: NowUtc.AddMinutes(-1),
            reviewerId: Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => OwnershipPeriodValidator.ValidateRequest(request, NowUtc));
    }

    [Fact]
    public void ValidateRequest_RejectsPartialVerificationMetadata()
    {
        var request = Request(
            OwnershipCompletenessStatus.Incomplete,
            65m,
            verifiedAt: NowUtc.AddMinutes(-1));

        Assert.Throws<ArgumentException>(() => OwnershipPeriodValidator.ValidateRequest(request, NowUtc));
    }

    [Fact]
    public void ValidateAgainstExisting_RejectsOverlappingSharesAboveOneHundredPercent()
    {
        var request = Request(OwnershipCompletenessStatus.Incomplete, 40m);
        var existing = new[]
        {
            Existing(Guid.NewGuid(), 70m, NowUtc.AddDays(-2), NowUtc.AddDays(2))
        };

        Assert.Throws<InvalidOperationException>(() =>
            OwnershipPeriodValidator.ValidateAgainstExisting(request, existing, NowUtc));
    }

    [Fact]
    public void ValidateAgainstExisting_RejectsDuplicateOwnerAcrossOverlappingRows()
    {
        var personId = Guid.NewGuid();
        var request = Request(OwnershipCompletenessStatus.Incomplete, 40m, personId: personId);
        var existing = new[]
        {
            Existing(personId, 20m, NowUtc.AddDays(-1), NowUtc.AddDays(1))
        };

        Assert.Throws<InvalidOperationException>(() =>
            OwnershipPeriodValidator.ValidateAgainstExisting(request, existing, NowUtc));
    }

    [Fact]
    public void ValidateAgainstExisting_AllowsAdjacentPeriods()
    {
        var request = Request(OwnershipCompletenessStatus.Incomplete, 40m);
        var existing = new[]
        {
            Existing(Guid.NewGuid(), 60m, NowUtc.AddDays(-1), NowUtc)
        };

        OwnershipPeriodValidator.ValidateAgainstExisting(request, existing, NowUtc);
    }

    private static RecordOwnershipPeriodRequest Request(
        OwnershipCompletenessStatus status,
        decimal share,
        DateTime? verifiedAt = null,
        Guid? reviewerId = null,
        Guid? personId = null) => new(
            "building-1",
            "apartment-1",
            NowUtc,
            NowUtc.AddDays(1),
            status,
            verifiedAt,
            reviewerId,
            OwnershipRecordSource.Import,
            null,
            new[] { new OwnershipShareInput(personId ?? Guid.NewGuid(), share, "deed:reference") });

    private static ApartmentOwnershipEntity Existing(Guid personId, decimal share, DateTime from, DateTime? to) => new()
    {
        OwnershipId = Guid.NewGuid(),
        BuildingId = "building-1",
        ApartmentId = "apartment-1",
        PersonId = personId,
        OwnershipSharePercent = share,
        CompletenessStatus = OwnershipCompletenessStatus.Incomplete,
        RecordSource = OwnershipRecordSource.Import,
        ValidFromUtc = from,
        ValidToUtc = to,
        EvidenceReference = "deed:existing"
    };
}
