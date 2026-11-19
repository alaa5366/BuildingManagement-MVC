namespace BuildingManagementMvc.Data.Entities;

/// <summary>Represents a normalized notification message shared with one or more recipients.</summary>
public sealed class NotificationMessageEntity
{
    public Guid MessageId { get; set; }
    public string? BuildingId { get; set; }
    public string? LegacyNotificationId { get; set; }
    public string? Title { get; set; }
    public string Body { get; set; } = "";
    public byte Channel { get; set; }
    public string Origin { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public ICollection<NotificationRecipientEntity> Recipients { get; set; } = new List<NotificationRecipientEntity>();
}

/// <summary>Represents a message delivery target within a building or apartment.</summary>
public sealed class NotificationRecipientEntity
{
    public Guid RecipientId { get; set; }
    public Guid MessageId { get; set; }
    public byte RecipientType { get; set; }
    public string RecipientReference { get; set; } = "";
    public string? ApartmentId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public NotificationMessageEntity? Message { get; set; }
    public NotificationReadStateEntity? ReadState { get; set; }
}

/// <summary>Records an individual provider delivery attempt for a notification recipient.</summary>
public sealed class NotificationDeliveryEntity
{
    public Guid DeliveryId { get; set; }
    public Guid RecipientId { get; set; }
    public int AttemptNumber { get; set; }
    public string Provider { get; set; } = "";
    public byte Status { get; set; }
    public DateTime AttemptedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ProviderReference { get; set; }
    public string? SafeErrorCode { get; set; }
}

/// <summary>Stores read and dismissal timestamps for a notification recipient.</summary>
public sealed class NotificationReadStateEntity
{
    public Guid ReadStateId { get; set; }
    public Guid RecipientId { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public DateTime? DismissedAtUtc { get; set; }
    public NotificationRecipientEntity? Recipient { get; set; }
}

/// <summary>Stores a subject's channel-specific notification preference.</summary>
public sealed class NotificationPreferenceEntity
{
    public Guid PreferenceId { get; set; }
    public byte SubjectType { get; set; }
    public string SubjectReference { get; set; } = "";
    public byte Channel { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
