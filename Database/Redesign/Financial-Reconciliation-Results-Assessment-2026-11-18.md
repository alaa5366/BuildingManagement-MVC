# Financial Reconciliation Results Assessment — 2026-11-18

**Branch:** `development/database-redesign-enterprise`  
**Database:** `BuildingManagement_RestoreTest`  
**Run:** `Database/Diagnostics/Financial-Reconciliation-Results/20261118-152535/`  
**Scope:** read-only reconciliation; no financial rows or schema objects were changed.

## 1. Run integrity

The runner exported five result sets (215 rows total). The identity result confirms the intended restored database. The SQL is SELECT/metadata-only; it did not recalculate or update stored shares.

## 2. Confirmed monthly reconciliation

### Building `LmkUDOJcjlvjiGmluz6c`, 2026-10

- Expense ledger: **4,700.00**; stored apartment expense shares: **4,700.22**; variance: **+0.22**.
- Revenue ledger: **27,173.00**; stored apartment revenue shares: **27,173.04**; variance: **+0.04**.
- 60 apartment share rows exist for 60 apartments; 57 are currently open and 3 currently closed.
- All 57 currently open apartments have stored shares of 82.46 expense and 476.72 revenue; the three closed apartments have zero shares.
- The deterministic candidate allocation gives 82.46 / 476.72 to 56 apartments and assigns the final remainder to one apartment: 82.24 expense and 476.68 revenue. The candidate sums exactly to the source ledgers.
- The exported comparison shows the entire +0.22 / +0.04 overage concentrated in the last candidate apartment. This is consistent with the stored snapshot having rounded every eligible apartment independently without applying the final remainder correction. It is not evidence of a missing or orphan share row.

**Decision:** classify this as a confirmed persisted allocation/rounding variance in the 2026-10 snapshot. Do not silently rewrite the historical shares. Resolve through an approved financial adjustment/restatement with a preserved before/after audit trail, or explicitly accept and document the variance under an approved accounting policy.

The candidate's ApartmentId ordering is a diagnostic convention, not proof of the historical order or historical eligible set. The exact affected apartment is therefore a comparison target, not a conclusive historical attribution of who should absorb the remainder.

### Building `zxhRd7cwZ07rZotXeQ17`, 2026-09 through 2026-11

- Expense and revenue ledgers and their stored share totals are all zero for the three months.
- Each month has 15 stored share rows for 15 apartments; 9 are currently open and 6 currently closed.
- September includes 5,119.00 confirmed deposits and 6,356.00 cancelled deposits. Cancelled deposits must remain excluded from collected wallet funds.
- Carry-over totals are 0.00 in September, 3,319.00 in October, and 1,519.00 in November. The per-apartment output shows the positive carry-over concentrated in apartment 12, while several currently open apartments have negative reconstructed balances based on the current 200.00 fee.
- This pattern is consistent with a deposit balance in apartment 12 offset by unpaid monthly fees elsewhere. It is not a ledger/share-total mismatch. However, the calculation is based on current fee/closed state and does not prove historical eligibility or historical fees.

**Decision:** preserve these stored carry-over snapshots pending a month-by-month accounting review. Do not infer that negative apartment balances are errors or net them across apartments without an approved wallet policy.

### Building `ZzgYia4qMZ23N8qhiCOV`, 2026-09 through 2026-11

- September expense/revenue ledger totals (900.00 / 1,800.00) exactly match stored shares.
- October expense/revenue ledger totals (2,700.00 / 4,500.00) exactly match stored shares.
- September and October have 4 share rows for 4 apartments; 3 are currently open and 1 currently closed.
- November has no monthly-share rows and no expense/revenue ledger rows. Two wallet adjustments total +200.00. The run date is November 18, 2026, so November is an in-progress month in this snapshot; absence of monthly shares is not treated as a defect.

**Decision:** no historical expense/revenue share variance is evidenced for September/October. November remains an open-period observation, not a failed reconciliation.

## 3. Missing/orphan checks

Result set 05 returned only its header. No missing share rows or orphan share rows were detected in the building/month scope returned by this diagnostic.

## 4. Accounting and migration implications

1. Persisted monthly share rows are historical evidence and must not be regenerated from today's apartment fee/closed status.
2. Future allocation must use a stable, explicitly versioned eligibility snapshot and deterministic ordering, and must assign rounding remainder so the sum of shares exactly equals the source ledger total.
3. Store assessed fee, eligibility, algorithm version, rounding policy, posting/closing timestamp, and actor with each monthly assessment.
4. Corrections to posted periods must be explicit adjustments or restatements with approval and immutable before/after evidence; no silent overwrite.
5. Keep pending/cancelled/confirmed deposit states distinct. Cancelled deposits are not collections.
6. Carry-over must be apartment-scoped; do not offset one apartment's debt against another apartment's credit unless a separately approved building-level policy explicitly permits it.

## 5. Remaining finance gate

Before Gate A can proceed, decide:
- Whether the 2026-10 variance is corrected through a posted adjustment/restatement or retained as a documented legacy variance.
- Whether negative apartment balances are valid apartment-level debt and how they are displayed/collected.
- Month closing/posting rules and correction authority.
- Historical fee and eligible-apartment snapshots for old periods.
- The exact semantics and audit treatment of wallet adjustments.

The diagnostic is evidence only. No stored shares, deposits, adjustments, wallet balances, schema objects, or credentials were modified. No EF migration, backfill, or application cutover was performed.
