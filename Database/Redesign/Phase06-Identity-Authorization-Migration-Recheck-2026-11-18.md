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

The original preflight confirmed that all 13 target tables were absent before the Phase06 rehearsal, the prior four migrations were present, legacy keys matched `nvarchar(64)` lengths, and key collations matched the database default. Subsequent controlled candidate registration is recorded below; no target Person or UserAccount was created and no credential or permission data was imported.

## Phase07 follow-up — 2026-11-18

- Added `AuthorizationPermissionCatalog` and `AuthorizationEvaluator` with code-owned permission keys, active-account and security-stamp checks, server-resolved resource scopes, explicit-deny precedence, and MFA step-up for sensitive operations. Unknown permissions and absent grants deny access.
- Added `ApartmentOwnershipService` for serializable period writes, active-person and actor checks, evidence/source recording, closure reasons, append-only audit entries, and overlap/share validation. CompleteVerified periods require exactly 100%; incomplete/unknown totals are not normalized.
- Added `ApartmentAccessGrantService` for serializable grant and revoke operations. It requires a fresh MFA-backed `apartment.access.grant.manage` decision at apartment/building scope, validates active actors and people, requires reasons, and audits changes atomically. Profile keys still do not imply permissions.
- Added `LegacyIdentityMappingService` for exact Users/Residents source registration and human-reviewed disposition. It requires a System-scoped MFA permission, evidence and reason for resolved decisions, validates UserAccount/Person pairing in the same transaction, and audits every decision. It performs no fuzzy matching.
- Refined the code-owned finance catalog per DEC-40: read, deposit confirm/cancel, adjustment, expense/revenue read/post/reverse, export, and period close/reopen are separate keys; broad unresolved legacy finance keys are rejected.
- Added the `OwnershipRecordSource` enum and constraints through `20261118184850_Phase07_OwnershipRecordProvenance`.
- Build succeeded with 0 warnings and 0 errors. `dotnet ef migrations has-pending-model-changes` reported no model changes.
- Mapping service integration tests now prove valid `Users` mappings are audited and a cross-linked Account/Person pair is rejected without changing the pending candidate. SQL Server concurrency/transaction failure tests remain an integration gate.
- The complete regression suite passed after these tests were added: 547 passed, 0 failed, 0 skipped. The first constrained test-host launch timed out; rerunning outside the restricted shell completed successfully.
- A new COPY_ONLY backup at `C:\Users\alaa5\AppData\Local\Temp\BuildingManagement_RestoreTest_Phase07-pre-20261118-204900.bak` passed `RESTORE VERIFYONLY WITH CHECKSUM`.
- Phase07 was applied to `BuildingManagement_RestoreTest` only. Reconciliation confirmed the migration history row, `RecordSource` as `tinyint NOT NULL` with default 0, both new checks enabled and trusted, and zero ownership rows.
- Focused SQL is saved as `Database/Redesign/GateA-Phase07-Focused-20261118.sql`.

## Pending identity candidate staging — 2026-11-18

- Read-only preflight confirmed the exact target database and schema history; source counts were Users=5 and Residents=79, with zero blank keys. `LegacyIdentityMap`, `Persons`, and `UserAccounts` were initially empty.
- Created and verified COPY_ONLY backup `C:\Users\alaa5\AppData\Local\Temp\BuildingManagement_RestoreTest_IdentityCandidates-pre-20261118-211500.bak` (13,230,080 bytes; `RESTORE VERIFYONLY WITH CHECKSUM` passed).
- Executed `Database/Redesign/GateA-Identity-Pending-Candidates-20261118.sql` against `BuildingManagement_RestoreTest` only. It inserted exact Users/Residents source keys as status `Pending` (0), with no PersonId or AccountId, and wrote one aggregate audit event. It performs no fuzzy matching and does not read or copy credential values.
- Reconciliation confirmed Users=5 Pending, Residents=79 Pending, invalid/mapped rows=0, one audit event with aggregate counts, Persons=0, UserAccounts=0, and both Phase06/07 migrations still present. Results are in `Database/Diagnostics/GateA-Preflight-Results/20261118-212000/GateA-Identity-Pending-Candidates-Reconciliation.txt`.
- These 84 candidates remain unresolved until an authorized reviewer records evidence-backed dispositions. No permission assignments or login cutover were performed.
