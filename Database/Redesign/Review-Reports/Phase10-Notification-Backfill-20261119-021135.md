# Phase 10 - Notification Backfill

- Run: 2026-11-19T02:11:35.1031613+02:00
- Mode: APPLY
- Source rows retained: dbo.Notifications

## Results
- ERROR: Cannot index into a null array.

## Rules
- LegacyDataMigrationMap preserves the full composite key BuildingId/Id; NotificationMessages stores its SHA-256 reference to fit the target column limit.
- Target RecipientType is set to unresolved (0) because the target enum mapping is not formally defined; exact legacy type remains in the untouched source row.
- Read state copied only when IsRead=1 and ReadAt exists.
- Deterministic IDs and source map make reruns idempotent.
- No source rows are updated or deleted.
