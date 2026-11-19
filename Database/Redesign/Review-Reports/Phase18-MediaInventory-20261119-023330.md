# Phase 18 - DVR and camera inventory backfill

- Run: 2026-11-19T02:33:30.1140017+02:00
- Mode: APPLY

## Results
- Database: BuildingManagement_RestoreTest
- DVR source rows: 1 (pending 1)
- Camera source rows: 3 (pending 3)
- DVR evidence rows: 1
- Camera evidence rows: 3
- Provenance rows: 4
- RECONCILIATION PASS

## Credential protection
- DVR encrypted passwords and usernames are not copied into the new inventory; configure a supported secrets vault and rotate credentials before enabling new media access.
- Camera RTSP/HLS URLs are not copied because connection strings can embed credentials.
- Camera and DVR topology metadata is retained as JSON with SHA-256 fingerprints.
- No MediaCredentialReferences or MediaAccessSessions are fabricated; no source row is changed or deleted.
