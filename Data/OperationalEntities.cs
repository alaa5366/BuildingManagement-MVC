namespace BuildingManagementMvc.Data.Entities;

/// <summary>Durable online-presence session. Legacy identifiers are retained only as references.</summary>
public sealed class PresenceSessionEntity
{
    public Guid PresenceSessionId { get; set; }
    public Guid? AccountId { get; set; }
    public string? LegacyUid { get; set; }
    public string? BuildingId { get; set; }
    public string? ApartmentId { get; set; }
    public string? DeviceReference { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    /// <summary>0 = active, 1 = ended/expired, 2 = revoked.</summary>
    public byte Status { get; set; }
    public ICollection<PresenceHeartbeatEntity> Heartbeats { get; set; } = new List<PresenceHeartbeatEntity>();
}

/// <summary>Append-only heartbeat observation associated with a presence session.</summary>
public sealed class PresenceHeartbeatEntity
{
    public long HeartbeatId { get; set; }
    public Guid PresenceSessionId { get; set; }
    public DateTime ObservedAtUtc { get; set; }
    public byte Signal { get; set; }
    public PresenceSessionEntity Session { get; set; } = null!;
}

/// <summary>Append-only business audit event stored independently from diagnostic application logs.</summary>
public sealed class AuditEventEntity
{
    public Guid AuditEventId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid? ActorAccountId { get; set; }
    public string? ActorReference { get; set; }
    public string? BuildingId { get; set; }
    public string EventCategory { get; set; } = "Business";
    public string EventType { get; set; } = "";
    public string? ResourceType { get; set; }
    public string? ResourceReference { get; set; }
    public string? CorrelationId { get; set; }
    /// <summary>0 = success/info, 1 = warning, 2 = error, 3 = critical.</summary>
    public byte Outcome { get; set; }
    public string? SafeDetailsJson { get; set; }
    public byte[]? PreviousHash { get; set; }
    public byte[]? EventHash { get; set; }
}
