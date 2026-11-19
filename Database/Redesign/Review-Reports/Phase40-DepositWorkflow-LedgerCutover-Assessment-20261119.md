# Phase40 — Deposit Workflow and Ledger Cutover Assessment

- Date: 2026-11-19
- Branch: `development/database-redesign-enterprise`
- Scope: read-only source/code assessment; no database rows, balances, deposit statuses, or ledger postings changed.
- Decision: do not backfill legacy deposits into operational `PaymentDeposits` or post them to `LedgerTransactions` until the readiness conditions below are met.

## 1. Legacy deposit source contract

The legacy `Deposit` aggregate stores:
- Deposit identity/number, apartment, amount, note, status and receipt metadata.
- Submitted timestamp and free-text `CreatedBy`.
- Confirmation timestamp and free-text `ConfirmedBy`.
- Cancellation timestamp, free-text `CancelledBy`, and cancellation reason.
- Edit history with timestamp, free-text actor, reason, before/after amount and note.

The legacy `WalletService` currently:
- Creates pending deposits and generates numbers from the in-memory maximum for that month.
- Confirms a pending deposit by changing its status and storing a timestamp and free-text actor; it does not create a target ledger transaction.
- Cancels a pending deposit and stores a timestamp, free-text actor and optional reason; a null reason is currently converted to an empty string.
- Edits pending deposits and appends edit history.
- Includes only confirmed deposits in the legacy wallet balance.

## 2. Target schema contract

The additive target schema is materially stricter:
- Every payment deposit belongs to a real `FinancialPeriod` and a building/apartment pair enforced by foreign keys.
- A confirmed deposit requires a posted ledger transaction, confirmation timestamp, and a valid `UserAccount` actor.
- A cancelled deposit requires cancellation timestamp, valid account actor, and a non-empty resolution reason.
- A reversed deposit requires its original posted transaction, reversal timestamp, valid account actor, reversal transaction, and non-empty reason.
- Source identity is protected by a unique `(SourceSystem, SourceKey)` key.
- Ledger transactions require period/building scope, idempotency key, and a complete source identity tuple when source metadata is supplied.

These constraints intentionally prevent treating a legacy text actor or a status string as proof of an account identity or a posted accounting event.

## 3. Confirmed legacy data snapshot

The previously reviewed September 2026 source set for building `zxhRd7cwZ07rZotXeQ17` contains 26 deposits:
- 10 confirmed deposits totalling 28,611.00 EGP.
- 16 cancelled deposits totalling 6,356.00 EGP.

Confirmed amounts remain part of the legacy wallet calculation; cancelled amounts remain excluded. This report does not reinterpret either group and does not create target postings.

## 4. Cutover blockers

| ID | Blocker | Required evidence / safe action |
|---|---|---|
| DEP-01 | Legacy actor fields are free text | Resolve each confirmed/cancelled actor to one exact canonical `UserAccount`; preserve unresolved text as source evidence and do not fabricate an account ID. |
| DEP-02 | Legacy confirmation/cancellation timestamps are text | Parse exact timestamps, normalize only when timezone/offset is explicit, and quarantine malformed or ambiguous values. |
| DEP-03 | Financial period timezone and UTC bounds are not proven for every building/month | Establish an approved timezone and period boundary per building; do not assume server-local time or Egypt time for all buildings. |
| DEP-04 | Legacy confirmation is not a ledger posting | Implement one atomic command that creates the transaction and entries, updates the deposit state, and writes provenance under a transaction and idempotency key. |
| DEP-05 | Cancellation reason may be blank in legacy data | Preserve the original blank value. Do not invent a reason to satisfy the target check constraint; map to an explicitly reviewed legacy disposition or keep the row in evidence-only staging. |
| DEP-06 | Deposit numbers are generated from an in-memory maximum | Use a database-enforced source key/unique number strategy for new operational deposits; do not rely on this legacy allocator for concurrent writes. |
| DEP-07 | Legacy statuses are strings, target statuses are a four-state lifecycle | Use an explicit, tested mapping: pending→Pending, confirmed→Confirmed only after valid posting proof, cancelled→Cancelled only with complete resolution evidence. Never map by default or infer Reversed. |
| DEP-08 | Edit history may contain multiple actor/time/reason records | Preserve each edit as immutable source evidence; do not overwrite the original amount/note or mistake the latest edit for a posting event. |

## 5. Required implementation sequence

1. Build a read-only deposit readiness report grouped by building/month/status, including missing/invalid actor mapping, timestamp parsing, reason, apartment reference, amount precision, and duplicate source keys.
2. Resolve account actors from authoritative identity links only. Unresolved actors remain blocked.
3. Resolve building timezone and exact financial period UTC boundaries.
4. Implement an idempotent deposit command service with explicit transitions, row-version concurrency, and a transactional outbox for audit/notifications.
5. For confirmation, create a posted `Deposit` ledger transaction and its apartment entry in the same database transaction as the deposit status transition.
6. For cancellation, require a non-empty reason and record the real account actor; do not create a wallet ledger entry.
7. For reversal, create a separate reversal transaction and equal/opposite entries linked to the original transaction; never edit or delete the original posting.
8. Add integration tests for duplicate requests, concurrent confirmation, cancellation reason, actor FK failures, period scope, apartment/building mismatch, and rollback on any failed write.
9. Reconcile target postings against the legacy confirmed-deposit totals before any wallet read path is switched to the ledger.

## 6. Current disposition

- The 26-row legacy set remains evidence-only.
- No confirmed deposit has been newly posted to the target ledger by this phase.
- No cancelled deposit has been promoted, deleted, or assigned a fabricated reason.
- No actor, timezone, period, timestamp, or financial event has been invented.
- Operational deposit cutover remains blocked until DEP-01 through DEP-04 and the corresponding integration tests are satisfied.
