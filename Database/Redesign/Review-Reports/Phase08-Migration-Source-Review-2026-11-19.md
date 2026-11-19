# Phase08 Migration Source Review — 2026-11-19

## Scope reviewed
- `Migrations/20261118213732_Phase08_FinancialLedgerFoundation.cs` (complete migration source)
- `Migrations/20261118224524_Phase08FinanceReversalScopeIntegrity.cs` (complete migration source)
- `Migrations/AppDbContextModelSnapshot.cs`
- `Migrations/20261118224524_Phase08FinanceReversalScopeIntegrity.Designer.cs`
- `Migrations/20261118184850_Phase07_OwnershipRecordProvenance.cs`
- `Data/AppDbContext.cs`
- Prior RestoreTest preflight report dated 2026-11-19 01:17:04

## Findings

### F08-SQL-01 — Composite reversal scope key is present and aligned
The Phase08 foundation migration creates alternate key
`UQ_LedgerTransactions_Period_Building` over
`(LedgerTransactionId, PeriodId, BuildingId)`. The follow-up migration adds
both reversal foreign keys against those exact principal columns. The latest
model snapshot contains the matching alternate key, composite indexes, and
composite relationships for both `LedgerTransactionEntity` and
`PaymentDepositEntity`. This resolves the earlier concern that the principal
key might be missing: it is explicitly created by the foundation migration.

**Disposition:** No key-definition blocker found in source review.

### F08-SQL-02 — Reversal migration operation ordering is coherent
The follow-up migration drops the two old single-column foreign keys and their
indexes before creating the replacement composite indexes and foreign keys.
Its Down method removes the composite constraints/indexes before restoring the
single-column indexes and foreign keys. The existing `CK_PaymentDeposits_Resolution`
is dropped before replacement and recreated on rollback.

**Disposition:** Up/Down dependency order is internally coherent by source
inspection. Runtime SQL Server execution is still required to prove it.

### F08-SQL-03 — Foundation rollback is destructive by design
The foundation migration Down method drops all nine Phase08 Finance tables.
This is expected for a schema-only migration, but it is not a safe rollback
against a database that has accepted financial records. Any rollback test
must use a disposable database restored from the verified pre-migration backup,
not a live database and not a populated test database whose records must remain.

**Disposition:** Do not run Down against a retained or operational database.

### F08-SQL-04 — Payment resolution check leaves reason unconstrained in two states
The revised `CK_PaymentDeposits_Resolution` requires a non-empty
`ResolutionReason` for Cancelled (Status 2) and Reversed (Status 3), but does
not require it to be NULL for Pending (Status 0) or Confirmed (Status 1).
This does not prevent migration application, but it permits stale/unexpected
resolution text in those two states.

**Disposition:** Medium data-integrity improvement. Decide whether the domain
requires `ResolutionReason IS NULL` for Status 0 and 1 before the ledger is
used operationally; preferably tighten the constraint and matching model
snapshot/configuration in a follow-up migration before production use.

## RestoreTest evidence carried forward
The prior preflight report records:
- Server: `(localdb)\\MSSQLLocalDB`
- Database: `BuildingManagement_RestoreTest`, state `ONLINE`
- Six migrations applied, latest Phase07
- No Phase08 Finance target tables present
- COPY_ONLY backup completed and `RESTORE VERIFYONLY WITH CHECKSUM` passed
- No EF migration was applied and no business data was changed

This is prior machine-reported evidence, not a new database connection or a
fresh backup performed by this review.

## Gate decision
**Source-level review: PASS WITH ONE MEDIUM MODEL-DOMAIN FOLLOW-UP.**
**Migration execution: NOT AUTHORIZED / NOT PERFORMED.**

Before any apply operation, generate the exact EF SQL script from the
Phase07 migration to `20261118224524_Phase08FinanceReversalScopeIntegrity`,
inspect it for unexpected destructive operations, verify the target database
identity and migration history again, and re-verify the backup. A separate
disposable restore must be used for rollback testing.

No application code, database, or business data was changed by this review.
