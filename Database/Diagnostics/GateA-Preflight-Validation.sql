/*
Gate A preflight — read-only metadata validation
Target: disposable BuildingManagement_RestoreTest only.

This script performs SELECTs against catalog metadata only. It does not create,
alter, or delete objects and does not read application rows or secret values.
Review every result set before considering the Gate A rehearsal script.
*/
USE [BuildingManagement_RestoreTest];
IF DB_NAME() <> N'BuildingManagement_RestoreTest'
    THROW 51000, 'Wrong database. Gate A preflight is allowed only on BuildingManagement_RestoreTest.', 1;
SET NOCOUNT ON;

SELECT
    DB_NAME() AS DatabaseName,
    @@SERVERNAME AS ServerName,
    SYSUTCDATETIME() AS CheckedAtUtc,
    'READ_ONLY_GATE_A_PREFLIGHT' AS CheckType;

DECLARE @ExpectedColumns TABLE
(
    SchemaName sysname NOT NULL,
    TableName sysname NOT NULL,
    ColumnName sysname NOT NULL,
    ExpectedType sysname NOT NULL,
    ExpectedMaxLengthBytes smallint NOT NULL
);

INSERT INTO @ExpectedColumns
    (SchemaName, TableName, ColumnName, ExpectedType, ExpectedMaxLengthBytes)
VALUES
    (N'dbo', N'Buildings', N'Id', N'nvarchar', 128),
    (N'dbo', N'Apartments', N'BuildingId', N'nvarchar', 128),
    (N'dbo', N'Apartments', N'Id', N'nvarchar', 128);

-- 1. Exact SQL type/length/collation for all legacy columns referenced by Gate A FKs.
SELECT
    e.SchemaName,
    e.TableName,
    e.ColumnName,
    e.ExpectedType,
    e.ExpectedMaxLengthBytes,
    actualType.name AS ActualType,
    c.max_length AS ActualMaxLengthBytes,
    c.collation_name AS ActualCollation,
    c.is_nullable AS IsNullable,
    CASE
        WHEN c.column_id IS NULL THEN 'MISSING'
        WHEN actualType.name <> e.ExpectedType THEN 'TYPE_MISMATCH'
        WHEN c.max_length <> e.ExpectedMaxLengthBytes THEN 'LENGTH_MISMATCH'
        ELSE 'TYPE_LENGTH_OK_REVIEW_COLLATION'
    END AS ValidationStatus
FROM @ExpectedColumns e
LEFT JOIN sys.schemas s ON s.name = e.SchemaName
LEFT JOIN sys.tables t ON t.schema_id = s.schema_id AND t.name = e.TableName
LEFT JOIN sys.columns c ON c.object_id = t.object_id AND c.name = e.ColumnName
LEFT JOIN sys.types actualType ON actualType.user_type_id = c.user_type_id
ORDER BY e.TableName, e.ColumnName;

-- 2. Key/index presence for the exact referenced legacy keys.
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    i.name AS IndexName,
    i.is_primary_key AS IsPrimaryKey,
    i.is_unique AS IsUnique,
    STRING_AGG(c.name, ',') WITHIN GROUP (ORDER BY ic.key_ordinal) AS KeyColumns
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.indexes i ON i.object_id = t.object_id
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE s.name = N'dbo'
  AND t.name IN (N'Buildings', N'Apartments')
  AND i.is_hypothetical = 0
  AND ic.key_ordinal > 0
GROUP BY s.name, t.name, i.name, i.is_primary_key, i.is_unique
ORDER BY t.name, i.is_primary_key DESC, i.is_unique DESC, i.name;

-- 3. Collation compatibility among referenced key columns.
SELECT
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS SqlType,
    c.max_length AS MaxLengthBytes,
    c.collation_name AS CollationName
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE s.name = N'dbo'
  AND ((t.name = N'Buildings' AND c.name = N'Id')
    OR (t.name = N'Apartments' AND c.name IN (N'BuildingId', N'Id')))
ORDER BY t.name, c.name;

-- 4. Database default collation. New nvarchar columns inherit this unless explicitly overridden.
SELECT
    d.name AS DatabaseName,
    d.collation_name AS DatabaseDefaultCollation,
    CASE
        WHEN d.collation_name = N'SQL_Latin1_General_CP1_CI_AS'
            THEN 'MATCHES_CURRENT_LEGACY_KEY_COLLATION'
        ELSE 'REVIEW_REQUIRED_BEFORE_FK_CREATION'
    END AS CollationStatus
FROM sys.databases d
WHERE d.database_id = DB_ID();

-- 5. Existing Gate A object names (now 13 tables). Any partial/manual objects require manual inspection;
-- the deployment script is not a partial-state repair script.
SELECT
    v.ObjectName,
    CASE WHEN o.object_id IS NULL THEN 'ABSENT' ELSE o.type_desc END AS ExistingObjectState
FROM (VALUES
    (N'Persons'), (N'UserAccounts'), (N'UserCredentials'),
    (N'UserTwoFactorSettings'), (N'UserRecoveryCodes'),
    (N'RoleDefinitions'), (N'RolePermissions'), (N'UserRoleAssignments'),
    (N'AccountPermissionOverrides'), (N'LegacyIdentityMap'),
    (N'ApartmentOwnerships'), (N'ApartmentOccupancies'), (N'ApartmentAccessGrants')
) v(ObjectName)
LEFT JOIN sys.objects o
    ON o.schema_id = SCHEMA_ID(N'dbo')
   AND o.name = v.ObjectName
ORDER BY v.ObjectName;

-- 5. Confirm applied EF migrations for the exact target database.
SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;
