# Phase 19 - User permission evidence backfill

- Run: 2026-11-19T02:33:37.3493013+02:00
- Mode: APPLY

## Results
- Database: BuildingManagement_RestoreTest
- Source permission rows: 81
- Pending: 81
- Target evidence rows: 81
- Provenance rows: 81
- RECONCILIATION PASS

## Permission semantics
- UserPermissions are account/user-scoped legacy permission entries; they are not role-level RolePermissions.
- Uid is preserved only as a legacy reference. No account/person linkage is inferred and no permissions are granted by this evidence migration.
- Exact permission strings are preserved; source rows are never changed or deleted.
