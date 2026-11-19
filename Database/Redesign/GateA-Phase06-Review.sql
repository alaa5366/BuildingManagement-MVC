IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [BackupHistory] (
        [Id] nvarchar(450) NOT NULL,
        [FileName] nvarchar(max) NOT NULL,
        [FileSize] bigint NOT NULL,
        [CollectionsJson] nvarchar(max) NOT NULL,
        [TotalDocuments] int NOT NULL,
        [CreatedAt] datetime2(0) NOT NULL,
        [DurationSeconds] float NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Source] nvarchar(max) NOT NULL,
        [ErrorMessage] nvarchar(max) NULL,
        CONSTRAINT [PK_BackupHistory] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [Buildings] (
        [Id] nvarchar(450) NOT NULL,
        [BuildingNumber] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [AdminPin] nvarchar(max) NOT NULL,
        [DataVersion] nvarchar(max) NOT NULL,
        [LogoUrl] nvarchar(max) NOT NULL,
        [AdminWhatsapp] nvarchar(max) NOT NULL,
        [PaymentLabel] nvarchar(max) NOT NULL,
        [PaymentAccountNumber] nvarchar(max) NOT NULL,
        [PaymentPhone] nvarchar(max) NOT NULL,
        [PaymentNotes] nvarchar(max) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2(0) NOT NULL,
        CONSTRAINT [PK_Buildings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [GlobalSettings] (
        [Id] nvarchar(450) NOT NULL,
        [AppName] nvarchar(max) NOT NULL,
        [DefaultLanguage] nvarchar(max) NOT NULL,
        [WhatsappApiToken] nvarchar(max) NOT NULL,
        [WhatsappPhoneId] nvarchar(max) NOT NULL,
        [CloudinaryCloudName] nvarchar(max) NOT NULL,
        [CloudinaryApiKey] nvarchar(max) NOT NULL,
        [EnablePolls] bit NOT NULL,
        [EnableMaintenance] bit NOT NULL,
        [EnableOcr] bit NOT NULL,
        [EnableQrAccess] bit NOT NULL,
        [EnableWhatsApp] bit NOT NULL,
        [SessionTimeoutDays] int NOT NULL,
        [MaxPinAttempts] int NOT NULL,
        [UpdatedAt] datetime2(0) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [StorageMode] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_GlobalSettings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [Presence] (
        [Uid] nvarchar(450) NOT NULL,
        [BuildingId] nvarchar(max) NULL,
        [ApartmentId] nvarchar(max) NULL,
        [LastSeen] datetime2(3) NOT NULL,
        CONSTRAINT [PK_Presence] PRIMARY KEY ([Uid])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [QrUsages] (
        [Id] nvarchar(128) NOT NULL,
        [TokenId] nvarchar(128) NOT NULL,
        [UseCount] int NOT NULL,
        [FirstUsedAt] nvarchar(max) NOT NULL,
        [LastUsedAt] nvarchar(max) NOT NULL,
        [DeviceFingerprint] nvarchar(max) NULL,
        [CreatedAt] datetime2(0) NOT NULL,
        CONSTRAINT [PK_QrUsages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [ScheduledBackupSettings] (
        [Id] nvarchar(450) NOT NULL,
        [Enabled] bit NOT NULL,
        [Frequency] nvarchar(max) NOT NULL,
        [Hour] int NOT NULL,
        [DayOfWeek] int NULL,
        [DayOfMonth] int NULL,
        [CollectionsJson] nvarchar(max) NOT NULL,
        [MaxBackupsToKeep] int NOT NULL,
        [LastRunAt] datetime2(0) NULL,
        [LastRunStatus] nvarchar(max) NULL,
        [UpdatedAt] datetime2(0) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_ScheduledBackupSettings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [SyncPendingChanges] (
        [Id] bigint NOT NULL IDENTITY,
        [EntityType] nvarchar(450) NOT NULL,
        [EntityId] nvarchar(450) NOT NULL,
        [Operation] nvarchar(max) NOT NULL,
        [Payload] nvarchar(max) NULL,
        [CreatedAt] datetime2(0) NOT NULL,
        [Applied] bit NOT NULL,
        [AppliedAt] datetime2(0) NULL,
        [RetryCount] int NOT NULL,
        [ErrorMessage] nvarchar(max) NULL,
        CONSTRAINT [PK_SyncPendingChanges] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Uid] nvarchar(450) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Whatsapp] nvarchar(max) NOT NULL,
        [Pin] nvarchar(max) NOT NULL,
        [PhotoUrl] nvarchar(max) NOT NULL,
        [Role] nvarchar(max) NOT NULL,
        [IsDisabled] bit NOT NULL,
        [DisabledReason] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [LastLoginAt] datetime2(0) NULL,
        [CreatedAt] datetime2(0) NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedAt] datetime2(0) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Uid])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLog] (
        [AuditId] bigint NOT NULL IDENTITY,
        [SourceId] nvarchar(max) NULL,
        [BuildingId] nvarchar(450) NULL,
        [Ts] datetime2(0) NOT NULL,
        [Action] nvarchar(max) NOT NULL,
        [Label] nvarchar(max) NOT NULL,
        [Actor] nvarchar(max) NOT NULL,
        [ActorRole] nvarchar(max) NOT NULL,
        [Details] nvarchar(max) NOT NULL,
        [MonthKey] nvarchar(max) NOT NULL,
        [BuildingNumber] nvarchar(max) NULL,
        [AptNumber] int NULL,
        [ApartmentId] nvarchar(64) NULL,
        [Severity] nvarchar(20) NOT NULL DEFAULT N'info',
        CONSTRAINT [PK_AuditLog] PRIMARY KEY ([AuditId]),
        CONSTRAINT [FK_AuditLog_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [BuildingSettings] (
        [BuildingId] nvarchar(450) NOT NULL,
        [DisplayName] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [WhatsappNumber] nvarchar(max) NOT NULL,
        [InvoiceDayOfMonth] int NOT NULL,
        [ExpenseDistribution] nvarchar(max) NOT NULL,
        [VotingQuorumPercent] int NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2(0) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_BuildingSettings] PRIMARY KEY ([BuildingId]),
        CONSTRAINT [FK_BuildingSettings_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [Dvrs] (
        [Id] nvarchar(450) NOT NULL,
        [BuildingId] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [IpAddress] nvarchar(max) NOT NULL,
        [Port] int NOT NULL,
        [Brand] nvarchar(max) NOT NULL,
        [Username] nvarchar(max) NOT NULL,
        [PasswordEncrypted] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2(0) NOT NULL,
        [UpdatedAt] datetime2(0) NULL,
        CONSTRAINT [PK_Dvrs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Dvrs_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [FinancialCategories] (
        [BuildingId] nvarchar(450) NOT NULL,
        [Id] nvarchar(450) NOT NULL,
        [Kind] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Color] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_FinancialCategories] PRIMARY KEY ([BuildingId], [Kind], [Id]),
        CONSTRAINT [FK_FinancialCategories_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [Floors] (
        [BuildingId] nvarchar(450) NOT NULL,
        [Id] nvarchar(450) NOT NULL,
        [Label] nvarchar(max) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Floors] PRIMARY KEY ([BuildingId], [Id]),
        CONSTRAINT [FK_Floors_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [MaintenanceRecords] (
        [BuildingId] nvarchar(450) NOT NULL,
        [Id] nvarchar(450) NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [MaintenanceDate] date NOT NULL,
        [Cost] decimal(18,2) NOT NULL,
        [Vendor] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2(0) NOT NULL,
        [AddedToExpense] bit NOT NULL,
        [AddedToExpenseAt] datetime2(0) NULL,
        CONSTRAINT [PK_MaintenanceRecords] PRIMARY KEY ([BuildingId], [Id]),
        CONSTRAINT [FK_MaintenanceRecords_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [Polls] (
        [BuildingId] nvarchar(450) NOT NULL,
        [Id] nvarchar(450) NOT NULL,
        [Question] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2(0) NOT NULL,
        [Deadline] datetime2(0) NULL,
        [IsClosed] bit NOT NULL,
        CONSTRAINT [PK_Polls] PRIMARY KEY ([BuildingId], [Id]),
        CONSTRAINT [FK_Polls_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [BuildingAdmins] (
        [BuildingId] nvarchar(450) NOT NULL,
        [AdminUid] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_BuildingAdmins] PRIMARY KEY ([BuildingId], [AdminUid]),
        CONSTRAINT [FK_BuildingAdmins_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BuildingAdmins_Users_AdminUid] FOREIGN KEY ([AdminUid]) REFERENCES [Users] ([Uid]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [UserPermissions] (
        [Uid] nvarchar(450) NOT NULL,
        [Permission] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_UserPermissions] PRIMARY KEY ([Uid], [Permission]),
        CONSTRAINT [FK_UserPermissions_Users_Uid] FOREIGN KEY ([Uid]) REFERENCES [Users] ([Uid]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [Cameras] (
        [Id] nvarchar(450) NOT NULL,
        [DvrId] nvarchar(450) NOT NULL,
        [BuildingId] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Channel] int NOT NULL,
        [RtspPath] nvarchar(max) NOT NULL,
        [HlsUrl] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2(0) NOT NULL,
        CONSTRAINT [PK_Cameras] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Cameras_Dvrs_DvrId] FOREIGN KEY ([DvrId]) REFERENCES [Dvrs] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [Expenses] (
        [BuildingId] nvarchar(450) NOT NULL,
        [Id] nvarchar(450) NOT NULL,
        [MonthKey] nvarchar(max) NOT NULL,
        [CategoryId] nvarchar(450) NULL,
        [CategoryKind] nvarchar(450) NOT NULL,
        [Note] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [ExpenseDate] date NOT NULL,
        [ReceiptUrl] nvarchar(max) NULL,
        CONSTRAINT [PK_Expenses] PRIMARY KEY ([BuildingId], [Id]),
        CONSTRAINT [FK_Expenses_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Expenses_FinancialCategories_BuildingId_CategoryKind_CategoryId] FOREIGN KEY ([BuildingId], [CategoryKind], [CategoryId]) REFERENCES [FinancialCategories] ([BuildingId], [Kind], [Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [Revenues] (
        [BuildingId] nvarchar(450) NOT NULL,
        [Id] nvarchar(450) NOT NULL,
        [MonthKey] nvarchar(max) NOT NULL,
        [CategoryId] nvarchar(450) NULL,
        [CategoryKind] nvarchar(450) NOT NULL,
        [Note] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [RevenueDate] date NOT NULL,
        CONSTRAINT [PK_Revenues] PRIMARY KEY ([BuildingId], [Id]),
        CONSTRAINT [FK_Revenues_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Revenues_FinancialCategories_BuildingId_CategoryKind_CategoryId] FOREIGN KEY ([BuildingId], [CategoryKind], [CategoryId]) REFERENCES [FinancialCategories] ([BuildingId], [Kind], [Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [Apartments] (
        [BuildingId] nvarchar(450) NOT NULL,
        [Id] nvarchar(450) NOT NULL,
        [FloorId] nvarchar(450) NOT NULL,
        [Number] int NOT NULL,
        [Owner] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [MonthlyFee] decimal(18,2) NOT NULL,
        [Pin] nvarchar(max) NOT NULL,
        [IsClosed] bit NOT NULL,
        [Label] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [OpenDate] date NULL,
        [CloseDate] date NULL,
        [IsDisabled] bit NOT NULL,
        [DisabledReason] nvarchar(max) NOT NULL,
        [Language] nvarchar(max) NOT NULL,
        [NotifyWhatsApp] bit NOT NULL,
        [NotifyEmail] bit NOT NULL,
        [NotifyInApp] bit NOT NULL,
        [PreferredPaymentMethod] nvarchar(max) NOT NULL,
        [SettingsUpdatedAt] datetime2(0) NULL,
        [SettingsUpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Apartments] PRIMARY KEY ([BuildingId], [Id]),
        CONSTRAINT [FK_Apartments_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Apartments_Floors_BuildingId_FloorId] FOREIGN KEY ([BuildingId], [FloorId]) REFERENCES [Floors] ([BuildingId], [Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [PollOptions] (
        [BuildingId] nvarchar(450) NOT NULL,
        [PollId] nvarchar(450) NOT NULL,
        [Id] nvarchar(450) NOT NULL,
        [Text] nvarchar(max) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_PollOptions] PRIMARY KEY ([BuildingId], [PollId], [Id]),
        CONSTRAINT [FK_PollOptions_Polls_BuildingId_PollId] FOREIGN KEY ([BuildingId], [PollId]) REFERENCES [Polls] ([BuildingId], [Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [Deposits] (
        [BuildingId] nvarchar(450) NOT NULL,
        [Id] nvarchar(450) NOT NULL,
        [MonthKey] nvarchar(max) NOT NULL,
        [Number] nvarchar(max) NOT NULL,
        [ApartmentId] nvarchar(450) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Note] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [ReceiptUrl] nvarchar(max) NULL,
        [ReceiptPath] nvarchar(max) NULL,
        [ReceiptFileName] nvarchar(max) NULL,
        [ReceiptFileSize] bigint NULL,
        [ReceiptFileType] nvarchar(max) NULL,
        [ReceiptSuccess] bit NULL,
        [CreatedAt] datetime2(0) NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ConfirmedAt] datetime2(0) NULL,
        [ConfirmedBy] nvarchar(max) NULL,
        [CancelledAt] datetime2(0) NULL,
        [CancelledBy] nvarchar(max) NULL,
        [CancelledReason] nvarchar(max) NULL,
        [UpdatedAt] datetime2(0) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [UpdateReason] nvarchar(max) NULL,
        CONSTRAINT [PK_Deposits] PRIMARY KEY ([BuildingId], [Id]),
        CONSTRAINT [FK_Deposits_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
        CONSTRAINT [FK_Deposits_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [MonthlyApartmentShares] (
        [BuildingId] nvarchar(450) NOT NULL,
        [MonthKey] nvarchar(450) NOT NULL,
        [ApartmentId] nvarchar(450) NOT NULL,
        [ExpenseShare] decimal(18,2) NOT NULL,
        [RevenueShare] decimal(18,2) NOT NULL,
        [CarryOver] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_MonthlyApartmentShares] PRIMARY KEY ([BuildingId], [MonthKey], [ApartmentId]),
        CONSTRAINT [FK_MonthlyApartmentShares_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
        CONSTRAINT [FK_MonthlyApartmentShares_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [Notifications] (
        [BuildingId] nvarchar(450) NOT NULL,
        [Id] nvarchar(450) NOT NULL,
        [Ts] datetime2(0) NOT NULL,
        [RecipientType] nvarchar(max) NOT NULL,
        [RecipientApartmentId] nvarchar(450) NULL,
        [Type] nvarchar(max) NOT NULL,
        [Icon] nvarchar(max) NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [IsRead] bit NOT NULL,
        [ReadAt] datetime2(0) NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([BuildingId], [Id]),
        CONSTRAINT [FK_Notifications_Apartments_BuildingId_RecipientApartmentId] FOREIGN KEY ([BuildingId], [RecipientApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
        CONSTRAINT [FK_Notifications_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [QrTokens] (
        [Uid] nvarchar(450) NOT NULL,
        [BuildingId] nvarchar(450) NOT NULL,
        [ApartmentId] nvarchar(450) NOT NULL,
        [ExpiresAt] datetime2(3) NOT NULL,
        [UseType] nvarchar(max) NOT NULL,
        [IssuedAt] datetime2(3) NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [IsUsed] bit NOT NULL,
        [UsedAt] datetime2(0) NULL,
        [UsedBy] nvarchar(max) NULL,
        [CreatedAt] datetime2(0) NOT NULL,
        CONSTRAINT [PK_QrTokens] PRIMARY KEY ([Uid]),
        CONSTRAINT [FK_QrTokens_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
        CONSTRAINT [FK_QrTokens_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [Residents] (
        [Id] nvarchar(450) NOT NULL,
        [Uid] nvarchar(450) NOT NULL,
        [BuildingId] nvarchar(450) NOT NULL,
        [ApartmentId] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Phone] nvarchar(450) NOT NULL,
        [Whatsapp] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [PhotoUrl] nvarchar(max) NOT NULL,
        [PinHash] nvarchar(max) NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [IsDisabled] bit NOT NULL,
        [DisabledReason] nvarchar(max) NOT NULL,
        [IsOwner] bit NOT NULL,
        [IsPrimary] bit NOT NULL,
        [LastLoginAt] datetime2(0) NULL,
        [CreatedAt] datetime2(0) NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedAt] datetime2(0) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Residents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Residents_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [WalletAdjustments] (
        [BuildingId] nvarchar(450) NOT NULL,
        [Id] nvarchar(450) NOT NULL,
        [ApartmentId] nvarchar(450) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [MonthKey] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2(0) NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_WalletAdjustments] PRIMARY KEY ([BuildingId], [Id]),
        CONSTRAINT [FK_WalletAdjustments_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
        CONSTRAINT [FK_WalletAdjustments_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [PollVotes] (
        [BuildingId] nvarchar(450) NOT NULL,
        [PollId] nvarchar(450) NOT NULL,
        [ApartmentId] nvarchar(450) NOT NULL,
        [OptionId] nvarchar(450) NOT NULL,
        [VotedAt] datetime2(0) NOT NULL,
        CONSTRAINT [PK_PollVotes] PRIMARY KEY ([BuildingId], [PollId], [ApartmentId]),
        CONSTRAINT [FK_PollVotes_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
        CONSTRAINT [FK_PollVotes_PollOptions_BuildingId_PollId_OptionId] FOREIGN KEY ([BuildingId], [PollId], [OptionId]) REFERENCES [PollOptions] ([BuildingId], [PollId], [Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE TABLE [DepositEditHistory] (
        [EditId] bigint NOT NULL IDENTITY,
        [BuildingId] nvarchar(450) NOT NULL,
        [DepositId] nvarchar(450) NOT NULL,
        [Ts] datetime2(0) NOT NULL,
        [ByUser] nvarchar(max) NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [OldAmount] decimal(18,2) NOT NULL,
        [NewAmount] decimal(18,2) NOT NULL,
        [OldNote] nvarchar(max) NOT NULL,
        [NewNote] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_DepositEditHistory] PRIMARY KEY ([EditId]),
        CONSTRAINT [FK_DepositEditHistory_Deposits_BuildingId_DepositId] FOREIGN KEY ([BuildingId], [DepositId]) REFERENCES [Deposits] ([BuildingId], [Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Apartments_BuildingId_FloorId] ON [Apartments] ([BuildingId], [FloorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Apartments_BuildingId_Number] ON [Apartments] ([BuildingId], [Number]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLog_ApartmentId_Ts] ON [AuditLog] ([ApartmentId], [Ts]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLog_BuildingId_Ts] ON [AuditLog] ([BuildingId], [Ts]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BuildingAdmins_AdminUid] ON [BuildingAdmins] ([AdminUid]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Buildings_BuildingNumber] ON [Buildings] ([BuildingNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Cameras_BuildingId] ON [Cameras] ([BuildingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Cameras_DvrId_Channel] ON [Cameras] ([DvrId], [Channel]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DepositEditHistory_BuildingId_DepositId] ON [DepositEditHistory] ([BuildingId], [DepositId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Deposits_BuildingId_ApartmentId] ON [Deposits] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Dvrs_BuildingId] ON [Dvrs] ([BuildingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Expenses_BuildingId_CategoryKind_CategoryId] ON [Expenses] ([BuildingId], [CategoryKind], [CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MonthlyApartmentShares_BuildingId_ApartmentId] ON [MonthlyApartmentShares] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_BuildingId_RecipientApartmentId] ON [Notifications] ([BuildingId], [RecipientApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PollVotes_BuildingId_ApartmentId] ON [PollVotes] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PollVotes_BuildingId_PollId_OptionId] ON [PollVotes] ([BuildingId], [PollId], [OptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QrTokens_BuildingId_ApartmentId] ON [QrTokens] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QrUsages_CreatedAt] ON [QrUsages] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QrUsages_TokenId] ON [QrUsages] ([TokenId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Residents_ApartmentId] ON [Residents] ([ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Residents_BuildingId] ON [Residents] ([BuildingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Residents_BuildingId_ApartmentId] ON [Residents] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Residents_BuildingId_Phone] ON [Residents] ([BuildingId], [Phone]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Residents_Phone] ON [Residents] ([Phone]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Residents_Uid] ON [Residents] ([Uid]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Revenues_BuildingId_CategoryKind_CategoryId] ON [Revenues] ([BuildingId], [CategoryKind], [CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SyncPendingChanges_Applied_CreatedAt] ON [SyncPendingChanges] ([Applied], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SyncPendingChanges_EntityType_EntityId] ON [SyncPendingChanges] ([EntityType], [EntityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WalletAdjustments_BuildingId_ApartmentId] ON [WalletAdjustments] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116120818_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261116120818_InitialCreate', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    SET NOCOUNT ON;
    DECLARE @constraintName sysname;
    DECLARE @matches int;
    DECLARE @sql nvarchar(max);
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Apartments -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Apartments] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Floors') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'FloorId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Apartments -> dbo.Floors.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Apartments] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.AuditLog') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 2 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.AuditLog -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[AuditLog] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.BuildingAdmins') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.BuildingAdmins -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[BuildingAdmins] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.BuildingAdmins') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Users') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'AdminUid' AND rc.name=N'Uid');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.BuildingAdmins -> dbo.Users.', 1;
    SET @sql = N'ALTER TABLE [dbo].[BuildingAdmins] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.BuildingSettings') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.BuildingSettings -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[BuildingSettings] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Cameras') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Dvrs') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'DvrId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Cameras -> dbo.Dvrs.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Cameras] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.DepositEditHistory') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Deposits') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'DepositId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.DepositEditHistory -> dbo.Deposits.', 1;
    SET @sql = N'ALTER TABLE [dbo].[DepositEditHistory] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Deposits') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'ApartmentId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Deposits -> dbo.Apartments.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Deposits] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Deposits') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Deposits -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Deposits] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Dvrs') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Dvrs -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Dvrs] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Expenses') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Expenses -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Expenses] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Expenses') AND fk.referenced_object_id = OBJECT_ID(N'dbo.FinancialCategories') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 3 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'CategoryKind' AND rc.name=N'Kind') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=3 AND pc.name=N'CategoryId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Expenses -> dbo.FinancialCategories.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Expenses] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.FinancialCategories') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.FinancialCategories -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[FinancialCategories] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Floors') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Floors -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Floors] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.MaintenanceRecords') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.MaintenanceRecords -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[MaintenanceRecords] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.MonthlyApartmentShares') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'ApartmentId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.MonthlyApartmentShares -> dbo.Apartments.', 1;
    SET @sql = N'ALTER TABLE [dbo].[MonthlyApartmentShares] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.MonthlyApartmentShares') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.MonthlyApartmentShares -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[MonthlyApartmentShares] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Notifications') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'RecipientApartmentId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Notifications -> dbo.Apartments.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Notifications] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Notifications') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Notifications -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Notifications] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.PollOptions') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Polls') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'PollId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.PollOptions -> dbo.Polls.', 1;
    SET @sql = N'ALTER TABLE [dbo].[PollOptions] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Polls') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Polls -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Polls] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.PollVotes') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'ApartmentId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.PollVotes -> dbo.Apartments.', 1;
    SET @sql = N'ALTER TABLE [dbo].[PollVotes] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.PollVotes') AND fk.referenced_object_id = OBJECT_ID(N'dbo.PollOptions') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 3 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'PollId' AND rc.name=N'PollId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=3 AND pc.name=N'OptionId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.PollVotes -> dbo.PollOptions.', 1;
    SET @sql = N'ALTER TABLE [dbo].[PollVotes] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.QrTokens') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'ApartmentId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.QrTokens -> dbo.Apartments.', 1;
    SET @sql = N'ALTER TABLE [dbo].[QrTokens] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.QrTokens') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.QrTokens -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[QrTokens] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Residents') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'ApartmentId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Residents -> dbo.Apartments.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Residents] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Revenues') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Revenues -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Revenues] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Revenues') AND fk.referenced_object_id = OBJECT_ID(N'dbo.FinancialCategories') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 3 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'CategoryKind' AND rc.name=N'Kind') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=3 AND pc.name=N'CategoryId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Revenues -> dbo.FinancialCategories.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Revenues] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.UserPermissions') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Users') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'Uid' AND rc.name=N'Uid');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.UserPermissions -> dbo.Users.', 1;
    SET @sql = N'ALTER TABLE [dbo].[UserPermissions] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.WalletAdjustments') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'ApartmentId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.WalletAdjustments -> dbo.Apartments.', 1;
    SET @sql = N'ALTER TABLE [dbo].[WalletAdjustments] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.WalletAdjustments') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.WalletAdjustments -> dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[WalletAdjustments] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    -- Drop only the 26 primary keys whose key columns change.
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Apartments') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Apartments.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Apartments] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.BackupHistory') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.BackupHistory.', 1;
    SET @sql = N'ALTER TABLE [dbo].[BackupHistory] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.BuildingAdmins') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'AdminUid');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.BuildingAdmins.', 1;
    SET @sql = N'ALTER TABLE [dbo].[BuildingAdmins] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Buildings') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Buildings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Buildings] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.BuildingSettings') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.BuildingSettings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[BuildingSettings] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Cameras') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Cameras.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Cameras] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Deposits') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Deposits.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Deposits] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Dvrs') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Dvrs.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Dvrs] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Expenses') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Expenses.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Expenses] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.FinancialCategories') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=3 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Kind') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=3 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.FinancialCategories.', 1;
    SET @sql = N'ALTER TABLE [dbo].[FinancialCategories] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Floors') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Floors.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Floors] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.GlobalSettings') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.GlobalSettings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[GlobalSettings] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.MaintenanceRecords') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.MaintenanceRecords.', 1;
    SET @sql = N'ALTER TABLE [dbo].[MaintenanceRecords] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.MonthlyApartmentShares') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=3 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'MonthKey') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=3 AND c.name=N'ApartmentId');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.MonthlyApartmentShares.', 1;
    SET @sql = N'ALTER TABLE [dbo].[MonthlyApartmentShares] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Notifications') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Notifications.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Notifications] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.PollOptions') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=3 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'PollId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=3 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.PollOptions.', 1;
    SET @sql = N'ALTER TABLE [dbo].[PollOptions] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Polls') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Polls.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Polls] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.PollVotes') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=3 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'PollId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=3 AND c.name=N'ApartmentId');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.PollVotes.', 1;
    SET @sql = N'ALTER TABLE [dbo].[PollVotes] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Presence') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Uid');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Presence.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Presence] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.QrTokens') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Uid');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.QrTokens.', 1;
    SET @sql = N'ALTER TABLE [dbo].[QrTokens] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Residents') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Residents.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Residents] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Revenues') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Revenues.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Revenues] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.ScheduledBackupSettings') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.ScheduledBackupSettings.', 1;
    SET @sql = N'ALTER TABLE [dbo].[ScheduledBackupSettings] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.UserPermissions') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Uid') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Permission');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.UserPermissions.', 1;
    SET @sql = N'ALTER TABLE [dbo].[UserPermissions] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Users') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Uid');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Users.', 1;
    SET @sql = N'ALTER TABLE [dbo].[Users] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
    SET @constraintName = NULL; SET @matches = 0;
    SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.WalletAdjustments') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
    IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.WalletAdjustments.', 1;
    SET @sql = N'ALTER TABLE [dbo].[WalletAdjustments] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[WalletAdjustments]') AND [c].[name] = N'MonthKey');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [WalletAdjustments] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [WalletAdjustments] ALTER COLUMN [MonthKey] char(7) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_WalletAdjustments_BuildingId_ApartmentId] ON [WalletAdjustments];
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[WalletAdjustments]') AND [c].[name] = N'ApartmentId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [WalletAdjustments] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [WalletAdjustments] ALTER COLUMN [ApartmentId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_WalletAdjustments_BuildingId_ApartmentId] ON [WalletAdjustments] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[WalletAdjustments]') AND [c].[name] = N'Id');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [WalletAdjustments] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [WalletAdjustments] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_WalletAdjustments_BuildingId_ApartmentId] ON [WalletAdjustments];
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[WalletAdjustments]') AND [c].[name] = N'BuildingId');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [WalletAdjustments] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [WalletAdjustments] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_WalletAdjustments_BuildingId_ApartmentId] ON [WalletAdjustments] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Uid');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [Users] ALTER COLUMN [Uid] nvarchar(128) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserPermissions]') AND [c].[name] = N'Permission');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [UserPermissions] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [UserPermissions] ALTER COLUMN [Permission] nvarchar(60) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserPermissions]') AND [c].[name] = N'Uid');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [UserPermissions] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [UserPermissions] ALTER COLUMN [Uid] nvarchar(128) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ScheduledBackupSettings]') AND [c].[name] = N'Id');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [ScheduledBackupSettings] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [ScheduledBackupSettings] ALTER COLUMN [Id] nvarchar(20) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Revenues]') AND [c].[name] = N'MonthKey');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Revenues] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [Revenues] ALTER COLUMN [MonthKey] char(7) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Revenues_BuildingId_CategoryKind_CategoryId] ON [Revenues];
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Revenues]') AND [c].[name] = N'CategoryKind');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Revenues] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [Revenues] ALTER COLUMN [CategoryKind] nvarchar(10) NOT NULL;
    CREATE INDEX [IX_Revenues_BuildingId_CategoryKind_CategoryId] ON [Revenues] ([BuildingId], [CategoryKind], [CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Revenues_BuildingId_CategoryKind_CategoryId] ON [Revenues];
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Revenues]') AND [c].[name] = N'CategoryId');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Revenues] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [Revenues] ALTER COLUMN [CategoryId] nvarchar(64) NULL;
    CREATE INDEX [IX_Revenues_BuildingId_CategoryKind_CategoryId] ON [Revenues] ([BuildingId], [CategoryKind], [CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Revenues]') AND [c].[name] = N'Id');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Revenues] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [Revenues] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Revenues_BuildingId_CategoryKind_CategoryId] ON [Revenues];
    DECLARE @var12 sysname;
    SELECT @var12 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Revenues]') AND [c].[name] = N'BuildingId');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Revenues] DROP CONSTRAINT [' + @var12 + '];');
    ALTER TABLE [Revenues] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_Revenues_BuildingId_CategoryKind_CategoryId] ON [Revenues] ([BuildingId], [CategoryKind], [CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Residents_Uid] ON [Residents];
    DECLARE @var13 sysname;
    SELECT @var13 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Residents]') AND [c].[name] = N'Uid');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Residents] DROP CONSTRAINT [' + @var13 + '];');
    ALTER TABLE [Residents] ALTER COLUMN [Uid] nvarchar(128) NOT NULL;
    CREATE UNIQUE INDEX [IX_Residents_Uid] ON [Residents] ([Uid]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Residents_BuildingId] ON [Residents];
    DROP INDEX [IX_Residents_BuildingId_ApartmentId] ON [Residents];
    DROP INDEX [IX_Residents_BuildingId_Phone] ON [Residents];
    DECLARE @var14 sysname;
    SELECT @var14 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Residents]') AND [c].[name] = N'BuildingId');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Residents] DROP CONSTRAINT [' + @var14 + '];');
    ALTER TABLE [Residents] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_Residents_BuildingId] ON [Residents] ([BuildingId]);
    CREATE INDEX [IX_Residents_BuildingId_ApartmentId] ON [Residents] ([BuildingId], [ApartmentId]);
    CREATE INDEX [IX_Residents_BuildingId_Phone] ON [Residents] ([BuildingId], [Phone]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Residents_ApartmentId] ON [Residents];
    DROP INDEX [IX_Residents_BuildingId_ApartmentId] ON [Residents];
    DECLARE @var15 sysname;
    SELECT @var15 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Residents]') AND [c].[name] = N'ApartmentId');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Residents] DROP CONSTRAINT [' + @var15 + '];');
    ALTER TABLE [Residents] ALTER COLUMN [ApartmentId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_Residents_ApartmentId] ON [Residents] ([ApartmentId]);
    CREATE INDEX [IX_Residents_BuildingId_ApartmentId] ON [Residents] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var16 sysname;
    SELECT @var16 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Residents]') AND [c].[name] = N'Id');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Residents] DROP CONSTRAINT [' + @var16 + '];');
    ALTER TABLE [Residents] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_QrTokens_BuildingId_ApartmentId] ON [QrTokens];
    DECLARE @var17 sysname;
    SELECT @var17 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QrTokens]') AND [c].[name] = N'BuildingId');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [QrTokens] DROP CONSTRAINT [' + @var17 + '];');
    ALTER TABLE [QrTokens] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_QrTokens_BuildingId_ApartmentId] ON [QrTokens] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_QrTokens_BuildingId_ApartmentId] ON [QrTokens];
    DECLARE @var18 sysname;
    SELECT @var18 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QrTokens]') AND [c].[name] = N'ApartmentId');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [QrTokens] DROP CONSTRAINT [' + @var18 + '];');
    ALTER TABLE [QrTokens] ALTER COLUMN [ApartmentId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_QrTokens_BuildingId_ApartmentId] ON [QrTokens] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var19 sysname;
    SELECT @var19 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QrTokens]') AND [c].[name] = N'Uid');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [QrTokens] DROP CONSTRAINT [' + @var19 + '];');
    ALTER TABLE [QrTokens] ALTER COLUMN [Uid] nvarchar(128) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var20 sysname;
    SELECT @var20 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Presence]') AND [c].[name] = N'BuildingId');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [Presence] DROP CONSTRAINT [' + @var20 + '];');
    ALTER TABLE [Presence] ALTER COLUMN [BuildingId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var21 sysname;
    SELECT @var21 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Presence]') AND [c].[name] = N'ApartmentId');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [Presence] DROP CONSTRAINT [' + @var21 + '];');
    ALTER TABLE [Presence] ALTER COLUMN [ApartmentId] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var22 sysname;
    SELECT @var22 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Presence]') AND [c].[name] = N'Uid');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [Presence] DROP CONSTRAINT [' + @var22 + '];');
    ALTER TABLE [Presence] ALTER COLUMN [Uid] nvarchar(128) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_PollVotes_BuildingId_PollId_OptionId] ON [PollVotes];
    DECLARE @var23 sysname;
    SELECT @var23 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PollVotes]') AND [c].[name] = N'OptionId');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [PollVotes] DROP CONSTRAINT [' + @var23 + '];');
    ALTER TABLE [PollVotes] ALTER COLUMN [OptionId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_PollVotes_BuildingId_PollId_OptionId] ON [PollVotes] ([BuildingId], [PollId], [OptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_PollVotes_BuildingId_ApartmentId] ON [PollVotes];
    DECLARE @var24 sysname;
    SELECT @var24 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PollVotes]') AND [c].[name] = N'ApartmentId');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [PollVotes] DROP CONSTRAINT [' + @var24 + '];');
    ALTER TABLE [PollVotes] ALTER COLUMN [ApartmentId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_PollVotes_BuildingId_ApartmentId] ON [PollVotes] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_PollVotes_BuildingId_PollId_OptionId] ON [PollVotes];
    DECLARE @var25 sysname;
    SELECT @var25 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PollVotes]') AND [c].[name] = N'PollId');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [PollVotes] DROP CONSTRAINT [' + @var25 + '];');
    ALTER TABLE [PollVotes] ALTER COLUMN [PollId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_PollVotes_BuildingId_PollId_OptionId] ON [PollVotes] ([BuildingId], [PollId], [OptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_PollVotes_BuildingId_ApartmentId] ON [PollVotes];
    DROP INDEX [IX_PollVotes_BuildingId_PollId_OptionId] ON [PollVotes];
    DECLARE @var26 sysname;
    SELECT @var26 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PollVotes]') AND [c].[name] = N'BuildingId');
    IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [PollVotes] DROP CONSTRAINT [' + @var26 + '];');
    ALTER TABLE [PollVotes] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_PollVotes_BuildingId_ApartmentId] ON [PollVotes] ([BuildingId], [ApartmentId]);
    CREATE INDEX [IX_PollVotes_BuildingId_PollId_OptionId] ON [PollVotes] ([BuildingId], [PollId], [OptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var27 sysname;
    SELECT @var27 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Polls]') AND [c].[name] = N'Id');
    IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [Polls] DROP CONSTRAINT [' + @var27 + '];');
    ALTER TABLE [Polls] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var28 sysname;
    SELECT @var28 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Polls]') AND [c].[name] = N'BuildingId');
    IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [Polls] DROP CONSTRAINT [' + @var28 + '];');
    ALTER TABLE [Polls] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var29 sysname;
    SELECT @var29 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PollOptions]') AND [c].[name] = N'Id');
    IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [PollOptions] DROP CONSTRAINT [' + @var29 + '];');
    ALTER TABLE [PollOptions] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var30 sysname;
    SELECT @var30 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PollOptions]') AND [c].[name] = N'PollId');
    IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [PollOptions] DROP CONSTRAINT [' + @var30 + '];');
    ALTER TABLE [PollOptions] ALTER COLUMN [PollId] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var31 sysname;
    SELECT @var31 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PollOptions]') AND [c].[name] = N'BuildingId');
    IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [PollOptions] DROP CONSTRAINT [' + @var31 + '];');
    ALTER TABLE [PollOptions] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Notifications_BuildingId_RecipientApartmentId] ON [Notifications];
    DECLARE @var32 sysname;
    SELECT @var32 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'RecipientApartmentId');
    IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var32 + '];');
    ALTER TABLE [Notifications] ALTER COLUMN [RecipientApartmentId] nvarchar(64) NULL;
    CREATE INDEX [IX_Notifications_BuildingId_RecipientApartmentId] ON [Notifications] ([BuildingId], [RecipientApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var33 sysname;
    SELECT @var33 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'Id');
    IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var33 + '];');
    ALTER TABLE [Notifications] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Notifications_BuildingId_RecipientApartmentId] ON [Notifications];
    DECLARE @var34 sysname;
    SELECT @var34 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'BuildingId');
    IF @var34 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var34 + '];');
    ALTER TABLE [Notifications] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_Notifications_BuildingId_RecipientApartmentId] ON [Notifications] ([BuildingId], [RecipientApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_MonthlyApartmentShares_BuildingId_ApartmentId] ON [MonthlyApartmentShares];
    DECLARE @var35 sysname;
    SELECT @var35 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonthlyApartmentShares]') AND [c].[name] = N'ApartmentId');
    IF @var35 IS NOT NULL EXEC(N'ALTER TABLE [MonthlyApartmentShares] DROP CONSTRAINT [' + @var35 + '];');
    ALTER TABLE [MonthlyApartmentShares] ALTER COLUMN [ApartmentId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_MonthlyApartmentShares_BuildingId_ApartmentId] ON [MonthlyApartmentShares] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var36 sysname;
    SELECT @var36 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonthlyApartmentShares]') AND [c].[name] = N'MonthKey');
    IF @var36 IS NOT NULL EXEC(N'ALTER TABLE [MonthlyApartmentShares] DROP CONSTRAINT [' + @var36 + '];');
    ALTER TABLE [MonthlyApartmentShares] ALTER COLUMN [MonthKey] char(7) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_MonthlyApartmentShares_BuildingId_ApartmentId] ON [MonthlyApartmentShares];
    DECLARE @var37 sysname;
    SELECT @var37 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonthlyApartmentShares]') AND [c].[name] = N'BuildingId');
    IF @var37 IS NOT NULL EXEC(N'ALTER TABLE [MonthlyApartmentShares] DROP CONSTRAINT [' + @var37 + '];');
    ALTER TABLE [MonthlyApartmentShares] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_MonthlyApartmentShares_BuildingId_ApartmentId] ON [MonthlyApartmentShares] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var38 sysname;
    SELECT @var38 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MaintenanceRecords]') AND [c].[name] = N'Id');
    IF @var38 IS NOT NULL EXEC(N'ALTER TABLE [MaintenanceRecords] DROP CONSTRAINT [' + @var38 + '];');
    ALTER TABLE [MaintenanceRecords] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var39 sysname;
    SELECT @var39 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MaintenanceRecords]') AND [c].[name] = N'BuildingId');
    IF @var39 IS NOT NULL EXEC(N'ALTER TABLE [MaintenanceRecords] DROP CONSTRAINT [' + @var39 + '];');
    ALTER TABLE [MaintenanceRecords] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var40 sysname;
    SELECT @var40 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[GlobalSettings]') AND [c].[name] = N'Id');
    IF @var40 IS NOT NULL EXEC(N'ALTER TABLE [GlobalSettings] DROP CONSTRAINT [' + @var40 + '];');
    ALTER TABLE [GlobalSettings] ALTER COLUMN [Id] nvarchar(20) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var41 sysname;
    SELECT @var41 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Floors]') AND [c].[name] = N'Id');
    IF @var41 IS NOT NULL EXEC(N'ALTER TABLE [Floors] DROP CONSTRAINT [' + @var41 + '];');
    ALTER TABLE [Floors] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var42 sysname;
    SELECT @var42 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Floors]') AND [c].[name] = N'BuildingId');
    IF @var42 IS NOT NULL EXEC(N'ALTER TABLE [Floors] DROP CONSTRAINT [' + @var42 + '];');
    ALTER TABLE [Floors] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var43 sysname;
    SELECT @var43 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FinancialCategories]') AND [c].[name] = N'Id');
    IF @var43 IS NOT NULL EXEC(N'ALTER TABLE [FinancialCategories] DROP CONSTRAINT [' + @var43 + '];');
    ALTER TABLE [FinancialCategories] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var44 sysname;
    SELECT @var44 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FinancialCategories]') AND [c].[name] = N'Kind');
    IF @var44 IS NOT NULL EXEC(N'ALTER TABLE [FinancialCategories] DROP CONSTRAINT [' + @var44 + '];');
    ALTER TABLE [FinancialCategories] ALTER COLUMN [Kind] nvarchar(10) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var45 sysname;
    SELECT @var45 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FinancialCategories]') AND [c].[name] = N'BuildingId');
    IF @var45 IS NOT NULL EXEC(N'ALTER TABLE [FinancialCategories] DROP CONSTRAINT [' + @var45 + '];');
    ALTER TABLE [FinancialCategories] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var46 sysname;
    SELECT @var46 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Expenses]') AND [c].[name] = N'MonthKey');
    IF @var46 IS NOT NULL EXEC(N'ALTER TABLE [Expenses] DROP CONSTRAINT [' + @var46 + '];');
    ALTER TABLE [Expenses] ALTER COLUMN [MonthKey] char(7) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Expenses_BuildingId_CategoryKind_CategoryId] ON [Expenses];
    DECLARE @var47 sysname;
    SELECT @var47 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Expenses]') AND [c].[name] = N'CategoryKind');
    IF @var47 IS NOT NULL EXEC(N'ALTER TABLE [Expenses] DROP CONSTRAINT [' + @var47 + '];');
    ALTER TABLE [Expenses] ALTER COLUMN [CategoryKind] nvarchar(10) NOT NULL;
    CREATE INDEX [IX_Expenses_BuildingId_CategoryKind_CategoryId] ON [Expenses] ([BuildingId], [CategoryKind], [CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Expenses_BuildingId_CategoryKind_CategoryId] ON [Expenses];
    DECLARE @var48 sysname;
    SELECT @var48 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Expenses]') AND [c].[name] = N'CategoryId');
    IF @var48 IS NOT NULL EXEC(N'ALTER TABLE [Expenses] DROP CONSTRAINT [' + @var48 + '];');
    ALTER TABLE [Expenses] ALTER COLUMN [CategoryId] nvarchar(64) NULL;
    CREATE INDEX [IX_Expenses_BuildingId_CategoryKind_CategoryId] ON [Expenses] ([BuildingId], [CategoryKind], [CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var49 sysname;
    SELECT @var49 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Expenses]') AND [c].[name] = N'Id');
    IF @var49 IS NOT NULL EXEC(N'ALTER TABLE [Expenses] DROP CONSTRAINT [' + @var49 + '];');
    ALTER TABLE [Expenses] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Expenses_BuildingId_CategoryKind_CategoryId] ON [Expenses];
    DECLARE @var50 sysname;
    SELECT @var50 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Expenses]') AND [c].[name] = N'BuildingId');
    IF @var50 IS NOT NULL EXEC(N'ALTER TABLE [Expenses] DROP CONSTRAINT [' + @var50 + '];');
    ALTER TABLE [Expenses] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_Expenses_BuildingId_CategoryKind_CategoryId] ON [Expenses] ([BuildingId], [CategoryKind], [CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Dvrs_BuildingId] ON [Dvrs];
    DECLARE @var51 sysname;
    SELECT @var51 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Dvrs]') AND [c].[name] = N'BuildingId');
    IF @var51 IS NOT NULL EXEC(N'ALTER TABLE [Dvrs] DROP CONSTRAINT [' + @var51 + '];');
    ALTER TABLE [Dvrs] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_Dvrs_BuildingId] ON [Dvrs] ([BuildingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var52 sysname;
    SELECT @var52 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Dvrs]') AND [c].[name] = N'Id');
    IF @var52 IS NOT NULL EXEC(N'ALTER TABLE [Dvrs] DROP CONSTRAINT [' + @var52 + '];');
    ALTER TABLE [Dvrs] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var53 sysname;
    SELECT @var53 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Deposits]') AND [c].[name] = N'MonthKey');
    IF @var53 IS NOT NULL EXEC(N'ALTER TABLE [Deposits] DROP CONSTRAINT [' + @var53 + '];');
    ALTER TABLE [Deposits] ALTER COLUMN [MonthKey] char(7) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Deposits_BuildingId_ApartmentId] ON [Deposits];
    DECLARE @var54 sysname;
    SELECT @var54 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Deposits]') AND [c].[name] = N'ApartmentId');
    IF @var54 IS NOT NULL EXEC(N'ALTER TABLE [Deposits] DROP CONSTRAINT [' + @var54 + '];');
    ALTER TABLE [Deposits] ALTER COLUMN [ApartmentId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_Deposits_BuildingId_ApartmentId] ON [Deposits] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var55 sysname;
    SELECT @var55 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Deposits]') AND [c].[name] = N'Id');
    IF @var55 IS NOT NULL EXEC(N'ALTER TABLE [Deposits] DROP CONSTRAINT [' + @var55 + '];');
    ALTER TABLE [Deposits] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Deposits_BuildingId_ApartmentId] ON [Deposits];
    DECLARE @var56 sysname;
    SELECT @var56 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Deposits]') AND [c].[name] = N'BuildingId');
    IF @var56 IS NOT NULL EXEC(N'ALTER TABLE [Deposits] DROP CONSTRAINT [' + @var56 + '];');
    ALTER TABLE [Deposits] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_Deposits_BuildingId_ApartmentId] ON [Deposits] ([BuildingId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_DepositEditHistory_BuildingId_DepositId] ON [DepositEditHistory];
    DECLARE @var57 sysname;
    SELECT @var57 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DepositEditHistory]') AND [c].[name] = N'DepositId');
    IF @var57 IS NOT NULL EXEC(N'ALTER TABLE [DepositEditHistory] DROP CONSTRAINT [' + @var57 + '];');
    ALTER TABLE [DepositEditHistory] ALTER COLUMN [DepositId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_DepositEditHistory_BuildingId_DepositId] ON [DepositEditHistory] ([BuildingId], [DepositId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_DepositEditHistory_BuildingId_DepositId] ON [DepositEditHistory];
    DECLARE @var58 sysname;
    SELECT @var58 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DepositEditHistory]') AND [c].[name] = N'BuildingId');
    IF @var58 IS NOT NULL EXEC(N'ALTER TABLE [DepositEditHistory] DROP CONSTRAINT [' + @var58 + '];');
    ALTER TABLE [DepositEditHistory] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_DepositEditHistory_BuildingId_DepositId] ON [DepositEditHistory] ([BuildingId], [DepositId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Cameras_DvrId_Channel] ON [Cameras];
    DECLARE @var59 sysname;
    SELECT @var59 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Cameras]') AND [c].[name] = N'DvrId');
    IF @var59 IS NOT NULL EXEC(N'ALTER TABLE [Cameras] DROP CONSTRAINT [' + @var59 + '];');
    ALTER TABLE [Cameras] ALTER COLUMN [DvrId] nvarchar(64) NOT NULL;
    CREATE UNIQUE INDEX [IX_Cameras_DvrId_Channel] ON [Cameras] ([DvrId], [Channel]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Cameras_BuildingId] ON [Cameras];
    DECLARE @var60 sysname;
    SELECT @var60 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Cameras]') AND [c].[name] = N'BuildingId');
    IF @var60 IS NOT NULL EXEC(N'ALTER TABLE [Cameras] DROP CONSTRAINT [' + @var60 + '];');
    ALTER TABLE [Cameras] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_Cameras_BuildingId] ON [Cameras] ([BuildingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var61 sysname;
    SELECT @var61 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Cameras]') AND [c].[name] = N'Id');
    IF @var61 IS NOT NULL EXEC(N'ALTER TABLE [Cameras] DROP CONSTRAINT [' + @var61 + '];');
    ALTER TABLE [Cameras] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var62 sysname;
    SELECT @var62 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BuildingSettings]') AND [c].[name] = N'BuildingId');
    IF @var62 IS NOT NULL EXEC(N'ALTER TABLE [BuildingSettings] DROP CONSTRAINT [' + @var62 + '];');
    ALTER TABLE [BuildingSettings] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var63 sysname;
    SELECT @var63 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Buildings]') AND [c].[name] = N'Id');
    IF @var63 IS NOT NULL EXEC(N'ALTER TABLE [Buildings] DROP CONSTRAINT [' + @var63 + '];');
    ALTER TABLE [Buildings] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_BuildingAdmins_AdminUid] ON [BuildingAdmins];
    DECLARE @var64 sysname;
    SELECT @var64 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BuildingAdmins]') AND [c].[name] = N'AdminUid');
    IF @var64 IS NOT NULL EXEC(N'ALTER TABLE [BuildingAdmins] DROP CONSTRAINT [' + @var64 + '];');
    ALTER TABLE [BuildingAdmins] ALTER COLUMN [AdminUid] nvarchar(128) NOT NULL;
    CREATE INDEX [IX_BuildingAdmins_AdminUid] ON [BuildingAdmins] ([AdminUid]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var65 sysname;
    SELECT @var65 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BuildingAdmins]') AND [c].[name] = N'BuildingId');
    IF @var65 IS NOT NULL EXEC(N'ALTER TABLE [BuildingAdmins] DROP CONSTRAINT [' + @var65 + '];');
    ALTER TABLE [BuildingAdmins] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var66 sysname;
    SELECT @var66 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BackupHistory]') AND [c].[name] = N'Id');
    IF @var66 IS NOT NULL EXEC(N'ALTER TABLE [BackupHistory] DROP CONSTRAINT [' + @var66 + '];');
    ALTER TABLE [BackupHistory] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var67 sysname;
    SELECT @var67 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLog]') AND [c].[name] = N'MonthKey');
    IF @var67 IS NOT NULL EXEC(N'ALTER TABLE [AuditLog] DROP CONSTRAINT [' + @var67 + '];');
    ALTER TABLE [AuditLog] ALTER COLUMN [MonthKey] nvarchar(7) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_AuditLog_BuildingId_Ts] ON [AuditLog];
    DECLARE @var68 sysname;
    SELECT @var68 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLog]') AND [c].[name] = N'BuildingId');
    IF @var68 IS NOT NULL EXEC(N'ALTER TABLE [AuditLog] DROP CONSTRAINT [' + @var68 + '];');
    ALTER TABLE [AuditLog] ALTER COLUMN [BuildingId] nvarchar(64) NULL;
    CREATE INDEX [IX_AuditLog_BuildingId_Ts] ON [AuditLog] ([BuildingId], [Ts]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Apartments_BuildingId_FloorId] ON [Apartments];
    DECLARE @var69 sysname;
    SELECT @var69 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Apartments]') AND [c].[name] = N'FloorId');
    IF @var69 IS NOT NULL EXEC(N'ALTER TABLE [Apartments] DROP CONSTRAINT [' + @var69 + '];');
    ALTER TABLE [Apartments] ALTER COLUMN [FloorId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_Apartments_BuildingId_FloorId] ON [Apartments] ([BuildingId], [FloorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DECLARE @var70 sysname;
    SELECT @var70 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Apartments]') AND [c].[name] = N'Id');
    IF @var70 IS NOT NULL EXEC(N'ALTER TABLE [Apartments] DROP CONSTRAINT [' + @var70 + '];');
    ALTER TABLE [Apartments] ALTER COLUMN [Id] nvarchar(64) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    DROP INDEX [IX_Apartments_BuildingId_FloorId] ON [Apartments];
    DROP INDEX [IX_Apartments_BuildingId_Number] ON [Apartments];
    DECLARE @var71 sysname;
    SELECT @var71 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Apartments]') AND [c].[name] = N'BuildingId');
    IF @var71 IS NOT NULL EXEC(N'ALTER TABLE [Apartments] DROP CONSTRAINT [' + @var71 + '];');
    ALTER TABLE [Apartments] ALTER COLUMN [BuildingId] nvarchar(64) NOT NULL;
    CREATE INDEX [IX_Apartments_BuildingId_FloorId] ON [Apartments] ([BuildingId], [FloorId]);
    CREATE UNIQUE INDEX [IX_Apartments_BuildingId_Number] ON [Apartments] ([BuildingId], [Number]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Apartments] ADD CONSTRAINT [PK_Apartments] PRIMARY KEY ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [BackupHistory] ADD CONSTRAINT [PK_BackupHistory] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [BuildingAdmins] ADD CONSTRAINT [PK_BuildingAdmins] PRIMARY KEY ([BuildingId], [AdminUid]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Buildings] ADD CONSTRAINT [PK_Buildings] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [BuildingSettings] ADD CONSTRAINT [PK_BuildingSettings] PRIMARY KEY ([BuildingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Cameras] ADD CONSTRAINT [PK_Cameras] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Deposits] ADD CONSTRAINT [PK_Deposits] PRIMARY KEY ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Dvrs] ADD CONSTRAINT [PK_Dvrs] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Expenses] ADD CONSTRAINT [PK_Expenses] PRIMARY KEY ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [FinancialCategories] ADD CONSTRAINT [PK_FinancialCategories] PRIMARY KEY ([BuildingId], [Kind], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Floors] ADD CONSTRAINT [PK_Floors] PRIMARY KEY ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [GlobalSettings] ADD CONSTRAINT [PK_GlobalSettings] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [MaintenanceRecords] ADD CONSTRAINT [PK_MaintenanceRecords] PRIMARY KEY ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [MonthlyApartmentShares] ADD CONSTRAINT [PK_MonthlyApartmentShares] PRIMARY KEY ([BuildingId], [MonthKey], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Notifications] ADD CONSTRAINT [PK_Notifications] PRIMARY KEY ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [PollOptions] ADD CONSTRAINT [PK_PollOptions] PRIMARY KEY ([BuildingId], [PollId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Polls] ADD CONSTRAINT [PK_Polls] PRIMARY KEY ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [PollVotes] ADD CONSTRAINT [PK_PollVotes] PRIMARY KEY ([BuildingId], [PollId], [ApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Presence] ADD CONSTRAINT [PK_Presence] PRIMARY KEY ([Uid]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [QrTokens] ADD CONSTRAINT [PK_QrTokens] PRIMARY KEY ([Uid]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Residents] ADD CONSTRAINT [PK_Residents] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Revenues] ADD CONSTRAINT [PK_Revenues] PRIMARY KEY ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [ScheduledBackupSettings] ADD CONSTRAINT [PK_ScheduledBackupSettings] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [UserPermissions] ADD CONSTRAINT [PK_UserPermissions] PRIMARY KEY ([Uid], [Permission]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [PK_Users] PRIMARY KEY ([Uid]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [WalletAdjustments] ADD CONSTRAINT [PK_WalletAdjustments] PRIMARY KEY ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Apartments] ADD CONSTRAINT [FK_Apartments_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Apartments] ADD CONSTRAINT [FK_Apartments_Floors_BuildingId_FloorId] FOREIGN KEY ([BuildingId], [FloorId]) REFERENCES [Floors] ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [AuditLog] ADD CONSTRAINT [FK_AuditLog_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [BuildingAdmins] ADD CONSTRAINT [FK_BuildingAdmins_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [BuildingAdmins] ADD CONSTRAINT [FK_BuildingAdmins_Users_AdminUid] FOREIGN KEY ([AdminUid]) REFERENCES [Users] ([Uid]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [BuildingSettings] ADD CONSTRAINT [FK_BuildingSettings_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Cameras] ADD CONSTRAINT [FK_Cameras_Dvrs_DvrId] FOREIGN KEY ([DvrId]) REFERENCES [Dvrs] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [DepositEditHistory] ADD CONSTRAINT [FK_DepositEditHistory_Deposits_BuildingId_DepositId] FOREIGN KEY ([BuildingId], [DepositId]) REFERENCES [Deposits] ([BuildingId], [Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Deposits] ADD CONSTRAINT [FK_Deposits_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Deposits] ADD CONSTRAINT [FK_Deposits_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Dvrs] ADD CONSTRAINT [FK_Dvrs_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Expenses] ADD CONSTRAINT [FK_Expenses_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Expenses] ADD CONSTRAINT [FK_Expenses_FinancialCategories_BuildingId_CategoryKind_CategoryId] FOREIGN KEY ([BuildingId], [CategoryKind], [CategoryId]) REFERENCES [FinancialCategories] ([BuildingId], [Kind], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [FinancialCategories] ADD CONSTRAINT [FK_FinancialCategories_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Floors] ADD CONSTRAINT [FK_Floors_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [MaintenanceRecords] ADD CONSTRAINT [FK_MaintenanceRecords_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [MonthlyApartmentShares] ADD CONSTRAINT [FK_MonthlyApartmentShares_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [MonthlyApartmentShares] ADD CONSTRAINT [FK_MonthlyApartmentShares_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Notifications] ADD CONSTRAINT [FK_Notifications_Apartments_BuildingId_RecipientApartmentId] FOREIGN KEY ([BuildingId], [RecipientApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Notifications] ADD CONSTRAINT [FK_Notifications_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [PollOptions] ADD CONSTRAINT [FK_PollOptions_Polls_BuildingId_PollId] FOREIGN KEY ([BuildingId], [PollId]) REFERENCES [Polls] ([BuildingId], [Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Polls] ADD CONSTRAINT [FK_Polls_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [PollVotes] ADD CONSTRAINT [FK_PollVotes_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [PollVotes] ADD CONSTRAINT [FK_PollVotes_PollOptions_BuildingId_PollId_OptionId] FOREIGN KEY ([BuildingId], [PollId], [OptionId]) REFERENCES [PollOptions] ([BuildingId], [PollId], [Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [QrTokens] ADD CONSTRAINT [FK_QrTokens_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [QrTokens] ADD CONSTRAINT [FK_QrTokens_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Residents] ADD CONSTRAINT [FK_Residents_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Revenues] ADD CONSTRAINT [FK_Revenues_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [Revenues] ADD CONSTRAINT [FK_Revenues_FinancialCategories_BuildingId_CategoryKind_CategoryId] FOREIGN KEY ([BuildingId], [CategoryKind], [CategoryId]) REFERENCES [FinancialCategories] ([BuildingId], [Kind], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [UserPermissions] ADD CONSTRAINT [FK_UserPermissions_Users_Uid] FOREIGN KEY ([Uid]) REFERENCES [Users] ([Uid]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [WalletAdjustments] ADD CONSTRAINT [FK_WalletAdjustments_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    ALTER TABLE [WalletAdjustments] ADD CONSTRAINT [FK_WalletAdjustments_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117012657_Phase04_StandardizeKeyColumnTypes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261117012657_Phase04_StandardizeKeyColumnTypes', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117050000_Phase05_RemoveRedundantResidentIndexes'
)
BEGIN
    IF OBJECT_ID(N'dbo.Residents', N'U') IS NOT NULL
    BEGIN
        IF EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_Residents_ApartmentId'
              AND object_id = OBJECT_ID(N'dbo.Residents')
        )
            DROP INDEX [IX_Residents_ApartmentId] ON [dbo].[Residents];

        IF EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_Residents_BuildingId'
              AND object_id = OBJECT_ID(N'dbo.Residents')
        )
            DROP INDEX [IX_Residents_BuildingId] ON [dbo].[Residents];
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117050000_Phase05_RemoveRedundantResidentIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261117050000_Phase05_RemoveRedundantResidentIndexes', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117130000_AddUserTwoFactor'
)
BEGIN
    ALTER TABLE [Users] ADD [TwoFactorEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117130000_AddUserTwoFactor'
)
BEGIN
    ALTER TABLE [Users] ADD [TwoFactorSecretEncrypted] nvarchar(512) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117130000_AddUserTwoFactor'
)
BEGIN
    ALTER TABLE [Users] ADD [TwoFactorRecoveryCodesJson] nvarchar(4000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117130000_AddUserTwoFactor'
)
BEGIN
    ALTER TABLE [Users] ADD [TwoFactorLastUsedStep] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117130000_AddUserTwoFactor'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261117130000_AddUserTwoFactor', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
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
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
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
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
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
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
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
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
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
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
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
        [Reason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ApartmentAccessGrants] PRIMARY KEY ([GrantId]),
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
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
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
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE TABLE [ApartmentOwnerships] (
        [OwnershipId] uniqueidentifier NOT NULL,
        [BuildingId] nvarchar(64) NOT NULL,
        [ApartmentId] nvarchar(64) NOT NULL,
        [PersonId] uniqueidentifier NOT NULL,
        [OwnershipSharePercent] decimal(9,6) NOT NULL,
        [ValidFromUtc] datetime2(3) NOT NULL,
        [ValidToUtc] datetime2(3) NULL,
        [RecordedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [RecordedByAccountId] uniqueidentifier NULL,
        [EvidenceReference] nvarchar(500) NULL,
        [Notes] nvarchar(1000) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ApartmentOwnerships] PRIMARY KEY ([OwnershipId]),
        CONSTRAINT [CK_ApartmentOwnerships_Share] CHECK ([OwnershipSharePercent] > 0 AND [OwnershipSharePercent] <= 100),
        CONSTRAINT [CK_ApartmentOwnerships_Validity] CHECK ([ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]),
        CONSTRAINT [FK_ApartmentOwnerships_Apartments_BuildingId_ApartmentId] FOREIGN KEY ([BuildingId], [ApartmentId]) REFERENCES [Apartments] ([BuildingId], [Id]),
        CONSTRAINT [FK_ApartmentOwnerships_Persons_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [Persons] ([PersonId]),
        CONSTRAINT [FK_ApartmentOwnerships_UserAccounts_RecordedByAccountId] FOREIGN KEY ([RecordedByAccountId]) REFERENCES [UserAccounts] ([AccountId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
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
        CONSTRAINT [CK_LegacyIdentityMap_ResolvedReview] CHECK (([MappingStatus] NOT IN (1,2) OR ([ReviewedAtUtc] IS NOT NULL AND [ReviewedByAccountId] IS NOT NULL))),
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
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
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
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
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
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
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
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
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
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_AccountPermissionOverrides_Account_Permission_Scope] ON [AccountPermissionOverrides] ([AccountId], [PermissionKey], [ScopeType], [ScopeBuildingId], [ScopeApartmentId], [ValidFromUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_AccountPermissionOverrides_GrantedByAccountId] ON [AccountPermissionOverrides] ([GrantedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_AccountPermissionOverrides_Permission_Scope] ON [AccountPermissionOverrides] ([PermissionKey], [ScopeType], [ScopeBuildingId], [ScopeApartmentId], [ValidFromUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_AccountPermissionOverrides_RevokedByAccountId] ON [AccountPermissionOverrides] ([RevokedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_AccountPermissionOverrides_ScopeBuildingId_ScopeApartmentId] ON [AccountPermissionOverrides] ([ScopeBuildingId], [ScopeApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentAccessGrants_Apartment_Period] ON [ApartmentAccessGrants] ([BuildingId], [ApartmentId], [ValidFromUtc], [ValidToUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentAccessGrants_GrantedByAccountId] ON [ApartmentAccessGrants] ([GrantedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentAccessGrants_Person_Period] ON [ApartmentAccessGrants] ([PersonId], [ValidFromUtc], [ValidToUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentAccessGrants_RevokedByAccountId] ON [ApartmentAccessGrants] ([RevokedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentOccupancies_Apartment_Period] ON [ApartmentOccupancies] ([BuildingId], [ApartmentId], [ValidFromUtc], [ValidToUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentOccupancies_Person_Period] ON [ApartmentOccupancies] ([PersonId], [ValidFromUtc], [ValidToUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentOccupancies_RecordedByAccountId] ON [ApartmentOccupancies] ([RecordedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_ApartmentOccupancies_OneOpenPrimary] ON [ApartmentOccupancies] ([BuildingId], [ApartmentId]) WHERE [IsPrimary] = 1 AND [ValidToUtc] IS NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentOwnerships_Apartment_Period] ON [ApartmentOwnerships] ([BuildingId], [ApartmentId], [ValidFromUtc], [ValidToUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentOwnerships_Person_Period] ON [ApartmentOwnerships] ([PersonId], [ValidFromUtc], [ValidToUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_ApartmentOwnerships_RecordedByAccountId] ON [ApartmentOwnerships] ([RecordedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_LegacyIdentityMap_Person] ON [LegacyIdentityMap] ([PersonId]) WHERE [PersonId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_LegacyIdentityMap_ReviewedByAccountId] ON [LegacyIdentityMap] ([ReviewedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_LegacyIdentityMap_Status_Source] ON [LegacyIdentityMap] ([MappingStatus], [SourceTable], [SourceSystem]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_LegacyIdentityMap_Account] ON [LegacyIdentityMap] ([AccountId]) WHERE [AccountId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Persons_PrimaryEmail] ON [Persons] ([PrimaryEmail]) WHERE [PrimaryEmail] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Persons_PrimaryPhone] ON [Persons] ([PrimaryPhone]) WHERE [PrimaryPhone] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_UserAccounts_NormalizedEmail] ON [UserAccounts] ([NormalizedEmail]) WHERE [NormalizedEmail] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_UserAccounts_ExternalLogin] ON [UserAccounts] ([ExternalProvider], [ExternalSubject]) WHERE [ExternalProvider] IS NOT NULL AND [ExternalSubject] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserCredentials_Account_Type] ON [UserCredentials] ([AccountId], [CredentialType], [RevokedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserCredentials_ReplacedByCredentialId_AccountId] ON [UserCredentials] ([ReplacedByCredentialId], [AccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserRecoveryCodes_Account_Unused] ON [UserRecoveryCodes] ([AccountId], [UsedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [UX_UserRecoveryCodes_CodeHash] ON [UserRecoveryCodes] ([CodeHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_Account_Scope] ON [UserRoleAssignments] ([AccountId], [ScopeType], [ScopeBuildingId], [ScopeApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_GrantedByAccountId] ON [UserRoleAssignments] ([GrantedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_RevokedByAccountId] ON [UserRoleAssignments] ([RevokedByAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_Role_Scope] ON [UserRoleAssignments] ([RoleKey], [ScopeType], [ScopeBuildingId], [ScopeApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignments_ScopeBuildingId_ScopeApartmentId] ON [UserRoleAssignments] ([ScopeBuildingId], [ScopeApartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118172211_Phase06_IdentityAuthorizationFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261118172211_Phase06_IdentityAuthorizationFoundation', N'8.0.10');
END;
GO

COMMIT;
GO

