namespace BuildingManagementMvc.Data.Entities;

/// <summary>Represents a maintenance request in the normalized operational schema.</summary>
public sealed class MaintenanceRequestEntity
{
    public Guid RequestId { get; set; }
    public string BuildingId { get; set; } = "";
    public string? ApartmentId { get; set; }
    public string? LegacyMaintenanceId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public byte Status { get; set; }
    public byte Priority { get; set; } = 2;
    public Guid? RequestedByPersonId { get; set; }
    public string? RequestedByReference { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public ICollection<MaintenanceAssignmentEntity> Assignments { get; set; } = new List<MaintenanceAssignmentEntity>();
    public ICollection<MaintenanceCommentEntity> Comments { get; set; } = new List<MaintenanceCommentEntity>();
    public ICollection<MaintenanceAttachmentEntity> Attachments { get; set; } = new List<MaintenanceAttachmentEntity>();
    public ICollection<MaintenanceStatusEventEntity> StatusEvents { get; set; } = new List<MaintenanceStatusEventEntity>();
    public ICollection<MaintenanceLedgerLinkEntity> LedgerLinks { get; set; } = new List<MaintenanceLedgerLinkEntity>();
}

/// <summary>Records an assignment and vendor history for a maintenance request.</summary>
public sealed class MaintenanceAssignmentEntity
{
    public Guid AssignmentId { get; set; }
    public Guid RequestId { get; set; }
    public string AssigneeReference { get; set; } = "";
    public string? VendorName { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Notes { get; set; }
    public MaintenanceRequestEntity? Request { get; set; }
}

/// <summary>Stores an internal or resident-visible comment on a maintenance request.</summary>
public sealed class MaintenanceCommentEntity
{
    public Guid CommentId { get; set; }
    public Guid RequestId { get; set; }
    public string? AuthorReference { get; set; }
    public byte Visibility { get; set; }
    public string Body { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public MaintenanceRequestEntity? Request { get; set; }
}

/// <summary>Stores metadata for a file attached to a maintenance request.</summary>
public sealed class MaintenanceAttachmentEntity
{
    public Guid AttachmentId { get; set; }
    public Guid RequestId { get; set; }
    public string StorageKey { get; set; } = "";
    public string FileName { get; set; } = "";
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public byte[]? Sha256 { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public string? UploadedByReference { get; set; }
    public MaintenanceRequestEntity? Request { get; set; }
}

/// <summary>Preserves the status transition history of a maintenance request.</summary>
public sealed class MaintenanceStatusEventEntity
{
    public Guid StatusEventId { get; set; }
    public Guid RequestId { get; set; }
    public byte? FromStatus { get; set; }
    public byte ToStatus { get; set; }
    public string? ActorReference { get; set; }
    public string? Reason { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public MaintenanceRequestEntity? Request { get; set; }
}

/// <summary>Links a maintenance request to its immutable ledger transaction.</summary>
public sealed class MaintenanceLedgerLinkEntity
{
    public Guid LinkId { get; set; }
    public Guid RequestId { get; set; }
    public Guid LedgerTransactionId { get; set; }
    public byte LinkType { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public MaintenanceRequestEntity? Request { get; set; }
}
