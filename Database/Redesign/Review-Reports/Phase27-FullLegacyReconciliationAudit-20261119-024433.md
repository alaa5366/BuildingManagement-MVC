# Phase 27 - Full legacy reconciliation audit

- Run: 2026-11-19T02:44:34.2650827+02:00
- Mode: READ ONLY

## Results
- Buildings: rows=3; provenance=0
- Floors: rows=16; provenance=0
- Apartments: rows=79; provenance=0
- BuildingSettings: rows=0; provenance=0
- FinancialCategories: rows=38; provenance=0
- Users: rows=5; provenance=5
- Residents: rows=79; provenance=79
- UserPermissions: rows=81; provenance=81
- BuildingAdmins: rows=2; provenance=2
- Persons: rows=1; provenance=0
- UserAccounts: rows=1; provenance=0
- UserCredentials: rows=1; provenance=0
- RoleDefinitions: rows=1; provenance=0
- RolePermissions: rows=0; provenance=0
- UserRoleAssignments: rows=1; provenance=0
- AccountPermissionOverrides: rows=0; provenance=0
- UserTwoFactorSettings: rows=0; provenance=0
- UserRecoveryCodes: rows=0; provenance=0
- LegacyIdentityMap: rows=84; provenance=0
- ApartmentOwnerships: rows=0; provenance=0
- ApartmentOccupancies: rows=0; provenance=0
- ApartmentAccessGrants: rows=0; provenance=0
- Deposits: rows=26; provenance=0
- DepositEditHistory: rows=1; provenance=0
- Expenses: rows=12; provenance=0
- Revenues: rows=4; provenance=0
- MonthlyApartmentShares: rows=113; provenance=0
- WalletAdjustments: rows=2; provenance=0
- Polls: rows=1; provenance=1
- PollOptions: rows=2; provenance=2
- PollVotes: rows=1; provenance=1
- MaintenanceRecords: rows=1; provenance=1
- Notifications: rows=68; provenance=68
- Presence: rows=10; provenance=10
- Dvrs: rows=1; provenance=1
- Cameras: rows=3; provenance=3
- QrTokens: rows=9; provenance=9
- QrUsages: rows=5; provenance=5
- AuditLog: rows=1053; provenance=1053
- BackupHistory: rows=2; provenance=2
- ScheduledBackupSettings: rows=1; provenance=1
- GlobalSettings: rows=1; provenance=0
- SyncPendingChanges: rows=0; provenance=0
- LoginThrottle: rows=0; provenance=0
- TOTAL listed table rows=1708; provenance rows=1324
- Counts are structural reconciliation indicators only; provenance rows do not prove semantic identity or authorization correctness.
- No data values were selected. No source or target rows were modified.

## Interpretation
- A row count and provenance count are not necessarily expected to match for canonical reference tables, aggregated evidence, or intentionally excluded sensitive data.
- Review zero-provenance rows and count differences against the approved mapping policy before any new backfill.
- This phase performs no migration and changes no source or target data.
