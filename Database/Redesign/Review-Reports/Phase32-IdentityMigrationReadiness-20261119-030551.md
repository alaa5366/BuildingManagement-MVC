# Phase 32 - Identity migration readiness

- Run: 2026-11-19T03:05:51.9241273+02:00
- Mode: READ ONLY

## Results
- Database: BuildingManagement_RestoreTest
- UsersTotal: 5
- UsersWithEmail: 5
- UsersWithoutEmail: 0
- DuplicateUserEmailGroups: 0
- UserEmailsOver254: 0
- UsersWithoutName: 0
- InactiveUsers: 0
- ResidentsTotal: 79
- ResidentsWithEmail: 1
- ResidentsWithoutEmail: 78
- DuplicateResidentEmailGroups: 0
- ResidentEmailsOver254: 0
- ResidentsWithoutName: 0
- InactiveResidents: 2
- ResidentsWithoutUid: 0
- DuplicateResidentUidGroups: 0
- BuildingAdminLinks: 2
- DistinctLinkedAdminUids: 2
- DistinctLinkedBuildings: 2
- PersonsExisting: 1
- UserAccountsExisting: 1
- CredentialsExisting: 1
- UserMappingCandidates: 5
- ResidentMappingCandidates: 79
- VerifiedUserMappings: 0
- VerifiedResidentPersonMappings: 0
- Admin users without an explicit BuildingAdmins row: 1
- BuildingAdmins rows without an exact Admin source user: 0
- No raw UIDs, names, email addresses, phone numbers, credentials, or hashes were exported.
- READINESS PROFILE COMPLETE - NO DATA CHANGED.

## Decision
- No identity, account, credential, role, ownership, occupancy, or apartment-access rows are created by this audit.
- Users and Residents remain distinct source populations.
- Resident identity mapping is Person-only in the current mapping service; no resident login account is inferred.
- Admin building scope requires one exact BuildingAdmins source row per Admin; absent links are not fabricated.
- Email is not treated as a login identifier until uniqueness, validity, and application authentication behavior are verified.
