[CmdletBinding()]
param(
    [string]$SourceConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30",
    [string]$TargetConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dir = Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$report = Join-Path $dir "Phase46-SourceTargetDatabaseComparison-$stamp.md"

function Get-DatabaseInventory([string]$ConnectionString, [string]$ExpectedDatabase) {
    $connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
    try {
        $connection.Open()
        $dbCommand = $connection.CreateCommand()
        $dbCommand.CommandText = 'SELECT DB_NAME();'
        $database = [string]$dbCommand.ExecuteScalar()
        if ($database -ne $ExpectedDatabase) {
            throw "Expected database '$ExpectedDatabase' but connected to '$database'."
        }

        $sql = @"
SET NOCOUNT ON;
DECLARE @Tables TABLE(
    SchemaName sysname NOT NULL,
    TableName sysname NOT NULL,
    TotalRowCount bigint NOT NULL,
    PrimaryKeyColumns nvarchar(max) NULL,
    ForeignKeyCount int NOT NULL,
    ColumnCount int NOT NULL
);
DECLARE @Schema sysname, @Table sysname, @Count bigint, @Sql nvarchar(max);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT s.name,t.name
    FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
    WHERE t.is_ms_shipped=0 ORDER BY s.name,t.name;
OPEN cur;
FETCH NEXT FROM cur INTO @Schema,@Table;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @Count=0;
    SET @Sql=N'SELECT @N=COUNT_BIG(*) FROM '+QUOTENAME(@Schema)+N'.'+QUOTENAME(@Table)+N';';
    EXEC sys.sp_executesql @Sql,N'@N bigint OUTPUT',@N=@Count OUTPUT;
    INSERT @Tables(SchemaName,TableName,TotalRowCount,PrimaryKeyColumns,ForeignKeyCount,ColumnCount)
    SELECT @Schema,@Table,@Count,
      (SELECT STRING_AGG(QUOTENAME(c.name),N', ') WITHIN GROUP(ORDER BY ic.key_ordinal)
       FROM sys.indexes i
       JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
       JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
       WHERE i.object_id=OBJECT_ID(QUOTENAME(@Schema)+N'.'+QUOTENAME(@Table)) AND i.is_primary_key=1),
      (SELECT COUNT(*) FROM sys.foreign_keys fk
       WHERE fk.parent_object_id=OBJECT_ID(QUOTENAME(@Schema)+N'.'+QUOTENAME(@Table))
          OR fk.referenced_object_id=OBJECT_ID(QUOTENAME(@Schema)+N'.'+QUOTENAME(@Table))),
      (SELECT COUNT(*) FROM sys.columns
       WHERE object_id=OBJECT_ID(QUOTENAME(@Schema)+N'.'+QUOTENAME(@Table)));
    FETCH NEXT FROM cur INTO @Schema,@Table;
END
CLOSE cur; DEALLOCATE cur;

SELECT SchemaName,TableName,TotalRowCount,PrimaryKeyColumns,ForeignKeyCount,ColumnCount
FROM @Tables ORDER BY SchemaName,TableName;
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
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 900
        $command.CommandText = $sql
        $dataSet = [System.Data.DataSet]::new()
        $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
        [void]$adapter.Fill($dataSet)
        if ($dataSet.Tables.Count -lt 3) {
            throw "Inventory query for '$database' did not return all result sets."
        }
        return [pscustomobject]@{
            Database = $database
            Tables = $dataSet.Tables[0]
            Columns = $dataSet.Tables[1]
            ForeignKeys = $dataSet.Tables[2]
        }
    }
    finally {
        if ($connection.State -ne 'Closed') { $connection.Close() }
    }
}

try {
    Write-Host 'Reading legacy source database (read-only)...'
    $source = Get-DatabaseInventory $SourceConnectionString 'BuildingManagement'
    Write-Host 'Reading new target database (read-only)...'
    $target = Get-DatabaseInventory $TargetConnectionString 'BuildingManagement_RestoreTest'

    $sourceMap = @{}
    foreach ($r in $source.Tables.Rows) { $sourceMap["$($r.SchemaName).$($r.TableName)"] = $r }
    $targetMap = @{}
    foreach ($r in $target.Tables.Rows) { $targetMap["$($r.SchemaName).$($r.TableName)"] = $r }

    $names = @($sourceMap.Keys + $targetMap.Keys | Sort-Object -Unique)
    $comparison = [System.Collections.Generic.List[object]]::new()
    $sourceRows = [long]0
    $targetRows = [long]0
    $sourceOnly = 0
    $targetOnly = 0
    $rowCountMismatch = 0
    foreach ($name in $names) {
        $s = $sourceMap[$name]
        $t = $targetMap[$name]
        $sExists = $null -ne $s
        $tExists = $null -ne $t
        $sr = if ($sExists) { [long]$s.TotalRowCount } else { [long]0 }
        $tr = if ($tExists) { [long]$t.TotalRowCount } else { [long]0 }
        if ($sExists) { $sourceRows += $sr } else { $targetOnly++ }
        if ($tExists) { $targetRows += $tr } else { $sourceOnly++ }
        if ($sExists -and $tExists -and $sr -ne $tr) { $rowCountMismatch++ }
        $status = if (-not $tExists) { 'SOURCE_ONLY_MISSING_IN_TARGET' }
                  elseif (-not $sExists) { 'TARGET_ONLY' }
                  elseif ($sr -ne $tr) { 'ROW_COUNT_MISMATCH' }
                  elseif ([string]$s.PrimaryKeyColumns -ne [string]$t.PrimaryKeyColumns -or
                          [int]$s.ColumnCount -ne [int]$t.ColumnCount) { 'SCHEMA_SHAPE_DIFFERS' }
                  else { 'ROW_COUNT_MATCH_SCHEMA_BASIC_MATCH' }
        $comparison.Add([pscustomobject]@{
            Name=$name; SourceExists=$sExists; SourceRows=$sr; TargetExists=$tExists; TargetRows=$tr
            Delta=($tr-$sr); SourcePK=$(if($sExists){[string]$s.PrimaryKeyColumns}else{''})
            TargetPK=$(if($tExists){[string]$t.PrimaryKeyColumns}else{''})
            SourceColumns=$(if($sExists){[int]$s.ColumnCount}else{0})
            TargetColumns=$(if($tExists){[int]$t.ColumnCount}else{0})
            SourceFK=$(if($sExists){[int]$s.ForeignKeyCount}else{0})
            TargetFK=$(if($tExists){[int]$t.ForeignKeyCount}else{0})
            Status=$status
        })
    }

    Write-Host "SOURCE: $($source.Database); tables=$($source.Tables.Rows.Count); rows=$sourceRows"
    Write-Host "TARGET: $($target.Database); tables=$($target.Tables.Rows.Count); rows=$targetRows"
    Write-Host "Source-only tables: $sourceOnly; target-only tables: $targetOnly; shared tables with row-count differences: $rowCountMismatch"
    Write-Host 'TABLE-BY-TABLE COMPARISON:'
    foreach ($r in $comparison) {
        Write-Host ("{0}: source={1} ({2}), target={3} ({4}), delta={5}, status={6}" -f
            $r.Name,$r.SourceRows,$r.SourceExists,$r.TargetRows,$r.TargetExists,$r.Delta,$r.Status)
    }

    $lines = @(
        '# Phase 46 — Legacy source versus new target database comparison','',
        "- Run: $(Get-Date -Format o)",
        "- Source database: $($source.Database)",
        "- Target database: $($target.Database)",
        "- Source user tables: $($source.Tables.Rows.Count)",
        "- Target user tables: $($target.Tables.Rows.Count)",
        "- Source total rows: $sourceRows",
        "- Target total rows: $targetRows",
        "- Source-only tables missing in target: $sourceOnly",
        "- Target-only tables: $targetOnly",
        "- Shared tables with row-count differences: $rowCountMismatch",
        '',
        '## Table-by-table comparison','',
        '| Table | Source rows | Target rows | Delta (target-source) | Source PK | Target PK | Source columns | Target columns | Source FK refs | Target FK refs | Status |',
        '|---|---:|---:|---:|---|---|---:|---:|---:|---:|---|'
    )
    foreach ($r in $comparison) {
        $spk = $r.SourcePK.Replace('|','\|')
        $tpk = $r.TargetPK.Replace('|','\|')
        $lines += "| $($r.Name) | $($r.SourceRows) | $($r.TargetRows) | $($r.Delta) | $spk | $tpk | $($r.SourceColumns) | $($r.TargetColumns) | $($r.SourceFK) | $($r.TargetFK) | $($r.Status) |"
    }

    $lines += @('','## Source-only tables','')
    foreach ($r in $comparison | Where-Object { -not $_.TargetExists }) { $lines += "- $($r.Name): $($r.SourceRows) rows" }
    $lines += @('','## Column metadata differences','')
    $columnKeys = @{}
    foreach ($r in $source.Columns.Rows) {
        $key = "$($r.SchemaName).$($r.TableName).$($r.ColumnName)"
        $columnKeys[$key] = [pscustomobject]@{Source=$r;Target=$null}
    }
    foreach ($r in $target.Columns.Rows) {
        $key = "$($r.SchemaName).$($r.TableName).$($r.ColumnName)"
        if (-not $columnKeys.ContainsKey($key)) { $columnKeys[$key] = [pscustomobject]@{Source=$null;Target=$r} }
        else { $columnKeys[$key].Target = $r }
    }
    $columnDiffs = [System.Collections.Generic.List[string]]::new()
    foreach ($key in ($columnKeys.Keys | Sort-Object)) {
        $pair = $columnKeys[$key]
        if ($null -eq $pair.Source) { $columnDiffs.Add("| $key | (missing) | $($pair.Target.DataType)($($pair.Target.MaxLength)) | target-only |") ; continue }
        if ($null -eq $pair.Target) { $columnDiffs.Add("| $key | $($pair.Source.DataType)($($pair.Source.MaxLength)) | (missing) | source-only |"); continue }
        $a = $pair.Source; $b = $pair.Target
        $sigA = "$($a.DataType)|$($a.MaxLength)|$($a.precision)|$($a.scale)|$($a.is_nullable)|$($a.is_identity)"
        $sigB = "$($b.DataType)|$($b.MaxLength)|$($b.precision)|$($b.scale)|$($b.is_nullable)|$($b.is_identity)"
        if ($sigA -ne $sigB) {
            $columnDiffs.Add("| $key | $($a.DataType)($($a.MaxLength)), p$($a.precision), s$($a.scale), nullable=$($a.is_nullable), identity=$($a.is_identity) | $($b.DataType)($($b.MaxLength)), p$($b.precision), s$($b.scale), nullable=$($b.is_nullable), identity=$($b.is_identity) | definition differs |")
        }
    }
    $lines += @('| Column | Source definition | Target definition | Difference |','|---|---|---|---|')
    if ($columnDiffs.Count -eq 0) { $lines += '| (none) | | | No column metadata differences |' }
    else { $lines += $columnDiffs }

    $lines += @('','## Foreign-key relationship counts','',
        "- Source FK relationship rows: $($source.ForeignKeys.Rows.Count)",
        "- Target FK relationship rows: $($target.ForeignKeys.Rows.Count)",
        '',
        '## Interpretation and safety','',
        '- This phase is strictly read-only. It does not modify either database.',
        '- A matching row count is not proof of matching records. This report does not yet compare primary-key sets or row fingerprints.',
        '- TARGET_ONLY tables may be newly introduced target features and are not automatically migration defects.',
        '- SOURCE_ONLY tables and every row-count/schema difference require classification and a dedicated migration or immutable archive strategy.',
        '- Do not retire or delete the legacy source database based on this report alone.',
        '- The next reconciliation phase must compare stable source keys and SHA-256 row fingerprints, including child relationships and records preserved as immutable legacy evidence.',
        '- The source database is treated as migration-only. All source records must be represented in the new target either operationally or as immutable, provenance-preserving evidence.'
    )
    Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
    Write-Host "READ-ONLY SOURCE/TARGET COMPARISON COMPLETE. Report: $report"
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)"
    throw
}
Push-Location $root
try {
    git add -- $PSCommandPath $report
    if ((git status --short -- $PSCommandPath $report).Count -gt 0) {
        git commit --only -m "feat(migration): compare legacy source and target databases" -- $PSCommandPath $report
        git push origin development/database-redesign-enterprise
    }
}
finally { Pop-Location }
