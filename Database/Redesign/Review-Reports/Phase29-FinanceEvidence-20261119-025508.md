# Phase 29 - Finance evidence backfill

- Run: 2026-11-19T02:55:08.3565315+02:00
- Mode: APPLY

## Results
- Database: BuildingManagement_RestoreTest
- DepositEditHistory: source=1, pending=1
- Deposits: source=26, pending=26
- Expenses: source=12, pending=12
- MonthlyApartmentShares: source=113, pending=113
- Revenues: source=4, pending=4
- WalletAdjustments: source=2, pending=2
- Total finance source rows: 158
- Pending evidence rows: 158
- ERROR: Exception calling "ExecuteNonQuery" with "0" argument(s): "There is already an object named '#FinanceSource' in the database."

## Safeguards
- All six legacy finance tables are preserved row-for-row with original source keys, JSON payloads, and SHA-256 fingerprints.
- Existing evidence is never overwritten; source drift or inconsistent provenance aborts the transaction.
- No financial periods, ledger transactions, ledger entries, payment postings, balances, or allocation lines are synthesized in this phase.
- No source rows are updated or deleted.
- Payloads and personal/financial row values are not written to this report.
