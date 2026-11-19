# Phase 45 â€” Full database table and relationship inventory

- Run: 2026-11-19T04:35:00.2846444+02:00
- Database: BuildingManagement_RestoreTest
- User tables: 97
- Non-empty tables: 56
- Total rows across user tables: 5119

## Table inventory

| Schema | Table | Rows | Primary key | FK references | Columns |
|---|---|---:|---|---:|---:|
| dbo | __EFMigrationsHistory | 19 | [MigrationId] | 0 | 2 |
| dbo | AccountPermissionOverrides | 0 | [OverrideId] | 5 | 16 |
| dbo | ApartmentAccessGrants | 0 | [GrantId] | 4 | 14 |
| dbo | ApartmentOccupancies | 0 | [OccupancyId] | 3 | 12 |
| dbo | ApartmentOwnerships | 0 | [OwnershipId] | 4 | 16 |
| dbo | Apartments | 79 | [BuildingId], [Id] | 18 | 23 |
| dbo | AuditEvents | 1053 | [AuditEventId] | 0 | 14 |
| dbo | AuditLog | 1053 | [AuditId] | 1 | 14 |
| dbo | BackupArtifacts | 0 | [BackupArtifactId] | 2 | 7 |
| dbo | BackupHistory | 2 | [Id] | 0 | 10 |
| dbo | BackupJobs | 2 | [BackupJobId] | 0 | 9 |
| dbo | BackupRetentionEvents | 0 | [RetentionEventId] | 1 | 6 |
| dbo | BackupScheduleConfigurations | 1 | [ConfigurationId] | 0 | 14 |
| dbo | BuildingAdmins | 2 | [BuildingId], [AdminUid] | 2 | 2 |
| dbo | Buildings | 3 | [Id] | 20 | 13 |
| dbo | BuildingSettings | 0 | [BuildingId] | 1 | 10 |
| dbo | Cameras | 3 | [Id] | 1 | 9 |
| dbo | DepositEditHistory | 1 | [EditId] | 1 | 10 |
| dbo | Deposits | 26 | [BuildingId], [Id] | 3 | 24 |
| dbo | DocumentAccessGrants | 0 | [GrantId] | 1 | 6 |
| dbo | DocumentAssets | 0 | [DocumentId] | 2 | 11 |
| dbo | Dvrs | 1 | [Id] | 2 | 11 |
| dbo | Expenses | 12 | [BuildingId], [Id] | 2 | 9 |
| dbo | ExportJobs | 0 | [ExportJobId] | 0 | 9 |
| dbo | FileProcessingJobs | 0 | [JobId] | 1 | 8 |
| dbo | FinancialAllocationBatches | 0 | [AllocationBatchId] | 4 | 14 |
| dbo | FinancialAllocationLines | 0 | [AllocationLineId] | 3 | 9 |
| dbo | FinancialCategories | 38 | [BuildingId], [Kind], [Id] | 3 | 7 |
| dbo | FinancialPeriods | 4 | [PeriodId] | 7 | 12 |
| dbo | FinancialReconciliationDifferences | 2 | [ReconciliationDifferenceId] | 4 | 16 |
| dbo | FinancialReconciliationRuns | 1 | [ReconciliationRunId] | 2 | 8 |
| dbo | FinancialSourceLinks | 26 | [FinancialSourceLinkId] | 1 | 7 |
| dbo | Floors | 16 | [BuildingId], [Id] | 2 | 4 |
| dbo | GlobalApplicationSettings | 1 | [SettingsId] | 0 | 14 |
| dbo | GlobalSettings | 1 | [Id] | 0 | 17 |
| dbo | GuestAccessSessions | 0 | [GuestSessionId] | 1 | 6 |
| dbo | ImportBatches | 0 | [ImportBatchId] | 1 | 7 |
| dbo | ImportRows | 0 | [ImportRowId] | 1 | 7 |
| dbo | InboxMessages | 0 | [InboxMessageId] | 0 | 7 |
| dbo | LedgerEntries | 136 | [LedgerEntryId] | 3 | 11 |
| dbo | LedgerTransactions | 16 | [LedgerTransactionId] | 8 | 16 |
| dbo | LegacyDataMigrationMap | 1482 | [MigrationMapId] | 0 | 11 |
| dbo | LegacyIdentityMap | 84 | [LegacyIdentityMapId] | 3 | 15 |
| dbo | LegacyOperationalRecords | 329 | [LegacyOperationalRecordId] | 0 | 8 |
| dbo | LoginThrottle | 0 | [Key] | 0 | 3 |
| dbo | MaintenanceAssignments | 0 | [AssignmentId] | 1 | 7 |
| dbo | MaintenanceAttachments | 0 | [AttachmentId] | 1 | 9 |
| dbo | MaintenanceComments | 0 | [CommentId] | 1 | 6 |
| dbo | MaintenanceLedgerLinks | 0 | [LinkId] | 1 | 5 |
| dbo | MaintenanceRecords | 1 | [BuildingId], [Id] | 1 | 12 |
| dbo | MaintenanceRequests | 1 | [RequestId] | 5 | 14 |
| dbo | MaintenanceStatusEvents | 0 | [StatusEventId] | 1 | 7 |
| dbo | MediaAccessSessions | 0 | [MediaAccessSessionId] | 0 | 7 |
| dbo | MediaCredentialReferences | 0 | [CredentialReferenceId] | 1 | 9 |
| dbo | MediaProvisioningOperations | 0 | [OperationId] | 1 | 9 |
| dbo | MonthlyApartmentShares | 113 | [BuildingId], [MonthKey], [ApartmentId] | 2 | 6 |
| dbo | NotificationDeliveries | 0 | [DeliveryId] | 1 | 9 |
| dbo | NotificationMessages | 68 | [MessageId] | 1 | 9 |
| dbo | NotificationPreferences | 0 | [PreferenceId] | 0 | 6 |
| dbo | NotificationReadStates | 35 | [ReadStateId] | 1 | 4 |
| dbo | NotificationRecipients | 68 | [RecipientId] | 3 | 6 |
| dbo | Notifications | 68 | [BuildingId], [Id] | 2 | 11 |
| dbo | OutboxMessages | 0 | [OutboxMessageId] | 0 | 11 |
| dbo | PaymentDeposits | 26 | [PaymentDepositId] | 7 | 20 |
| dbo | Persons | 85 | [PersonId] | 5 | 12 |
| dbo | PollBallots | 0 | [BallotId] | 0 | 7 |
| dbo | PollDelegations | 0 | [DelegationId] | 0 | 9 |
| dbo | PollEligibility | 0 | [EligibilityId] | 0 | 9 |
| dbo | PollLifecycleEvents | 1 | [EventId] | 0 | 8 |
| dbo | PollOptions | 2 | [BuildingId], [PollId], [Id] | 2 | 5 |
| dbo | Polls | 1 | [BuildingId], [Id] | 2 | 8 |
| dbo | PollVotes | 1 | [BuildingId], [PollId], [ApartmentId] | 2 | 5 |
| dbo | PollVotingPolicies | 0 | [PolicyId] | 0 | 8 |
| dbo | Presence | 10 | [Uid] | 0 | 4 |
| dbo | PresenceHeartbeats | 0 | [HeartbeatId] | 1 | 4 |
| dbo | PresenceSessions | 10 | [PresenceSessionId] | 1 | 11 |
| dbo | ProtectedIntegrationSecrets | 0 | [SecretId] | 0 | 10 |
| dbo | QrCapabilities | 9 | [CapabilityId] | 2 | 11 |
| dbo | QrCapabilityUses | 0 | [UseId] | 1 | 5 |
| dbo | QrTokens | 9 | [Uid] | 2 | 11 |
| dbo | QrUsages | 5 | [Id] | 0 | 7 |
| dbo | Residents | 79 | [Id] | 1 | 21 |
| dbo | RestoreOperations | 0 | [RestoreOperationId] | 1 | 10 |
| dbo | Revenues | 4 | [BuildingId], [Id] | 2 | 8 |
| dbo | RoleDefinitions | 2 | [RoleKey] | 2 | 4 |
| dbo | RolePermissions | 28 | [RoleKey], [PermissionKey] | 1 | 3 |
| dbo | ScheduledBackupSettings | 1 | [Id] | 0 | 12 |
| dbo | SyncPendingChanges | 0 | [Id] | 0 | 10 |
| dbo | UserAccounts | 6 | [AccountId] | 25 | 10 |
| dbo | UserCredentials | 1 | [CredentialId] | 2 | 10 |
| dbo | UserPermissions | 81 | [Uid], [Permission] | 1 | 2 |
| dbo | UserPreferences | 0 | [PreferenceId] | 0 | 5 |
| dbo | UserRecoveryCodes | 0 | [RecoveryCodeId] | 1 | 5 |
| dbo | UserRoleAssignments | 4 | [RoleAssignmentId] | 6 | 15 |
| dbo | Users | 5 | [Uid] | 2 | 20 |
| dbo | UserTwoFactorSettings | 0 | [AccountId] | 1 | 9 |
| dbo | WalletAdjustments | 2 | [BuildingId], [Id] | 2 | 8 |

## Column inventory

| Schema | Table | Column | SQL type | Max length | Precision | Scale | Nullable | Identity |
|---|---|---|---|---:|---:|---:|---|---|
| dbo | __EFMigrationsHistory | MigrationId | nvarchar | 300 | 0 | 0 | False | False |
| dbo | __EFMigrationsHistory | ProductVersion | nvarchar | 64 | 0 | 0 | False | False |
| dbo | AccountPermissionOverrides | OverrideId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | AccountPermissionOverrides | AccountId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | AccountPermissionOverrides | PermissionKey | nvarchar | 200 | 0 | 0 | False | False |
| dbo | AccountPermissionOverrides | Effect | tinyint | 1 | 3 | 0 | False | False |
| dbo | AccountPermissionOverrides | ScopeType | tinyint | 1 | 3 | 0 | False | False |
| dbo | AccountPermissionOverrides | ScopeBuildingId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | AccountPermissionOverrides | ScopeApartmentId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | AccountPermissionOverrides | ValidFromUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | AccountPermissionOverrides | ValidToUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | AccountPermissionOverrides | GrantedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | AccountPermissionOverrides | GrantedByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | AccountPermissionOverrides | GrantReason | nvarchar | 1000 | 0 | 0 | True | False |
| dbo | AccountPermissionOverrides | RevokedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | AccountPermissionOverrides | RevokedByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | AccountPermissionOverrides | RevocationReason | nvarchar | 1000 | 0 | 0 | True | False |
| dbo | AccountPermissionOverrides | RowVersion | timestamp | 8 | 0 | 0 | False | False |
| dbo | ApartmentAccessGrants | GrantId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | ApartmentAccessGrants | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | ApartmentAccessGrants | ApartmentId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | ApartmentAccessGrants | PersonId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | ApartmentAccessGrants | GrantType | tinyint | 1 | 3 | 0 | False | False |
| dbo | ApartmentAccessGrants | PermissionSetKey | nvarchar | 160 | 0 | 0 | False | False |
| dbo | ApartmentAccessGrants | ValidFromUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | ApartmentAccessGrants | ValidToUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | ApartmentAccessGrants | GrantedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | ApartmentAccessGrants | GrantedByAccountId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | ApartmentAccessGrants | RevokedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | ApartmentAccessGrants | RevokedByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | ApartmentAccessGrants | Reason | nvarchar | 1000 | 0 | 0 | False | False |
| dbo | ApartmentAccessGrants | RowVersion | timestamp | 8 | 0 | 0 | False | False |
| dbo | ApartmentOccupancies | OccupancyId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | ApartmentOccupancies | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | ApartmentOccupancies | ApartmentId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | ApartmentOccupancies | PersonId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | ApartmentOccupancies | OccupancyType | tinyint | 1 | 3 | 0 | False | False |
| dbo | ApartmentOccupancies | IsPrimary | bit | 1 | 1 | 0 | False | False |
| dbo | ApartmentOccupancies | ValidFromUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | ApartmentOccupancies | ValidToUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | ApartmentOccupancies | RecordedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | ApartmentOccupancies | RecordedByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | ApartmentOccupancies | Notes | nvarchar | 2000 | 0 | 0 | True | False |
| dbo | ApartmentOccupancies | RowVersion | timestamp | 8 | 0 | 0 | False | False |
| dbo | ApartmentOwnerships | OwnershipId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | ApartmentOwnerships | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | ApartmentOwnerships | ApartmentId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | ApartmentOwnerships | PersonId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | ApartmentOwnerships | OwnershipSharePercent | decimal | 5 | 9 | 6 | False | False |
| dbo | ApartmentOwnerships | CompletenessStatus | tinyint | 1 | 3 | 0 | False | False |
| dbo | ApartmentOwnerships | ValidFromUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | ApartmentOwnerships | ValidToUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | ApartmentOwnerships | RecordedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | ApartmentOwnerships | RecordedByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | ApartmentOwnerships | EvidenceReference | nvarchar | 1000 | 0 | 0 | False | False |
| dbo | ApartmentOwnerships | CompletenessVerifiedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | ApartmentOwnerships | CompletenessVerifiedByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | ApartmentOwnerships | Notes | nvarchar | 2000 | 0 | 0 | True | False |
| dbo | ApartmentOwnerships | RowVersion | timestamp | 8 | 0 | 0 | False | False |
| dbo | ApartmentOwnerships | RecordSource | tinyint | 1 | 3 | 0 | False | False |
| dbo | Apartments | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Apartments | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Apartments | FloorId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Apartments | Number | int | 4 | 10 | 0 | False | False |
| dbo | Apartments | Owner | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Apartments | Phone | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Apartments | Email | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Apartments | MonthlyFee | decimal | 9 | 18 | 2 | False | False |
| dbo | Apartments | Pin | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Apartments | IsClosed | bit | 1 | 1 | 0 | False | False |
| dbo | Apartments | Label | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Apartments | Notes | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Apartments | OpenDate | date | 3 | 10 | 0 | True | False |
| dbo | Apartments | CloseDate | date | 3 | 10 | 0 | True | False |
| dbo | Apartments | IsDisabled | bit | 1 | 1 | 0 | False | False |
| dbo | Apartments | DisabledReason | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Apartments | Language | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Apartments | NotifyWhatsApp | bit | 1 | 1 | 0 | False | False |
| dbo | Apartments | NotifyEmail | bit | 1 | 1 | 0 | False | False |
| dbo | Apartments | NotifyInApp | bit | 1 | 1 | 0 | False | False |
| dbo | Apartments | PreferredPaymentMethod | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Apartments | SettingsUpdatedAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | Apartments | SettingsUpdatedBy | nvarchar | -1 | 0 | 0 | True | False |
| dbo | AuditEvents | AuditEventId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | AuditEvents | OccurredAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | AuditEvents | ActorAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | AuditEvents | ActorReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | AuditEvents | BuildingId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | AuditEvents | EventCategory | nvarchar | 160 | 0 | 0 | False | False |
| dbo | AuditEvents | EventType | nvarchar | 240 | 0 | 0 | False | False |
| dbo | AuditEvents | ResourceType | nvarchar | 200 | 0 | 0 | True | False |
| dbo | AuditEvents | ResourceReference | nvarchar | 400 | 0 | 0 | True | False |
| dbo | AuditEvents | CorrelationId | nvarchar | 200 | 0 | 0 | True | False |
| dbo | AuditEvents | Outcome | tinyint | 1 | 3 | 0 | False | False |
| dbo | AuditEvents | SafeDetailsJson | nvarchar | -1 | 0 | 0 | True | False |
| dbo | AuditEvents | PreviousHash | varbinary | 32 | 0 | 0 | True | False |
| dbo | AuditEvents | EventHash | varbinary | 32 | 0 | 0 | True | False |
| dbo | AuditLog | AuditId | bigint | 8 | 19 | 0 | False | True |
| dbo | AuditLog | SourceId | nvarchar | -1 | 0 | 0 | True | False |
| dbo | AuditLog | BuildingId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | AuditLog | Ts | datetime2 | 6 | 19 | 0 | False | False |
| dbo | AuditLog | Action | nvarchar | -1 | 0 | 0 | False | False |
| dbo | AuditLog | Label | nvarchar | -1 | 0 | 0 | False | False |
| dbo | AuditLog | Actor | nvarchar | -1 | 0 | 0 | False | False |
| dbo | AuditLog | ActorRole | nvarchar | -1 | 0 | 0 | False | False |
| dbo | AuditLog | Details | nvarchar | -1 | 0 | 0 | False | False |
| dbo | AuditLog | MonthKey | nvarchar | 14 | 0 | 0 | False | False |
| dbo | AuditLog | BuildingNumber | nvarchar | -1 | 0 | 0 | True | False |
| dbo | AuditLog | AptNumber | int | 4 | 10 | 0 | True | False |
| dbo | AuditLog | ApartmentId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | AuditLog | Severity | nvarchar | 40 | 0 | 0 | False | False |
| dbo | BackupArtifacts | BackupArtifactId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | BackupArtifacts | StorageReference | nvarchar | 1000 | 0 | 0 | False | False |
| dbo | BackupArtifacts | Sha256 | varbinary | 32 | 0 | 0 | False | False |
| dbo | BackupArtifacts | SizeBytes | bigint | 8 | 19 | 0 | False | False |
| dbo | BackupArtifacts | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | BackupArtifacts | RetainUntilUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | BackupArtifacts | EncryptionKeyVersion | nvarchar | 200 | 0 | 0 | True | False |
| dbo | BackupHistory | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | BackupHistory | FileName | nvarchar | -1 | 0 | 0 | False | False |
| dbo | BackupHistory | FileSize | bigint | 8 | 19 | 0 | False | False |
| dbo | BackupHistory | CollectionsJson | nvarchar | -1 | 0 | 0 | False | False |
| dbo | BackupHistory | TotalDocuments | int | 4 | 10 | 0 | False | False |
| dbo | BackupHistory | CreatedAt | datetime2 | 6 | 19 | 0 | False | False |
| dbo | BackupHistory | DurationSeconds | float | 8 | 53 | 0 | False | False |
| dbo | BackupHistory | Status | nvarchar | -1 | 0 | 0 | False | False |
| dbo | BackupHistory | Source | nvarchar | -1 | 0 | 0 | False | False |
| dbo | BackupHistory | ErrorMessage | nvarchar | -1 | 0 | 0 | True | False |
| dbo | BackupJobs | BackupJobId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | BackupJobs | JobType | tinyint | 1 | 3 | 0 | False | False |
| dbo | BackupJobs | Status | tinyint | 1 | 3 | 0 | False | False |
| dbo | BackupJobs | StartedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | BackupJobs | CompletedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | BackupJobs | RequestedByReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | BackupJobs | DatabaseReference | nvarchar | 400 | 0 | 0 | False | False |
| dbo | BackupJobs | BackupArtifactId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | BackupJobs | SafeErrorCode | nvarchar | 200 | 0 | 0 | True | False |
| dbo | BackupRetentionEvents | RetentionEventId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | BackupRetentionEvents | BackupArtifactId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | BackupRetentionEvents | EventType | tinyint | 1 | 3 | 0 | False | False |
| dbo | BackupRetentionEvents | OccurredAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | BackupRetentionEvents | ActorReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | BackupRetentionEvents | Reason | nvarchar | 2000 | 0 | 0 | True | False |
| dbo | BackupScheduleConfigurations | ConfigurationId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | BackupScheduleConfigurations | LegacySettingId | nvarchar | 40 | 0 | 0 | False | False |
| dbo | BackupScheduleConfigurations | IsEnabled | bit | 1 | 1 | 0 | False | False |
| dbo | BackupScheduleConfigurations | Frequency | nvarchar | 40 | 0 | 0 | False | False |
| dbo | BackupScheduleConfigurations | ScheduledHour | int | 4 | 10 | 0 | False | False |
| dbo | BackupScheduleConfigurations | DayOfWeek | int | 4 | 10 | 0 | True | False |
| dbo | BackupScheduleConfigurations | DayOfMonth | int | 4 | 10 | 0 | True | False |
| dbo | BackupScheduleConfigurations | CollectionsJson | nvarchar | -1 | 0 | 0 | False | False |
| dbo | BackupScheduleConfigurations | MaxBackupsToKeep | int | 4 | 10 | 0 | False | False |
| dbo | BackupScheduleConfigurations | LastRunAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | BackupScheduleConfigurations | LastRunStatus | nvarchar | 60 | 0 | 0 | True | False |
| dbo | BackupScheduleConfigurations | UpdatedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | BackupScheduleConfigurations | UpdatedByReference | nvarchar | 256 | 0 | 0 | True | False |
| dbo | BackupScheduleConfigurations | ImportedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | BuildingAdmins | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | BuildingAdmins | AdminUid | nvarchar | 256 | 0 | 0 | False | False |
| dbo | Buildings | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Buildings | BuildingNumber | nvarchar | 900 | 0 | 0 | False | False |
| dbo | Buildings | Name | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Buildings | AdminPin | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Buildings | DataVersion | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Buildings | LogoUrl | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Buildings | AdminWhatsapp | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Buildings | PaymentLabel | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Buildings | PaymentAccountNumber | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Buildings | PaymentPhone | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Buildings | PaymentNotes | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Buildings | RowVersion | timestamp | 8 | 0 | 0 | False | False |
| dbo | Buildings | CreatedAt | datetime2 | 6 | 19 | 0 | False | False |
| dbo | BuildingSettings | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | BuildingSettings | DisplayName | nvarchar | -1 | 0 | 0 | False | False |
| dbo | BuildingSettings | Address | nvarchar | -1 | 0 | 0 | False | False |
| dbo | BuildingSettings | WhatsappNumber | nvarchar | -1 | 0 | 0 | False | False |
| dbo | BuildingSettings | InvoiceDayOfMonth | int | 4 | 10 | 0 | False | False |
| dbo | BuildingSettings | ExpenseDistribution | nvarchar | -1 | 0 | 0 | False | False |
| dbo | BuildingSettings | VotingQuorumPercent | int | 4 | 10 | 0 | False | False |
| dbo | BuildingSettings | Currency | nvarchar | -1 | 0 | 0 | False | False |
| dbo | BuildingSettings | UpdatedAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | BuildingSettings | UpdatedBy | nvarchar | -1 | 0 | 0 | True | False |
| dbo | Cameras | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Cameras | DvrId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Cameras | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Cameras | Name | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Cameras | Channel | int | 4 | 10 | 0 | False | False |
| dbo | Cameras | RtspPath | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Cameras | HlsUrl | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Cameras | IsActive | bit | 1 | 1 | 0 | False | False |
| dbo | Cameras | CreatedAt | datetime2 | 6 | 19 | 0 | False | False |
| dbo | DepositEditHistory | EditId | bigint | 8 | 19 | 0 | False | True |
| dbo | DepositEditHistory | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | DepositEditHistory | DepositId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | DepositEditHistory | Ts | datetime2 | 6 | 19 | 0 | False | False |
| dbo | DepositEditHistory | ByUser | nvarchar | -1 | 0 | 0 | False | False |
| dbo | DepositEditHistory | Reason | nvarchar | -1 | 0 | 0 | False | False |
| dbo | DepositEditHistory | OldAmount | decimal | 9 | 18 | 2 | False | False |
| dbo | DepositEditHistory | NewAmount | decimal | 9 | 18 | 2 | False | False |
| dbo | DepositEditHistory | OldNote | nvarchar | -1 | 0 | 0 | False | False |
| dbo | DepositEditHistory | NewNote | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Deposits | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Deposits | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Deposits | MonthKey | char | 7 | 0 | 0 | False | False |
| dbo | Deposits | Number | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Deposits | ApartmentId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Deposits | Amount | decimal | 9 | 18 | 2 | False | False |
| dbo | Deposits | Note | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Deposits | Status | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Deposits | ReceiptUrl | nvarchar | -1 | 0 | 0 | True | False |
| dbo | Deposits | ReceiptPath | nvarchar | -1 | 0 | 0 | True | False |
| dbo | Deposits | ReceiptFileName | nvarchar | -1 | 0 | 0 | True | False |
| dbo | Deposits | ReceiptFileSize | bigint | 8 | 19 | 0 | True | False |
| dbo | Deposits | ReceiptFileType | nvarchar | -1 | 0 | 0 | True | False |
| dbo | Deposits | ReceiptSuccess | bit | 1 | 1 | 0 | True | False |
| dbo | Deposits | CreatedAt | datetime2 | 6 | 19 | 0 | False | False |
| dbo | Deposits | CreatedBy | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Deposits | ConfirmedAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | Deposits | ConfirmedBy | nvarchar | -1 | 0 | 0 | True | False |
| dbo | Deposits | CancelledAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | Deposits | CancelledBy | nvarchar | -1 | 0 | 0 | True | False |
| dbo | Deposits | CancelledReason | nvarchar | -1 | 0 | 0 | True | False |
| dbo | Deposits | UpdatedAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | Deposits | UpdatedBy | nvarchar | -1 | 0 | 0 | True | False |
| dbo | Deposits | UpdateReason | nvarchar | -1 | 0 | 0 | True | False |
| dbo | DocumentAccessGrants | GrantId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | DocumentAccessGrants | DocumentId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | DocumentAccessGrants | SubjectReference | nvarchar | 320 | 0 | 0 | False | False |
| dbo | DocumentAccessGrants | GrantedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | DocumentAccessGrants | ExpiresAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | DocumentAccessGrants | RevokedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | DocumentAssets | DocumentId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | DocumentAssets | BuildingId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | DocumentAssets | OwnerReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | DocumentAssets | StorageKey | nvarchar | 1000 | 0 | 0 | False | False |
| dbo | DocumentAssets | FileName | nvarchar | 520 | 0 | 0 | False | False |
| dbo | DocumentAssets | ContentType | nvarchar | 240 | 0 | 0 | True | False |
| dbo | DocumentAssets | SizeBytes | bigint | 8 | 19 | 0 | False | False |
| dbo | DocumentAssets | Sha256 | varbinary | 32 | 0 | 0 | True | False |
| dbo | DocumentAssets | Classification | tinyint | 1 | 3 | 0 | False | False |
| dbo | DocumentAssets | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | DocumentAssets | RetainUntilUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | Dvrs | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Dvrs | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Dvrs | Name | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Dvrs | IpAddress | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Dvrs | Port | int | 4 | 10 | 0 | False | False |
| dbo | Dvrs | Brand | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Dvrs | Username | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Dvrs | PasswordEncrypted | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Dvrs | IsActive | bit | 1 | 1 | 0 | False | False |
| dbo | Dvrs | CreatedAt | datetime2 | 6 | 19 | 0 | False | False |
| dbo | Dvrs | UpdatedAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | Expenses | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Expenses | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Expenses | MonthKey | char | 7 | 0 | 0 | False | False |
| dbo | Expenses | CategoryId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | Expenses | CategoryKind | nvarchar | 20 | 0 | 0 | False | False |
| dbo | Expenses | Note | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Expenses | Amount | decimal | 9 | 18 | 2 | False | False |
| dbo | Expenses | ExpenseDate | date | 3 | 10 | 0 | False | False |
| dbo | Expenses | ReceiptUrl | nvarchar | -1 | 0 | 0 | True | False |
| dbo | ExportJobs | ExportJobId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | ExportJobs | RequestedByReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | ExportJobs | ScopeJson | nvarchar | 4000 | 0 | 0 | False | False |
| dbo | ExportJobs | Format | nvarchar | 40 | 0 | 0 | False | False |
| dbo | ExportJobs | Status | tinyint | 1 | 3 | 0 | False | False |
| dbo | ExportJobs | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | ExportJobs | CompletedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | ExportJobs | ExpiresAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | ExportJobs | ArtifactStorageKey | nvarchar | 1000 | 0 | 0 | True | False |
| dbo | FileProcessingJobs | JobId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | FileProcessingJobs | DocumentId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | FileProcessingJobs | JobType | tinyint | 1 | 3 | 0 | False | False |
| dbo | FileProcessingJobs | Status | tinyint | 1 | 3 | 0 | False | False |
| dbo | FileProcessingJobs | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | FileProcessingJobs | CompletedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | FileProcessingJobs | Attempts | int | 4 | 10 | 0 | False | False |
| dbo | FileProcessingJobs | SafeErrorCode | nvarchar | 200 | 0 | 0 | True | False |
| dbo | FinancialAllocationBatches | AllocationBatchId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | FinancialAllocationBatches | PeriodId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | FinancialAllocationBatches | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | FinancialAllocationBatches | SourceTransactionId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | FinancialAllocationBatches | BasisKey | nvarchar | 160 | 0 | 0 | False | False |
| dbo | FinancialAllocationBatches | AlgorithmVersion | nvarchar | 160 | 0 | 0 | False | False |
| dbo | FinancialAllocationBatches | InputFingerprint | varbinary | 32 | 0 | 0 | False | False |
| dbo | FinancialAllocationBatches | SourceAmount | decimal | 9 | 19 | 4 | False | False |
| dbo | FinancialAllocationBatches | AllocatedAmount | decimal | 9 | 19 | 4 | False | False |
| dbo | FinancialAllocationBatches | CurrencyCode | char | 3 | 0 | 0 | False | False |
| dbo | FinancialAllocationBatches | Status | tinyint | 1 | 3 | 0 | False | False |
| dbo | FinancialAllocationBatches | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | FinancialAllocationBatches | CompletedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | FinancialAllocationBatches | CreatedByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | FinancialAllocationLines | AllocationLineId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | FinancialAllocationLines | AllocationBatchId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | FinancialAllocationLines | PeriodId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | FinancialAllocationLines | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | FinancialAllocationLines | ApartmentId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | FinancialAllocationLines | Weight | decimal | 9 | 19 | 8 | False | False |
| dbo | FinancialAllocationLines | ExactShare | decimal | 9 | 19 | 8 | False | False |
| dbo | FinancialAllocationLines | AllocatedAmount | decimal | 9 | 19 | 4 | False | False |
| dbo | FinancialAllocationLines | RemainderRank | int | 4 | 10 | 0 | False | False |
| dbo | FinancialCategories | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | FinancialCategories | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | FinancialCategories | Kind | nvarchar | 20 | 0 | 0 | False | False |
| dbo | FinancialCategories | Name | nvarchar | -1 | 0 | 0 | False | False |
| dbo | FinancialCategories | Color | nvarchar | -1 | 0 | 0 | False | False |
| dbo | FinancialCategories | IsActive | bit | 1 | 1 | 0 | False | False |
| dbo | FinancialCategories | SortOrder | int | 4 | 10 | 0 | False | False |
| dbo | FinancialPeriods | PeriodId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | FinancialPeriods | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | FinancialPeriods | PeriodKey | char | 7 | 0 | 0 | False | False |
| dbo | FinancialPeriods | TimeZoneId | nvarchar | 200 | 0 | 0 | False | False |
| dbo | FinancialPeriods | Status | tinyint | 1 | 3 | 0 | False | False |
| dbo | FinancialPeriods | StartsAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | FinancialPeriods | EndsAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | FinancialPeriods | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | FinancialPeriods | ClosedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | FinancialPeriods | ClosedByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | FinancialPeriods | CloseReason | nvarchar | 1000 | 0 | 0 | True | False |
| dbo | FinancialPeriods | RowVersion | timestamp | 8 | 0 | 0 | False | False |
| dbo | FinancialReconciliationDifferences | ReconciliationDifferenceId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | FinancialReconciliationDifferences | ReconciliationRunId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | FinancialReconciliationDifferences | DifferenceType | nvarchar | 160 | 0 | 0 | False | False |
| dbo | FinancialReconciliationDifferences | BuildingId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | FinancialReconciliationDifferences | ApartmentId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | FinancialReconciliationDifferences | PeriodKey | char | 7 | 0 | 0 | True | False |
| dbo | FinancialReconciliationDifferences | SourceType | nvarchar | 160 | 0 | 0 | True | False |
| dbo | FinancialReconciliationDifferences | SourceKey | nvarchar | 512 | 0 | 0 | True | False |
| dbo | FinancialReconciliationDifferences | SourceAmount | decimal | 9 | 19 | 4 | True | False |
| dbo | FinancialReconciliationDifferences | TargetAmount | decimal | 9 | 19 | 4 | True | False |
| dbo | FinancialReconciliationDifferences | DifferenceAmount | decimal | 9 | 19 | 4 | True | False |
| dbo | FinancialReconciliationDifferences | Classification | nvarchar | 160 | 0 | 0 | False | False |
| dbo | FinancialReconciliationDifferences | Disposition | nvarchar | 2000 | 0 | 0 | True | False |
| dbo | FinancialReconciliationDifferences | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | FinancialReconciliationDifferences | ReviewedByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | FinancialReconciliationDifferences | ReviewedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | FinancialReconciliationRuns | ReconciliationRunId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | FinancialReconciliationRuns | RuleVersion | nvarchar | 160 | 0 | 0 | False | False |
| dbo | FinancialReconciliationRuns | SourceSystem | nvarchar | 160 | 0 | 0 | False | False |
| dbo | FinancialReconciliationRuns | StartedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | FinancialReconciliationRuns | CompletedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | FinancialReconciliationRuns | Status | tinyint | 1 | 3 | 0 | False | False |
| dbo | FinancialReconciliationRuns | StartedByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | FinancialReconciliationRuns | SafeSummary | nvarchar | 4000 | 0 | 0 | True | False |
| dbo | FinancialSourceLinks | FinancialSourceLinkId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | FinancialSourceLinks | SourceSystem | nvarchar | 160 | 0 | 0 | False | False |
| dbo | FinancialSourceLinks | SourceType | nvarchar | 160 | 0 | 0 | False | False |
| dbo | FinancialSourceLinks | SourceKey | nvarchar | 512 | 0 | 0 | False | False |
| dbo | FinancialSourceLinks | LedgerTransactionId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | FinancialSourceLinks | SourceFingerprint | varbinary | 32 | 0 | 0 | True | False |
| dbo | FinancialSourceLinks | LinkedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | Floors | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Floors | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Floors | Label | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Floors | SortOrder | int | 4 | 10 | 0 | False | False |
| dbo | GlobalApplicationSettings | SettingsId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | GlobalApplicationSettings | AppName | nvarchar | 400 | 0 | 0 | False | False |
| dbo | GlobalApplicationSettings | DefaultLanguage | nvarchar | 32 | 0 | 0 | False | False |
| dbo | GlobalApplicationSettings | EnablePolls | bit | 1 | 1 | 0 | False | False |
| dbo | GlobalApplicationSettings | EnableMaintenance | bit | 1 | 1 | 0 | False | False |
| dbo | GlobalApplicationSettings | EnableOcr | bit | 1 | 1 | 0 | False | False |
| dbo | GlobalApplicationSettings | EnableQrAccess | bit | 1 | 1 | 0 | False | False |
| dbo | GlobalApplicationSettings | EnableWhatsApp | bit | 1 | 1 | 0 | False | False |
| dbo | GlobalApplicationSettings | SessionTimeoutDays | int | 4 | 10 | 0 | False | False |
| dbo | GlobalApplicationSettings | MaxPinAttempts | int | 4 | 10 | 0 | False | False |
| dbo | GlobalApplicationSettings | StorageMode | nvarchar | 64 | 0 | 0 | False | False |
| dbo | GlobalApplicationSettings | UpdatedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | GlobalApplicationSettings | UpdatedByReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | GlobalApplicationSettings | ImportedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | GlobalSettings | Id | nvarchar | 40 | 0 | 0 | False | False |
| dbo | GlobalSettings | AppName | nvarchar | -1 | 0 | 0 | False | False |
| dbo | GlobalSettings | DefaultLanguage | nvarchar | -1 | 0 | 0 | False | False |
| dbo | GlobalSettings | WhatsappApiToken | nvarchar | -1 | 0 | 0 | False | False |
| dbo | GlobalSettings | WhatsappPhoneId | nvarchar | -1 | 0 | 0 | False | False |
| dbo | GlobalSettings | CloudinaryCloudName | nvarchar | -1 | 0 | 0 | False | False |
| dbo | GlobalSettings | CloudinaryApiKey | nvarchar | -1 | 0 | 0 | False | False |
| dbo | GlobalSettings | EnablePolls | bit | 1 | 1 | 0 | False | False |
| dbo | GlobalSettings | EnableMaintenance | bit | 1 | 1 | 0 | False | False |
| dbo | GlobalSettings | EnableOcr | bit | 1 | 1 | 0 | False | False |
| dbo | GlobalSettings | EnableQrAccess | bit | 1 | 1 | 0 | False | False |
| dbo | GlobalSettings | EnableWhatsApp | bit | 1 | 1 | 0 | False | False |
| dbo | GlobalSettings | SessionTimeoutDays | int | 4 | 10 | 0 | False | False |
| dbo | GlobalSettings | MaxPinAttempts | int | 4 | 10 | 0 | False | False |
| dbo | GlobalSettings | UpdatedAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | GlobalSettings | UpdatedBy | nvarchar | -1 | 0 | 0 | True | False |
| dbo | GlobalSettings | StorageMode | nvarchar | -1 | 0 | 0 | False | False |
| dbo | GuestAccessSessions | GuestSessionId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | GuestAccessSessions | CapabilityId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | GuestAccessSessions | GuestReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | GuestAccessSessions | StartedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | GuestAccessSessions | ExpiresAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | GuestAccessSessions | RevokedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | ImportBatches | ImportBatchId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | ImportBatches | ImportType | nvarchar | 200 | 0 | 0 | False | False |
| dbo | ImportBatches | SourceFingerprint | varbinary | 32 | 0 | 0 | False | False |
| dbo | ImportBatches | RequestedByReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | ImportBatches | Status | tinyint | 1 | 3 | 0 | False | False |
| dbo | ImportBatches | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | ImportBatches | CompletedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | ImportRows | ImportRowId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | ImportRows | ImportBatchId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | ImportRows | SourceRowKey | nvarchar | 400 | 0 | 0 | False | False |
| dbo | ImportRows | RowFingerprint | varbinary | 32 | 0 | 0 | False | False |
| dbo | ImportRows | Status | tinyint | 1 | 3 | 0 | False | False |
| dbo | ImportRows | SafeErrorCode | nvarchar | 200 | 0 | 0 | True | False |
| dbo | ImportRows | TargetReference | nvarchar | 400 | 0 | 0 | True | False |
| dbo | InboxMessages | InboxMessageId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | InboxMessages | Consumer | nvarchar | 320 | 0 | 0 | False | False |
| dbo | InboxMessages | MessageId | nvarchar | 400 | 0 | 0 | False | False |
| dbo | InboxMessages | ReceivedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | InboxMessages | ProcessedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | InboxMessages | Outcome | tinyint | 1 | 3 | 0 | False | False |
| dbo | InboxMessages | SafeErrorCode | nvarchar | 200 | 0 | 0 | True | False |
| dbo | LedgerEntries | LedgerEntryId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | LedgerEntries | LedgerTransactionId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | LedgerEntries | PeriodId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | LedgerEntries | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | LedgerEntries | ApartmentId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | LedgerEntries | EntryKind | tinyint | 1 | 3 | 0 | False | False |
| dbo | LedgerEntries | SignedAmount | decimal | 9 | 19 | 4 | False | False |
| dbo | LedgerEntries | CurrencyCode | char | 3 | 0 | 0 | False | False |
| dbo | LedgerEntries | Description | nvarchar | 1000 | 0 | 0 | True | False |
| dbo | LedgerEntries | EffectiveAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | LedgerEntries | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | LedgerTransactions | LedgerTransactionId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | LedgerTransactions | PeriodId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | LedgerTransactions | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | LedgerTransactions | Kind | tinyint | 1 | 3 | 0 | False | False |
| dbo | LedgerTransactions | Status | tinyint | 1 | 3 | 0 | False | False |
| dbo | LedgerTransactions | IdempotencyKey | nvarchar | 256 | 0 | 0 | False | False |
| dbo | LedgerTransactions | SourceSystem | nvarchar | 160 | 0 | 0 | True | False |
| dbo | LedgerTransactions | SourceType | nvarchar | 160 | 0 | 0 | True | False |
| dbo | LedgerTransactions | SourceKey | nvarchar | 512 | 0 | 0 | True | False |
| dbo | LedgerTransactions | ReversesTransactionId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | LedgerTransactions | ActorAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | LedgerTransactions | ActorReference | nvarchar | 256 | 0 | 0 | True | False |
| dbo | LedgerTransactions | Reason | nvarchar | 2000 | 0 | 0 | True | False |
| dbo | LedgerTransactions | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | LedgerTransactions | PostedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | LedgerTransactions | RowVersion | timestamp | 8 | 0 | 0 | False | False |
| dbo | LegacyDataMigrationMap | MigrationMapId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | LegacyDataMigrationMap | SourceSystem | nvarchar | 160 | 0 | 0 | False | False |
| dbo | LegacyDataMigrationMap | SourceTable | nvarchar | 256 | 0 | 0 | False | False |
| dbo | LegacyDataMigrationMap | SourceKey | nvarchar | 512 | 0 | 0 | False | False |
| dbo | LegacyDataMigrationMap | TargetTable | nvarchar | 256 | 0 | 0 | False | False |
| dbo | LegacyDataMigrationMap | TargetKey | nvarchar | 512 | 0 | 0 | True | False |
| dbo | LegacyDataMigrationMap | SourceFingerprint | varbinary | 32 | 0 | 0 | True | False |
| dbo | LegacyDataMigrationMap | Outcome | tinyint | 1 | 3 | 0 | False | False |
| dbo | LegacyDataMigrationMap | Disposition | nvarchar | 160 | 0 | 0 | False | False |
| dbo | LegacyDataMigrationMap | MigratedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | LegacyDataMigrationMap | SafeNote | nvarchar | 1000 | 0 | 0 | True | False |
| dbo | LegacyIdentityMap | LegacyIdentityMapId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | LegacyIdentityMap | SourceSystem | nvarchar | 160 | 0 | 0 | False | False |
| dbo | LegacyIdentityMap | SourceTable | nvarchar | 256 | 0 | 0 | False | False |
| dbo | LegacyIdentityMap | SourceKey | nvarchar | 512 | 0 | 0 | False | False |
| dbo | LegacyIdentityMap | PersonId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | LegacyIdentityMap | AccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | LegacyIdentityMap | MappingStatus | tinyint | 1 | 3 | 0 | False | False |
| dbo | LegacyIdentityMap | MatchMethod | nvarchar | 160 | 0 | 0 | True | False |
| dbo | LegacyIdentityMap | EvidenceReference | nvarchar | 1000 | 0 | 0 | True | False |
| dbo | LegacyIdentityMap | ReviewReason | nvarchar | 2000 | 0 | 0 | True | False |
| dbo | LegacyIdentityMap | ReviewedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | LegacyIdentityMap | ReviewedByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | LegacyIdentityMap | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | LegacyIdentityMap | UpdatedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | LegacyIdentityMap | RowVersion | timestamp | 8 | 0 | 0 | False | False |
| dbo | LegacyOperationalRecords | LegacyOperationalRecordId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | LegacyOperationalRecords | SourceSystem | nvarchar | 160 | 0 | 0 | False | False |
| dbo | LegacyOperationalRecords | SourceTable | nvarchar | 256 | 0 | 0 | False | False |
| dbo | LegacyOperationalRecords | SourceKey | nvarchar | 512 | 0 | 0 | False | False |
| dbo | LegacyOperationalRecords | BuildingId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | LegacyOperationalRecords | PayloadJson | nvarchar | -1 | 0 | 0 | False | False |
| dbo | LegacyOperationalRecords | SourceFingerprint | varbinary | 32 | 0 | 0 | False | False |
| dbo | LegacyOperationalRecords | ImportedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | LoginThrottle | Key | nvarchar | 160 | 0 | 0 | False | False |
| dbo | LoginThrottle | Count | int | 4 | 10 | 0 | False | False |
| dbo | LoginThrottle | ExpiresAt | datetime2 | 8 | 27 | 7 | False | False |
| dbo | MaintenanceAssignments | AssignmentId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | MaintenanceAssignments | RequestId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | MaintenanceAssignments | AssigneeReference | nvarchar | 320 | 0 | 0 | False | False |
| dbo | MaintenanceAssignments | VendorName | nvarchar | 400 | 0 | 0 | True | False |
| dbo | MaintenanceAssignments | AssignedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | MaintenanceAssignments | CompletedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | MaintenanceAssignments | Notes | nvarchar | 2000 | 0 | 0 | True | False |
| dbo | MaintenanceAttachments | AttachmentId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | MaintenanceAttachments | RequestId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | MaintenanceAttachments | StorageKey | nvarchar | 1000 | 0 | 0 | False | False |
| dbo | MaintenanceAttachments | FileName | nvarchar | 520 | 0 | 0 | False | False |
| dbo | MaintenanceAttachments | ContentType | nvarchar | 240 | 0 | 0 | True | False |
| dbo | MaintenanceAttachments | SizeBytes | bigint | 8 | 19 | 0 | False | False |
| dbo | MaintenanceAttachments | Sha256 | varbinary | 32 | 0 | 0 | True | False |
| dbo | MaintenanceAttachments | UploadedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | MaintenanceAttachments | UploadedByReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | MaintenanceComments | CommentId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | MaintenanceComments | RequestId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | MaintenanceComments | AuthorReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | MaintenanceComments | Visibility | tinyint | 1 | 3 | 0 | False | False |
| dbo | MaintenanceComments | Body | nvarchar | 8000 | 0 | 0 | False | False |
| dbo | MaintenanceComments | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | MaintenanceLedgerLinks | LinkId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | MaintenanceLedgerLinks | RequestId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | MaintenanceLedgerLinks | LedgerTransactionId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | MaintenanceLedgerLinks | LinkType | tinyint | 1 | 3 | 0 | False | False |
| dbo | MaintenanceLedgerLinks | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | MaintenanceRecords | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | MaintenanceRecords | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | MaintenanceRecords | Title | nvarchar | -1 | 0 | 0 | False | False |
| dbo | MaintenanceRecords | Description | nvarchar | -1 | 0 | 0 | False | False |
| dbo | MaintenanceRecords | MaintenanceDate | date | 3 | 10 | 0 | False | False |
| dbo | MaintenanceRecords | Cost | decimal | 9 | 18 | 2 | False | False |
| dbo | MaintenanceRecords | Vendor | nvarchar | -1 | 0 | 0 | False | False |
| dbo | MaintenanceRecords | Status | nvarchar | -1 | 0 | 0 | False | False |
| dbo | MaintenanceRecords | CreatedBy | nvarchar | -1 | 0 | 0 | False | False |
| dbo | MaintenanceRecords | CreatedAt | datetime2 | 6 | 19 | 0 | False | False |
| dbo | MaintenanceRecords | AddedToExpense | bit | 1 | 1 | 0 | False | False |
| dbo | MaintenanceRecords | AddedToExpenseAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | MaintenanceRequests | RequestId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | MaintenanceRequests | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | MaintenanceRequests | ApartmentId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | MaintenanceRequests | LegacyMaintenanceId | nvarchar | 256 | 0 | 0 | True | False |
| dbo | MaintenanceRequests | Title | nvarchar | 400 | 0 | 0 | False | False |
| dbo | MaintenanceRequests | Description | nvarchar | 8000 | 0 | 0 | True | False |
| dbo | MaintenanceRequests | Category | nvarchar | 160 | 0 | 0 | True | False |
| dbo | MaintenanceRequests | Status | tinyint | 1 | 3 | 0 | False | False |
| dbo | MaintenanceRequests | Priority | tinyint | 1 | 3 | 0 | False | False |
| dbo | MaintenanceRequests | RequestedByPersonId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | MaintenanceRequests | RequestedByReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | MaintenanceRequests | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | MaintenanceRequests | UpdatedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | MaintenanceRequests | ClosedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | MaintenanceStatusEvents | StatusEventId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | MaintenanceStatusEvents | RequestId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | MaintenanceStatusEvents | FromStatus | tinyint | 1 | 3 | 0 | True | False |
| dbo | MaintenanceStatusEvents | ToStatus | tinyint | 1 | 3 | 0 | False | False |
| dbo | MaintenanceStatusEvents | ActorReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | MaintenanceStatusEvents | Reason | nvarchar | 2000 | 0 | 0 | True | False |
| dbo | MaintenanceStatusEvents | OccurredAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | MediaAccessSessions | MediaAccessSessionId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | MediaAccessSessions | AccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | MediaAccessSessions | CameraLegacyId | nvarchar | 256 | 0 | 0 | True | False |
| dbo | MediaAccessSessions | SessionReference | nvarchar | 400 | 0 | 0 | False | False |
| dbo | MediaAccessSessions | IssuedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | MediaAccessSessions | ExpiresAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | MediaAccessSessions | RevokedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | MediaCredentialReferences | CredentialReferenceId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | MediaCredentialReferences | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | MediaCredentialReferences | DvrLegacyId | nvarchar | 256 | 0 | 0 | True | False |
| dbo | MediaCredentialReferences | VaultProvider | nvarchar | 160 | 0 | 0 | False | False |
| dbo | MediaCredentialReferences | VaultSecretReference | nvarchar | 1000 | 0 | 0 | False | False |
| dbo | MediaCredentialReferences | KeyVersion | nvarchar | 200 | 0 | 0 | False | False |
| dbo | MediaCredentialReferences | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | MediaCredentialReferences | RotatedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | MediaCredentialReferences | RevokedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | MediaProvisioningOperations | OperationId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | MediaProvisioningOperations | CredentialReferenceId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | MediaProvisioningOperations | DvrLegacyId | nvarchar | 256 | 0 | 0 | True | False |
| dbo | MediaProvisioningOperations | OperationType | tinyint | 1 | 3 | 0 | False | False |
| dbo | MediaProvisioningOperations | Status | tinyint | 1 | 3 | 0 | False | False |
| dbo | MediaProvisioningOperations | RequestedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | MediaProvisioningOperations | CompletedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | MediaProvisioningOperations | Attempts | int | 4 | 10 | 0 | False | False |
| dbo | MediaProvisioningOperations | SafeErrorCode | nvarchar | 200 | 0 | 0 | True | False |
| dbo | MonthlyApartmentShares | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | MonthlyApartmentShares | MonthKey | char | 7 | 0 | 0 | False | False |
| dbo | MonthlyApartmentShares | ApartmentId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | MonthlyApartmentShares | ExpenseShare | decimal | 9 | 18 | 2 | False | False |
| dbo | MonthlyApartmentShares | RevenueShare | decimal | 9 | 18 | 2 | False | False |
| dbo | MonthlyApartmentShares | CarryOver | decimal | 9 | 18 | 2 | False | False |
| dbo | NotificationDeliveries | DeliveryId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | NotificationDeliveries | RecipientId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | NotificationDeliveries | AttemptNumber | int | 4 | 10 | 0 | False | False |
| dbo | NotificationDeliveries | Provider | nvarchar | 160 | 0 | 0 | False | False |
| dbo | NotificationDeliveries | Status | tinyint | 1 | 3 | 0 | False | False |
| dbo | NotificationDeliveries | AttemptedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | NotificationDeliveries | CompletedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | NotificationDeliveries | ProviderReference | nvarchar | 400 | 0 | 0 | True | False |
| dbo | NotificationDeliveries | SafeErrorCode | nvarchar | 200 | 0 | 0 | True | False |
| dbo | NotificationMessages | MessageId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | NotificationMessages | BuildingId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | NotificationMessages | LegacyNotificationId | nvarchar | 256 | 0 | 0 | True | False |
| dbo | NotificationMessages | Title | nvarchar | 500 | 0 | 0 | True | False |
| dbo | NotificationMessages | Body | nvarchar | 8000 | 0 | 0 | False | False |
| dbo | NotificationMessages | Channel | tinyint | 1 | 3 | 0 | False | False |
| dbo | NotificationMessages | Origin | nvarchar | 160 | 0 | 0 | False | False |
| dbo | NotificationMessages | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | NotificationMessages | ExpiresAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | NotificationPreferences | PreferenceId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | NotificationPreferences | SubjectType | tinyint | 1 | 3 | 0 | False | False |
| dbo | NotificationPreferences | SubjectReference | nvarchar | 320 | 0 | 0 | False | False |
| dbo | NotificationPreferences | Channel | tinyint | 1 | 3 | 0 | False | False |
| dbo | NotificationPreferences | IsEnabled | bit | 1 | 1 | 0 | False | False |
| dbo | NotificationPreferences | UpdatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | NotificationReadStates | ReadStateId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | NotificationReadStates | RecipientId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | NotificationReadStates | ReadAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | NotificationReadStates | DismissedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | NotificationRecipients | RecipientId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | NotificationRecipients | MessageId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | NotificationRecipients | RecipientType | tinyint | 1 | 3 | 0 | False | False |
| dbo | NotificationRecipients | RecipientReference | nvarchar | 320 | 0 | 0 | False | False |
| dbo | NotificationRecipients | ApartmentId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | NotificationRecipients | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | Notifications | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Notifications | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Notifications | Ts | datetime2 | 6 | 19 | 0 | False | False |
| dbo | Notifications | RecipientType | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Notifications | RecipientApartmentId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | Notifications | Type | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Notifications | Icon | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Notifications | Title | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Notifications | Body | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Notifications | IsRead | bit | 1 | 1 | 0 | False | False |
| dbo | Notifications | ReadAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | OutboxMessages | OutboxMessageId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | OutboxMessages | MessageType | nvarchar | 320 | 0 | 0 | False | False |
| dbo | OutboxMessages | AggregateType | nvarchar | 240 | 0 | 0 | False | False |
| dbo | OutboxMessages | AggregateReference | nvarchar | 400 | 0 | 0 | False | False |
| dbo | OutboxMessages | PayloadJson | nvarchar | -1 | 0 | 0 | False | False |
| dbo | OutboxMessages | IdempotencyKey | nvarchar | 400 | 0 | 0 | False | False |
| dbo | OutboxMessages | OccurredAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | OutboxMessages | AvailableAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | OutboxMessages | ProcessedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | OutboxMessages | Attempts | int | 4 | 10 | 0 | False | False |
| dbo | OutboxMessages | LastSafeErrorCode | nvarchar | 200 | 0 | 0 | True | False |
| dbo | PaymentDeposits | PaymentDepositId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | PaymentDeposits | PeriodId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | PaymentDeposits | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PaymentDeposits | ApartmentId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PaymentDeposits | PostedTransactionId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | PaymentDeposits | Amount | decimal | 9 | 19 | 4 | False | False |
| dbo | PaymentDeposits | CurrencyCode | char | 3 | 0 | 0 | False | False |
| dbo | PaymentDeposits | Status | tinyint | 1 | 3 | 0 | False | False |
| dbo | PaymentDeposits | SubmittedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | PaymentDeposits | ConfirmedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | PaymentDeposits | ConfirmedByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | PaymentDeposits | CancelledAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | PaymentDeposits | CancelledByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | PaymentDeposits | ReversedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | PaymentDeposits | ReversedByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | PaymentDeposits | ReversalTransactionId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | PaymentDeposits | ResolutionReason | nvarchar | 1000 | 0 | 0 | True | False |
| dbo | PaymentDeposits | SourceSystem | nvarchar | 160 | 0 | 0 | False | False |
| dbo | PaymentDeposits | SourceKey | nvarchar | 512 | 0 | 0 | True | False |
| dbo | PaymentDeposits | RowVersion | timestamp | 8 | 0 | 0 | False | False |
| dbo | Persons | PersonId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | Persons | LegalName | nvarchar | 400 | 0 | 0 | True | False |
| dbo | Persons | DisplayName | nvarchar | 320 | 0 | 0 | False | False |
| dbo | Persons | PrimaryEmail | nvarchar | 508 | 0 | 0 | True | False |
| dbo | Persons | PrimaryPhone | nvarchar | 64 | 0 | 0 | True | False |
| dbo | Persons | Whatsapp | nvarchar | 64 | 0 | 0 | True | False |
| dbo | Persons | PhotoUrl | nvarchar | 4096 | 0 | 0 | True | False |
| dbo | Persons | IsArchived | bit | 1 | 1 | 0 | False | False |
| dbo | Persons | ArchivedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | Persons | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | Persons | UpdatedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | Persons | RowVersion | timestamp | 8 | 0 | 0 | False | False |
| dbo | PollBallots | BallotId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | PollBallots | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollBallots | PollId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollBallots | BallotTokenHash | varbinary | 64 | 0 | 0 | False | False |
| dbo | PollBallots | ChoicePayload | varbinary | -1 | 0 | 0 | False | False |
| dbo | PollBallots | CastAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | PollBallots | BallotVersion | int | 4 | 10 | 0 | False | False |
| dbo | PollDelegations | DelegationId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | PollDelegations | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollDelegations | PollId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollDelegations | ApartmentId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollDelegations | DelegatePersonId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | PollDelegations | GrantedByPersonId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | PollDelegations | ValidFromUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | PollDelegations | ValidToUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | PollDelegations | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | PollEligibility | EligibilityId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | PollEligibility | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollEligibility | PollId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollEligibility | SubjectReference | nvarchar | 320 | 0 | 0 | False | False |
| dbo | PollEligibility | CredentialHash | varbinary | 64 | 0 | 0 | True | False |
| dbo | PollEligibility | IssuedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | PollEligibility | ExpiresAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | PollEligibility | ConsumedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | PollEligibility | RevokedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | PollLifecycleEvents | EventId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | PollLifecycleEvents | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollLifecycleEvents | PollId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollLifecycleEvents | EventType | tinyint | 1 | 3 | 0 | False | False |
| dbo | PollLifecycleEvents | OccurredAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | PollLifecycleEvents | ActorReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | PollLifecycleEvents | Reason | nvarchar | 2000 | 0 | 0 | True | False |
| dbo | PollLifecycleEvents | MetadataJson | nvarchar | -1 | 0 | 0 | True | False |
| dbo | PollOptions | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollOptions | PollId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollOptions | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollOptions | Text | nvarchar | -1 | 0 | 0 | False | False |
| dbo | PollOptions | SortOrder | int | 4 | 10 | 0 | False | False |
| dbo | Polls | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Polls | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Polls | Question | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Polls | Description | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Polls | CreatedBy | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Polls | CreatedAt | datetime2 | 6 | 19 | 0 | False | False |
| dbo | Polls | Deadline | datetime2 | 6 | 19 | 0 | True | False |
| dbo | Polls | IsClosed | bit | 1 | 1 | 0 | False | False |
| dbo | PollVotes | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollVotes | PollId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollVotes | ApartmentId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollVotes | OptionId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollVotes | VotedAt | datetime2 | 6 | 19 | 0 | False | False |
| dbo | PollVotingPolicies | PolicyId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | PollVotingPolicies | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollVotingPolicies | PollId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | PollVotingPolicies | PolicyVersion | int | 4 | 10 | 0 | False | False |
| dbo | PollVotingPolicies | VotingMode | tinyint | 1 | 3 | 0 | False | False |
| dbo | PollVotingPolicies | EligibilityMode | tinyint | 1 | 3 | 0 | False | False |
| dbo | PollVotingPolicies | IsActive | bit | 1 | 1 | 0 | False | False |
| dbo | PollVotingPolicies | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | Presence | Uid | nvarchar | 256 | 0 | 0 | False | False |
| dbo | Presence | BuildingId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | Presence | ApartmentId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | Presence | LastSeen | datetime2 | 7 | 23 | 3 | False | False |
| dbo | PresenceHeartbeats | HeartbeatId | bigint | 8 | 19 | 0 | False | True |
| dbo | PresenceHeartbeats | PresenceSessionId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | PresenceHeartbeats | ObservedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | PresenceHeartbeats | Signal | tinyint | 1 | 3 | 0 | False | False |
| dbo | PresenceSessions | PresenceSessionId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | PresenceSessions | AccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | PresenceSessions | LegacyUid | nvarchar | 256 | 0 | 0 | True | False |
| dbo | PresenceSessions | BuildingId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | PresenceSessions | ApartmentId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | PresenceSessions | DeviceReference | nvarchar | 400 | 0 | 0 | True | False |
| dbo | PresenceSessions | StartedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | PresenceSessions | LastSeenAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | PresenceSessions | ExpiresAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | PresenceSessions | EndedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | PresenceSessions | Status | tinyint | 1 | 3 | 0 | False | False |
| dbo | ProtectedIntegrationSecrets | SecretId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | ProtectedIntegrationSecrets | ScopeType | tinyint | 1 | 3 | 0 | False | False |
| dbo | ProtectedIntegrationSecrets | ScopeReference | nvarchar | 320 | 0 | 0 | False | False |
| dbo | ProtectedIntegrationSecrets | Provider | nvarchar | 200 | 0 | 0 | False | False |
| dbo | ProtectedIntegrationSecrets | SecretName | nvarchar | 320 | 0 | 0 | False | False |
| dbo | ProtectedIntegrationSecrets | Ciphertext | varbinary | -1 | 0 | 0 | False | False |
| dbo | ProtectedIntegrationSecrets | KeyVersion | nvarchar | 200 | 0 | 0 | False | False |
| dbo | ProtectedIntegrationSecrets | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | ProtectedIntegrationSecrets | RotatedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | ProtectedIntegrationSecrets | RevokedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | QrCapabilities | CapabilityId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | QrCapabilities | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | QrCapabilities | ApartmentId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | QrCapabilities | TargetType | tinyint | 1 | 3 | 0 | False | False |
| dbo | QrCapabilities | TargetReference | nvarchar | 320 | 0 | 0 | False | False |
| dbo | QrCapabilities | TokenHash | varbinary | 64 | 0 | 0 | False | False |
| dbo | QrCapabilities | IssuedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | QrCapabilities | ExpiresAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | QrCapabilities | MaxUses | int | 4 | 10 | 0 | False | False |
| dbo | QrCapabilities | RevokedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | QrCapabilities | RevokedByReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | QrCapabilityUses | UseId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | QrCapabilityUses | CapabilityId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | QrCapabilityUses | UsedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | QrCapabilityUses | UseReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | QrCapabilityUses | Outcome | tinyint | 1 | 3 | 0 | False | False |
| dbo | QrTokens | Uid | nvarchar | 256 | 0 | 0 | False | False |
| dbo | QrTokens | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | QrTokens | ApartmentId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | QrTokens | ExpiresAt | datetime2 | 7 | 23 | 3 | False | False |
| dbo | QrTokens | UseType | nvarchar | -1 | 0 | 0 | False | False |
| dbo | QrTokens | IssuedAt | datetime2 | 7 | 23 | 3 | False | False |
| dbo | QrTokens | CreatedBy | nvarchar | -1 | 0 | 0 | False | False |
| dbo | QrTokens | IsUsed | bit | 1 | 1 | 0 | False | False |
| dbo | QrTokens | UsedAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | QrTokens | UsedBy | nvarchar | -1 | 0 | 0 | True | False |
| dbo | QrTokens | CreatedAt | datetime2 | 6 | 19 | 0 | False | False |
| dbo | QrUsages | Id | nvarchar | 256 | 0 | 0 | False | False |
| dbo | QrUsages | TokenId | nvarchar | 256 | 0 | 0 | False | False |
| dbo | QrUsages | UseCount | int | 4 | 10 | 0 | False | False |
| dbo | QrUsages | FirstUsedAt | nvarchar | -1 | 0 | 0 | False | False |
| dbo | QrUsages | LastUsedAt | nvarchar | -1 | 0 | 0 | False | False |
| dbo | QrUsages | DeviceFingerprint | nvarchar | -1 | 0 | 0 | True | False |
| dbo | QrUsages | CreatedAt | datetime2 | 6 | 19 | 0 | False | False |
| dbo | Residents | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Residents | Uid | nvarchar | 256 | 0 | 0 | False | False |
| dbo | Residents | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Residents | ApartmentId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Residents | Name | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Residents | Phone | nvarchar | 900 | 0 | 0 | False | False |
| dbo | Residents | Whatsapp | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Residents | Email | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Residents | PhotoUrl | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Residents | PinHash | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Residents | PasswordHash | nvarchar | -1 | 0 | 0 | True | False |
| dbo | Residents | IsActive | bit | 1 | 1 | 0 | False | False |
| dbo | Residents | IsDisabled | bit | 1 | 1 | 0 | False | False |
| dbo | Residents | DisabledReason | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Residents | IsOwner | bit | 1 | 1 | 0 | False | False |
| dbo | Residents | IsPrimary | bit | 1 | 1 | 0 | False | False |
| dbo | Residents | LastLoginAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | Residents | CreatedAt | datetime2 | 6 | 19 | 0 | False | False |
| dbo | Residents | CreatedBy | nvarchar | -1 | 0 | 0 | True | False |
| dbo | Residents | UpdatedAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | Residents | UpdatedBy | nvarchar | -1 | 0 | 0 | True | False |
| dbo | RestoreOperations | RestoreOperationId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | RestoreOperations | BackupArtifactId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | RestoreOperations | TargetDatabase | nvarchar | 400 | 0 | 0 | False | False |
| dbo | RestoreOperations | Status | tinyint | 1 | 3 | 0 | False | False |
| dbo | RestoreOperations | RequestedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | RestoreOperations | ApprovedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | RestoreOperations | ApprovedByReference | nvarchar | 320 | 0 | 0 | True | False |
| dbo | RestoreOperations | StartedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | RestoreOperations | CompletedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | RestoreOperations | SafeErrorCode | nvarchar | 200 | 0 | 0 | True | False |
| dbo | Revenues | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Revenues | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | Revenues | MonthKey | char | 7 | 0 | 0 | False | False |
| dbo | Revenues | CategoryId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | Revenues | CategoryKind | nvarchar | 20 | 0 | 0 | False | False |
| dbo | Revenues | Note | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Revenues | Amount | decimal | 9 | 18 | 2 | False | False |
| dbo | Revenues | RevenueDate | date | 3 | 10 | 0 | False | False |
| dbo | RoleDefinitions | RoleKey | nvarchar | 128 | 0 | 0 | False | False |
| dbo | RoleDefinitions | DisplayName | nvarchar | 240 | 0 | 0 | False | False |
| dbo | RoleDefinitions | IsSystemRole | bit | 1 | 1 | 0 | False | False |
| dbo | RoleDefinitions | IsActive | bit | 1 | 1 | 0 | False | False |
| dbo | RolePermissions | RoleKey | nvarchar | 128 | 0 | 0 | False | False |
| dbo | RolePermissions | PermissionKey | nvarchar | 200 | 0 | 0 | False | False |
| dbo | RolePermissions | Effect | tinyint | 1 | 3 | 0 | False | False |
| dbo | ScheduledBackupSettings | Id | nvarchar | 40 | 0 | 0 | False | False |
| dbo | ScheduledBackupSettings | Enabled | bit | 1 | 1 | 0 | False | False |
| dbo | ScheduledBackupSettings | Frequency | nvarchar | -1 | 0 | 0 | False | False |
| dbo | ScheduledBackupSettings | Hour | int | 4 | 10 | 0 | False | False |
| dbo | ScheduledBackupSettings | DayOfWeek | int | 4 | 10 | 0 | True | False |
| dbo | ScheduledBackupSettings | DayOfMonth | int | 4 | 10 | 0 | True | False |
| dbo | ScheduledBackupSettings | CollectionsJson | nvarchar | -1 | 0 | 0 | False | False |
| dbo | ScheduledBackupSettings | MaxBackupsToKeep | int | 4 | 10 | 0 | False | False |
| dbo | ScheduledBackupSettings | LastRunAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | ScheduledBackupSettings | LastRunStatus | nvarchar | -1 | 0 | 0 | True | False |
| dbo | ScheduledBackupSettings | UpdatedAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | ScheduledBackupSettings | UpdatedBy | nvarchar | -1 | 0 | 0 | True | False |
| dbo | SyncPendingChanges | Id | bigint | 8 | 19 | 0 | False | True |
| dbo | SyncPendingChanges | EntityType | nvarchar | 900 | 0 | 0 | False | False |
| dbo | SyncPendingChanges | EntityId | nvarchar | 900 | 0 | 0 | False | False |
| dbo | SyncPendingChanges | Operation | nvarchar | -1 | 0 | 0 | False | False |
| dbo | SyncPendingChanges | Payload | nvarchar | -1 | 0 | 0 | True | False |
| dbo | SyncPendingChanges | CreatedAt | datetime2 | 6 | 19 | 0 | False | False |
| dbo | SyncPendingChanges | Applied | bit | 1 | 1 | 0 | False | False |
| dbo | SyncPendingChanges | AppliedAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | SyncPendingChanges | RetryCount | int | 4 | 10 | 0 | False | False |
| dbo | SyncPendingChanges | ErrorMessage | nvarchar | -1 | 0 | 0 | True | False |
| dbo | UserAccounts | AccountId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | UserAccounts | PersonId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | UserAccounts | AccountStatus | tinyint | 1 | 3 | 0 | False | False |
| dbo | UserAccounts | NormalizedEmail | nvarchar | 508 | 0 | 0 | True | False |
| dbo | UserAccounts | ExternalProvider | nvarchar | 80 | 0 | 0 | True | False |
| dbo | UserAccounts | ExternalSubject | nvarchar | 400 | 0 | 0 | True | False |
| dbo | UserAccounts | SecurityStamp | nvarchar | 256 | 0 | 0 | False | False |
| dbo | UserAccounts | LastLoginAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | UserAccounts | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | UserAccounts | RowVersion | timestamp | 8 | 0 | 0 | False | False |
| dbo | UserCredentials | CredentialId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | UserCredentials | AccountId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | UserCredentials | CredentialType | tinyint | 1 | 3 | 0 | False | False |
| dbo | UserCredentials | CredentialHash | nvarchar | 1024 | 0 | 0 | False | False |
| dbo | UserCredentials | HashAlgorithm | nvarchar | 80 | 0 | 0 | False | False |
| dbo | UserCredentials | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | UserCredentials | ExpiresAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | UserCredentials | RevokedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | UserCredentials | ReplacedByCredentialId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | UserCredentials | RowVersion | timestamp | 8 | 0 | 0 | False | False |
| dbo | UserPermissions | Uid | nvarchar | 256 | 0 | 0 | False | False |
| dbo | UserPermissions | Permission | nvarchar | 120 | 0 | 0 | False | False |
| dbo | UserPreferences | PreferenceId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | UserPreferences | AccountId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | UserPreferences | PreferenceKey | nvarchar | 240 | 0 | 0 | False | False |
| dbo | UserPreferences | PreferenceJson | nvarchar | 4000 | 0 | 0 | False | False |
| dbo | UserPreferences | UpdatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | UserRecoveryCodes | RecoveryCodeId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | UserRecoveryCodes | AccountId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | UserRecoveryCodes | CodeHash | varbinary | 64 | 0 | 0 | False | False |
| dbo | UserRecoveryCodes | CreatedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | UserRecoveryCodes | UsedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | UserRoleAssignments | RoleAssignmentId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | UserRoleAssignments | AccountId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | UserRoleAssignments | RoleKey | nvarchar | 128 | 0 | 0 | False | False |
| dbo | UserRoleAssignments | ScopeType | tinyint | 1 | 3 | 0 | False | False |
| dbo | UserRoleAssignments | ScopeBuildingId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | UserRoleAssignments | ScopeApartmentId | nvarchar | 128 | 0 | 0 | True | False |
| dbo | UserRoleAssignments | ValidFromUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | UserRoleAssignments | ValidToUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | UserRoleAssignments | GrantedAtUtc | datetime2 | 7 | 23 | 3 | False | False |
| dbo | UserRoleAssignments | GrantedByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | UserRoleAssignments | GrantReason | nvarchar | 1000 | 0 | 0 | True | False |
| dbo | UserRoleAssignments | RevokedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | UserRoleAssignments | RevokedByAccountId | uniqueidentifier | 16 | 0 | 0 | True | False |
| dbo | UserRoleAssignments | RevocationReason | nvarchar | 1000 | 0 | 0 | True | False |
| dbo | UserRoleAssignments | RowVersion | timestamp | 8 | 0 | 0 | False | False |
| dbo | Users | Uid | nvarchar | 256 | 0 | 0 | False | False |
| dbo | Users | Email | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Users | Name | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Users | Phone | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Users | Whatsapp | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Users | Pin | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Users | PhotoUrl | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Users | Role | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Users | IsDisabled | bit | 1 | 1 | 0 | False | False |
| dbo | Users | DisabledReason | nvarchar | -1 | 0 | 0 | False | False |
| dbo | Users | IsActive | bit | 1 | 1 | 0 | False | False |
| dbo | Users | LastLoginAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | Users | CreatedAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | Users | CreatedBy | nvarchar | -1 | 0 | 0 | True | False |
| dbo | Users | UpdatedAt | datetime2 | 6 | 19 | 0 | True | False |
| dbo | Users | UpdatedBy | nvarchar | -1 | 0 | 0 | True | False |
| dbo | Users | TwoFactorEnabled | bit | 1 | 1 | 0 | False | False |
| dbo | Users | TwoFactorSecretEncrypted | nvarchar | 1024 | 0 | 0 | True | False |
| dbo | Users | TwoFactorRecoveryCodesJson | nvarchar | 8000 | 0 | 0 | True | False |
| dbo | Users | TwoFactorLastUsedStep | bigint | 8 | 19 | 0 | True | False |
| dbo | UserTwoFactorSettings | AccountId | uniqueidentifier | 16 | 0 | 0 | False | False |
| dbo | UserTwoFactorSettings | IsEnabled | bit | 1 | 1 | 0 | False | False |
| dbo | UserTwoFactorSettings | SecretCiphertext | varbinary | -1 | 0 | 0 | True | False |
| dbo | UserTwoFactorSettings | KeyVersion | nvarchar | 128 | 0 | 0 | True | False |
| dbo | UserTwoFactorSettings | LastAcceptedTimeStep | bigint | 8 | 19 | 0 | True | False |
| dbo | UserTwoFactorSettings | EnrolledAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | UserTwoFactorSettings | LastVerifiedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | UserTwoFactorSettings | UpdatedAtUtc | datetime2 | 7 | 23 | 3 | True | False |
| dbo | UserTwoFactorSettings | RowVersion | timestamp | 8 | 0 | 0 | False | False |
| dbo | WalletAdjustments | BuildingId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | WalletAdjustments | Id | nvarchar | 128 | 0 | 0 | False | False |
| dbo | WalletAdjustments | ApartmentId | nvarchar | 128 | 0 | 0 | False | False |
| dbo | WalletAdjustments | Amount | decimal | 9 | 18 | 2 | False | False |
| dbo | WalletAdjustments | Reason | nvarchar | -1 | 0 | 0 | False | False |
| dbo | WalletAdjustments | MonthKey | char | 7 | 0 | 0 | False | False |
| dbo | WalletAdjustments | CreatedAt | datetime2 | 6 | 19 | 0 | False | False |
| dbo | WalletAdjustments | CreatedBy | nvarchar | -1 | 0 | 0 | False | False |

## Foreign-key inventory

| Parent | Parent column | Referenced table | Referenced column | Constraint |
|---|---|---|---|---|
| dbo.AccountPermissionOverrides | ScopeBuildingId | dbo.Apartments | BuildingId | FK_AccountPermissionOverrides_Apartments_ScopeBuildingId_ScopeApartmentId |
| dbo.AccountPermissionOverrides | ScopeApartmentId | dbo.Apartments | Id | FK_AccountPermissionOverrides_Apartments_ScopeBuildingId_ScopeApartmentId |
| dbo.AccountPermissionOverrides | ScopeBuildingId | dbo.Buildings | Id | FK_AccountPermissionOverrides_Buildings_ScopeBuildingId |
| dbo.AccountPermissionOverrides | AccountId | dbo.UserAccounts | AccountId | FK_AccountPermissionOverrides_UserAccounts_AccountId |
| dbo.AccountPermissionOverrides | GrantedByAccountId | dbo.UserAccounts | AccountId | FK_AccountPermissionOverrides_UserAccounts_GrantedByAccountId |
| dbo.AccountPermissionOverrides | RevokedByAccountId | dbo.UserAccounts | AccountId | FK_AccountPermissionOverrides_UserAccounts_RevokedByAccountId |
| dbo.ApartmentAccessGrants | BuildingId | dbo.Apartments | BuildingId | FK_ApartmentAccessGrants_Apartments_BuildingId_ApartmentId |
| dbo.ApartmentAccessGrants | ApartmentId | dbo.Apartments | Id | FK_ApartmentAccessGrants_Apartments_BuildingId_ApartmentId |
| dbo.ApartmentAccessGrants | PersonId | dbo.Persons | PersonId | FK_ApartmentAccessGrants_Persons_PersonId |
| dbo.ApartmentAccessGrants | GrantedByAccountId | dbo.UserAccounts | AccountId | FK_ApartmentAccessGrants_UserAccounts_GrantedByAccountId |
| dbo.ApartmentAccessGrants | RevokedByAccountId | dbo.UserAccounts | AccountId | FK_ApartmentAccessGrants_UserAccounts_RevokedByAccountId |
| dbo.ApartmentOccupancies | BuildingId | dbo.Apartments | BuildingId | FK_ApartmentOccupancies_Apartments_BuildingId_ApartmentId |
| dbo.ApartmentOccupancies | ApartmentId | dbo.Apartments | Id | FK_ApartmentOccupancies_Apartments_BuildingId_ApartmentId |
| dbo.ApartmentOccupancies | PersonId | dbo.Persons | PersonId | FK_ApartmentOccupancies_Persons_PersonId |
| dbo.ApartmentOccupancies | RecordedByAccountId | dbo.UserAccounts | AccountId | FK_ApartmentOccupancies_UserAccounts_RecordedByAccountId |
| dbo.ApartmentOwnerships | BuildingId | dbo.Apartments | BuildingId | FK_ApartmentOwnerships_Apartments_BuildingId_ApartmentId |
| dbo.ApartmentOwnerships | ApartmentId | dbo.Apartments | Id | FK_ApartmentOwnerships_Apartments_BuildingId_ApartmentId |
| dbo.ApartmentOwnerships | PersonId | dbo.Persons | PersonId | FK_ApartmentOwnerships_Persons_PersonId |
| dbo.ApartmentOwnerships | CompletenessVerifiedByAccountId | dbo.UserAccounts | AccountId | FK_ApartmentOwnerships_UserAccounts_CompletenessVerifiedByAccountId |
| dbo.ApartmentOwnerships | RecordedByAccountId | dbo.UserAccounts | AccountId | FK_ApartmentOwnerships_UserAccounts_RecordedByAccountId |
| dbo.Apartments | BuildingId | dbo.Buildings | Id | FK_Apartments_Buildings_BuildingId |
| dbo.Apartments | BuildingId | dbo.Floors | BuildingId | FK_Apartments_Floors_BuildingId_FloorId |
| dbo.Apartments | FloorId | dbo.Floors | Id | FK_Apartments_Floors_BuildingId_FloorId |
| dbo.AuditLog | BuildingId | dbo.Buildings | Id | FK_AuditLog_Buildings_BuildingId |
| dbo.BackupRetentionEvents | BackupArtifactId | dbo.BackupArtifacts | BackupArtifactId | FK_BackupRetentionEvents_Artifact |
| dbo.BuildingAdmins | BuildingId | dbo.Buildings | Id | FK_BuildingAdmins_Buildings_BuildingId |
| dbo.BuildingAdmins | AdminUid | dbo.Users | Uid | FK_BuildingAdmins_Users_AdminUid |
| dbo.BuildingSettings | BuildingId | dbo.Buildings | Id | FK_BuildingSettings_Buildings_BuildingId |
| dbo.Cameras | DvrId | dbo.Dvrs | Id | FK_Cameras_Dvrs_DvrId |
| dbo.DepositEditHistory | BuildingId | dbo.Deposits | BuildingId | FK_DepositEditHistory_Deposits_BuildingId_DepositId |
| dbo.DepositEditHistory | DepositId | dbo.Deposits | Id | FK_DepositEditHistory_Deposits_BuildingId_DepositId |
| dbo.Deposits | BuildingId | dbo.Apartments | BuildingId | FK_Deposits_Apartments_BuildingId_ApartmentId |
| dbo.Deposits | ApartmentId | dbo.Apartments | Id | FK_Deposits_Apartments_BuildingId_ApartmentId |
| dbo.Deposits | BuildingId | dbo.Buildings | Id | FK_Deposits_Buildings_BuildingId |
| dbo.DocumentAccessGrants | DocumentId | dbo.DocumentAssets | DocumentId | FK_DocumentAccessGrants_Document |
| dbo.Dvrs | BuildingId | dbo.Buildings | Id | FK_Dvrs_Buildings_BuildingId |
| dbo.Expenses | BuildingId | dbo.Buildings | Id | FK_Expenses_Buildings_BuildingId |
| dbo.Expenses | BuildingId | dbo.FinancialCategories | BuildingId | FK_Expenses_FinancialCategories_BuildingId_CategoryKind_CategoryId |
| dbo.Expenses | CategoryKind | dbo.FinancialCategories | Kind | FK_Expenses_FinancialCategories_BuildingId_CategoryKind_CategoryId |
| dbo.Expenses | CategoryId | dbo.FinancialCategories | Id | FK_Expenses_FinancialCategories_BuildingId_CategoryKind_CategoryId |
| dbo.FileProcessingJobs | DocumentId | dbo.DocumentAssets | DocumentId | FK_FileProcessingJobs_Document |
| dbo.FinancialAllocationBatches | PeriodId | dbo.FinancialPeriods | PeriodId | FK_FinancialAllocationBatches_FinancialPeriods_PeriodId_BuildingId |
| dbo.FinancialAllocationBatches | BuildingId | dbo.FinancialPeriods | BuildingId | FK_FinancialAllocationBatches_FinancialPeriods_PeriodId_BuildingId |
| dbo.FinancialAllocationBatches | SourceTransactionId | dbo.LedgerTransactions | LedgerTransactionId | FK_FinancialAllocationBatches_LedgerTransactions_SourceTransactionId_PeriodId_BuildingId |
| dbo.FinancialAllocationBatches | PeriodId | dbo.LedgerTransactions | PeriodId | FK_FinancialAllocationBatches_LedgerTransactions_SourceTransactionId_PeriodId_BuildingId |
| dbo.FinancialAllocationBatches | BuildingId | dbo.LedgerTransactions | BuildingId | FK_FinancialAllocationBatches_LedgerTransactions_SourceTransactionId_PeriodId_BuildingId |
| dbo.FinancialAllocationBatches | CreatedByAccountId | dbo.UserAccounts | AccountId | FK_FinancialAllocationBatches_UserAccounts_CreatedByAccountId |
| dbo.FinancialAllocationLines | BuildingId | dbo.Apartments | BuildingId | FK_FinancialAllocationLines_Apartments_BuildingId_ApartmentId |
| dbo.FinancialAllocationLines | ApartmentId | dbo.Apartments | Id | FK_FinancialAllocationLines_Apartments_BuildingId_ApartmentId |
| dbo.FinancialAllocationLines | AllocationBatchId | dbo.FinancialAllocationBatches | AllocationBatchId | FK_FinancialAllocationLines_FinancialAllocationBatches_AllocationBatchId_PeriodId_BuildingId |
| dbo.FinancialAllocationLines | PeriodId | dbo.FinancialAllocationBatches | PeriodId | FK_FinancialAllocationLines_FinancialAllocationBatches_AllocationBatchId_PeriodId_BuildingId |
| dbo.FinancialAllocationLines | BuildingId | dbo.FinancialAllocationBatches | BuildingId | FK_FinancialAllocationLines_FinancialAllocationBatches_AllocationBatchId_PeriodId_BuildingId |
| dbo.FinancialAllocationLines | PeriodId | dbo.FinancialPeriods | PeriodId | FK_FinancialAllocationLines_FinancialPeriods_PeriodId_BuildingId |
| dbo.FinancialAllocationLines | BuildingId | dbo.FinancialPeriods | BuildingId | FK_FinancialAllocationLines_FinancialPeriods_PeriodId_BuildingId |
| dbo.FinancialCategories | BuildingId | dbo.Buildings | Id | FK_FinancialCategories_Buildings_BuildingId |
| dbo.FinancialPeriods | BuildingId | dbo.Buildings | Id | FK_FinancialPeriods_Buildings_BuildingId |
| dbo.FinancialPeriods | ClosedByAccountId | dbo.UserAccounts | AccountId | FK_FinancialPeriods_UserAccounts_ClosedByAccountId |
| dbo.FinancialReconciliationDifferences | BuildingId | dbo.Apartments | BuildingId | FK_FinancialReconciliationDifferences_Apartments_BuildingId_ApartmentId |
| dbo.FinancialReconciliationDifferences | ApartmentId | dbo.Apartments | Id | FK_FinancialReconciliationDifferences_Apartments_BuildingId_ApartmentId |
| dbo.FinancialReconciliationDifferences | BuildingId | dbo.Buildings | Id | FK_FinancialReconciliationDifferences_Buildings_BuildingId |
| dbo.FinancialReconciliationDifferences | ReconciliationRunId | dbo.FinancialReconciliationRuns | ReconciliationRunId | FK_FinancialReconciliationDifferences_FinancialReconciliationRuns_ReconciliationRunId |
| dbo.FinancialReconciliationDifferences | ReviewedByAccountId | dbo.UserAccounts | AccountId | FK_FinancialReconciliationDifferences_UserAccounts_ReviewedByAccountId |
| dbo.FinancialReconciliationRuns | StartedByAccountId | dbo.UserAccounts | AccountId | FK_FinancialReconciliationRuns_UserAccounts_StartedByAccountId |
| dbo.FinancialSourceLinks | LedgerTransactionId | dbo.LedgerTransactions | LedgerTransactionId | FK_FinancialSourceLinks_LedgerTransactions_LedgerTransactionId |
| dbo.Floors | BuildingId | dbo.Buildings | Id | FK_Floors_Buildings_BuildingId |
| dbo.GuestAccessSessions | CapabilityId | dbo.QrCapabilities | CapabilityId | FK_GuestAccessSessions_Capability |
| dbo.ImportRows | ImportBatchId | dbo.ImportBatches | ImportBatchId | FK_ImportRows_Batch |
| dbo.LedgerEntries | BuildingId | dbo.Apartments | BuildingId | FK_LedgerEntries_Apartments_BuildingId_ApartmentId |
| dbo.LedgerEntries | ApartmentId | dbo.Apartments | Id | FK_LedgerEntries_Apartments_BuildingId_ApartmentId |
| dbo.LedgerEntries | PeriodId | dbo.FinancialPeriods | PeriodId | FK_LedgerEntries_FinancialPeriods_PeriodId_BuildingId |
| dbo.LedgerEntries | BuildingId | dbo.FinancialPeriods | BuildingId | FK_LedgerEntries_FinancialPeriods_PeriodId_BuildingId |
| dbo.LedgerEntries | LedgerTransactionId | dbo.LedgerTransactions | LedgerTransactionId | FK_LedgerEntries_LedgerTransactions_LedgerTransactionId_PeriodId_BuildingId |
| dbo.LedgerEntries | PeriodId | dbo.LedgerTransactions | PeriodId | FK_LedgerEntries_LedgerTransactions_LedgerTransactionId_PeriodId_BuildingId |
| dbo.LedgerEntries | BuildingId | dbo.LedgerTransactions | BuildingId | FK_LedgerEntries_LedgerTransactions_LedgerTransactionId_PeriodId_BuildingId |
| dbo.LedgerTransactions | PeriodId | dbo.FinancialPeriods | PeriodId | FK_LedgerTransactions_FinancialPeriods_PeriodId_BuildingId |
| dbo.LedgerTransactions | BuildingId | dbo.FinancialPeriods | BuildingId | FK_LedgerTransactions_FinancialPeriods_PeriodId_BuildingId |
| dbo.LedgerTransactions | ReversesTransactionId | dbo.LedgerTransactions | LedgerTransactionId | FK_LedgerTransactions_LedgerTransactions_ReversesTransactionId_PeriodId_BuildingId |
| dbo.LedgerTransactions | PeriodId | dbo.LedgerTransactions | PeriodId | FK_LedgerTransactions_LedgerTransactions_ReversesTransactionId_PeriodId_BuildingId |
| dbo.LedgerTransactions | BuildingId | dbo.LedgerTransactions | BuildingId | FK_LedgerTransactions_LedgerTransactions_ReversesTransactionId_PeriodId_BuildingId |
| dbo.LedgerTransactions | ActorAccountId | dbo.UserAccounts | AccountId | FK_LedgerTransactions_UserAccounts_ActorAccountId |
| dbo.LegacyIdentityMap | PersonId | dbo.Persons | PersonId | FK_LegacyIdentityMap_Persons_PersonId |
| dbo.LegacyIdentityMap | AccountId | dbo.UserAccounts | AccountId | FK_LegacyIdentityMap_UserAccounts_AccountId |
| dbo.LegacyIdentityMap | ReviewedByAccountId | dbo.UserAccounts | AccountId | FK_LegacyIdentityMap_UserAccounts_ReviewedByAccountId |
| dbo.MaintenanceAssignments | RequestId | dbo.MaintenanceRequests | RequestId | FK_MaintenanceAssignments_Request |
| dbo.MaintenanceAttachments | RequestId | dbo.MaintenanceRequests | RequestId | FK_MaintenanceAttachments_Request |
| dbo.MaintenanceComments | RequestId | dbo.MaintenanceRequests | RequestId | FK_MaintenanceComments_Request |
| dbo.MaintenanceLedgerLinks | RequestId | dbo.MaintenanceRequests | RequestId | FK_MaintenanceLedgerLinks_Request |
| dbo.MaintenanceRecords | BuildingId | dbo.Buildings | Id | FK_MaintenanceRecords_Buildings_BuildingId |
| dbo.MaintenanceStatusEvents | RequestId | dbo.MaintenanceRequests | RequestId | FK_MaintenanceStatusEvents_Request |
| dbo.MediaProvisioningOperations | CredentialReferenceId | dbo.MediaCredentialReferences | CredentialReferenceId | FK_MediaProvisioningOperations_Credential |
| dbo.MonthlyApartmentShares | BuildingId | dbo.Apartments | BuildingId | FK_MonthlyApartmentShares_Apartments_BuildingId_ApartmentId |
| dbo.MonthlyApartmentShares | ApartmentId | dbo.Apartments | Id | FK_MonthlyApartmentShares_Apartments_BuildingId_ApartmentId |
| dbo.MonthlyApartmentShares | BuildingId | dbo.Buildings | Id | FK_MonthlyApartmentShares_Buildings_BuildingId |
| dbo.NotificationDeliveries | RecipientId | dbo.NotificationRecipients | RecipientId | FK_NotificationDeliveries_Recipient |
| dbo.NotificationReadStates | RecipientId | dbo.NotificationRecipients | RecipientId | FK_NotificationReadStates_Recipient |
| dbo.NotificationRecipients | MessageId | dbo.NotificationMessages | MessageId | FK_NotificationRecipients_Message |
| dbo.Notifications | BuildingId | dbo.Apartments | BuildingId | FK_Notifications_Apartments_BuildingId_RecipientApartmentId |
| dbo.Notifications | RecipientApartmentId | dbo.Apartments | Id | FK_Notifications_Apartments_BuildingId_RecipientApartmentId |
| dbo.Notifications | BuildingId | dbo.Buildings | Id | FK_Notifications_Buildings_BuildingId |
| dbo.PaymentDeposits | BuildingId | dbo.Apartments | BuildingId | FK_PaymentDeposits_Apartments_BuildingId_ApartmentId |
| dbo.PaymentDeposits | ApartmentId | dbo.Apartments | Id | FK_PaymentDeposits_Apartments_BuildingId_ApartmentId |
| dbo.PaymentDeposits | PeriodId | dbo.FinancialPeriods | PeriodId | FK_PaymentDeposits_FinancialPeriods_PeriodId_BuildingId |
| dbo.PaymentDeposits | BuildingId | dbo.FinancialPeriods | BuildingId | FK_PaymentDeposits_FinancialPeriods_PeriodId_BuildingId |
| dbo.PaymentDeposits | PostedTransactionId | dbo.LedgerTransactions | LedgerTransactionId | FK_PaymentDeposits_LedgerTransactions_PostedTransactionId_PeriodId_BuildingId |
| dbo.PaymentDeposits | PeriodId | dbo.LedgerTransactions | PeriodId | FK_PaymentDeposits_LedgerTransactions_PostedTransactionId_PeriodId_BuildingId |
| dbo.PaymentDeposits | BuildingId | dbo.LedgerTransactions | BuildingId | FK_PaymentDeposits_LedgerTransactions_PostedTransactionId_PeriodId_BuildingId |
| dbo.PaymentDeposits | ReversalTransactionId | dbo.LedgerTransactions | LedgerTransactionId | FK_PaymentDeposits_LedgerTransactions_ReversalTransactionId_PeriodId_BuildingId |
| dbo.PaymentDeposits | PeriodId | dbo.LedgerTransactions | PeriodId | FK_PaymentDeposits_LedgerTransactions_ReversalTransactionId_PeriodId_BuildingId |
| dbo.PaymentDeposits | BuildingId | dbo.LedgerTransactions | BuildingId | FK_PaymentDeposits_LedgerTransactions_ReversalTransactionId_PeriodId_BuildingId |
| dbo.PaymentDeposits | CancelledByAccountId | dbo.UserAccounts | AccountId | FK_PaymentDeposits_UserAccounts_CancelledByAccountId |
| dbo.PaymentDeposits | ConfirmedByAccountId | dbo.UserAccounts | AccountId | FK_PaymentDeposits_UserAccounts_ConfirmedByAccountId |
| dbo.PaymentDeposits | ReversedByAccountId | dbo.UserAccounts | AccountId | FK_PaymentDeposits_UserAccounts_ReversedByAccountId |
| dbo.PollOptions | BuildingId | dbo.Polls | BuildingId | FK_PollOptions_Polls_BuildingId_PollId |
| dbo.PollOptions | PollId | dbo.Polls | Id | FK_PollOptions_Polls_BuildingId_PollId |
| dbo.Polls | BuildingId | dbo.Buildings | Id | FK_Polls_Buildings_BuildingId |
| dbo.PollVotes | BuildingId | dbo.Apartments | BuildingId | FK_PollVotes_Apartments_BuildingId_ApartmentId |
| dbo.PollVotes | ApartmentId | dbo.Apartments | Id | FK_PollVotes_Apartments_BuildingId_ApartmentId |
| dbo.PollVotes | BuildingId | dbo.PollOptions | BuildingId | FK_PollVotes_PollOptions_BuildingId_PollId_OptionId |
| dbo.PollVotes | PollId | dbo.PollOptions | PollId | FK_PollVotes_PollOptions_BuildingId_PollId_OptionId |
| dbo.PollVotes | OptionId | dbo.PollOptions | Id | FK_PollVotes_PollOptions_BuildingId_PollId_OptionId |
| dbo.PresenceHeartbeats | PresenceSessionId | dbo.PresenceSessions | PresenceSessionId | FK_PresenceHeartbeats_Session |
| dbo.QrCapabilityUses | CapabilityId | dbo.QrCapabilities | CapabilityId | FK_QrCapabilityUses_Capability |
| dbo.QrTokens | BuildingId | dbo.Apartments | BuildingId | FK_QrTokens_Apartments_BuildingId_ApartmentId |
| dbo.QrTokens | ApartmentId | dbo.Apartments | Id | FK_QrTokens_Apartments_BuildingId_ApartmentId |
| dbo.QrTokens | BuildingId | dbo.Buildings | Id | FK_QrTokens_Buildings_BuildingId |
| dbo.Residents | BuildingId | dbo.Apartments | BuildingId | FK_Residents_Apartments_BuildingId_ApartmentId |
| dbo.Residents | ApartmentId | dbo.Apartments | Id | FK_Residents_Apartments_BuildingId_ApartmentId |
| dbo.RestoreOperations | BackupArtifactId | dbo.BackupArtifacts | BackupArtifactId | FK_RestoreOperations_Artifact |
| dbo.Revenues | BuildingId | dbo.Buildings | Id | FK_Revenues_Buildings_BuildingId |
| dbo.Revenues | BuildingId | dbo.FinancialCategories | BuildingId | FK_Revenues_FinancialCategories_BuildingId_CategoryKind_CategoryId |
| dbo.Revenues | CategoryKind | dbo.FinancialCategories | Kind | FK_Revenues_FinancialCategories_BuildingId_CategoryKind_CategoryId |
| dbo.Revenues | CategoryId | dbo.FinancialCategories | Id | FK_Revenues_FinancialCategories_BuildingId_CategoryKind_CategoryId |
| dbo.RolePermissions | RoleKey | dbo.RoleDefinitions | RoleKey | FK_RolePermissions_RoleDefinitions_RoleKey |
| dbo.UserAccounts | PersonId | dbo.Persons | PersonId | FK_UserAccounts_Persons_PersonId |
| dbo.UserCredentials | AccountId | dbo.UserAccounts | AccountId | FK_UserCredentials_UserAccounts_AccountId |
| dbo.UserCredentials | ReplacedByCredentialId | dbo.UserCredentials | CredentialId | FK_UserCredentials_UserCredentials_ReplacedByCredentialId_AccountId |
| dbo.UserCredentials | AccountId | dbo.UserCredentials | AccountId | FK_UserCredentials_UserCredentials_ReplacedByCredentialId_AccountId |
| dbo.UserPermissions | Uid | dbo.Users | Uid | FK_UserPermissions_Users_Uid |
| dbo.UserRecoveryCodes | AccountId | dbo.UserAccounts | AccountId | FK_UserRecoveryCodes_UserAccounts_AccountId |
| dbo.UserRoleAssignments | ScopeBuildingId | dbo.Apartments | BuildingId | FK_UserRoleAssignments_Apartments_ScopeBuildingId_ScopeApartmentId |
| dbo.UserRoleAssignments | ScopeApartmentId | dbo.Apartments | Id | FK_UserRoleAssignments_Apartments_ScopeBuildingId_ScopeApartmentId |
| dbo.UserRoleAssignments | ScopeBuildingId | dbo.Buildings | Id | FK_UserRoleAssignments_Buildings_ScopeBuildingId |
| dbo.UserRoleAssignments | RoleKey | dbo.RoleDefinitions | RoleKey | FK_UserRoleAssignments_RoleDefinitions_RoleKey |
| dbo.UserRoleAssignments | AccountId | dbo.UserAccounts | AccountId | FK_UserRoleAssignments_UserAccounts_AccountId |
| dbo.UserRoleAssignments | GrantedByAccountId | dbo.UserAccounts | AccountId | FK_UserRoleAssignments_UserAccounts_GrantedByAccountId |
| dbo.UserRoleAssignments | RevokedByAccountId | dbo.UserAccounts | AccountId | FK_UserRoleAssignments_UserAccounts_RevokedByAccountId |
| dbo.UserTwoFactorSettings | AccountId | dbo.UserAccounts | AccountId | FK_UserTwoFactorSettings_UserAccounts_AccountId |
| dbo.WalletAdjustments | BuildingId | dbo.Apartments | BuildingId | FK_WalletAdjustments_Apartments_BuildingId_ApartmentId |
| dbo.WalletAdjustments | ApartmentId | dbo.Apartments | Id | FK_WalletAdjustments_Apartments_BuildingId_ApartmentId |
| dbo.WalletAdjustments | BuildingId | dbo.Buildings | Id | FK_WalletAdjustments_Buildings_BuildingId |

## Migration safety

- This phase is read-only and inventories every non-system user table, including empty tables.
- Row counts are exact COUNT_BIG values taken from the inspected database at execution time.
- Primary keys, columns, and declared foreign-key relationships are recorded to design deterministic, relationship-safe migration.
- This inventory does not claim that a table is migrated merely because it exists in the target database.
- The next migration phase must classify every table as operationally migrated, preserved as immutable legacy evidence, intentionally empty, or explicitly unresolved.
- No source data is updated or deleted. Do not retire the legacy database until source/target row counts, fingerprints, keys, and relationships reconcile table by table.
