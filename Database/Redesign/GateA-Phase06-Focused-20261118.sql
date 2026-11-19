BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE TABLE [Persons] (
        [PersonId] uniqueidentifier NOT NULL,
        [LegalName] nvarchar(200) NULL,
        [DisplayName] nvarchar(160) NOT NULL,
        [PrimaryEmail] nvarchar(254) NULL,
        [PrimaryPhone] nvarchar(32) NULL,
        [Whatsapp] nvarchar(32) NULL,
        [PhotoUrl] nvarchar(2048) NULL,
        [IsArchived] bit NOT NULL,
        [ArchivedAtUtc] datetime2(3) NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAtUtc] datetime2(3) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Persons] PRIMARY KEY ([PersonId]),
        CONSTRAINT [CK_Persons_ArchiveState] CHECK ((([IsArchived] = 0 AND [ArchivedAtUtc] IS NULL) OR ([IsArchived] = 1 AND [ArchivedAtUtc] IS NOT NULL)))
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE TABLE [RoleDefinitions] (
        [RoleKey] nvarchar(64) NOT NULL,
        [DisplayName] nvarchar(120) NOT NULL,
        [IsSystemRole] bit NOT NULL DEFAULT CAST(0 AS bit),
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_RoleDefinitions] PRIMARY KEY ([RoleKey])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE TABLE [UserAccounts] (
        [AccountId] uniqueidentifier NOT NULL,
        [PersonId] uniqueidentifier NOT NULL,
        [AccountStatus] tinyint NOT NULL,
        [NormalizedEmail] nvarchar(254) NULL,
        [ExternalProvider] nvarchar(40) NULL,
        [ExternalSubject] nvarchar(200) NULL,
        [SecurityStamp] nvarchar(128) NOT NULL,
        [LastLoginAtUtc] datetime2(3) NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UserAccounts] PRIMARY KEY ([AccountId]),
        CONSTRAINT [UQ_UserAccounts_PersonId] UNIQUE ([PersonId]),
        CONSTRAINT [CK_UserAccounts_AccountStatus] CHECK ([AccountStatus] IN (0,1,2,3)),
        CONSTRAINT [CK_UserAccounts_ExternalLoginPair] CHECK ((([ExternalProvider] IS NULL AND [ExternalSubject] IS NULL) OR ([ExternalProvider] IS NOT NULL AND [ExternalSubject] IS NOT NULL))),
        CONSTRAINT [FK_UserAccounts_Persons_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [Persons] ([PersonId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE TABLE [RolePermissions] (
        [RoleKey] nvarchar(64) NOT NULL,
        [PermissionKey] nvarchar(100) NOT NULL,
        [Effect] tinyint NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleKey], [PermissionKey]),
        CONSTRAINT [CK_RolePermissions_Effect] CHECK ([Effect] IN (0,1)),
        CONSTRAINT [FK_RolePermissions_RoleDefinitions_RoleKey] FOREIGN KEY ([RoleKey]) REFERENCES [RoleDefinitions] ([RoleKey])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE TABLE [AccountPermissionOverrides] (
        [OverrideId] uniqueidentifier NOT NULL,
        [AccountId] uniqueidentifier NOT NULL,
        [PermissionKey] nvarchar(100) NOT NULL,
        [Effect] tinyint NOT NULL,
        [ScopeType] tinyint NOT NULL,
        [ScopeBuildingId] nvarchar(64) NULL,
        [ScopeApartmentId] nvarchar(64) NULL,
        [ValidFromUtc] datetime2(3) NOT NULL,
        [ValidToUtc] datetime2(3) NULL,
        [GrantedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [GrantedByAccountId] uniqueidentifier NULL,
        [GrantReason] nvarchar(500) NULL,
        [RevokedAtUtc] datetime2(3) NULL,
        [RevokedByAccountId] uniqueidentifier NULL,
        [RevocationReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AccountPermissionOverrides] PRIMARY KEY ([OverrideId]),
        CONSTRAINT [CK_AccountPermissionOverrides_Effect] CHECK ([Effect] IN (0,1)),
        CONSTRAINT [CK_AccountPermissionOverrides_Revocation] CHECK ((([RevokedAtUtc] IS NULL AND [RevokedByAccountId] IS NULL AND [RevocationReason] IS NULL) OR ([RevokedAtUtc] IS NOT NULL AND [RevokedByAccountId] IS NOT NULL))),
        CONSTRAINT [CK_AccountPermissionOverrides_Scope] CHECK ((([ScopeType] IN (0,3) AND [ScopeBuildingId] IS NULL AND [ScopeApartmentId] IS NULL) OR ([ScopeType] = 1 AND [ScopeBuildingId] IS NOT NULL AND [ScopeApartmentId] IS NULL) OR ([ScopeType] = 2 AND [ScopeBuildingId] IS NOT NULL AND [ScopeApartmentId] IS NOT NULL))),
        CONSTRAINT [CK_AccountPermissionOverrides_Validity] CHECK ([ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]),
        CONSTRAINT [FK_AccountPermissionOverrides_Apartments_ScopeBuildingId_ScopeApartmentId] FOREIGN KEY ([ScopeBuildingId], [ScopeApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
        CONSTRAINT [FK_AccountPermissionOverrides_Buildings_ScopeBuildingId] FOREIGN KEY ([ScopeBuildingId]) REFERENCES [Buildings] ([Id]),
        CONSTRAINT [FK_AccountPermissionOverrides_UserAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [UserAccounts] ([AccountId]),
        CONSTRAINT [FK_AccountPermissionOverrides_UserAccounts_GrantedByAccountId] FOREIGN KEY ([GrantedByAccountId]) REFERENCES [UserAccounts] ([AccountId]),
        CONSTRAINT [FK_AccountPermissionOverrides_UserAccounts_RevokedByAccountId] FOREIGN KEY ([RevokedByAccountId]) REFERENCES [UserAccounts] ([AccountId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE TABLE [ApartmentAccessGrants] (
        [GrantId] uniqueidentifier NOT NULL,
        [BuildingId] nvarchar(64) NOT NULL,
        [ApartmentId] nvarchar(64) NOT NULL,
        [PersonId] uniqueidentifier NOT NULL,
        [GrantType] tinyint NOT NULL,
        [PermissionSetKey] nvarchar(80) NOT NULL,
        [ValidFromUtc] datetime2(3) NOT NULL,
        [ValidToUtc] datetime2(3) NULL,
        [GrantedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [GrantedByAccountId] uniqueidentifier NOT NULL,
        [RevokedAtUtc] datetime2(3) NULL,
        [RevokedByAccountId] uniqueidentifier NULL,
        [Reason] nvarchar(500) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ApartmentAccessGrants] PRIMARY KEY ([GrantId]),
        CONSTRAINT [CK_ApartmentAccessGrants_BoundedUrgentAccess] CHECK (([GrantType] NOT IN (4,5) OR [ValidToUtc] IS NOT NULL)),
        CONSTRAINT [CK_ApartmentAccessGrants_GrantType] CHECK ([GrantType] IN (1,2,3,4,5)),
        CONSTRAINT [CK_ApartmentAccessGrants_PermissionSet] CHECK ((([GrantType] = 1 AND [PermissionSetKey] = N'apartment.access.family.v1') OR ([GrantType] = 2 AND [PermissionSetKey] = N'apartment.access.tenant.v1') OR ([GrantType] = 3 AND [PermissionSetKey] = N'apartment.access.caretaker.v1') OR ([GrantType] = 4 AND [PermissionSetKey] = N'apartment.access.emergency.v1') OR ([GrantType] = 5 AND [PermissionSetKey] = N'apartment.access.temporary.v1'))),
        CONSTRAINT [CK_ApartmentAccessGrants_Reason] CHECK (LEN(LTRIM(RTRIM([Reason]))) > 0),
        CONSTRAINT [CK_ApartmentAccessGrants_Revocation] CHECK ((([RevokedAtUtc] IS NULL AND [RevokedByAccountId] IS NULL) OR ([RevokedAtUtc] IS NOT NULL AND [RevokedByAccountId] IS NOT NULL))),
        CONSTRAINT [CK_ApartmentAccessGrants_Validity] CHECK ([ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]),
        CONSTRAINT [FK_ApartmentAccessGrants_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
        CONSTRAINT [FK_ApartmentAccessGrants_Persons_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [Persons] ([PersonId]),
        CONSTRAINT [FK_ApartmentAccessGrants_UserAccounts_GrantedByAccountId] FOREIGN KEY ([GrantedByAccountId]) REFERENCES [UserAccounts] ([AccountId]),
        CONSTRAINT [FK_ApartmentAccessGrants_UserAccounts_RevokedByAccountId] FOREIGN KEY ([RevokedByAccountId]) REFERENCES [UserAccounts] ([AccountId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE TABLE [ApartmentOccupancies] (
        [OccupancyId] uniqueidentifier NOT NULL,
        [BuildingId] nvarchar(64) NOT NULL,
        [ApartmentId] nvarchar(64) NOT NULL,
        [PersonId] uniqueidentifier NOT NULL,
        [OccupancyType] tinyint NOT NULL,
        [IsPrimary] bit NOT NULL,
        [ValidFromUtc] datetime2(3) NOT NULL,
        [ValidToUtc] datetime2(3) NULL,
        [RecordedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [RecordedByAccountId] uniqueidentifier NULL,
        [Notes] nvarchar(1000) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ApartmentOccupancies] PRIMARY KEY ([OccupancyId]),
        CONSTRAINT [CK_ApartmentOccupancies_Type] CHECK ([OccupancyType] IN (0,1,2,3)),
        CONSTRAINT [CK_ApartmentOccupancies_Validity] CHECK ([ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]),
        CONSTRAINT [FK_ApartmentOccupancies_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
        CONSTRAINT [FK_ApartmentOccupancies_Persons_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [Persons] ([PersonId]),
        CONSTRAINT [FK_ApartmentOccupancies_UserAccounts_RecordedByAccountId] FOREIGN KEY ([RecordedByAccountId]) REFERENCES [UserAccounts] ([AccountId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE TABLE [ApartmentOwnerships] (
        [OwnershipId] uniqueidentifier NOT NULL,
        [BuildingId] nvarchar(64) NOT NULL,
        [ApartmentId] nvarchar(64) NOT NULL,
        [PersonId] uniqueidentifier NOT NULL,
        [OwnershipSharePercent] decimal(9,6) NOT NULL,
        [CompletenessStatus] tinyint NOT NULL DEFAULT CAST(0 AS tinyint),
        [ValidFromUtc] datetime2(3) NOT NULL,
        [ValidToUtc] datetime2(3) NULL,
        [RecordedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [RecordedByAccountId] uniqueidentifier NULL,
        [EvidenceReference] nvarchar(500) NOT NULL,
        [CompletenessVerifiedAtUtc] datetime2(3) NULL,
        [CompletenessVerifiedByAccountId] uniqueidentifier NULL,
        [Notes] nvarchar(1000) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ApartmentOwnerships] PRIMARY KEY ([OwnershipId]),
        CONSTRAINT [CK_ApartmentOwnerships_CompletenessStatus] CHECK ([CompletenessStatus] IN (0,1,2)),
        CONSTRAINT [CK_ApartmentOwnerships_CompletenessVerification] CHECK ((([CompletenessStatus] = 2 AND [CompletenessVerifiedAtUtc] IS NOT NULL AND [CompletenessVerifiedByAccountId] IS NOT NULL) OR ([CompletenessStatus] IN (0,1) AND [CompletenessVerifiedAtUtc] IS NULL AND [CompletenessVerifiedByAccountId] IS NULL))),
        CONSTRAINT [CK_ApartmentOwnerships_Evidence] CHECK (LEN(LTRIM(RTRIM([EvidenceReference]))) > 0),
        CONSTRAINT [CK_ApartmentOwnerships_Share] CHECK ([OwnershipSharePercent] > 0 AND [OwnershipSharePercent] <= 100),
        CONSTRAINT [CK_ApartmentOwnerships_Validity] CHECK ([ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]),
        CONSTRAINT [FK_ApartmentOwnerships_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
        CONSTRAINT [FK_ApartmentOwnerships_Persons_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [Persons] ([PersonId]),
        CONSTRAINT [FK_ApartmentOwnerships_UserAccounts_CompletenessVerifiedByAccountId] FOREIGN KEY ([CompletenessVerifiedByAccountId]) REFERENCES [UserAccounts] ([AccountId]),
        CONSTRAINT [FK_ApartmentOwnerships_UserAccounts_RecordedByAccountId] FOREIGN KEY ([RecordedByAccountId]) REFERENCES [UserAccounts] ([AccountId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE TABLE [LegacyIdentityMap] (
        [LegacyIdentityMapId] uniqueidentifier NOT NULL,
        [SourceSystem] nvarchar(80) NOT NULL,
        [SourceTable] nvarchar(128) NOT NULL,
        [SourceKey] nvarchar(256) NOT NULL,
        [PersonId] uniqueidentifier NULL,
        [AccountId] uniqueidentifier NULL,
        [MappingStatus] tinyint NOT NULL,
        [MatchMethod] nvarchar(80) NULL,
        [EvidenceReference] nvarchar(500) NULL,
        [ReviewReason] nvarchar(1000) NULL,
        [ReviewedAtUtc] datetime2(3) NULL,
        [ReviewedByAccountId] uniqueidentifier NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAtUtc] datetime2(3) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_LegacyIdentityMap] PRIMARY KEY ([LegacyIdentityMapId]),
        CONSTRAINT [UQ_LegacyIdentityMap_Source] UNIQUE ([SourceSystem], [SourceTable], [SourceKey]),
        CONSTRAINT [CK_LegacyIdentityMap_LinkTargets] CHECK ((([MappingStatus] IN (0,3,4) AND [PersonId] IS NULL AND [AccountId] IS NULL) OR ([MappingStatus] IN (1,2) AND [PersonId] IS NOT NULL))),
        CONSTRAINT [CK_LegacyIdentityMap_ResolvedReview] CHECK (([MappingStatus] NOT IN (1,2) OR ([ReviewedAtUtc] IS NOT NULL AND [ReviewedByAccountId] IS NOT NULL AND [ReviewReason] IS NOT NULL AND LEN(LTRIM(RTRIM([ReviewReason]))) > 0))),
        CONSTRAINT [CK_LegacyIdentityMap_ReviewStamp] CHECK ((([ReviewedAtUtc] IS NULL AND [ReviewedByAccountId] IS NULL) OR ([ReviewedAtUtc] IS NOT NULL AND [ReviewedByAccountId] IS NOT NULL))),
        CONSTRAINT [CK_LegacyIdentityMap_SourceTable] CHECK ([SourceTable] IN (N'Users', N'Residents')),
        CONSTRAINT [CK_LegacyIdentityMap_Status] CHECK ([MappingStatus] IN (0,1,2,3,4)),
        CONSTRAINT [CK_LegacyIdentityMap_UserAccountTarget] CHECK ((([SourceTable] <> N'Users' OR [MappingStatus] NOT IN (1,2) OR [AccountId] IS NOT NULL) AND ([SourceTable] <> N'Residents' OR [AccountId] IS NULL))),
        CONSTRAINT [FK_LegacyIdentityMap_Persons_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [Persons] ([PersonId]),
        CONSTRAINT [FK_LegacyIdentityMap_UserAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [UserAccounts] ([AccountId]),
        CONSTRAINT [FK_LegacyIdentityMap_UserAccounts_ReviewedByAccountId] FOREIGN KEY ([ReviewedByAccountId]) REFERENCES [UserAccounts] ([AccountId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE TABLE [UserCredentials] (
        [CredentialId] uniqueidentifier NOT NULL,
        [AccountId] uniqueidentifier NOT NULL,
        [CredentialType] tinyint NOT NULL,
        [CredentialHash] nvarchar(512) NOT NULL,
        [HashAlgorithm] nvarchar(40) NOT NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ExpiresAtUtc] datetime2(3) NULL,
        [RevokedAtUtc] datetime2(3) NULL,
        [ReplacedByCredentialId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UserCredentials] PRIMARY KEY ([CredentialId]),
        CONSTRAINT [UQ_UserCredentials_Credential_Account] UNIQUE ([CredentialId], [AccountId]),
        CONSTRAINT [CK_UserCredentials_Expiry] CHECK ([ExpiresAtUtc] IS NULL OR [ExpiresAtUtc] > [CreatedAtUtc]),
        CONSTRAINT [CK_UserCredentials_Type] CHECK ([CredentialType] IN (0,1)),
        CONSTRAINT [FK_UserCredentials_UserAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [UserAccounts] ([AccountId]),
        CONSTRAINT [FK_UserCredentials_UserCredentials_ReplacedByCredentialId_AccountId] FOREIGN KEY ([ReplacedByCredentialId], [AccountId]) REFERENCES [UserCredentials] ([CredentialId], [AccountId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE TABLE [UserRecoveryCodes] (
        [RecoveryCodeId] uniqueidentifier NOT NULL,
        [AccountId] uniqueidentifier NOT NULL,
        [CodeHash] varbinary(64) NOT NULL,
        [CreatedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UsedAtUtc] datetime2(3) NULL,
        CONSTRAINT [PK_UserRecoveryCodes] PRIMARY KEY ([RecoveryCodeId]),
        CONSTRAINT [FK_UserRecoveryCodes_UserAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [UserAccounts] ([AccountId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE TABLE [UserRoleAssignments] (
        [RoleAssignmentId] uniqueidentifier NOT NULL,
        [AccountId] uniqueidentifier NOT NULL,
        [RoleKey] nvarchar(64) NOT NULL,
        [ScopeType] tinyint NOT NULL,
        [ScopeBuildingId] nvarchar(64) NULL,
        [ScopeApartmentId] nvarchar(64) NULL,
        [ValidFromUtc] datetime2(3) NOT NULL,
        [ValidToUtc] datetime2(3) NULL,
        [GrantedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [GrantedByAccountId] uniqueidentifier NULL,
        [GrantReason] nvarchar(500) NULL,
        [RevokedAtUtc] datetime2(3) NULL,
        [RevokedByAccountId] uniqueidentifier NULL,
        [RevocationReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UserRoleAssignments] PRIMARY KEY ([RoleAssignmentId]),
        CONSTRAINT [CK_UserRoleAssignments_Revocation] CHECK ((([RevokedAtUtc] IS NULL AND [RevokedByAccountId] IS NULL AND [RevocationReason] IS NULL) OR ([RevokedAtUtc] IS NOT NULL AND [RevokedByAccountId] IS NOT NULL))),
        CONSTRAINT [CK_UserRoleAssignments_Scope] CHECK ((([ScopeType] IN (0,3) AND [ScopeBuildingId] IS NULL AND [ScopeApartmentId] IS NULL) OR ([ScopeType] = 1 AND [ScopeBuildingId] IS NOT NULL AND [ScopeApartmentId] IS NULL) OR ([ScopeType] = 2 AND [ScopeBuildingId] IS NOT NULL AND [ScopeApartmentId] IS NOT NULL))),
        CONSTRAINT [CK_UserRoleAssignments_Validity] CHECK ([ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]),
        CONSTRAINT [FK_UserRoleAssignments_Apartments_ScopeBuildingId_ScopeApartmentId] FOREIGN KEY ([ScopeBuildingId], [ScopeApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
        CONSTRAINT [FK_UserRoleAssignments_Buildings_ScopeBuildingId] FOREIGN KEY ([ScopeBuildingId]) REFERENCES [Buildings] ([Id]),
        CONSTRAINT [FK_UserRoleAssignments_RoleDefinitions_RoleKey] FOREIGN KEY ([RoleKey]) REFERENCES [RoleDefinitions] ([RoleKey]),
        CONSTRAINT [FK_UserRoleAssignments_UserAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [UserAccounts] ([AccountId]),
        CONSTRAINT [FK_UserRoleAssignments_UserAccounts_GrantedByAccountId] FOREIGN KEY ([GrantedByAccountId]) REFERENCES [UserAccounts] ([AccountId]),
        CONSTRAINT [FK_UserRoleAssignments_UserAccounts_RevokedByAccountId] FOREIGN KEY ([RevokedByAccountId]) REFERENCES [UserAccounts] ([AccountId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE TABLE [UserTwoFactorSettings] (
        [AccountId] uniqueidentifier NOT NULL,
        [IsEnabled] bit NOT NULL,
        [SecretCiphertext] varbinary(max) NULL,
        [KeyVersion] nvarchar(64) NULL,
        [LastAcceptedTimeStep] bigint NULL,
        [EnrolledAtUtc] datetime2(3) NULL,
        [LastVerifiedAtUtc] datetime2(3) NULL,
        [UpdatedAtUtc] datetime2(3) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UserTwoFactorSettings] PRIMARY KEY ([AccountId]),
        CONSTRAINT [CK_UserTwoFactorSettings_EnabledSecret] CHECK ((([IsEnabled] = 0 AND [SecretCiphertext] IS NULL AND [KeyVersion] IS NULL) OR ([IsEnabled] = 1 AND [SecretCiphertext] IS NOT NULL AND [KeyVersion] IS NOT NULL AND [EnrolledAtUtc] IS NOT NULL))),
        CONSTRAINT [FK_UserTwoFactorSettings_UserAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [UserAccounts] ([AccountId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_AccountPermissionOverrides_Account_Permission_Scope] ON [AccountPermissionOverrides] ([AccountId], [PermissionKey], [ScopeType], [ScopeBuildingId], [ScopeApartmentId], [ValidFromUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_AccountPermissionOverrides_GrantedByAccountId] ON [AccountPermissionOverrides] ([GrantedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_AccountPermissionOverrides_Permission_Scope] ON [AccountPermissionOverrides] ([PermissionKey], [ScopeType], [ScopeBuildingId], [ScopeApartmentId], [ValidFromUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_AccountPermissionOverrides_RevokedByAccountId] ON [AccountPermissionOverrides] ([RevokedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_AccountPermissionOverrides_ScopeBuildingId_ScopeApartmentId] ON [AccountPermissionOverrides] ([ScopeBuildingId], [ScopeApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentAccessGrants_Apartment_Period] ON [ApartmentAccessGrants] ([BuildingId], [ApartmentId], [ValidFromUtc], [ValidToUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentAccessGrants_GrantedByAccountId] ON [ApartmentAccessGrants] ([GrantedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentAccessGrants_Person_Period] ON [ApartmentAccessGrants] ([PersonId], [ValidFromUtc], [ValidToUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentAccessGrants_RevokedByAccountId] ON [ApartmentAccessGrants] ([RevokedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentOccupancies_Apartment_Period] ON [ApartmentOccupancies] ([BuildingId], [ApartmentId], [ValidFromUtc], [ValidToUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentOccupancies_Person_Period] ON [ApartmentOccupancies] ([PersonId], [ValidFromUtc], [ValidToUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentOccupancies_RecordedByAccountId] ON [ApartmentOccupancies] ([RecordedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_ApartmentOccupancies_OneOpenPrimary] ON [ApartmentOccupancies] ([BuildingId], [ApartmentId]) WHERE [IsPrimary] = 1 AND [ValidToUtc] IS NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentOwnerships_Apartment_Period] ON [ApartmentOwnerships] ([BuildingId], [ApartmentId], [ValidFromUtc], [ValidToUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentOwnerships_CompletenessVerifiedByAccountId] ON [ApartmentOwnerships] ([CompletenessVerifiedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentOwnerships_Person_Period] ON [ApartmentOwnerships] ([PersonId], [ValidFromUtc], [ValidToUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentOwnerships_RecordedByAccountId] ON [ApartmentOwnerships] ([RecordedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_LegacyIdentityMap_Person] ON [LegacyIdentityMap] ([PersonId]) WHERE [PersonId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_LegacyIdentityMap_ReviewedByAccountId] ON [LegacyIdentityMap] ([ReviewedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_LegacyIdentityMap_Status_Source] ON [LegacyIdentityMap] ([MappingStatus], [SourceTable], [SourceSystem]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_LegacyIdentityMap_Account] ON [LegacyIdentityMap] ([AccountId]) WHERE [AccountId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Persons_PrimaryEmail] ON [Persons] ([PrimaryEmail]) WHERE [PrimaryEmail] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Persons_PrimaryPhone] ON [Persons] ([PrimaryPhone]) WHERE [PrimaryPhone] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_UserAccounts_NormalizedEmail] ON [UserAccounts] ([NormalizedEmail]) WHERE [NormalizedEmail] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_UserAccounts_ExternalLogin] ON [UserAccounts] ([ExternalProvider], [ExternalSubject]) WHERE [ExternalProvider] IS NOT NULL AND [ExternalSubject] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserCredentials_Account_Type] ON [UserCredentials] ([AccountId], [CredentialType], [RevokedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserCredentials_ReplacedByCredentialId_AccountId] ON [UserCredentials] ([ReplacedByCredentialId], [AccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserRecoveryCodes_Account_Unused] ON [UserRecoveryCodes] ([AccountId], [UsedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [UX_UserRecoveryCodes_CodeHash] ON [UserRecoveryCodes] ([CodeHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_Account_Scope] ON [UserRoleAssignments] ([AccountId], [ScopeType], [ScopeBuildingId], [ScopeApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_GrantedByAccountId] ON [UserRoleAssignments] ([GrantedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_RevokedByAccountId] ON [UserRoleAssignments] ([RevokedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_Role_Scope] ON [UserRoleAssignments] ([RoleKey], [ScopeType], [ScopeBuildingId], [ScopeApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_ScopeBuildingId_ScopeApartmentId] ON [UserRoleAssignments] ([ScopeBuildingId], [ScopeApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118182110_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261118182110_Phase06_IdentityAuthorizationFoundation', N'8.0.10');
END;
GO

COMMIT;
GO
