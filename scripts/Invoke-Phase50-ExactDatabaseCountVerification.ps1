[CmdletBinding()]
param(
    [string]$SourceConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30",
    [string]$TargetConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$report=Join-Path $dir "Phase50-ExactDatabaseCountVerification-$stamp.md"

function Read-Database([string]$cs,[string]$expected) {
    $cn=[System.Data.SqlClient.SqlConnection]::new($cs)
    try {
        $cn.Open()
        $cmd=$cn.CreateCommand()
        $cmd.CommandText=@"
SELECT DB_NAME() AS DatabaseName,@@SERVERNAME AS ServerName,
       DB_ID() AS DatabaseId,USER_NAME() AS DatabaseUser,
       d.state_desc,d.create_date,
       (SELECT physical_name FROM sys.database_files WHERE file_id=1) AS PrimaryDataFile
FROM sys.databases d WHERE d.name=DB_NAME();
"@
        $meta=[System.Data.DataTable]::new()
        $da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd)
        [void]$da.Fill($meta)
        if($meta.Rows.Count -ne 1 -or [string]$meta.Rows[0].DatabaseName -ne $expected) {
            throw "Expected '$expected'; connected metadata did not match."
        }
        $countSql=@"
SET NOCOUNT ON;
DECLARE @R TABLE(SchemaName sysname,TableName sysname,TotalRows bigint);
DECLARE @S sysname,@T sysname,@N bigint,@Q nvarchar(max);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR
 SELECT s.name,t.name FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
 WHERE t.is_ms_shipped=0 ORDER BY s.name,t.name;
OPEN c; FETCH NEXT FROM c INTO @S,@T;
WHILE @@FETCH_STATUS=0
BEGIN
 SET @N=0;
 SET @Q=N'SELECT @X=COUNT_BIG(*) FROM '+QUOTENAME(@S)+N'.'+QUOTENAME(@T)+N';';
 EXEC sys.sp_executesql @Q,N'@X bigint OUTPUT',@X=@N OUTPUT;
 INSERT @R VALUES(@S,@T,@N);
 FETCH NEXT FROM c INTO @S,@T;
END
CLOSE c; DEALLOCATE c;
SELECT SchemaName,TableName,TotalRows FROM @R ORDER BY SchemaName,TableName;
"@
        $cmd=$cn.CreateCommand();$cmd.CommandTimeout=900;$cmd.CommandText=$countSql
        $dt=[System.Data.DataTable]::new()
        $da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd)
        [void]$da.Fill($dt)
        $total=[long]0
        foreach($r in $dt.Rows){$total += [long]$r.TotalRows}
        return [pscustomobject]@{Metadata=$meta.Rows[0];Tables=$dt;TotalRows=$total}
    } finally { if($cn.State -ne 'Closed'){$cn.Close()} }
}

$failure=$null
try {
    $src=Read-Database $SourceConnectionString 'BuildingManagement'
    $dst=Read-Database $TargetConnectionString 'BuildingManagement_RestoreTest'
    Write-Host ("SOURCE {0} on {1}: tables={2}; rows={3}; file={4}" -f $src.Metadata.DatabaseName,$src.Metadata.ServerName,$src.Tables.Rows.Count,$src.TotalRows,$src.Metadata.PrimaryDataFile)
    Write-Host ("TARGET {0} on {1}: tables={2}; rows={3}; file={4}" -f $dst.Metadata.DatabaseName,$dst.Metadata.ServerName,$dst.Tables.Rows.Count,$dst.TotalRows,$dst.Metadata.PrimaryDataFile)
    $sm=@{};foreach($r in $src.Tables.Rows){$sm["$($r.SchemaName).$($r.TableName)"]=$r}
    $tm=@{};foreach($r in $dst.Tables.Rows){$tm["$($r.SchemaName).$($r.TableName)"]=$r}
    $names=@($sm.Keys+$tm.Keys|Sort-Object -Unique)
    $lines=@('# Phase 50 — Exact source/target database count verification','',"- Run: $(Get-Date -Format o)",'- Mode: READ ONLY','',
      '## Connection identity','',
      '| Property | Source | Target |','|---|---|---|',
      "| Database | $($src.Metadata.DatabaseName) | $($dst.Metadata.DatabaseName) |",
      "| Server | $($src.Metadata.ServerName) | $($dst.Metadata.ServerName) |",
      "| Database ID | $($src.Metadata.DatabaseId) | $($dst.Metadata.DatabaseId) |",
      "| Database user | $($src.Metadata.DatabaseUser) | $($dst.Metadata.DatabaseUser) |",
      "| State | $($src.Metadata.state_desc) | $($dst.Metadata.state_desc) |",
      "| Created | $($src.Metadata.create_date) | $($dst.Metadata.create_date) |",
      "| Primary data file | $($src.Metadata.PrimaryDataFile) | $($dst.Metadata.PrimaryDataFile) |",
      "| User tables | $($src.Tables.Rows.Count) | $($dst.Tables.Rows.Count) |",
      "| Total rows (COUNT_BIG per table) | $($src.TotalRows) | $($dst.TotalRows) |",
      '','## Table-by-table exact counts','','| Table | Source rows | Target rows | Delta | Status |','|---|---:|---:|---:|---|')
    foreach($n in $names) {
        $s=$sm[$n];$t=$tm[$n]
        $sr=if($null -ne $s){[long]$s.TotalRows}else{0L}
        $tr=if($null -ne $t){[long]$t.TotalRows}else{0L}
        $status=if($null -eq $s){'TARGET_ONLY'}elseif($null -eq $t){'SOURCE_ONLY'}elseif($sr -eq $tr){'COUNT_MATCH'}else{'COUNT_MISMATCH'}
        $lines += "| $n | $sr | $tr | $($tr-$sr) | $status |"
    }
    $lines+=@('','## Safety conclusion','','- This script executes COUNT_BIG(*) against every user table independently; it does not use partition-statistics estimates.',
      '- No rows, schema, indexes, constraints, or database settings are modified.',
      '- This report is diagnostic only and does not authorize legacy-table deletion.')
    Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
    Write-Host "PHASE50 READ-ONLY VERIFICATION COMPLETE. Report: $report"
} catch { $failure=$_.Exception.Message;Write-Host "ERROR: $failure" }
finally {
    if(Test-Path $report){Push-Location $root;try{git add -- $report;if((git status --short -- $report).Count -gt 0){git commit --only -m "docs(migration): record exact source target count verification" -- $report;git push origin development/database-redesign-enterprise}}finally{Pop-Location}}
}
if($failure){throw $failure}
