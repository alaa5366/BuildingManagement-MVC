# Phase 29 - Finance evidence backfill

- Run: 2026-11-19T02:54:09.4576201+02:00
- Mode: APPLY

## Results
- ERROR: Exception calling "Fill" with "1" argument(s): "Ambiguous column name 'SourceTable'.
Ambiguous column name 'SourceTable'."

## Safeguards
- All six legacy finance tables are preserved row-for-row with original source keys, JSON payloads, and SHA-256 fingerprints.
- Existing evidence is never overwritten; source drift or inconsistent provenance aborts the transaction.
- No financial periods, ledger transactions, ledger entries, payment postings, balances, or allocation lines are synthesized in this phase.
- No source rows are updated or deleted.
- Payloads and personal/financial row values are not written to this report.
