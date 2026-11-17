/*
Phase06 - Read-only index and query workload diagnostics
Target: BuildingManagement_RestoreTest (TEST ONLY)
SQL Server: 2017+ (STRING_AGG)
No schema/data changes are performed.

IMPORTANT:
- Run in SSMS after selecting the TEST database.
- sys.dm_db_index_usage_stats counters reset when SQL Server restarts.
- Run after representative application usage; a quiet test DB cannot establish production index usage.
- Missing-index DMV suggestions are estimates, not instructions to create indexes.
*/

SET NOCOUNT ON;

SELECT
    DB_NAME() AS CurrentDatabase,
    @@SERVERNAME AS SqlServerName,
    SYSDATETIMEOFFSET() AS CapturedAt,
    sqlserver_start_time AS SqlServerStartTime
FROM sys.dm_os_sys_info;

-- 1. Existing indexes, key/include columns, row counts, and usage counters.
;WITH IndexKeyColumns AS
(
    SELECT
        ic.object_id,
        ic.index_id,
        STRING_AGG(
            QUOTENAME(c.name) + CASE WHEN ic.is_descending_key = 1 THEN ' DESC' ELSE ' ASC' END,
            ', '
        ) WITHIN GROUP (ORDER BY ic.key_ordinal) AS KeyColumns
    FROM sys.index_columns AS ic
    INNER JOIN sys.columns AS c
        ON c.object_id = ic.object_id
       AND c.column_id = ic.column_id
    WHERE ic.is_included_column = 0
    GROUP BY ic.object_id, ic.index_id
),
IndexIncludedColumns AS
(
    SELECT
        ic.object_id,
        ic.index_id,
        STRING_AGG(QUOTENAME(c.name), ', ')
            WITHIN GROUP (ORDER BY ic.index_column_id) AS IncludedColumns
    FROM sys.index_columns AS ic
    INNER JOIN sys.columns AS c
        ON c.object_id = ic.object_id
       AND c.column_id = ic.column_id
    WHERE ic.is_included_column = 1
    GROUP BY ic.object_id, ic.index_id
),
PartitionRows AS
(
    SELECT object_id, index_id, SUM(row_count) AS RowCountValue
    FROM sys.dm_db_partition_stats
    GROUP BY object_id, index_id
)
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_primary_key AS IsPrimaryKey,
    i.is_unique AS IsUnique,
    i.is_disabled AS IsDisabled,
    i.has_filter AS HasFilter,
    i.filter_definition AS FilterDefinition,
    ik.KeyColumns,
    ii.IncludedColumns,
    COALESCE(pr.RowCountValue, 0) AS RowCountValue,
    COALESCE(us.user_seeks, 0) AS UserSeeks,
    COALESCE(us.user_scans, 0) AS UserScans,
    COALESCE(us.user_lookups, 0) AS UserLookups,
    COALESCE(us.user_updates, 0) AS UserUpdates,
    us.last_user_seek AS LastUserSeek,
    us.last_user_scan AS LastUserScan,
    us.last_user_lookup AS LastUserLookup,
    us.last_user_update AS LastUserUpdate
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
INNER JOIN sys.indexes AS i ON i.object_id = t.object_id
LEFT JOIN IndexKeyColumns AS ik
    ON ik.object_id = i.object_id AND ik.index_id = i.index_id
LEFT JOIN IndexIncludedColumns AS ii
    ON ii.object_id = i.object_id AND ii.index_id = i.index_id
LEFT JOIN PartitionRows AS pr
    ON pr.object_id = i.object_id AND pr.index_id = i.index_id
LEFT JOIN sys.dm_db_index_usage_stats AS us
    ON us.database_id = DB_ID()
   AND us.object_id = i.object_id
   AND us.index_id = i.index_id
WHERE i.index_id > 0
ORDER BY s.name, t.name, i.index_id;

-- 2. Foreign-key columns and whether an index starts with the first FK column.
;WITH ForeignKeySummary AS
(
    SELECT
        fk.parent_object_id,
        fk.name AS ForeignKeyName,
        MIN(fkc.parent_column_id) AS FirstParentColumnId,
        STRING_AGG(QUOTENAME(pc.name), ', ')
            WITHIN GROUP (ORDER BY fkc.constraint_column_id) AS ForeignKeyColumns
    FROM sys.foreign_keys AS fk
    INNER JOIN sys.foreign_key_columns AS fkc
        ON fkc.constraint_object_id = fk.object_id
    INNER JOIN sys.columns AS pc
        ON pc.object_id = fkc.parent_object_id
       AND pc.column_id = fkc.parent_column_id
    GROUP BY fk.parent_object_id, fk.name
)
SELECT
    OBJECT_SCHEMA_NAME(fks.parent_object_id) AS SchemaName,
    OBJECT_NAME(fks.parent_object_id) AS TableName,
    fks.ForeignKeyName,
    fks.ForeignKeyColumns,
    CASE WHEN EXISTS
    (
        SELECT 1
        FROM sys.indexes AS ix
        INNER JOIN sys.index_columns AS first_ic
            ON first_ic.object_id = ix.object_id
           AND first_ic.index_id = ix.index_id
           AND first_ic.key_ordinal = 1
        WHERE ix.object_id = fks.parent_object_id
          AND first_ic.column_id = fks.FirstParentColumnId
          AND ix.is_disabled = 0
          AND ix.index_id > 0
    ) THEN 'At least one index starts with first FK column'
      ELSE 'Review: no index starts with first FK column'
    END AS LeadingColumnCoverage
FROM ForeignKeySummary AS fks
ORDER BY SchemaName, TableName, fks.ForeignKeyName;

-- 3. Missing-index suggestions for this database (advisory only).
SELECT TOP (100)
    OBJECT_SCHEMA_NAME(mid.object_id, mid.database_id) AS SchemaName,
    OBJECT_NAME(mid.object_id, mid.database_id) AS TableName,
    migs.user_seeks AS UserSeeks,
    migs.user_scans AS UserScans,
    migs.avg_total_user_cost AS AvgTotalUserCost,
    migs.avg_user_impact AS AvgUserImpactPercent,
    mid.equality_columns AS EqualityColumns,
    mid.inequality_columns AS InequalityColumns,
    mid.included_columns AS IncludedColumns,
    'CREATE INDEX (candidate only - validate workload first)' AS ReviewStatus
FROM sys.dm_db_missing_index_group_stats AS migs
INNER JOIN sys.dm_db_missing_index_groups AS mig
    ON mig.index_group_handle = migs.group_handle
INNER JOIN sys.dm_db_missing_index_details AS mid
    ON mid.index_handle = mig.index_handle
WHERE mid.database_id = DB_ID()
ORDER BY (migs.user_seeks + migs.user_scans) * migs.avg_total_user_cost * (migs.avg_user_impact / 100.0) DESC;

-- 4. Current database table row counts for workload context.
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    SUM(ps.row_count) AS RowCountValue
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.dm_db_partition_stats ps ON ps.object_id = t.object_id
WHERE ps.index_id IN (0, 1)
GROUP BY s.name, t.name
ORDER BY s.name, t.name;
