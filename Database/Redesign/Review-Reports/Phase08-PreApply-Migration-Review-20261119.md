# Phase08 Pre-Apply Migration Review — 2026-11-19

**Branch:** `development/database-redesign-enterprise`  
**RestoreTest evidence:** `Phase08-RestoreTest-Preflight-20261119-011703.md`  
**Scope:** static review of the two pending Phase08 EF Core migrations against the reported RestoreTest migration history and table inventory. No database operation was executed for this review.

## 1. RestoreTest baseline

The published preflight report records:

- Server: `(localdb)\\MSSQLLocalDB`
- Database: `BuildingManagement_RestoreTest`
- State: `ONLINE`; access: `MULTI_USER`
- EF history has six migrations through `20261118184850_Phase07_OwnershipRecordProvenance`.
- Neither Phase08 migration is recorded as applied.
- None of the nine Phase08 Finance target tables exists.
- A `COPY_ONLY` backup was created and SQL Server `RESTORE VERIFYONLY WITH CHECKSUM` passed.
- No migration or business-data mutation was performed by preflight.

This establishes the expected schema starting point from the report. It does not by itself prove that the backup can be restored to a second database or that the target is isolated from any non-test workload.

## 2. Pending migration inventory

### `20261118213732_Phase08_FinancialLedgerFoundation`

Static Up() review found:

- 9 `CreateTable` operations:
  - `FinancialPeriods`
  - `FinancialReconciliationRuns`
  - `LedgerTransactions`
  - `FinancialReconciliationDifferences`
  - `FinancialAllocationBatches`
  - `FinancialSourceLinks`
  - `LedgerEntries`
  - `PaymentDeposits`
  - `FinancialAllocationLines`
- 35 `CreateIndex` operations.
- No `migrationBuilder.Sql`, `DropTable`, `DropColumn`, or `AlterColumn` operation in Up().
- The new tables have FK/check/unique-key constraints, including building/period scope and idempotency/source uniqueness.

The migration is additive DDL. It does not import or transform existing legacy rows.

### `20261118224524_Phase08FinanceReversalScopeIntegrity`

Static Up() review found:

- Drops two scalar reversal FKs, two associated indexes, and the existing PaymentDeposits resolution check.
- Creates two composite indexes and re-adds the check constraint.
- Adds two composite FKs that bind reversal references to the same `PeriodId` and `BuildingId`.
- No `migrationBuilder.Sql` or row-level data transformation.

The purpose is to prevent a reversal from pointing across a different financial period/building. Since the prior migration creates the new finance tables empty and the preflight reports that they do not yet exist, the expected application order starts this migration with no rows in those tables.

## 3. Rollback characteristics

- The Down() for `Phase08FinanceReversalScopeIntegrity` restores the weaker scalar reversal relationships and their indexes/check constraint.
- The Down() for `Phase08_FinancialLedgerFoundation` drops all nine newly created Finance tables.
- Therefore, rolling back the foundation after any ledger, payment, allocation, reconciliation, or period data has been written would destroy that new Finance data.

**Rollback rule:** the foundation migration may only be rolled back on a disposable rehearsal database before any business data is entered, or after an independently designed/exported recovery procedure. Do not treat EF Down() as a safe operational rollback after Finance usage begins.

## 4. Legacy-data and cutover boundary

The reported RestoreTest currently contains legacy finance data in tables such as `Deposits` (26 rows), `Expenses` (12 rows), `Revenues` (4 rows), and `MonthlyApartmentShares` (113 rows). The two Phase08 migrations do not migrate these rows into the new ledger model.

Consequently, applying the migrations would create an empty Finance schema foundation only. It would **not**:

- create opening balances or ledger entries from legacy data;
- migrate old deposits into `PaymentDeposits`;
- reconcile historical wallet balances;
- resolve the documented October 2026 rounding variance;
- implement posting, idempotency services, period close, or reversal workflows;
- authorize a feature-flag change or production cutover.

The legacy finance tables remain the existing data source until a separately reviewed migration/backfill and reconciliation phase is implemented.

## 5. Review conclusion

<badge>Conditional rehearsal readiness</badge>

Static source review found no row-destructive Up() operation in either pending migration. The first migration is additive; the second tightens reversal referential scope. The rollback of the first migration is destructive to all nine new tables and must not be used after Finance data is entered.

**Still required before applying to RestoreTest:**

1. Confirm `BuildingManagement_RestoreTest` is a disposable, isolated restore target and not a production/live source.
2. Confirm the reported backup file still exists and remains the intended recovery point. `RESTORE VERIFYONLY` passed, but a separate restore rehearsal is stronger evidence than VERIFYONLY alone.
3. Generate and inspect the exact EF SQL deployment script from the Phase07 migration through the final Phase08 migration.
4. Recheck the target database identity, migration history, and absence of all nine target tables immediately before application.
5. Apply only to the isolated RestoreTest database; then verify migration history, constraints, indexes, and row counts. Do not apply to Production.

**Status:** no EF migration applied; no database changed by this review; Production remains out of scope.
