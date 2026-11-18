# EF Core / Live SQL Schema Reconciliation — 2026-11-18

**Branch:** `development/database-redesign-enterprise`  
**Database evidence:** `Database/Diagnostics/Redesign-Schema-Parity-Results/20261118-180620/`  
**Scope:** static comparison of the checked-in EF Core model snapshot and migrations against read-only SQL Server catalog metadata. No database changes or application execution were performed.

## 1. Executive result

The live schema is substantially aligned with the EF Core snapshot for the mapped relational tables, but the deployment model is not fully self-contained:

1. The snapshot maps 30 physical application tables and one keyless view.
2. The restored database contains those 30 mapped tables, plus the EF migration-history table and a separate `dbo.LoginThrottle` table.
3. All 313 mapped table properties found in the snapshot have corresponding live columns. The remaining five catalog columns are accounted for by the three-column LoginThrottle table and the two-column EF migration-history table.
4. The 27 ordinary indexes in the snapshot match the live non-constraint indexes by table, ordered key columns, and uniqueness.
5. All 30 mapped primary keys match the live primary-key column order. The two additional primary keys belong to `__EFMigrationsHistory` and `LoginThrottle`.
6. One semantic type spelling difference exists: EF uses `rowversion` for `Buildings.RowVersion`, while SQL Server catalog reports the synonymous legacy type name `timestamp`. This is not a storage incompatibility.
7. The keyless EF view mapping `vw_ApartmentWalletMovements` has no corresponding view in the restored database.
8. `LoginThrottle` is created at application startup by raw SQL, outside the EF model snapshot and migration history. It is also present in the FullSchema artifact.

This is a useful parity result, not a release approval. Foreign-key semantics, SQL-only object lifecycle, view deployment, and FullSchema authority still require explicit reconciliation.

## 2. Table and column accounting

| Source | Tables / objects | Columns |
|---|---:|---:|
| EF model snapshot, physical tables | 30 | 313 |
| SQL-only `dbo.LoginThrottle` | 1 | 3 |
| EF migration history | 1 | 2 |
| **Live database total** | **32** | **318** |

The snapshot also contains one keyless entity mapped to `vw_ApartmentWalletMovements`; that object is not a physical table and is absent from the restore.

No live physical table was missing from the model comparison except the intentional/non-model SQL-only `LoginThrottle` table. No extra physical table was declared in the snapshot. No mapped table column was missing from the live catalog, and no live application-table column was left unexplained by this accounting.

## 3. Type and column comparison

- All mapped table properties compared against the live catalog have matching SQL types and declared lengths/precision in the reviewed snapshot metadata.
- `Buildings.RowVersion` is declared as `rowversion` by EF while SQL Server reports `timestamp`. SQL Server treats these as synonyms; preserve rowversion semantics and do not attempt a type conversion solely to normalize the spelling.
- The comparison used the model snapshot's explicit relational column types and the catalog's `sys.columns` metadata. It was a source/catalog comparison, not a generated EF migration diff or runtime model validation.

## 4. Primary keys and indexes

- All 30 primary keys declared for mapped physical tables match the live key column sequence.
- The additional live primary keys are:
  - `__EFMigrationsHistory(MigrationId)`;
  - `LoginThrottle(Key)`.
- All 27 non-constraint indexes declared in the snapshot match the live index inventory by table, ordered key columns, and uniqueness.
- The 59 live indexes account for the 30 mapped-table primary keys, 27 mapped secondary indexes, and the two infrastructure primary keys on `__EFMigrationsHistory` and `LoginThrottle`.
- The catalog reports no disabled indexes, filtered indexes, included columns, or descending key columns. Four mapped secondary indexes are unique: Apartments(BuildingId, Number), Buildings(BuildingNumber), Cameras(DvrId, Channel), and Residents(Uid).
- Fill factor and operational usage were not part of this comparison.

## 5. Foreign-key inventory

The live catalog contains 32 FK constraints, and the EF snapshot declares 32 relationships. A relationship-by-relationship comparison matched all 32 on dependent table/columns, principal table/columns, composite-column order, and delete behavior. All live FKs are enabled and trusted; none is disabled or marked untrusted.

The observed delete actions remain 22 CASCADE, 9 NO_ACTION, and 1 SET_NULL. The existing cascade examples include resident-to-apartment, financial records-to-building, deposit edit history-to-deposit, poll options/votes-to-poll parents, and user permissions-to-users. This confirms schema parity, not policy safety: the current delete actions still require retention review before target redesign or cutover.

### Cascade-retention conflict

The live schema and EF snapshot agree on the existing cascade behavior, but that behavior conflicts with the approved target retention principle:

- Deleting a Building cascades into Apartments, BuildingAdmins, BuildingSettings, Deposits, Dvrs, Expenses, FinancialCategories, Floors, MaintenanceRecords, MonthlyApartmentShares, Notifications, Polls, QrTokens, Revenues, and WalletAdjustments.
- Deleting an Apartment cascades into Residents.
- Deleting a Deposit cascades into DepositEditHistory.
- Deleting a Poll cascades into PollOptions, which in turn cascades into PollVotes.
- Deleting a User cascades into BuildingAdmins and UserPermissions.
- Deleting a Dvr cascades into Cameras.
- AuditLog's BuildingId FK uses SET_NULL rather than deleting audit rows.

This is not merely theoretical referential metadata: the current database explicitly permits physical deletion of substantial financial, voting, resident, and access-related history through parent deletion. The target design must use archive/disable/close workflows and restrictive retention FKs for posted financial records, ballots, identity, and audit evidence. Any legally required erasure must be a separate approved process. Do not physically delete a Building, Apartment, Deposit, Poll, or User as an ordinary lifecycle operation until the application workflows and retention policy are redesigned and tested.

## 6. Wallet view deployment gap

Evidence:
- `Data/AppDbContext.cs` maps `ApartmentWalletMovementsView` as a keyless entity to `vw_ApartmentWalletMovements`.
- `Migrations/AppDbContextModelSnapshot.cs` contains the same `ToView` mapping.
- None of the four applied migration source files contains a `CREATE VIEW` operation or references this view.
- The restore catalog reports zero views.
- `BuildingManagement-FullSchema.sql` contains a `CREATE VIEW [dbo].[vw_ApartmentWalletMovements]` definition.

The current evidence establishes a deployment-path mismatch. The view definition exists only in the FullSchema artifact among the inspected deployment sources; it is not installed by the four EF migrations. Search results found the DbSet and mapping, but no additional application consumer was established in this review. Do not infer that the view is actively queried merely because the DbSet exists.

**Recommended resolution:** keep EF Core migrations as the single authoritative deployment mechanism, and add an explicit migration for the view only after its SQL definition and consumer contract are reviewed. Treat FullSchema as generated/reference output, not a second independent deployment authority. This recommendation remains subject to owner decision DEC-28. If the view is unused, consider removing the mapping and view artifact instead of deploying an unnecessary database object.

## 7. LoginThrottle lifecycle

`Services/LoginThrottle.cs` executes startup SQL that creates `dbo.LoginThrottle` if absent, deletes expired rows, then restores persisted counters. The table is therefore a raw-SQL-managed persistence object rather than an EF entity.

The FullSchema artifact also declares the table. This creates three schema-lifecycle mechanisms in the repository:
- EF migrations for the mapped relational model;
- application startup DDL for LoginThrottle;
- the FullSchema SQL artifact.

The current LoginThrottle implementation intentionally uses process-local memory for active enforcement and SQL as restart-recovery state for a confirmed single-instance deployment. The SQL mirror is not distributed enforcement and may lose pending writes after an abrupt process stop.

**Target direction:** decide whether LoginThrottle remains an explicitly documented infrastructure-owned SQL object with versioned, idempotent schema provisioning, or becomes a first-class EF-managed object. Do not silently add it to the EF model without deciding ownership and startup ordering. Remove ad-hoc startup DDL once a canonical provisioning path is approved.

## 8. Migration history and deployment implications

The restore contains the four expected EF migrations, all at ProductVersion 8.0.10:
- `20261116120818_InitialCreate`
- `20261117012657_Phase04_StandardizeKeyColumnTypes`
- `20261117050000_Phase05_RemoveRedundantResidentIndexes`
- `20261117130000_AddUserTwoFactor`

These migrations account for the mapped model's migration history, but do not provision the wallet view or LoginThrottle. A successful EF migration run therefore does not, by itself, guarantee the full runtime database contract.

## 8A. FullSchema artifact drift

A static column-definition comparison was also performed against `BuildingManagement-FullSchema.sql`:

- FullSchema contains 32 table definitions and 314 parsed column definitions (including identity columns).
- The live restore contains the same 32 table objects when the 30 EF tables, LoginThrottle, and EF migration history are counted.
- FullSchema has no live-missing columns in the compared set.
- The live database has four columns absent from FullSchema: `Users.TwoFactorEnabled`, `Users.TwoFactorSecretEncrypted`, `Users.TwoFactorRecoveryCodesJson`, and `Users.TwoFactorLastUsedStep`. These are introduced by the applied `AddUserTwoFactor` migration.
- 119 matched FullSchema column definitions differ from the live catalog in declared string length. The comparison did not find type, precision/scale, or nullability differences among those 119 records; the drift is specifically length metadata.
- Examples include FullSchema bounded `nvarchar(n)` declarations where the live schema and current EF snapshot use `nvarchar(max)`, plus `Buildings.BuildingNumber` and `Residents.Phone` whose live lengths are wider than the FullSchema declarations.

This confirms that FullSchema is not a faithful current-schema deployment artifact for the restored database. It is stale in both the post-2FA column inventory and a substantial set of string-length declarations. Do not use it to recreate or alter a current database until it is regenerated from the authoritative model and reviewed. Preserve the old artifact for historical comparison only.

## 9. Required follow-up

1. Complete physical index review: included columns, filter predicates, sort direction, disabled state, and any indexes not represented by the snapshot.
2. Resolve DEC-28 and decide the wallet view's authoritative deployment path or remove it if unused.
3. Define the lifecycle owner for LoginThrottle and remove duplicate schema ownership between startup SQL and FullSchema.
4. Treat the current FullSchema as stale: reconcile its 119 string-length differences and four missing 2FA columns, then regenerate it from the approved authoritative source only after DEC-28 is decided.
5. Keep the existing cascade-retention classification open until financial, voting, audit, and identity retention rules are approved.

## 10. Verification boundary

- Evidence was read-only SQL catalog metadata and checked-in source files.
- The comparison was performed against the supplied disposable restore evidence.
- No EF migration was generated or applied.
- No SQL DDL/DML was executed.
- No application build, test run, or runtime query was performed as part of this review.
- Gate A remains **NOT AUTHORIZED**.
