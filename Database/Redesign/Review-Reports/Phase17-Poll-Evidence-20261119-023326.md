# Phase 17 - Poll evidence backfill

- Run: 2026-11-19T02:33:26.1903664+02:00
- Mode: APPLY

## Results
- Database: BuildingManagement_RestoreTest
- Source polls: 1
- Source options: 2
- Source apartment votes: 1
- Pending poll lifecycle records: 0
- Target lifecycle events: 1
- Poll provenance: 1
- Option provenance: 2
- Apartment-vote provenance: 1
- RECONCILIATION PASS

## Preservation rules
- Poll creation is represented as a lifecycle event with EventType 0 explicitly classified as unclassified; metadata preserves question, description, deadline, and closed state.
- PollOptions and PollVotes are retained as fingerprinted legacy evidence in LegacyDataMigrationMap; source rows are not changed.
- Apartment-level vote attribution remains apartment-level. No person identity, eligibility, delegation, ballot token, or anonymous ballot is inferred.
- No individual events are synthesized from aggregate vote records; legacy source tables remain unchanged.
