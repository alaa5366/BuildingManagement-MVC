# Phase 10 - Notification Backfill

- Run: 2026-11-19T02:12:32.9562904+02:00
- Mode: APPLY
- Source rows retained: dbo.Notifications

## Results
- Database: BuildingManagement_RestoreTest
- Source: 68
- Existing migrated: 0
- Pending: 68
- Target: 68
- Provenance: 68
- RECONCILIATION PASS

## Rules
- LegacyDataMigrationMap preserves the full composite key BuildingId/Id; NotificationMessages stores its SHA-256 reference to fit the target column limit.
- Target RecipientType is set to unresolved (0) because the target enum mapping is not formally defined; exact legacy type remains in the untouched source row.
- Read state copied only when IsRead=1 and ReadAt exists.
- Deterministic IDs and source map make reruns idempotent.
- No source rows are updated or deleted.
