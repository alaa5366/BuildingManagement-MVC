using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BuildingManagementMvc.Data;

#nullable disable

namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261119100000_Phase09_OperationalDomainSchema")]
public sealed class Phase09_OperationalDomainSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.PollVotingPolicies', N'U') IS NULL
CREATE TABLE dbo.PollVotingPolicies (
    PolicyId uniqueidentifier NOT NULL CONSTRAINT PK_PollVotingPolicies PRIMARY KEY,
    BuildingId nvarchar(64) NOT NULL,
    PollId nvarchar(64) NOT NULL,
    PolicyVersion int NOT NULL,
    VotingMode tinyint NOT NULL,
    EligibilityMode tinyint NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_PollVotingPolicies_IsActive DEFAULT(1),
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_PollVotingPolicies_Created DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_PollVotingPolicies_Mode CHECK (VotingMode IN (0,1,2) AND EligibilityMode IN (0,1,2)),
    CONSTRAINT UQ_PollVotingPolicies_PollVersion UNIQUE(BuildingId, PollId, PolicyVersion)
);
IF OBJECT_ID(N'dbo.PollDelegations', N'U') IS NULL
CREATE TABLE dbo.PollDelegations (
    DelegationId uniqueidentifier NOT NULL CONSTRAINT PK_PollDelegations PRIMARY KEY,
    BuildingId nvarchar(64) NOT NULL, PollId nvarchar(64) NOT NULL, ApartmentId nvarchar(64) NOT NULL,
    DelegatePersonId uniqueidentifier NOT NULL, GrantedByPersonId uniqueidentifier NULL,
    ValidFromUtc datetime2(3) NOT NULL, ValidToUtc datetime2(3) NULL,
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_PollDelegations_Created DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_PollDelegations_Validity CHECK(ValidToUtc IS NULL OR ValidToUtc > ValidFromUtc)
);
IF OBJECT_ID(N'dbo.PollEligibility', N'U') IS NULL
CREATE TABLE dbo.PollEligibility (
    EligibilityId uniqueidentifier NOT NULL CONSTRAINT PK_PollEligibility PRIMARY KEY,
    BuildingId nvarchar(64) NOT NULL, PollId nvarchar(64) NOT NULL,
    SubjectReference nvarchar(160) NOT NULL, CredentialHash varbinary(64) NULL,
    IssuedAtUtc datetime2(3) NOT NULL, ExpiresAtUtc datetime2(3) NOT NULL,
    ConsumedAtUtc datetime2(3) NULL, RevokedAtUtc datetime2(3) NULL,
    CONSTRAINT CK_PollEligibility_Expiry CHECK(ExpiresAtUtc > IssuedAtUtc)
);
IF OBJECT_ID(N'dbo.PollBallots', N'U') IS NULL
CREATE TABLE dbo.PollBallots (
    BallotId uniqueidentifier NOT NULL CONSTRAINT PK_PollBallots PRIMARY KEY,
    BuildingId nvarchar(64) NOT NULL, PollId nvarchar(64) NOT NULL,
    BallotTokenHash varbinary(64) NOT NULL, ChoicePayload varbinary(max) NOT NULL,
    CastAtUtc datetime2(3) NOT NULL, BallotVersion int NOT NULL,
    CONSTRAINT UQ_PollBallots_Token UNIQUE(BallotTokenHash)
);
IF OBJECT_ID(N'dbo.PollLifecycleEvents', N'U') IS NULL
CREATE TABLE dbo.PollLifecycleEvents (
    EventId uniqueidentifier NOT NULL CONSTRAINT PK_PollLifecycleEvents PRIMARY KEY,
    BuildingId nvarchar(64) NOT NULL, PollId nvarchar(64) NOT NULL,
    EventType tinyint NOT NULL, OccurredAtUtc datetime2(3) NOT NULL,
    ActorReference nvarchar(160) NULL, Reason nvarchar(1000) NULL, MetadataJson nvarchar(max) NULL
);

IF OBJECT_ID(N'dbo.MaintenanceRequests', N'U') IS NULL
CREATE TABLE dbo.MaintenanceRequests (
    RequestId uniqueidentifier NOT NULL CONSTRAINT PK_MaintenanceRequests PRIMARY KEY,
    BuildingId nvarchar(64) NOT NULL, ApartmentId nvarchar(64) NULL,
    LegacyMaintenanceId nvarchar(128) NULL, Title nvarchar(200) NOT NULL,
    Description nvarchar(4000) NULL, Category nvarchar(80) NULL,
    Status tinyint NOT NULL, Priority tinyint NOT NULL,
    RequestedByPersonId uniqueidentifier NULL, RequestedByReference nvarchar(160) NULL,
    CreatedAtUtc datetime2(3) NOT NULL, UpdatedAtUtc datetime2(3) NULL, ClosedAtUtc datetime2(3) NULL,
    CONSTRAINT CK_MaintenanceRequests_Status CHECK(Status BETWEEN 0 AND 8),
    CONSTRAINT CK_MaintenanceRequests_Priority CHECK(Priority BETWEEN 0 AND 4),
    CONSTRAINT UQ_MaintenanceRequests_Legacy UNIQUE(BuildingId, LegacyMaintenanceId)
);
IF OBJECT_ID(N'dbo.MaintenanceAssignments', N'U') IS NULL
CREATE TABLE dbo.MaintenanceAssignments (
    AssignmentId uniqueidentifier NOT NULL CONSTRAINT PK_MaintenanceAssignments PRIMARY KEY,
    RequestId uniqueidentifier NOT NULL, AssigneeReference nvarchar(160) NOT NULL,
    VendorName nvarchar(200) NULL, AssignedAtUtc datetime2(3) NOT NULL,
    CompletedAtUtc datetime2(3) NULL, Notes nvarchar(1000) NULL,
    CONSTRAINT FK_MaintenanceAssignments_Request FOREIGN KEY(RequestId) REFERENCES dbo.MaintenanceRequests(RequestId)
);
IF OBJECT_ID(N'dbo.MaintenanceComments', N'U') IS NULL
CREATE TABLE dbo.MaintenanceComments (
    CommentId uniqueidentifier NOT NULL CONSTRAINT PK_MaintenanceComments PRIMARY KEY,
    RequestId uniqueidentifier NOT NULL, AuthorReference nvarchar(160) NULL,
    Visibility tinyint NOT NULL, Body nvarchar(4000) NOT NULL, CreatedAtUtc datetime2(3) NOT NULL,
    CONSTRAINT FK_MaintenanceComments_Request FOREIGN KEY(RequestId) REFERENCES dbo.MaintenanceRequests(RequestId)
);
IF OBJECT_ID(N'dbo.MaintenanceAttachments', N'U') IS NULL
CREATE TABLE dbo.MaintenanceAttachments (
    AttachmentId uniqueidentifier NOT NULL CONSTRAINT PK_MaintenanceAttachments PRIMARY KEY,
    RequestId uniqueidentifier NOT NULL, StorageKey nvarchar(500) NOT NULL,
    FileName nvarchar(260) NOT NULL, ContentType nvarchar(120) NULL,
    SizeBytes bigint NOT NULL, Sha256 varbinary(32) NULL,
    UploadedAtUtc datetime2(3) NOT NULL, UploadedByReference nvarchar(160) NULL,
    CONSTRAINT FK_MaintenanceAttachments_Request FOREIGN KEY(RequestId) REFERENCES dbo.MaintenanceRequests(RequestId),
    CONSTRAINT CK_MaintenanceAttachments_Size CHECK(SizeBytes >= 0)
);
IF OBJECT_ID(N'dbo.MaintenanceStatusEvents', N'U') IS NULL
CREATE TABLE dbo.MaintenanceStatusEvents (
    StatusEventId uniqueidentifier NOT NULL CONSTRAINT PK_MaintenanceStatusEvents PRIMARY KEY,
    RequestId uniqueidentifier NOT NULL, FromStatus tinyint NULL, ToStatus tinyint NOT NULL,
    ActorReference nvarchar(160) NULL, Reason nvarchar(1000) NULL, OccurredAtUtc datetime2(3) NOT NULL,
    CONSTRAINT FK_MaintenanceStatusEvents_Request FOREIGN KEY(RequestId) REFERENCES dbo.MaintenanceRequests(RequestId)
);
IF OBJECT_ID(N'dbo.MaintenanceLedgerLinks', N'U') IS NULL
CREATE TABLE dbo.MaintenanceLedgerLinks (
    LinkId uniqueidentifier NOT NULL CONSTRAINT PK_MaintenanceLedgerLinks PRIMARY KEY,
    RequestId uniqueidentifier NOT NULL, LedgerTransactionId uniqueidentifier NOT NULL,
    LinkType tinyint NOT NULL, CreatedAtUtc datetime2(3) NOT NULL,
    CONSTRAINT FK_MaintenanceLedgerLinks_Request FOREIGN KEY(RequestId) REFERENCES dbo.MaintenanceRequests(RequestId)
);

IF OBJECT_ID(N'dbo.NotificationMessages', N'U') IS NULL
CREATE TABLE dbo.NotificationMessages (
    MessageId uniqueidentifier NOT NULL CONSTRAINT PK_NotificationMessages PRIMARY KEY,
    BuildingId nvarchar(64) NULL, LegacyNotificationId nvarchar(128) NULL,
    Title nvarchar(250) NULL, Body nvarchar(4000) NOT NULL,
    Channel tinyint NOT NULL, Origin nvarchar(80) NOT NULL,
    CreatedAtUtc datetime2(3) NOT NULL, ExpiresAtUtc datetime2(3) NULL,
    CONSTRAINT UQ_NotificationMessages_Legacy UNIQUE(LegacyNotificationId)
);
IF OBJECT_ID(N'dbo.NotificationRecipients', N'U') IS NULL
CREATE TABLE dbo.NotificationRecipients (
    RecipientId uniqueidentifier NOT NULL CONSTRAINT PK_NotificationRecipients PRIMARY KEY,
    MessageId uniqueidentifier NOT NULL, RecipientType tinyint NOT NULL,
    RecipientReference nvarchar(160) NOT NULL, ApartmentId nvarchar(64) NULL,
    CreatedAtUtc datetime2(3) NOT NULL,
    CONSTRAINT FK_NotificationRecipients_Message FOREIGN KEY(MessageId) REFERENCES dbo.NotificationMessages(MessageId)
);
IF OBJECT_ID(N'dbo.NotificationDeliveries', N'U') IS NULL
CREATE TABLE dbo.NotificationDeliveries (
    DeliveryId uniqueidentifier NOT NULL CONSTRAINT PK_NotificationDeliveries PRIMARY KEY,
    RecipientId uniqueidentifier NOT NULL, AttemptNumber int NOT NULL,
    Provider nvarchar(80) NOT NULL, Status tinyint NOT NULL,
    AttemptedAtUtc datetime2(3) NOT NULL, CompletedAtUtc datetime2(3) NULL,
    ProviderReference nvarchar(200) NULL, SafeErrorCode nvarchar(100) NULL,
    CONSTRAINT FK_NotificationDeliveries_Recipient FOREIGN KEY(RecipientId) REFERENCES dbo.NotificationRecipients(RecipientId),
    CONSTRAINT UQ_NotificationDeliveries_Attempt UNIQUE(RecipientId, AttemptNumber)
);
IF OBJECT_ID(N'dbo.NotificationReadStates', N'U') IS NULL
CREATE TABLE dbo.NotificationReadStates (
    ReadStateId uniqueidentifier NOT NULL CONSTRAINT PK_NotificationReadStates PRIMARY KEY,
    RecipientId uniqueidentifier NOT NULL, ReadAtUtc datetime2(3) NULL,
    DismissedAtUtc datetime2(3) NULL,
    CONSTRAINT FK_NotificationReadStates_Recipient FOREIGN KEY(RecipientId) REFERENCES dbo.NotificationRecipients(RecipientId),
    CONSTRAINT UQ_NotificationReadStates_Recipient UNIQUE(RecipientId)
);
IF OBJECT_ID(N'dbo.NotificationPreferences', N'U') IS NULL
CREATE TABLE dbo.NotificationPreferences (
    PreferenceId uniqueidentifier NOT NULL CONSTRAINT PK_NotificationPreferences PRIMARY KEY,
    SubjectType tinyint NOT NULL, SubjectReference nvarchar(160) NOT NULL,
    Channel tinyint NOT NULL, IsEnabled bit NOT NULL,
    UpdatedAtUtc datetime2(3) NOT NULL,
    CONSTRAINT UQ_NotificationPreferences_Subject UNIQUE(SubjectType, SubjectReference, Channel)
);

IF OBJECT_ID(N'dbo.PresenceSessions', N'U') IS NULL
CREATE TABLE dbo.PresenceSessions (
    PresenceSessionId uniqueidentifier NOT NULL CONSTRAINT PK_PresenceSessions PRIMARY KEY,
    AccountId uniqueidentifier NULL, LegacyUid nvarchar(128) NULL,
    BuildingId nvarchar(64) NULL, ApartmentId nvarchar(64) NULL,
    DeviceReference nvarchar(200) NULL, StartedAtUtc datetime2(3) NOT NULL,
    LastSeenAtUtc datetime2(3) NOT NULL, ExpiresAtUtc datetime2(3) NOT NULL,
    EndedAtUtc datetime2(3) NULL, Status tinyint NOT NULL
);
IF OBJECT_ID(N'dbo.PresenceHeartbeats', N'U') IS NULL
CREATE TABLE dbo.PresenceHeartbeats (
    HeartbeatId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PresenceHeartbeats PRIMARY KEY,
    PresenceSessionId uniqueidentifier NOT NULL, ObservedAtUtc datetime2(3) NOT NULL,
    Signal tinyint NOT NULL,
    CONSTRAINT FK_PresenceHeartbeats_Session FOREIGN KEY(PresenceSessionId) REFERENCES dbo.PresenceSessions(PresenceSessionId)
);

IF OBJECT_ID(N'dbo.QrCapabilities', N'U') IS NULL
CREATE TABLE dbo.QrCapabilities (
    CapabilityId uniqueidentifier NOT NULL CONSTRAINT PK_QrCapabilities PRIMARY KEY,
    BuildingId nvarchar(64) NOT NULL, ApartmentId nvarchar(64) NULL,
    TargetType tinyint NOT NULL, TargetReference nvarchar(160) NOT NULL,
    TokenHash varbinary(64) NOT NULL, IssuedAtUtc datetime2(3) NOT NULL,
    ExpiresAtUtc datetime2(3) NOT NULL, MaxUses int NOT NULL,
    RevokedAtUtc datetime2(3) NULL, RevokedByReference nvarchar(160) NULL,
    CONSTRAINT UQ_QrCapabilities_Token UNIQUE(TokenHash),
    CONSTRAINT CK_QrCapabilities_Validity CHECK(ExpiresAtUtc > IssuedAtUtc AND MaxUses > 0)
);
IF OBJECT_ID(N'dbo.QrCapabilityUses', N'U') IS NULL
CREATE TABLE dbo.QrCapabilityUses (
    UseId uniqueidentifier NOT NULL CONSTRAINT PK_QrCapabilityUses PRIMARY KEY,
    CapabilityId uniqueidentifier NOT NULL, UsedAtUtc datetime2(3) NOT NULL,
    UseReference nvarchar(160) NULL, Outcome tinyint NOT NULL,
    CONSTRAINT FK_QrCapabilityUses_Capability FOREIGN KEY(CapabilityId) REFERENCES dbo.QrCapabilities(CapabilityId)
);
IF OBJECT_ID(N'dbo.GuestAccessSessions', N'U') IS NULL
CREATE TABLE dbo.GuestAccessSessions (
    GuestSessionId uniqueidentifier NOT NULL CONSTRAINT PK_GuestAccessSessions PRIMARY KEY,
    CapabilityId uniqueidentifier NOT NULL, GuestReference nvarchar(160) NULL,
    StartedAtUtc datetime2(3) NOT NULL, ExpiresAtUtc datetime2(3) NOT NULL,
    RevokedAtUtc datetime2(3) NULL,
    CONSTRAINT FK_GuestAccessSessions_Capability FOREIGN KEY(CapabilityId) REFERENCES dbo.QrCapabilities(CapabilityId)
);

IF OBJECT_ID(N'dbo.MediaCredentialReferences', N'U') IS NULL
CREATE TABLE dbo.MediaCredentialReferences (
    CredentialReferenceId uniqueidentifier NOT NULL CONSTRAINT PK_MediaCredentialReferences PRIMARY KEY,
    BuildingId nvarchar(64) NOT NULL, DvrLegacyId nvarchar(128) NULL,
    VaultProvider nvarchar(80) NOT NULL, VaultSecretReference nvarchar(500) NOT NULL,
    KeyVersion nvarchar(100) NOT NULL, CreatedAtUtc datetime2(3) NOT NULL,
    RotatedAtUtc datetime2(3) NULL, RevokedAtUtc datetime2(3) NULL,
    CONSTRAINT UQ_MediaCredentialReferences_Legacy UNIQUE(DvrLegacyId)
);
IF OBJECT_ID(N'dbo.MediaProvisioningOperations', N'U') IS NULL
CREATE TABLE dbo.MediaProvisioningOperations (
    OperationId uniqueidentifier NOT NULL CONSTRAINT PK_MediaProvisioningOperations PRIMARY KEY,
    CredentialReferenceId uniqueidentifier NULL, DvrLegacyId nvarchar(128) NULL,
    OperationType tinyint NOT NULL, Status tinyint NOT NULL,
    RequestedAtUtc datetime2(3) NOT NULL, CompletedAtUtc datetime2(3) NULL,
    Attempts int NOT NULL, SafeErrorCode nvarchar(100) NULL,
    CONSTRAINT FK_MediaProvisioningOperations_Credential FOREIGN KEY(CredentialReferenceId) REFERENCES dbo.MediaCredentialReferences(CredentialReferenceId)
);
IF OBJECT_ID(N'dbo.MediaAccessSessions', N'U') IS NULL
CREATE TABLE dbo.MediaAccessSessions (
    MediaAccessSessionId uniqueidentifier NOT NULL CONSTRAINT PK_MediaAccessSessions PRIMARY KEY,
    AccountId uniqueidentifier NULL, CameraLegacyId nvarchar(128) NULL,
    SessionReference nvarchar(200) NOT NULL, IssuedAtUtc datetime2(3) NOT NULL,
    ExpiresAtUtc datetime2(3) NOT NULL, RevokedAtUtc datetime2(3) NULL
);

IF OBJECT_ID(N'dbo.ProtectedIntegrationSecrets', N'U') IS NULL
CREATE TABLE dbo.ProtectedIntegrationSecrets (
    SecretId uniqueidentifier NOT NULL CONSTRAINT PK_ProtectedIntegrationSecrets PRIMARY KEY,
    ScopeType tinyint NOT NULL, ScopeReference nvarchar(160) NOT NULL,
    Provider nvarchar(100) NOT NULL, SecretName nvarchar(160) NOT NULL,
    Ciphertext varbinary(max) NOT NULL, KeyVersion nvarchar(100) NOT NULL,
    CreatedAtUtc datetime2(3) NOT NULL, RotatedAtUtc datetime2(3) NULL,
    RevokedAtUtc datetime2(3) NULL,
    CONSTRAINT UQ_ProtectedIntegrationSecrets_Scope UNIQUE(ScopeType, ScopeReference, Provider, SecretName)
);
IF OBJECT_ID(N'dbo.UserPreferences', N'U') IS NULL
CREATE TABLE dbo.UserPreferences (
    PreferenceId uniqueidentifier NOT NULL CONSTRAINT PK_UserPreferences PRIMARY KEY,
    AccountId uniqueidentifier NOT NULL, PreferenceKey nvarchar(120) NOT NULL,
    PreferenceJson nvarchar(2000) NOT NULL, UpdatedAtUtc datetime2(3) NOT NULL,
    CONSTRAINT UQ_UserPreferences_Account_Key UNIQUE(AccountId, PreferenceKey)
);
IF OBJECT_ID(N'dbo.DocumentAssets', N'U') IS NULL
CREATE TABLE dbo.DocumentAssets (
    DocumentId uniqueidentifier NOT NULL CONSTRAINT PK_DocumentAssets PRIMARY KEY,
    BuildingId nvarchar(64) NULL, OwnerReference nvarchar(160) NULL,
    StorageKey nvarchar(500) NOT NULL, FileName nvarchar(260) NOT NULL,
    ContentType nvarchar(120) NULL, SizeBytes bigint NOT NULL,
    Sha256 varbinary(32) NULL, Classification tinyint NOT NULL,
    CreatedAtUtc datetime2(3) NOT NULL, RetainUntilUtc datetime2(3) NULL,
    CONSTRAINT CK_DocumentAssets_Size CHECK(SizeBytes >= 0)
);
IF OBJECT_ID(N'dbo.DocumentAccessGrants', N'U') IS NULL
CREATE TABLE dbo.DocumentAccessGrants (
    GrantId uniqueidentifier NOT NULL CONSTRAINT PK_DocumentAccessGrants PRIMARY KEY,
    DocumentId uniqueidentifier NOT NULL, SubjectReference nvarchar(160) NOT NULL,
    GrantedAtUtc datetime2(3) NOT NULL, ExpiresAtUtc datetime2(3) NULL,
    RevokedAtUtc datetime2(3) NULL,
    CONSTRAINT FK_DocumentAccessGrants_Document FOREIGN KEY(DocumentId) REFERENCES dbo.DocumentAssets(DocumentId)
);
IF OBJECT_ID(N'dbo.FileProcessingJobs', N'U') IS NULL
CREATE TABLE dbo.FileProcessingJobs (
    JobId uniqueidentifier NOT NULL CONSTRAINT PK_FileProcessingJobs PRIMARY KEY,
    DocumentId uniqueidentifier NOT NULL, JobType tinyint NOT NULL, Status tinyint NOT NULL,
    CreatedAtUtc datetime2(3) NOT NULL, CompletedAtUtc datetime2(3) NULL,
    Attempts int NOT NULL, SafeErrorCode nvarchar(100) NULL,
    CONSTRAINT FK_FileProcessingJobs_Document FOREIGN KEY(DocumentId) REFERENCES dbo.DocumentAssets(DocumentId)
);

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NULL
CREATE TABLE dbo.AuditEvents (
    AuditEventId uniqueidentifier NOT NULL CONSTRAINT PK_AuditEvents PRIMARY KEY,
    OccurredAtUtc datetime2(3) NOT NULL, ActorAccountId uniqueidentifier NULL,
    ActorReference nvarchar(160) NULL, BuildingId nvarchar(64) NULL,
    EventCategory nvarchar(80) NOT NULL, EventType nvarchar(120) NOT NULL,
    ResourceType nvarchar(100) NULL, ResourceReference nvarchar(200) NULL,
    CorrelationId nvarchar(100) NULL, Outcome tinyint NOT NULL,
    SafeDetailsJson nvarchar(max) NULL, PreviousHash varbinary(32) NULL,
    EventHash varbinary(32) NULL
);
CREATE INDEX IX_AuditEvents_OccurredAt ON dbo.AuditEvents(OccurredAtUtc DESC, AuditEventId);
CREATE INDEX IX_AuditEvents_Resource ON dbo.AuditEvents(ResourceType, ResourceReference, OccurredAtUtc DESC);
IF OBJECT_ID(N'dbo.OutboxMessages', N'U') IS NULL
CREATE TABLE dbo.OutboxMessages (
    OutboxMessageId uniqueidentifier NOT NULL CONSTRAINT PK_OutboxMessages PRIMARY KEY,
    MessageType nvarchar(160) NOT NULL, AggregateType nvarchar(120) NOT NULL,
    AggregateReference nvarchar(200) NOT NULL, PayloadJson nvarchar(max) NOT NULL,
    IdempotencyKey nvarchar(200) NOT NULL, OccurredAtUtc datetime2(3) NOT NULL,
    AvailableAtUtc datetime2(3) NOT NULL, ProcessedAtUtc datetime2(3) NULL,
    Attempts int NOT NULL, LastSafeErrorCode nvarchar(100) NULL,
    CONSTRAINT UQ_OutboxMessages_Idempotency UNIQUE(IdempotencyKey)
);
CREATE INDEX IX_OutboxMessages_Pending ON dbo.OutboxMessages(AvailableAtUtc, OccurredAtUtc) WHERE ProcessedAtUtc IS NULL;
IF OBJECT_ID(N'dbo.InboxMessages', N'U') IS NULL
CREATE TABLE dbo.InboxMessages (
    InboxMessageId uniqueidentifier NOT NULL CONSTRAINT PK_InboxMessages PRIMARY KEY,
    Consumer nvarchar(160) NOT NULL, MessageId nvarchar(200) NOT NULL,
    ReceivedAtUtc datetime2(3) NOT NULL, ProcessedAtUtc datetime2(3) NULL,
    Outcome tinyint NOT NULL, SafeErrorCode nvarchar(100) NULL,
    CONSTRAINT UQ_InboxMessages_Consumer_Message UNIQUE(Consumer, MessageId)
);

IF OBJECT_ID(N'dbo.BackupJobs', N'U') IS NULL
CREATE TABLE dbo.BackupJobs (
    BackupJobId uniqueidentifier NOT NULL CONSTRAINT PK_BackupJobs PRIMARY KEY,
    JobType tinyint NOT NULL, Status tinyint NOT NULL,
    StartedAtUtc datetime2(3) NOT NULL, CompletedAtUtc datetime2(3) NULL,
    RequestedByReference nvarchar(160) NULL, DatabaseReference nvarchar(200) NOT NULL,
    BackupArtifactId uniqueidentifier NULL, SafeErrorCode nvarchar(100) NULL
);
IF OBJECT_ID(N'dbo.BackupArtifacts', N'U') IS NULL
CREATE TABLE dbo.BackupArtifacts (
    BackupArtifactId uniqueidentifier NOT NULL CONSTRAINT PK_BackupArtifacts PRIMARY KEY,
    StorageReference nvarchar(500) NOT NULL, Sha256 varbinary(32) NOT NULL,
    SizeBytes bigint NOT NULL, CreatedAtUtc datetime2(3) NOT NULL,
    RetainUntilUtc datetime2(3) NULL, EncryptionKeyVersion nvarchar(100) NULL,
    CONSTRAINT CK_BackupArtifacts_Size CHECK(SizeBytes >= 0)
);
IF OBJECT_ID(N'dbo.BackupRetentionEvents', N'U') IS NULL
CREATE TABLE dbo.BackupRetentionEvents (
    RetentionEventId uniqueidentifier NOT NULL CONSTRAINT PK_BackupRetentionEvents PRIMARY KEY,
    BackupArtifactId uniqueidentifier NOT NULL, EventType tinyint NOT NULL,
    OccurredAtUtc datetime2(3) NOT NULL, ActorReference nvarchar(160) NULL,
    Reason nvarchar(1000) NULL,
    CONSTRAINT FK_BackupRetentionEvents_Artifact FOREIGN KEY(BackupArtifactId) REFERENCES dbo.BackupArtifacts(BackupArtifactId)
);
IF OBJECT_ID(N'dbo.RestoreOperations', N'U') IS NULL
CREATE TABLE dbo.RestoreOperations (
    RestoreOperationId uniqueidentifier NOT NULL CONSTRAINT PK_RestoreOperations PRIMARY KEY,
    BackupArtifactId uniqueidentifier NOT NULL, TargetDatabase nvarchar(200) NOT NULL,
    Status tinyint NOT NULL, RequestedAtUtc datetime2(3) NOT NULL,
    ApprovedAtUtc datetime2(3) NULL, ApprovedByReference nvarchar(160) NULL,
    StartedAtUtc datetime2(3) NULL, CompletedAtUtc datetime2(3) NULL,
    SafeErrorCode nvarchar(100) NULL,
    CONSTRAINT FK_RestoreOperations_Artifact FOREIGN KEY(BackupArtifactId) REFERENCES dbo.BackupArtifacts(BackupArtifactId)
);

IF OBJECT_ID(N'dbo.ImportBatches', N'U') IS NULL
CREATE TABLE dbo.ImportBatches (
    ImportBatchId uniqueidentifier NOT NULL CONSTRAINT PK_ImportBatches PRIMARY KEY,
    ImportType nvarchar(100) NOT NULL, SourceFingerprint varbinary(32) NOT NULL,
    RequestedByReference nvarchar(160) NULL, Status tinyint NOT NULL,
    CreatedAtUtc datetime2(3) NOT NULL, CompletedAtUtc datetime2(3) NULL
);
IF OBJECT_ID(N'dbo.ImportRows', N'U') IS NULL
CREATE TABLE dbo.ImportRows (
    ImportRowId uniqueidentifier NOT NULL CONSTRAINT PK_ImportRows PRIMARY KEY,
    ImportBatchId uniqueidentifier NOT NULL, SourceRowKey nvarchar(200) NOT NULL,
    RowFingerprint varbinary(32) NOT NULL, Status tinyint NOT NULL,
    SafeErrorCode nvarchar(100) NULL, TargetReference nvarchar(200) NULL,
    CONSTRAINT FK_ImportRows_Batch FOREIGN KEY(ImportBatchId) REFERENCES dbo.ImportBatches(ImportBatchId),
    CONSTRAINT UQ_ImportRows_Source UNIQUE(ImportBatchId, SourceRowKey)
);
IF OBJECT_ID(N'dbo.ExportJobs', N'U') IS NULL
CREATE TABLE dbo.ExportJobs (
    ExportJobId uniqueidentifier NOT NULL CONSTRAINT PK_ExportJobs PRIMARY KEY,
    RequestedByReference nvarchar(160) NULL, ScopeJson nvarchar(2000) NOT NULL,
    Format nvarchar(20) NOT NULL, Status tinyint NOT NULL,
    CreatedAtUtc datetime2(3) NOT NULL, CompletedAtUtc datetime2(3) NULL,
    ExpiresAtUtc datetime2(3) NULL, ArtifactStorageKey nvarchar(500) NULL
);

IF OBJECT_ID(N'dbo.LegacyDataMigrationMap', N'U') IS NULL
CREATE TABLE dbo.LegacyDataMigrationMap (
    MigrationMapId uniqueidentifier NOT NULL CONSTRAINT PK_LegacyDataMigrationMap PRIMARY KEY,
    SourceSystem nvarchar(80) NOT NULL, SourceTable nvarchar(128) NOT NULL,
    SourceKey nvarchar(256) NOT NULL, TargetTable nvarchar(128) NOT NULL,
    TargetKey nvarchar(256) NULL, SourceFingerprint varbinary(32) NULL,
    Outcome tinyint NOT NULL, Disposition nvarchar(80) NOT NULL,
    MigratedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_LegacyDataMigrationMap_Date DEFAULT SYSUTCDATETIME(),
    SafeNote nvarchar(500) NULL,
    CONSTRAINT UQ_LegacyDataMigrationMap_Source UNIQUE(SourceSystem, SourceTable, SourceKey)
);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally non-destructive: target operational history is never dropped by rollback.
        // Restore the pre-migration backup to reverse this schema addition if required.
    }
}
