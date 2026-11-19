# Phase 44 â€” Expense, revenue and historical apartment-share cutover

- Run: 2026-11-19T04:32:58.7462823+02:00
- Database: BuildingManagement_RestoreTest
- Mode: APPLY

## Results
- Database: BuildingManagement_RestoreTest
- Mode: APPLY
- Expense: Building=LmkUDOJcjlvjiGmluz6c; Month=2026-10; source rows=8; source amount=4700.0000; apartment share rows=60; allocated=4700.2200; variance=-0.2200
- Revenue: Building=LmkUDOJcjlvjiGmluz6c; Month=2026-10; source rows=1; source amount=27173.0000; apartment share rows=60; allocated=27173.0400; variance=-0.0400
- Expense: Building=ZzgYia4qMZ23N8qhiCOV; Month=2026-09; source rows=1; source amount=900.0000; apartment share rows=4; allocated=900.0000; variance=0.0000
- Revenue: Building=ZzgYia4qMZ23N8qhiCOV; Month=2026-09; source rows=1; source amount=1800.0000; apartment share rows=4; allocated=1800.0000; variance=0.0000
- Expense: Building=ZzgYia4qMZ23N8qhiCOV; Month=2026-10; source rows=3; source amount=2700.0000; apartment share rows=4; allocated=2700.0000; variance=0.0000
- Revenue: Building=ZzgYia4qMZ23N8qhiCOV; Month=2026-10; source rows=2; source amount=4500.0000; apartment share rows=4; allocated=4500.0000; variance=0.0000
- All source rows: expenses=12; revenues=4; monthly shares=113
- September/October links: expenses=12; revenues=4; share ledger entries=126
- Outside posting scope (retained in immutable evidence): expenses=0; revenues=0; monthly shares=15
- Allocation policy: use original MonthlyApartmentShares only; no historical reallocation or invented apartment attribution.
- Variance policy: preserve the registered +0.22 expense and +0.04 revenue rounding variances without balancing entries.
- Actor policy: exactly one active building-scoped Admin per building; SuperAdmin is never selected.
- Only September/October 2026 are posted. Other dated source rows remain preserved in Phase29 immutable evidence for a separate non-posting archive/reconciliation phase.
- PHASE44 APPLY AND RECONCILIATION COMPLETED.

## Guarantees
- Restricted to BuildingManagement_RestoreTest; writes are protected by one SQL transaction.
- Expenses and revenues are represented by building/month ledger transactions and linked to every original source row using immutable fingerprints.
- Apartment wallet entries are generated only from the exact historical MonthlyApartmentShares rows; no historical reallocation is inferred.
- Expense shares reduce wallet balances; revenue shares increase wallet balances.
- Registered legacy rounding variances are preserved and are never assigned to apartments or balanced with invented entries.
- Source rows and Phase29 evidence are never updated or deleted.
- September/October 2026 are the only permitted posting periods; all other source rows remain preserved as immutable evidence and are not posted.
- Deterministic IDs and idempotency keys make reruns safe.
