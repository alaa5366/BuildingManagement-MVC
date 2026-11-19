[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase11-Maintenance-Backfill-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$x){$log.Add($x);Write-Host $x}
function Query($c,[string]$s){
    $cmd=$c.CreateCommand();$cmd.CommandText=$s;$cmd.CommandTimeout=180
    $dt=[System.Data.DataTable]::new()
    $da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd)
    [void]$da.Fill($dt)
    return ,$dt
}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
    $c.Open()
    $db=(Query $c "SELECT DB_NAME() N").Rows[0].N
    if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
    foreach($t in @('MaintenanceRecords','MaintenanceRequests','LegacyDataMigrationMap','__EFMigrationsHistory')){
        $v=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') O").Rows[0].O
        if($null -eq $v -or $v -is [DBNull]){throw "Missing table: $t"}
    }
    $m=(Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119100000_Phase09_OperationalDomainSchema'").Rows[0].N
    if([int]$m -ne 1){throw 'Phase09 is not applied'}
    $tooLong=[int](Query $c "SELECT COUNT(*) N FROM dbo.MaintenanceRecords WHERE LEN(Title)>200").Rows[0].N
    if($tooLong -gt 0){throw "Preflight blocked: $tooLong legacy titles exceed target Title length 200; no truncation is allowed."}
    $src=[int](Query $c "SELECT COUNT(*) N FROM dbo.MaintenanceRecords").Rows[0].N
    $existing=[int](Query $c "SELECT COUNT(*) N FROM dbo.MaintenanceRequests WHERE LegacyMaintenanceId IS NOT NULL").Rows[0].N
    $pending=[int](Query $c @"
SELECT COUNT(*) N
FROM dbo.MaintenanceRecords s
WHERE NOT EXISTS(
 SELECT 1 FROM dbo.MaintenanceRequests t
 WHERE t.BuildingId=s.BuildingId AND t.LegacyMaintenanceId=s.Id
)
"@).Rows[0].N
    Log "Database: $db";Log "Source rows: $src";Log "Existing target legacy rows: $existing";Log "Pending: $pending";Log "Titles over target limit: $tooLong"
    if(-not $Apply){Log 'DRY RUN - no data changed'}
    else{
        $tx=$c.BeginTransaction()
        try{
            $sql=@"
SET NOCOUNT ON;
;WITH S AS (
 SELECT m.*,
 CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.maintenance.request.v1|',m.BuildingId,N'|',m.Id)),1,16)) RequestId,
 CONCAT(m.BuildingId,N'/',m.Id) SourceKey,
 HASHBYTES('SHA2_256',CONCAT(m.BuildingId,N'|',m.Id,N'|',m.Title,N'|',m.Description,N'|',CONVERT(nvarchar(10),m.MaintenanceDate,23),N'|',CONVERT(nvarchar(40),m.Cost),N'|',m.Vendor,N'|',m.Status,N'|',m.CreatedBy,N'|',CONVERT(nvarchar(33),m.CreatedAt,126),N'|',CONVERT(nvarchar(1),m.AddedToExpense),N'|',COALESCE(CONVERT(nvarchar(33),m.AddedToExpenseAt,126),N''))) Fingerprint
 FROM dbo.MaintenanceRecords m
)
INSERT dbo.MaintenanceRequests(
 RequestId,BuildingId,ApartmentId,LegacyMaintenanceId,Title,Description,Category,
 Status,Priority,RequestedByPersonId,RequestedByReference,CreatedAtUtc,UpdatedAtUtc,ClosedAtUtc
)
SELECT s.RequestId,s.BuildingId,NULL,s.Id,s.Title,s.Description,NULL,
 0,0,NULL,s.CreatedBy,s.CreatedAt,NULL,NULL
FROM S s
WHERE NOT EXISTS(
 SELECT 1 FROM dbo.MaintenanceRequests t
 WHERE t.BuildingId=s.BuildingId AND t.LegacyMaintenanceId=s.Id
);
;WITH S AS (
 SELECT m.*,
 CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.maintenance.request.v1|',m.BuildingId,N'|',m.Id)),1,16)) RequestId,
 CONCAT(m.BuildingId,N'/',m.Id) SourceKey,
 HASHBYTES('SHA2_256',CONCAT(m.BuildingId,N'|',m.Id,N'|',m.Title,N'|',m.Description,N'|',CONVERT(nvarchar(10),m.MaintenanceDate,23),N'|',CONVERT(nvarchar(40),m.Cost),N'|',m.Vendor,N'|',m.Status,N'|',m.CreatedBy,N'|',CONVERT(nvarchar(33),m.CreatedAt,126),N'|',CONVERT(nvarchar(1),m.AddedToExpense),N'|',COALESCE(CONVERT(nvarchar(33),m.AddedToExpenseAt,126),N''))) Fingerprint
 FROM dbo.MaintenanceRecords m
)
INSERT dbo.LegacyDataMigrationMap(
 MigrationMapId,SourceSystem,SourceTable,SourceKey,TargetTable,TargetKey,
 SourceFingerprint,Outcome,Disposition,SafeNote
)
SELECT
 CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.maintenance.map.v1|',s.BuildingId,N'|',s.Id)),1,16)),
 N'BuildingManagement.Legacy',N'MaintenanceRecords',s.SourceKey,
 N'MaintenanceRequests',CONVERT(nvarchar(36),s.RequestId),s.Fingerprint,1,
 N'MigratedWithLegacyFields',
 LEFT(CONCAT(N'LegacyStatus=',s.Status,N';MaintenanceDate=',CONVERT(nvarchar(10),s.MaintenanceDate,23),
 N';Cost=',CONVERT(nvarchar(40),s.Cost),N';Vendor=',s.Vendor,
 N';AddedToExpense=',CONVERT(nvarchar(1),s.AddedToExpense),
 N';AddedToExpenseAt=',COALESCE(CONVERT(nvarchar(33),s.AddedToExpenseAt,126),N'NULL')),500)
FROM S s
WHERE NOT EXISTS(
 SELECT 1 FROM dbo.LegacyDataMigrationMap x
 WHERE x.SourceSystem=N'BuildingManagement.Legacy'
 AND x.SourceTable=N'MaintenanceRecords' AND x.SourceKey=s.SourceKey
);
"@
            $cmd=$c.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandText=$sql;$cmd.CommandTimeout=300
            [void]$cmd.ExecuteNonQuery()
            $tx.Commit()
        }catch{$tx.Rollback();throw}
        $after=[int](Query $c "SELECT COUNT(*) N FROM dbo.MaintenanceRequests WHERE LegacyMaintenanceId IS NOT NULL").Rows[0].N
        $maps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'MaintenanceRecords'").Rows[0].N
        Log "Target rows: $after";Log "Provenance rows: $maps"
        if($after -ne $src -or $maps -ne $src){throw "Reconciliation failed: source=$src target=$after provenance=$maps"}
        Log 'RECONCILIATION PASS'
    }
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
    if($c.State -ne 'Closed'){$c.Close()}
    $mode=if($Apply){'APPLY'}else{'DRY RUN'}
    $lines=@("# Phase 11 - Maintenance Backfill","","- Run: $(Get-Date -Format o)","- Mode: $mode","- Source rows retained: dbo.MaintenanceRecords","","## Results")
    $lines+=($log|ForEach-Object{"- $_"})
    $lines+=@("","## Mapping and preservation rules",
    "- Request IDs are deterministic from the composite legacy key BuildingId/Id.",
    "- Legacy CreatedBy is retained as RequestedByReference; no person identity is inferred.",
    "- Status and Priority are initialized to 0 (unclassified); original Status, MaintenanceDate, Cost, Vendor, AddedToExpense and AddedToExpenseAt are preserved in LegacyDataMigrationMap.SafeNote.",
    "- Source titles longer than the target's 200-character limit block the run; no truncation is performed.",
    "- Source rows are never updated or deleted.")
    Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
    Write-Host "Report: $report"
}
Push-Location $root
try{
    git add -- scripts/Invoke-Phase11-MaintenanceBackfill.ps1 $report
    if((git status --short -- scripts/Invoke-Phase11-MaintenanceBackfill.ps1 $report).Count -gt 0){
        git commit --only -m "feat: backfill legacy maintenance records" -- scripts/Invoke-Phase11-MaintenanceBackfill.ps1 $report
        git push origin development/database-redesign-enterprise
    }
}finally{Pop-Location}
