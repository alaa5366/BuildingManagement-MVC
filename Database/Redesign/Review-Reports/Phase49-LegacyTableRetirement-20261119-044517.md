# Phase 49 â€” Legacy table retirement

- Run: 2026-11-19T04:45:17.2103530+02:00
- Mode: APPLY
- Scope: BuildingManagement_RestoreTest only
- Source database BuildingManagement is read-only and never modified.

## Table results

| Table | Source rows | Target rows | Migration map rows | Status |
|---|---:|---:|---:|---|

## Execution log

- ERROR: Exception calling "Fill" with "1" argument(s): "Incorrect syntax near the keyword 'RowCount'."

## Backup

- Backup file: Not created
- Tables dropped: 0

## Safety notes

- The script fails closed. Any missing target table, incomplete migration-map coverage, invalid target mapping, or foreign-key dependency prevents all drops.
- __EFMigrationsHistory is deliberately retained as Entity Framework deployment metadata.
- Target-only operational tables are never selected for deletion.
- A migration-map count is a conservative gate, not a cryptographic proof of payload equality; the Phase 47 fingerprints and table-specific migration reports remain required evidence.
- If any blocker is reported, resolve it before retrying. No source table or source row is ever changed.
