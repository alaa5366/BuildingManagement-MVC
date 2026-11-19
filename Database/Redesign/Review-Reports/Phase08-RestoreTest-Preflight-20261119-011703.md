# Phase08 RestoreTest Preflight and Backup

- Timestamp: 2026-11-19 01:17:04
- Server: (localdb)\MSSQLLocalDB
- Database: BuildingManagement_RestoreTest
- State: ONLINE
- User access: MULTI_USER
- Recovery model: SIMPLE

## Pre-existing local working-tree changes (preserved; not included in this report commit)
- ?? Publish-Phase08-Local-Changes.ps1
- Backup: C:\Users\alaa5\AppData\Local\BuildingManagement\Backups\BuildingManagement_RestoreTest-Phase08-20261119-011703.bak
- Backup type: COPY_ONLY
- RESTORE VERIFYONLY WITH CHECKSUM: PASSED
- EF migration application: NOT RUN
- Business data changes: NONE

## Table inventory
- __EFMigrationsHistory: 6
- AccountPermissionOverrides: 0
- ApartmentAccessGrants: 0
- ApartmentOccupancies: 0
- ApartmentOwnerships: 0
- Apartments: 79
- AuditLog: 1053
- BackupHistory: 2
- BuildingAdmins: 2
- Buildings: 3
- BuildingSettings: 0
- Cameras: 3
- DepositEditHistory: 1
- Deposits: 26
- Dvrs: 1
- Expenses: 12
- FinancialCategories: 38
- Floors: 16
- GlobalSettings: 1
- LegacyIdentityMap: 84
- LoginThrottle: 0
- MaintenanceRecords: 1
- MonthlyApartmentShares: 113
- Notifications: 68
- Persons: 1
- PollOptions: 2
- Polls: 1
- PollVotes: 1
- Presence: 10
- QrTokens: 9
- QrUsages: 5
- Residents: 79
- Revenues: 4
- RoleDefinitions: 1
- RolePermissions: 0
- ScheduledBackupSettings: 1
- SyncPendingChanges: 0
- UserAccounts: 1
- UserCredentials: 1
- UserPermissions: 81
- UserRecoveryCodes: 0
- UserRoleAssignments: 1
- Users: 5
- UserTwoFactorSettings: 0
- WalletAdjustments: 2

## EF migration history
- 20261116120818_InitialCreate (EF 8.0.10)
- 20261117012657_Phase04_StandardizeKeyColumnTypes (EF 8.0.10)
- 20261117050000_Phase05_RemoveRedundantResidentIndexes (EF 8.0.10)
- 20261117130000_AddUserTwoFactor (EF 8.0.10)
- 20261118182110_Phase06_IdentityAuthorizationFoundation (EF 8.0.10)
- 20261118184850_Phase07_OwnershipRecordProvenance (EF 8.0.10)

## Phase08 finance target tables
- None found.

## Gate status
- Database identity and ONLINE state verified.
- COPY_ONLY backup completed and verified by SQL Server.
- This report is preflight evidence only; it does not authorize migration application.
