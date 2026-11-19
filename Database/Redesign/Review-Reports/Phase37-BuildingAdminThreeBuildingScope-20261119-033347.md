# Phase 37 - Three building administrators, isolated scopes

- Run: 2026-11-19T03:33:47.4464731+02:00
- Mode: DRY RUN

## Execution
- ERROR: Exception calling "Fill" with "1" argument(s): "String or binary data would be truncated in table 'tempdb.dbo.#BAB45DC0', column 'MappingSource'. Truncated value: 'Owner-confirmed Building 3; unique remai'.
Resolved Admin-to-Building map is not exactly three unique accounts and three unique buildings.
The statement has been terminated."

## Security boundaries
- Three legacy Users rows with role Admin/buildingadmin must resolve to three distinct target UserAccounts and three distinct Buildings.
- Two administrators use exact BuildingAdmins source links.
- The sole unlinked administrator is assigned to the sole building without a BuildingAdmins link, per the owner-approved Building 3 decision; no row ordering or guessed ID is used.
- Every assignment is ScopeType=Building with exactly one ScopeBuildingId and no apartment scope.
- The shared building-admin role contains only code-catalogued building-capable permissions; system-only and personal permissions are excluded.
- SuperAdmin accounts and existing unrelated assignments are not modified.
- The script is restricted to BuildingManagement_RestoreTest, transactional, idempotent, and dry-run by default.
- Runtime authorization enforcement is a separate required Phase38; Phase37 database grants alone do not make the legacy perm-claim filters safe.
