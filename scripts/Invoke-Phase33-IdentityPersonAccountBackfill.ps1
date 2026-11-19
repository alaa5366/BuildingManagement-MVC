[CmdletBinding()]
param(
 [switch]$Apply,
 [string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase33-IdentityPersonAccountBackfill-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Scalar($c,[string]$sql){$cmd=$c.CreateCommand();$cmd.CommandTimeout=180;$cmd.CommandText=$sql;return $cmd.ExecuteScalar()}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
$applied=$false
try{
 $c.Open()
 $db=[string](Scalar $c "SELECT DB_NAME()")
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @('Users','Residents','LegacyIdentityMap','Persons','UserAccounts','__EFMigrationsHistory')){
  $v=Scalar $c "SELECT OBJECT_ID(N'dbo.$t',N'U')"
  if($null -eq $v -or $v -is [DBNull]){throw "Missing dbo.$t"}
 }
 $migration=[int](Scalar $c "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119200000_Phase20_IdentityProfileEvidence'")
 if($migration -ne 1){throw 'Required identity schema migration is not applied'}
 $preflight=@"
SELECT
 (SELECT COUNT(*) FROM dbo.Users u WHERE LEN(LTRIM(RTRIM(u.Name)))>160 OR LEN(NULLIF(LTRIM(RTRIM(u.Phone)),N''))>32 OR LEN(NULLIF(LTRIM(RTRIM(u.Whatsapp)),N''))>32 OR LEN(NULLIF(LTRIM(RTRIM(u.PhotoUrl)),N''))>2048 OR LEN(LTRIM(RTRIM(u.Email)))>254) InvalidUsers,
 (SELECT COUNT(*) FROM dbo.Residents r WHERE LEN(LTRIM(RTRIM(r.Name)))>160 OR LEN(NULLIF(LTRIM(RTRIM(r.Phone)),N''))>32 OR LEN(NULLIF(LTRIM(RTRIM(r.Whatsapp)),N''))>32 OR LEN(NULLIF(LTRIM(RTRIM(r.PhotoUrl)),N''))>2048 OR LEN(NULLIF(LTRIM(RTRIM(r.Email)),N''))>254) InvalidResidents,
 (SELECT COUNT(*) FROM dbo.Users u WHERE NULLIF(LTRIM(RTRIM(u.Email)),N'') IS NULL) UsersMissingEmail,
 (SELECT COUNT(*) FROM dbo.Users u WHERE NULLIF(LTRIM(RTRIM(u.Name)),N'') IS NULL) UsersMissingName,
 (SELECT COUNT(*) FROM dbo.Residents r WHERE NULLIF(LTRIM(RTRIM(r.Name)),N'') IS NULL) ResidentsMissingName,
 (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users' AND (MappingStatus<>0 OR PersonId IS NOT NULL OR AccountId IS NOT NULL)) NonPendingUserMappings,
 (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents' AND (MappingStatus<>0 OR PersonId IS NOT NULL OR AccountId IS NOT NULL)) NonPendingResidentMappings,
 (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users') UserMaps,
 (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents') ResidentMaps;
"@
 $cmd=$c.CreateCommand();$cmd.CommandText=$preflight
 $dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt);$r=$dt.Rows[0]
 foreach($col in $dt.Columns){Log "$($col.ColumnName): $($r[$col.ColumnName])"}
 if([int]$r.InvalidUsers -gt 0 -or [int]$r.InvalidResidents -gt 0 -or [int]$r.UsersMissingEmail -gt 0 -or [int]$r.UsersMissingName -gt 0 -or [int]$r.ResidentsMissingName -gt 0){throw 'Source profile exceeds target constraints or required identity fields are missing.'}
 if([int]$r.NonPendingUserMappings -gt 0 -or [int]$r.NonPendingResidentMappings -gt 0){throw 'Existing identity mappings are not all pending; refusing partial or conflicting backfill.'}
 if([int]$r.UserMaps -ne 5 -or [int]$r.ResidentMaps -ne 79){throw 'Expected exact identity mapping candidate counts Users=5 and Residents=79.'}
 if(-not $Apply){Log 'DRY RUN - no data changed';return}
 $tx=$c.BeginTransaction()
 try{
  $sql=@"
SET NOCOUNT ON;
DECLARE @now datetime2(3)=SYSUTCDATETIME();
DECLARE @sourceKey nvarchar(512),@personId uniqueidentifier,@accountId uniqueidentifier;
DECLARE @name nvarchar(max),@email nvarchar(max),@phone nvarchar(max),@whatsapp nvarchar(max),@photo nvarchar(max);
DECLARE @uid nvarchar(256),@active bit,@disabled bit;

DECLARE resident_cursor CURSOR LOCAL FAST_FORWARD FOR
 SELECT m.SourceKey,r.Name,r.Email,r.Phone,r.Whatsapp,r.PhotoUrl
 FROM dbo.LegacyIdentityMap m
 JOIN dbo.Residents r ON m.SourceKey=CONCAT(r.BuildingId,N'|',r.Id)
 WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Residents'
   AND m.MappingStatus=0 AND m.PersonId IS NULL AND m.AccountId IS NULL
 ORDER BY m.SourceKey;
OPEN resident_cursor;
FETCH NEXT FROM resident_cursor INTO @sourceKey,@name,@email,@phone,@whatsapp,@photo;
WHILE @@FETCH_STATUS=0
BEGIN
 SET @personId=NEWID();
 INSERT dbo.Persons(PersonId,LegalName,DisplayName,PrimaryEmail,PrimaryPhone,Whatsapp,PhotoUrl,IsArchived,ArchivedAtUtc,CreatedAtUtc,UpdatedAtUtc)
 VALUES(@personId,NULL,LTRIM(RTRIM(@name)),NULLIF(LTRIM(RTRIM(@email)),N''),NULLIF(LTRIM(RTRIM(@phone)),N''),NULLIF(LTRIM(RTRIM(@whatsapp)),N''),NULLIF(LTRIM(RTRIM(@photo)),N''),0,NULL,@now,NULL);
 UPDATE dbo.LegacyIdentityMap
 SET PersonId=@personId,AccountId=NULL,MappingStatus=2,MatchMethod=N'user-confirmed-separate-population',
     EvidenceReference=N'Phase30 user-confirmed Residents are residents only; Phase32 exact source profile',
     ReviewReason=N'User explicitly confirmed all Residents rows are residents, not administrative identities. Person only; no login account or credential inferred.',
     ReviewedAtUtc=@now,ReviewedByAccountId=NULL,UpdatedAtUtc=@now
 WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents' AND SourceKey=@sourceKey AND MappingStatus=0 AND PersonId IS NULL AND AccountId IS NULL;
 IF @@ROWCOUNT<>1 THROW 51033,'Resident identity map changed during backfill.',1;
 FETCH NEXT FROM resident_cursor INTO @sourceKey,@name,@email,@phone,@whatsapp,@photo;
END
CLOSE resident_cursor; DEALLOCATE resident_cursor;

DECLARE user_cursor CURSOR LOCAL FAST_FORWARD FOR
 SELECT m.SourceKey,u.Uid,u.Name,u.Email,u.Phone,u.Whatsapp,u.PhotoUrl,u.IsActive,u.IsDisabled
 FROM dbo.LegacyIdentityMap m
 JOIN dbo.Users u ON m.SourceKey=u.Uid
 WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users'
   AND m.MappingStatus=0 AND m.PersonId IS NULL AND m.AccountId IS NULL
 ORDER BY m.SourceKey;
OPEN user_cursor;
FETCH NEXT FROM user_cursor INTO @sourceKey,@uid,@name,@email,@phone,@whatsapp,@photo,@active,@disabled;
WHILE @@FETCH_STATUS=0
BEGIN
 SET @personId=NEWID(); SET @accountId=NEWID();
 INSERT dbo.Persons(PersonId,LegalName,DisplayName,PrimaryEmail,PrimaryPhone,Whatsapp,PhotoUrl,IsArchived,ArchivedAtUtc,CreatedAtUtc,UpdatedAtUtc)
 VALUES(@personId,NULL,LTRIM(RTRIM(@name)),NULLIF(LTRIM(RTRIM(@email)),N''),NULLIF(LTRIM(RTRIM(@phone)),N''),NULLIF(LTRIM(RTRIM(@whatsapp)),N''),NULLIF(LTRIM(RTRIM(@photo)),N''),0,NULL,@now,NULL);
 INSERT dbo.UserAccounts(AccountId,PersonId,AccountStatus,NormalizedEmail,ExternalProvider,ExternalSubject,SecurityStamp,LastLoginAtUtc,CreatedAtUtc)
 VALUES(@accountId,@personId,0,UPPER(LTRIM(RTRIM(@email))),NULL,NULL,CONVERT(nvarchar(128),NEWID()),NULL,@now);
 UPDATE dbo.LegacyIdentityMap
 SET PersonId=@personId,AccountId=@accountId,MappingStatus=1,MatchMethod=N'exact-legacy-user-row',
     EvidenceReference=N'Phase30 exact Users role classification; Phase32 unique email and exact UID source row',
     ReviewReason=N'Administrative account migrated as a distinct Person and pending UserAccount. Credentials and roles are provisioned only in later controlled phases.',
     ReviewedAtUtc=@now,ReviewedByAccountId=NULL,UpdatedAtUtc=@now
 WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users' AND SourceKey=@sourceKey AND MappingStatus=0 AND PersonId IS NULL AND AccountId IS NULL;
 IF @@ROWCOUNT<>1 THROW 51034,'User identity map changed during backfill.',1;
 FETCH NEXT FROM user_cursor INTO @sourceKey,@uid,@name,@email,@phone,@whatsapp,@photo,@active,@disabled;
END
CLOSE user_cursor; DEALLOCATE user_cursor;
"@
  $cmd=$c.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandText=$sql;$cmd.CommandTimeout=300;[void]$cmd.ExecuteNonQuery();$tx.Commit();$applied=$true
 }catch{try{$tx.Rollback()}catch{};throw}
 foreach($q in @(
  "SELECT COUNT(*) FROM dbo.Persons",
  "SELECT COUNT(*) FROM dbo.UserAccounts",
  "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users' AND MappingStatus=1 AND PersonId IS NOT NULL AND AccountId IS NOT NULL",
  "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents' AND MappingStatus=2 AND PersonId IS NOT NULL AND AccountId IS NULL",
  "SELECT COUNT(*) FROM dbo.UserCredentials",
  "SELECT COUNT(*) FROM dbo.UserRoleAssignments"
 )){$null=Scalar $c $q}
 Log 'APPLY TRANSACTION COMMITTED'
 Log ("Persons total: "+(Scalar $c "SELECT COUNT(*) FROM dbo.Persons"))
 Log ("UserAccounts total: "+(Scalar $c "SELECT COUNT(*) FROM dbo.UserAccounts"))
 Log ("Verified Users mappings: "+(Scalar $c "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users' AND MappingStatus=1 AND PersonId IS NOT NULL AND AccountId IS NOT NULL"))
 Log ("Separate Resident person mappings: "+(Scalar $c "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents' AND MappingStatus=2 AND PersonId IS NOT NULL AND AccountId IS NULL"))
 Log ("Credentials created: "+(Scalar $c "SELECT COUNT(*) FROM dbo.UserCredentials"))
 Log ("Role assignments created: "+(Scalar $c "SELECT COUNT(*) FROM dbo.UserRoleAssignments"))
 Log 'IDENTITY PERSON/ACCOUNT BACKFILL COMPLETE - no credentials or roles were created.'
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $mode=if($Apply){'APPLY'}else{'DRY RUN'}
 $lines=@('# Phase 33 - Identity Person and Account backfill','',"- Run: $(Get-Date -Format o)","- Mode: $mode",'','## Results')
 $lines+=$log|%{"- $_"}
 $lines+=@('','## Safeguards',
 '- Residents are mapped as separate Persons only, based on the explicit user confirmation; no Resident login accounts or credentials are created.',
 '- Five administrative Users become distinct Persons and pending UserAccounts; AccountStatus=0 prevents sign-in until later controlled activation.',
 '- No credentials, temporary passwords, MFA secrets, role assignments, permissions, ownership, occupancy, or apartment access grants are created.',
 '- The two inactive resident records remain identity profiles only; no login account is activated for any resident.',
 '- The third Admin remains without a building role until an exact source BuildingAdmins link is available.',
 '- All writes are transactional and idempotency is guarded by pending exact-key mapping rows; legacy source tables are read-only.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{
 $script=Join-Path $PSScriptRoot 'Invoke-Phase33-IdentityPersonAccountBackfill.ps1'
 git add -- $script $report
 if((git status --short -- $script $report).Count -gt 0){git commit --only -m "feat: add transactional identity person account backfill" -- $script $report;git push origin development/database-redesign-enterprise}
}finally{Pop-Location}
