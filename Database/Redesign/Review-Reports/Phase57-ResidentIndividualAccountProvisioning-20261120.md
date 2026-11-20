# Phase 57 — Resident individual-account provisioning (DeepQA verification)

- Date: 2026-11-20
- Branch: `qa/deep-role-screen-audit-2026-11-20`
- Database used for writes: `BuildingManagement_DeepQA_20261120` only
- Production and `BuildingManagement_RestoreTest`: read-only / unchanged
- Mode: source-evidence backfill rehearsal + SQL Server integration verification

## Source relationship verification

- Legacy resident identity mappings: 79
- Exact preserved legacy resident payload matches by source row ID: 79
- Distinct target Persons: 79
- Exact BuildingId + ApartmentId matches to Apartments: 79
- Distinct apartments: 79
- Legacy active residents (`IsActive=1`, `IsDisabled=0`): 77
- Inactive legacy residents: 2; excluded from current occupancy and account provisioning
- Existing current occupancy rows before rehearsal: 0
- Current occupancy rows inserted on DeepQA: 77
- Occupancy source provenance is recorded in Notes; no guessed dates or contact-based apartment matching was used. `ValidFromUtc` is the migration snapshot time.

## Contact readiness

- Current occupants: 77
- Unique, syntactically valid phone numbers: 59
- Occupants in duplicate-phone conflicts: 16
- Occupants missing a phone: 2
- No person records were merged and no phone number was treated as proof of identity.
- All 77 occupants receive a pending account record. Phone conflicts prevent PIN issuance/activation, not creation of the independent Person-linked account.

## SQL Server service integration

The real `ResidentAccountProvisioningService` was invoked against DeepQA using SQL Server and its normal serializable transactions:

- Batch 1: 50 pending accounts created
- Batch 2: 27 pending accounts created
- Repeated batch: 0 additional accounts created (idempotency)
- Total resident accounts: 77, each linked to a distinct Person
- All 77 accounts: `AccountStatus=0` (Pending), `MustChangePin=1`
- Credentials created for these 77 accounts: 0
- Resident apartment-scope role assignments created: 77
- Existing 6 accounts: AccountStatus, SecurityStamp, and MustChangePin remained unchanged
- No PIN values were generated, displayed, or logged.

## Security behavior verified

- No default/shared PIN such as `1234`.
- Legacy automatic sign-in/provisioning using the shared PIN was removed.
- Apartment-wide QR PIN reset no longer changes individual account credentials.
- A resident account cannot be activated without an individual credential.
- PIN issuance for a resident is blocked while the primary phone is missing, invalid, or duplicated.
- Issuing a PIN creates a random individual PIN hash, activates the pending resident account transactionally, and requires a change on first login.
- Multiple apartment scopes remain attached to one Person-linked account rather than creating one account per apartment.


## Ongoing resident lifecycle implementation

The application registration workflow now follows the canonical Person ? ApartmentOccupancy ? UserAccount ? scoped resident role assignment model:

- New residents are created independently of phone numbers; duplicate or missing phone values never merge identities.
- A requested login is created as Pending (`AccountStatus=0`, `MustChangePin=1`) without any credential.
- The same Person can be assigned to another apartment while reusing the existing account and adding only the missing scope.
- A registration request token makes a repeated new-person submission idempotent; repeated occupancy submissions do not create duplicate rows.
- Returning residents reuse the same Person and account; a previously disabled account returns to Pending and old credentials are revoked for re-enrollment.
- Leaving an apartment closes the occupancy and revokes only its resident scope; identity, financial history and audit history remain.
- People without login accounts remain visible in resident administration.
- Resident PIN reset and activation paths require an explicit identity/phone verification attestation plus a unique, valid phone; direct resident-profile PIN changes and apartment-wide PIN mutation are disabled.
- Excel building import now creates pending person/account/occupancy records from populated apartment-owner rows and does not apply imported apartment PIN values.

The new lifecycle paths are covered by automated tests for pending account creation, duplicate-phone non-merging, missing phone, multiple apartment scopes, request idempotency, no-login occupancy, move, leave, return/re-enrollment, and verified PIN activation.

Additional isolated SQL Server integration checks ran against `BuildingManagement_DeepQA_20261120` and passed:

- `SqlServerLifecycle_CreateReuseLeaveReturnAndRequestRetry`: passed. Verified pending account creation with no credential, repeated registration idempotency, a second apartment scope reusing the same account, ending the final occupancy, disabling the account, and returning with the same Person/account in Pending state.
- `SqlServerMove_PreservesAccountAndTransfersScope`: passed. Verified that a move closes the old occupancy, adds the target occupancy, revokes only the old apartment scope, retains the same Person/account, and does not create a credential.
- Both temporary SQL integration harness source files were removed after execution. The DeepQA database was restored from the reviewed baseline backup after each write rehearsal, and `DBCC CHECKDB` passed after the final restore.

## Validation

- `dotnet build BuildingManagementMvc.slnx --no-restore -m:1 /nodeReuse:false`: passed, 0 warnings, 0 errors.
- Full automated suite after lifecycle and move workflows: 667 passed, 0 failed, 0 skipped.
- DeepQA SQL Server lifecycle integration: 1 passed; SQL Server move integration: 1 passed.
- `git diff --check`: passed; Git emitted only existing LF/CRLF normalization notices.
- No EF schema migration was required; no `dotnet ef database update` was run.
- No commit, push, or merge was performed.

## Cleanup

The DeepQA clone was restored from its saved baseline backup after verification. `RESTORE VERIFYONLY` passed and `DBCC CHECKDB` passed. The backup is retained at:

`D:\DBBackups\BuildingManagementQA\BuildingManagement_RestoreTest_20261120_preQA.bak`

Post-restore aggregate parity: both RestoreTest and DeepQA have 85 Persons, 6 UserAccounts, 0 ApartmentOccupancies, 5 UserCredentials, and 6 UserRoleAssignments. The QA app is stopped, the temporary SQL integration harness was removed, and the user-secrets connection remains pointed at RestoreTest.

This report contains aggregate counts only; no names, phone numbers, PINs, hashes, or raw identity keys are included.
