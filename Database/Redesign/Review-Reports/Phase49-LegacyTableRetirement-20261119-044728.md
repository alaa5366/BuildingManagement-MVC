# Phase 49 â€” Legacy table retirement

- Run: 2026-11-19T04:47:29.1859259+02:00
- Mode: APPLY
- Scope: BuildingManagement_RestoreTest only
- Source database BuildingManagement is read-only and never modified.

## Table results

| Table | Source rows | Target rows | Migration map rows | Status |
|---|---:|---:|---:|---|
| dbo.__EFMigrationsHistory | 0 | 19 | 0 | RETAIN_EF_METADATA |
| dbo.Apartments | 0 | 79 | 0 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.AuditLog | 0 | 1053 | 1053 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.BackupHistory | 0 | 2 | 2 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.BuildingAdmins | 0 | 2 | 2 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.Buildings | 0 | 3 | 0 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.BuildingSettings | 0 | 0 | 0 | READY_FOR_REVIEW |
| dbo.Cameras | 0 | 3 | 3 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.DepositEditHistory | 0 | 1 | 1 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.Deposits | 0 | 26 | 26 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.Dvrs | 0 | 1 | 1 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.Expenses | 0 | 12 | 12 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.FinancialCategories | 0 | 38 | 0 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.Floors | 0 | 16 | 0 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.GlobalSettings | 0 | 1 | 0 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.LoginThrottle | 0 | 0 | 0 | READY_FOR_REVIEW |
| dbo.MaintenanceRecords | 0 | 1 | 1 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.MonthlyApartmentShares | 0 | 113 | 113 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.Notifications | 0 | 68 | 68 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.PollOptions | 0 | 2 | 2 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.Polls | 0 | 1 | 1 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.PollVotes | 0 | 1 | 1 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.Presence | 0 | 10 | 10 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.QrTokens | 0 | 9 | 9 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.QrUsages | 0 | 5 | 5 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.Residents | 0 | 79 | 79 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.Revenues | 0 | 4 | 4 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.ScheduledBackupSettings | 0 | 1 | 1 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.SyncPendingChanges | 0 | 0 | 0 | READY_FOR_REVIEW |
| dbo.UserPermissions | 0 | 81 | 81 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.Users | 0 | 5 | 5 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |
| dbo.WalletAdjustments | 0 | 2 | 2 | BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF |

## Execution log

- Source tables: 32; target tables: 97
- Legacy tables eligible for retirement review: 3
- Foreign-key dependencies touching legacy tables: 45
- Safety blockers: 73
- dbo.__EFMigrationsHistory: source=0; target=19; map=0; RETAIN_EF_METADATA
- dbo.Apartments: source=0; target=79; map=0; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.AuditLog: source=0; target=1053; map=1053; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.BackupHistory: source=0; target=2; map=2; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.BuildingAdmins: source=0; target=2; map=2; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.Buildings: source=0; target=3; map=0; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.BuildingSettings: source=0; target=0; map=0; READY_FOR_REVIEW
- dbo.Cameras: source=0; target=3; map=3; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.DepositEditHistory: source=0; target=1; map=1; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.Deposits: source=0; target=26; map=26; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.Dvrs: source=0; target=1; map=1; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.Expenses: source=0; target=12; map=12; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.FinancialCategories: source=0; target=38; map=0; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.Floors: source=0; target=16; map=0; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.GlobalSettings: source=0; target=1; map=0; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.LoginThrottle: source=0; target=0; map=0; READY_FOR_REVIEW
- dbo.MaintenanceRecords: source=0; target=1; map=1; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.MonthlyApartmentShares: source=0; target=113; map=113; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.Notifications: source=0; target=68; map=68; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.PollOptions: source=0; target=2; map=2; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.Polls: source=0; target=1; map=1; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.PollVotes: source=0; target=1; map=1; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.Presence: source=0; target=10; map=10; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.QrTokens: source=0; target=9; map=9; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.QrUsages: source=0; target=5; map=5; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.Residents: source=0; target=79; map=79; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.Revenues: source=0; target=4; map=4; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.ScheduledBackupSettings: source=0; target=1; map=1; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.SyncPendingChanges: source=0; target=0; map=0; READY_FOR_REVIEW
- dbo.UserPermissions: source=0; target=81; map=81; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.Users: source=0; target=5; map=5; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- dbo.WalletAdjustments: source=0; target=2; map=2; BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF
- BLOCKER: dbo.Apartments contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.AuditLog contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.BackupHistory contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.BuildingAdmins contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.Buildings contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.Cameras contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.DepositEditHistory contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.Deposits contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.Dvrs contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.Expenses contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.FinancialCategories contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.Floors contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.GlobalSettings contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.MaintenanceRecords contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.MonthlyApartmentShares contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.Notifications contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.PollOptions contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.Polls contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.PollVotes contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.Presence contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.QrTokens contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.QrUsages contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.Residents contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.Revenues contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.ScheduledBackupSettings contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.UserPermissions contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.Users contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: dbo.WalletAdjustments contains  non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.
- BLOCKER: Foreign key FK_WalletAdjustments_Apartments_BuildingId_ApartmentId still connects legacy table dbo.WalletAdjustments to dbo.Apartments.
- BLOCKER: Foreign key FK_FinancialReconciliationDifferences_Apartments_BuildingId_ApartmentId still connects legacy table dbo.FinancialReconciliationDifferences to dbo.Apartments.
- BLOCKER: Foreign key FK_AccountPermissionOverrides_Apartments_ScopeBuildingId_ScopeApartmentId still connects legacy table dbo.AccountPermissionOverrides to dbo.Apartments.
- BLOCKER: Foreign key FK_ApartmentAccessGrants_Apartments_BuildingId_ApartmentId still connects legacy table dbo.ApartmentAccessGrants to dbo.Apartments.
- BLOCKER: Foreign key FK_LedgerEntries_Apartments_BuildingId_ApartmentId still connects legacy table dbo.LedgerEntries to dbo.Apartments.
- BLOCKER: Foreign key FK_ApartmentOccupancies_Apartments_BuildingId_ApartmentId still connects legacy table dbo.ApartmentOccupancies to dbo.Apartments.
- BLOCKER: Foreign key FK_PaymentDeposits_Apartments_BuildingId_ApartmentId still connects legacy table dbo.PaymentDeposits to dbo.Apartments.
- BLOCKER: Foreign key FK_ApartmentOwnerships_Apartments_BuildingId_ApartmentId still connects legacy table dbo.ApartmentOwnerships to dbo.Apartments.
- BLOCKER: Foreign key FK_FinancialAllocationLines_Apartments_BuildingId_ApartmentId still connects legacy table dbo.FinancialAllocationLines to dbo.Apartments.
- BLOCKER: Foreign key FK_UserRoleAssignments_Apartments_ScopeBuildingId_ScopeApartmentId still connects legacy table dbo.UserRoleAssignments to dbo.Apartments.
- BLOCKER: Foreign key FK_Deposits_Apartments_BuildingId_ApartmentId still connects legacy table dbo.Deposits to dbo.Apartments.
- BLOCKER: Foreign key FK_MonthlyApartmentShares_Apartments_BuildingId_ApartmentId still connects legacy table dbo.MonthlyApartmentShares to dbo.Apartments.
- BLOCKER: Foreign key FK_Notifications_Apartments_BuildingId_RecipientApartmentId still connects legacy table dbo.Notifications to dbo.Apartments.
- BLOCKER: Foreign key FK_PollVotes_Apartments_BuildingId_ApartmentId still connects legacy table dbo.PollVotes to dbo.Apartments.
- BLOCKER: Foreign key FK_QrTokens_Apartments_BuildingId_ApartmentId still connects legacy table dbo.QrTokens to dbo.Apartments.
- BLOCKER: Foreign key FK_Residents_Apartments_BuildingId_ApartmentId still connects legacy table dbo.Residents to dbo.Apartments.
- BLOCKER: Foreign key FK_PollVotes_PollOptions_BuildingId_PollId_OptionId still connects legacy table dbo.PollVotes to dbo.PollOptions.
- BLOCKER: Foreign key FK_DepositEditHistory_Deposits_BuildingId_DepositId still connects legacy table dbo.DepositEditHistory to dbo.Deposits.
- BLOCKER: Foreign key FK_FinancialPeriods_Buildings_BuildingId still connects legacy table dbo.FinancialPeriods to dbo.Buildings.
- BLOCKER: Foreign key FK_WalletAdjustments_Buildings_BuildingId still connects legacy table dbo.WalletAdjustments to dbo.Buildings.
- BLOCKER: Foreign key FK_FinancialReconciliationDifferences_Buildings_BuildingId still connects legacy table dbo.FinancialReconciliationDifferences to dbo.Buildings.
- BLOCKER: Foreign key FK_AccountPermissionOverrides_Buildings_ScopeBuildingId still connects legacy table dbo.AccountPermissionOverrides to dbo.Buildings.
- BLOCKER: Foreign key FK_UserRoleAssignments_Buildings_ScopeBuildingId still connects legacy table dbo.UserRoleAssignments to dbo.Buildings.
- BLOCKER: Foreign key FK_Apartments_Buildings_BuildingId still connects legacy table dbo.Apartments to dbo.Buildings.
- BLOCKER: Foreign key FK_AuditLog_Buildings_BuildingId still connects legacy table dbo.AuditLog to dbo.Buildings.
- BLOCKER: Foreign key FK_BuildingAdmins_Buildings_BuildingId still connects legacy table dbo.BuildingAdmins to dbo.Buildings.
- BLOCKER: Foreign key FK_BuildingSettings_Buildings_BuildingId still connects legacy table dbo.BuildingSettings to dbo.Buildings.
- BLOCKER: Foreign key FK_Deposits_Buildings_BuildingId still connects legacy table dbo.Deposits to dbo.Buildings.
- BLOCKER: Foreign key FK_Dvrs_Buildings_BuildingId still connects legacy table dbo.Dvrs to dbo.Buildings.
- BLOCKER: Foreign key FK_Expenses_Buildings_BuildingId still connects legacy table dbo.Expenses to dbo.Buildings.
- BLOCKER: Foreign key FK_FinancialCategories_Buildings_BuildingId still connects legacy table dbo.FinancialCategories to dbo.Buildings.
- BLOCKER: Foreign key FK_Floors_Buildings_BuildingId still connects legacy table dbo.Floors to dbo.Buildings.
- BLOCKER: Foreign key FK_MaintenanceRecords_Buildings_BuildingId still connects legacy table dbo.MaintenanceRecords to dbo.Buildings.
- BLOCKER: Foreign key FK_MonthlyApartmentShares_Buildings_BuildingId still connects legacy table dbo.MonthlyApartmentShares to dbo.Buildings.
- BLOCKER: Foreign key FK_Notifications_Buildings_BuildingId still connects legacy table dbo.Notifications to dbo.Buildings.
- BLOCKER: Foreign key FK_Polls_Buildings_BuildingId still connects legacy table dbo.Polls to dbo.Buildings.
- BLOCKER: Foreign key FK_QrTokens_Buildings_BuildingId still connects legacy table dbo.QrTokens to dbo.Buildings.
- BLOCKER: Foreign key FK_Revenues_Buildings_BuildingId still connects legacy table dbo.Revenues to dbo.Buildings.
- BLOCKER: Foreign key FK_UserPermissions_Users_Uid still connects legacy table dbo.UserPermissions to dbo.Users.
- BLOCKER: Foreign key FK_BuildingAdmins_Users_AdminUid still connects legacy table dbo.BuildingAdmins to dbo.Users.
- BLOCKER: Foreign key FK_Cameras_Dvrs_DvrId still connects legacy table dbo.Cameras to dbo.Dvrs.
- BLOCKER: Foreign key FK_Revenues_FinancialCategories_BuildingId_CategoryKind_CategoryId still connects legacy table dbo.Revenues to dbo.FinancialCategories.
- BLOCKER: Foreign key FK_Expenses_FinancialCategories_BuildingId_CategoryKind_CategoryId still connects legacy table dbo.Expenses to dbo.FinancialCategories.
- BLOCKER: Foreign key FK_Apartments_Floors_BuildingId_FloorId still connects legacy table dbo.Apartments to dbo.Floors.
- BLOCKER: Foreign key FK_PollOptions_Polls_BuildingId_PollId still connects legacy table dbo.PollOptions to dbo.Polls.
- ERROR: Retirement stopped safely: 73 blocker(s). No table was dropped.

## Backup

- Backup file: Not created
- Tables dropped: 0

## Safety notes

- The script fails closed. Any missing target table, incomplete migration-map coverage, invalid target mapping, or foreign-key dependency prevents all drops.
- __EFMigrationsHistory is deliberately retained as Entity Framework deployment metadata.
- Target-only operational tables are never selected for deletion.
- A migration-map count is a conservative gate, not a cryptographic proof of payload equality; the Phase 47 fingerprints and table-specific migration reports remain required evidence.
- If any blocker is reported, resolve it before retrying. No source table or source row is ever changed.
