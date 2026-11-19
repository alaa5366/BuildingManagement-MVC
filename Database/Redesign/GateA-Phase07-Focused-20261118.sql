BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118184850_Phase07_OwnershipRecordProvenance'
)
BEGIN
    ALTER TABLE [ApartmentOwnerships] ADD [RecordSource] tinyint NOT NULL DEFAULT CAST(0 AS tinyint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118184850_Phase07_OwnershipRecordProvenance'
)
BEGIN
    EXEC(N'ALTER TABLE [ApartmentOwnerships] ADD CONSTRAINT [CK_ApartmentOwnerships_RecordActor] CHECK (([RecordSource] <> 0 OR [RecordedByAccountId] IS NOT NULL))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118184850_Phase07_OwnershipRecordProvenance'
)
BEGIN
    EXEC(N'ALTER TABLE [ApartmentOwnerships] ADD CONSTRAINT [CK_ApartmentOwnerships_RecordSource] CHECK ([RecordSource] IN (0,1,2))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118184850_Phase07_OwnershipRecordProvenance'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261118184850_Phase07_OwnershipRecordProvenance', N'8.0.10');
END;
GO

COMMIT;
GO
