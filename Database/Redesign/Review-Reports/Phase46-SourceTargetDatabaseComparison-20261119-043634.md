# Phase 46 â€” Legacy source versus new target database comparison

- Run: 2026-11-19T04:36:36.1724782+02:00
- Source database: BuildingManagement
- Target database: BuildingManagement_RestoreTest
- Source user tables: 32
- Target user tables: 97
- Source total rows: 1618
- Target total rows: 5119
- Source-only tables missing in target: 0
- Target-only tables: 65
- Shared tables with row-count differences: 4

## Table-by-table comparison

| Table | Source rows | Target rows | Delta (target-source) | Source PK | Target PK | Source columns | Target columns | Source FK refs | Target FK refs | Status |
|---|---:|---:|---:|---|---|---:|---:|---:|---:|---|
| dbo.__EFMigrationsHistory | 4 | 19 | 15 | [MigrationId] | [MigrationId] | 2 | 2 | 0 | 0 | ROW_COUNT_MISMATCH |
| dbo.AccountPermissionOverrides | 0 | 0 | 0 |  | [OverrideId] | 0 | 16 | 0 | 5 | TARGET_ONLY |
| dbo.ApartmentAccessGrants | 0 | 0 | 0 |  | [GrantId] | 0 | 14 | 0 | 4 | TARGET_ONLY |
| dbo.ApartmentOccupancies | 0 | 0 | 0 |  | [OccupancyId] | 0 | 12 | 0 | 3 | TARGET_ONLY |
| dbo.ApartmentOwnerships | 0 | 0 | 0 |  | [OwnershipId] | 0 | 16 | 0 | 4 | TARGET_ONLY |
| dbo.Apartments | 79 | 79 | 0 | [BuildingId], [Id] | [BuildingId], [Id] | 23 | 23 | 9 | 18 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.AuditEvents | 0 | 1053 | 1053 |  | [AuditEventId] | 0 | 14 | 0 | 0 | TARGET_ONLY |
| dbo.AuditLog | 1050 | 1053 | 3 | [AuditId] | [AuditId] | 14 | 14 | 1 | 1 | ROW_COUNT_MISMATCH |
| dbo.BackupArtifacts | 0 | 0 | 0 |  | [BackupArtifactId] | 0 | 7 | 0 | 2 | TARGET_ONLY |
| dbo.BackupHistory | 1 | 2 | 1 | [Id] | [Id] | 10 | 10 | 0 | 0 | ROW_COUNT_MISMATCH |
| dbo.BackupJobs | 0 | 2 | 2 |  | [BackupJobId] | 0 | 9 | 0 | 0 | TARGET_ONLY |
| dbo.BackupRetentionEvents | 0 | 0 | 0 |  | [RetentionEventId] | 0 | 6 | 0 | 1 | TARGET_ONLY |
| dbo.BackupScheduleConfigurations | 0 | 1 | 1 |  | [ConfigurationId] | 0 | 14 | 0 | 0 | TARGET_ONLY |
| dbo.BuildingAdmins | 2 | 2 | 0 | [BuildingId], [AdminUid] | [BuildingId], [AdminUid] | 2 | 2 | 2 | 2 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.Buildings | 3 | 3 | 0 | [Id] | [Id] | 13 | 13 | 16 | 20 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.BuildingSettings | 0 | 0 | 0 | [BuildingId] | [BuildingId] | 10 | 10 | 1 | 1 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.Cameras | 3 | 3 | 0 | [Id] | [Id] | 9 | 9 | 1 | 1 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.DepositEditHistory | 1 | 1 | 0 | [EditId] | [EditId] | 10 | 10 | 1 | 1 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.Deposits | 26 | 26 | 0 | [BuildingId], [Id] | [BuildingId], [Id] | 24 | 24 | 3 | 3 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.DocumentAccessGrants | 0 | 0 | 0 |  | [GrantId] | 0 | 6 | 0 | 1 | TARGET_ONLY |
| dbo.DocumentAssets | 0 | 0 | 0 |  | [DocumentId] | 0 | 11 | 0 | 2 | TARGET_ONLY |
| dbo.Dvrs | 1 | 1 | 0 | [Id] | [Id] | 11 | 11 | 2 | 2 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.Expenses | 12 | 12 | 0 | [BuildingId], [Id] | [BuildingId], [Id] | 9 | 9 | 2 | 2 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.ExportJobs | 0 | 0 | 0 |  | [ExportJobId] | 0 | 9 | 0 | 0 | TARGET_ONLY |
| dbo.FileProcessingJobs | 0 | 0 | 0 |  | [JobId] | 0 | 8 | 0 | 1 | TARGET_ONLY |
| dbo.FinancialAllocationBatches | 0 | 0 | 0 |  | [AllocationBatchId] | 0 | 14 | 0 | 4 | TARGET_ONLY |
| dbo.FinancialAllocationLines | 0 | 0 | 0 |  | [AllocationLineId] | 0 | 9 | 0 | 3 | TARGET_ONLY |
| dbo.FinancialCategories | 38 | 38 | 0 | [BuildingId], [Kind], [Id] | [BuildingId], [Kind], [Id] | 7 | 7 | 3 | 3 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.FinancialPeriods | 0 | 4 | 4 |  | [PeriodId] | 0 | 12 | 0 | 7 | TARGET_ONLY |
| dbo.FinancialReconciliationDifferences | 0 | 2 | 2 |  | [ReconciliationDifferenceId] | 0 | 16 | 0 | 4 | TARGET_ONLY |
| dbo.FinancialReconciliationRuns | 0 | 1 | 1 |  | [ReconciliationRunId] | 0 | 8 | 0 | 2 | TARGET_ONLY |
| dbo.FinancialSourceLinks | 0 | 26 | 26 |  | [FinancialSourceLinkId] | 0 | 7 | 0 | 1 | TARGET_ONLY |
| dbo.Floors | 16 | 16 | 0 | [BuildingId], [Id] | [BuildingId], [Id] | 4 | 4 | 2 | 2 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.GlobalApplicationSettings | 0 | 1 | 1 |  | [SettingsId] | 0 | 14 | 0 | 0 | TARGET_ONLY |
| dbo.GlobalSettings | 1 | 1 | 0 | [Id] | [Id] | 17 | 17 | 0 | 0 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.GuestAccessSessions | 0 | 0 | 0 |  | [GuestSessionId] | 0 | 6 | 0 | 1 | TARGET_ONLY |
| dbo.ImportBatches | 0 | 0 | 0 |  | [ImportBatchId] | 0 | 7 | 0 | 1 | TARGET_ONLY |
| dbo.ImportRows | 0 | 0 | 0 |  | [ImportRowId] | 0 | 7 | 0 | 1 | TARGET_ONLY |
| dbo.InboxMessages | 0 | 0 | 0 |  | [InboxMessageId] | 0 | 7 | 0 | 0 | TARGET_ONLY |
| dbo.LedgerEntries | 0 | 136 | 136 |  | [LedgerEntryId] | 0 | 11 | 0 | 3 | TARGET_ONLY |
| dbo.LedgerTransactions | 0 | 16 | 16 |  | [LedgerTransactionId] | 0 | 16 | 0 | 8 | TARGET_ONLY |
| dbo.LegacyDataMigrationMap | 0 | 1482 | 1482 |  | [MigrationMapId] | 0 | 11 | 0 | 0 | TARGET_ONLY |
| dbo.LegacyIdentityMap | 0 | 84 | 84 |  | [LegacyIdentityMapId] | 0 | 15 | 0 | 3 | TARGET_ONLY |
| dbo.LegacyOperationalRecords | 0 | 329 | 329 |  | [LegacyOperationalRecordId] | 0 | 8 | 0 | 0 | TARGET_ONLY |
| dbo.LoginThrottle | 0 | 0 | 0 | [Key] | [Key] | 3 | 3 | 0 | 0 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.MaintenanceAssignments | 0 | 0 | 0 |  | [AssignmentId] | 0 | 7 | 0 | 1 | TARGET_ONLY |
| dbo.MaintenanceAttachments | 0 | 0 | 0 |  | [AttachmentId] | 0 | 9 | 0 | 1 | TARGET_ONLY |
| dbo.MaintenanceComments | 0 | 0 | 0 |  | [CommentId] | 0 | 6 | 0 | 1 | TARGET_ONLY |
| dbo.MaintenanceLedgerLinks | 0 | 0 | 0 |  | [LinkId] | 0 | 5 | 0 | 1 | TARGET_ONLY |
| dbo.MaintenanceRecords | 1 | 1 | 0 | [BuildingId], [Id] | [BuildingId], [Id] | 12 | 12 | 1 | 1 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.MaintenanceRequests | 0 | 1 | 1 |  | [RequestId] | 0 | 14 | 0 | 5 | TARGET_ONLY |
| dbo.MaintenanceStatusEvents | 0 | 0 | 0 |  | [StatusEventId] | 0 | 7 | 0 | 1 | TARGET_ONLY |
| dbo.MediaAccessSessions | 0 | 0 | 0 |  | [MediaAccessSessionId] | 0 | 7 | 0 | 0 | TARGET_ONLY |
| dbo.MediaCredentialReferences | 0 | 0 | 0 |  | [CredentialReferenceId] | 0 | 9 | 0 | 1 | TARGET_ONLY |
| dbo.MediaProvisioningOperations | 0 | 0 | 0 |  | [OperationId] | 0 | 9 | 0 | 1 | TARGET_ONLY |
| dbo.MonthlyApartmentShares | 113 | 113 | 0 | [BuildingId], [MonthKey], [ApartmentId] | [BuildingId], [MonthKey], [ApartmentId] | 6 | 6 | 2 | 2 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.NotificationDeliveries | 0 | 0 | 0 |  | [DeliveryId] | 0 | 9 | 0 | 1 | TARGET_ONLY |
| dbo.NotificationMessages | 0 | 68 | 68 |  | [MessageId] | 0 | 9 | 0 | 1 | TARGET_ONLY |
| dbo.NotificationPreferences | 0 | 0 | 0 |  | [PreferenceId] | 0 | 6 | 0 | 0 | TARGET_ONLY |
| dbo.NotificationReadStates | 0 | 35 | 35 |  | [ReadStateId] | 0 | 4 | 0 | 1 | TARGET_ONLY |
| dbo.NotificationRecipients | 0 | 68 | 68 |  | [RecipientId] | 0 | 6 | 0 | 3 | TARGET_ONLY |
| dbo.Notifications | 68 | 68 | 0 | [BuildingId], [Id] | [BuildingId], [Id] | 11 | 11 | 2 | 2 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.OutboxMessages | 0 | 0 | 0 |  | [OutboxMessageId] | 0 | 11 | 0 | 0 | TARGET_ONLY |
| dbo.PaymentDeposits | 0 | 26 | 26 |  | [PaymentDepositId] | 0 | 20 | 0 | 7 | TARGET_ONLY |
| dbo.Persons | 0 | 85 | 85 |  | [PersonId] | 0 | 12 | 0 | 5 | TARGET_ONLY |
| dbo.PollBallots | 0 | 0 | 0 |  | [BallotId] | 0 | 7 | 0 | 0 | TARGET_ONLY |
| dbo.PollDelegations | 0 | 0 | 0 |  | [DelegationId] | 0 | 9 | 0 | 0 | TARGET_ONLY |
| dbo.PollEligibility | 0 | 0 | 0 |  | [EligibilityId] | 0 | 9 | 0 | 0 | TARGET_ONLY |
| dbo.PollLifecycleEvents | 0 | 1 | 1 |  | [EventId] | 0 | 8 | 0 | 0 | TARGET_ONLY |
| dbo.PollOptions | 2 | 2 | 0 | [BuildingId], [PollId], [Id] | [BuildingId], [PollId], [Id] | 5 | 5 | 2 | 2 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.Polls | 1 | 1 | 0 | [BuildingId], [Id] | [BuildingId], [Id] | 8 | 8 | 2 | 2 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.PollVotes | 1 | 1 | 0 | [BuildingId], [PollId], [ApartmentId] | [BuildingId], [PollId], [ApartmentId] | 5 | 5 | 2 | 2 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.PollVotingPolicies | 0 | 0 | 0 |  | [PolicyId] | 0 | 8 | 0 | 0 | TARGET_ONLY |
| dbo.Presence | 9 | 10 | 1 | [Uid] | [Uid] | 4 | 4 | 0 | 0 | ROW_COUNT_MISMATCH |
| dbo.PresenceHeartbeats | 0 | 0 | 0 |  | [HeartbeatId] | 0 | 4 | 0 | 1 | TARGET_ONLY |
| dbo.PresenceSessions | 0 | 10 | 10 |  | [PresenceSessionId] | 0 | 11 | 0 | 1 | TARGET_ONLY |
| dbo.ProtectedIntegrationSecrets | 0 | 0 | 0 |  | [SecretId] | 0 | 10 | 0 | 0 | TARGET_ONLY |
| dbo.QrCapabilities | 0 | 9 | 9 |  | [CapabilityId] | 0 | 11 | 0 | 2 | TARGET_ONLY |
| dbo.QrCapabilityUses | 0 | 0 | 0 |  | [UseId] | 0 | 5 | 0 | 1 | TARGET_ONLY |
| dbo.QrTokens | 9 | 9 | 0 | [Uid] | [Uid] | 11 | 11 | 2 | 2 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.QrUsages | 5 | 5 | 0 | [Id] | [Id] | 7 | 7 | 0 | 0 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.Residents | 79 | 79 | 0 | [Id] | [Id] | 21 | 21 | 1 | 1 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.RestoreOperations | 0 | 0 | 0 |  | [RestoreOperationId] | 0 | 10 | 0 | 1 | TARGET_ONLY |
| dbo.Revenues | 4 | 4 | 0 | [BuildingId], [Id] | [BuildingId], [Id] | 8 | 8 | 2 | 2 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.RoleDefinitions | 0 | 2 | 2 |  | [RoleKey] | 0 | 4 | 0 | 2 | TARGET_ONLY |
| dbo.RolePermissions | 0 | 28 | 28 |  | [RoleKey], [PermissionKey] | 0 | 3 | 0 | 1 | TARGET_ONLY |
| dbo.ScheduledBackupSettings | 1 | 1 | 0 | [Id] | [Id] | 12 | 12 | 0 | 0 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.SyncPendingChanges | 0 | 0 | 0 | [Id] | [Id] | 10 | 10 | 0 | 0 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.UserAccounts | 0 | 6 | 6 |  | [AccountId] | 0 | 10 | 0 | 25 | TARGET_ONLY |
| dbo.UserCredentials | 0 | 1 | 1 |  | [CredentialId] | 0 | 10 | 0 | 2 | TARGET_ONLY |
| dbo.UserPermissions | 81 | 81 | 0 | [Uid], [Permission] | [Uid], [Permission] | 2 | 2 | 1 | 1 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.UserPreferences | 0 | 0 | 0 |  | [PreferenceId] | 0 | 5 | 0 | 0 | TARGET_ONLY |
| dbo.UserRecoveryCodes | 0 | 0 | 0 |  | [RecoveryCodeId] | 0 | 5 | 0 | 1 | TARGET_ONLY |
| dbo.UserRoleAssignments | 0 | 4 | 4 |  | [RoleAssignmentId] | 0 | 15 | 0 | 6 | TARGET_ONLY |
| dbo.Users | 5 | 5 | 0 | [Uid] | [Uid] | 20 | 20 | 2 | 2 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |
| dbo.UserTwoFactorSettings | 0 | 0 | 0 |  | [AccountId] | 0 | 9 | 0 | 1 | TARGET_ONLY |
| dbo.WalletAdjustments | 2 | 2 | 0 | [BuildingId], [Id] | [BuildingId], [Id] | 8 | 8 | 2 | 2 | ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH |

## Source-only tables


## Column metadata differences

| Column | Source definition | Target definition | Difference |
|---|---|---|---|
| dbo.AccountPermissionOverrides.AccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.AccountPermissionOverrides.Effect | (missing) | tinyint(1) | target-only |
| dbo.AccountPermissionOverrides.GrantedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.AccountPermissionOverrides.GrantedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.AccountPermissionOverrides.GrantReason | (missing) | nvarchar(1000) | target-only |
| dbo.AccountPermissionOverrides.OverrideId | (missing) | uniqueidentifier(16) | target-only |
| dbo.AccountPermissionOverrides.PermissionKey | (missing) | nvarchar(200) | target-only |
| dbo.AccountPermissionOverrides.RevocationReason | (missing) | nvarchar(1000) | target-only |
| dbo.AccountPermissionOverrides.RevokedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.AccountPermissionOverrides.RevokedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.AccountPermissionOverrides.RowVersion | (missing) | timestamp(8) | target-only |
| dbo.AccountPermissionOverrides.ScopeApartmentId | (missing) | nvarchar(128) | target-only |
| dbo.AccountPermissionOverrides.ScopeBuildingId | (missing) | nvarchar(128) | target-only |
| dbo.AccountPermissionOverrides.ScopeType | (missing) | tinyint(1) | target-only |
| dbo.AccountPermissionOverrides.ValidFromUtc | (missing) | datetime2(7) | target-only |
| dbo.AccountPermissionOverrides.ValidToUtc | (missing) | datetime2(7) | target-only |
| dbo.ApartmentAccessGrants.ApartmentId | (missing) | nvarchar(128) | target-only |
| dbo.ApartmentAccessGrants.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.ApartmentAccessGrants.GrantedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.ApartmentAccessGrants.GrantedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ApartmentAccessGrants.GrantId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ApartmentAccessGrants.GrantType | (missing) | tinyint(1) | target-only |
| dbo.ApartmentAccessGrants.PermissionSetKey | (missing) | nvarchar(160) | target-only |
| dbo.ApartmentAccessGrants.PersonId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ApartmentAccessGrants.Reason | (missing) | nvarchar(1000) | target-only |
| dbo.ApartmentAccessGrants.RevokedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.ApartmentAccessGrants.RevokedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ApartmentAccessGrants.RowVersion | (missing) | timestamp(8) | target-only |
| dbo.ApartmentAccessGrants.ValidFromUtc | (missing) | datetime2(7) | target-only |
| dbo.ApartmentAccessGrants.ValidToUtc | (missing) | datetime2(7) | target-only |
| dbo.ApartmentOccupancies.ApartmentId | (missing) | nvarchar(128) | target-only |
| dbo.ApartmentOccupancies.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.ApartmentOccupancies.IsPrimary | (missing) | bit(1) | target-only |
| dbo.ApartmentOccupancies.Notes | (missing) | nvarchar(2000) | target-only |
| dbo.ApartmentOccupancies.OccupancyId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ApartmentOccupancies.OccupancyType | (missing) | tinyint(1) | target-only |
| dbo.ApartmentOccupancies.PersonId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ApartmentOccupancies.RecordedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.ApartmentOccupancies.RecordedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ApartmentOccupancies.RowVersion | (missing) | timestamp(8) | target-only |
| dbo.ApartmentOccupancies.ValidFromUtc | (missing) | datetime2(7) | target-only |
| dbo.ApartmentOccupancies.ValidToUtc | (missing) | datetime2(7) | target-only |
| dbo.ApartmentOwnerships.ApartmentId | (missing) | nvarchar(128) | target-only |
| dbo.ApartmentOwnerships.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.ApartmentOwnerships.CompletenessStatus | (missing) | tinyint(1) | target-only |
| dbo.ApartmentOwnerships.CompletenessVerifiedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.ApartmentOwnerships.CompletenessVerifiedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ApartmentOwnerships.EvidenceReference | (missing) | nvarchar(1000) | target-only |
| dbo.ApartmentOwnerships.Notes | (missing) | nvarchar(2000) | target-only |
| dbo.ApartmentOwnerships.OwnershipId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ApartmentOwnerships.OwnershipSharePercent | (missing) | decimal(5) | target-only |
| dbo.ApartmentOwnerships.PersonId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ApartmentOwnerships.RecordedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.ApartmentOwnerships.RecordedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ApartmentOwnerships.RecordSource | (missing) | tinyint(1) | target-only |
| dbo.ApartmentOwnerships.RowVersion | (missing) | timestamp(8) | target-only |
| dbo.ApartmentOwnerships.ValidFromUtc | (missing) | datetime2(7) | target-only |
| dbo.ApartmentOwnerships.ValidToUtc | (missing) | datetime2(7) | target-only |
| dbo.AuditEvents.ActorAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.AuditEvents.ActorReference | (missing) | nvarchar(320) | target-only |
| dbo.AuditEvents.AuditEventId | (missing) | uniqueidentifier(16) | target-only |
| dbo.AuditEvents.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.AuditEvents.CorrelationId | (missing) | nvarchar(200) | target-only |
| dbo.AuditEvents.EventCategory | (missing) | nvarchar(160) | target-only |
| dbo.AuditEvents.EventHash | (missing) | varbinary(32) | target-only |
| dbo.AuditEvents.EventType | (missing) | nvarchar(240) | target-only |
| dbo.AuditEvents.OccurredAtUtc | (missing) | datetime2(7) | target-only |
| dbo.AuditEvents.Outcome | (missing) | tinyint(1) | target-only |
| dbo.AuditEvents.PreviousHash | (missing) | varbinary(32) | target-only |
| dbo.AuditEvents.ResourceReference | (missing) | nvarchar(400) | target-only |
| dbo.AuditEvents.ResourceType | (missing) | nvarchar(200) | target-only |
| dbo.AuditEvents.SafeDetailsJson | (missing) | nvarchar(-1) | target-only |
| dbo.BackupArtifacts.BackupArtifactId | (missing) | uniqueidentifier(16) | target-only |
| dbo.BackupArtifacts.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.BackupArtifacts.EncryptionKeyVersion | (missing) | nvarchar(200) | target-only |
| dbo.BackupArtifacts.RetainUntilUtc | (missing) | datetime2(7) | target-only |
| dbo.BackupArtifacts.Sha256 | (missing) | varbinary(32) | target-only |
| dbo.BackupArtifacts.SizeBytes | (missing) | bigint(8) | target-only |
| dbo.BackupArtifacts.StorageReference | (missing) | nvarchar(1000) | target-only |
| dbo.BackupJobs.BackupArtifactId | (missing) | uniqueidentifier(16) | target-only |
| dbo.BackupJobs.BackupJobId | (missing) | uniqueidentifier(16) | target-only |
| dbo.BackupJobs.CompletedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.BackupJobs.DatabaseReference | (missing) | nvarchar(400) | target-only |
| dbo.BackupJobs.JobType | (missing) | tinyint(1) | target-only |
| dbo.BackupJobs.RequestedByReference | (missing) | nvarchar(320) | target-only |
| dbo.BackupJobs.SafeErrorCode | (missing) | nvarchar(200) | target-only |
| dbo.BackupJobs.StartedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.BackupJobs.Status | (missing) | tinyint(1) | target-only |
| dbo.BackupRetentionEvents.ActorReference | (missing) | nvarchar(320) | target-only |
| dbo.BackupRetentionEvents.BackupArtifactId | (missing) | uniqueidentifier(16) | target-only |
| dbo.BackupRetentionEvents.EventType | (missing) | tinyint(1) | target-only |
| dbo.BackupRetentionEvents.OccurredAtUtc | (missing) | datetime2(7) | target-only |
| dbo.BackupRetentionEvents.Reason | (missing) | nvarchar(2000) | target-only |
| dbo.BackupRetentionEvents.RetentionEventId | (missing) | uniqueidentifier(16) | target-only |
| dbo.BackupScheduleConfigurations.CollectionsJson | (missing) | nvarchar(-1) | target-only |
| dbo.BackupScheduleConfigurations.ConfigurationId | (missing) | uniqueidentifier(16) | target-only |
| dbo.BackupScheduleConfigurations.DayOfMonth | (missing) | int(4) | target-only |
| dbo.BackupScheduleConfigurations.DayOfWeek | (missing) | int(4) | target-only |
| dbo.BackupScheduleConfigurations.Frequency | (missing) | nvarchar(40) | target-only |
| dbo.BackupScheduleConfigurations.ImportedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.BackupScheduleConfigurations.IsEnabled | (missing) | bit(1) | target-only |
| dbo.BackupScheduleConfigurations.LastRunAtUtc | (missing) | datetime2(7) | target-only |
| dbo.BackupScheduleConfigurations.LastRunStatus | (missing) | nvarchar(60) | target-only |
| dbo.BackupScheduleConfigurations.LegacySettingId | (missing) | nvarchar(40) | target-only |
| dbo.BackupScheduleConfigurations.MaxBackupsToKeep | (missing) | int(4) | target-only |
| dbo.BackupScheduleConfigurations.ScheduledHour | (missing) | int(4) | target-only |
| dbo.BackupScheduleConfigurations.UpdatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.BackupScheduleConfigurations.UpdatedByReference | (missing) | nvarchar(256) | target-only |
| dbo.DocumentAccessGrants.DocumentId | (missing) | uniqueidentifier(16) | target-only |
| dbo.DocumentAccessGrants.ExpiresAtUtc | (missing) | datetime2(7) | target-only |
| dbo.DocumentAccessGrants.GrantedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.DocumentAccessGrants.GrantId | (missing) | uniqueidentifier(16) | target-only |
| dbo.DocumentAccessGrants.RevokedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.DocumentAccessGrants.SubjectReference | (missing) | nvarchar(320) | target-only |
| dbo.DocumentAssets.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.DocumentAssets.Classification | (missing) | tinyint(1) | target-only |
| dbo.DocumentAssets.ContentType | (missing) | nvarchar(240) | target-only |
| dbo.DocumentAssets.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.DocumentAssets.DocumentId | (missing) | uniqueidentifier(16) | target-only |
| dbo.DocumentAssets.FileName | (missing) | nvarchar(520) | target-only |
| dbo.DocumentAssets.OwnerReference | (missing) | nvarchar(320) | target-only |
| dbo.DocumentAssets.RetainUntilUtc | (missing) | datetime2(7) | target-only |
| dbo.DocumentAssets.Sha256 | (missing) | varbinary(32) | target-only |
| dbo.DocumentAssets.SizeBytes | (missing) | bigint(8) | target-only |
| dbo.DocumentAssets.StorageKey | (missing) | nvarchar(1000) | target-only |
| dbo.ExportJobs.ArtifactStorageKey | (missing) | nvarchar(1000) | target-only |
| dbo.ExportJobs.CompletedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.ExportJobs.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.ExportJobs.ExpiresAtUtc | (missing) | datetime2(7) | target-only |
| dbo.ExportJobs.ExportJobId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ExportJobs.Format | (missing) | nvarchar(40) | target-only |
| dbo.ExportJobs.RequestedByReference | (missing) | nvarchar(320) | target-only |
| dbo.ExportJobs.ScopeJson | (missing) | nvarchar(4000) | target-only |
| dbo.ExportJobs.Status | (missing) | tinyint(1) | target-only |
| dbo.FileProcessingJobs.Attempts | (missing) | int(4) | target-only |
| dbo.FileProcessingJobs.CompletedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.FileProcessingJobs.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.FileProcessingJobs.DocumentId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FileProcessingJobs.JobId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FileProcessingJobs.JobType | (missing) | tinyint(1) | target-only |
| dbo.FileProcessingJobs.SafeErrorCode | (missing) | nvarchar(200) | target-only |
| dbo.FileProcessingJobs.Status | (missing) | tinyint(1) | target-only |
| dbo.FinancialAllocationBatches.AlgorithmVersion | (missing) | nvarchar(160) | target-only |
| dbo.FinancialAllocationBatches.AllocatedAmount | (missing) | decimal(9) | target-only |
| dbo.FinancialAllocationBatches.AllocationBatchId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialAllocationBatches.BasisKey | (missing) | nvarchar(160) | target-only |
| dbo.FinancialAllocationBatches.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.FinancialAllocationBatches.CompletedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.FinancialAllocationBatches.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.FinancialAllocationBatches.CreatedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialAllocationBatches.CurrencyCode | (missing) | char(3) | target-only |
| dbo.FinancialAllocationBatches.InputFingerprint | (missing) | varbinary(32) | target-only |
| dbo.FinancialAllocationBatches.PeriodId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialAllocationBatches.SourceAmount | (missing) | decimal(9) | target-only |
| dbo.FinancialAllocationBatches.SourceTransactionId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialAllocationBatches.Status | (missing) | tinyint(1) | target-only |
| dbo.FinancialAllocationLines.AllocatedAmount | (missing) | decimal(9) | target-only |
| dbo.FinancialAllocationLines.AllocationBatchId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialAllocationLines.AllocationLineId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialAllocationLines.ApartmentId | (missing) | nvarchar(128) | target-only |
| dbo.FinancialAllocationLines.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.FinancialAllocationLines.ExactShare | (missing) | decimal(9) | target-only |
| dbo.FinancialAllocationLines.PeriodId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialAllocationLines.RemainderRank | (missing) | int(4) | target-only |
| dbo.FinancialAllocationLines.Weight | (missing) | decimal(9) | target-only |
| dbo.FinancialPeriods.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.FinancialPeriods.ClosedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.FinancialPeriods.ClosedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialPeriods.CloseReason | (missing) | nvarchar(1000) | target-only |
| dbo.FinancialPeriods.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.FinancialPeriods.EndsAtUtc | (missing) | datetime2(7) | target-only |
| dbo.FinancialPeriods.PeriodId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialPeriods.PeriodKey | (missing) | char(7) | target-only |
| dbo.FinancialPeriods.RowVersion | (missing) | timestamp(8) | target-only |
| dbo.FinancialPeriods.StartsAtUtc | (missing) | datetime2(7) | target-only |
| dbo.FinancialPeriods.Status | (missing) | tinyint(1) | target-only |
| dbo.FinancialPeriods.TimeZoneId | (missing) | nvarchar(200) | target-only |
| dbo.FinancialReconciliationDifferences.ApartmentId | (missing) | nvarchar(128) | target-only |
| dbo.FinancialReconciliationDifferences.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.FinancialReconciliationDifferences.Classification | (missing) | nvarchar(160) | target-only |
| dbo.FinancialReconciliationDifferences.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.FinancialReconciliationDifferences.DifferenceAmount | (missing) | decimal(9) | target-only |
| dbo.FinancialReconciliationDifferences.DifferenceType | (missing) | nvarchar(160) | target-only |
| dbo.FinancialReconciliationDifferences.Disposition | (missing) | nvarchar(2000) | target-only |
| dbo.FinancialReconciliationDifferences.PeriodKey | (missing) | char(7) | target-only |
| dbo.FinancialReconciliationDifferences.ReconciliationDifferenceId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialReconciliationDifferences.ReconciliationRunId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialReconciliationDifferences.ReviewedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.FinancialReconciliationDifferences.ReviewedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialReconciliationDifferences.SourceAmount | (missing) | decimal(9) | target-only |
| dbo.FinancialReconciliationDifferences.SourceKey | (missing) | nvarchar(512) | target-only |
| dbo.FinancialReconciliationDifferences.SourceType | (missing) | nvarchar(160) | target-only |
| dbo.FinancialReconciliationDifferences.TargetAmount | (missing) | decimal(9) | target-only |
| dbo.FinancialReconciliationRuns.CompletedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.FinancialReconciliationRuns.ReconciliationRunId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialReconciliationRuns.RuleVersion | (missing) | nvarchar(160) | target-only |
| dbo.FinancialReconciliationRuns.SafeSummary | (missing) | nvarchar(4000) | target-only |
| dbo.FinancialReconciliationRuns.SourceSystem | (missing) | nvarchar(160) | target-only |
| dbo.FinancialReconciliationRuns.StartedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.FinancialReconciliationRuns.StartedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialReconciliationRuns.Status | (missing) | tinyint(1) | target-only |
| dbo.FinancialSourceLinks.FinancialSourceLinkId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialSourceLinks.LedgerTransactionId | (missing) | uniqueidentifier(16) | target-only |
| dbo.FinancialSourceLinks.LinkedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.FinancialSourceLinks.SourceFingerprint | (missing) | varbinary(32) | target-only |
| dbo.FinancialSourceLinks.SourceKey | (missing) | nvarchar(512) | target-only |
| dbo.FinancialSourceLinks.SourceSystem | (missing) | nvarchar(160) | target-only |
| dbo.FinancialSourceLinks.SourceType | (missing) | nvarchar(160) | target-only |
| dbo.GlobalApplicationSettings.AppName | (missing) | nvarchar(400) | target-only |
| dbo.GlobalApplicationSettings.DefaultLanguage | (missing) | nvarchar(32) | target-only |
| dbo.GlobalApplicationSettings.EnableMaintenance | (missing) | bit(1) | target-only |
| dbo.GlobalApplicationSettings.EnableOcr | (missing) | bit(1) | target-only |
| dbo.GlobalApplicationSettings.EnablePolls | (missing) | bit(1) | target-only |
| dbo.GlobalApplicationSettings.EnableQrAccess | (missing) | bit(1) | target-only |
| dbo.GlobalApplicationSettings.EnableWhatsApp | (missing) | bit(1) | target-only |
| dbo.GlobalApplicationSettings.ImportedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.GlobalApplicationSettings.MaxPinAttempts | (missing) | int(4) | target-only |
| dbo.GlobalApplicationSettings.SessionTimeoutDays | (missing) | int(4) | target-only |
| dbo.GlobalApplicationSettings.SettingsId | (missing) | nvarchar(128) | target-only |
| dbo.GlobalApplicationSettings.StorageMode | (missing) | nvarchar(64) | target-only |
| dbo.GlobalApplicationSettings.UpdatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.GlobalApplicationSettings.UpdatedByReference | (missing) | nvarchar(320) | target-only |
| dbo.GuestAccessSessions.CapabilityId | (missing) | uniqueidentifier(16) | target-only |
| dbo.GuestAccessSessions.ExpiresAtUtc | (missing) | datetime2(7) | target-only |
| dbo.GuestAccessSessions.GuestReference | (missing) | nvarchar(320) | target-only |
| dbo.GuestAccessSessions.GuestSessionId | (missing) | uniqueidentifier(16) | target-only |
| dbo.GuestAccessSessions.RevokedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.GuestAccessSessions.StartedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.ImportBatches.CompletedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.ImportBatches.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.ImportBatches.ImportBatchId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ImportBatches.ImportType | (missing) | nvarchar(200) | target-only |
| dbo.ImportBatches.RequestedByReference | (missing) | nvarchar(320) | target-only |
| dbo.ImportBatches.SourceFingerprint | (missing) | varbinary(32) | target-only |
| dbo.ImportBatches.Status | (missing) | tinyint(1) | target-only |
| dbo.ImportRows.ImportBatchId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ImportRows.ImportRowId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ImportRows.RowFingerprint | (missing) | varbinary(32) | target-only |
| dbo.ImportRows.SafeErrorCode | (missing) | nvarchar(200) | target-only |
| dbo.ImportRows.SourceRowKey | (missing) | nvarchar(400) | target-only |
| dbo.ImportRows.Status | (missing) | tinyint(1) | target-only |
| dbo.ImportRows.TargetReference | (missing) | nvarchar(400) | target-only |
| dbo.InboxMessages.Consumer | (missing) | nvarchar(320) | target-only |
| dbo.InboxMessages.InboxMessageId | (missing) | uniqueidentifier(16) | target-only |
| dbo.InboxMessages.MessageId | (missing) | nvarchar(400) | target-only |
| dbo.InboxMessages.Outcome | (missing) | tinyint(1) | target-only |
| dbo.InboxMessages.ProcessedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.InboxMessages.ReceivedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.InboxMessages.SafeErrorCode | (missing) | nvarchar(200) | target-only |
| dbo.LedgerEntries.ApartmentId | (missing) | nvarchar(128) | target-only |
| dbo.LedgerEntries.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.LedgerEntries.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.LedgerEntries.CurrencyCode | (missing) | char(3) | target-only |
| dbo.LedgerEntries.Description | (missing) | nvarchar(1000) | target-only |
| dbo.LedgerEntries.EffectiveAtUtc | (missing) | datetime2(7) | target-only |
| dbo.LedgerEntries.EntryKind | (missing) | tinyint(1) | target-only |
| dbo.LedgerEntries.LedgerEntryId | (missing) | uniqueidentifier(16) | target-only |
| dbo.LedgerEntries.LedgerTransactionId | (missing) | uniqueidentifier(16) | target-only |
| dbo.LedgerEntries.PeriodId | (missing) | uniqueidentifier(16) | target-only |
| dbo.LedgerEntries.SignedAmount | (missing) | decimal(9) | target-only |
| dbo.LedgerTransactions.ActorAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.LedgerTransactions.ActorReference | (missing) | nvarchar(256) | target-only |
| dbo.LedgerTransactions.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.LedgerTransactions.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.LedgerTransactions.IdempotencyKey | (missing) | nvarchar(256) | target-only |
| dbo.LedgerTransactions.Kind | (missing) | tinyint(1) | target-only |
| dbo.LedgerTransactions.LedgerTransactionId | (missing) | uniqueidentifier(16) | target-only |
| dbo.LedgerTransactions.PeriodId | (missing) | uniqueidentifier(16) | target-only |
| dbo.LedgerTransactions.PostedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.LedgerTransactions.Reason | (missing) | nvarchar(2000) | target-only |
| dbo.LedgerTransactions.ReversesTransactionId | (missing) | uniqueidentifier(16) | target-only |
| dbo.LedgerTransactions.RowVersion | (missing) | timestamp(8) | target-only |
| dbo.LedgerTransactions.SourceKey | (missing) | nvarchar(512) | target-only |
| dbo.LedgerTransactions.SourceSystem | (missing) | nvarchar(160) | target-only |
| dbo.LedgerTransactions.SourceType | (missing) | nvarchar(160) | target-only |
| dbo.LedgerTransactions.Status | (missing) | tinyint(1) | target-only |
| dbo.LegacyDataMigrationMap.Disposition | (missing) | nvarchar(160) | target-only |
| dbo.LegacyDataMigrationMap.MigratedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.LegacyDataMigrationMap.MigrationMapId | (missing) | uniqueidentifier(16) | target-only |
| dbo.LegacyDataMigrationMap.Outcome | (missing) | tinyint(1) | target-only |
| dbo.LegacyDataMigrationMap.SafeNote | (missing) | nvarchar(1000) | target-only |
| dbo.LegacyDataMigrationMap.SourceFingerprint | (missing) | varbinary(32) | target-only |
| dbo.LegacyDataMigrationMap.SourceKey | (missing) | nvarchar(512) | target-only |
| dbo.LegacyDataMigrationMap.SourceSystem | (missing) | nvarchar(160) | target-only |
| dbo.LegacyDataMigrationMap.SourceTable | (missing) | nvarchar(256) | target-only |
| dbo.LegacyDataMigrationMap.TargetKey | (missing) | nvarchar(512) | target-only |
| dbo.LegacyDataMigrationMap.TargetTable | (missing) | nvarchar(256) | target-only |
| dbo.LegacyIdentityMap.AccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.LegacyIdentityMap.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.LegacyIdentityMap.EvidenceReference | (missing) | nvarchar(1000) | target-only |
| dbo.LegacyIdentityMap.LegacyIdentityMapId | (missing) | uniqueidentifier(16) | target-only |
| dbo.LegacyIdentityMap.MappingStatus | (missing) | tinyint(1) | target-only |
| dbo.LegacyIdentityMap.MatchMethod | (missing) | nvarchar(160) | target-only |
| dbo.LegacyIdentityMap.PersonId | (missing) | uniqueidentifier(16) | target-only |
| dbo.LegacyIdentityMap.ReviewedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.LegacyIdentityMap.ReviewedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.LegacyIdentityMap.ReviewReason | (missing) | nvarchar(2000) | target-only |
| dbo.LegacyIdentityMap.RowVersion | (missing) | timestamp(8) | target-only |
| dbo.LegacyIdentityMap.SourceKey | (missing) | nvarchar(512) | target-only |
| dbo.LegacyIdentityMap.SourceSystem | (missing) | nvarchar(160) | target-only |
| dbo.LegacyIdentityMap.SourceTable | (missing) | nvarchar(256) | target-only |
| dbo.LegacyIdentityMap.UpdatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.LegacyOperationalRecords.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.LegacyOperationalRecords.ImportedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.LegacyOperationalRecords.LegacyOperationalRecordId | (missing) | uniqueidentifier(16) | target-only |
| dbo.LegacyOperationalRecords.PayloadJson | (missing) | nvarchar(-1) | target-only |
| dbo.LegacyOperationalRecords.SourceFingerprint | (missing) | varbinary(32) | target-only |
| dbo.LegacyOperationalRecords.SourceKey | (missing) | nvarchar(512) | target-only |
| dbo.LegacyOperationalRecords.SourceSystem | (missing) | nvarchar(160) | target-only |
| dbo.LegacyOperationalRecords.SourceTable | (missing) | nvarchar(256) | target-only |
| dbo.MaintenanceAssignments.AssignedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MaintenanceAssignments.AssigneeReference | (missing) | nvarchar(320) | target-only |
| dbo.MaintenanceAssignments.AssignmentId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MaintenanceAssignments.CompletedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MaintenanceAssignments.Notes | (missing) | nvarchar(2000) | target-only |
| dbo.MaintenanceAssignments.RequestId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MaintenanceAssignments.VendorName | (missing) | nvarchar(400) | target-only |
| dbo.MaintenanceAttachments.AttachmentId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MaintenanceAttachments.ContentType | (missing) | nvarchar(240) | target-only |
| dbo.MaintenanceAttachments.FileName | (missing) | nvarchar(520) | target-only |
| dbo.MaintenanceAttachments.RequestId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MaintenanceAttachments.Sha256 | (missing) | varbinary(32) | target-only |
| dbo.MaintenanceAttachments.SizeBytes | (missing) | bigint(8) | target-only |
| dbo.MaintenanceAttachments.StorageKey | (missing) | nvarchar(1000) | target-only |
| dbo.MaintenanceAttachments.UploadedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MaintenanceAttachments.UploadedByReference | (missing) | nvarchar(320) | target-only |
| dbo.MaintenanceComments.AuthorReference | (missing) | nvarchar(320) | target-only |
| dbo.MaintenanceComments.Body | (missing) | nvarchar(8000) | target-only |
| dbo.MaintenanceComments.CommentId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MaintenanceComments.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MaintenanceComments.RequestId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MaintenanceComments.Visibility | (missing) | tinyint(1) | target-only |
| dbo.MaintenanceLedgerLinks.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MaintenanceLedgerLinks.LedgerTransactionId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MaintenanceLedgerLinks.LinkId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MaintenanceLedgerLinks.LinkType | (missing) | tinyint(1) | target-only |
| dbo.MaintenanceLedgerLinks.RequestId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MaintenanceRequests.ApartmentId | (missing) | nvarchar(128) | target-only |
| dbo.MaintenanceRequests.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.MaintenanceRequests.Category | (missing) | nvarchar(160) | target-only |
| dbo.MaintenanceRequests.ClosedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MaintenanceRequests.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MaintenanceRequests.Description | (missing) | nvarchar(8000) | target-only |
| dbo.MaintenanceRequests.LegacyMaintenanceId | (missing) | nvarchar(256) | target-only |
| dbo.MaintenanceRequests.Priority | (missing) | tinyint(1) | target-only |
| dbo.MaintenanceRequests.RequestedByPersonId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MaintenanceRequests.RequestedByReference | (missing) | nvarchar(320) | target-only |
| dbo.MaintenanceRequests.RequestId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MaintenanceRequests.Status | (missing) | tinyint(1) | target-only |
| dbo.MaintenanceRequests.Title | (missing) | nvarchar(400) | target-only |
| dbo.MaintenanceRequests.UpdatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MaintenanceStatusEvents.ActorReference | (missing) | nvarchar(320) | target-only |
| dbo.MaintenanceStatusEvents.FromStatus | (missing) | tinyint(1) | target-only |
| dbo.MaintenanceStatusEvents.OccurredAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MaintenanceStatusEvents.Reason | (missing) | nvarchar(2000) | target-only |
| dbo.MaintenanceStatusEvents.RequestId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MaintenanceStatusEvents.StatusEventId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MaintenanceStatusEvents.ToStatus | (missing) | tinyint(1) | target-only |
| dbo.MediaAccessSessions.AccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MediaAccessSessions.CameraLegacyId | (missing) | nvarchar(256) | target-only |
| dbo.MediaAccessSessions.ExpiresAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MediaAccessSessions.IssuedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MediaAccessSessions.MediaAccessSessionId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MediaAccessSessions.RevokedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MediaAccessSessions.SessionReference | (missing) | nvarchar(400) | target-only |
| dbo.MediaCredentialReferences.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.MediaCredentialReferences.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MediaCredentialReferences.CredentialReferenceId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MediaCredentialReferences.DvrLegacyId | (missing) | nvarchar(256) | target-only |
| dbo.MediaCredentialReferences.KeyVersion | (missing) | nvarchar(200) | target-only |
| dbo.MediaCredentialReferences.RevokedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MediaCredentialReferences.RotatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MediaCredentialReferences.VaultProvider | (missing) | nvarchar(160) | target-only |
| dbo.MediaCredentialReferences.VaultSecretReference | (missing) | nvarchar(1000) | target-only |
| dbo.MediaProvisioningOperations.Attempts | (missing) | int(4) | target-only |
| dbo.MediaProvisioningOperations.CompletedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MediaProvisioningOperations.CredentialReferenceId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MediaProvisioningOperations.DvrLegacyId | (missing) | nvarchar(256) | target-only |
| dbo.MediaProvisioningOperations.OperationId | (missing) | uniqueidentifier(16) | target-only |
| dbo.MediaProvisioningOperations.OperationType | (missing) | tinyint(1) | target-only |
| dbo.MediaProvisioningOperations.RequestedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.MediaProvisioningOperations.SafeErrorCode | (missing) | nvarchar(200) | target-only |
| dbo.MediaProvisioningOperations.Status | (missing) | tinyint(1) | target-only |
| dbo.NotificationDeliveries.AttemptedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.NotificationDeliveries.AttemptNumber | (missing) | int(4) | target-only |
| dbo.NotificationDeliveries.CompletedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.NotificationDeliveries.DeliveryId | (missing) | uniqueidentifier(16) | target-only |
| dbo.NotificationDeliveries.Provider | (missing) | nvarchar(160) | target-only |
| dbo.NotificationDeliveries.ProviderReference | (missing) | nvarchar(400) | target-only |
| dbo.NotificationDeliveries.RecipientId | (missing) | uniqueidentifier(16) | target-only |
| dbo.NotificationDeliveries.SafeErrorCode | (missing) | nvarchar(200) | target-only |
| dbo.NotificationDeliveries.Status | (missing) | tinyint(1) | target-only |
| dbo.NotificationMessages.Body | (missing) | nvarchar(8000) | target-only |
| dbo.NotificationMessages.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.NotificationMessages.Channel | (missing) | tinyint(1) | target-only |
| dbo.NotificationMessages.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.NotificationMessages.ExpiresAtUtc | (missing) | datetime2(7) | target-only |
| dbo.NotificationMessages.LegacyNotificationId | (missing) | nvarchar(256) | target-only |
| dbo.NotificationMessages.MessageId | (missing) | uniqueidentifier(16) | target-only |
| dbo.NotificationMessages.Origin | (missing) | nvarchar(160) | target-only |
| dbo.NotificationMessages.Title | (missing) | nvarchar(500) | target-only |
| dbo.NotificationPreferences.Channel | (missing) | tinyint(1) | target-only |
| dbo.NotificationPreferences.IsEnabled | (missing) | bit(1) | target-only |
| dbo.NotificationPreferences.PreferenceId | (missing) | uniqueidentifier(16) | target-only |
| dbo.NotificationPreferences.SubjectReference | (missing) | nvarchar(320) | target-only |
| dbo.NotificationPreferences.SubjectType | (missing) | tinyint(1) | target-only |
| dbo.NotificationPreferences.UpdatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.NotificationReadStates.DismissedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.NotificationReadStates.ReadAtUtc | (missing) | datetime2(7) | target-only |
| dbo.NotificationReadStates.ReadStateId | (missing) | uniqueidentifier(16) | target-only |
| dbo.NotificationReadStates.RecipientId | (missing) | uniqueidentifier(16) | target-only |
| dbo.NotificationRecipients.ApartmentId | (missing) | nvarchar(128) | target-only |
| dbo.NotificationRecipients.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.NotificationRecipients.MessageId | (missing) | uniqueidentifier(16) | target-only |
| dbo.NotificationRecipients.RecipientId | (missing) | uniqueidentifier(16) | target-only |
| dbo.NotificationRecipients.RecipientReference | (missing) | nvarchar(320) | target-only |
| dbo.NotificationRecipients.RecipientType | (missing) | tinyint(1) | target-only |
| dbo.OutboxMessages.AggregateReference | (missing) | nvarchar(400) | target-only |
| dbo.OutboxMessages.AggregateType | (missing) | nvarchar(240) | target-only |
| dbo.OutboxMessages.Attempts | (missing) | int(4) | target-only |
| dbo.OutboxMessages.AvailableAtUtc | (missing) | datetime2(7) | target-only |
| dbo.OutboxMessages.IdempotencyKey | (missing) | nvarchar(400) | target-only |
| dbo.OutboxMessages.LastSafeErrorCode | (missing) | nvarchar(200) | target-only |
| dbo.OutboxMessages.MessageType | (missing) | nvarchar(320) | target-only |
| dbo.OutboxMessages.OccurredAtUtc | (missing) | datetime2(7) | target-only |
| dbo.OutboxMessages.OutboxMessageId | (missing) | uniqueidentifier(16) | target-only |
| dbo.OutboxMessages.PayloadJson | (missing) | nvarchar(-1) | target-only |
| dbo.OutboxMessages.ProcessedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PaymentDeposits.Amount | (missing) | decimal(9) | target-only |
| dbo.PaymentDeposits.ApartmentId | (missing) | nvarchar(128) | target-only |
| dbo.PaymentDeposits.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.PaymentDeposits.CancelledAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PaymentDeposits.CancelledByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PaymentDeposits.ConfirmedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PaymentDeposits.ConfirmedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PaymentDeposits.CurrencyCode | (missing) | char(3) | target-only |
| dbo.PaymentDeposits.PaymentDepositId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PaymentDeposits.PeriodId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PaymentDeposits.PostedTransactionId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PaymentDeposits.ResolutionReason | (missing) | nvarchar(1000) | target-only |
| dbo.PaymentDeposits.ReversalTransactionId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PaymentDeposits.ReversedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PaymentDeposits.ReversedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PaymentDeposits.RowVersion | (missing) | timestamp(8) | target-only |
| dbo.PaymentDeposits.SourceKey | (missing) | nvarchar(512) | target-only |
| dbo.PaymentDeposits.SourceSystem | (missing) | nvarchar(160) | target-only |
| dbo.PaymentDeposits.Status | (missing) | tinyint(1) | target-only |
| dbo.PaymentDeposits.SubmittedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.Persons.ArchivedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.Persons.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.Persons.DisplayName | (missing) | nvarchar(320) | target-only |
| dbo.Persons.IsArchived | (missing) | bit(1) | target-only |
| dbo.Persons.LegalName | (missing) | nvarchar(400) | target-only |
| dbo.Persons.PersonId | (missing) | uniqueidentifier(16) | target-only |
| dbo.Persons.PhotoUrl | (missing) | nvarchar(4096) | target-only |
| dbo.Persons.PrimaryEmail | (missing) | nvarchar(508) | target-only |
| dbo.Persons.PrimaryPhone | (missing) | nvarchar(64) | target-only |
| dbo.Persons.RowVersion | (missing) | timestamp(8) | target-only |
| dbo.Persons.UpdatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.Persons.Whatsapp | (missing) | nvarchar(64) | target-only |
| dbo.PollBallots.BallotId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PollBallots.BallotTokenHash | (missing) | varbinary(64) | target-only |
| dbo.PollBallots.BallotVersion | (missing) | int(4) | target-only |
| dbo.PollBallots.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.PollBallots.CastAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PollBallots.ChoicePayload | (missing) | varbinary(-1) | target-only |
| dbo.PollBallots.PollId | (missing) | nvarchar(128) | target-only |
| dbo.PollDelegations.ApartmentId | (missing) | nvarchar(128) | target-only |
| dbo.PollDelegations.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.PollDelegations.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PollDelegations.DelegatePersonId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PollDelegations.DelegationId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PollDelegations.GrantedByPersonId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PollDelegations.PollId | (missing) | nvarchar(128) | target-only |
| dbo.PollDelegations.ValidFromUtc | (missing) | datetime2(7) | target-only |
| dbo.PollDelegations.ValidToUtc | (missing) | datetime2(7) | target-only |
| dbo.PollEligibility.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.PollEligibility.ConsumedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PollEligibility.CredentialHash | (missing) | varbinary(64) | target-only |
| dbo.PollEligibility.EligibilityId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PollEligibility.ExpiresAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PollEligibility.IssuedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PollEligibility.PollId | (missing) | nvarchar(128) | target-only |
| dbo.PollEligibility.RevokedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PollEligibility.SubjectReference | (missing) | nvarchar(320) | target-only |
| dbo.PollLifecycleEvents.ActorReference | (missing) | nvarchar(320) | target-only |
| dbo.PollLifecycleEvents.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.PollLifecycleEvents.EventId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PollLifecycleEvents.EventType | (missing) | tinyint(1) | target-only |
| dbo.PollLifecycleEvents.MetadataJson | (missing) | nvarchar(-1) | target-only |
| dbo.PollLifecycleEvents.OccurredAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PollLifecycleEvents.PollId | (missing) | nvarchar(128) | target-only |
| dbo.PollLifecycleEvents.Reason | (missing) | nvarchar(2000) | target-only |
| dbo.PollVotingPolicies.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.PollVotingPolicies.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PollVotingPolicies.EligibilityMode | (missing) | tinyint(1) | target-only |
| dbo.PollVotingPolicies.IsActive | (missing) | bit(1) | target-only |
| dbo.PollVotingPolicies.PolicyId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PollVotingPolicies.PolicyVersion | (missing) | int(4) | target-only |
| dbo.PollVotingPolicies.PollId | (missing) | nvarchar(128) | target-only |
| dbo.PollVotingPolicies.VotingMode | (missing) | tinyint(1) | target-only |
| dbo.PresenceHeartbeats.HeartbeatId | (missing) | bigint(8) | target-only |
| dbo.PresenceHeartbeats.ObservedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PresenceHeartbeats.PresenceSessionId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PresenceHeartbeats.Signal | (missing) | tinyint(1) | target-only |
| dbo.PresenceSessions.AccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PresenceSessions.ApartmentId | (missing) | nvarchar(128) | target-only |
| dbo.PresenceSessions.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.PresenceSessions.DeviceReference | (missing) | nvarchar(400) | target-only |
| dbo.PresenceSessions.EndedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PresenceSessions.ExpiresAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PresenceSessions.LastSeenAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PresenceSessions.LegacyUid | (missing) | nvarchar(256) | target-only |
| dbo.PresenceSessions.PresenceSessionId | (missing) | uniqueidentifier(16) | target-only |
| dbo.PresenceSessions.StartedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.PresenceSessions.Status | (missing) | tinyint(1) | target-only |
| dbo.ProtectedIntegrationSecrets.Ciphertext | (missing) | varbinary(-1) | target-only |
| dbo.ProtectedIntegrationSecrets.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.ProtectedIntegrationSecrets.KeyVersion | (missing) | nvarchar(200) | target-only |
| dbo.ProtectedIntegrationSecrets.Provider | (missing) | nvarchar(200) | target-only |
| dbo.ProtectedIntegrationSecrets.RevokedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.ProtectedIntegrationSecrets.RotatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.ProtectedIntegrationSecrets.ScopeReference | (missing) | nvarchar(320) | target-only |
| dbo.ProtectedIntegrationSecrets.ScopeType | (missing) | tinyint(1) | target-only |
| dbo.ProtectedIntegrationSecrets.SecretId | (missing) | uniqueidentifier(16) | target-only |
| dbo.ProtectedIntegrationSecrets.SecretName | (missing) | nvarchar(320) | target-only |
| dbo.QrCapabilities.ApartmentId | (missing) | nvarchar(128) | target-only |
| dbo.QrCapabilities.BuildingId | (missing) | nvarchar(128) | target-only |
| dbo.QrCapabilities.CapabilityId | (missing) | uniqueidentifier(16) | target-only |
| dbo.QrCapabilities.ExpiresAtUtc | (missing) | datetime2(7) | target-only |
| dbo.QrCapabilities.IssuedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.QrCapabilities.MaxUses | (missing) | int(4) | target-only |
| dbo.QrCapabilities.RevokedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.QrCapabilities.RevokedByReference | (missing) | nvarchar(320) | target-only |
| dbo.QrCapabilities.TargetReference | (missing) | nvarchar(320) | target-only |
| dbo.QrCapabilities.TargetType | (missing) | tinyint(1) | target-only |
| dbo.QrCapabilities.TokenHash | (missing) | varbinary(64) | target-only |
| dbo.QrCapabilityUses.CapabilityId | (missing) | uniqueidentifier(16) | target-only |
| dbo.QrCapabilityUses.Outcome | (missing) | tinyint(1) | target-only |
| dbo.QrCapabilityUses.UsedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.QrCapabilityUses.UseId | (missing) | uniqueidentifier(16) | target-only |
| dbo.QrCapabilityUses.UseReference | (missing) | nvarchar(320) | target-only |
| dbo.RestoreOperations.ApprovedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.RestoreOperations.ApprovedByReference | (missing) | nvarchar(320) | target-only |
| dbo.RestoreOperations.BackupArtifactId | (missing) | uniqueidentifier(16) | target-only |
| dbo.RestoreOperations.CompletedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.RestoreOperations.RequestedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.RestoreOperations.RestoreOperationId | (missing) | uniqueidentifier(16) | target-only |
| dbo.RestoreOperations.SafeErrorCode | (missing) | nvarchar(200) | target-only |
| dbo.RestoreOperations.StartedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.RestoreOperations.Status | (missing) | tinyint(1) | target-only |
| dbo.RestoreOperations.TargetDatabase | (missing) | nvarchar(400) | target-only |
| dbo.RoleDefinitions.DisplayName | (missing) | nvarchar(240) | target-only |
| dbo.RoleDefinitions.IsActive | (missing) | bit(1) | target-only |
| dbo.RoleDefinitions.IsSystemRole | (missing) | bit(1) | target-only |
| dbo.RoleDefinitions.RoleKey | (missing) | nvarchar(128) | target-only |
| dbo.RolePermissions.Effect | (missing) | tinyint(1) | target-only |
| dbo.RolePermissions.PermissionKey | (missing) | nvarchar(200) | target-only |
| dbo.RolePermissions.RoleKey | (missing) | nvarchar(128) | target-only |
| dbo.UserAccounts.AccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.UserAccounts.AccountStatus | (missing) | tinyint(1) | target-only |
| dbo.UserAccounts.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.UserAccounts.ExternalProvider | (missing) | nvarchar(80) | target-only |
| dbo.UserAccounts.ExternalSubject | (missing) | nvarchar(400) | target-only |
| dbo.UserAccounts.LastLoginAtUtc | (missing) | datetime2(7) | target-only |
| dbo.UserAccounts.NormalizedEmail | (missing) | nvarchar(508) | target-only |
| dbo.UserAccounts.PersonId | (missing) | uniqueidentifier(16) | target-only |
| dbo.UserAccounts.RowVersion | (missing) | timestamp(8) | target-only |
| dbo.UserAccounts.SecurityStamp | (missing) | nvarchar(256) | target-only |
| dbo.UserCredentials.AccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.UserCredentials.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.UserCredentials.CredentialHash | (missing) | nvarchar(1024) | target-only |
| dbo.UserCredentials.CredentialId | (missing) | uniqueidentifier(16) | target-only |
| dbo.UserCredentials.CredentialType | (missing) | tinyint(1) | target-only |
| dbo.UserCredentials.ExpiresAtUtc | (missing) | datetime2(7) | target-only |
| dbo.UserCredentials.HashAlgorithm | (missing) | nvarchar(80) | target-only |
| dbo.UserCredentials.ReplacedByCredentialId | (missing) | uniqueidentifier(16) | target-only |
| dbo.UserCredentials.RevokedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.UserCredentials.RowVersion | (missing) | timestamp(8) | target-only |
| dbo.UserPreferences.AccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.UserPreferences.PreferenceId | (missing) | uniqueidentifier(16) | target-only |
| dbo.UserPreferences.PreferenceJson | (missing) | nvarchar(4000) | target-only |
| dbo.UserPreferences.PreferenceKey | (missing) | nvarchar(240) | target-only |
| dbo.UserPreferences.UpdatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.UserRecoveryCodes.AccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.UserRecoveryCodes.CodeHash | (missing) | varbinary(64) | target-only |
| dbo.UserRecoveryCodes.CreatedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.UserRecoveryCodes.RecoveryCodeId | (missing) | uniqueidentifier(16) | target-only |
| dbo.UserRecoveryCodes.UsedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.UserRoleAssignments.AccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.UserRoleAssignments.GrantedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.UserRoleAssignments.GrantedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.UserRoleAssignments.GrantReason | (missing) | nvarchar(1000) | target-only |
| dbo.UserRoleAssignments.RevocationReason | (missing) | nvarchar(1000) | target-only |
| dbo.UserRoleAssignments.RevokedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.UserRoleAssignments.RevokedByAccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.UserRoleAssignments.RoleAssignmentId | (missing) | uniqueidentifier(16) | target-only |
| dbo.UserRoleAssignments.RoleKey | (missing) | nvarchar(128) | target-only |
| dbo.UserRoleAssignments.RowVersion | (missing) | timestamp(8) | target-only |
| dbo.UserRoleAssignments.ScopeApartmentId | (missing) | nvarchar(128) | target-only |
| dbo.UserRoleAssignments.ScopeBuildingId | (missing) | nvarchar(128) | target-only |
| dbo.UserRoleAssignments.ScopeType | (missing) | tinyint(1) | target-only |
| dbo.UserRoleAssignments.ValidFromUtc | (missing) | datetime2(7) | target-only |
| dbo.UserRoleAssignments.ValidToUtc | (missing) | datetime2(7) | target-only |
| dbo.UserTwoFactorSettings.AccountId | (missing) | uniqueidentifier(16) | target-only |
| dbo.UserTwoFactorSettings.EnrolledAtUtc | (missing) | datetime2(7) | target-only |
| dbo.UserTwoFactorSettings.IsEnabled | (missing) | bit(1) | target-only |
| dbo.UserTwoFactorSettings.KeyVersion | (missing) | nvarchar(128) | target-only |
| dbo.UserTwoFactorSettings.LastAcceptedTimeStep | (missing) | bigint(8) | target-only |
| dbo.UserTwoFactorSettings.LastVerifiedAtUtc | (missing) | datetime2(7) | target-only |
| dbo.UserTwoFactorSettings.RowVersion | (missing) | timestamp(8) | target-only |
| dbo.UserTwoFactorSettings.SecretCiphertext | (missing) | varbinary(-1) | target-only |
| dbo.UserTwoFactorSettings.UpdatedAtUtc | (missing) | datetime2(7) | target-only |

## Foreign-key relationship counts

- Source FK relationship rows: 48
- Target FK relationship rows: 150

## Interpretation and safety

- This phase is strictly read-only. It does not modify either database.
- A matching row count is not proof of matching records. This report does not yet compare primary-key sets or row fingerprints.
- TARGET_ONLY tables may be newly introduced target features and are not automatically migration defects.
- SOURCE_ONLY tables and every row-count/schema difference require classification and a dedicated migration or immutable archive strategy.
- Do not retire or delete the legacy source database based on this report alone.
- The next reconciliation phase must compare stable source keys and SHA-256 row fingerprints, including child relationships and records preserved as immutable legacy evidence.
- The source database is treated as migration-only. All source records must be represented in the new target either operationally or as immutable, provenance-preserving evidence.
