# Live Database Inventory Assessment — Enterprise Redesign

**Repository:** `alaa5366/BuildingManagement-MVC`  
**Branch:** `development/database-redesign-enterprise`  
**Assessment source:** `Database/Diagnostics/DatabaseRedesign-ReadOnly-Profiling_01.txt` through `_07.txt` supplied from the restored test database.  
**Database:** `BuildingManagement_RestoreTest`  
**Server:** `ZOS-AAM\\LOCALDB#8B6B20E4`  
**SQL Server:** `17.0.4025.3 Express Edition (64-bit)`  
**Captured:** `2026-11-18 10:30:15 UTC`

## 1. Executive result

The seven supplied result files are a **schema discovery inventory** (database identity, table row counts, column metadata, primary keys, foreign keys, indexes, and EF migration history). They are not the result sets from `DatabaseRedesign-ReadOnly-Profiling.sql`. In particular, no supplied result set establishes identity matching quality, ownership/occupancy anomalies, financial reconciliation totals, poll integrity counts, camera/DVR cross-building counts, or secret-presence counts.

Accordingly, the current evidence is sufficient to design additive target structures and migration controls, but **not sufficient to safely backfill or switch application authentication/authorization to the new model**. No production or test database was modified by this assessment.

## 2. Observed data volume

| Table | Rows | Table | Rows |
|---|---:|---|---:|
| Buildings | 3 | Floors | 16 |
| Apartments | 79 | Residents | 79 |
| Users | 5 | BuildingAdmins | 2 |
| UserPermissions | 81 | AuditLog | 1,050 |
| Deposits | 26 | Expenses | 12 |
| Revenues | 4 | MonthlyApartmentShares | 113 |
| FinancialCategories | 38 | WalletAdjustments | 2 |
| Polls | 1 | PollOptions | 2 |
| PollVotes | 1 | Notifications | 68 |
| Cameras | 3 | Dvrs | 1 |
| QrTokens | 9 | QrUsages | 5 |
| Presence | 9 | DepositEditHistory | 1 |
| BackupHistory | 1 | ScheduledBackupSettings | 1 |
| BuildingSettings | 0 | LoginThrottle | 0 |
| SyncPendingChanges | 0 | GlobalSettings | 1 |
| MaintenanceRecords | 1 | `__EFMigrationsHistory` | 4 |

These counts are a point-in-time baseline for this restored test copy only.

## 3. Physical schema facts that constrain implementation

### 3.1 Key lengths

The supplied live metadata reports:
- Building/apartment scoped keys such as `Buildings.Id`, `Apartments.BuildingId`, `Apartments.Id`, `Residents.BuildingId`, and the corresponding finance FK columns as `nvarchar(64)`.
- Account/person-like identifiers `Users.Uid`, `Residents.Uid`, `BuildingAdmins.AdminUid`, `Presence.Uid`, and `QrTokens.Uid` as `nvarchar(128)`.
- `Residents.Phone` is `nvarchar(450)`, while `Users.Phone` is `nvarchar(max)`.
- `Buildings.BuildingNumber` is `nvarchar(450)`.
- Several descriptive and legacy credential fields are `nvarchar(max)`.

Do not shrink, widen, or repurpose these columns as part of the first additive migration. Any FK that references a legacy key must match its actual SQL type, length, and collation. New independent identity keys can use the target UUID strategy described in the data dictionary.

### 3.2 Existing key structure

The current model uses composite keys for building-scoped data, including:
- `Apartments(BuildingId, Id)`
- `Floors(BuildingId, Id)`
- `Deposits(BuildingId, Id)`
- `Expenses(BuildingId, Id)`
- `Polls(BuildingId, Id)`
- `PollOptions(BuildingId, PollId, Id)`
- `PollVotes(BuildingId, PollId, ApartmentId)`

The redesign must retain tenant/building isolation through composite relationships when legacy entities are referenced. Do not replace these keys in-place in the identity migration.

### 3.3 Referential actions requiring redesign review

The inventory reports enabled and trusted FKs. Current cascade paths include:
- `Buildings` to apartments, floors, building admins/settings, financial categories, deposits, expenses, revenues, polls, notifications, maintenance records, DVRs, QR tokens, monthly shares and wallet adjustments.
- `Apartments` to `Residents` (CASCADE).
- `Polls` to `PollOptions` (CASCADE), and `PollOptions` to `PollVotes` (CASCADE).
- `Deposits` to `DepositEditHistory` (CASCADE).
- `Users` to `BuildingAdmins` and `UserPermissions` (CASCADE).
- `Dvrs` to `Cameras` (CASCADE).

Target policy: preserve finance, audit, identity, voting, and legal relationship history. Replace destructive lifecycle operations with archive/close/revoke/void/reversal workflows. Cascade is allowed only for disposable dependent data after explicit review.

### 3.4 Tenant-boundary gap

`Cameras` has `BuildingId`, but its current FK to `Dvrs` references `DvrId` only. The schema therefore does not declaratively ensure that the camera and DVR belong to the same building. Target design should add a unique candidate key on `Dvrs(BuildingId, Id)` and a composite FK from `Cameras(BuildingId, DvrId)`, after checking duplicate/orphan/cross-building data using the missing profiling output.

### 3.5 Index baseline

The inventory includes the expected building/apartment and financial indexes, including:
- `Apartments(BuildingId, Number)` unique.
- `Residents(Uid)` unique, plus apartment and phone lookup indexes.
- `AuditLog(BuildingId, Ts)` and `AuditLog(ApartmentId, Ts)`.
- `Cameras(DvrId, Channel)` unique.
- Wallet adjustment, deposit, revenue/expense category, notification, QR and poll indexes.

No new index should be added solely by convention. Validate query patterns and key widths, especially for SQL Server's index key-size constraints.

## 4. EF migration baseline

The restored test database reports these four applied migrations (EF Core product version 8.0.10):
1. `20261116120818_InitialCreate`
2. `20261117012657_Phase04_StandardizeKeyColumnTypes`
3. `20261117050000_Phase05_RemoveRedundantResidentIndexes`
4. `20261117130000_AddUserTwoFactor`

The target migration chain must be generated from the current model snapshot and verified against this exact history. Do not create a parallel initial schema or assume the checked-in legacy schema script fully describes the restored database.

## 5. Decisions now fixed for the implementation

1. A human is represented once in `Persons`; contact details are not identity keys and do not trigger automatic person merges.
2. One human person may have at most one interactive `UserAccount`; a person may exist without an account.
3. Ownership and occupancy are separate dated relationships. Multiple owners and occupants are supported; primary occupant is optional and constrained to one active primary per apartment.
4. A person/account can hold multiple roles in multiple buildings and apartments, with every authorization decision checking role **and** scope.
5. SuperAdmin is a System-scoped role assignment on the same account/person model, not a separate identity type.
6. Apartment wallet remains apartment-scoped.
7. Voting policy explicitly selects Person or Apartment eligibility. Apartment voting requires explicit delegation where the selected policy requires it. Secret ballots must not persist an identity-to-choice join.
8. Existing financial tables and services remain intact until mapping and reconciliation prove a safe cutover.
9. Credentials and integration secrets are never returned to browser DTOs. Legacy PIN and integration-secret migration requires validated hashing/encryption and key-management procedures.
10. Existing audit/financial/ownership/occupancy history is retained. No silent cascade deletion or automatic merge is permitted.

## 6. Required profiling evidence before backfill/cutover

Run the existing `Database/Diagnostics/DatabaseRedesign-ReadOnly-Profiling.sql` against `BuildingManagement_RestoreTest` and save **all ten result sets**. They must include:
- Duplicate UID/email/phone group counts and Resident UID-to-User matches.
- Residents without apartments; apartment/building/floor integrity; legacy Owner text vs owner flags; multiple active primary residents.
- Per-apartment resident/owner/primary counts.
- Deposits, expenses and revenues aggregate totals by building/month/status or category.
- Monthly apartment-share totals and wallet adjustment totals.
- Poll vote-to-option and vote-to-poll integrity.
- Cameras without DVR and cross-building camera/DVR counts.
- Presence-only counts for legacy PIN/hash/integration secrets (never export secret values).
- Wallet view presence, current FK delete actions, and migration history.

The seven inventory files do not contain these ten profiling result sets. No PII, PIN, hash, token, or secret value should be added to the result files.

## 7. Implementation sequence and gates

### Gate A — additive identity foundation
Create new identity, account, credential, recovery-code, and scoped-role tables without dropping or renaming legacy tables. Add explicit indexes, check constraints, restrictive FKs, and concurrency/audit columns. Keep legacy authentication active until the new model is fully mapped and tested.

### Gate B — dated apartment relationships
Add ownership, occupancy, and explicit access-grant tables. Backfill only from evidence-backed matches. Legacy `Apartments.Owner` text is not sufficient to create a legal Person/ownership row without review. Preserve source references and migration provenance.

### Gate C — application cutover
Update authentication, authorization, resident/account management, building-admin management, and user settings to use one account/person and scoped role assignments. Add compatibility reads during transition; do not dual-write without transaction/outbox guarantees. Require SuperAdmin/admin MFA by policy; resident MFA remains optional by default with policy configuration.

### Gate D — voting and security
Implement explicit poll voting policy, eligibility snapshots, delegation, and anonymous ballots. Preserve historical apartment-attributed votes as legacy records; do not fabricate person identities for them. Move secrets to protected storage and return status-only DTOs.

### Gate E — validation and retirement
Run data reconciliation, authorization isolation tests, financial totals comparisons, backup/restore drills, and rollback rehearsals. Only then retire legacy columns/tables in a separately approved deployment.

## 8. Release and safety status

- This document is an assessment and implementation contract, not a migration.
- No DDL/DML was executed against the database.
- No EF migration was generated or applied.
- No production data/schema was accessed or modified.
- The next safe implementation action is to complete Gate A as an additive code/schema change, while retaining the legacy path, after the missing profiling output is reviewed for any unexpected data constraints.

## 9. Gate A preflight results reviewed — 2026-11-18 11:17 UTC

The six result files `Database/Diagnostics/GateA-Preflight-Validation_01.txt` through `_06.txt` were reviewed against the preflight script.

Observed on `BuildingManagement_RestoreTest` / `ZOS-AAM\\LOCALDB#8B6B20E4`:

- The three referenced legacy key columns are present and match `nvarchar(128)` bytes (64 Unicode characters): `Buildings.Id`, `Apartments.BuildingId`, and `Apartments.Id`.
- All three legacy columns use `SQL_Latin1_General_CP1_CI_AS`.
- `Apartments` has PK `(BuildingId, Id)`; `Buildings` has PK `(Id)`. The expected apartment composite key is present.
- All twelve Gate A target table names are absent; no partial target table was reported.
- The database reports the four expected EF migrations through `20261117130000_AddUserTwoFactor`, ProductVersion `8.0.10`.
- The preflight result set did not include the database default collation. The preflight SQL has therefore been extended to report it. New target `nvarchar` columns inherit the database default collation unless explicitly overridden; this remains an execution gate.

**Assessment:** the legacy FK key type/length, legacy key collation consistency, and referenced key/index structure are supported by the returned evidence. The overall Gate A execution gate is **not yet passed** until the database default collation result is reviewed and the full ten-section aggregate profiling results are supplied. No SQL DDL/DML was executed.


## Superseding aggregate profiling update — 2026-11-18

The 15 aggregate profiling result sets are now available under `Database/Diagnostics/Profiling-Results/20261118-145432/`. Their findings and detailed reconciliation decisions are documented in `Database/Redesign/Aggregate-Profiling-Assessment-2026-11-18.md`.

The prior statement that aggregate profiling outputs were still missing is superseded. Profiling is complete, but Gate A remains blocked: identity matches are zero; credential migration requires algorithm verification/re-enrollment; small financial differences and carry-over balances require reconciliation; both expected wallet views are absent in the restored database; cascade retention rules need approval; and the updated Gate A preflight (including database default collation) must be rerun and reviewed. No Gate A DDL, EF migration, backfill, or cutover has been executed.
\n## Superseding Gate A preflight result — 2026-11-18 15:09 +02:00\n\nThe latest run is committed under `Database/Diagnostics/GateA-Preflight-Results/20261118-150906/` and contains seven result sets. It confirms the three legacy FK key columns are non-nullable `nvarchar(64)` (128 bytes), all use `SQL_Latin1_General_CP1_CI_AS`, and the database default collation matches. The expected Buildings and Apartments primary keys/indexes are present; all twelve Gate A target objects are absent; and the four expected EF migrations remain applied.\n\nThis supersedes the earlier statement that the database default collation was still pending. The collation/type/length/key metadata portion of preflight is now supported by evidence. Gate A is **still blocked overall** by credential migration design, manual identity-link decisions, financial and carry-over reconciliation, wallet-view discrepancy, cascade retention policy, and the planned disposable-restore execution/rollback rehearsal. This was a read-only metadata run; no DDL/DML or application changes were performed.\n