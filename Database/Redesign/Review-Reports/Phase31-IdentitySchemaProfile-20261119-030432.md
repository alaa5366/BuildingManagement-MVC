# Phase 31 - Identity schema profile

- Run: 2026-11-19T03:04:32.7268782+02:00
- Mode: READ ONLY

## Results
## Users
- Uid: nvarchar, max_length=256, nullable=False
- Email: nvarchar, max_length=-1, nullable=False
- Name: nvarchar, max_length=-1, nullable=False
- Phone: nvarchar, max_length=-1, nullable=False
- Whatsapp: nvarchar, max_length=-1, nullable=False
- Pin: nvarchar, max_length=-1, nullable=False
- PhotoUrl: nvarchar, max_length=-1, nullable=False
- Role: nvarchar, max_length=-1, nullable=False
- IsDisabled: bit, max_length=1, nullable=False
- DisabledReason: nvarchar, max_length=-1, nullable=False
- IsActive: bit, max_length=1, nullable=False
- LastLoginAt: datetime2, max_length=6, nullable=True
- CreatedAt: datetime2, max_length=6, nullable=True
- CreatedBy: nvarchar, max_length=-1, nullable=True
- UpdatedAt: datetime2, max_length=6, nullable=True
- UpdatedBy: nvarchar, max_length=-1, nullable=True
- TwoFactorEnabled: bit, max_length=1, nullable=False
- TwoFactorSecretEncrypted: nvarchar, max_length=1024, nullable=True
- TwoFactorRecoveryCodesJson: nvarchar, max_length=8000, nullable=True
- TwoFactorLastUsedStep: bigint, max_length=8, nullable=True
## Residents
- Id: nvarchar, max_length=128, nullable=False
- Uid: nvarchar, max_length=256, nullable=False
- BuildingId: nvarchar, max_length=128, nullable=False
- ApartmentId: nvarchar, max_length=128, nullable=False
- Name: nvarchar, max_length=-1, nullable=False
- Phone: nvarchar, max_length=900, nullable=False
- Whatsapp: nvarchar, max_length=-1, nullable=False
- Email: nvarchar, max_length=-1, nullable=False
- PhotoUrl: nvarchar, max_length=-1, nullable=False
- PinHash: nvarchar, max_length=-1, nullable=False
- PasswordHash: nvarchar, max_length=-1, nullable=True
- IsActive: bit, max_length=1, nullable=False
- IsDisabled: bit, max_length=1, nullable=False
- DisabledReason: nvarchar, max_length=-1, nullable=False
- IsOwner: bit, max_length=1, nullable=False
- IsPrimary: bit, max_length=1, nullable=False
- LastLoginAt: datetime2, max_length=6, nullable=True
- CreatedAt: datetime2, max_length=6, nullable=False
- CreatedBy: nvarchar, max_length=-1, nullable=True
- UpdatedAt: datetime2, max_length=6, nullable=True
- UpdatedBy: nvarchar, max_length=-1, nullable=True
## BuildingAdmins
- BuildingId: nvarchar, max_length=128, nullable=False
- AdminUid: nvarchar, max_length=256, nullable=False
## Persons
- PersonId: uniqueidentifier, max_length=16, nullable=False
- LegalName: nvarchar, max_length=400, nullable=True
- DisplayName: nvarchar, max_length=320, nullable=False
- PrimaryEmail: nvarchar, max_length=508, nullable=True
- PrimaryPhone: nvarchar, max_length=64, nullable=True
- Whatsapp: nvarchar, max_length=64, nullable=True
- PhotoUrl: nvarchar, max_length=4096, nullable=True
- IsArchived: bit, max_length=1, nullable=False
- ArchivedAtUtc: datetime2, max_length=7, nullable=True
- CreatedAtUtc: datetime2, max_length=7, nullable=False
- UpdatedAtUtc: datetime2, max_length=7, nullable=True
- RowVersion: timestamp, max_length=8, nullable=False
## UserAccounts
- AccountId: uniqueidentifier, max_length=16, nullable=False
- PersonId: uniqueidentifier, max_length=16, nullable=False
- AccountStatus: tinyint, max_length=1, nullable=False
- NormalizedEmail: nvarchar, max_length=508, nullable=True
- ExternalProvider: nvarchar, max_length=80, nullable=True
- ExternalSubject: nvarchar, max_length=400, nullable=True
- SecurityStamp: nvarchar, max_length=256, nullable=False
- LastLoginAtUtc: datetime2, max_length=7, nullable=True
- CreatedAtUtc: datetime2, max_length=7, nullable=False
- RowVersion: timestamp, max_length=8, nullable=False
## UserCredentials
- CredentialId: uniqueidentifier, max_length=16, nullable=False
- AccountId: uniqueidentifier, max_length=16, nullable=False
- CredentialType: tinyint, max_length=1, nullable=False
- CredentialHash: nvarchar, max_length=1024, nullable=False
- HashAlgorithm: nvarchar, max_length=80, nullable=False
- CreatedAtUtc: datetime2, max_length=7, nullable=False
- ExpiresAtUtc: datetime2, max_length=7, nullable=True
- RevokedAtUtc: datetime2, max_length=7, nullable=True
- ReplacedByCredentialId: uniqueidentifier, max_length=16, nullable=True
- RowVersion: timestamp, max_length=8, nullable=False
## LegacyIdentityMap
- LegacyIdentityMapId: uniqueidentifier, max_length=16, nullable=False
- SourceSystem: nvarchar, max_length=160, nullable=False
- SourceTable: nvarchar, max_length=256, nullable=False
- SourceKey: nvarchar, max_length=512, nullable=False
- PersonId: uniqueidentifier, max_length=16, nullable=True
- AccountId: uniqueidentifier, max_length=16, nullable=True
- MappingStatus: tinyint, max_length=1, nullable=False
- MatchMethod: nvarchar, max_length=160, nullable=True
- EvidenceReference: nvarchar, max_length=1000, nullable=True
- ReviewReason: nvarchar, max_length=2000, nullable=True
- ReviewedAtUtc: datetime2, max_length=7, nullable=True
- ReviewedByAccountId: uniqueidentifier, max_length=16, nullable=True
- CreatedAtUtc: datetime2, max_length=7, nullable=False
- UpdatedAtUtc: datetime2, max_length=7, nullable=True
- RowVersion: timestamp, max_length=8, nullable=False
Database: BuildingManagement_RestoreTest
UsersCount: 5
ResidentsCount: 79
BuildingAdminLinks: 2
BuildingsCount: 3
PersonsCount: 1
UserAccountsCount: 1
UserCredentialsCount: 1
UserMapRows: 5
ResidentMapRows: 79
No row values, names, contact details, UID values, credential material, or hashes were exported.
READ-ONLY SCHEMA PROFILE COMPLETE.

## Safety
- Schema metadata and aggregate counts only.
- No source or target data changed.
- No names, contact details, raw identifiers, credential material, or hashes exported.
