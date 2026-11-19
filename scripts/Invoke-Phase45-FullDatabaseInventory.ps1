[CmdletBinding()]
param(
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dir = Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$report = Join-Path $dir "Phase45-LegacyDatabaseFullInventory-$stamp.md"
$log = [System.Collections.Generic.List[string]]::new()
function Log([string]$s) { $log.Add($s); Write-Host $s }
$c = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try {
    $c.Open()
    $dbCmd = $c.CreateCommand()
    $dbCmd.CommandText = 'SELECT DB_NAME();'
    $db = [string]$dbCmd.ExecuteScalar()
    if ($db -ne 'BuildingManagement_RestoreTest') { throw "Unsafe inventory target '$db'." }

    $sql = @"
SET NOCOUNT ON;
DECLARE @Tables TABLE(
    SchemaName sysname NOT NULL,
    TableName sysname NOT NULL,
    ObjectId int NOT NULL,
    RowCount bigint NOT NULL,
    PrimaryKeyColumns nvarchar(max) NULL,
    ForeignKeyCount int NOT NULL,
    ColumnCount int NOT NULL
);
DECLARE @Schema sysname, @Table sysname, @ObjectId int, @Count bigint, @Sql nvarchar(max);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT s.name, t.name, t.object_id
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id=t.schema_id
    WHERE t.is_ms_shipped=0
    ORDER BY s.name,t.name;
OPEN cur;
FETCH NEXT FROM cur INTO @Schema,@Table,@ObjectId;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @Count=0;
    SET @Sql=N'SELECT @N=COUNT_BIG(*) FROM '+QUOTENAME(@Schema)+N'.'+QUOTENAME(@Table)+N';';
    EXEC sys.sp_executesql @Sql,N'@N bigint OUTPUT',@N=@Count OUTPUT;
    INSERT @Tables(SchemaName,TableName,ObjectId,RowCount,PrimaryKeyColumns,ForeignKeyCount,ColumnCount)
    SELECT @Schema,@Table,@ObjectId,@Count,
      (SELECT STRING_AGG(QUOTENAME(c.name),N', ') WITHIN GROUP(ORDER BY ic.key_ordinal)
       FROM sys.indexes i
       JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
       JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
       WHERE i.object_id=@ObjectId AND i.is_primary_key=1),
      (SELECT COUNT(*) FROM sys.foreign_keys fk WHERE fk.parent_object_id=@ObjectId OR fk.referenced_object_id=@ObjectId),
      (SELECT COUNT(*) FROM sys.columns WHERE object_id=@ObjectId);
    FETCH NEXT FROM cur INTO @Schema,@Table,@ObjectId;
END
CLOSE cur; DEALLOCATE cur;
SELECT SchemaName,TableName,RowCount,PrimaryKeyColumns,ForeignKeyCount,ColumnCount
FROM @Tables ORDER BY SchemaName,TableName;
SELECT COUNT(*) TableCount, SUM(RowCount) TotalRows,
       SUM(CASE WHEN RowCount>0 THEN 1 ELSE 0 END) NonEmptyTables
FROM @Tables;
SELECT s.name SchemaName,t.name TableName,c.name ColumnName,ty.name DataType,
       c.max_length MaxLength,c.precision,c.scale,c.is_nullable,c.is_identity
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id=t.schema_id
JOIN sys.columns c ON c.object_id=t.object_id
JOIN sys.types ty ON ty.user_type_id=c.user_type_id
WHERE t.is_ms_shipped=0
ORDER BY s.name,t.name,c.column_id;
SELECT ps.name ParentSchema,pt.name ParentTable,pc.name ParentColumn,
       rs.name ReferencedSchema,rt.name ReferencedTable,rc.name ReferencedColumn,
       fk.name ForeignKeyName
FROM sys.foreign_key_columns fkc
JOIN sys.foreign_keys fk ON fk.object_id=fkc.constraint_object_id
JOIN sys.tables pt ON pt.object_id=fkc.parent_object_id
JOIN sys.schemas ps ON ps.schema_id=pt.schema_id
JOIN sys.columns pc ON pc.object_id=pt.object_id AND pc.column_id=fkc.parent_column_id
JOIN sys.tables rt ON rt.object_id=fkc.referenced_object_id
JOIN sys.schemas rs ON rs.schema_id=rt.schema_id
JOIN sys.columns rc ON rc.object_id=rt.object_id AND rc.column_id=fkc.referenced_column_id
ORDER BY ps.name,pt.name,fk.name,fkc.constraint_column_id;
"@
    $cmd = $c.CreateCommand()
    $cmd.CommandTimeout = 600
    $cmd.CommandText = $sql
    $ds = [System.Data.DataSet]::new()
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($cmd)
    [void]$adapter.Fill($ds)
    if ($ds.Tables.Count -lt 4) { throw 'Inventory query did not return all four result sets.' }

    Log "Database: $db"
    Log "User tables: $($ds.Tables[1].Rows[0].TableCount); non-empty tables: $($ds.Tables[1].Rows[0].NonEmptyTables); total rows: $($ds.Tables[1].Rows[0].TotalRows)"
    Log 'TABLE INVENTORY (row counts, primary keys, relationships, and column metadata):'
    foreach ($r in $ds.Tables[0].Rows) {
        $pk = if ([string]::IsNullOrWhiteSpace([string]$r.PrimaryKeyColumns)) { '(NO PRIMARY KEY)' } else { [string]$r.PrimaryKeyColumns }
        Log ("{0}.{1}: rows={2}; PK={3}; FK references={4}; columns={5}" -f $r.SchemaName,$r.TableName,$r.RowCount,$pk,$r.ForeignKeyCount,$r.ColumnCount)
    }
    Log "Column metadata rows: $($ds.Tables[2].Rows.Count)"
    Log "Foreign-key relationship rows: $($ds.Tables[3].Rows.Count)"
    Log 'READ-ONLY INVENTORY COMPLETE. No rows, schemas, identities, or migration evidence were changed.'
    $lines = @('# Phase 45 — Full database table and relationship inventory','',"- Run: $(Get-Date -Format o)","- Database: $db","- User tables: $($ds.Tables[1].Rows[0].TableCount)","- Non-empty tables: $($ds.Tables[1].Rows[0].NonEmptyTables)","- Total rows across user tables: $($ds.Tables[1].Rows[0].TotalRows)",'','## Table inventory','')
    $lines += '| Schema | Table | Rows | Primary key | FK references | Columns |'
    $lines += '|---|---|---:|---|---:|---:|'
    foreach ($r in $ds.Tables[0].Rows) {
        $pk = if ([string]::IsNullOrWhiteSpace([string]$r.PrimaryKeyColumns)) { 'NO PRIMARY KEY' } else { ([string]$r.PrimaryKeyColumns).Replace('|','\|') }
        $lines += "| $($r.SchemaName) | $($r.TableName) | $($r.RowCount) | $pk | $($r.ForeignKeyCount) | $($r.ColumnCount) |"
    }
    $lines += @('','## Column inventory','')
    $lines += '| Schema | Table | Column | SQL type | Max length | Precision | Scale | Nullable | Identity |'
    $lines += '|---|---|---|---|---:|---:|---:|---|---|'
    foreach ($r in $ds.Tables[2].Rows) {
        $lines += "| $($r.SchemaName) | $($r.TableName) | $($r.ColumnName) | $($r.DataType) | $($r.MaxLength) | $($r.Precision) | $($r.Scale) | $($r.is_nullable) | $($r.is_identity) |"
    }
    $lines += @('','## Foreign-key inventory','')
    $lines += '| Parent | Parent column | Referenced table | Referenced column | Constraint |'
    $lines += '|---|---|---|---|---|'
    foreach ($r in $ds.Tables[3].Rows) {
        $lines += "| $($r.ParentSchema).$($r.ParentTable) | $($r.ParentColumn) | $($r.ReferencedSchema).$($r.ReferencedTable) | $($r.ReferencedColumn) | $($r.ForeignKeyName) |"
    }
    $lines += @('','## Migration safety','',
      '- This phase is read-only and inventories every non-system user table, including empty tables.',
      '- Row counts are exact COUNT_BIG values taken from the inspected database at execution time.',
      '- Primary keys, columns, and declared foreign-key relationships are recorded to design deterministic, relationship-safe migration.',
      '- This inventory does not claim that a table is migrated merely because it exists in the target database.',
      '- The next migration phase must classify every table as operationally migrated, preserved as immutable legacy evidence, intentionally empty, or explicitly unresolved.',
      '- No source data is updated or deleted. Do not retire the legacy database until source/target row counts, fingerprints, keys, and relationships reconcile table by table.')
    Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
    Write-Host "Report: $report"
} catch {
    Log "ERROR: $($_.Exception.Message)"
    throw
} finally {
    if ($c.State -ne 'Closed') { $c.Close() }
}
Push-Location $root
try {
    git add -- $PSCommandPath $report
    if ((git status --short -- $PSCommandPath $report).Count -gt 0) {
        git commit --only -m "feat(migration): inventory every database table and relationship" -- $PSCommandPath $report
        git push origin development/database-redesign-enterprise
    }
} finally { Pop-Location }
