# Live Schema Parity Assessment — 2026-11-18

**Evidence folder:** `Database/Diagnostics/Redesign-Schema-Parity-Results/20261118-180620/`  
**Evidence commit:** `ccce2bf4f6337582b562f5fbbc8c4dc95d8b1b75`  
**Database:** `BuildingManagement_RestoreTest` on `(localdb)\\MSSQLLocalDB`  
**Branch:** `development/database-redesign-enterprise`  
**Assessment scope:** read-only SQL Server catalog metadata and approximate table row counts.

## 1. Collection integrity

- All 15 diagnostic queries exported successfully (15/15).
- The README confirms the expected repository, branch, SQL instance, and disposable restore database.
- No query-error file was produced by the successful run.
- The collector exported metadata and approximate row counts only; it did not export business row values or secrets.
- The results were committed and pushed. The branch head advanced to `ccce2bf4f6337582b562f5fbbc8c4dc95d8b1b75`.

The terminal displayed a PowerShell `NativeCommandError` while Git printed its normal push destination line. The remote branch head and commit tree confirm that the push succeeded. The collector's Git wrapper has since been hardened to use the native Git exit code rather than treating normal stderr output as a failed command. No diagnostic rerun is needed for this fix.

## 2. Database identity and settings

| Property | Observed value |
|---|---|
| Database | `BuildingManagement_RestoreTest` |
| SQL Server instance identity | `ZOS-AAM\\LOCALDB#420F145D` |
| SQL Server version | `17.0.4025.3` |
| Edition | Express Edition (64-bit) |
| Compatibility level | 170 |
| Database collation | `SQL_Latin1_General_CP1_CI_AS` |
| State | ONLINE |
| Read-only | False |
| Recovery model | SIMPLE |
| Read committed snapshot | ON |
| Snapshot isolation | OFF |
| AUTO_CLOSE | ON |
| AUTO_SHRINK | OFF |
| Query Store | ON |

These are properties of this disposable LocalDB restore, not a production-environment assessment. AUTO_CLOSE is noted for awareness; it is not a Gate A schema blocker by itself.

## 3. Existing schema inventory

- 32 tables were reported, including `dbo.__EFMigrationsHistory`.
- 318 columns were reported across those tables.
- 51 primary/unique-key column rows were reported.
- 100 index-column rows describe 59 distinct indexes.
- 48 foreign-key column mappings describe 32 foreign-key constraints.
- Zero legacy CHECK constraints were found.
- Two default constraints were found: `AuditLog.Severity` and `Users.TwoFactorEnabled`.
- Zero SQL views and zero SQL expression dependencies were found.

The zero CHECK-constraint result means the current legacy schema does not provide database-level CHECK enforcement for the reviewed business invariants. Existing application validation must not be mistaken for a database constraint.

### Main table row counts

Counts below are SQL Server partition metadata estimates, not a transactional business reconciliation.

| Table | Approx. rows | Table | Approx. rows |
|---|---:|---|---:|
| Apartments | 79 | Residents | 79 |
| Users | 5 | UserPermissions | 81 |
| Buildings | 3 | BuildingAdmins | 2 |
| AuditLog | 1,050 | MonthlyApartmentShares | 113 |
| Deposits | 26 | Expenses | 12 |
| Revenues | 4 | WalletAdjustments | 2 |
| Polls | 1 | PollOptions | 2 |
| PollVotes | 1 | Notifications | 68 |
| QrTokens | 9 | QrUsages | 5 |
| Cameras | 3 | Dvrs | 1 |

Other tables and exact counts are preserved in `02-Table-Inventory-And-RowCounts.csv`.

## 4. Gate A preflight result

All 13 proposed Gate A tables are absent from the restore database:

- Persons
- UserAccounts
- UserCredentials
- UserTwoFactorSettings
- UserRecoveryCodes
- RoleDefinitions
- RolePermissions
- UserRoleAssignments
- ApartmentOwnerships
- ApartmentOccupancies
- ApartmentAccessGrants
- AccountPermissionOverrides
- LegacyIdentityMap

This is the required clean starting object-set result for a first disposable rehearsal. It is not authorization to execute the SQL.

## 5. Legacy key compatibility

The live catalog confirms:

- `Buildings.Id`: NOT NULL `nvarchar(64)`, 128 bytes.
- `Apartments.BuildingId`: NOT NULL `nvarchar(64)`, 128 bytes.
- `Apartments.Id`: NOT NULL `nvarchar(64)`, 128 bytes.
- All three use `SQL_Latin1_General_CP1_CI_AS`.
- `Buildings.Id` has the expected primary key.
- `Apartments` has the composite primary key in the order `(BuildingId, Id)`.

This supports the current Gate A use of `nvarchar(64)` for those legacy references. It does not validate every proposed FK or replace a SQL Server execution rehearsal.

## 6. Referential actions and retention risks

The 32 legacy FK constraints have these delete actions:

| Delete action | FK constraints |
|---|---:|
| CASCADE | 22 |
| NO_ACTION | 9 |
| SET_NULL | 1 |

Important cascade examples confirmed by the inventory:

- `Residents -> Apartments`
- `Deposits -> Buildings`
- `MonthlyApartmentShares -> Buildings`
- `WalletAdjustments -> Buildings`
- `DepositEditHistory -> Deposits`
- `PollOptions -> Polls` and `PollVotes -> PollOptions`
- `UserPermissions -> Users`
- `BuildingAdmins -> Users`

These are existing schema behaviors, not newly introduced Gate A relationships. Before any future delete-policy change or cutover, classify each path by retention requirements and prove that financial, voting, identity, and audit history cannot be unintentionally deleted. In particular, physical poll deletion can cascade to options and votes under the current schema.

## 7. Views and EF migration history

- The restore database contains no views.
- No SQL dependency rows were returned.
- Therefore the restore does not currently contain `vw_ApartmentWalletMovements` (or `vw_ApartmentWalletBalance`).
- This confirms the existing mismatch between the checked-in EF model mapping / FullSchema artifact and the restored database. The view's authoritative deployment path remains an open owner decision; the recommended direction is EF Core migrations as the canonical deployment mechanism, with FullSchema treated as generated/secondary output.
- The database has exactly these four applied EF migrations, all with ProductVersion `8.0.10`:
  1. `20261116120818_InitialCreate`
  2. `20261117012657_Phase04_StandardizeKeyColumnTypes`
  3. `20261117050000_Phase05_RemoveRedundantResidentIndexes`
  4. `20261117130000_AddUserTwoFactor`

The migration history is consistent with the expected restore baseline. It does not, by itself, prove full EF model snapshot parity.

## 8. Gate A design inventory correction

A fresh source count of the 13 `CREATE TABLE` declarations in `GateA-Additive-Identity-Foundation.sql` totals **137 columns**. The parity matrix previously said 134 even though its per-table counts sum to 137. The matrix total has been corrected to 137.

This is a static source inventory only. It is not proof of SQL Server parser acceptance or successful DDL execution.

## 9. Findings and required next work

### Cleared by this diagnostic

- Exact disposable database and SQL Server identity confirmed.
- All 13 Gate A tables absent.
- Legacy Buildings/Apartments key type, length, collation, and key order confirmed.
- Four expected EF migrations confirmed.
- Full legacy PK/UK, FK, CHECK/default, index, view, and dependency inventory collected.

### Still blocking Gate A

1. Review EF Core model and migration snapshot against all 318 live columns, 32 tables, 59 indexes, and 32 FK constraints; include FullSchema drift.
2. Resolve the missing wallet-view deployment and consumer contract.
3. Finalize credential policy and verify legacy hash formats without exposing credential material; define invalid-credential recovery.
4. Complete manual identity-link decisions. Do not infer a Person match from name, phone, or email alone.
5. Reconcile financial totals, carryovers, adjustments, and rounding before any financial migration.
6. Approve retention behavior for all 22 cascade relationships, including financial and voting history.
7. Resolve Gate A business decisions: GrantType / PermissionSetKey catalog, grantor/system actor policy, ownership completeness, permission scope, and uniqueness/concurrency rules.
8. Rehearse the exact proposed SQL on a fresh disposable restore, inspect the resulting 13 tables / 137 columns / constraints / indexes, and verify restore rollback.

## 10. Authorization boundary

**Gate A remains NOT AUTHORIZED.**

This assessment records metadata only. No Gate A DDL, EF migration, data backfill, credential migration, application cutover, production deployment, or destructive operation was performed or approved.
