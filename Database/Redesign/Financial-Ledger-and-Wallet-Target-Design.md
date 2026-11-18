# Financial Ledger and Apartment Wallet Target Design

**Date:** 2026-11-18  
**Branch:** development/database-redesign-enterprise  
**Status:** Target financial design proposal based on review Sections 19 and 25 and the supplied restored-database profiling. No finance schema or data is changed by this document.

## 1. Design objective

Redesign financial commands and reporting without changing the meaning of historical apartment balances until the legacy rules are formally approved and reconciled.

The target must distinguish:
- payment evidence and deposit workflow;
- posted cash/financial movements;
- apartment wallet liability/position;
- monthly charges and allocations;
- expenses and revenues;
- opening/carryover balances;
- adjustments and reversals;
- report-period totals and actual wallet balances.

A report subtotal must never be labelled as the full wallet balance unless it includes every approved component of the balance formula.

## 2. Current legacy wallet behavior to preserve during parity

The reviewed legacy calculation currently depends on these components:

- carryover/opening amount;
- confirmed deposits;
- monthly fee, except where the apartment is closed for the applicable period;
- expense allocation to eligible open apartments;
- revenue allocation to eligible open apartments;
- wallet adjustments.

Pending and cancelled deposits are excluded from collected funds. Expense/revenue shares are calculated only for open apartments under the reviewed rules. Existing rounding remainder allocation depends on ordering and must be replaced with an explicit deterministic rule only after approval.

The legacy view vw_ApartmentWalletMovements is only confirmed deposits plus wallet adjustments. It is not the full wallet balance and must remain a movement subtotal.

## 3. Proposed target concepts

These are logical concepts for a later Finance phase, not Gate A tables:

| Concept | Purpose | Key invariants |
|---|---|---|
| FinancialPeriod | Defines month/period, timezone, open/closed state and close metadata | One canonical period key per building; no implicit server-local month |
| FinancialSource | Identifies legacy or new source record and source type/key | Unique source identity; retained through reconciliation |
| LedgerTransaction | Atomic business posting group | All lines commit together; idempotency key; actor/reason/correlation |
| LedgerEntry | Immutable debit/credit or signed movement line | No in-place amount edit after posting; reversal references original |
| ApartmentWalletProjection | Read model of an apartment's position for a period | Derived from approved ledger/rules; rebuildable and reconcilable |
| AllocationBatch | One approved period allocation run | Frozen input set, algorithm version, deterministic output |
| AllocationLine | Apartment-level amount produced by allocation | Total allocated equals source amount within approved currency precision |
| FinancialAdjustment | Controlled correction/adjustment command | Reason, actor, period, source, approval and audit |
| ReconciliationRun | Compares legacy and target by building/apartment/period/component | Immutable run evidence and versioned rules |
| ReconciliationDifference | One variance with classification and disposition | No auto-correction; owner approval for unresolved variance |

Do not create a new table until the accounting model, opening balance policy, and legacy mapping are approved.

## 4. Command state machines

### 4.1 Deposits

- CreateDeposit: amount > 0, valid apartment/building, strict period resolution, valid source and receipt metadata; initial state Pending.
- ConfirmDeposit: conditional Pending -> Confirmed; actor, timestamp, idempotency key and ledger posting are atomic.
- CancelDeposit: conditional Pending -> Cancelled; reason and actor required.
- A Confirmed deposit is not cancelled by changing its state or deleting it. Correct it through a linked reversal/void transaction with reason and authority.
- Concurrent confirm/cancel requests must result in exactly one successful transition.
- Posted ledger movement and deposit state must commit in one database transaction or via an explicitly recoverable outbox design.

### 4.2 Expenses and revenues

- Validate amount, category existence, category type, building scope, active status, period and target apartment/allocation policy at the application boundary.
- A posted expense/revenue is immutable. Corrections use a reversal and replacement posting.
- A category cannot be used across buildings unless the category model explicitly supports a system-wide catalog.
- Maintenance-to-expense linkage must preserve MaintenanceRecordId/source reference and support reversal if the maintenance record changes.

### 4.3 Wallet adjustments

- Require non-zero amount within approved limits, exact apartment/building, valid period, reason, actor and explicit permission.
- Use optimistic concurrency and idempotency.
- Preserve the original adjustment; correct through a compensating adjustment, not an edit/delete.
- Do not allow a generic service call to silently change a wallet balance without an immutable source movement.

## 5. Period and time semantics

- Store system instants in UTC; define financial month as a business-local period using an approved building timezone.
- Canonical month key is strict YYYY-MM; reject malformed values rather than silently dropping the filter.
- Every command must resolve the period from the persisted source record or explicit business date, not trust a posted month that may differ from the deposit's actual month.
- Period close freezes the allocation input set and records the calculation/rule version.
- Reopen requires privileged authorization, reason, audit, and recalculation plan.
- Avoid exact-day monthly scheduler logic that skips months without day 29/30/31; use calendar-aware period scheduling.

## 6. Allocation and rounding

Every allocation batch records:
- source transaction/period;
- eligible apartment set and eligibility snapshot;
- allocation basis and weights;
- algorithm version;
- precision and currency;
- rounding mode;
- deterministic tie-break key;
- total source amount and total allocated amount;
- actor/job and completion status.

Recommended default: use a deterministic largest-remainder allocation with a stable Apartment key tie-break, so sum(allocations) equals the source amount exactly at the approved currency precision. This is a recommendation only; DEC-17 requires owner approval.

Do not distribute rounding remainder to whichever apartment happens to be last in an incidental SQL query order.

## 7. Reconciliation requirements

Reconciliation is required before any finance cutover. Compare at minimum:

1. Row counts by source table, building, period and status.
2. Deposits by Pending/Confirmed/Cancelled, including amounts.
3. Expense/revenue totals and allocated shares by building/month.
4. MonthlyApartmentShares rows and carryover values.
5. WalletAdjustments by building/apartment/period.
6. Computed wallet position for every apartment and period.
7. Closed-apartment treatment and fee waivers.
8. Rounding residuals and unallocated cents.
9. Reversal/void behavior and source links.
10. Report totals versus wallet positions; do not equate unlike metrics.

Every difference receives a reason code such as:
- source data inconsistency;
- legacy rounding;
- missing allocation;
- period mismatch;
- closed-apartment eligibility;
- duplicate/overlapping source;
- legacy adjustment;
- target calculation defect;
- unresolved/needs owner decision.

No reconciliation job may update the legacy source records to force parity.

## 8. Existing evidence and known variances

The supplied restored-database profiling included:
- one building/month with expense total 4,700.00 versus shares 4,700.22 (+0.22);
- the same building/month with revenue total 27,173.00 versus shares 27,173.04 (+0.04);
- other profiled building/month examples with exact expense/revenue share totals;
- confirmed deposits distributed across two buildings/months as documented in the profiling results;
- 16 cancelled deposits totalling 6,356.00, which must remain excluded from collected funds;
- a building with 15 monthly-share rows across September/October/November 2026 and no expense/revenue shares in the captured data;
- wallet adjustments in one building with a net +200.00.

These are profiling observations, not instructions to alter any amount. The reconciliation report must explain them row-by-row before a target ledger becomes authoritative.

## 9. Read model and reporting design

Separate queries:
- ApartmentWalletStatement: complete apartment balance components and movements.
- BuildingFinancialSummary: collected deposits, revenues, expenses, adjustments and wallet liabilities separately.
- DepositQueue: pending evidence and actions.
- AllocationPreview: eligible apartments, weights, rounding preview.
- FinancialPeriodSummary: period totals and close status.
- FinancialReconciliationReport: legacy vs target components and differences.

Do not calculate financial reports by hydrating the whole building aggregate and iterating in memory. Use scoped SQL projections and bounded paging. Keep reports consistent about selected period vs current month; label every date basis.

## 10. Financial integrity controls

- SQL transactions for multi-row posting and allocation.
- RowVersion/conditional update for concurrent state transitions.
- Idempotency keys for all externally retried commands.
- Append-only ledger postings and explicit reversals.
- Restrictive FKs for source records and audit history.
- Durable audit/outbox for every financial command.
- No secrets or receipt OCR raw text in audit metadata.
- Currency/decimal precision explicitly approved; never use floating-point money.
- Rebuildable projections with reconciliation checksums.
- Permission split between read, confirm, cancel, adjust, post, reverse, export, close and reopen.

## 11. Owner decisions

- **DEC-15 — Accounting basis:** Is the apartment wallet a resident-facing liability/credit balance, a cash allocation, or a hybrid? Recommendation: preserve current semantics and name every component precisely until an accounting owner approves a ledger definition.
- **DEC-16 — Opening balance and period close:** Should carryover be represented as an explicit opening ledger posting per apartment/period? Recommendation: yes, with source provenance and no implicit carryover mutation.
- **DEC-17 — Rounding:** Approve deterministic largest-remainder allocation with stable apartment-key tie-break, or specify another accounting rule.
- **FIN-01:** Which business timezone defines month boundaries per building? Recommendation: explicit timezone setting per building with a controlled fallback.
- **FIN-02:** Which roles may confirm deposits, reverse postings, reopen a period, and approve high-value adjustments? Recommendation: separate duties and step-up MFA for high-risk actions.
- **FIN-03:** Are confirmed deposits legally final after posting, or can an operator reverse them under defined authority? Recommendation: immutable original plus reversal; never delete.

These are owner decisions. No financial schema or behavior is changed until the relevant decisions are accepted.

## 12. Migration sequence

1. Freeze and profile all legacy finance sources.
2. Document current formulas with executable parity fixtures from a disposable restore.
3. Approve accounting terms, period semantics, rounding and reversal authority.
4. Implement new read-only projection in parallel with legacy calculations.
5. Compare every apartment/building/month and classify all variances.
6. Implement ledger commands without cutover.
7. Dual-read/compare under feature flag; do not dual-write unless idempotent transactional coordination is proven.
8. Cut over one building or controlled cohort only after zero unexplained variance and rollback rehearsal.
9. Retire old wallet calculation only after historical and operational acceptance.

## 13. Safety status

This is a financial target design. No financial schema was created, no balance recalculated, no source row changed, and no EF migration was generated. No build or test was run. Gate A remains NOT AUTHORIZED.
