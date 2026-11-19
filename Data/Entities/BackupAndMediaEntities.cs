namespace BuildingManagementMvc.Data.Entities;

    /// <summary> Represents the BackupJobEntity data or service contract. </summary>
public sealed class BackupJobEntity
{
    public Guid BackupJobId { get; set; }
    public byte JobType { get; set; }
    public byte Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? RequestedByReference { get; set; }
    public string DatabaseReference { get; set; } = "";
    public Guid? BackupArtifactId { get; set; }
    public string? SafeErrorCode { get; set; }
    public string? MetadataJson { get; set; }
}

    /// <summary> Represents the BackupArtifactEntity data or service contract. </summary>
public sealed class BackupArtifactEntity
{
    public Guid BackupArtifactId { get; set; }
    public string StorageReference { get; set; } = "";
    public byte[] Sha256 { get; set; } = Array.Empty<byte>();
    public long SizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RetainUntilUtc { get; set; }
    public string? EncryptionKeyVersion { get; set; }
}

    /// <summary> Represents the BackupRetentionEventEntity data or service contract. </summary>
public sealed class BackupRetentionEventEntity
{
    public Guid RetentionEventId { get; set; }
    public Guid BackupArtifactId { get; set; }
    public byte EventType { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? ActorReference { get; set; }
    public string? Reason { get; set; }
}

    /// <summary> Represents the RestoreOperationEntity data or service contract. </summary>
public sealed class RestoreOperationEntity
{
    public Guid RestoreOperationId { get; set; }
    public Guid BackupArtifactId { get; set; }
    public string TargetDatabase { get; set; } = "";
    public byte Status { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovedByReference { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? SafeErrorCode { get; set; }
}

    /// <summary> Represents the BackupScheduleConfigurationEntity data or service contract. </summary>
public sealed class BackupScheduleConfigurationEntity
{
    public Guid ConfigurationId { get; set; }
    public string LegacySettingId { get; set; } = "";
    public bool IsEnabled { get; set; }
    public string Frequency { get; set; } = "";
    public int ScheduledHour { get; set; }
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public string CollectionsJson { get; set; } = "[]";
    public int MaxBackupsToKeep { get; set; }
    public DateTime? LastRunAtUtc { get; set; }
    public string? LastRunStatus { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedByReference { get; set; }
    public DateTime ImportedAtUtc { get; set; }
}

    /// <summary> Represents the MediaCredentialReferenceEntity data or service contract. </summary>
public sealed class MediaCredentialReferenceEntity
{
    public Guid CredentialReferenceId { get; set; }
    public string BuildingId { get; set; } = "";
    public string? DvrLegacyId { get; set; }
    public string VaultProvider { get; set; } = "";
    public string VaultSecretReference { get; set; } = "";
    public string KeyVersion { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RotatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}

    /// <summary> Represents the MediaProvisioningOperationEntity data or service contract. </summary>
public sealed class MediaProvisioningOperationEntity
{
    public Guid OperationId { get; set; }
    public Guid? CredentialReferenceId { get; set; }
    public string? DvrLegacyId { get; set; }
    public byte OperationType { get; set; }
    public byte Status { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int Attempts { get; set; }
    public string? SafeErrorCode { get; set; }
}

    /// <summary> Represents the MediaAccessSessionEntity data or service contract. </summary>
public sealed class MediaAccessSessionEntity
{
    public Guid MediaAccessSessionId { get; set; }
    public Guid? AccountId { get; set; }
    public string? CameraLegacyId { get; set; }
    public string SessionReference { get; set; } = "";
    public DateTime IssuedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
