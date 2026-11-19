using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class BuildingSqlMapperAuditTests
{
    [Fact]
    public void MapAggregateAuditEvent_RestoresAllAggregateFieldsFromTargetEvent()
    {
        var occurredAt = new DateTime(2026, 11, 19, 3, 45, 12, DateTimeKind.Utc);
        var source = new AuditEventEntity
        {
            AuditEventId = Guid.NewGuid(),
            OccurredAtUtc = occurredAt,
            ActorReference = "account-reference",
            BuildingId = "building-1",
            EventCategory = "BuildingAggregate",
            EventType = "deposit.confirmed",
            ResourceType = "BuildingAuditEntry",
            ResourceReference = "source-key-1",
            Outcome = 2,
            SafeDetailsJson = """{"label":"Deposit D-001","actorRole":"admin","details":"Confirmed","month":"2026-10","buildingNumber":"B-01","aptNumber":12}"""
        };

        var result = BuildingSqlMapper.MapAggregateAuditEvent(source);

        Assert.Equal("source-key-1", result.Id);
        Assert.Equal("deposit.confirmed", result.Action);
        Assert.Equal("Deposit D-001", result.Label);
        Assert.Equal("account-reference", result.Actor);
        Assert.Equal("admin", result.ActorRole);
        Assert.Equal("Confirmed", result.Details);
        Assert.Equal("2026-10", result.Month);
        Assert.Equal("B-01", result.BuildingNumber);
        Assert.Equal(12, result.AptNumber);
        Assert.Equal("error", result.Severity);
        Assert.Equal("2026-11-19T03:45:12.000Z", result.Ts);
    }

    [Theory]
    [InlineData((byte)0, "info")]
    [InlineData((byte)1, "warning")]
    [InlineData((byte)2, "error")]
    [InlineData((byte)3, "critical")]
    public void MapAggregateAuditEvent_MapsOutcomeToSeverity(byte outcome, string expectedSeverity)
    {
        var source = new AuditEventEntity
        {
            AuditEventId = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow,
            EventType = "building.updated",
            Outcome = outcome
        };

        var result = BuildingSqlMapper.MapAggregateAuditEvent(source);

        Assert.Equal(expectedSeverity, result.Severity);
        Assert.Equal(source.AuditEventId.ToString("N"), result.Id);
    }

    [Fact]
    public void MapAggregateAuditEvent_UsesSafeDefaultsForMissingPayload()
    {
        var source = new AuditEventEntity
        {
            AuditEventId = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow,
            EventType = "building.updated"
        };

        var result = BuildingSqlMapper.MapAggregateAuditEvent(source);

        Assert.Equal(string.Empty, result.Label);
        Assert.Equal(string.Empty, result.ActorRole);
        Assert.Equal(string.Empty, result.Details);
        Assert.Equal(string.Empty, result.Month);
        Assert.Null(result.BuildingNumber);
        Assert.Null(result.AptNumber);
    }

    [Theory]
    [InlineData("pin=1234", "pin=[REDACTED]")]
    [InlineData("password:secret-value", "password:[REDACTED]")]
    [InlineData("token = \"abc-123\"", "token = [REDACTED]")]
    public void RedactAuditDetails_MasksSensitiveKeyValuePairs(string source, string expected)
    {
        Assert.Equal(expected, BuildingSqlMapper.RedactAuditDetails(source));
    }
}
