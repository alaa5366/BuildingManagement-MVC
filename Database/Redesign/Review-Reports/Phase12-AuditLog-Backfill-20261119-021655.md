# Phase 12 - AuditLog Backfill

- Run: 2026-11-19T02:16:56.0348221+02:00
- Mode: APPLY
- Source rows retained: dbo.AuditLog

## Results
- Database: BuildingManagement_RestoreTest
- Source rows: 1053
- Already mapped: 0
- Pending: 1053
- Actor over target limit: 0
- Label over target limit: 0
- Target events: 1053
- Provenance rows: 1053
- RECONCILIATION PASS

## Preservation rules
- Actor is copied as a reference string only; no account identity is inferred.
- Event outcome is 0 (unknown) because legacy outcome semantics are not defined.
- Legacy role, severity, month and apartment/building context are retained in SafeDetailsJson.
- Legacy Details remains intact in the source table; it is not copied into a field labelled safe without content classification.
- EventHash is the SHA-256 fingerprint of selected immutable source fields; PreviousHash remains NULL because no verified chain order/hash contract exists.
- Oversized Actor or Label blocks the run; no truncation is performed.
- Source rows are never updated or deleted.
