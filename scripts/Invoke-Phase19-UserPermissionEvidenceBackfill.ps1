[CmdletBinding()]
param(
 [switch]$Apply,
 [string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase19-UserPermissionEvidence-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Query($c,[string]$sql){$cmd=$c.CreateCommand();$cmd.CommandTimeout=180;$cmd.CommandText=$sql;$dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt);return ,$dt}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open()
 $db=(Query $c "SELECT DB_NAME() N").Rows[0].N
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @('UserPermissions','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){
  $x=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N
  if($null -eq $x -or $x -is [DBNull]){throw "Missing dbo.$t"}
 }
 $m=[int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119190000_Phase19_UserPermissionEvidence'").Rows[0].N
 if($m -ne 1){throw 'Phase19 schema migration is not applied'}
 $bad=[int](Query $c "SELECT COUNT(*) N FROM dbo.UserPermissions WHERE Uid IS NULL OR LEN(Uid)=0 OR LEN(Uid)>128 OR Permission IS NULL OR LEN(Permission)=0 OR LEN(Permission)>256 OR LEN(Uid)+1+LEN(Permission)>256").Rows[0].N
 if($bad){throw "Preflight blocked: $bad permission rows have invalid key/value."}
 $source=[int](Query $c "SELECT COUNT(*) N FROM dbo.UserPermissions").Rows[0].N
 $pending=[int](Query $c "SELECT COUNT(*) N FROM dbo.UserPermissions s WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'UserPermissions' AND t.SourceKey=s.Uid+N'|'+s.Permission)").Rows[0].N
 Log "Database: $db";Log "Source permission rows: $source";Log "Pending: $pending"
 if(-not $Apply){Log 'DRY RUN - no data changed'}
 else{
  $tx=$c.BeginTransaction()
  try{
   $sql=@"
SET NOCOUNT ON;
;WITH S AS (
 SELECT p.*,p.Uid+N'|'+p.Permission SourceKey,
  (SELECT p.Uid,p.Permission FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) Payload
 FROM dbo.UserPermissions p
)
INSERT dbo.LegacyOperationalRecords(LegacyOperationalRecordId,SourceSystem,SourceTable,SourceKey,BuildingId,PayloadJson,SourceFingerprint)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.permission.record.v1|',s.SourceKey))),1,16)),
 N'BuildingManagement.Legacy',N'UserPermissions',s.SourceKey,NULL,s.Payload,HASHBYTES('SHA2_256',CONVERT(varbinary(max),s.Payload))
FROM S s
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'UserPermissions' AND t.SourceKey=s.SourceKey);

INSERT dbo.LegacyDataMigrationMap(MigrationMapId,SourceSystem,SourceTable,SourceKey,TargetTable,TargetKey,SourceFingerprint,Outcome,Disposition,SafeNote)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.permission.map.v1|',t.SourceKey))),1,16)),
 t.SourceSystem,t.SourceTable,t.SourceKey,N'LegacyOperationalRecords',CONVERT(nvarchar(36),t.LegacyOperationalRecordId),t.SourceFingerprint,1,N'PreservedAsLegacyPermissionEvidence',
 N'Per-user permission retained verbatim as legacy evidence; no role-level permission or account identity inferred.'
FROM dbo.LegacyOperationalRecords t
WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'UserPermissions'
AND NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=t.SourceSystem AND m.SourceTable=t.SourceTable AND m.SourceKey=t.SourceKey);
"@
   $cmd=$c.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandText=$sql;$cmd.CommandTimeout=300;[void]$cmd.ExecuteNonQuery();$tx.Commit()
  }catch{$tx.Rollback();throw}
  $target=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyOperationalRecords WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'UserPermissions'").Rows[0].N
  $maps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'UserPermissions'").Rows[0].N
  Log "Target evidence rows: $target";Log "Provenance rows: $maps"
  if($target -ne $source -or $maps -ne $source){throw "Reconciliation failed: source=$source target=$target provenance=$maps"}
  Log 'RECONCILIATION PASS'
 }
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $mode=if($Apply){'APPLY'}else{'DRY RUN'}
 $lines=@('# Phase 19 - User permission evidence backfill','',"- Run: $(Get-Date -Format o)","- Mode: $mode",'','## Results')
 $lines+=$log|%{"- $_"}
 $lines+=@('','## Permission semantics',
 '- UserPermissions are account/user-scoped legacy permission entries; they are not role-level RolePermissions.',
 '- Uid is preserved only as a legacy reference. No account/person linkage is inferred and no permissions are granted by this evidence migration.',
 '- Exact permission strings are preserved; source rows are never changed or deleted.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{$sp=Join-Path $PSScriptRoot 'Invoke-Phase19-UserPermissionEvidenceBackfill.ps1';git add -- $sp $report;if((git status --short -- $sp $report).Count -gt 0){git commit --only -m "feat: preserve legacy user permission evidence" -- $sp $report;git push origin development/database-redesign-enterprise}}finally{Pop-Location}
