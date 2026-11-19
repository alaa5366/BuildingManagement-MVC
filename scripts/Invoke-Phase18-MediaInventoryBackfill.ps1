[CmdletBinding()]
param(
 [switch]$Apply,
 [string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase18-MediaInventory-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Query($c,[string]$sql){$cmd=$c.CreateCommand();$cmd.CommandTimeout=180;$cmd.CommandText=$sql;$dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt);return ,$dt}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open()
 $db=(Query $c "SELECT DB_NAME() N").Rows[0].N
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @('Dvrs','Cameras','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){
  $x=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N
  if($null -eq $x -or $x -is [DBNull]){throw "Missing dbo.$t"}
 }
 $m=[int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119180000_Phase18_MediaInventoryEvidence'").Rows[0].N
 if($m -ne 1){throw 'Phase18 schema migration is not applied'}
 $bad=[int](Query $c @"
SELECT
 (SELECT COUNT(*) FROM dbo.Dvrs WHERE Id IS NULL OR LEN(Id)=0 OR LEN(Id)>256 OR LEN(BuildingId)>64 OR LEN(Name)>500 OR LEN(IpAddress)>500 OR Port<1 OR Port>65535)
 +(SELECT COUNT(*) FROM dbo.Cameras WHERE Id IS NULL OR LEN(Id)=0 OR LEN(Id)>256 OR LEN(DvrId)>256 OR LEN(BuildingId)>64 OR LEN(Name)>500 OR Channel<0) N;
"@).Rows[0].N
 if($bad){throw "Preflight blocked: $bad media records have invalid keys or values."}
 $dvr=[int](Query $c "SELECT COUNT(*) N FROM dbo.Dvrs").Rows[0].N
 $cam=[int](Query $c "SELECT COUNT(*) N FROM dbo.Cameras").Rows[0].N
 $dvrPending=[int](Query $c "SELECT COUNT(*) N FROM dbo.Dvrs s WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'Dvrs' AND t.SourceKey=s.Id)").Rows[0].N
 $camPending=[int](Query $c "SELECT COUNT(*) N FROM dbo.Cameras s WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'Cameras' AND t.SourceKey=s.Id)").Rows[0].N
 Log "Database: $db";Log "DVR source rows: $dvr (pending $dvrPending)";Log "Camera source rows: $cam (pending $camPending)"
 if(-not $Apply){Log 'DRY RUN - no data changed'}
 else{
  $tx=$c.BeginTransaction()
  try{
   $sql=@"
SET NOCOUNT ON;
;WITH D AS (
 SELECT d.*, (SELECT d.Id,d.BuildingId,d.Name,d.IpAddress,d.Port,d.Brand,d.IsActive,d.CreatedAt,d.UpdatedAt
 FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) Payload
 FROM dbo.Dvrs d
)
INSERT dbo.LegacyOperationalRecords(LegacyOperationalRecordId,SourceSystem,SourceTable,SourceKey,BuildingId,PayloadJson,SourceFingerprint)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.media.dvr.v1|',d.Id))),1,16)),
 N'BuildingManagement.Legacy',N'Dvrs',d.Id,d.BuildingId,d.Payload,
 HASHBYTES('SHA2_256',CONVERT(varbinary(max),d.Payload))
FROM D d
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'Dvrs' AND t.SourceKey=d.Id);

;WITH C AS (
 SELECT c.*, (SELECT c.Id,c.DvrId,c.BuildingId,c.Name,c.Channel,c.IsActive,c.CreatedAt
 FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) Payload
 FROM dbo.Cameras c
)
INSERT dbo.LegacyOperationalRecords(LegacyOperationalRecordId,SourceSystem,SourceTable,SourceKey,BuildingId,PayloadJson,SourceFingerprint)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.media.camera.v1|',c.Id))),1,16)),
 N'BuildingManagement.Legacy',N'Cameras',c.Id,c.BuildingId,c.Payload,
 HASHBYTES('SHA2_256',CONVERT(varbinary(max),c.Payload))
FROM C c
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'Cameras' AND t.SourceKey=c.Id);

INSERT dbo.LegacyDataMigrationMap(MigrationMapId,SourceSystem,SourceTable,SourceKey,TargetTable,TargetKey,SourceFingerprint,Outcome,Disposition,SafeNote)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.media.map.v1|',t.SourceTable,N'|',t.SourceKey))),1,16)),
 t.SourceSystem,t.SourceTable,t.SourceKey,N'LegacyOperationalRecords',CONVERT(nvarchar(36),t.LegacyOperationalRecordId),t.SourceFingerprint,1,N'PreservedAsLegacyMediaInventory',
 CASE WHEN t.SourceTable=N'Dvrs' THEN N'Encrypted password and username intentionally excluded; credential rotation and vault re-provisioning required.'
 ELSE N'RTSP/HLS URLs excluded because they may embed credentials; camera topology and non-secret metadata retained.' END
FROM dbo.LegacyOperationalRecords t
WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable IN(N'Dvrs',N'Cameras')
AND NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=t.SourceSystem AND m.SourceTable=t.SourceTable AND m.SourceKey=t.SourceKey);
"@
   $cmd=$c.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandText=$sql;$cmd.CommandTimeout=300;[void]$cmd.ExecuteNonQuery();$tx.Commit()
  }catch{$tx.Rollback();throw}
  $dvrTarget=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyOperationalRecords WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'Dvrs'").Rows[0].N
  $camTarget=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyOperationalRecords WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'Cameras'").Rows[0].N
  $maps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable IN(N'Dvrs',N'Cameras')").Rows[0].N
  Log "DVR evidence rows: $dvrTarget";Log "Camera evidence rows: $camTarget";Log "Provenance rows: $maps"
  if($dvrTarget -ne $dvr -or $camTarget -ne $cam -or $maps -ne ($dvr+$cam)){throw 'Reconciliation failed.'}
  Log 'RECONCILIATION PASS'
 }
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $mode=if($Apply){'APPLY'}else{'DRY RUN'}
 $lines=@('# Phase 18 - DVR and camera inventory backfill','',"- Run: $(Get-Date -Format o)","- Mode: $mode",'','## Results')
 $lines+=$log|%{"- $_"}
 $lines+=@('','## Credential protection',
 '- DVR encrypted passwords and usernames are not copied into the new inventory; configure a supported secrets vault and rotate credentials before enabling new media access.',
 '- Camera RTSP/HLS URLs are not copied because connection strings can embed credentials.',
 '- Camera and DVR topology metadata is retained as JSON with SHA-256 fingerprints.',
 '- No MediaCredentialReferences or MediaAccessSessions are fabricated; no source row is changed or deleted.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{$sp=Join-Path $PSScriptRoot 'Invoke-Phase18-MediaInventoryBackfill.ps1';git add -- $sp $report;if((git status --short -- $sp $report).Count -gt 0){git commit --only -m "feat: preserve legacy DVR and camera inventory" -- $sp $report;git push origin development/database-redesign-enterprise}}finally{Pop-Location}
