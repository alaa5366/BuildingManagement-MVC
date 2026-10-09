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
    WHERE [MigrationId] = N'20261007195502_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261007195502_InitialCreate', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008164958_Drop_Receipts_Add_QrUsages'
)
BEGIN
    DROP TABLE [Receipts];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008164958_Drop_Receipts_Add_QrUsages'
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
    WHERE [MigrationId] = N'20261008164958_Drop_Receipts_Add_QrUsages'
)
BEGIN
    CREATE INDEX [IX_QrUsages_CreatedAt] ON [QrUsages] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008164958_Drop_Receipts_Add_QrUsages'
)
BEGIN
    CREATE INDEX [IX_QrUsages_TokenId] ON [QrUsages] ([TokenId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008164958_Drop_Receipts_Add_QrUsages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008164958_Drop_Receipts_Add_QrUsages', N'8.0.10');
END;
GO

COMMIT;
GO

