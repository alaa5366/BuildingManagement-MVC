# Financial and Wallet Code Review — 2026-11-18

**Repository:** `alaa5366/BuildingManagement-MVC`  
**Branch:** `development/database-redesign-enterprise`  
**Scope:** source review of `WalletService`, `BuildingSqlMapper`, `Models/Building.cs`, `Data/Entities.cs`, `Data/AppDbContext.cs`, and the expense/revenue controller call sites.  
**Execution:** repository source inspection only. No application execution, test execution, SQL query, or database modification was performed by this review.

## 1. Executive decision

The current wallet is calculated from a mixture of monthly source transactions and persisted monthly snapshots:

- Source ledgers: confirmed deposits, expenses, revenues, and wallet adjustments.
- Stored monthly snapshots: per-apartment expense share, revenue share, and carry-over in `MonthlyApartmentShares`.
- Current apartment attributes: `MonthlyFee` and `Closed`.

This mixture creates a historical reproducibility problem. The balance calculation uses current apartment fee/closed state when evaluating any month in history. Before migrating financial records into a new normalized wallet model, historical monthly fee and apartment eligibility must be preserved as dated/monthly snapshots or derived from an approved historical policy.

The small aggregate differences found in profiling (+0.22 expense shares and +0.04 revenue shares) cannot be attributed to the rounding algorithm from aggregate totals alone. The current code's distribution algorithm explicitly assigns the rounding remainder to one apartment, so a complete distribution over the same eligible apartment set should sum exactly to the ledger total.

## 2. Findings

### FIN-01 — Historical wallet balances use the current monthly fee
**Severity: High / financial-history correctness**

`WalletService.ComputeWalletBalance(building, aptId, monthKey)` obtains `monthlyFee` from the current `Apartment.MonthlyFee` and applies it when computing any requested month. There is no monthly fee snapshot in `MonthlyApartmentShareEntity`; that entity stores only `ExpenseShare`, `RevenueShare`, and `CarryOver`.

If an apartment's monthly fee changes, recomputing or displaying a historical month can use the new fee instead of the fee applicable at that time. Since `ComputeCarryOverForNextMonth` calls `ComputeWalletBalance` for the previous month, a fee change can also affect subsequent carry-over calculations when they are recomputed.

**Required direction:** store a month-specific fee snapshot (or a fully auditable fee assessment ledger) and use it for historical calculations. Preserve the original monthly fee history; do not backfill all months from today's apartment fee.

### FIN-02 — Current Closed state is used for every historical month
**Severity: High / financial-history correctness**

`ComputeWalletBalance` decides whether to deduct the monthly fee using the apartment's current `Closed` flag. `RecalculateDistribution` and `RecalculateRevenueDistribution` also select recipients using the current `!Closed` predicate for whichever month is being recalculated.

An apartment closed today can therefore be treated as closed in an earlier month when recalculating that month. Conversely, an apartment that was previously closed but is now open can be included in historical distributions. `OpenDate` and `CloseDate` exist on the apartment model, but the wallet calculation does not use them to determine historical eligibility.

**Required direction:** define a dated apartment financial-eligibility policy and preserve the month-specific eligible-apartment set. A current boolean cannot safely reconstruct historical financial participation.

### FIN-03 — Distribution remainder is assigned to the last item in an unordered apartment list
**Severity: Medium / determinism and reconciliation**

`DistributeExactCents` rounds the equal share to two decimals for all but the final apartment, then assigns the exact remainder to the final apartment. This preserves the aggregate total, but the input order comes from `building.Apartments`. `BuildingSqlMapper.LoadAsync` loads apartment rows using `ToListAsync()` without an explicit `OrderBy`, so the remainder recipient is not defined by a stable business rule.

The aggregate total remains conserved when the same set of apartments is used, but the per-apartment cent allocation can vary with row-return order.

**Required direction:** define a deterministic allocation order (for example, a documented immutable apartment key order) and persist the allocation result/version for each month. Do not recalculate historical allocations using an implicit SQL row order.

### FIN-04 — Stored monthly shares are loaded as authoritative wallet inputs
**Severity: High / migration and reconciliation**

`BuildingSqlMapper.LoadAsync` loads `MonthlyApartmentShares` and places each row's `ExpenseShare`, `RevenueShare`, and `CarryOver` into the aggregate's monthly dictionaries. `WalletService.ComputeWalletBalance` then consumes those persisted dictionary values. The expense/revenue source ledgers are loaded separately; the mapper does not recalculate shares while loading.

`BuildingSqlMapper.SaveAsync` persists the aggregate's dictionaries back to `MonthlyApartmentShares`, rounded to two decimals. This means a stored share can remain stale relative to its source expense/revenue ledger until a recalculation path is invoked. The controllers recalculate distributions on normal expense/revenue add/delete operations, but that does not prove every historical/import/repair path has done so.

**Required direction:** treat source ledgers and monthly share snapshots as separate evidence. Reconcile them at building/month/apartment level. Do not regenerate all shares during migration until the exact historical eligibility and rounding policy are agreed and a before/after comparison is retained.

### FIN-05 — Aggregate differences need apartment-level attribution
**Severity: High / unresolved data-reconciliation blocker**

The profiling run reported:
- Building `LmkUDOJcjlvjiGmluz6c`, 2026-10: expense ledger 4,700.00 vs stored shares 4,700.22 (+0.22); revenue ledger 27,173.00 vs stored shares 27,173.04 (+0.04).

The current C# distribution method assigns the remainder so that the sum of a complete share set equals the source total. Therefore these deltas are not explained by the remainder assignment alone. Plausible causes requiring evidence include stale share rows, missing/extra apartment share rows, different eligible-apartment sets, legacy allocation rules, or historical manual adjustments embedded in share values.

**Required direction:** run a read-only per-apartment reconciliation for the affected building/month, including share-row counts, missing/extra apartments, current closed status, monthly fee, source totals, stored shares, and deterministic expected shares under a clearly labelled candidate algorithm. Keep candidate calculations separate from historical truth; do not overwrite the stored shares.

### FIN-06 — Carry-over is a derived snapshot but depends on mutable prior-month inputs
**Severity: High / financial-history correctness**

`ComputeCarryOverForNextMonth` computes each apartment's carry-over from the previous month's `ComputeWalletBalance`. `RecomputeCarryOverFrom` recalculates carry-over for each subsequent month. Since the previous-month balance uses current `MonthlyFee` and current `Closed` state (FIN-01/FIN-02), recomputing a chain can rewrite historical carry-over semantics based on present-day apartment settings.

**Required direction:** distinguish immutable posted monthly closing balances from draft/recomputable calculations. Define when a month becomes posted/closed, how corrections are authorized, and how a restatement is recorded without silently rewriting previously issued statements.

### FIN-07 — Deposit status handling is explicit in the wallet service
**Severity: Positive control / preserve**

`CreateDeposit` creates pending deposits. `ConfirmDeposit` and `CancelDeposit` transition only pending records. `TotalConfirmedDeposits` and `ComputeWalletBalance` count only `confirmed` deposits. Pending and cancelled deposits do not increase the wallet balance. Preserve status, confirmation/cancellation actors/timestamps, reasons, and edit history in the target model. The 16 cancelled deposits (6,356.00) from profiling must not be counted as collected revenue.

## 3. Target finance design implications

The target model should separate:

1. **Financial source transactions:** confirmed deposits, expenses, revenues, and adjustments with immutable IDs, status transitions, actor, timestamps, and correction/reversal history.
2. **Monthly assessment snapshots:** fee assessed per apartment, eligible apartment set, expense/revenue allocation policy/version, exact per-apartment shares, and rounding remainder assignment.
3. **Monthly closing:** posted opening/carry-over balance, assessed fee, confirmed deposits, expense share, revenue share, adjustments, closing balance, and posting/reversal/restatement references.
4. **Reconciliation evidence:** source-ledger totals vs allocation totals, per-apartment variances, unresolved variance reason, reviewer, and approval state.

Do not treat a recomputed wallet balance as historical truth until the applicable fee, eligibility, distribution, and posting rules are evidenced.

## 4. Next read-only diagnostic

Prepare a disposable-restore-only SQL diagnostic for:
- The affected building/month (expense and revenue deltas).
- Stored share row count vs apartment count and open/closed counts.
- Per-apartment expense/revenue shares, monthly fee, closed/open state, and carry-over.
- Ledger totals and share totals at full decimal precision.
- Other months where share totals differ from their ledgers.
- Missing share rows and share rows for apartments no longer present.

The diagnostic must not recalculate or update stored shares, and must not export personal or payment credentials. Results should be CSV per result set and pushed only under the diagnostics results folder.

## 5. Work boundary and next gate

This review is based on repository source only. No source code was changed, no tests were executed, no SQL diagnostic was run, and no financial rows were updated or deleted.

**Gate A remains blocked** until historical fee/eligibility semantics, the +0.22/+0.04 differences, carry-over restatement policy, cancelled-deposit treatment, wallet adjustments, and retention rules are resolved. No DDL/DML, EF migration, financial backfill, or cutover is authorized by this review.
