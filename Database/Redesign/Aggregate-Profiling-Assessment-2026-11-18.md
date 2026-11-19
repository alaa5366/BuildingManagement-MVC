# Aggregate Profiling Assessment — 2026-11-18

**Repository:** `alaa5366/BuildingManagement-MVC`  
**Branch:** `development/database-redesign-enterprise`  
**Source:** `Database/Diagnostics/Profiling-Results/20261118-145432/`  
**Database:** `BuildingManagement_RestoreTest`  
**Profile type:** `READ_ONLY_PROFILE`  
**Captured:** 2026-11-18 14:54:33 +02:00  
**Evidence:** 15 result sets; 142 aggregate rows.

## 1. Executive decision

The aggregate profiling run completed and was uploaded successfully. It materially improves the evidence available for the redesign, but **Gate A is not yet cleared for execution**. Identity linking, credential migration, financial reconciliation, missing wallet views, retention-sensitive cascades, and the separate Gate A collation preflight still require explicit resolution.

This assessment concerns the restored test database only. It does not authorize DDL/DML, EF migration, backfill, application cutover, or production changes.

## 2. Identity and account-linking

| Metric | Result |
|---|---:|
| Residents | 79 |
| Users | 5 |
| Residents with UID matching a User | 0 |
| Residents with UID but no matching User | 79 |
| Duplicate UID groups (Residents / Users) | 0 / 0 |
| Duplicate normalized email groups (Residents / Users) | 0 / 0 |
| Duplicate phone groups (Residents / Users) | 4 / 1 |

**Decision:** do not auto-link Residents to Users. UID currently supplies no cross-table matches; phone duplication also makes phone-only matching unsafe. Name, email, or phone similarity must not create identity links automatically. Preserve unresolved candidates for reviewed/manual disposition. The duplicate phone groups need a privacy-safe review workflow that does not publish raw phone values in diagnostics.

## 3. Apartment ownership and occupancy

- 79 apartments have legacy Owner text.
- 79 apartments have at least one Resident row flagged `IsOwner`.
- 0 apartments have legacy Owner text without a corresponding Resident owner flag.
- 0 apartments have multiple active primary Residents under the profile's current predicate.
- No Resident lacks its apartment; no apartment lacks a Building; no non-empty FloorId failed the floor reference check.
- The apartment-level output contains 79 apartment groups. Two groups have `OwnerFlagRows=1` but `ActivePrimaryRows=0` and `ActiveResidentRows=0`; the other 77 have one active primary and one active resident.

**Decision:** these are legacy candidate indicators, not legal proof of ownership or occupancy. Do not infer ownership shares, legal dates, or active tenancy from Owner text / IsOwner / IsPrimary alone. Review the two inactive cases and preserve their historical state.

## 4. Financial reconciliation baseline

### 4.1 Deposits

- Cancelled deposits: 16 rows, total 6,356.00 (Building `zxhRd7cwZ07rZotXeQ17`, 2026-09).
- Confirmed deposits are reported separately by status in the source CSV.

**Decision:** retain cancelled status and history. Cancelled deposits must not be treated as collected revenue or silently merged into confirmed deposits.

### 4.2 Expenses, revenues, and apartment shares

For Building `LmkUDOJcjlvjiGmluz6c`, 2026-10:

| Measure | Source ledger | Monthly apartment shares | Difference (shares - ledger) |
|---|---:|---:|---:|
| Expenses | 4,700.00 | 4,700.22 | +0.22 |
| Revenues | 27,173.00 | 27,173.04 | +0.04 |

The other reported comparison for Building `ZzgYia4qMZ23N8qhiCOV` matches exactly:
- 2026-09 expenses 900.00; revenue 1,800.00.
- 2026-10 expenses 2,700.00; revenue 4,500.00.

The building `zxhRd7cwZ07rZotXeQ17` has 15 monthly-share rows in each of 2026-09, 2026-10, and 2026-11, with zero expense/revenue shares and carry-over values 0.00, 3,319.00, and 1,519.00 respectively. Building `ZzgYia4qMZ23N8qhiCOV` has 4 monthly-share rows in both 2026-09 and 2026-10; its 2026-10 carry-over is 17,030.00. These carry-over amounts need semantic reconciliation before migration.

### 4.3 Wallet adjustments

Building `ZzgYia4qMZ23N8qhiCOV` has 2 WalletAdjustment rows with a net amount of +200.00. Preserve both transaction-level history and the aggregate baseline; do not infer the business reason from the aggregate alone.

Confirmed deposits in the profile are also status-separated: 4 rows / 5,119.00 for Building `zxhRd7cwZ07rZotXeQ17` in 2026-09, 1 row / 16,780.00 for Building `ZzgYia4qMZ23N8qhiCOV` in 2026-09, and 5 rows / 6,712.00 for that building in 2026-10.\n\n**Decision:** the 0.22 / 0.04 differences are small but non-zero. Define rounding/precision rules and reconcile at transaction and apartment level before cutover. Never silently round away or overwrite the differences.

## 5. Polls and cameras

- Legacy PollVotes: 1; one poll and one apartment represented.
- Votes without a matching option: 0.
- Votes without a matching poll: 0.
- Cameras: 3; cameras without DVR: 0; cross-building DVR assignments: 0.

**Decision:** referential integrity is clean for this small current sample. It does not validate the target secret-ballot model, delegated representation, eligibility snapshots, or identity/ballot separation. Historical apartment-attributed vote evidence must remain intact.

## 6. Credential and secret inventory

| Metric | Result |
|---|---:|
| Users with non-empty legacy plain PIN | 5 |
| Apartments with non-empty legacy plain PIN | 0 |
| Residents with PinHash | 79 |
| Residents with PasswordHash | 0 |
| GlobalSettings with WhatsApp token | 0 |
| GlobalSettings with Cloudinary API key | 0 |
| DVRs with encrypted password | 0 |

**Decision:** do not copy the five legacy plain PINs into the target credential store. Determine the exact legacy PIN verification/hash algorithm and design a forced reset or secure re-enrollment path where safe verification is not possible. The presence of 79 Resident PinHash values does not prove compatibility with the new credential hasher; validate algorithm, salt, parameters, and verification behavior before migration. No secret values were exported.

## 7. Wallet views and schema alignment

The live database returned zero objects for both expected names:
- `vw_ApartmentWalletMovements`
- `vw_ApartmentWalletBalance`

The checked-in generated FullSchema artifact previously listed a wallet view, so repository schema artifacts and the restored database are not aligned on this point. Identify the exact view(s), definitions, dependencies, and runtime consumers. Do not assume the view exists in the target database or recreate it from a guessed definition.

## 8. Foreign-key delete actions requiring retention review

The profile reports these enabled/trusted relationships:

| Parent table | Referenced table | On delete |
|---|---|---|
| AuditLog | Buildings | SET_NULL |
| Cameras | Dvrs | CASCADE |
| DepositEditHistory | Deposits | CASCADE |
| Deposits | Apartments | NO_ACTION |
| Deposits | Buildings | CASCADE |
| Expenses | Buildings | CASCADE |
| Expenses | FinancialCategories | NO_ACTION |
| MonthlyApartmentShares | Apartments | NO_ACTION |
| MonthlyApartmentShares | Buildings | CASCADE |
| PollOptions | Polls | CASCADE |
| PollVotes | Apartments | NO_ACTION |
| PollVotes | PollOptions | CASCADE |
| QrTokens | Apartments | NO_ACTION |
| QrTokens | Buildings | CASCADE |
| Residents | Apartments | CASCADE |
| Revenues | Buildings | CASCADE |
| Revenues | FinancialCategories | NO_ACTION |
| WalletAdjustments | Apartments | NO_ACTION |
| WalletAdjustments | Buildings | CASCADE |

**Decision:** building deletion currently cascades into multiple financial and operational histories; apartment deletion cascades into Residents; poll-option deletion cascades into votes; deposit deletion cascades into edit history. The target retention policy must replace unsafe physical-delete assumptions with restrictive FKs, soft deletion, archival, or explicit controlled retention workflows as appropriate. Do not change these relationships without a tested migration and rollback plan.

## 9. Migration baseline

The restored database reports four EF migrations:
- `20261116120818_InitialCreate`
- `20261117012657_Phase04_StandardizeKeyColumnTypes`
- `20261117050000_Phase05_RemoveRedundantResidentIndexes`
- `20261117130000_AddUserTwoFactor`

This is the exact observed migration history for this restored database at capture time.

## 10. Remaining Gate A blockers and next actions

1. Re-run `GateA-Preflight-Validation.sql` against this same disposable restore and publish all result sets, including the database default collation check added to the script. The 15-set profiling batch does not include that preflight.
2. Reconcile the database default collation with all referenced legacy key columns before creating new FKs.
3. Resolve the wallet-view discrepancy by inspecting the live view/module metadata and checked-in definitions.
4. Specify a safe credential migration/re-enrollment strategy; no plaintext PIN migration.
5. Define reviewed identity-link decisions; no automatic Resident/User matching.
6. Reconcile the 0.22 and 0.04 financial differences, carry-over amounts, cancelled deposits, and the +200.00 wallet adjustments.
7. Approve retention rules for every cascade listed above.
8. Perform a disposable-restore Gate A rehearsal only after the above checks pass, then test rollback and application compatibility.

## 11. Safety boundary

The profiling script is SELECT/metadata-only and was run against `BuildingManagement_RestoreTest`. This assessment is documentation only. No production database was accessed or modified; no Gate A DDL, EF migration, backfill, credential migration, or application cutover has been executed.
\n## 12. Updated Gate A preflight reviewed — 2026-11-18 15:09 +02:00\n\nThe new read-only preflight run is committed under `Database/Diagnostics/GateA-Preflight-Results/20261118-150906/` (7 result sets, 29 rows).\n\nObserved on `BuildingManagement_RestoreTest` / `ZOS-AAM\\LOCALDB#8B6B20E4`:\n- The three referenced legacy key columns are present, non-nullable, and have the expected `nvarchar(64)` shape (128 bytes): `Buildings.Id`, `Apartments.BuildingId`, and `Apartments.Id`.\n- All three columns use `SQL_Latin1_General_CP1_CI_AS`; the database default collation is the same. The collation compatibility blocker is therefore cleared for the currently inspected FK key columns and target columns that inherit the database default.\n- `Buildings(Id)` and `Apartments(BuildingId, Id)` primary keys are present. The expected unique apartment number index and other reported apartment/building indexes are present.\n- All twelve Gate A target objects are absent.\n- The four expected EF migrations remain present through `20261117130000_AddUserTwoFactor` (EF Core 8.0.10).\n\n**Gate decision:** the preflight metadata checks pass for the inspected legacy key types, lengths, collations, referenced keys/indexes, target-object absence, and migration baseline. This does **not** authorize running `GateA-Additive-Identity-Foundation.sql`. Gate A remains blocked by the non-collation items in Section 10: credential strategy, reviewed identity linking, financial/carry-over reconciliation, wallet-view discrepancy, cascade retention policy, and the required disposable-restore rehearsal/rollback plan. No DDL/DML, migration, backfill, or cutover was executed by this preflight.\n

## 13. Repository code review — wallet views and PIN migration — 2026-11-18

The source-level findings are documented in `Wallet-and-Credential-Code-Review-2026-11-18.md`. The repository contains a definition for `vw_ApartmentWalletMovements` in `BuildingManagement-FullSchema.sql`, and EF maps a keyless entity to it, but no EF migration creates the view and no runtime use was found in `WalletService` or repository search. Its definition is movement totals only (confirmed deposits plus adjustments), not a full balance. No source definition or mapping for `vw_ApartmentWalletBalance` was found. Therefore, do not recreate either view until actual external/module consumers are confirmed; the second view should not be treated as a known legacy object.

PIN source review found that application startup invokes `PinMigrationService.MigrateLegacySecretsAsync`. It hashes non-BCrypt-prefixed Users PIN and Residents PinHash values, clears non-empty Buildings.AdminPin values, and migrates/clears Apartments.Pin values. The resident loop treats every non-prefixed non-empty value as raw PIN; this can corrupt access if the stored value is malformed or uses another encoding. The routine also clears source PIN fields without a durable per-record migration audit/verification proof. Do not start the application against the restore for inspection before reviewing the new SQL-only format diagnostic. The previous counts (5 non-empty User PINs and 79 non-empty Resident PinHash values) do not prove their current formats.

New read-only runner and SQL: `Database/Diagnostics/Run-Wallet-and-Credential-ReadOnly-Validation.ps1` and `Wallet-and-Credential-ReadOnly-Validation.sql`. Results are pending. No app startup, credential rewrite, DDL/DML, view creation, or migration was performed.



## 14. Superseding wallet and credential diagnostic — 2026-11-18 15:15 +02:00

The six-result-set diagnostic is committed under `Database/Diagnostics/Wallet-Credential-Validation-Results/20261118-151511/`. It supersedes the earlier statement that wallet metadata/dependency and credential-format results were pending.

- Both requested wallet views are absent in the restored database; no wallet-named SQL view definition or SQL-module dependency rows were returned. This supports schema/model drift, not a demonstrated database-module runtime dependency. External reporting/client dependencies remain unverified.
- All 5 Users PIN values (3 admin, 2 superadmin) and all 79 Residents.PinHash values classify as `BCRYPT_SHAPE_60_UNVERIFIED` (length 60). This is format/length evidence only, not cryptographic verification.
- Apartments.Pin is empty for all 79 apartments; Buildings.AdminPin is empty for all 3 buildings; Residents.PasswordHash is empty for all 79 residents.
- No secret values or personal identifiers were exported.

**Updated decision:** the previous aggregate finding of five non-empty User PINs and 79 non-empty Resident PinHash values remains true, but their current format is now known to be BCrypt-shaped. Do not describe them as plaintext. Still do not run the startup migration for inspection: its prefix-only logic and source-clearing behavior require redesign. The next safe credential step is non-mutating cryptographic verification that emits aggregate outcomes only, followed by an explicit recovery/re-enrollment policy for any invalid hashes. Gate A remains blocked by the other unresolved items in Section 10. No DDL/DML or credential rewrite occurred.



## 15. Authentication source review — 2026-11-18

See `Authentication-and-Credential-Flow-Review-2026-11-18.md` for the detailed review of startup PIN migration, BCrypt classification/verification, direct and unified login, account-context selection, session revalidation, MFA policy, and required tests.

This adds implementation blockers beyond the aggregate database evidence: remove credential mutation from startup; separate BCrypt shape from parseability and actual secret verification; make direct and unified resident account-state checks consistent; decide generic failure messaging for missing credentials; revalidate account scope before issuing a cookie; and plan mandatory admin/SuperAdmin MFA enrollment/recovery. No source behavior was changed and no tests or application startup were run by this review. Gate A remains blocked.



## 16. Financial and wallet source-code review — 2026-11-18

See `Financial-and-Wallet-Code-Review-2026-11-18.md` for source-level findings. Historical balance calculations use current apartment `MonthlyFee` and `Closed` state, while distribution recalculation uses the current open-apartment set. Stored monthly shares are loaded as authoritative wallet inputs and can diverge from source ledgers. The equal-share algorithm assigns exact remainder to the final apartment but uses an apartment list with no explicit stable ordering.

The +0.22 expense and +0.04 revenue variances therefore require per-apartment evidence; do not assume they are harmless rounding. Carry-over restatement must be designed around immutable posted balances and explicit corrections. Cancelled deposits remain excluded from collected wallet funds.

Next action is a read-only per-apartment reconciliation diagnostic; no source code, tests, SQL, or financial rows were changed by this review. Gate A remains blocked.



## 17. Per-apartment financial reconciliation runner prepared — 2026-11-18

Added `Database/Diagnostics/Financial-Reconciliation-ReadOnly-Validation.sql` and `Database/Diagnostics/Run-Financial-Reconciliation-ReadOnly-Validation.ps1`. This read-only batch is designed to resolve the building/month share variances with five result sets: database identity; monthly source-ledger vs share totals; per-apartment stored inputs; deterministic candidate allocation (ApartmentId order, explicitly not historical proof); and missing/orphan share rows.

The runner is restricted to `BuildingManagement_RestoreTest`, exports CSV files, and auto-commits/pushes only the timestamped results folder to the discovery branch. It has not yet been executed. No stored share, wallet, or ledger row has been changed. Review the output before making any financial migration decision.
