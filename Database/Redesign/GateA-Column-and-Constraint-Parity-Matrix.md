# Gate A Column and Constraint Parity Matrix

**Date:** 2026-11-18  
**Branch:** `development/database-redesign-enterprise`  
**Status:** Static document-to-script comparison. Not SQL execution or physical schema validation.

## 1. Static comparison result

A source-text comparison was performed between:
- `GateA-Additive-Identity-Foundation.sql`;
- `Target-Data-Dictionary-and-ERD.md`.

<badge>Static comparison only</badge>

- 13 CREATE TABLE definitions found in the proposed SQL.
- 13 corresponding data-dictionary definitions found.
- 134 SQL column declarations identified.
- No missing or extra column names found across the 13 corresponding definitions.
- The redundant serialized recovery-code column was removed from the data dictionary; `UserRecoveryCodes` is authoritative.

This confirms document-to-script column-name alignment only. It does not validate that SQL Server accepts the script, that FKs bind to the actual live keys, or that the proposed business model is approved.

## 2. Table-by-table column inventory

| Gate A table | SQL columns | Count | Dictionary alignment |
|---|---|---:|---|
| Persons | PersonId, LegalName, DisplayName, PrimaryEmail, PrimaryPhone, Whatsapp, PhotoUrl, IsArchived, ArchivedAtUtc, CreatedAtUtc, UpdatedAtUtc, RowVersion | 12 | Names aligned |
| UserAccounts | AccountId, PersonId, AccountStatus, NormalizedEmail, ExternalProvider, ExternalSubject, SecurityStamp, LastLoginAtUtc, CreatedAtUtc, RowVersion | 10 | Names aligned |
| UserCredentials | CredentialId, AccountId, CredentialType, CredentialHash, HashAlgorithm, CreatedAtUtc, ExpiresAtUtc, RevokedAtUtc, ReplacedByCredentialId, RowVersion | 10 | Names aligned |
| UserTwoFactorSettings | AccountId, IsEnabled, SecretCiphertext, KeyVersion, LastAcceptedTimeStep, EnrolledAtUtc, LastVerifiedAtUtc, UpdatedAtUtc, RowVersion | 9 | Names aligned |
| UserRecoveryCodes | RecoveryCodeId, AccountId, CodeHash, CreatedAtUtc, UsedAtUtc | 5 | Names aligned |
| RoleDefinitions | RoleKey, DisplayName, IsSystemRole, IsActive | 4 | Names aligned |
| RolePermissions | RoleKey, PermissionKey, Effect | 3 | Names aligned |
| UserRoleAssignments | RoleAssignmentId, AccountId, RoleKey, ScopeType, ScopeBuildingId, ScopeApartmentId, ValidFromUtc, ValidToUtc, GrantedAtUtc, GrantedByAccountId, GrantReason, RevokedAtUtc, RevokedByAccountId, RevocationReason, RowVersion | 15 | Names aligned |
| ApartmentOwnerships | OwnershipId, BuildingId, ApartmentId, PersonId, OwnershipSharePercent, ValidFromUtc, ValidToUtc, RecordedAtUtc, RecordedByAccountId, EvidenceReference, Notes, RowVersion | 12 | Names aligned |
| ApartmentOccupancies | OccupancyId, BuildingId, ApartmentId, PersonId, OccupancyType, IsPrimary, ValidFromUtc, ValidToUtc, RecordedAtUtc, RecordedByAccountId, Notes, RowVersion | 12 | Names aligned |
| ApartmentAccessGrants | GrantId, BuildingId, ApartmentId, PersonId, GrantType, PermissionSetKey, ValidFromUtc, ValidToUtc, GrantedAtUtc, GrantedByAccountId, RevokedAtUtc, RevokedByAccountId, Reason, RowVersion | 14 | Names aligned |
| AccountPermissionOverrides | OverrideId, AccountId, PermissionKey, Effect, ScopeType, ScopeBuildingId, ScopeApartmentId, ValidFromUtc, ValidToUtc, GrantedAtUtc, GrantedByAccountId, GrantReason, RevokedAtUtc, RevokedByAccountId, RevocationReason, RowVersion | 16 | Names aligned |
| LegacyIdentityMap | LegacyIdentityMapId, SourceSystem, SourceTable, SourceKey, PersonId, AccountId, MappingStatus, MatchMethod, EvidenceReference, ReviewReason, ReviewedAtUtc, ReviewedByAccountId, CreatedAtUtc, UpdatedAtUtc, RowVersion | 15 | Names aligned |

## 3. Physical type alignment

The data dictionary deliberately labels references to existing Building/Apartment keys as `legacy key type`. The current proposed Gate A SQL uses `nvarchar(64)` for those columns.

The supplied 2026-11-18 preflight reports:
- `Buildings.Id`: non-null nvarchar(64), 128 bytes;
- `Apartments.BuildingId`: non-null nvarchar(64), 128 bytes;
- `Apartments.Id`: non-null nvarchar(64), 128 bytes;
- matching `SQL_Latin1_General_CP1_CI_AS` collation for those inspected keys.

The SQL choice is therefore supported by the supplied restore metadata for these keys, but the following must still be verified from a fresh pre-execution inventory:
- every referenced key's exact type, length, collation, nullability, and key ordinal;
- composite FK order for Apartments(BuildingId, Id);
- all existing constraints and FK trust/disabled states;
- EF model and migration snapshot parity;
- whether any alternate schema or deployment artifact defines a different key shape.

Do not convert `legacy key type` to `nvarchar(64)` in any additional table until the same metadata check supports it.

## 4. Constraint inventory in the proposed SQL

| Table | Constraint/index intent | Remaining concern |
|---|---|---|
| Persons | PK, archive-state CHECK, non-unique email/phone lookup indexes | Identity duplicate resolution remains manual; archive retention policy open |
| UserAccounts | PK, Person FK, unique PersonId, AccountStatus CHECK, unique provider/subject filtered index, email lookup index | Email login uniqueness policy and normalization policy remain open |
| UserCredentials | PK, Account FK, replacement self-FK, CredentialType CHECK (Password/PIN), expiry CHECK, account/type lookup, RowVersion on revoke/replace | At most one active credential per type must be transactionally enforced or a filtered unique index approved |
| UserTwoFactorSettings | Account PK/FK, enabled-secret/enrollment CHECK, RowVersion for enrollment/replay-state concurrency | Disable workflow must revoke recovery codes and invalidate sessions; test atomicity |
| UserRecoveryCodes | PK, Account FK, unique CodeHash, account/unused lookup | Atomic single-use consumption and rotation policy must be tested |
| RoleDefinitions | PK | Seed/version lifecycle and code-vs-database catalog ownership open |
| RolePermissions | composite PK, Role FK, Effect CHECK (Deny/Allow) | Permission catalog is code-owned but deployment/version mismatch policy open |
| UserRoleAssignments | PK, account/role/building/apartment/actor FKs, legal-scope CHECK, validity CHECK, paired revocation actor/time CHECK, indexes | Interval overlap, duplicate active grants, grantor attribution, and system actor policy need transactional rules |
| ApartmentOwnerships | PK, apartment composite FK, Person FK, actor FK, share CHECK, validity CHECK, period indexes | Aggregate share completeness and overlap need transactional rules; evidence requirements open |
| ApartmentOccupancies | PK, apartment composite FK, Person FK, actor FK, type CHECK, validity CHECK, filtered unique current-primary index | Historical interval overlap and primary transition rules need transactional tests |
| ApartmentAccessGrants | PK, apartment composite FK, Person FK, grant/revoke actor FKs, validity CHECK, paired revocation actor/time CHECK, RowVersion, period indexes | GrantType catalog and PermissionSetKey version semantics open |
| AccountPermissionOverrides | PK, account/building/apartment/actor FKs, Effect CHECK, scope CHECK, validity CHECK, paired revocation actor/time CHECK, indexes | Permission scope must match actual resource; interval overlap and actor attribution need rules |
| LegacyIdentityMap | PK, restrictive Person/Account/reviewer FKs, unique source key, source/status CHECKs, resolved-review CHECK, link-target CHECKs, filtered Account uniqueness, Person/status indexes | SQL cannot enforce Account.PersonId = LegacyIdentityMap.PersonId; migration service must verify in one transaction |

## 5. Constraints that cannot be solved by row CHECK alone

These require domain/application transactional validation plus concurrency tests:

1. Ownership shares total exactly 100% only for a period marked complete.
2. Ownership intervals for the same person/apartment do not overlap improperly.
3. Primary occupancy intervals do not overlap and never produce two active primary occupants.
4. No duplicate effective role assignment or permission override exists for the same account/role-or-permission/scope/time.
5. A LegacyIdentityMap Users row's AccountId belongs to its PersonId.
6. Apartment access grants obey approved GrantType and permission-profile semantics.
7. Credential replacement and recovery-code consumption are atomic.
8. A permission decision resolves a Personal scope to the account's Person and cannot cross to another Person.

## 6. Pre-execution SQL hardening now present

The proposed SQL script now:
- uses `SET XACT_ABORT ON` and a single transaction;
- rejects a partial pre-existing set of the 13 Gate A tables;
- checks account status and external provider/subject pairing;
- limits credential type to Password/PIN while external providers remain account-level;
- requires enabled MFA to have ciphertext, key version, and enrollment timestamp, and requires disabled MFA to have no active secret/key version;
- enforces Deny/Allow effect ranges;
- requires resolved identity mappings to have reviewer identity and timestamp;
- records grant timestamps and rationale fields for scoped grants/overrides;
- enforces paired revocation timestamp and actor for role assignments, permission overrides, and apartment access grants.

A complete pre-existing set of all 13 tables is still not schema-validated by the creation script. A separate column/constraint parity check must pass before a rerun is allowed to skip creation. The first rehearsal must be on a fresh disposable restore with all 13 tables absent.

## 7. Remaining design blockers

- Define `ApartmentAccessGrants.GrantType` values and a versioned `PermissionSetKey` registry.
- Decide whether human grantor is mandatory and define the dedicated system actor for scheduled/system-issued grants.
- Decide ownership completeness, voting eligibility/delegation, financial rounding, maintenance visibility, preference ownership, audit retention, and ballot assurance.
- Finalize each permission's resource scope and split composite legacy capabilities.
- Reconcile SQL view deployment: AppDbContext maps `vw_ApartmentWalletMovements`, while the supplied restored database has no views and FullSchema is a separate source.
- Remove/feature-gate startup legacy PIN mutation before running the application against an evidence-preserving restore.
- Complete fresh metadata, FK, EF model/snapshot, migration history, and FullSchema parity review.

## 8. Verification and authorization

- This matrix is based on static source-text comparison of the checked-in design artifacts.
- No SQL parser/database execution, build, or test suite was run.
- No database or application source runtime was changed.
- No Gate A DDL was executed.
- **Gate A remains NOT AUTHORIZED.**
