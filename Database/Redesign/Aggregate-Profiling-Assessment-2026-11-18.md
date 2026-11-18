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

The second building `zxhRd7cwZ07rZotXeQ17` has 15 monthly-share rows in each of 2026-09, 2026-10, and 2026-11, with zero expense/revenue shares and carry-over values 0.00, 3,319.00, and 1,519.00 respectively. These carry-over amounts need semantic reconciliation before migration.

### 4.3 Wallet adjustments

Building `ZzgYia4qMZ23N8qhiCOV` has 2 WalletAdjustment rows with a net amount of +200.00. Preserve both transaction-level history and the aggregate baseline; do not infer the business reason from the aggregate alone.

**Decision:** the 0.22 / 0.04 differences are small but non-zero. Define rounding/precision rules and reconcile at transaction and apartment level before cutover. Never silently round away or overwrite the differences.

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
