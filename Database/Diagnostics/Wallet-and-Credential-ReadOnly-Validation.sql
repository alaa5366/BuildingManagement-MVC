/*
Wallet and credential format diagnostics — READ ONLY.
Target: disposable BuildingManagement_RestoreTest only.
Exports metadata and aggregate classifications/counts only.
Never returns PINs, hashes, phone numbers, names, or other secret values.
*/
USE [BuildingManagement_RestoreTest];
IF DB_NAME() <> N'BuildingManagement_RestoreTest'
    THROW 51000, 'Wrong database. This diagnostic is allowed only on BuildingManagement_RestoreTest.', 1;
SET NOCOUNT ON;

-- 1. Database identity and requested wallet-view object presence.
SELECT DB_NAME() AS DatabaseName, @@SERVERNAME AS ServerName,
       SYSUTCDATETIME() AS CheckedAtUtc,
       N'READ_ONLY_WALLET_CREDENTIAL_DIAGNOSTIC' AS CheckType;

SELECT v.SchemaName, v.ViewName,
       CASE WHEN o.object_id IS NULL THEN N'ABSENT'
            WHEN o.type = 'V' THEN N'VIEW_PRESENT'
            ELSE o.type_desc END AS ObjectState,
       o.create_date AS CreatedAt,
       o.modify_date AS ModifiedAt
FROM (VALUES (N'dbo',N'vw_ApartmentWalletMovements'),
             (N'dbo',N'vw_ApartmentWalletBalance')) v(SchemaName,ViewName)
LEFT JOIN sys.schemas s ON s.name = v.SchemaName
LEFT JOIN sys.objects o ON o.schema_id = s.schema_id AND o.name = v.ViewName
ORDER BY v.ViewName;

-- 2. Actual wallet-named views and their definitions (definition contains SQL only, no row data).
SELECT s.name AS SchemaName, o.name AS ViewName,
       OBJECT_DEFINITION(o.object_id) AS ViewDefinition
FROM sys.views o
JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE o.name LIKE N'%Wallet%'
ORDER BY s.name, o.name;

-- 3. SQL-module dependencies referencing wallet-named views.
SELECT referencingSchema.name AS ReferencingSchema,
       referencingObject.name AS ReferencingObject,
       referencingObject.type_desc AS ReferencingType,
       referencedSchema.name AS ReferencedSchema,
       referencedObject.name AS ReferencedObject
FROM sys.sql_expression_dependencies d
JOIN sys.objects referencingObject ON referencingObject.object_id = d.referencing_id
JOIN sys.schemas referencingSchema ON referencingSchema.schema_id = referencingObject.schema_id
LEFT JOIN sys.objects referencedObject ON referencedObject.object_id = d.referenced_id
LEFT JOIN sys.schemas referencedSchema ON referencedSchema.schema_id = referencedObject.schema_id
WHERE d.referenced_entity_name LIKE N'%Wallet%'
   OR referencingObject.name LIKE N'%Wallet%'
ORDER BY referencingSchema.name, referencingObject.name;

-- 4. Credential format inventory. Values are used only transiently for classification;
-- only counts and length bands are returned. No stored value or identifying field is exported.
DECLARE @CredentialInventory TABLE
(
    CredentialSource nvarchar(40) NOT NULL,
    RoleName nvarchar(40) NOT NULL,
    CredentialField nvarchar(40) NOT NULL,
    StoredValue nvarchar(max) NULL
);

INSERT INTO @CredentialInventory (CredentialSource, RoleName, CredentialField, StoredValue)
SELECT N'Users', COALESCE(Role,N'(null)'), N'Pin', CONVERT(nvarchar(max),Pin)
FROM dbo.Users
UNION ALL
SELECT N'Residents', N'(not-applicable)', N'PinHash', CONVERT(nvarchar(max),PinHash)
FROM dbo.Residents
UNION ALL
SELECT N'Residents', N'(not-applicable)', N'PasswordHash', CONVERT(nvarchar(max),PasswordHash)
FROM dbo.Residents
UNION ALL
SELECT N'Apartments', N'(not-applicable)', N'Pin', CONVERT(nvarchar(max),Pin)
FROM dbo.Apartments
UNION ALL
SELECT N'Buildings', N'(not-applicable)', N'AdminPin', CONVERT(nvarchar(max),AdminPin)
FROM dbo.Buildings;

WITH Classified AS
(
    SELECT CredentialSource, RoleName, CredentialField,
           CASE
             WHEN StoredValue IS NULL OR LEN(LTRIM(RTRIM(StoredValue))) = 0 THEN N'EMPTY'
             WHEN LEFT(LTRIM(RTRIM(StoredValue)),4) IN (N'$2a$',N'$2b$',N'$2y$')
                  AND LEN(LTRIM(RTRIM(StoredValue))) = 60 THEN N'BCRYPT_SHAPE_60_UNVERIFIED'
             WHEN LEFT(LTRIM(RTRIM(StoredValue)),4) IN (N'$2a$',N'$2b$',N'$2y$')
                  THEN N'BCRYPT_PREFIX_NONSTANDARD_LENGTH'
             WHEN LEN(LTRIM(RTRIM(StoredValue))) = 4
                  AND LTRIM(RTRIM(StoredValue)) NOT LIKE N'%[^0-9]%' THEN N'FOUR_DIGIT_PLAINTEXT_CANDIDATE'
             ELSE N'NON_BCRYPT_OTHER'
           END AS FormatClass,
           CASE WHEN StoredValue IS NULL THEN 0 ELSE LEN(LTRIM(RTRIM(StoredValue))) END AS ValueLength
    FROM @CredentialInventory
)
SELECT CredentialSource, RoleName, CredentialField, FormatClass,
       COUNT_BIG(*) AS RecordCount,
       MIN(ValueLength) AS MinValueLength,
       MAX(ValueLength) AS MaxValueLength
FROM Classified
GROUP BY CredentialSource, RoleName, CredentialField, FormatClass
ORDER BY CredentialSource, RoleName, CredentialField, FormatClass;

-- 5. Total row counts by credential field, including empty values.
SELECT CredentialSource, RoleName, CredentialField, COUNT_BIG(*) AS TotalRecords,
       SUM(CASE WHEN StoredValue IS NULL OR LEN(LTRIM(RTRIM(StoredValue)))=0 THEN 1 ELSE 0 END) AS EmptyRecords,
       SUM(CASE WHEN StoredValue IS NOT NULL AND LEN(LTRIM(RTRIM(StoredValue)))>0 THEN 1 ELSE 0 END) AS NonEmptyRecords
FROM @CredentialInventory
GROUP BY CredentialSource, RoleName, CredentialField
ORDER BY CredentialSource, RoleName, CredentialField;
