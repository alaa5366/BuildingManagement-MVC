# Phase 37 - Three building administrators, isolated scopes

- Run: 2026-11-19T03:45:35.8152742+02:00
- Mode: APPLY

## Execution
System.Object[]

## Security boundaries
- Three legacy Users rows with role Admin/buildingadmin must resolve to three distinct target UserAccounts and three distinct Buildings.
- Two administrators use exact BuildingAdmins source links.
- The sole unlinked administrator is assigned to the sole building without a BuildingAdmins link, per the owner-approved Building 3 decision; no row ordering or guessed ID is used.
- Every assignment is ScopeType=Building with exactly one ScopeBuildingId and no apartment scope.
- The shared building-admin role contains only code-catalogued building-capable permissions; system-only and personal permissions are excluded.
- SuperAdmin accounts and existing unrelated assignments are not modified.
- The script is restricted to BuildingManagement_RestoreTest, transactional, idempotent, and dry-run by default.
- Runtime authorization enforcement is a separate required Phase38; Phase37 database grants alone do not make the legacy perm-claim filters safe.
