# Phase 11 - Maintenance Backfill

- Run: 2026-11-19T02:15:19.9891843+02:00
- Mode: APPLY
- Source rows retained: dbo.MaintenanceRecords

## Results
- Database: BuildingManagement_RestoreTest
- Source rows: 1
- Existing target legacy rows: 0
- Pending: 1
- Titles over target limit: 0
- Target rows: 1
- Provenance rows: 1
- RECONCILIATION PASS

## Mapping and preservation rules
- Request IDs are deterministic from the composite legacy key BuildingId/Id.
- Legacy CreatedBy is retained as RequestedByReference; no person identity is inferred.
- Status and Priority are initialized to 0 (unclassified); original Status, MaintenanceDate, Cost, Vendor, AddedToExpense and AddedToExpenseAt are preserved in LegacyDataMigrationMap.SafeNote.
- Source titles longer than the target's 200-character limit block the run; no truncation is performed.
- Source rows are never updated or deleted.
