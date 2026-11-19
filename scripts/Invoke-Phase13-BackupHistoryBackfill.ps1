[CmdletBinding()]
param(
 [switch]$Apply,
 [string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase13-BackupHistory-Backfill-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
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
 foreach($t in @('BackupHistory','BackupJobs','LegacyDataMigrationMap','__EFMigrationsHistory')){
  $v=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') O").Rows[0].O
  if($null -eq $v -or $v -is [DBNull]){throw "Missing table: $t"}
 }
 $m=(Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119100000_Phase09_OperationalDomainSchema'").Rows[0].N
 if([int]$m -ne 1){throw 'Phase09 is not applied'}
 $badSize=[int](Query $c "SELECT COUNT(*) N FROM dbo.BackupHistory WHERE FileSize<0").Rows[0].N
 $badDuration=[int](Query $c "SELECT COUNT(*) N FROM dbo.BackupHistory WHERE DurationSeconds<0").Rows[0].N
 if($badSize -gt 0 -or $badDuration -gt 0){throw "Preflight blocked: negative file sizes=$badSize; negative durations=$badDuration"}
 $src=[int](Query $c "SELECT COUNT(*) N FROM dbo.BackupHistory").Rows[0].N
 $existing=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'BackupHistory'").Rows[0].N
 $pending=[int](Query $c @"
SELECT COUNT(*) N FROM dbo.BackupHistory s
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap x
 WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'BackupHistory'
 AND x.SourceKey=s.Id)
"@).Rows[0].N
 Log "Database: $db";Log "Source rows: $src";Log "Already mapped: $existing";Log "Pending: $pending"
 if(-not $Apply){Log 'DRY RUN - no data changed'}
 else{
  $tx=$c.BeginTransaction()
  try{
   $sql=@"
SET NOCOUNT ON;
;WITH S AS(
 SELECT b.*,
 CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.backup.job.v1|',b.Id)),1,16)) JobId,
 HASHBYTES('SHA2_256',CONCAT(b.Id,N'|',b.FileName,N'|',b.FileSize,N'|',b.CollectionsJson,N'|',b.TotalDocuments,N'|',CONVERT(nvarchar(33),b.CreatedAt,126),N'|',CONVERT(nvarchar(50),b.DurationSeconds),N'|',b.Status,N'|',b.Source,N'|',COALESCE(b.ErrorMessage,N''))) Fingerprint
 FROM dbo.BackupHistory b
)
INSERT dbo.BackupJobs(BackupJobId,JobType,Status,StartedAtUtc,CompletedAtUtc,RequestedByReference,DatabaseReference,BackupArtifactId,SafeErrorCode)
SELECT s.JobId,0,0,s.CreatedAt,NULL,NULL,N'legacy:unspecified',NULL,NULL
FROM S s
WHERE NOT EXISTS(
 SELECT 1 FROM dbo.LegacyDataMigrationMap x
 WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'BackupHistory' AND x.SourceKey=s.Id
);
;WITH S AS(
 SELECT b.*,
 CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.backup.job.v1|',b.Id)),1,16)) JobId,
 HASHBYTES('SHA2_256',CONCAT(b.Id,N'|',b.FileName,N'|',b.FileSize,N'|',b.CollectionsJson,N'|',b.TotalDocuments,N'|',CONVERT(nvarchar(33),b.CreatedAt,126),N'|',CONVERT(nvarchar(50),b.DurationSeconds),N'|',b.Status,N'|',b.Source,N'|',COALESCE(b.ErrorMessage,N''))) Fingerprint
 FROM dbo.BackupHistory b
)
INSERT dbo.LegacyDataMigrationMap(MigrationMapId,SourceSystem,SourceTable,SourceKey,TargetTable,TargetKey,SourceFingerprint,Outcome,Disposition,SafeNote)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.backup.map.v1|',s.Id)),1,16)),
 N'BuildingManagement.Legacy',N'BackupHistory',s.Id,N'BackupJobs',CONVERT(nvarchar(36),s.JobId),s.Fingerprint,1,N'MigratedWithLegacyMetadata',
 LEFT(CONCAT(N'LegacyStatus=',s.Status,N';Source=',s.Source,N';FileName=',s.FileName,N';FileSize=',s.FileSize,
 N';TotalDocuments=',s.TotalDocuments,N';DurationSeconds=',CONVERT(nvarchar(50),s.DurationSeconds),
 N';CollectionsJson and ErrorMessage retained in source; artifact hash/storage not inferred.'),500)
FROM S s
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap x
 WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'BackupHistory' AND x.SourceKey=s.Id);
"@
   $cmd=$c.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandText=$sql;$cmd.CommandTimeout=300
   [void]$cmd.ExecuteNonQuery();$tx.Commit()
  }catch{$tx.Rollback();throw}
  $target=[int](Query $c "SELECT COUNT(*) N FROM dbo.BackupJobs j INNER JOIN dbo.LegacyDataMigrationMap x ON x.TargetKey=CONVERT(nvarchar(36),j.BackupJobId) WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'BackupHistory'").Rows[0].N
  $maps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'BackupHistory'").Rows[0].N
  Log "Target backup jobs: $target";Log "Provenance rows: $maps"
  if($target -ne $src -or $maps -ne $src){throw "Reconciliation failed: source=$src target=$target provenance=$maps"}
  Log 'RECONCILIATION PASS'
 }
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $mode=if($Apply){'APPLY'}else{'DRY RUN'}
 $lines=@("# Phase 13 - BackupHistory Backfill","","- Run: $(Get-Date -Format o)","- Mode: $mode","- Source rows retained: dbo.BackupHistory","","## Results")
 $lines+=($log|ForEach-Object{"- $_"})
 $lines+=@("","## Preservation rules",
 "- BackupJobs are created with JobType=0 and Status=0 (unclassified); legacy status/source are preserved in migration provenance.",
 "- StartedAtUtc uses the recorded CreatedAt only; no completion time or duration is fabricated.",
 "- DatabaseReference is explicitly marked legacy:unspecified because the source row does not identify a database name.",
 "- No BackupArtifacts are fabricated: file existence, storage location, and SHA-256 content hash are not verifiable from BackupHistory metadata.",
 "- CollectionsJson and ErrorMessage remain intact in the source; their complete contents are covered by the source fingerprint.",
 "- Deterministic IDs and source mapping make reruns idempotent; source rows are never updated or deleted.")
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{
 git add -- $PSScriptRoot\Invoke-Phase13-BackupHistoryBackfill.ps1 $report
 if((git status --short -- $PSScriptRoot\Invoke-Phase13-BackupHistoryBackfill.ps1 $report).Count -gt 0){
  git commit --only -m "feat: backfill legacy backup history" -- $PSScriptRoot\Invoke-Phase13-BackupHistoryBackfill.ps1 $report
  git push origin development/database-redesign-enterprise
 }
}finally{Pop-Location}
