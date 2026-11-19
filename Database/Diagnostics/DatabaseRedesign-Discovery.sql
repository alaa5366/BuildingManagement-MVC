/*
    BuildingManagement - Database Redesign Discovery
    ------------------------------------------------
    PURPOSE:
      Read-only inventory of the connected SQL Server database before redesign.

    SAFETY:
      - This script performs SELECT statements only.
      - It does not create, alter, drop, truncate, update, or delete anything.
      - It does not read application row values or secrets.
      - Run it against the intended BuildingManagement database.
      - Save all result sets for the schema-to-code reconciliation.

    NOTE:
      The output describes the database to which the current connection is pointed.
      A checked-in schema script is not proof of the live database schema.
*/
SET NOCOUNT ON;

SELECT
    DB_NAME() AS DatabaseName,
    @@SERVERNAME AS ServerName,
    CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(128)) AS SqlServerVersion,
    CAST(SERVERPROPERTY('Edition') AS nvarchar(128)) AS SqlServerEdition,
    SYSUTCDATETIME() AS CapturedAtUtc;

-- 1. User tables and approximate row counts (metadata only).
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    SUM(CASE WHEN p.index_id IN (0, 1) THEN p.rows ELSE 0 END) AS ApproximateRows
FROM sys.tables AS t
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
LEFT JOIN sys.partitions AS p ON p.object_id = t.object_id
WHERE t.is_ms_shipped = 0
GROUP BY s.name, t.name
ORDER BY s.name, t.name;

-- 2. Columns, SQL types, nullability, identity, computed and generated properties.
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    c.column_id AS ColumnOrdinal,
    c.name AS ColumnName,
    ty.name AS SqlType,
    CASE
        WHEN ty.name IN ('varchar','char','varbinary','binary')
            THEN CASE WHEN c.max_length = -1 THEN 'MAX' ELSE CONVERT(varchar(12), c.max_length) END
        WHEN ty.name IN ('nvarchar','nchar')
            THEN CASE WHEN c.max_length = -1 THEN 'MAX' ELSE CONVERT(varchar(12), c.max_length / 2) END
        WHEN ty.name IN ('decimal','numeric')
            THEN CONCAT(c.precision, ',', c.scale)
        WHEN ty.name IN ('datetime2','datetimeoffset','time')
            THEN CONVERT(varchar(12), c.scale)
        ELSE NULL
    END AS TypeSizeOrPrecision,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity,
    c.is_computed AS IsComputed,
    c.is_rowguidcol AS IsRowGuid,
    dc.definition AS DefaultDefinition,
    cc.definition AS ComputedDefinition,
    c.collation_name AS CollationName
FROM sys.tables AS t
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
JOIN sys.columns AS c ON c.object_id = t.object_id
JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints AS dc
    ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
LEFT JOIN sys.computed_columns AS cc
    ON cc.object_id = c.object_id AND cc.column_id = c.column_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name, c.column_id;

-- 3. Primary keys and unique constraints.
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    kc.name AS ConstraintName,
    kc.type_desc AS ConstraintType,
    ic.key_ordinal AS KeyOrdinal,
    c.name AS ColumnName,
    ic.is_descending_key AS IsDescending
FROM sys.key_constraints AS kc
JOIN sys.tables AS t ON t.object_id = kc.parent_object_id
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
JOIN sys.index_columns AS ic
    ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
JOIN sys.columns AS c
    ON c.object_id = ic.object_id AND c.column_id = ic.column_id
ORDER BY s.name, t.name, kc.name, ic.key_ordinal;

-- 4. Foreign keys, including column mappings and delete/update actions.
SELECT
    ps.name AS ParentSchema,
    pt.name AS ParentTable,
    fk.name AS ForeignKeyName,
    pc.name AS ParentColumn,
    rs.name AS ReferencedSchema,
    rt.name AS ReferencedTable,
    rc.name AS ReferencedColumn,
    fkc.constraint_column_id AS ColumnOrdinal,
    fk.delete_referential_action_desc AS OnDelete,
    fk.update_referential_action_desc AS OnUpdate,
    fk.is_disabled AS IsDisabled,
    fk.is_not_trusted AS IsNotTrusted
FROM sys.foreign_keys AS fk
JOIN sys.foreign_key_columns AS fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.tables AS pt ON pt.object_id = fk.parent_object_id
JOIN sys.schemas AS ps ON ps.schema_id = pt.schema_id
JOIN sys.columns AS pc ON pc.object_id = pt.object_id AND pc.column_id = fkc.parent_column_id
JOIN sys.tables AS rt ON rt.object_id = fk.referenced_object_id
JOIN sys.schemas AS rs ON rs.schema_id = rt.schema_id
JOIN sys.columns AS rc ON rc.object_id = rt.object_id AND rc.column_id = fkc.referenced_column_id
ORDER BY ps.name, pt.name, fk.name, fkc.constraint_column_id;

-- 5. Indexes, key columns, included columns, filters and operational flags.
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_primary_key AS IsPrimaryKey,
    i.is_unique AS IsUnique,
    i.is_unique_constraint AS IsUniqueConstraint,
    i.has_filter AS HasFilter,
    i.filter_definition AS FilterDefinition,
    i.is_disabled AS IsDisabled,
    ic.key_ordinal AS KeyOrdinal,
    ic.is_included_column AS IsIncludedColumn,
    ic.is_descending_key AS IsDescending,
    c.name AS ColumnName
FROM sys.indexes AS i
JOIN sys.tables AS t ON t.object_id = i.object_id
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE t.is_ms_shipped = 0
  AND i.index_id > 0
ORDER BY s.name, t.name, i.name, ic.key_ordinal, ic.index_column_id;

-- 6. CHECK constraints and their trust/disabled state.
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    cc.name AS ConstraintName,
    cc.definition AS Definition,
    cc.is_disabled AS IsDisabled,
    cc.is_not_trusted AS IsNotTrusted
FROM sys.check_constraints AS cc
JOIN sys.tables AS t ON t.object_id = cc.parent_object_id
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
ORDER BY s.name, t.name, cc.name;

-- 7. Views and SQL modules (names and metadata only; no secret-bearing definitions).
SELECT
    s.name AS SchemaName,
    o.name AS ObjectName,
    o.type_desc AS ObjectType,
    o.create_date AS CreatedAt,
    o.modify_date AS ModifiedAt,
    OBJECTPROPERTYEX(o.object_id, 'IsEncrypted') AS IsEncrypted
FROM sys.objects AS o
JOIN sys.schemas AS s ON s.schema_id = o.schema_id
WHERE o.type IN ('V','P','FN','IF','TF','TR')
  AND o.is_ms_shipped = 0
ORDER BY s.name, o.type_desc, o.name;

-- 8. Triggers and status.
SELECT
    OBJECT_SCHEMA_NAME(tr.parent_id) AS ParentSchema,
    OBJECT_NAME(tr.parent_id) AS ParentObject,
    tr.name AS TriggerName,
    tr.is_disabled AS IsDisabled,
    tr.is_instead_of_trigger AS IsInsteadOfTrigger,
    tr.create_date AS CreatedAt,
    tr.modify_date AS ModifiedAt
FROM sys.triggers AS tr
WHERE tr.is_ms_shipped = 0
ORDER BY ParentSchema, ParentObject, tr.name;

-- 9. EF Core migration history, if the standard table exists.
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
BEGIN
    SELECT MigrationId, ProductVersion
    FROM dbo.__EFMigrationsHistory
    ORDER BY MigrationId;
END
ELSE
BEGIN
    SELECT CAST(N'dbo.__EFMigrationsHistory not found' AS nvarchar(200)) AS MigrationHistoryStatus;
END;

-- 10. Synonyms and database-scoped dependencies that may affect deployment.
SELECT
    SCHEMA_NAME(schema_id) AS SchemaName,
    name AS SynonymName,
    base_object_name AS BaseObjectName
FROM sys.synonyms
ORDER BY SchemaName, SynonymName;
