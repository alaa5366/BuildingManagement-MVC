[CmdletBinding()]
param(
    [string]$SourceConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30",
    [string]$TargetConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dir = Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$report = Join-Path $dir ("Phase47-RowLevelMigrationReconciliation-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + ".md")

function Open-CheckedConnection([string]$ConnectionString,[string]$ExpectedDatabase) {
    $c = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
    $c.Open()
    $cmd = $c.CreateCommand(); $cmd.CommandText = 'SELECT DB_NAME();'
    $actual = [string]$cmd.ExecuteScalar()
    if ($actual -ne $ExpectedDatabase) {
        $c.Close()
        throw "Expected database '$ExpectedDatabase' but connected to '$actual'."
    }
    return $c
}
function Get-Metadata($Connection) {
    $cmd = $Connection.CreateCommand()
    $cmd.CommandText = @"
SELECT s.name SchemaName,t.name TableName,c.name ColumnName,ty.name DataType,
       c.max_length MaxLength,c.precision,c.scale,c.is_nullable,c.is_identity,
       CONVERT(int,CASE WHEN pk.column_id IS NULL THEN 0 ELSE ic.key_ordinal END) KeyOrdinal
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id=t.schema_id
JOIN sys.columns c ON c.object_id=t.object_id
JOIN sys.types ty ON ty.user_type_id=c.user_type_id
LEFT JOIN sys.indexes ix ON ix.object_id=t.object_id AND ix.is_primary_key=1
LEFT JOIN sys.index_columns ic ON ic.object_id=ix.object_id AND ic.index_id=ix.index_id AND ic.column_id=c.column_id
LEFT JOIN sys.columns pk ON pk.object_id=t.object_id AND pk.column_id=ic.column_id
WHERE t.is_ms_shipped=0
ORDER BY s.name,t.name,c.column_id;
"@
    $dt = [System.Data.DataTable]::new()
    $da = [System.Data.SqlClient.SqlDataAdapter]::new($cmd)
    [void]$da.Fill($dt)
    $map = @{}
    foreach ($r in $dt.Rows) {
        $name = "$($r.SchemaName).$($r.TableName)"
        if (-not $map.ContainsKey($name)) {
            $map[$name] = [pscustomobject]@{Schema=[string]$r.SchemaName;Table=[string]$r.TableName;Columns=@{};PK=[System.Collections.Generic.List[object]]::new()}
        }
        $map[$name].Columns[[string]$r.ColumnName] = $r
        if ([int]$r.KeyOrdinal -gt 0) {
            $map[$name].PK.Add([pscustomobject]@{Name=[string]$r.ColumnName;Ordinal=[int]$r.KeyOrdinal})
        }
    }
    foreach ($item in $map.Values) { $item.PK = @($item.PK | Sort-Object Ordinal) }
    return $map
}
function Get-RowHashes($Connection,$TableMeta,[string[]]$KeyColumns,[string[]]$HashColumns) {
    $q = { param([string]$n) '[' + $n.Replace(']',']]') + ']' }
    $keySelect = ($KeyColumns | ForEach-Object { 't.' + (& $q $_) }) -join ','
    $hashSelect = ($HashColumns | ForEach-Object { 't.' + (& $q $_) }) -join ','
    $schema = & $q $TableMeta.Schema
    $table = & $q $TableMeta.Table
    $sql = @"
SELECT (SELECT $keySelect FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) AS KeyJson,
       HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT $hashSelect FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES))) AS RowHash
FROM $schema.$table AS t;
"@
    $cmd = $Connection.CreateCommand()
    $cmd.CommandTimeout = 900
    $cmd.CommandText = $sql
    $reader = $cmd.ExecuteReader()
    $rows = @{}
    try {
        while ($reader.Read()) {
            $keyJson = [string]$reader['KeyJson']
            $key = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($keyJson))
            $hash = [Convert]::ToBase64String([byte[]]$reader['RowHash'])
            if ($rows.ContainsKey($key)) { throw "Duplicate primary key encountered in $($TableMeta.Schema).$($TableMeta.Table)." }
            $rows[$key] = $hash
        }
    } finally { $reader.Close() }
    return $rows
}

$sourceConnection = $null
$targetConnection = $null
try {
    Write-Host 'Opening and validating source and target databases...'
    $sourceConnection = Open-CheckedConnection $SourceConnectionString 'BuildingManagement'
    $targetConnection = Open-CheckedConnection $TargetConnectionString 'BuildingManagement_RestoreTest'
    $sourceMeta = Get-Metadata $sourceConnection
    $targetMeta = Get-Metadata $targetConnection
    $results = [System.Collections.Generic.List[object]]::new()
    $sourceTotal = [long]0; $targetTotal = [long]0
    $missingTotal = 0; $extraTotal = 0; $changedTotal = 0; $unverifiable = 0

    foreach ($name in ($sourceMeta.Keys | Sort-Object)) {
        if (-not $targetMeta.ContainsKey($name)) {
            $results.Add([pscustomobject]@{Table=$name;SourceRows=0;TargetRows=0;MatchedKeys=0;MissingInTarget=0;TargetOnly=0;ContentDifferences=0;Status='TABLE_MISSING_IN_TARGET'})
            continue
        }
        $s = $sourceMeta[$name]; $t = $targetMeta[$name]
        $sPk = @($s.PK | ForEach-Object Name)
        $tPk = @($t.PK | ForEach-Object Name)
        if ($sPk.Count -eq 0 -or $tPk.Count -eq 0 -or (($sPk -join '|') -ne ($tPk -join '|'))) {
            $unverifiable++
            $results.Add([pscustomobject]@{Table=$name;SourceRows='';TargetRows='';MatchedKeys='';MissingInTarget='';TargetOnly='';ContentDifferences='';Status='NO_COMPATIBLE_PRIMARY_KEY'})
            continue
        }
        $common = @($s.Columns.Keys | Where-Object { $t.Columns.ContainsKey($_) -and $_ -notin @('rowversion','timestamp') } | Sort-Object)
        if (@($sPk | Where-Object { $_ -notin $common }).Count -gt 0 -or $common.Count -eq 0) {
            $unverifiable++
            $results.Add([pscustomobject]@{Table=$name;SourceRows='';TargetRows='';MatchedKeys='';MissingInTarget='';TargetOnly='';ContentDifferences='';Status='NO_COMMON_HASHABLE_COLUMNS'})
            continue
        }
        $sourceRows = Get-RowHashes $sourceConnection $s $sPk $common
        $targetRows = Get-RowHashes $targetConnection $t $tPk $common
        $missing = 0; $extra = 0; $changed = 0; $matched = 0
        foreach ($key in $sourceRows.Keys) {
            if (-not $targetRows.ContainsKey($key)) { $missing++ }
            else {
                $matched++
                if ($sourceRows[$key] -ne $targetRows[$key]) { $changed++ }
            }
        }
        foreach ($key in $targetRows.Keys) { if (-not $sourceRows.ContainsKey($key)) { $extra++ } }
        $status = if ($missing -eq 0 -and $changed -eq 0) { if ($extra -eq 0) {'EXACT_SOURCE_RECONCILIATION'} else {'SOURCE_PRESERVED_WITH_TARGET_ADDITIONS'} }
                  else {'SOURCE_DATA_MISSING_OR_CHANGED'}
        $results.Add([pscustomobject]@{Table=$name;SourceRows=$sourceRows.Count;TargetRows=$targetRows.Count;MatchedKeys=$matched;MissingInTarget=$missing;TargetOnly=$extra;ContentDifferences=$changed;Status=$status})
        $sourceTotal += $sourceRows.Count; $targetTotal += $targetRows.Count
        $missingTotal += $missing; $extraTotal += $extra; $changedTotal += $changed
    }

    Write-Host 'ROW-LEVEL RECONCILIATION (primary keys + SHA-256 over common columns):'
    foreach ($r in $results) {
        Write-Host ("{0}: source={1}; target={2}; matched={3}; missing={4}; target-only={5}; changed={6}; {7}" -f $r.Table,$r.SourceRows,$r.TargetRows,$r.MatchedKeys,$r.MissingInTarget,$r.TargetOnly,$r.ContentDifferences,$r.Status)
    }
    Write-Host "Totals: missing source rows=$missingTotal; content differences=$changedTotal; target-only rows=$extraTotal; unverifiable tables=$unverifiable"

    $lines = @(
        '# Phase 47 — Row-level source/target migration reconciliation','',
        "- Run: $(Get-Date -Format o)",
        '- Source: BuildingManagement',
        '- Target: BuildingManagement_RestoreTest',
        '- Comparison: primary-key identity plus SHA-256 of a deterministic SQL JSON projection over columns shared by both schemas.',
        '- System-generated rowversion/timestamp columns are excluded from content fingerprints.',
        '- No row payloads, personal data, keys, or hashes are written to this report.',
        '- This phase is read-only; neither database is modified.',
        '',
        '## Results','',
        '| Table | Source rows | Target rows | Matched keys | Missing in target | Target-only | Content differences | Status |',
        '|---|---:|---:|---:|---:|---:|---:|---|'
    )
    foreach ($r in $results) {
        $lines += "| $($r.Table) | $($r.SourceRows) | $($r.TargetRows) | $($r.MatchedKeys) | $($r.MissingInTarget) | $($r.TargetOnly) | $($r.ContentDifferences) | $($r.Status) |"
    }
    $lines += @('','## Aggregate findings','',
        "- Missing source rows in target: $missingTotal",
        "- Content differences on matched primary keys: $changedTotal",
        "- Additional target rows: $extraTotal",
        "- Tables not row-level verifiable due to primary-key/schema limitations: $unverifiable",
        '',
        '## Important limitations','',
        '- Hashes cover columns shared by name; schema-only target columns are not part of the source-to-target content hash.',
        '- Different SQL types or serialization semantics on shared columns may require table-specific normalization before treating a hash difference as a true data change.',
        '- Additional target rows are not automatically defects; they may be newly created target records.',
        '- __EFMigrationsHistory naturally contains target migration rows absent from the legacy source and must be classified separately.',
        '- No source records are updated or deleted. Do not retire the legacy database until every source table is reconciled and all differences are classified.'
    )
    Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
    Write-Host "READ-ONLY ROW-LEVEL RECONCILIATION COMPLETE. Report: $report"
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)"
    throw
}
finally {
    if ($null -ne $sourceConnection -and $sourceConnection.State -ne 'Closed') { $sourceConnection.Close() }
    if ($null -ne $targetConnection -and $targetConnection.State -ne 'Closed') { $targetConnection.Close() }
}
Push-Location $root
try {
    git add -- $PSCommandPath $report
    if ((git status --short -- $PSCommandPath $report).Count -gt 0) {
        git commit --only -m "feat(migration): reconcile legacy rows by key and fingerprint" -- $PSCommandPath $report
        git push origin development/database-redesign-enterprise
    }
}
finally { Pop-Location }
