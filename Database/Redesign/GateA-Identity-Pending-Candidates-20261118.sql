SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'BuildingManagement_RestoreTest'
    THROW 51000, 'This script is restricted to BuildingManagement_RestoreTest.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261118182110_Phase06_IdentityAuthorizationFoundation')
   OR NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261118184850_Phase07_OwnershipRecordProvenance')
    THROW 51001, 'The expected Phase06 and Phase07 schema migrations are required.', 1;

IF EXISTS (SELECT 1 FROM dbo.Users WHERE Uid IS NULL OR LEN(LTRIM(RTRIM(Uid))) = 0)
   OR EXISTS (SELECT 1 FROM dbo.Residents WHERE Id IS NULL OR LEN(LTRIM(RTRIM(Id))) = 0)
    THROW 51002, 'A source row has an invalid key; stop and reconcile before candidate registration.', 1;

DECLARE @NowUtc datetime2(3) = SYSUTCDATETIME();
DECLARE @UsersInserted int = 0;
DECLARE @ResidentsInserted int = 0;

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO dbo.LegacyIdentityMap
        (LegacyIdentityMapId, SourceSystem, SourceTable, SourceKey, PersonId, AccountId, MappingStatus, CreatedAtUtc)
    SELECT NEWID(), N'legacy-sql', N'Users', source.Uid, NULL, NULL, 0, @NowUtc
    FROM dbo.Users AS source
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.LegacyIdentityMap AS existing
        WHERE existing.SourceSystem = N'legacy-sql'
          AND existing.SourceTable = N'Users'
          AND existing.SourceKey = source.Uid
    );
    SET @UsersInserted = @@ROWCOUNT;

    INSERT INTO dbo.LegacyIdentityMap
        (LegacyIdentityMapId, SourceSystem, SourceTable, SourceKey, PersonId, AccountId, MappingStatus, CreatedAtUtc)
    SELECT NEWID(), N'legacy-sql', N'Residents', source.Id, NULL, NULL, 0, @NowUtc
    FROM dbo.Residents AS source
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.LegacyIdentityMap AS existing
        WHERE existing.SourceSystem = N'legacy-sql'
          AND existing.SourceTable = N'Residents'
          AND existing.SourceKey = source.Id
    );
    SET @ResidentsInserted = @@ROWCOUNT;

    IF @UsersInserted + @ResidentsInserted > 0
    BEGIN
        INSERT INTO dbo.AuditLog
            (Ts, Action, Label, Actor, ActorRole, Details, MonthKey, Severity)
        VALUES
            (@NowUtc,
             N'identity.mapping.candidates.imported',
             N'Pending identity candidates registered',
             N'migration:identity-candidates',
             N'system.migration',
             N'{"usersInserted":' + CONVERT(nvarchar(20), @UsersInserted) +
                N',"residentsInserted":' + CONVERT(nvarchar(20), @ResidentsInserted) + N'}',
             CONVERT(nvarchar(7), @NowUtc, 126),
             N'info');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT DB_NAME() AS DatabaseName,
       @UsersInserted AS UsersInserted,
       @ResidentsInserted AS ResidentsInserted,
       (SELECT COUNT_BIG(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem = N'legacy-sql' AND MappingStatus = 0) AS PendingLegacySqlCandidates;
