# Phase41 — Deposit Readiness Audit

- Date: 2026-11-19
- Branch: `development/database-redesign-enterprise`
- Script: `scripts/Invoke-Phase41-DepositReadinessAudit.ps1`
- Mode: read-only only; no `-Apply` switch exists.

## Purpose

Profile legacy deposits against the target financial and identity schema before any deposit backfill or ledger posting. The script is intentionally diagnostic: it does not create staging rows, financial periods, deposits, ledger transactions, ledger entries, source links, or wallet adjustments.

## Checks

- Required source/target tables and Phase08 / Phase20 / Phase34 migrations.
- Counts and amounts by building, month, and legacy status.
- Known legacy status values: pending, confirmed, cancelled; unknown values are blocked.
- Positive amounts and precision no finer than one cent.
- Valid `yyyy-MM` month keys.
- Exact apartment/building membership.
- Confirmation/cancellation timestamp and actor presence.
- Exact actor resolution only through `LegacyIdentityMap` rows for `legacy-sql / Users`, with `MappingStatus=1`, both PersonId and AccountId, and an existing UserAccounts row. No name, email, or fuzzy matching.
- Non-empty cancellation reason.
- Pending rows carrying contradictory resolution metadata.
- Existing target PaymentDeposits by exact source key `BuildingId|DepositId`.
- Existing financial periods and whether their timezone field is populated.

## Important timestamp clarification

The legacy EF entity defines `CreatedAt`, `ConfirmedAt`, and `CancelledAt` as `DateTime` / nullable `DateTime`, not text columns. The earlier Phase40 wording that called these timestamps text was imprecise. Phase41 corrects that: the audit checks timestamp presence, but it cannot prove whether historical SQL datetime values represent UTC, local time, or another convention. No timezone conversion is performed.

## Run

```powershell
git pull --ff-only
.\scripts\Invoke-Phase41-DepositReadinessAudit.ps1
```

Default target is the isolated `BuildingManagement_RestoreTest` LocalDB database. A different database name is rejected unless the script is deliberately reviewed and changed. The script emits a timestamped report under `Database/Redesign/Review-Reports`.

## Interpretation / cutover gate

A row is not ready merely because the aggregate scan runs successfully. Before ledger cutover:
1. Every confirmed deposit must have an exact, reviewed actor account mapping, a proven timezone interpretation, a valid building period, and a transactionally created posted ledger transaction.
2. Every cancelled deposit must have a valid actor and original non-empty reason. Blank historical reasons remain unresolved; do not manufacture text.
3. Every source apartment must resolve to the same building.
4. Duplicate source identities must be prevented by unique source keys and idempotent posting.
5. All posting and status changes must be atomic, concurrency-safe, reversible through explicit reversal transactions, and integration-tested.
6. Reconciliation must match the legacy confirmed-deposit totals before wallet reads switch to the ledger.

No historical deposit, actor, reason, period, timestamp interpretation, ledger entry, or balance was invented or changed by Phase41.
