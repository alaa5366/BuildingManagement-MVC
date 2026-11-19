# Finance Data Findings and Approved Logical Allocation Policy

- Date: 2026-11-19
- Branch: `development/database-redesign-enterprise`
- Target used by prior finance phases: `BuildingManagement_RestoreTest`
- Decision: use equal-weight Largest Remainder allocation for future recalculations, with stable ordinal `ApartmentId` tie-breaking.
- Scope: application allocation logic and finance-data disposition. No historical financial rows are rewritten by this code change.

## 1. Confirmed source-data findings

### 1.1 October 2026 allocation mismatch
Building: `LmkUDOJcjlvjiGmluz6c`.

- Expense source: 4,700.00; stored apartment shares: 4,700.22; difference: +0.22.
- Revenue source: 27,173.00; stored apartment shares: 27,173.04; difference: +0.04.
- 60 monthly share rows exist for 60 apartments. The reviewed snapshot has 57 non-zero/open-eligible shares and 3 zero shares.
- Existing values independently rounded each of the 57 eligible shares upward, causing the aggregate overage.
- Phase36 already registered both differences as `LegacyRoundingVariance`. No source rows or wallet balances were changed.

### 1.2 Deterministic equal-weight allocation math
For the same 57-recipient set, if equal allocation is the selected rule:

- Expenses: 4,700.00 / 57. Largest Remainder produces 35 shares of 82.46 and 22 shares of 82.45; total exactly 4,700.00.
- Revenues: 27,173.00 / 57. Largest Remainder produces 53 shares of 476.72 and 4 shares of 476.71; total exactly 27,173.00.
- Residual cents are assigned in ordinal ApartmentId order, so the result does not depend on SQL row order or in-memory collection order.

These are deterministic candidate normalized allocations, not proof of the historical recipient ordering. The legacy October rows remain preserved as historical evidence. A historical rewrite would invalidate Phase29 source fingerprints and alter previously displayed apartment balances; it is deliberately not performed by an ordinary recalculation.

### 1.3 Deposits
The reviewed September 2026 deposit set for building `zxhRd7cwZ07rZotXeQ17` contains 26 deposits:
- 10 confirmed deposits totalling 28,611.00.
- 16 cancelled deposits totalling 6,356.00.

Confirmed deposits count as collected funds. Cancelled deposits remain excluded. Do not convert cancelled rows to confirmed, delete them, or add their amounts to wallet balances. Preserve their resolution metadata and edit history.

### 1.4 Carry-over and balances
For `zxhRd7cwZ07rZotXeQ17`, carry-over totals were 0.00, 3,319.00, and 1,519.00 across September-November 2026. Credit is concentrated in one apartment while some other apartments have negative balances under the current 200.00 fee calculation.

Negative apartment balances are not automatically defects. They represent apartment-specific debt under the current calculation and must not be netted against another apartment's credit without an approved policy.

### 1.5 Reconciled building
Building `ZzgYia4qMZ23N8qhiCOV` reconciles for September and October 2026:
- September expense/revenue totals: 900.00 / 1,800.00, matching stored shares.
- October expense/revenue totals: 2,700.00 / 4,500.00, matching stored shares.
- November has no monthly share or expense/revenue rows in the 2026-11-18 snapshot and is treated as an open period, not a defect.
- Two November wallet adjustments total +200.00; preserve them with their source metadata.

## 2. Implementation completed

`WalletService.DistributeExactCents` now:
1. Rounds the source total to currency cents once using AwayFromZero.
2. Splits integer cents into a base allocation and residual cents.
3. Assigns residual cents using ordinal ApartmentId order.
4. Verifies that allocated shares equal the rounded source total exactly.
5. Is independent of apartment collection order.

Tests were added for the 100.00 / 3 apartment case and input-order independence. The source changes are committed on the branch. Build/test execution is pending the user's local run; no test pass is claimed here.

## 3. Remaining financial defects and required disposition

| ID | Finding | Safe disposition |
|---|---|---|
| FIN-01 | Historical balance calculation uses today's Apartment.MonthlyFee | Do not backfill old months from today's fee. Add effective-dated fee/assessment snapshots before ledger cutover. |
| FIN-02 | Historical eligibility uses today's Apartment.Closed | Do not infer past eligibility from current state. Use preserved month recipient set for legacy periods and dated eligibility for new periods. |
| FIN-03 | Previous allocator depended on collection order and assigned all residual to the last apartment | Fixed in application code with Largest Remainder and stable ApartmentId tie-break. |
| FIN-04 | MonthlyApartmentShares are treated as authoritative wallet snapshots | Preserve legacy snapshots and reconcile them; never silently regenerate posted historical shares. |
| FIN-05 | October 2026 +0.22/+0.04 variance | Already registered by Phase36. Keep visible as historical variance until a separately audited restatement is posted. |
| FIN-06 | Carry-over can be recomputed from mutable historical fee/eligibility | Preserve existing carry-over snapshots. Future period-close must freeze balances; later corrections require explicit adjustment/reversal. |
| FIN-07 | Expense/revenue deletion is physical through aggregate synchronization | Replace with void/reversal workflow before operational ledger cutover; preserve old source evidence. |
| FIN-08 | Malformed expense/revenue dates can fall back to first day of month in mapper | Reject malformed dates at command boundary; never silently change date. |
| FIN-09 | Expense/revenue and wallet adjustment command validation is incomplete | Validate positive source amounts, exact month keys, apartment/building membership, and actor/reason before mutation. |
| FIN-10 | Deposit numbering is derived from in-memory maximum and can race | Add a database uniqueness/sequence constraint and idempotent command handling before concurrent production use. |
| FIN-11 | Deposit status transitions and notifications/audit/carry-over are coordinated across controller paths | Move to one transactional application command with outbox and idempotency. |
| FIN-12 | Finance target schema exists but operational posting/allocation/closing/reconciliation services do not | Do not cut over or treat target ledger as authoritative until those services and tests exist. |

## 4. Why historical amounts were not automatically overwritten

Phase29 stores immutable source payloads and SHA-256 fingerprints for 158 legacy financial rows. Updating the original MonthlyApartmentShares rows would create source-fingerprint drift and change historic apartment balances. A valid restatement must instead create a separately identified, auditable correction with:
- a known building and period;
- the actual eligible apartment set;
- the allocation rule/version;
- exact per-apartment deltas;
- a linked source/reversal transaction;
- a real actor and timestamp;
- preserved before/after evidence.

The current source evidence does not prove the historical tie-break recipient, historical fee, or historical eligibility dates. No actor, period boundary, or posting event is fabricated.

## 5. Verification required on the user's machine

Run:
```powershell
git pull --ff-only
dotnet test
dotnet build --no-restore
```

Do not run any broad historical share recalculation. The deterministic allocation change applies when a recalculation is explicitly invoked; existing October 2026 source snapshots remain unchanged until a separately governed restatement is implemented.
