# Phase 30 - Identity and building scope audit

- Run: 2026-11-19T03:03:11.4858508+02:00
- Mode: READ ONLY

## Results
- Database: BuildingManagement_RestoreTest
- Users total: 5; SuperAdmins by role: 2; Admins by role: 3
- Users role 'admin': total=3; inactive/disabled=0
- Users role 'superadmin': total=2; inactive/disabled=0
- Residents total: 79
- Residents BuildingId=LmkUDOJcjlvjiGmluz6c: residents=60; apartments=60; inactive/disabled=2
- Residents BuildingId=zxhRd7cwZ07rZotXeQ17: residents=15; apartments=15; inactive/disabled=0
- Residents BuildingId=ZzgYia4qMZ23N8qhiCOV: residents=4; apartments=4; inactive/disabled=0
- Users/Residents exact UID overlap: 0
- BuildingAdmins BuildingId=zxhRd7cwZ07rZotXeQ17: links=1; distinct AdminUid=1; missing Users UID=0; role mismatch=0
- BuildingAdmins BuildingId=ZzgYia4qMZ23N8qhiCOV: links=1; distinct AdminUid=1; missing Users UID=0; role mismatch=0
- Target Persons=1; UserAccounts=1; verified Users mappings=0; verified Resident mappings=0; UserRoleAssignments=1
- READ-ONLY AUDIT COMPLETE - no data changed.

## User-provided classification to validate
- Residents are residents only, not administrators or super administrators.
- Expected resident distribution: 4, 15, and 60 across three buildings.
- Users expected: 2 SuperAdmins and 3 building Admins, one Admin per building.

## Safety and next decision
- This audit reads counts, role labels, building identifiers, and relationship integrity only; it does not export names, contact details, credentials, or hashes.
- No Persons, UserAccounts, credentials, role assignments, ownership, occupancy, or apartment access grants are created or changed.
- Resident login accounts will be created only after exact identity/account policy is confirmed; residents are not assigned administrative roles.
- Admin role assignments must be scoped to the exact BuildingId verified from BuildingAdmins; a missing third building-admin link must not be fabricated.
- Temporary password provisioning must use BCrypt, force change at first login, and be limited to verified accounts.
- Legacy source tables remain unchanged.
