# Phase 13 - BackupHistory Backfill

- Run: 2026-11-19T02:20:29.5513009+02:00
- Mode: APPLY
- Source rows retained: dbo.BackupHistory

## Results
- Database: BuildingManagement_RestoreTest
- Source rows: 2
- Already mapped: 0
- Pending: 2
- Target backup jobs: 2
- Provenance rows: 2
- RECONCILIATION PASS

## Preservation rules
- BackupJobs are created with JobType=0 and Status=0 (unclassified); legacy status/source are preserved in migration provenance.
- StartedAtUtc uses the recorded CreatedAt only; no completion time or duration is fabricated.
- DatabaseReference is explicitly marked legacy:unspecified because the source row does not identify a database name.
- No BackupArtifacts are fabricated: file existence, storage location, and SHA-256 content hash are not verifiable from BackupHistory metadata.
- CollectionsJson and ErrorMessage remain intact in the source; their complete contents are covered by the source fingerprint.
- Deterministic IDs and source mapping make reruns idempotent; source rows are never updated or deleted.
