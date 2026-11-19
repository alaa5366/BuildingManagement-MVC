namespace BuildingManagementMvc.Data.Entities;

/// <summary>Defines the voting and eligibility policy version for a poll.</summary>
public sealed class PollVotingPolicyEntity
{
    /// <summary>Gets or sets the policy identifier.</summary>
    public Guid PolicyId { get; set; }
    /// <summary>Gets or sets the building identifier.</summary>
    public string BuildingId { get; set; } = "";
    /// <summary>Gets or sets the poll identifier.</summary>
    public string PollId { get; set; } = "";
    /// <summary>Gets or sets the policy version.</summary>
    public int PolicyVersion { get; set; }
    /// <summary>Gets or sets the voting mode.</summary>
    public byte VotingMode { get; set; }
    /// <summary>Gets or sets the eligibility mode.</summary>
    public byte EligibilityMode { get; set; }
    /// <summary>Gets or sets whether this policy is active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets the UTC creation time.</summary>
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>Represents a one-time voting entitlement without storing a selected option.</summary>
public sealed class PollEligibilityEntity
{
    /// <summary>Gets or sets the eligibility identifier.</summary>
    public Guid EligibilityId { get; set; }
    /// <summary>Gets or sets the building identifier.</summary>
    public string BuildingId { get; set; } = "";
    /// <summary>Gets or sets the poll identifier.</summary>
    public string PollId { get; set; } = "";
    /// <summary>Gets or sets the eligible subject reference.</summary>
    public string SubjectReference { get; set; } = "";
    /// <summary>Gets or sets the optional credential hash.</summary>
    public byte[]? CredentialHash { get; set; }
    /// <summary>Gets or sets the UTC issue time.</summary>
    public DateTime IssuedAtUtc { get; set; }
    /// <summary>Gets or sets the UTC expiry time.</summary>
    public DateTime ExpiresAtUtc { get; set; }
    /// <summary>Gets or sets the UTC consumption time.</summary>
    public DateTime? ConsumedAtUtc { get; set; }
    /// <summary>Gets or sets the UTC revocation time.</summary>
    public DateTime? RevokedAtUtc { get; set; }
}

/// <summary>Stores an anonymous ballot with an opaque token hash and protected choice payload.</summary>
public sealed class PollBallotEntity
{
    /// <summary>Gets or sets the ballot identifier.</summary>
    public Guid BallotId { get; set; }
    /// <summary>Gets or sets the building identifier.</summary>
    public string BuildingId { get; set; } = "";
    /// <summary>Gets or sets the poll identifier.</summary>
    public string PollId { get; set; } = "";
    /// <summary>Gets or sets the one-way ballot token hash.</summary>
    public byte[] BallotTokenHash { get; set; } = Array.Empty<byte>();
    /// <summary>Gets or sets the protected choice payload.</summary>
    public byte[] ChoicePayload { get; set; } = Array.Empty<byte>();
    /// <summary>Gets or sets the UTC cast time.</summary>
    public DateTime CastAtUtc { get; set; }
    /// <summary>Gets or sets the ballot format version.</summary>
    public int BallotVersion { get; set; }
}

/// <summary>Records an append-only poll lifecycle event.</summary>
public sealed class PollLifecycleEventEntity
{
    /// <summary>Gets or sets the event identifier.</summary>
    public Guid EventId { get; set; }
    /// <summary>Gets or sets the building identifier.</summary>
    public string BuildingId { get; set; } = "";
    /// <summary>Gets or sets the poll identifier.</summary>
    public string PollId { get; set; } = "";
    /// <summary>Gets or sets the event type.</summary>
    public byte EventType { get; set; }
    /// <summary>Gets or sets the UTC occurrence time.</summary>
    public DateTime OccurredAtUtc { get; set; }
    /// <summary>Gets or sets the actor reference.</summary>
    public string? ActorReference { get; set; }
    /// <summary>Gets or sets the reason.</summary>
    public string? Reason { get; set; }
    /// <summary>Gets or sets safe event metadata JSON.</summary>
    public string? MetadataJson { get; set; }
}
