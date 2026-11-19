# Phase 43 â€” Deposit and ledger cutover

- Run: 2026-11-19T04:19:23.0740992+02:00
- Database: BuildingManagement_RestoreTest
- Mode: APPLY

## Results
- Database: BuildingManagement_RestoreTest
- Mode: APPLY
- Source deposits=26; confirmed=10; cancelled=16; confirmed amount=28611.0000; cancelled amount=6356.0000; buildings=2; months=2
- Target deposits=26; confirmed ledger transactions=10; confirmed ledger entries=10; target periods=3
- Admin actor policy: exactly one active building-scoped Admin account per building; SuperAdmin is never selected.
- Cancellation disposition: owner-approved initial system testing reason; cancelled deposits create no ledger movement.
- Timestamp policy: legacy SQL DateTime values are interpreted as Egypt local time and converted to UTC; source values remain immutable.
- PHASE43 APPLY AND RECONCILIATION COMPLETED.

## Guarantees
- Restricted to BuildingManagement_RestoreTest; a single SQL transaction protects all writes.
- Every legacy deposit is represented in PaymentDeposits with the same source key, building, apartment, amount, and state.
- Confirmed deposits create one posted deposit transaction, one positive apartment ledger entry, and one source fingerprint link.
- Cancelled deposits remain cancelled, have no ledger posting, and carry the owner-approved initial-testing disposition.
- One active building-scoped Admin account is used for each building; no SuperAdmin account is selected.
- Source Deposits, Phase29 evidence payloads, hashes, and historical edit rows are never updated or deleted.
- Periods are limited to source MonthKey values in September/October 2026 and use Africa/Cairo boundaries.
- Deterministic identifiers and source uniqueness make reruns idempotent.
