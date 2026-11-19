# Phase 47 â€” Row-level source/target migration reconciliation

- Run: 2026-11-19T04:38:15.7505964+02:00
- Source: BuildingManagement
- Target: BuildingManagement_RestoreTest
- Comparison: primary-key identity plus SHA-256 of a deterministic SQL JSON projection over columns shared by both schemas.
- System-generated rowversion/timestamp columns are excluded from content fingerprints.
- No row payloads, personal data, keys, or hashes are written to this report.
- This phase is read-only; neither database is modified.

## Results

| Table | Source rows | Target rows | Matched keys | Missing in target | Target-only | Content differences | Status |
|---|---:|---:|---:|---:|---:|---:|---|
| dbo.__EFMigrationsHistory | 4 | 19 | 4 | 0 | 15 | 0 | SOURCE_PRESERVED_WITH_TARGET_ADDITIONS |
| dbo.Apartments | 79 | 79 | 79 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.AuditLog | 1050 | 1053 | 1050 | 0 | 3 | 0 | SOURCE_PRESERVED_WITH_TARGET_ADDITIONS |
| dbo.BackupHistory | 1 | 2 | 1 | 0 | 1 | 0 | SOURCE_PRESERVED_WITH_TARGET_ADDITIONS |
| dbo.BuildingAdmins | 2 | 2 | 2 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.Buildings | 3 | 3 | 3 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.BuildingSettings | 0 | 0 | 0 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.Cameras | 3 | 3 | 3 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.DepositEditHistory | 1 | 1 | 1 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.Deposits | 26 | 26 | 26 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.Dvrs | 1 | 1 | 1 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.Expenses | 12 | 12 | 12 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.FinancialCategories | 38 | 38 | 38 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.Floors | 16 | 16 | 16 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.GlobalSettings | 1 | 1 | 1 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.LoginThrottle | 0 | 0 | 0 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.MaintenanceRecords | 1 | 1 | 1 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.MonthlyApartmentShares | 113 | 113 | 113 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.Notifications | 68 | 68 | 68 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.PollOptions | 2 | 2 | 2 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.Polls | 1 | 1 | 1 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.PollVotes | 1 | 1 | 1 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.Presence | 9 | 10 | 9 | 0 | 1 | 1 | SOURCE_DATA_MISSING_OR_CHANGED |
| dbo.QrTokens | 9 | 9 | 9 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.QrUsages | 5 | 5 | 5 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.Residents | 79 | 79 | 79 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.Revenues | 4 | 4 | 4 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.ScheduledBackupSettings | 1 | 1 | 1 | 0 | 0 | 1 | SOURCE_DATA_MISSING_OR_CHANGED |
| dbo.SyncPendingChanges | 0 | 0 | 0 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.UserPermissions | 81 | 81 | 81 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.Users | 5 | 5 | 5 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |
| dbo.WalletAdjustments | 2 | 2 | 2 | 0 | 0 | 0 | EXACT_SOURCE_RECONCILIATION |

## Aggregate findings

- Missing source rows in target: 0
- Content differences on matched primary keys: 2
- Additional target rows: 20
- Tables not row-level verifiable due to primary-key/schema limitations: 0

## Important limitations

- Hashes cover columns shared by name; schema-only target columns are not part of the source-to-target content hash.
- Different SQL types or serialization semantics on shared columns may require table-specific normalization before treating a hash difference as a true data change.
- Additional target rows are not automatically defects; they may be newly created target records.
- __EFMigrationsHistory naturally contains target migration rows absent from the legacy source and must be classified separately.
- No source records are updated or deleted. Do not retire the legacy database until every source table is reconciled and all differences are classified.
