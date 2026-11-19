# Phase 20 - Identity profile evidence backfill

- Run: 2026-11-19T02:36:45.8286318+02:00
- Mode: APPLY

## Results
- Database: BuildingManagement_RestoreTest
- Users source rows: 5
- Residents source rows: 79
- BuildingAdmins source rows: 2
- Users target evidence: 5; provenance: 5
- Residents target evidence: 79; provenance: 79
- BuildingAdmins target evidence: 2; provenance: 2
- Total source rows reconciled: 86
- RECONCILIATION PASS

## Identity and credential safeguards
- Users, Residents, and BuildingAdmins are preserved as unresolved legacy evidence; this phase does not create Persons, UserAccounts, credentials, or role assignments.
- User PIN, resident PIN hash/password hash, MFA secret ciphertext, and recovery-code material are excluded from target evidence.
- Residents.IsOwner and IsPrimary remain source facts; no legal ownership shares or effective-dated occupancy periods are fabricated.
- Building-admin links remain legacy references and do not grant new access.
- Source tables are never updated or deleted.
