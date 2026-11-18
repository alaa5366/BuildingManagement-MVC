/*
Gate A — Additive identity and relationship foundation
Target: SQL Server / BuildingManagement_RestoreTest for first rehearsal only.

SAFETY:
- This script is intentionally additive. It does not alter, rename, or drop legacy tables/columns.
- It does not backfill Persons, accounts, roles, ownerships, or occupancies.
- It must be reviewed and rehearsed against a fresh disposable restore before execution.
- Do NOT run against production as part of this commit.
- Application-level encryption is required for UserTwoFactorSettings.SecretCiphertext.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.Persons', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Persons
    (
        PersonId uniqueidentifier NOT NULL CONSTRAINT PK_Persons PRIMARY KEY,
        LegalName nvarchar(200) NULL,
        DisplayName nvarchar(160) NOT NULL,
        PrimaryEmail nvarchar(254) NULL,
        PrimaryPhone nvarchar(32) NULL,
        Whatsapp nvarchar(32) NULL,
        PhotoUrl nvarchar(2048) NULL,
        IsArchived bit NOT NULL CONSTRAINT DF_Persons_IsArchived DEFAULT (0),
        ArchivedAtUtc datetime2(3) NULL,
        CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_Persons_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc datetime2(3) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT CK_Persons_ArchiveState CHECK
            ((IsArchived = 0 AND ArchivedAtUtc IS NULL) OR (IsArchived = 1 AND ArchivedAtUtc IS NOT NULL))
    );
    CREATE INDEX IX_Persons_PrimaryEmail ON dbo.Persons(PrimaryEmail) WHERE PrimaryEmail IS NOT NULL;
    CREATE INDEX IX_Persons_PrimaryPhone ON dbo.Persons(PrimaryPhone) WHERE PrimaryPhone IS NOT NULL;
END;

IF OBJECT_ID(N'dbo.UserAccounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserAccounts
    (
        AccountId uniqueidentifier NOT NULL CONSTRAINT PK_UserAccounts PRIMARY KEY,
        PersonId uniqueidentifier NOT NULL,
        AccountStatus tinyint NOT NULL,
        NormalizedEmail nvarchar(254) NULL,
        ExternalProvider nvarchar(40) NULL,
        ExternalSubject nvarchar(200) NULL,
        SecurityStamp nvarchar(128) NOT NULL,
        LastLoginAtUtc datetime2(3) NULL,
        CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_UserAccounts_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        RowVersion rowversion NOT NULL,
        CONSTRAINT FK_UserAccounts_Persons FOREIGN KEY(PersonId)
            REFERENCES dbo.Persons(PersonId) ON DELETE NO ACTION,
        CONSTRAINT UQ_UserAccounts_PersonId UNIQUE(PersonId),
        CONSTRAINT CK_UserAccounts_AccountStatus CHECK (AccountStatus IN (0,1,2,3))
    );
    CREATE UNIQUE INDEX UX_UserAccounts_ExternalLogin
        ON dbo.UserAccounts(ExternalProvider, ExternalSubject)
        WHERE ExternalProvider IS NOT NULL AND ExternalSubject IS NOT NULL;
    CREATE INDEX IX_UserAccounts_NormalizedEmail
        ON dbo.UserAccounts(NormalizedEmail) WHERE NormalizedEmail IS NOT NULL;
END;

IF OBJECT_ID(N'dbo.UserCredentials', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserCredentials
    (
        CredentialId uniqueidentifier NOT NULL CONSTRAINT PK_UserCredentials PRIMARY KEY,
        AccountId uniqueidentifier NOT NULL,
        CredentialType tinyint NOT NULL,
        CredentialHash nvarchar(512) NOT NULL,
        HashAlgorithm nvarchar(40) NOT NULL,
        CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_UserCredentials_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        ExpiresAtUtc datetime2(3) NULL,
        RevokedAtUtc datetime2(3) NULL,
        ReplacedByCredentialId uniqueidentifier NULL,
        CONSTRAINT FK_UserCredentials_UserAccounts FOREIGN KEY(AccountId)
            REFERENCES dbo.UserAccounts(AccountId) ON DELETE NO ACTION,
        CONSTRAINT FK_UserCredentials_ReplacedBy FOREIGN KEY(ReplacedByCredentialId)
            REFERENCES dbo.UserCredentials(CredentialId) ON DELETE NO ACTION,
        CONSTRAINT CK_UserCredentials_Expiry CHECK (ExpiresAtUtc IS NULL OR ExpiresAtUtc > CreatedAtUtc)
    );
    CREATE INDEX IX_UserCredentials_Account_Type
        ON dbo.UserCredentials(AccountId, CredentialType, RevokedAtUtc);
END;

IF OBJECT_ID(N'dbo.UserTwoFactorSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserTwoFactorSettings
    (
        AccountId uniqueidentifier NOT NULL CONSTRAINT PK_UserTwoFactorSettings PRIMARY KEY,
        IsEnabled bit NOT NULL CONSTRAINT DF_UserTwoFactorSettings_IsEnabled DEFAULT (0),
        SecretCiphertext varbinary(max) NULL,
        KeyVersion nvarchar(64) NULL,
        LastAcceptedTimeStep bigint NULL,
        EnrolledAtUtc datetime2(3) NULL,
        LastVerifiedAtUtc datetime2(3) NULL,
        UpdatedAtUtc datetime2(3) NULL,
        CONSTRAINT FK_UserTwoFactorSettings_UserAccounts FOREIGN KEY(AccountId)
            REFERENCES dbo.UserAccounts(AccountId) ON DELETE NO ACTION,
        CONSTRAINT CK_UserTwoFactorSettings_EnabledSecret CHECK
            (IsEnabled = 0 OR (SecretCiphertext IS NOT NULL AND KeyVersion IS NOT NULL))
    );
END;

IF OBJECT_ID(N'dbo.UserRecoveryCodes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRecoveryCodes
    (
        RecoveryCodeId uniqueidentifier NOT NULL CONSTRAINT PK_UserRecoveryCodes PRIMARY KEY,
        AccountId uniqueidentifier NOT NULL,
        CodeHash varbinary(64) NOT NULL,
        CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_UserRecoveryCodes_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UsedAtUtc datetime2(3) NULL,
        CONSTRAINT FK_UserRecoveryCodes_UserAccounts FOREIGN KEY(AccountId)
            REFERENCES dbo.UserAccounts(AccountId) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX UX_UserRecoveryCodes_CodeHash ON dbo.UserRecoveryCodes(CodeHash);
    CREATE INDEX IX_UserRecoveryCodes_Account_Unused
        ON dbo.UserRecoveryCodes(AccountId, UsedAtUtc);
END;

IF OBJECT_ID(N'dbo.RoleDefinitions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoleDefinitions
    (
        RoleKey nvarchar(64) NOT NULL CONSTRAINT PK_RoleDefinitions PRIMARY KEY,
        DisplayName nvarchar(120) NOT NULL,
        IsSystemRole bit NOT NULL CONSTRAINT DF_RoleDefinitions_IsSystemRole DEFAULT (0),
        IsActive bit NOT NULL CONSTRAINT DF_RoleDefinitions_IsActive DEFAULT (1)
    );
END;

IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermissions
    (
        RoleKey nvarchar(64) NOT NULL,
        PermissionKey nvarchar(100) NOT NULL,
        Effect tinyint NOT NULL,
        CONSTRAINT PK_RolePermissions PRIMARY KEY(RoleKey, PermissionKey),
        CONSTRAINT FK_RolePermissions_RoleDefinitions FOREIGN KEY(RoleKey)
            REFERENCES dbo.RoleDefinitions(RoleKey) ON DELETE NO ACTION,
        CONSTRAINT CK_RolePermissions_Effect CHECK (Effect IN (0,1))
    );
END;

IF OBJECT_ID(N'dbo.UserRoleAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoleAssignments
    (
        RoleAssignmentId uniqueidentifier NOT NULL CONSTRAINT PK_UserRoleAssignments PRIMARY KEY,
        AccountId uniqueidentifier NOT NULL,
        RoleKey nvarchar(64) NOT NULL,
        ScopeType tinyint NOT NULL, -- 0 System, 1 Building, 2 Apartment, 3 Personal
        ScopeBuildingId nvarchar(64) NULL,
        ScopeApartmentId nvarchar(64) NULL,
        ValidFromUtc datetime2(3) NOT NULL,
        ValidToUtc datetime2(3) NULL,
        GrantedByAccountId uniqueidentifier NULL,
        RevokedAtUtc datetime2(3) NULL,
        RevokedByAccountId uniqueidentifier NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT FK_UserRoleAssignments_Accounts FOREIGN KEY(AccountId)
            REFERENCES dbo.UserAccounts(AccountId) ON DELETE NO ACTION,
        CONSTRAINT FK_UserRoleAssignments_Roles FOREIGN KEY(RoleKey)
            REFERENCES dbo.RoleDefinitions(RoleKey) ON DELETE NO ACTION,
        CONSTRAINT FK_UserRoleAssignments_Buildings FOREIGN KEY(ScopeBuildingId)
            REFERENCES dbo.Buildings(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_UserRoleAssignments_Apartments FOREIGN KEY(ScopeBuildingId, ScopeApartmentId)
            REFERENCES dbo.Apartments(BuildingId, Id) ON DELETE NO ACTION,
        CONSTRAINT FK_UserRoleAssignments_GrantedBy FOREIGN KEY(GrantedByAccountId)
            REFERENCES dbo.UserAccounts(AccountId) ON DELETE NO ACTION,
        CONSTRAINT FK_UserRoleAssignments_RevokedBy FOREIGN KEY(RevokedByAccountId)
            REFERENCES dbo.UserAccounts(AccountId) ON DELETE NO ACTION,
        CONSTRAINT CK_UserRoleAssignments_Scope CHECK
        (
            (ScopeType IN (0,3) AND ScopeBuildingId IS NULL AND ScopeApartmentId IS NULL)
            OR (ScopeType = 1 AND ScopeBuildingId IS NOT NULL AND ScopeApartmentId IS NULL)
            OR (ScopeType = 2 AND ScopeBuildingId IS NOT NULL AND ScopeApartmentId IS NOT NULL)
        ),
        CONSTRAINT CK_UserRoleAssignments_Validity CHECK
            (ValidToUtc IS NULL OR ValidToUtc > ValidFromUtc),
        CONSTRAINT CK_UserRoleAssignments_Revocation CHECK
            ((RevokedAtUtc IS NULL AND RevokedByAccountId IS NULL)
             OR (RevokedAtUtc IS NOT NULL))
    );
    CREATE INDEX IX_UserRoleAssignments_Account_Scope
        ON dbo.UserRoleAssignments(AccountId, ScopeType, ScopeBuildingId, ScopeApartmentId);
    CREATE INDEX IX_UserRoleAssignments_Role_Scope
        ON dbo.UserRoleAssignments(RoleKey, ScopeType, ScopeBuildingId, ScopeApartmentId);
END;

IF OBJECT_ID(N'dbo.ApartmentOwnerships', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApartmentOwnerships
    (
        OwnershipId uniqueidentifier NOT NULL CONSTRAINT PK_ApartmentOwnerships PRIMARY KEY,
        BuildingId nvarchar(64) NOT NULL,
        ApartmentId nvarchar(64) NOT NULL,
        PersonId uniqueidentifier NOT NULL,
        OwnershipSharePercent decimal(9,6) NOT NULL,
        ValidFromUtc datetime2(3) NOT NULL,
        ValidToUtc datetime2(3) NULL,
        RecordedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_ApartmentOwnerships_RecordedAtUtc DEFAULT (SYSUTCDATETIME()),
        RecordedByAccountId uniqueidentifier NULL,
        EvidenceReference nvarchar(500) NULL,
        Notes nvarchar(1000) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT FK_ApartmentOwnerships_Apartments FOREIGN KEY(BuildingId, ApartmentId)
            REFERENCES dbo.Apartments(BuildingId, Id) ON DELETE NO ACTION,
        CONSTRAINT FK_ApartmentOwnerships_Persons FOREIGN KEY(PersonId)
            REFERENCES dbo.Persons(PersonId) ON DELETE NO ACTION,
        CONSTRAINT FK_ApartmentOwnerships_RecordedBy FOREIGN KEY(RecordedByAccountId)
            REFERENCES dbo.UserAccounts(AccountId) ON DELETE NO ACTION,
        CONSTRAINT CK_ApartmentOwnerships_Share CHECK
            (OwnershipSharePercent > 0 AND OwnershipSharePercent <= 100),
        CONSTRAINT CK_ApartmentOwnerships_Validity CHECK
            (ValidToUtc IS NULL OR ValidToUtc > ValidFromUtc)
    );
    CREATE INDEX IX_ApartmentOwnerships_Apartment_Period
        ON dbo.ApartmentOwnerships(BuildingId, ApartmentId, ValidFromUtc, ValidToUtc);
    CREATE INDEX IX_ApartmentOwnerships_Person_Period
        ON dbo.ApartmentOwnerships(PersonId, ValidFromUtc, ValidToUtc);
END;

IF OBJECT_ID(N'dbo.ApartmentOccupancies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApartmentOccupancies
    (
        OccupancyId uniqueidentifier NOT NULL CONSTRAINT PK_ApartmentOccupancies PRIMARY KEY,
        BuildingId nvarchar(64) NOT NULL,
        ApartmentId nvarchar(64) NOT NULL,
        PersonId uniqueidentifier NOT NULL,
        OccupancyType tinyint NOT NULL, -- 0 owner-occupant, 1 tenant, 2 family, 3 other
        IsPrimary bit NOT NULL CONSTRAINT DF_ApartmentOccupancies_IsPrimary DEFAULT (0),
        ValidFromUtc datetime2(3) NOT NULL,
        ValidToUtc datetime2(3) NULL,
        RecordedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_ApartmentOccupancies_RecordedAtUtc DEFAULT (SYSUTCDATETIME()),
        RecordedByAccountId uniqueidentifier NULL,
        Notes nvarchar(1000) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT FK_ApartmentOccupancies_Apartments FOREIGN KEY(BuildingId, ApartmentId)
            REFERENCES dbo.Apartments(BuildingId, Id) ON DELETE NO ACTION,
        CONSTRAINT FK_ApartmentOccupancies_Persons FOREIGN KEY(PersonId)
            REFERENCES dbo.Persons(PersonId) ON DELETE NO ACTION,
        CONSTRAINT FK_ApartmentOccupancies_RecordedBy FOREIGN KEY(RecordedByAccountId)
            REFERENCES dbo.UserAccounts(AccountId) ON DELETE NO ACTION,
        CONSTRAINT CK_ApartmentOccupancies_Type CHECK (OccupancyType IN (0,1,2,3)),
        CONSTRAINT CK_ApartmentOccupancies_Validity CHECK
            (ValidToUtc IS NULL OR ValidToUtc > ValidFromUtc)
    );
    CREATE INDEX IX_ApartmentOccupancies_Apartment_Period
        ON dbo.ApartmentOccupancies(BuildingId, ApartmentId, ValidFromUtc, ValidToUtc);
    CREATE INDEX IX_ApartmentOccupancies_Person_Period
        ON dbo.ApartmentOccupancies(PersonId, ValidFromUtc, ValidToUtc);
    CREATE UNIQUE INDEX UX_ApartmentOccupancies_OneOpenPrimary
        ON dbo.ApartmentOccupancies(BuildingId, ApartmentId)
        WHERE IsPrimary = 1 AND ValidToUtc IS NULL;
END;

IF OBJECT_ID(N'dbo.ApartmentAccessGrants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApartmentAccessGrants
    (
        GrantId uniqueidentifier NOT NULL CONSTRAINT PK_ApartmentAccessGrants PRIMARY KEY,
        BuildingId nvarchar(64) NOT NULL,
        ApartmentId nvarchar(64) NOT NULL,
        PersonId uniqueidentifier NOT NULL,
        GrantType tinyint NOT NULL,
        PermissionSetKey nvarchar(80) NOT NULL,
        ValidFromUtc datetime2(3) NOT NULL,
        ValidToUtc datetime2(3) NULL,
        GrantedByAccountId uniqueidentifier NOT NULL,
        RevokedAtUtc datetime2(3) NULL,
        RevokedByAccountId uniqueidentifier NULL,
        Reason nvarchar(500) NULL,
        CONSTRAINT FK_ApartmentAccessGrants_Apartments FOREIGN KEY(BuildingId, ApartmentId)
            REFERENCES dbo.Apartments(BuildingId, Id) ON DELETE NO ACTION,
        CONSTRAINT FK_ApartmentAccessGrants_Persons FOREIGN KEY(PersonId)
            REFERENCES dbo.Persons(PersonId) ON DELETE NO ACTION,
        CONSTRAINT FK_ApartmentAccessGrants_GrantedBy FOREIGN KEY(GrantedByAccountId)
            REFERENCES dbo.UserAccounts(AccountId) ON DELETE NO ACTION,
        CONSTRAINT FK_ApartmentAccessGrants_RevokedBy FOREIGN KEY(RevokedByAccountId)
            REFERENCES dbo.UserAccounts(AccountId) ON DELETE NO ACTION,
        CONSTRAINT CK_ApartmentAccessGrants_Validity CHECK
            (ValidToUtc IS NULL OR ValidToUtc > ValidFromUtc)
    );
    CREATE INDEX IX_ApartmentAccessGrants_Apartment_Period
        ON dbo.ApartmentAccessGrants(BuildingId, ApartmentId, ValidFromUtc, ValidToUtc);
    CREATE INDEX IX_ApartmentAccessGrants_Person_Period
        ON dbo.ApartmentAccessGrants(PersonId, ValidFromUtc, ValidToUtc);
END;


-- Direct per-account permissions are distinct from role permissions.
-- Preserve legacy UserPermissions semantics here; never promote them to global role grants.
IF OBJECT_ID(N'dbo.AccountPermissionOverrides', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AccountPermissionOverrides
    (
        OverrideId uniqueidentifier NOT NULL CONSTRAINT PK_AccountPermissionOverrides PRIMARY KEY,
        AccountId uniqueidentifier NOT NULL,
        PermissionKey nvarchar(100) NOT NULL,
        Effect tinyint NOT NULL, -- 0 Deny, 1 Allow; deny takes precedence
        ScopeType tinyint NOT NULL, -- 0 System, 1 Building, 2 Apartment, 3 Personal
        ScopeBuildingId nvarchar(64) NULL,
        ScopeApartmentId nvarchar(64) NULL,
        ValidFromUtc datetime2(3) NOT NULL,
        ValidToUtc datetime2(3) NULL,
        GrantedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_AccountPermissionOverrides_GrantedAtUtc DEFAULT (SYSUTCDATETIME()),
        GrantedByAccountId uniqueidentifier NULL,
        RevokedAtUtc datetime2(3) NULL,
        RevokedByAccountId uniqueidentifier NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT FK_AccountPermissionOverrides_Accounts FOREIGN KEY(AccountId)
            REFERENCES dbo.UserAccounts(AccountId) ON DELETE NO ACTION,
        CONSTRAINT FK_AccountPermissionOverrides_Buildings FOREIGN KEY(ScopeBuildingId)
            REFERENCES dbo.Buildings(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_AccountPermissionOverrides_Apartments FOREIGN KEY(ScopeBuildingId, ScopeApartmentId)
            REFERENCES dbo.Apartments(BuildingId, Id) ON DELETE NO ACTION,
        CONSTRAINT FK_AccountPermissionOverrides_GrantedBy FOREIGN KEY(GrantedByAccountId)
            REFERENCES dbo.UserAccounts(AccountId) ON DELETE NO ACTION,
        CONSTRAINT FK_AccountPermissionOverrides_RevokedBy FOREIGN KEY(RevokedByAccountId)
            REFERENCES dbo.UserAccounts(AccountId) ON DELETE NO ACTION,
        CONSTRAINT CK_AccountPermissionOverrides_Effect CHECK (Effect IN (0,1)),
        CONSTRAINT CK_AccountPermissionOverrides_Scope CHECK
        (
            (ScopeType IN (0,3) AND ScopeBuildingId IS NULL AND ScopeApartmentId IS NULL)
            OR (ScopeType = 1 AND ScopeBuildingId IS NOT NULL AND ScopeApartmentId IS NULL)
            OR (ScopeType = 2 AND ScopeBuildingId IS NOT NULL AND ScopeApartmentId IS NOT NULL)
        ),
        CONSTRAINT CK_AccountPermissionOverrides_Validity CHECK
            (ValidToUtc IS NULL OR ValidToUtc > ValidFromUtc),
        CONSTRAINT CK_AccountPermissionOverrides_Revocation CHECK
            ((RevokedAtUtc IS NULL AND RevokedByAccountId IS NULL)
             OR RevokedAtUtc IS NOT NULL)
    );
    CREATE INDEX IX_AccountPermissionOverrides_Account_Permission_Scope
        ON dbo.AccountPermissionOverrides
            (AccountId, PermissionKey, ScopeType, ScopeBuildingId, ScopeApartmentId, ValidFromUtc);
    CREATE INDEX IX_AccountPermissionOverrides_Permission_Scope
        ON dbo.AccountPermissionOverrides
            (PermissionKey, ScopeType, ScopeBuildingId, ScopeApartmentId, ValidFromUtc);
END;


-- Durable source-to-target identity provenance.
-- Residents rows may map many-to-one to Person; Users rows map to Person + Account.
-- Unresolved rows remain present with NULL target IDs and are never auto-merged.
IF OBJECT_ID(N'dbo.LegacyIdentityMap', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LegacyIdentityMap
    (
        LegacyIdentityMapId uniqueidentifier NOT NULL
            CONSTRAINT PK_LegacyIdentityMap PRIMARY KEY,
        SourceSystem nvarchar(80) NOT NULL,
        SourceTable nvarchar(128) NOT NULL,
        SourceKey nvarchar(256) NOT NULL,
        PersonId uniqueidentifier NULL,
        AccountId uniqueidentifier NULL,
        MappingStatus tinyint NOT NULL, -- 0 Pending, 1 Linked, 2 Separate, 3 Rejected, 4 ManualReview
        MatchMethod nvarchar(80) NULL,
        EvidenceReference nvarchar(500) NULL,
        ReviewReason nvarchar(1000) NULL,
        ReviewedAtUtc datetime2(3) NULL,
        ReviewedByAccountId uniqueidentifier NULL,
        CreatedAtUtc datetime2(3) NOT NULL
            CONSTRAINT DF_LegacyIdentityMap_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc datetime2(3) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT FK_LegacyIdentityMap_Persons FOREIGN KEY(PersonId)
            REFERENCES dbo.Persons(PersonId) ON DELETE NO ACTION,
        CONSTRAINT FK_LegacyIdentityMap_Accounts FOREIGN KEY(AccountId)
            REFERENCES dbo.UserAccounts(AccountId) ON DELETE NO ACTION,
        CONSTRAINT FK_LegacyIdentityMap_ReviewedBy FOREIGN KEY(ReviewedByAccountId)
            REFERENCES dbo.UserAccounts(AccountId) ON DELETE NO ACTION,
        CONSTRAINT UQ_LegacyIdentityMap_Source UNIQUE(SourceSystem, SourceTable, SourceKey),
        CONSTRAINT CK_LegacyIdentityMap_SourceTable CHECK (SourceTable IN (N'Users', N'Residents')),
        CONSTRAINT CK_LegacyIdentityMap_Status CHECK (MappingStatus IN (0,1,2,3,4)),
        CONSTRAINT CK_LegacyIdentityMap_ReviewStamp CHECK
        (
            (ReviewedAtUtc IS NULL AND ReviewedByAccountId IS NULL)
            OR (ReviewedAtUtc IS NOT NULL AND ReviewedByAccountId IS NOT NULL)
        ),
        CONSTRAINT CK_LegacyIdentityMap_LinkTargets CHECK
        (
            (MappingStatus IN (0,3,4) AND PersonId IS NULL AND AccountId IS NULL)
            OR (MappingStatus IN (1,2) AND PersonId IS NOT NULL)
        ),
        CONSTRAINT CK_LegacyIdentityMap_UserAccountTarget CHECK
        (
            (SourceTable <> N'Users' OR MappingStatus NOT IN (1,2) OR AccountId IS NOT NULL)
            AND (SourceTable <> N'Residents' OR AccountId IS NULL)
        )
    );
    CREATE UNIQUE INDEX UX_LegacyIdentityMap_Account
        ON dbo.LegacyIdentityMap(AccountId) WHERE AccountId IS NOT NULL;
    CREATE INDEX IX_LegacyIdentityMap_Person
        ON dbo.LegacyIdentityMap(PersonId) WHERE PersonId IS NOT NULL;
    CREATE INDEX IX_LegacyIdentityMap_Account
        ON dbo.LegacyIdentityMap(AccountId) WHERE AccountId IS NOT NULL;
    CREATE INDEX IX_LegacyIdentityMap_Status_Source
        ON dbo.LegacyIdentityMap(MappingStatus, SourceTable, SourceSystem);
END;

COMMIT TRANSACTION;
