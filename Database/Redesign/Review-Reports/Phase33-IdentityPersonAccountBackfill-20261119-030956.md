# Phase 33 - Identity Person and Account backfill

- Run: 2026-11-19T03:09:56.3485817+02:00
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
- ERROR: Exception calling "ExecuteNonQuery" with "0" argument(s): "The UPDATE statement conflicted with the CHECK constraint "CK_LegacyIdentityMap_ResolvedReview". The conflict occurred in database "BuildingManagement_RestoreTest", table "dbo.LegacyIdentityMap".
Resident identity map changed during backfill.
The statement has been terminated."

## Safeguards
- Residents are mapped as separate Persons only, based on the explicit user confirmation; no Resident login accounts or credentials are created.
- Five administrative Users become distinct Persons and pending UserAccounts; AccountStatus=0 prevents sign-in until later controlled activation.
- No credentials, temporary passwords, MFA secrets, role assignments, permissions, ownership, occupancy, or apartment access grants are created.
- The two inactive resident records remain identity profiles only; no login account is activated for any resident.
- The third Admin remains without a building role until an exact source BuildingAdmins link is available.
- All writes are transactional and idempotency is guarded by pending exact-key mapping rows; legacy source tables are read-only.
