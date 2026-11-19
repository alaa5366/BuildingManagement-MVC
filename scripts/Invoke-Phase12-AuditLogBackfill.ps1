[CmdletBinding()]
param(
 [switch]$Apply,
 [string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase12-AuditLog-Backfill-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$x){$log.Add($x);Write-Host $x}
function Query($c,[string]$s){
 $cmd=$c.CreateCommand();$cmd.CommandText=$s;$cmd.CommandTimeout=180
 $dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd)
 [void]$da.Fill($dt);return ,$dt
}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open()
 $db=(Query $c "SELECT DB_NAME() N").Rows[0].N
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @('AuditLog','AuditEvents','LegacyDataMigrationMap','__EFMigrationsHistory')){
  $v=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') O").Rows[0].O
  if($null -eq $v -or $v -is [DBNull]){throw "Missing table: $t"}
 }
 $m=(Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119100000_Phase09_OperationalDomainSchema'").Rows[0].N
 if([int]$m -ne 1){throw 'Phase09 is not applied'}
 $badActor=[int](Query $c "SELECT COUNT(*) N FROM dbo.AuditLog WHERE LEN(Actor)>160").Rows[0].N
 $badLabel=[int](Query $c "SELECT COUNT(*) N FROM dbo.AuditLog WHERE LEN(Label)>200").Rows[0].N
 if($badActor -gt 0 -or $badLabel -gt 0){throw "Preflight blocked: Actor over 160=$badActor; Label over 200=$badLabel. No truncation is allowed."}
 $src=[int](Query $c "SELECT COUNT(*) N FROM dbo.AuditLog").Rows[0].N
 $existing=[int](Query $c "SELECT COUNT(*) N FROM dbo.AuditEvents e INNER JOIN dbo.LegacyDataMigrationMap x ON x.TargetKey=CONVERT(nvarchar(36),e.AuditEventId) WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'AuditLog'").Rows[0].N
 $pending=[int](Query $c @"
SELECT COUNT(*) N FROM dbo.AuditLog s
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap x
 WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'AuditLog'
 AND x.SourceKey=CONVERT(nvarchar(30),s.AuditId))
"@).Rows[0].N
 Log "Database: $db";Log "Source rows: $src";Log "Already mapped: $existing";Log "Pending: $pending";Log "Actor over target limit: $badActor";Log "Label over target limit: $badLabel"
 if(-not $Apply){Log 'DRY RUN - no data changed'}
 else{
  $tx=$c.BeginTransaction()
  try{
   $sql=@"
SET NOCOUNT ON;
;WITH S AS(
 SELECT a.*,
 CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.audit.event.v1|',a.AuditId)),1,16)) EventId,
 CONVERT(nvarchar(30),a.AuditId) SourceKey,
 HASHBYTES('SHA2_256',CONCAT(a.AuditId,N'|',COALESCE(a.SourceId,N''),N'|',COALESCE(a.BuildingId,N''),N'|',CONVERT(nvarchar(33),a.Ts,126),N'|',a.Action,N'|',a.Label,N'|',a.Actor,N'|',a.ActorRole,N'|',a.MonthKey,N'|',a.Severity,N'|',COALESCE(a.BuildingNumber,N''),N'|',COALESCE(CONVERT(nvarchar(20),a.AptNumber),N''),N'|',COALESCE(a.ApartmentId,N''))) Fingerprint
 FROM dbo.AuditLog a
)
INSERT dbo.AuditEvents(
 AuditEventId,OccurredAtUtc,ActorAccountId,ActorReference,BuildingId,
 EventCategory,EventType,ResourceType,ResourceReference,CorrelationId,
 Outcome,SafeDetailsJson,PreviousHash,EventHash
)
SELECT s.EventId,s.Ts,NULL,s.Actor,s.BuildingId,
 N'Legacy',s.Action,NULL,s.SourceId,NULL,
 0,
 (SELECT s.Label AS legacyLabel,s.ActorRole AS legacyActorRole,s.Severity AS legacySeverity,
         s.MonthKey AS legacyMonthKey,s.BuildingNumber AS legacyBuildingNumber,
         s.AptNumber AS legacyApartmentNumber,s.ApartmentId AS legacyApartmentId
  FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
 NULL,s.Fingerprint
FROM S s
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap x
 WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'AuditLog'
 AND x.SourceKey=s.SourceKey);
;WITH S AS(
 SELECT a.*,
 CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.audit.event.v1|',a.AuditId)),1,16)) EventId,
 CONVERT(nvarchar(30),a.AuditId) SourceKey,
 HASHBYTES('SHA2_256',CONCAT(a.AuditId,N'|',COALESCE(a.SourceId,N''),N'|',COALESCE(a.BuildingId,N''),N'|',CONVERT(nvarchar(33),a.Ts,126),N'|',a.Action,N'|',a.Label,N'|',a.Actor,N'|',a.ActorRole,N'|',a.MonthKey,N'|',a.Severity)) Fingerprint
 FROM dbo.AuditLog a
)
INSERT dbo.LegacyDataMigrationMap(
 MigrationMapId,SourceSystem,SourceTable,SourceKey,TargetTable,TargetKey,
 SourceFingerprint,Outcome,Disposition,SafeNote
)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.audit.map.v1|',s.AuditId)),1,16)),
 N'BuildingManagement.Legacy',N'AuditLog',s.SourceKey,N'AuditEvents',
 CONVERT(nvarchar(36),s.EventId),s.Fingerprint,1,N'MigratedLegacyAudit',
 LEFT(CONCAT(N'LegacyLabel=',s.Label,N';ActorRole=',s.ActorRole,N';Severity=',s.Severity,
 N';MonthKey=',s.MonthKey,N';Details retained in source AuditLog.'),500)
FROM S s
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap x
 WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'AuditLog'
 AND x.SourceKey=s.SourceKey);
"@
   $cmd=$c.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandText=$sql;$cmd.CommandTimeout=300
   [void]$cmd.ExecuteNonQuery();$tx.Commit()
  }catch{$tx.Rollback();throw}
  $target=[int](Query $c "SELECT COUNT(*) N FROM dbo.AuditEvents e INNER JOIN dbo.LegacyDataMigrationMap x ON x.TargetKey=CONVERT(nvarchar(36),e.AuditEventId) WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'AuditLog'").Rows[0].N
  $maps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'AuditLog'").Rows[0].N
  Log "Target events: $target";Log "Provenance rows: $maps"
  if($target -ne $src -or $maps -ne $src){throw "Reconciliation failed: source=$src target=$target provenance=$maps"}
  Log 'RECONCILIATION PASS'
 }
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $mode=if($Apply){'APPLY'}else{'DRY RUN'}
 $lines=@("# Phase 12 - AuditLog Backfill","","- Run: $(Get-Date -Format o)","- Mode: $mode","- Source rows retained: dbo.AuditLog","","## Results")
 $lines+=($log|ForEach-Object{"- $_"})
 $lines+=@("","## Preservation rules",
 "- Actor is copied as a reference string only; no account identity is inferred.",
 "- Event outcome is 0 (unknown) because legacy outcome semantics are not defined.",
 "- Legacy label, role, severity, month and apartment/building context are retained in SafeDetailsJson; SourceId is retained as ResourceReference when present.",
 "- Legacy Details remains intact in the source table; it is not copied into a field labelled safe without content classification.",
 "- EventHash is the SHA-256 fingerprint of selected immutable source fields; PreviousHash remains NULL because no verified chain order/hash contract exists.",
 "- Oversized Actor or Label blocks the run; no truncation is performed.",
 "- Source rows are never updated or deleted.")
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{
 git add -- scripts/Invoke-Phase12-AuditLogBackfill.ps1 $report
 if((git status --short -- scripts/Invoke-Phase12-AuditLogBackfill.ps1 $report).Count -gt 0){
  git commit --only -m "feat: backfill legacy audit events" -- scripts/Invoke-Phase12-AuditLogBackfill.ps1 $report
  git push origin development/database-redesign-enterprise
 }
}finally{Pop-Location}
