# Phase06 Identity & Authorization Migration Recheck

**Date:** 2026-11-18
**Branch:** `development/database-redesign-enterprise`
**Migration:** `20261118182110_Phase06_IdentityAuthorizationFoundation`
**Status:** Phase06 schema rehearsal completed on the approved disposable restore database.

## Artifacts reviewed

- `Migrations/20261118182110_Phase06_IdentityAuthorizationFoundation.cs`
- `Migrations/20261118182110_Phase06_IdentityAuthorizationFoundation.Designer.cs`
- `Migrations/AppDbContextModelSnapshot.cs`
- `Database/Redesign/GateA-Phase06-Focused-20261118.sql`
- `Database/Redesign/Phase06-Identity-Authorization-Migration-Review-2026-11-18.md`

The focused SQL was generated idempotently from `20261117130000_AddUserTwoFactor` through the new Phase06 migration. It references the current migration id and does not include either superseded Phase06 id.

## Verification

- `dotnet build .\BuildingManagementMVC.csproj --no-restore -v:minimal`: succeeded, 0 warnings and 0 errors after the approved defaults were applied.
- `dotnet test .\Tests\BuildingManagement.Tests\BuildingManagement.Tests.csproj --no-restore -v:minimal`: 495 passed, 0 failed, 0 skipped after final model generation.
- `dotnet ef migrations has-pending-model-changes`: no model changes since the migration.
- Focused idempotent SQL: 13 `CREATE TABLE` statements, no `ALTER TABLE` or `DROP TABLE` statements, and the current Phase06 migration id.
- The migration `Up()` creates the new identity and authorization schema. Its `Down()` drops the new tables, so it is not a production recovery strategy after writes begin.
- Read-only preflight ran on `BuildingManagement_RestoreTest` at `(localdb)\MSSQLLocalDB`; no application rows or secret values were exported.
- A `COPY_ONLY` backup was created at `C:\Users\alaa5\AppData\Local\Temp\BuildingManagement_RestoreTest_Phase06-pre-20261118-202322.bak` and passed `RESTORE VERIFYONLY WITH CHECKSUM`.
- The migration was applied using an explicit connection string for `BuildingManagement_RestoreTest` only.
- Post-migration reconciliation found 13/13 expected tables, the exact Phase06 history row, all eight checked CHECK constraints enabled/trusted, and no disabled or untrusted foreign keys. Results are in `Database/Diagnostics/GateA-Preflight-Results/20261118-202453/GateA-PostMigration-Reconciliation.txt`.

## Corrected constraints confirmed

- `ApartmentOwnerships.EvidenceReference` is required and must contain non-whitespace text.
- `ApartmentAccessGrants.Reason` is required and must contain non-whitespace text.
- Resolved `LegacyIdentityMap` rows require review time, reviewer, and a non-empty review reason.
- The migration and focused SQL contain these constraints.

## Owner-approved defaults

- `ApartmentAccessGrantType` is a byte-backed enum with stable values: `Family = 1`, `Tenant = 2`, `Caretaker = 3`, `Emergency = 4`, and `Temporary = 5`. A SQL CHECK constraint rejects other values.
- `PermissionSetKey` uses fixed, code-owned versioned keys: `apartment.access.family.v1`, `apartment.access.tenant.v1`, `apartment.access.caretaker.v1`, `apartment.access.emergency.v1`, and `apartment.access.temporary.v1`. The SQL constraint binds each key to its matching grant type. These profiles carry no implicit permissions; unknown or mismatched keys fail closed.
- Emergency and temporary grants require an explicit expiry timestamp.
- Ownership rows carry `Unknown`, `Incomplete`, or `CompleteVerified` status. CompleteVerified requires reviewer and timestamp. Incomplete/unknown totals below 100% are allowed; never infer or normalize missing shares. For a verified complete apartment period, service validation must ensure a 100% total and prevent overlapping shares above 100% transactionally.
- `RecordedByAccountId` stays nullable for system/import-originated ownership records. Human-created ownership records must carry the acting account; imported/system records must carry a durable evidence/source reference. Enforce this in the future write service.

## Open items before application use of the new tables

1. The permission evaluator and ownership write service are now implemented in application code. They are registered for dependency injection but are not wired into legacy controllers or authentication endpoints. Access-profile keys still authorize no operations.
2. Before any production deployment, require a separate target confirmation, production backup/restore plan, and production-specific reconciliation. Rehearsals changed only the disposable restore database.

The preflight confirmed that all 13 target tables were absent before the rehearsal, the prior four migrations were present, legacy keys matched `nvarchar(64)` lengths, and key collations matched the database default. The schema rehearsal is complete and reconciled; the new tables remain empty, and no backfill or application data mutation was performed.

## Phase07 follow-up — 2026-11-18

- Added `AuthorizationPermissionCatalog` and `AuthorizationEvaluator` with code-owned permission keys, active-account and security-stamp checks, server-resolved resource scopes, explicit-deny precedence, and MFA step-up for sensitive operations. Unknown permissions and absent grants deny access.
- Added `ApartmentOwnershipService` for serializable period writes, active-person and actor checks, evidence/source recording, closure reasons, append-only audit entries, and overlap/share validation. CompleteVerified periods require exactly 100%; incomplete/unknown totals are not normalized.
- Added the `OwnershipRecordSource` enum and constraints through `20261118184850_Phase07_OwnershipRecordProvenance`.
- Build succeeded with 0 warnings and 0 errors. `dotnet ef migrations has-pending-model-changes` reported no model changes.
- After the service and evaluator test coverage was added, the complete regression suite passed: 507 passed, 0 failed, 0 skipped. The first constrained test-host launch timed out; rerunning outside the restricted shell completed successfully.
- A new COPY_ONLY backup at `C:\Users\alaa5\AppData\Local\Temp\BuildingManagement_RestoreTest_Phase07-pre-20261118-204900.bak` passed `RESTORE VERIFYONLY WITH CHECKSUM`.
- Phase07 was applied to `BuildingManagement_RestoreTest` only. Reconciliation confirmed the migration history row, `RecordSource` as `tinyint NOT NULL` with default 0, both new checks enabled and trusted, and zero ownership rows.
- Focused SQL is saved as `Database/Redesign/GateA-Phase07-Focused-20261118.sql`.
