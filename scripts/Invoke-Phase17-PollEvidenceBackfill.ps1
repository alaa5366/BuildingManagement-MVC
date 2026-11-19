[CmdletBinding()]
param(
 [switch]$Apply,
 [string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase17-Poll-Evidence-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Query($c,[string]$sql){$cmd=$c.CreateCommand();$cmd.CommandTimeout=180;$cmd.CommandText=$sql;$dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt);return ,$dt}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open()
 $db=(Query $c "SELECT DB_NAME() N").Rows[0].N
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @('Polls','PollOptions','PollVotes','PollLifecycleEvents','LegacyDataMigrationMap','__EFMigrationsHistory')){
  $x=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N
  if($null -eq $x -or $x -is [DBNull]){throw "Missing dbo.$t"}
 }
 $m=[int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119170000_Phase17_LegacyPollEvidence'").Rows[0].N
 if($m -ne 1){throw 'Phase17 schema migration is not applied'}
 $invalid=[int](Query $c @"
SELECT COUNT(*) N FROM dbo.Polls
WHERE Id IS NULL OR LEN(Id)=0 OR LEN(Id)>64 OR BuildingId IS NULL OR LEN(BuildingId)=0 OR LEN(BuildingId)>64
 OR CreatedAt IS NULL OR LEN(CreatedBy)>160;
"@).Rows[0].N
 if($invalid){throw "Preflight blocked: $invalid poll rows contain invalid required fields."}
 $orphanOptions=[int](Query $c @"
SELECT COUNT(*) N FROM dbo.PollOptions o
WHERE NOT EXISTS(SELECT 1 FROM dbo.Polls p WHERE p.BuildingId=o.BuildingId AND p.Id=o.PollId);
"@).Rows[0].N
 $orphanVotes=[int](Query $c @"
SELECT COUNT(*) N FROM dbo.PollVotes v
WHERE NOT EXISTS(SELECT 1 FROM dbo.Polls p WHERE p.BuildingId=v.BuildingId AND p.Id=v.PollId)
 OR NOT EXISTS(SELECT 1 FROM dbo.PollOptions o WHERE o.BuildingId=v.BuildingId AND o.PollId=v.PollId AND o.Id=v.OptionId);
"@).Rows[0].N
 if($orphanOptions -or $orphanVotes){throw "Preflight blocked: orphan options=$orphanOptions votes=$orphanVotes."}
 $polls=[int](Query $c "SELECT COUNT(*) N FROM dbo.Polls").Rows[0].N
 $options=[int](Query $c "SELECT COUNT(*) N FROM dbo.PollOptions").Rows[0].N
 $votes=[int](Query $c "SELECT COUNT(*) N FROM dbo.PollVotes").Rows[0].N
 $pendingPolls=[int](Query $c @"
SELECT COUNT(*) N FROM dbo.Polls p WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Polls' AND m.SourceKey=p.BuildingId+N'|'+p.Id);
"@).Rows[0].N
 Log "Database: $db";Log "Source polls: $polls";Log "Source options: $options";Log "Source apartment votes: $votes";Log "Pending poll lifecycle records: $pendingPolls"
 if(-not $Apply){Log 'DRY RUN - no data changed'}
 else{
  $tx=$c.BeginTransaction()
  try{
   $sql=@"
SET NOCOUNT ON;
;WITH S AS (
 SELECT p.*,
  CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.poll.event.v1|',p.BuildingId,N'|',p.Id))),1,16)) EventId,
  HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT p.BuildingId,p.Id,p.Question,p.Description,p.CreatedBy,p.CreatedAt,p.Deadline,p.IsClosed FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES))) Fingerprint
 FROM dbo.Polls p
)
INSERT dbo.PollLifecycleEvents(EventId,BuildingId,PollId,EventType,OccurredAtUtc,ActorReference,Reason,MetadataJson)
SELECT s.EventId,s.BuildingId,s.Id,0,s.CreatedAt,NULLIF(s.CreatedBy,N''),NULL,
 (SELECT s.Question,s.Description,s.Deadline,s.IsClosed,N'legacy-event-type-unclassified' AS MigrationClassification FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)
FROM S s
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Polls' AND m.SourceKey=s.BuildingId+N'|'+s.Id);

;WITH S AS (
 SELECT p.*,
  CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.poll.event.v1|',p.BuildingId,N'|',p.Id))),1,16)) EventId,
  HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT p.BuildingId,p.Id,p.Question,p.Description,p.CreatedBy,p.CreatedAt,p.Deadline,p.IsClosed FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES))) Fingerprint
 FROM dbo.Polls p
)
INSERT dbo.LegacyDataMigrationMap(MigrationMapId,SourceSystem,SourceTable,SourceKey,TargetTable,TargetKey,SourceFingerprint,Outcome,Disposition,SafeNote)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.poll.map.v1|',s.BuildingId,N'|',s.Id))),1,16)),
 N'BuildingManagement.Legacy',N'Polls',s.BuildingId+N'|'+s.Id,N'PollLifecycleEvents',CONVERT(nvarchar(36),s.EventId),s.Fingerprint,1,N'MigratedWithLegacyMetadata',
 N'Created event recorded; EventType 0 is unclassified; poll definition and voting policy remain legacy pending semantic review.'
FROM S s
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Polls' AND m.SourceKey=s.BuildingId+N'|'+s.Id);

;WITH S AS (
 SELECT o.*,HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT o.BuildingId,o.PollId,o.Id,o.Text,o.SortOrder FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES))) Fingerprint
 FROM dbo.PollOptions o
)
INSERT dbo.LegacyDataMigrationMap(MigrationMapId,SourceSystem,SourceTable,SourceKey,TargetTable,TargetKey,SourceFingerprint,Outcome,Disposition,SafeNote)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.poll.option.map.v1|',s.BuildingId,N'|',s.PollId,N'|',s.Id))),1,16)),
 N'BuildingManagement.Legacy',N'PollOptions',s.BuildingId+N'|'+s.PollId+N'|'+s.Id,N'LegacyDataMigrationMap',NULL,s.Fingerprint,1,N'PreservedAsLegacyOptionEvidence',
 LEFT(CONCAT(N'Poll=',s.PollId,N'; Option=',s.Id,N'; SortOrder=',s.SortOrder,N'; option text remains in source; no new voting model inferred.'),500)
FROM S s
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'PollOptions' AND m.SourceKey=s.BuildingId+N'|'+s.PollId+N'|'+s.Id);

;WITH S AS (
 SELECT v.*,HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT v.BuildingId,v.PollId,v.ApartmentId,v.OptionId,v.VotedAt FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES))) Fingerprint
 FROM dbo.PollVotes v
)
INSERT dbo.LegacyDataMigrationMap(MigrationMapId,SourceSystem,SourceTable,SourceKey,TargetTable,TargetKey,SourceFingerprint,Outcome,Disposition,SafeNote)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.poll.vote.map.v1|',s.BuildingId,N'|',s.PollId,N'|',s.ApartmentId))),1,16)),
 N'BuildingManagement.Legacy',N'PollVotes',s.BuildingId+N'|'+s.PollId+N'|'+s.ApartmentId,N'LegacyDataMigrationMap',NULL,s.Fingerprint,1,N'PreservedAsApartmentVoteEvidence',
 LEFT(CONCAT(N'Poll=',s.PollId,N'; Apartment=',s.ApartmentId,N'; Option=',s.OptionId,N'; VotedAt=',CONVERT(nvarchar(33),s.VotedAt,126),N'; apartment-level attribution retained; no person identity or anonymous ballot conversion inferred.'),500)
FROM S s
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'PollVotes' AND m.SourceKey=s.BuildingId+N'|'+s.PollId+N'|'+s.ApartmentId);
"@
   $cmd=$c.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandText=$sql;$cmd.CommandTimeout=300;[void]$cmd.ExecuteNonQuery();$tx.Commit()
  }catch{$tx.Rollback();throw}
  $events=[int](Query $c @"
SELECT COUNT(*) N FROM dbo.PollLifecycleEvents e JOIN dbo.LegacyDataMigrationMap m ON m.TargetKey=CONVERT(nvarchar(36),e.EventId)
WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Polls';
"@).Rows[0].N
  $pollMaps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'Polls'").Rows[0].N
  $optionMaps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'PollOptions'").Rows[0].N
  $voteMaps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'PollVotes'").Rows[0].N
  Log "Target lifecycle events: $events";Log "Poll provenance: $pollMaps";Log "Option provenance: $optionMaps";Log "Apartment-vote provenance: $voteMaps"
  if($events -ne $polls -or $pollMaps -ne $polls -or $optionMaps -ne $options -or $voteMaps -ne $votes){throw "Reconciliation failed: polls=$polls events=$events pollMap=$pollMaps options=$options optionMap=$optionMaps votes=$votes voteMap=$voteMaps"}
  Log 'RECONCILIATION PASS'
 }
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $mode=if($Apply){'APPLY'}else{'DRY RUN'}
 $lines=@('# Phase 17 - Poll evidence backfill','',"- Run: $(Get-Date -Format o)","- Mode: $mode",'','## Results')
 $lines+=$log|%{"- $_"}
 $lines+=@('','## Preservation rules',
 '- Poll creation is represented as a lifecycle event with EventType 0 explicitly classified as unclassified; metadata preserves question, description, deadline, and closed state.',
 '- PollOptions and PollVotes are retained as fingerprinted legacy evidence in LegacyDataMigrationMap; source rows are not changed.',
 '- Apartment-level vote attribution remains apartment-level. No person identity, eligibility, delegation, ballot token, or anonymous ballot is inferred.',
 '- No individual events are synthesized from aggregate vote records; legacy source tables remain unchanged.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{
 $sp=Join-Path $PSScriptRoot 'Invoke-Phase17-PollEvidenceBackfill.ps1'
 git add -- $sp $report
 if((git status --short -- $sp $report).Count -gt 0){git commit --only -m "feat: preserve legacy poll evidence" -- $sp $report;git push origin development/database-redesign-enterprise}
}finally{Pop-Location}
