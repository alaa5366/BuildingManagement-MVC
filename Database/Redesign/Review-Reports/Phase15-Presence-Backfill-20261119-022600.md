# Phase 15 - Presence Backfill

- Run: 2026-11-19T02:26:00.7275078+02:00
- Mode: APPLY
- Source rows retained: dbo.Presence

## Results
- Database: BuildingManagement_RestoreTest
- Source rows: 10
- Already mapped: 0
- Pending: 10
- Target sessions: 10
- Provenance rows: 10
- Synthetic heartbeat rows: 0
- RECONCILIATION PASS

## Preservation rules
- One legacy last-seen row maps to one PresenceSessions row with a deterministic ID.
- StartedAtUtc remains NULL because the legacy source has no session-start timestamp.
- AccountId and DeviceReference remain NULL; legacy UID is retained only as a source reference.
- ExpiresAtUtc is LastSeen + the application two-minute online threshold; no historical heartbeat rows are fabricated.
- The source Presence table is never updated or deleted.
