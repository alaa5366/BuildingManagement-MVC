[CmdletBinding()]
param(
 [switch]$Apply,
 [string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase20-IdentityProfileEvidence-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Query($c,[string]$sql){$cmd=$c.CreateCommand();$cmd.CommandTimeout=180;$cmd.CommandText=$sql;$dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt);return ,$dt}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open()
 $db=(Query $c "SELECT DB_NAME() N").Rows[0].N
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @('Users','Residents','BuildingAdmins','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){
  $x=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N
  if($null -eq $x -or $x -is [DBNull]){throw "Missing dbo.$t"}
 }
 $m=[int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119200000_Phase20_IdentityProfileEvidence'").Rows[0].N
 if($m -ne 1){throw 'Phase20 schema migration is not applied'}
 $invalidUsers=[int](Query $c "SELECT COUNT(*) N FROM dbo.Users WHERE Uid IS NULL OR LEN(Uid)=0 OR LEN(Uid)>128").Rows[0].N
 $invalidResidents=[int](Query $c "SELECT COUNT(*) N FROM dbo.Residents WHERE Id IS NULL OR LEN(Id)=0 OR LEN(Id)>128 OR BuildingId IS NULL OR LEN(BuildingId)=0 OR LEN(BuildingId)>64 OR ApartmentId IS NULL OR LEN(ApartmentId)=0 OR LEN(ApartmentId)>64").Rows[0].N
 $invalidAdmins=[int](Query $c "SELECT COUNT(*) N FROM dbo.BuildingAdmins WHERE BuildingId IS NULL OR LEN(BuildingId)=0 OR LEN(BuildingId)>64 OR AdminUid IS NULL OR LEN(AdminUid)=0 OR LEN(AdminUid)>128").Rows[0].N
 if($invalidUsers -or $invalidResidents -or $invalidAdmins){throw "Preflight blocked: invalid Users=$invalidUsers Residents=$invalidResidents BuildingAdmins=$invalidAdmins."}
 $counts=@{}
 foreach($t in @('Users','Residents','BuildingAdmins')){$counts[$t]=[int](Query $c "SELECT COUNT(*) N FROM dbo.$t").Rows[0].N}
 Log "Database: $db"
 foreach($t in @('Users','Residents','BuildingAdmins')){Log "$t source rows: $($counts[$t])"}
 if(-not $Apply){Log 'DRY RUN - no data changed'}
 else{
  $tx=$c.BeginTransaction()
  try{
   $sql=@"
SET NOCOUNT ON;
;WITH S AS (
 SELECT u.Uid SourceKey,NULL BuildingId,
  (SELECT u.Uid,u.Email,u.Name,u.Phone,u.Whatsapp,u.PhotoUrl,u.Role,u.IsDisabled,u.DisabledReason,u.IsActive,
          u.LastLoginAt,u.CreatedAt,u.CreatedBy,u.UpdatedAt,u.UpdatedBy
   FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) Payload
 FROM dbo.Users u
)
INSERT dbo.LegacyOperationalRecords(LegacyOperationalRecordId,SourceSystem,SourceTable,SourceKey,BuildingId,PayloadJson,SourceFingerprint)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.identity.user.v1|',s.SourceKey))),1,16)),
 N'BuildingManagement.Legacy',N'Users',s.SourceKey,s.BuildingId,s.Payload,HASHBYTES('SHA2_256',CONVERT(varbinary(max),s.Payload))
FROM S s WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'Users' AND t.SourceKey=s.SourceKey);

;WITH S AS (
 SELECT r.BuildingId+N'|'+r.Id SourceKey,r.BuildingId,
  (SELECT r.Id,r.Uid,r.BuildingId,r.ApartmentId,r.Name,r.Phone,r.Whatsapp,r.Email,r.PhotoUrl,
          r.IsActive,r.IsDisabled,r.DisabledReason,r.IsOwner,r.IsPrimary,r.LastLoginAt,r.CreatedAt,r.CreatedBy,r.UpdatedAt,r.UpdatedBy
   FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) Payload
 FROM dbo.Residents r
)
INSERT dbo.LegacyOperationalRecords(LegacyOperationalRecordId,SourceSystem,SourceTable,SourceKey,BuildingId,PayloadJson,SourceFingerprint)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.identity.resident.v1|',s.SourceKey))),1,16)),
 N'BuildingManagement.Legacy',N'Residents',s.SourceKey,s.BuildingId,s.Payload,HASHBYTES('SHA2_256',CONVERT(varbinary(max),s.Payload))
FROM S s WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'Residents' AND t.SourceKey=s.SourceKey);

;WITH S AS (
 SELECT a.BuildingId+N'|'+a.AdminUid SourceKey,a.BuildingId,
  (SELECT a.BuildingId,a.AdminUid FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) Payload
 FROM dbo.BuildingAdmins a
)
INSERT dbo.LegacyOperationalRecords(LegacyOperationalRecordId,SourceSystem,SourceTable,SourceKey,BuildingId,PayloadJson,SourceFingerprint)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.identity.buildingadmin.v1|',s.SourceKey))),1,16)),
 N'BuildingManagement.Legacy',N'BuildingAdmins',s.SourceKey,s.BuildingId,s.Payload,HASHBYTES('SHA2_256',CONVERT(varbinary(max),s.Payload))
FROM S s WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=N'BuildingAdmins' AND t.SourceKey=s.SourceKey);

INSERT dbo.LegacyDataMigrationMap(MigrationMapId,SourceSystem,SourceTable,SourceKey,TargetTable,TargetKey,SourceFingerprint,Outcome,Disposition,SafeNote)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.identity.map.v1|',t.SourceTable,N'|',t.SourceKey))),1,16)),
 t.SourceSystem,t.SourceTable,t.SourceKey,N'LegacyOperationalRecords',CONVERT(nvarchar(36),t.LegacyOperationalRecordId),t.SourceFingerprint,1,N'PreservedAsUnresolvedIdentityEvidence',
 CASE t.SourceTable
  WHEN N'Users' THEN N'Profile and account state retained; PIN, MFA secrets and recovery codes excluded. No Person or UserAccount linkage inferred.'
  WHEN N'Residents' THEN N'Resident profile and apartment scope retained; PIN/password hashes excluded. IsOwner and IsPrimary are not converted to ownership or occupancy history.'
  ELSE N'Building-admin relationship retained as legacy evidence; no account identity or scoped role assignment inferred.'
 END
FROM dbo.LegacyOperationalRecords t
WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable IN(N'Users',N'Residents',N'BuildingAdmins')
AND NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=t.SourceSystem AND m.SourceTable=t.SourceTable AND m.SourceKey=t.SourceKey);
"@
   $cmd=$c.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandText=$sql;$cmd.CommandTimeout=300;[void]$cmd.ExecuteNonQuery();$tx.Commit()
  }catch{$tx.Rollback();throw}
  $total=0;$pass=$true
  foreach($t in @('Users','Residents','BuildingAdmins')){
   $target=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyOperationalRecords WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'$t'").Rows[0].N
   $mapped=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'$t'").Rows[0].N
   Log "$t target evidence: $target; provenance: $mapped"
   if($target -ne $counts[$t] -or $mapped -ne $counts[$t]){$pass=$false}
   $total+=$counts[$t]
  }
  if(-not $pass){throw 'Reconciliation failed for one or more identity source tables.'}
  Log "Total source rows reconciled: $total"
  Log 'RECONCILIATION PASS'
 }
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $mode=if($Apply){'APPLY'}else{'DRY RUN'}
 $lines=@('# Phase 20 - Identity profile evidence backfill','',"- Run: $(Get-Date -Format o)","- Mode: $mode",'','## Results')
 $lines+=$log|%{"- $_"}
 $lines+=@('','## Identity and credential safeguards',
 '- Users, Residents, and BuildingAdmins are preserved as unresolved legacy evidence; this phase does not create Persons, UserAccounts, credentials, or role assignments.',
 '- User PIN, resident PIN hash/password hash, MFA secret ciphertext, and recovery-code material are excluded from target evidence.',
 '- Residents.IsOwner and IsPrimary remain source facts; no legal ownership shares or effective-dated occupancy periods are fabricated.',
 '- Building-admin links remain legacy references and do not grant new access.',
 '- Source tables are never updated or deleted.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{$sp=Join-Path $PSScriptRoot 'Invoke-Phase20-IdentityProfileEvidenceBackfill.ps1';git add -- $sp $report;if((git status --short -- $sp $report).Count -gt 0){git commit --only -m "feat: preserve legacy identity profile evidence" -- $sp $report;git push origin development/database-redesign-enterprise}}finally{Pop-Location}
