# Phase 50 â€” Exact source/target database count verification

- Run: 2026-11-19T04:50:57.6427660+02:00
- Mode: READ ONLY

## Connection identity

| Property | Source | Target |
|---|---|---|
| Database | BuildingManagement | BuildingManagement_RestoreTest |
| Server | ZOS-AAM\LOCALDB#D33C7E97 | ZOS-AAM\LOCALDB#D33C7E97 |
| Database ID | 7 | 6 |
| Database user | dbo | dbo |
| State | ONLINE | ONLINE |
| Created | 11/17/2026 23:36:59 | 11/16/2026 14:08:44 |
| Primary data file | C:\Users\alaa5\BuildingManagement.mdf | C:\Users\alaa5\BuildingManagement_RestoreTest.mdf |
| User tables | 32 | 97 |
| Total rows (COUNT_BIG per table) | 1618 | 5119 |

## Table-by-table exact counts

| Table | Source rows | Target rows | Delta | Status |
|---|---:|---:|---:|---|
| dbo.__EFMigrationsHistory | 4 | 19 | 15 | COUNT_MISMATCH |
| dbo.AccountPermissionOverrides | 0 | 0 | 0 | TARGET_ONLY |
| dbo.ApartmentAccessGrants | 0 | 0 | 0 | TARGET_ONLY |
| dbo.ApartmentOccupancies | 0 | 0 | 0 | TARGET_ONLY |
| dbo.ApartmentOwnerships | 0 | 0 | 0 | TARGET_ONLY |
| dbo.Apartments | 79 | 79 | 0 | COUNT_MATCH |
| dbo.AuditEvents | 0 | 1053 | 1053 | TARGET_ONLY |
| dbo.AuditLog | 1050 | 1053 | 3 | COUNT_MISMATCH |
| dbo.BackupArtifacts | 0 | 0 | 0 | TARGET_ONLY |
| dbo.BackupHistory | 1 | 2 | 1 | COUNT_MISMATCH |
| dbo.BackupJobs | 0 | 2 | 2 | TARGET_ONLY |
| dbo.BackupRetentionEvents | 0 | 0 | 0 | TARGET_ONLY |
| dbo.BackupScheduleConfigurations | 0 | 1 | 1 | TARGET_ONLY |
| dbo.BuildingAdmins | 2 | 2 | 0 | COUNT_MATCH |
| dbo.Buildings | 3 | 3 | 0 | COUNT_MATCH |
| dbo.BuildingSettings | 0 | 0 | 0 | COUNT_MATCH |
| dbo.Cameras | 3 | 3 | 0 | COUNT_MATCH |
| dbo.DepositEditHistory | 1 | 1 | 0 | COUNT_MATCH |
| dbo.Deposits | 26 | 26 | 0 | COUNT_MATCH |
| dbo.DocumentAccessGrants | 0 | 0 | 0 | TARGET_ONLY |
| dbo.DocumentAssets | 0 | 0 | 0 | TARGET_ONLY |
| dbo.Dvrs | 1 | 1 | 0 | COUNT_MATCH |
| dbo.Expenses | 12 | 12 | 0 | COUNT_MATCH |
| dbo.ExportJobs | 0 | 0 | 0 | TARGET_ONLY |
| dbo.FileProcessingJobs | 0 | 0 | 0 | TARGET_ONLY |
| dbo.FinancialAllocationBatches | 0 | 0 | 0 | TARGET_ONLY |
| dbo.FinancialAllocationLines | 0 | 0 | 0 | TARGET_ONLY |
| dbo.FinancialCategories | 38 | 38 | 0 | COUNT_MATCH |
| dbo.FinancialPeriods | 0 | 4 | 4 | TARGET_ONLY |
| dbo.FinancialReconciliationDifferences | 0 | 2 | 2 | TARGET_ONLY |
| dbo.FinancialReconciliationRuns | 0 | 1 | 1 | TARGET_ONLY |
| dbo.FinancialSourceLinks | 0 | 26 | 26 | TARGET_ONLY |
| dbo.Floors | 16 | 16 | 0 | COUNT_MATCH |
| dbo.GlobalApplicationSettings | 0 | 1 | 1 | TARGET_ONLY |
| dbo.GlobalSettings | 1 | 1 | 0 | COUNT_MATCH |
| dbo.GuestAccessSessions | 0 | 0 | 0 | TARGET_ONLY |
| dbo.ImportBatches | 0 | 0 | 0 | TARGET_ONLY |
| dbo.ImportRows | 0 | 0 | 0 | TARGET_ONLY |
| dbo.InboxMessages | 0 | 0 | 0 | TARGET_ONLY |
| dbo.LedgerEntries | 0 | 136 | 136 | TARGET_ONLY |
| dbo.LedgerTransactions | 0 | 16 | 16 | TARGET_ONLY |
| dbo.LegacyDataMigrationMap | 0 | 1482 | 1482 | TARGET_ONLY |
| dbo.LegacyIdentityMap | 0 | 84 | 84 | TARGET_ONLY |
| dbo.LegacyOperationalRecords | 0 | 329 | 329 | TARGET_ONLY |
| dbo.LoginThrottle | 0 | 0 | 0 | COUNT_MATCH |
| dbo.MaintenanceAssignments | 0 | 0 | 0 | TARGET_ONLY |
| dbo.MaintenanceAttachments | 0 | 0 | 0 | TARGET_ONLY |
| dbo.MaintenanceComments | 0 | 0 | 0 | TARGET_ONLY |
| dbo.MaintenanceLedgerLinks | 0 | 0 | 0 | TARGET_ONLY |
| dbo.MaintenanceRecords | 1 | 1 | 0 | COUNT_MATCH |
| dbo.MaintenanceRequests | 0 | 1 | 1 | TARGET_ONLY |
| dbo.MaintenanceStatusEvents | 0 | 0 | 0 | TARGET_ONLY |
| dbo.MediaAccessSessions | 0 | 0 | 0 | TARGET_ONLY |
| dbo.MediaCredentialReferences | 0 | 0 | 0 | TARGET_ONLY |
| dbo.MediaProvisioningOperations | 0 | 0 | 0 | TARGET_ONLY |
| dbo.MonthlyApartmentShares | 113 | 113 | 0 | COUNT_MATCH |
| dbo.NotificationDeliveries | 0 | 0 | 0 | TARGET_ONLY |
| dbo.NotificationMessages | 0 | 68 | 68 | TARGET_ONLY |
| dbo.NotificationPreferences | 0 | 0 | 0 | TARGET_ONLY |
| dbo.NotificationReadStates | 0 | 35 | 35 | TARGET_ONLY |
| dbo.NotificationRecipients | 0 | 68 | 68 | TARGET_ONLY |
| dbo.Notifications | 68 | 68 | 0 | COUNT_MATCH |
| dbo.OutboxMessages | 0 | 0 | 0 | TARGET_ONLY |
| dbo.PaymentDeposits | 0 | 26 | 26 | TARGET_ONLY |
| dbo.Persons | 0 | 85 | 85 | TARGET_ONLY |
| dbo.PollBallots | 0 | 0 | 0 | TARGET_ONLY |
| dbo.PollDelegations | 0 | 0 | 0 | TARGET_ONLY |
| dbo.PollEligibility | 0 | 0 | 0 | TARGET_ONLY |
| dbo.PollLifecycleEvents | 0 | 1 | 1 | TARGET_ONLY |
| dbo.PollOptions | 2 | 2 | 0 | COUNT_MATCH |
| dbo.Polls | 1 | 1 | 0 | COUNT_MATCH |
| dbo.PollVotes | 1 | 1 | 0 | COUNT_MATCH |
| dbo.PollVotingPolicies | 0 | 0 | 0 | TARGET_ONLY |
| dbo.Presence | 9 | 10 | 1 | COUNT_MISMATCH |
| dbo.PresenceHeartbeats | 0 | 0 | 0 | TARGET_ONLY |
| dbo.PresenceSessions | 0 | 10 | 10 | TARGET_ONLY |
| dbo.ProtectedIntegrationSecrets | 0 | 0 | 0 | TARGET_ONLY |
| dbo.QrCapabilities | 0 | 9 | 9 | TARGET_ONLY |
| dbo.QrCapabilityUses | 0 | 0 | 0 | TARGET_ONLY |
| dbo.QrTokens | 9 | 9 | 0 | COUNT_MATCH |
| dbo.QrUsages | 5 | 5 | 0 | COUNT_MATCH |
| dbo.Residents | 79 | 79 | 0 | COUNT_MATCH |
| dbo.RestoreOperations | 0 | 0 | 0 | TARGET_ONLY |
| dbo.Revenues | 4 | 4 | 0 | COUNT_MATCH |
| dbo.RoleDefinitions | 0 | 2 | 2 | TARGET_ONLY |
| dbo.RolePermissions | 0 | 28 | 28 | TARGET_ONLY |
| dbo.ScheduledBackupSettings | 1 | 1 | 0 | COUNT_MATCH |
| dbo.SyncPendingChanges | 0 | 0 | 0 | COUNT_MATCH |
| dbo.UserAccounts | 0 | 6 | 6 | TARGET_ONLY |
| dbo.UserCredentials | 0 | 1 | 1 | TARGET_ONLY |
| dbo.UserPermissions | 81 | 81 | 0 | COUNT_MATCH |
| dbo.UserPreferences | 0 | 0 | 0 | TARGET_ONLY |
| dbo.UserRecoveryCodes | 0 | 0 | 0 | TARGET_ONLY |
| dbo.UserRoleAssignments | 0 | 4 | 4 | TARGET_ONLY |
| dbo.Users | 5 | 5 | 0 | COUNT_MATCH |
| dbo.UserTwoFactorSettings | 0 | 0 | 0 | TARGET_ONLY |
| dbo.WalletAdjustments | 2 | 2 | 0 | COUNT_MATCH |

## Safety conclusion

- This script executes COUNT_BIG(*) against every user table independently; it does not use partition-statistics estimates.
- No rows, schema, indexes, constraints, or database settings are modified.
- This report is diagnostic only and does not authorize legacy-table deletion.
