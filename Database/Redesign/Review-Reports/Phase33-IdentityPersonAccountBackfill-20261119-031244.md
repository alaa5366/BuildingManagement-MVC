# Phase 33 - Identity Person and Account backfill

- Run: 2026-11-19T03:12:44.1925230+02:00
- Mode: APPLY

## Results
- InvalidUsers: 0
- InvalidResidents: 0
- UsersMissingEmail: 0
- UsersMissingName: 0
- ResidentsMissingName: 0
- NonPendingUserMappings: 0
- NonPendingResidentMappings: 0
- UserMaps: 5
- ResidentMaps: 79
- UnmatchedOrAmbiguousResidentMapKeys: 0
- UnmatchedOrAmbiguousUserMapKeys: 0
- APPLY TRANSACTION COMMITTED
- Persons total: 85
- UserAccounts total: 6
- Verified Users mappings: 5
- Separate Resident person mappings: 79
- Credentials created: 1
- Role assignments created: 1
- IDENTITY PERSON/ACCOUNT BACKFILL COMPLETE - no credentials or roles were created.

## Safeguards
- Residents are mapped as separate Persons only, based on the explicit user confirmation; no Resident login accounts or credentials are created.
- Five administrative Users become distinct Persons and pending UserAccounts; AccountStatus=0 prevents sign-in until later controlled activation.
- No credentials, temporary passwords, MFA secrets, role assignments, permissions, ownership, occupancy, or apartment access grants are created.
- The two inactive resident records remain identity profiles only; no login account is activated for any resident.
- The third Admin remains without a building role until an exact source BuildingAdmins link is available.
- All writes are transactional and idempotency is guarded by pending exact-key mapping rows; legacy source tables are read-only.
