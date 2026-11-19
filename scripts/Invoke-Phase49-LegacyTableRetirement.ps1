[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$SourceConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30",
    [string]$TargetConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30",
    [string]$BackupDirectory = (Join-Path $env:USERPROFILE "BuildingManagement-PrePhase49-Backups")
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$reportDir = Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$report = Join-Path $reportDir ("Phase49-LegacyTableRetirement-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + ".md")
$log = [System.Collections.Generic.List[string]]::new()
$blockers = [System.Collections.Generic.List[string]]::new()
function Log([string]$s) { $log.Add($s); Write-Host $s }
function Open-Checked([string]$cs,[string]$expected) {
    $c = [System.Data.SqlClient.SqlConnection]::new($cs); $c.Open()
    $cmd = $c.CreateCommand(); $cmd.CommandText = 'SELECT DB_NAME()'
    $actual = [string]$cmd.ExecuteScalar()
    if ($actual -ne $expected) { $c.Close(); throw "Expected database '$expected', connected to '$actual'." }
    return $c
}
function Query($c,[string]$sql) {
    $cmd=$c.CreateCommand(); $cmd.CommandTimeout=900; $cmd.CommandText=$sql
    $dt=[System.Data.DataTable]::new(); $da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd); [void]$da.Fill($dt)
    return ,$dt
}
function Quote([string]$n) { '[' + $n.Replace(']',']]') + ']' }
$source=$null; $target=$null; $backupPath=$null; $dropped=@(); $failure=$null
try {
    $source=Open-Checked $SourceConnectionString 'BuildingManagement'
    $target=Open-Checked $TargetConnectionString 'BuildingManagement_RestoreTest'
    $sourceTables=Query $source @"
SELECT s.name SchemaName,t.name TableName,
       SUM(ps.row_count) AS TotalRows
FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
JOIN sys.dm_db_partition_stats ps ON ps.object_id=t.object_id AND ps.index_id IN (0,1)
WHERE t.is_ms_shipped=0
GROUP BY s.name,t.name ORDER BY s.name,t.name;
"@
    $targetTables=Query $target @"
SELECT s.name SchemaName,t.name TableName,
       SUM(ps.row_count) AS TotalRows
FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
JOIN sys.dm_db_partition_stats ps ON ps.object_id=t.object_id AND ps.index_id IN (0,1)
WHERE t.is_ms_shipped=0
GROUP BY s.name,t.name ORDER BY s.name,t.name;
"@
    $targetNames=@{}; foreach($r in $targetTables.Rows){$targetNames["$($r.SchemaName).$($r.TableName)"]=$r}
    $hasMap=(Query $target "SELECT CASE WHEN OBJECT_ID(N'dbo.LegacyDataMigrationMap',N'U') IS NULL THEN 0 ELSE 1 END HasMap").Rows[0].HasMap
    if(-not $hasMap){throw 'dbo.LegacyDataMigrationMap is missing; retirement cannot be proven.'}
    $mapCounts=Query $target @"
;WITH MapRows AS
(
    SELECT m.SourceTable,
           CASE WHEN m.TargetKey IS NULL OR LEN(m.TargetKey)=0 THEN 1 ELSE 0 END AS MissingTargetKey,
           CASE WHEN EXISTS
           (
               SELECT 1
               FROM sys.tables tt
               JOIN sys.schemas ss ON ss.schema_id=tt.schema_id
               WHERE ss.name=N'dbo' AND tt.name=m.TargetTable
           ) THEN 0 ELSE 1 END AS MissingTargetTable
    FROM dbo.LegacyDataMigrationMap m
    WHERE m.SourceSystem=N'BuildingManagement.Legacy'
)
SELECT SourceTable,
       COUNT_BIG(*) AS MapRows,
       SUM(MissingTargetKey) AS MissingTargetKeys,
       SUM(MissingTargetTable) AS MissingTargetTables
FROM MapRows
GROUP BY SourceTable;
"@
    $maps=@{}; foreach($r in $mapCounts.Rows){$maps[[string]$r.SourceTable]=$r}
    $legacyToDrop=[System.Collections.Generic.List[string]]::new()
    $tableResults=[System.Collections.Generic.List[object]]::new()
    foreach($s in $sourceTables.Rows) {
        $name="$($s.SchemaName).$($s.TableName)"
        if(-not $targetNames.ContainsKey($name)) {
            $blockers.Add("$name is absent from target; source data is not represented by a same-name target table.")
            $tableResults.Add([pscustomobject]@{Table=$name;SourceRows=[long]$s.TotalRows;TargetRows=0;MapRows=0;Status='TARGET_TABLE_MISSING'})
            continue
        }
        $t=$targetNames[$name]
        $map=if($maps.ContainsKey([string]$s.TableName)){$maps[[string]$s.TableName]}else{$null}
        $mapRows=if($null -ne $map){[long]$map.MapRows}else{0}
        $status='READY_FOR_REVIEW'
        if([string]$s.TableName -eq '__EFMigrationsHistory') {
            $status='RETAIN_EF_METADATA'
        } elseif([long]$s.TotalRows -gt 0) {
            $status='BLOCKED_NONEMPTY_REQUIRES_TARGET_PAYLOAD_PROOF'
            $blockers.Add("$name contains $($s.RowCount) non-empty source rows. Migration-map counts alone cannot prove the mapped target payloads are complete and identical; table-specific target fingerprint reconciliation is required before DROP.")
        } elseif([long]$s.RowCount -gt 0 -and $mapRows -lt [long]$s.RowCount) {
            $status='BLOCKED_INCOMPLETE_ROW_PROVENANCE'
            $blockers.Add("$name has $($s.RowCount) source rows but only $mapRows migration-map rows.")
        } elseif($null -ne $map -and ([long]$map.MissingTargetKeys -gt 0 -or [long]$map.MissingTargetTables -gt 0)) {
            $status='BLOCKED_INVALID_TARGET_MAPPING'
            $blockers.Add("$name has migration-map entries with missing target keys or target tables.")
        }
        if($status -eq 'READY_FOR_REVIEW'){$legacyToDrop.Add($name)}
        $tableResults.Add([pscustomobject]@{Table=$name;SourceRows=[long]$s.RowCount;TargetRows=[long]$t.TotalRows;MapRows=$mapRows;Status=$status})
    }
    # Build FK check using a temporary table populated from the exact source-table inventory.
    $legacyValues=($sourceTables.Rows | Where-Object { $_.TableName -ne '__EFMigrationsHistory' } | ForEach-Object { "(N'$($_.SchemaName.Replace("'","''"))',N'$($_.TableName.Replace("'","''"))')" }) -join ','
    $fkSql="DECLARE @Legacy TABLE(SchemaName sysname,TableName sysname); INSERT @Legacy VALUES $legacyValues; " + @"
SELECT fk.name ForeignKeyName,ps.name ParentSchema,pt.name ParentTable,
       rs.name ReferencedSchema,rt.name ReferencedTable
FROM sys.foreign_keys fk
JOIN sys.tables pt ON pt.object_id=fk.parent_object_id JOIN sys.schemas ps ON ps.schema_id=pt.schema_id
JOIN sys.tables rt ON rt.object_id=fk.referenced_object_id JOIN sys.schemas rs ON rs.schema_id=rt.schema_id
WHERE EXISTS(SELECT 1 FROM @Legacy l WHERE l.TableName=pt.name AND l.SchemaName=ps.name)
   OR EXISTS(SELECT 1 FROM @Legacy l WHERE l.TableName=rt.name AND l.SchemaName=rs.name);
"@
    $fk=Query $target $fkSql
    foreach($f in $fk.Rows){$blockers.Add("Foreign key $($f.ForeignKeyName) still connects legacy table $($f.ParentSchema).$($f.ParentTable) to $($f.ReferencedSchema).$($f.ReferencedTable).")}
    Log "Source tables: $($sourceTables.Rows.Count); target tables: $($targetTables.Rows.Count)"
    Log "Legacy tables eligible for retirement review: $($legacyToDrop.Count)"
    Log "Foreign-key dependencies touching legacy tables: $($fk.Rows.Count)"
    Log "Safety blockers: $($blockers.Count)"
    foreach($r in $tableResults){Log ("{0}: source={1}; target={2}; map={3}; {4}" -f $r.Table,$r.SourceRows,$r.TargetRows,$r.MapRows,$r.Status)}
    foreach($b in $blockers){Log "BLOCKER: $b"}
    if($blockers.Count -gt 0){throw "Retirement stopped safely: $($blockers.Count) blocker(s). No table was dropped."}
    if(-not $Apply){Log 'DRY RUN: all structural gates passed; rerun with -Apply to create backup and retire legacy tables.'}
    else {
        New-Item -ItemType Directory -Force -Path $BackupDirectory | Out-Null
        $backupPath=Join-Path $BackupDirectory ("BuildingManagement_RestoreTest_PrePhase49_"+(Get-Date -Format 'yyyyMMdd_HHmmss')+".bak")
        $backupCmd=$target.CreateCommand()
        $backupCmd.CommandTimeout=0
        $backupCmd.CommandText="BACKUP DATABASE [BuildingManagement_RestoreTest] TO DISK = N'$($backupPath.Replace("'","''"))' WITH COPY_ONLY, CHECKSUM, INIT, STATS=10;"
        [void]$backupCmd.ExecuteNonQuery()
        $verify=Query $target "RESTORE VERIFYONLY FROM DISK=N'$($backupPath.Replace("'","''"))' WITH CHECKSUM;"
        if($null -eq $verify){throw 'Backup verification did not return successfully; no tables were dropped.'}
        # Final guard: drop only source-named tables, never target-only operational tables or EF migration history.
        $dropSql=($legacyToDrop | ForEach-Object { $parts=$_.Split('.',2); "DROP TABLE $(Quote $parts[0]).$(Quote $parts[1]);" }) -join [Environment]::NewLine
        if(-not [string]::IsNullOrWhiteSpace($dropSql)){
            $tx=$target.BeginTransaction()
            try{$cmd=$target.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandTimeout=900;$cmd.CommandText=$dropSql;[void]$cmd.ExecuteNonQuery();$tx.Commit();$dropped=@($legacyToDrop)}
            catch{$tx.Rollback();throw}
        }
        Log "Backup verified: $backupPath"
        Log "Tables dropped: $($dropped.Count)"
    }
}
catch { $failure=$_.Exception.Message; Log "ERROR: $failure" }
finally {
    if($source -and $source.State -ne 'Closed'){$source.Close()}
    if($target -and $target.State -ne 'Closed'){$target.Close()}
    $mode=if($Apply){'APPLY'}else{'DRY RUN'}
    $lines=@('# Phase 49 — Legacy table retirement','',"- Run: $(Get-Date -Format o)","- Mode: $mode",'- Scope: BuildingManagement_RestoreTest only','- Source database BuildingManagement is read-only and never modified.','', '## Table results','', '| Table | Source rows | Target rows | Migration map rows | Status |','|---|---:|---:|---:|---|')
    foreach($r in $tableResults){$lines+="| $($r.Table) | $($r.SourceRows) | $($r.TargetRows) | $($r.MapRows) | $($r.Status) |"}
    $lines+=@('','## Execution log','')
    $lines+=($log|ForEach-Object{"- $_"})
    $lines+=@('','## Backup','',"- Backup file: $(if($backupPath){$backupPath}else{'Not created'})","- Tables dropped: $($dropped.Count)",'','## Safety notes','',
      '- The script fails closed. Any missing target table, incomplete migration-map coverage, invalid target mapping, or foreign-key dependency prevents all drops.',
      '- __EFMigrationsHistory is deliberately retained as Entity Framework deployment metadata.',
      '- Target-only operational tables are never selected for deletion.',
      '- A migration-map count is a conservative gate, not a cryptographic proof of payload equality; the Phase 47 fingerprints and table-specific migration reports remain required evidence.',
      '- If any blocker is reported, resolve it before retrying. No source table or source row is ever changed.')
    Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
    Write-Host "Report: $report"
}
Push-Location $root
try {
    git add -- $PSCommandPath $report
    if((git status --short -- $PSCommandPath $report).Count -gt 0){
        git commit --only -m "feat(migration): add guarded legacy table retirement phase" -- $PSCommandPath $report
        git push origin development/database-redesign-enterprise
    }
} finally { Pop-Location }
if($failure){ throw $failure }
