[CmdletBinding()]
param(
 [switch]$Apply,
 [string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase16-QR-Backfill-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Query($c,[string]$sql){$cmd=$c.CreateCommand();$cmd.CommandText=$sql;$cmd.CommandTimeout=180;$dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt);return ,$dt}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open()
 $db=(Query $c "SELECT DB_NAME() N").Rows[0].N
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @('QrTokens','QrUsages','QrCapabilities','QrCapabilityUses','LegacyDataMigrationMap','__EFMigrationsHistory')){
  $x=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N
  if($null -eq $x -or $x -is [DBNull]){throw "Missing dbo.$t"}
 }
 $m=[int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119100000_Phase09_OperationalDomainSchema'").Rows[0].N
 if($m -ne 1){throw 'Phase09 schema migration missing'}
 $bad=[int](Query $c @"
SELECT COUNT(*) N FROM dbo.QrTokens
WHERE Uid IS NULL OR LEN(Uid)=0 OR LEN(Uid)>128
 OR BuildingId IS NULL OR LEN(BuildingId)=0 OR LEN(BuildingId)>64
 OR ApartmentId IS NULL OR LEN(ApartmentId)=0 OR LEN(ApartmentId)>64
 OR IssuedAt IS NULL OR ExpiresAt IS NULL OR ExpiresAt<=IssuedAt
 OR UseType NOT IN ('single','multi');
"@).Rows[0].N
 if($bad){throw "Preflight blocked: $bad QR token rows have invalid fields."}
 $badUsage=[int](Query $c "SELECT COUNT(*) N FROM dbo.QrUsages WHERE Id IS NULL OR LEN(Id)=0 OR LEN(Id)>128 OR TokenId IS NULL OR LEN(TokenId)>128 OR UseCount<0").Rows[0].N
 if($badUsage){throw "Preflight blocked: $badUsage QR usage rows invalid."}
 $tokens=[int](Query $c "SELECT COUNT(*) N FROM dbo.QrTokens").Rows[0].N
 $usage=[int](Query $c "SELECT COUNT(*) N FROM dbo.QrUsages").Rows[0].N
 $pending=[int](Query $c @"
SELECT COUNT(*) N FROM dbo.QrTokens s WHERE NOT EXISTS
(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'QrTokens' AND m.SourceKey=s.Uid);
"@).Rows[0].N
 Log "Database: $db";Log "QR token source rows: $tokens";Log "QR usage aggregate rows: $usage";Log "Pending tokens: $pending"
 if(-not $Apply){Log 'DRY RUN - no data changed'}
 else{
  $tx=$c.BeginTransaction()
  try{
   $sql=@"
SET NOCOUNT ON;
;WITH S AS (
 SELECT q.*,
  CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.qr.capability.v1|',q.Uid))),1,16)) CapabilityId,
  HASHBYTES('SHA2_256',CONVERT(varbinary(max),q.Uid)) TokenDigest,
  HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT q.Uid,q.BuildingId,q.ApartmentId,q.ExpiresAt,q.UseType,q.IssuedAt,q.CreatedBy,q.IsUsed,q.UsedAt,q.UsedBy,q.CreatedAt FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES))) Fingerprint
 FROM dbo.QrTokens q
)
INSERT dbo.QrCapabilities(CapabilityId,BuildingId,ApartmentId,TargetType,TargetReference,TokenHash,IssuedAtUtc,ExpiresAtUtc,MaxUses,RevokedAtUtc,RevokedByReference)
SELECT s.CapabilityId,s.BuildingId,s.ApartmentId,0,
 CONCAT(N'legacy-apartment:',s.BuildingId,N'/',s.ApartmentId),
 s.TokenDigest,s.IssuedAt,s.ExpiresAt,CASE WHEN s.UseType=N'single' THEN 1 ELSE 2147483647 END,
 NULL,NULL
FROM S s
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'QrTokens' AND m.SourceKey=s.Uid);

;WITH S AS (
 SELECT q.*,
  CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.qr.capability.v1|',q.Uid))),1,16)) CapabilityId,
  HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT q.Uid,q.BuildingId,q.ApartmentId,q.ExpiresAt,q.UseType,q.IssuedAt,q.CreatedBy,q.IsUsed,q.UsedAt,q.UsedBy,q.CreatedAt FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES))) Fingerprint
 FROM dbo.QrTokens q
)
INSERT dbo.LegacyDataMigrationMap(MigrationMapId,SourceSystem,SourceTable,SourceKey,TargetTable,TargetKey,SourceFingerprint,Outcome,Disposition,SafeNote)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.qr.token.map.v1|',s.Uid))),1,16)),
 N'BuildingManagement.Legacy',N'QrTokens',s.Uid,N'QrCapabilities',CONVERT(nvarchar(36),s.CapabilityId),s.Fingerprint,1,N'MigratedWithLegacyMetadata',
 N'Legacy UID digest stored as lookup hash; legacy HMAC validation remains required; target type 0 is unclassified; no account/person identity inferred.'
FROM S s
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'QrTokens' AND m.SourceKey=s.Uid);

;WITH S AS (
 SELECT u.*,
  HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT u.Id,u.TokenId,u.UseCount,u.FirstUsedAt,u.LastUsedAt,u.CreatedAt FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES))) Fingerprint
 FROM dbo.QrUsages u
)
INSERT dbo.LegacyDataMigrationMap(MigrationMapId,SourceSystem,SourceTable,SourceKey,TargetTable,TargetKey,SourceFingerprint,Outcome,Disposition,SafeNote)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.qr.usage.map.v1|',s.Id))),1,16)),
 N'BuildingManagement.Legacy',N'QrUsages',s.Id,N'QrCapabilityUses',NULL,s.Fingerprint,1,N'PreservedAsAggregateEvidence',
 LEFT(CONCAT(N'TokenId=',s.TokenId,N'; UseCount=',s.UseCount,N'; FirstUsedAt=',s.FirstUsedAt,N'; LastUsedAt=',s.LastUsedAt,N'; aggregate is not expanded into fabricated individual use events; device fingerprint intentionally excluded.'),500)
FROM S s
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'QrUsages' AND m.SourceKey=s.Id);
"@
   $cmd=$c.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandText=$sql;$cmd.CommandTimeout=300;[void]$cmd.ExecuteNonQuery();$tx.Commit()
  }catch{$tx.Rollback();throw}
  $target=[int](Query $c @"
SELECT COUNT(*) N FROM dbo.QrCapabilities t JOIN dbo.LegacyDataMigrationMap m ON m.TargetKey=CONVERT(nvarchar(36),t.CapabilityId)
WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'QrTokens';
"@).Rows[0].N
  $tokenMap=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'QrTokens'").Rows[0].N
  $usageMap=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'QrUsages'").Rows[0].N
  Log "Target capabilities: $target";Log "Token provenance: $tokenMap";Log "Usage aggregate provenance: $usageMap"
  if($target -ne $tokens -or $tokenMap -ne $tokens -or $usageMap -ne $usage){throw "Reconciliation failed: tokens=$tokens target=$target tokenMap=$tokenMap usage=$usage usageMap=$usageMap"}
  Log 'RECONCILIATION PASS'
 }
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $mode=if($Apply){'APPLY'}else{'DRY RUN'}
 $lines=@('# Phase 16 - QR capability backfill','',"- Run: $(Get-Date -Format o)","- Mode: $mode",'','## Results')
 $lines+= $log|%{"- $_"}
 $lines+=@('','## Security and fidelity rules',
 '- Capability TokenHash is SHA-256 of the legacy opaque UID; legacy HMAC signature validation must still occur before this digest is used for authorization.',
 '- Account/person identity is not inferred; target type remains unclassified and target reference is explicitly legacy apartment scope.',
 '- Single-use tokens retain MaxUses=1; legacy multi-use behavior is represented by the maximum integer bound.',
 '- QrUsages are aggregate counters, not event logs. They are retained as provenance evidence and never expanded into fabricated individual uses.',
 '- DeviceFingerprint is intentionally not copied. Source QR tables are never updated or deleted.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{
 $sp=Join-Path $PSScriptRoot 'Invoke-Phase16-QRBackfill.ps1'
 git add -- $sp $report
 if((git status --short -- $sp $report).Count -gt 0){git commit --only -m "feat: backfill legacy QR capabilities" -- $sp $report;git push origin development/database-redesign-enterprise}
}finally{Pop-Location}
