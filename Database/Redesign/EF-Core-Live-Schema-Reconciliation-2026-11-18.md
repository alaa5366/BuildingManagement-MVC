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
- This does not yet constitute a complete comparison of index included columns, sort direction, filter predicates, fill factor, or operational usage. Those remain part of the detailed physical-index review.

## 5. Foreign-key inventory

The live catalog contains 32 FK constraints. All reported FKs are enabled and trusted; none is disabled or marked untrusted.

The inventory confirms that composite FK column order is represented explicitly in the evidence. A separate relationship-by-relationship comparison against EF's `HasForeignKey` / `OnDelete` model declarations is still required before calling FK parity complete. The existing delete actions must also be reviewed against the target retention policy; schema parity alone does not mean the current cascade policy is safe.

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

## 9. Required follow-up

1. Complete FK parity: compare all 32 live FK constraints to snapshot principal/dependent columns, composite order, optionality, and delete behavior.
2. Complete physical index review: included columns, filter predicates, sort direction, disabled state, and any indexes not represented by the snapshot.
3. Resolve DEC-28 and decide the wallet view's authoritative deployment path or remove it if unused.
4. Define the lifecycle owner for LoginThrottle and remove duplicate schema ownership between startup SQL and FullSchema.
5. Compare the FullSchema table/constraint definitions to the live catalog and EF migrations, documenting every drift rather than treating the artifact as authoritative by default.
6. Keep the existing cascade-retention classification open until financial, voting, audit, and identity retention rules are approved.

## 10. Verification boundary

- Evidence was read-only SQL catalog metadata and checked-in source files.
- The comparison was performed against the supplied disposable restore evidence.
- No EF migration was generated or applied.
- No SQL DDL/DML was executed.
- No application build, test run, or runtime query was performed as part of this review.
- Gate A remains **NOT AUTHORIZED**.
