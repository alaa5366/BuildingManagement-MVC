# Phase08 Generated SQL Source Review — 2026-11-19

- Branch: `development/database-redesign-enterprise`
- SQL artifact: `Database/Redesign/Generated-Sql/Phase08-20261119-012912.sql`
- SQL blob SHA: `f1161967c4acc79b006a68ee9c077b51f9a721f2`
- Expected SHA-256: `CFA0D13D61AE0389660C56462987A3B94B723AAE50207CC6A89BD1D6A8D5F616`
- Reviewed SQL length: 368 lines including final newline (36?); 22,391 characters.
- Review mode: read-only inspection of the complete generated SQL and comparison with both Phase08 migration source files, model snapshot, finance entity definitions, and finance target design.
- Database changes: none. SQL was not executed.

## 1. Overall result

**SQL source review: PASS WITH ONE MEDIUM DOMAIN-CONSTRAINT FOLLOW-UP.**

The generated SQL matches the reviewed EF migration operations in the expected order. No unexpected table/column deletion, data rewrite, truncate, or business-data DML was found. The SQL is suitable to proceed to a separately guarded, disposable RestoreTest rehearsal after the remaining pre-apply identity/backup/history gates are rechecked.

This is not approval for production execution, finance cutover, or treating the target ledger as authoritative.

## 2. Migration 1 — additive financial foundation

Verified the generated SQL creates the nine expected tables:

1. FinancialPeriods
2. FinancialReconciliationRuns
3. LedgerTransactions
4. FinancialReconciliationDifferences
5. FinancialAllocationBatches
6. FinancialSourceLinks
7. LedgerEntries
8. PaymentDeposits
9. FinancialAllocationLines

Reviewed the generated primary/alternate keys, foreign keys, check constraints, defaults, rowversion columns, and indexes against `20261118213732_Phase08_FinancialLedgerFoundation.cs`.

Notable integrity controls present:
- Building-scoped period uniqueness and period/building composite references.
- Ledger transaction alternate key `(LedgerTransactionId, PeriodId, BuildingId)`.
- Ledger entries and allocation records reference matching period/building/transaction scope.
- Apartment references use the composite `(BuildingId, ApartmentId)` key.
- Source/idempotency unique indexes are emitted as defined by the model.
- Decimal money columns use `decimal(19,4)`; allocation weights/shares use `decimal(19,8)`.
- SHA-256-sized fingerprints are constrained to 32 bytes.
- Payment deposit status and amount checks are present.

The SQL inserts the first migration-history row only after its schema operations and commits that migration transaction.

## 3. Migration 2 — reversal scope integrity

Verified the generated SQL:
- Drops the two original single-column reversal foreign keys.
- Drops the old reversal indexes and the old deposit-resolution check before replacing them.
- Adds period/building-scoped reversal indexes.
- Recreates the deposit-resolution check with explicit NULL requirements for `ReversalTransactionId` in Pending, Confirmed, and Cancelled states.
- Adds composite self-reference for ledger reversals using `(ReversesTransactionId, PeriodId, BuildingId)`.
- Adds composite payment-deposit reversal reference using `(ReversalTransactionId, PeriodId, BuildingId)`.
- Inserts the second migration-history row only after these operations and commits the second migration transaction.

These operations match `20261118224524_Phase08FinanceReversalScopeIntegrity.cs` and the model snapshot's composite relationship definitions.

The migration's `Down` path restores the previous single-column references/indexes and previous check definition; no table or business-row deletion is part of this correction migration.

## 4. Destructive-operation and DML scan

No `DROP TABLE`, `DROP COLUMN`, `TRUNCATE`, business-row `DELETE`, or business-row `UPDATE` statement appears in the generated artifact.

The only DROP operations are the expected replacement of the two reversal foreign keys, their indexes, and the deposit-resolution check constraint.

The only INSERT statements target `__EFMigrationsHistory`, one row per migration. No business data is inserted or rewritten.

The script contains two separate EF migration transactions, each committed independently. Therefore, if the second migration fails after the first commits, the database can be left at the first migration boundary. The rehearsal runner must detect and report this partial state; it must not claim all-or-nothing across both migration transactions or attempt an automatic destructive rollback.

## 5. Follow-up finding

### F08-SQL-01 — ResolutionReason is not constrained to NULL for Pending/Confirmed deposits
**Severity: Medium — correct before operational deposit workflow is enabled.**

The final `CK_PaymentDeposits_Resolution` requires a nonblank `ResolutionReason` for Cancelled and Reversed states, but Pending and Confirmed branches do not require `ResolutionReason IS NULL`. This permits a stale or semantically contradictory resolution reason on an unresolved or confirmed deposit.

This does not prevent the additive schema rehearsal and is not a SQL-generation discrepancy: the generated SQL matches the migration and snapshot. Tighten the entity/model constraint and add state-transition tests in a follow-up migration before exposing operational deposit commands.

## 6. Operational limitations that remain

The schema is only a foundation. The prior post-publication review findings remain open:
- Resident credential-purpose selection (High).
- Session revalidation against current role assignment (High).
- Multi-apartment resident context selection (Medium).
- Finance posting/idempotency enforcement, immutability, deterministic allocation, period close/reopen, and reconciliation execution are not yet implemented as a complete operational service layer (High for finance cutover).

The unique indexes provide database uniqueness, but do not alone implement safe transactional command handlers, retry semantics, immutable posting, or a complete ledger.

## 7. Execution gate

- SQL generation: completed.
- Full generated SQL source review: passed with the medium follow-up above.
- SQL execution: **NOT PERFORMED**.
- EF migrations: **NOT APPLIED**.
- Business data changes: **NONE**.
- Production/cutover authorization: **NOT GRANTED**.

Next permitted action: prepare and run a guarded rehearsal against only `(localdb)\\MSSQLLocalDB` / `BuildingManagement_RestoreTest`, after revalidating exact target identity, backup existence and `RESTORE VERIFYONLY WITH CHECKSUM`, expected Phase07 migration history, and absence of Phase08 target objects. Preserve the backup and do not run rollback against the retained RestoreTest database; any rollback rehearsal must use a separate disposable restore.
