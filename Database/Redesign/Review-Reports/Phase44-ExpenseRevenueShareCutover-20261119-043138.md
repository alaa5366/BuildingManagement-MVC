# Phase 44 â€” Expense, revenue and historical apartment-share cutover

- Run: 2026-11-19T04:31:38.0510433+02:00
- Database: BuildingManagement_RestoreTest
- Mode: DRY RUN

## Results
- ERROR: Exception calling "Fill" with "1" argument(s): "Finance source rows outside the owner-approved September/October 2026 scope require separate treatment."

## Guarantees
- Restricted to BuildingManagement_RestoreTest; writes are protected by one SQL transaction.
- Expenses and revenues are represented by building/month ledger transactions and linked to every original source row using immutable fingerprints.
- Apartment wallet entries are generated only from the exact historical MonthlyApartmentShares rows; no historical reallocation is inferred.
- Expense shares reduce wallet balances; revenue shares increase wallet balances.
- Registered legacy rounding variances are preserved and are never assigned to apartments or balanced with invented entries.
- Source rows and Phase29 evidence are never updated or deleted.
- September/October 2026 are the only permitted posting periods; all other source rows remain preserved as immutable evidence and are not posted.
- Deterministic IDs and idempotency keys make reruns safe.
