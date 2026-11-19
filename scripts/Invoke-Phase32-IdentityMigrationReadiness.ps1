[CmdletBinding()]
param([string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30")
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase32-IdentityMigrationReadiness-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Scalar($c,[string]$sql){$cmd=$c.CreateCommand();$cmd.CommandTimeout=180;$cmd.CommandText=$sql;return $cmd.ExecuteScalar()}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open()
 $db=[string](Scalar $c "SELECT DB_NAME()")
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 $sql=@"
SELECT
 (SELECT COUNT(*) FROM dbo.Users) UsersTotal,
 (SELECT COUNT(*) FROM dbo.Users WHERE NULLIF(LTRIM(RTRIM(Email)),N'') IS NOT NULL) UsersWithEmail,
 (SELECT COUNT(*) FROM dbo.Users WHERE NULLIF(LTRIM(RTRIM(Email)),N'') IS NULL) UsersWithoutEmail,
 (SELECT COUNT(*) FROM (SELECT LOWER(LTRIM(RTRIM(Email))) E FROM dbo.Users WHERE NULLIF(LTRIM(RTRIM(Email)),N'') IS NOT NULL GROUP BY LOWER(LTRIM(RTRIM(Email))) HAVING COUNT(*)>1) d) DuplicateUserEmailGroups,
 (SELECT COUNT(*) FROM dbo.Users WHERE LEN(LTRIM(RTRIM(Email)))>254) UserEmailsOver254,
 (SELECT COUNT(*) FROM dbo.Users WHERE NULLIF(LTRIM(RTRIM(Name)),N'') IS NULL) UsersWithoutName,
 (SELECT COUNT(*) FROM dbo.Users WHERE IsDisabled=1 OR IsActive=0) InactiveUsers,
 (SELECT COUNT(*) FROM dbo.Residents) ResidentsTotal,
 (SELECT COUNT(*) FROM dbo.Residents WHERE NULLIF(LTRIM(RTRIM(Email)),N'') IS NOT NULL) ResidentsWithEmail,
 (SELECT COUNT(*) FROM dbo.Residents WHERE NULLIF(LTRIM(RTRIM(Email)),N'') IS NULL) ResidentsWithoutEmail,
 (SELECT COUNT(*) FROM (SELECT LOWER(LTRIM(RTRIM(Email))) E FROM dbo.Residents WHERE NULLIF(LTRIM(RTRIM(Email)),N'') IS NOT NULL GROUP BY LOWER(LTRIM(RTRIM(Email))) HAVING COUNT(*)>1) d) DuplicateResidentEmailGroups,
 (SELECT COUNT(*) FROM dbo.Residents WHERE LEN(LTRIM(RTRIM(Email)))>254) ResidentEmailsOver254,
 (SELECT COUNT(*) FROM dbo.Residents WHERE NULLIF(LTRIM(RTRIM(Name)),N'') IS NULL) ResidentsWithoutName,
 (SELECT COUNT(*) FROM dbo.Residents WHERE IsDisabled=1 OR IsActive=0) InactiveResidents,
 (SELECT COUNT(*) FROM dbo.Residents WHERE NULLIF(LTRIM(RTRIM(Uid)),N'') IS NULL) ResidentsWithoutUid,
 (SELECT COUNT(*) FROM (SELECT Uid FROM dbo.Residents GROUP BY Uid HAVING COUNT(*)>1) d) DuplicateResidentUidGroups,
 (SELECT COUNT(*) FROM dbo.BuildingAdmins) BuildingAdminLinks,
 (SELECT COUNT(DISTINCT AdminUid) FROM dbo.BuildingAdmins) DistinctLinkedAdminUids,
 (SELECT COUNT(DISTINCT BuildingId) FROM dbo.BuildingAdmins) DistinctLinkedBuildings,
 (SELECT COUNT(*) FROM dbo.Persons) PersonsExisting,
 (SELECT COUNT(*) FROM dbo.UserAccounts) UserAccountsExisting,
 (SELECT COUNT(*) FROM dbo.UserCredentials) CredentialsExisting,
 (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users') UserMappingCandidates,
 (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents') ResidentMappingCandidates,
 (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users' AND MappingStatus=1 AND PersonId IS NOT NULL AND AccountId IS NOT NULL) VerifiedUserMappings,
 (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents' AND MappingStatus=1 AND PersonId IS NOT NULL AND AccountId IS NULL) VerifiedResidentPersonMappings;
"@
 $cmd=$c.CreateCommand();$cmd.CommandText=$sql;$cmd.CommandTimeout=180
 $dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt);$r=$dt.Rows[0]
 Log "Database: $db"
 foreach($col in $dt.Columns){Log "$($col.ColumnName): $($r[$col.ColumnName])"}
 $sql2=@"
SELECT COUNT(*) FROM dbo.Users u
WHERE LOWER(LTRIM(RTRIM(u.Role))) IN(N'admin',N'buildingadmin')
AND NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba WHERE ba.AdminUid=u.Uid);
"@
 $missing=[int](Scalar $c $sql2)
 Log "Admin users without an explicit BuildingAdmins row: $missing"
 $sql3=@"
SELECT COUNT(*) FROM dbo.BuildingAdmins ba
WHERE NOT EXISTS(SELECT 1 FROM dbo.Users u WHERE u.Uid=ba.AdminUid AND LOWER(LTRIM(RTRIM(u.Role))) IN(N'admin',N'buildingadmin'));
"@
 $invalid=[int](Scalar $c $sql3)
 Log "BuildingAdmins rows without an exact Admin source user: $invalid"
 Log 'No raw UIDs, names, email addresses, phone numbers, credentials, or hashes were exported.'
 Log 'READINESS PROFILE COMPLETE - NO DATA CHANGED.'
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $lines=@('# Phase 32 - Identity migration readiness','',"- Run: $(Get-Date -Format o)",'- Mode: READ ONLY','','## Results')
 $lines+=$log|%{"- $_"}
 $lines+=@('','## Decision','- No identity, account, credential, role, ownership, occupancy, or apartment-access rows are created by this audit.','- Users and Residents remain distinct source populations.','- Resident identity mapping is Person-only in the current mapping service; no resident login account is inferred.','- Admin building scope requires one exact BuildingAdmins source row per Admin; absent links are not fabricated.','- Email is not treated as a login identifier until uniqueness, validity, and application authentication behavior are verified.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{
 $script=Join-Path $PSScriptRoot 'Invoke-Phase32-IdentityMigrationReadiness.ps1'
 git add -- $script $report
 if((git status --short -- $script $report).Count -gt 0){git commit --only -m "feat: add identity migration readiness audit" -- $script $report;git push origin development/database-redesign-enterprise}
}finally{Pop-Location}
