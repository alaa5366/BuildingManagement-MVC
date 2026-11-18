# Phase06 Identity & Authorization Migration Review

**Date:** 2026-11-18  
**Branch:** `development/database-redesign-enterprise`  
**Reviewed package commit:** `662387f`  
**Model hardening commits:** `4aedaf6`, `f8b6e41`  
**Status:** NOT APPROVED FOR DATABASE EXECUTION — regenerate and re-review the migration after the model changes below.

## 1. Scope and result

The generated `Phase06_IdentityAuthorizationFoundation` migration is additive in its `Up()` method. It creates 13 new tables:

- Persons and UserAccounts
- RoleDefinitions and RolePermissions
- UserCredentials, UserRecoveryCodes, and UserTwoFactorSettings
- UserRoleAssignments and AccountPermissionOverrides
- ApartmentOwnerships, ApartmentOccupancies, and ApartmentAccessGrants
- LegacyIdentityMap

The migration does not insert legacy identity mappings, merge people, migrate PINs, copy old 2FA secrets, or alter/drop existing legacy tables. This is consistent with the approved no-automatic-identity-resolution approach.

The generated `Down()` drops all 13 new tables. That is destructive after the application starts writing data; treat it as a pre-deployment rollback only, not as a production recovery strategy.

## 2. Model hardening applied after package generation

Review found three provenance/control gaps. These have now been corrected in the EF model and configuration:

1. **Apartment access grants:** `Reason` is now required and constrained to a non-empty value, aligning with DEC-07.
2. **Apartment ownership:** `EvidenceReference` is now required and constrained to a non-empty value, aligning with DEC-05's evidence requirement.
3. **Resolved legacy identity mappings:** `ReviewReason` must now be non-empty whenever MappingStatus is resolved (1 or 2), in addition to reviewer and review timestamp, aligning with DEC-01.

Commits:
- `4aedaf60a4981a0e1ad29fb0b408305f58ecb874` — entity nullability
- `f8b6e41204d2716b4b4b77b4d8d641f428b8dfc5` — EF constraints

**Important:** the migration and SQL review package in commit `662387f` predate these two model changes. They are stale and must not be treated as the final migration.

## 3. Remaining model items to resolve

- `ApartmentAccessGrantEntity.GrantType` is currently an unconstrained byte. Define the supported grant-type values (or a typed enum) before adding a database CHECK constraint. Do not guess numeric meanings.
- `ApartmentOwnerships.RecordedByAccountId` remains nullable. The implementation must define how system/import-originated ownership records receive durable attribution (for example, a system actor or explicit source/import provenance). An evidence reference alone does not identify who or what recorded the assertion.
- Ownership percentage validation is row-level only (0 < share <= 100). Aggregate overlapping-period share totals cannot be enforced by this CHECK; the future ownership write service must validate totals transactionally and prevent concurrent over-allocation.

## 4. SQL script scope

`GateA-Phase06-Review.sql` was generated without a from-migration argument, so it contains the complete chain:

1. `20261116120818_InitialCreate`
2. `20261117012657_Phase04_StandardizeKeyColumnTypes`
3. `20261117050000_Phase05_RemoveRedundantResidentIndexes`
4. `20261117130000_AddUserTwoFactor`
5. `20261118172211_Phase06_IdentityAuthorizationFoundation`

It is not a Phase06-only script. Phase04 includes many legacy-column narrowing operations (for example, IDs to nvarchar(64), user IDs to nvarchar(128), and other key/type standardization) and drops/recreates dependent constraints and indexes. SQL Server will reject values that cannot fit a narrowed column, but this generated script does not provide a dedicated preflight report of offending values. The Phase04 block is guarded by its migration-history row; when that row exists it is skipped.

For a focused review/deployment artifact, generate only the Phase06 range from `20261117130000_AddUserTwoFactor` to `20261118172211_Phase06_IdentityAuthorizationFoundation`, using `--idempotent`.

## 5. 2FA transition

The legacy `Users` 2FA columns remain in the model/schema while Phase06 adds `UserTwoFactorSettings`. No data is copied between them. Do not switch runtime reads/writes to the new table until a separately reviewed compatibility and re-enrollment/migration plan exists. This is especially important because the new table stores ciphertext as varbinary with a key version, while the legacy representation is different.

## 6. Required next steps

1. Pull the latest branch changes.
2. Regenerate the Phase06 migration and model snapshot from the corrected EF model. Replace the stale local Phase06 migration artifacts; do not manually edit generated migration files.
3. Build and run the test suite again; record the exact totals and failures.
4. Generate a Phase06-only idempotent SQL script from the previous migration to Phase06.
5. Review the regenerated migration, snapshot, and focused SQL as a single package.
6. Keep database execution blocked until the regenerated migration passes review and a fresh restore-test preflight is complete.

No database update or SQL execution was performed during this review.
