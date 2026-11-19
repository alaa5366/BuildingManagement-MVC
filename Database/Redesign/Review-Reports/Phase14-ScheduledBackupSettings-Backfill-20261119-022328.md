# Phase 14 - ScheduledBackupSettings Backfill

- Run: 2026-11-19T02:23:28.7831555+02:00
- Mode: APPLY
- Source rows retained: dbo.ScheduledBackupSettings

## Results
- Database: BuildingManagement_RestoreTest
- Source rows: 1
- Already mapped: 0
- Pending: 1
- Target configurations: 1
- Provenance rows: 1
- RECONCILIATION PASS

## Preservation rules
- The legacy schedule configuration is migrated to its own BackupScheduleConfigurations table; it is not represented as a BackupJobs execution.
- Frequency, hour, day selectors, retention count, timestamps, updater reference, and CollectionsJson are copied without normalization.
- Unsupported frequency values, invalid schedule ranges, invalid JSON, and target-length violations block the run; no truncation or silent correction occurs.
- Deterministic configuration IDs and LegacyDataMigrationMap provenance make the operation idempotent.
- The source ScheduledBackupSettings row is never updated or deleted.
