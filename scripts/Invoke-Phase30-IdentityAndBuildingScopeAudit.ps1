[CmdletBinding()]
param(
 [string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase30-IdentityAndBuildingScopeAudit-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Query($c,[string]$sql){
 $cmd=$c.CreateCommand();$cmd.CommandTimeout=180;$cmd.CommandText=$sql
 $dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt);return ,$dt
}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open()
 $db=[string](Query $c "SELECT DB_NAME() N").Rows[0].N
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @('Users','Residents','BuildingAdmins','Buildings','Apartments','LegacyIdentityMap','UserAccounts','Persons','UserRoleAssignments','__EFMigrationsHistory')){
  $v=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N
  if($null -eq $v -or $v -is [DBNull]){throw "Missing dbo.$t"}
 }
 $required=@{
  Users=@('Uid','Role','IsDisabled','IsActive')
  Residents=@('Id','BuildingId','ApartmentId','Uid','IsActive','IsDisabled')
  BuildingAdmins=@('AdminUid','BuildingId')
 }
 foreach($table in $required.Keys){foreach($col in $required[$table]){
  $v=(Query $c "SELECT COL_LENGTH(N'dbo.$table',N'$col') N").Rows[0].N
  if($null -eq $v -or $v -is [DBNull]){throw "Missing dbo.$table.$col"}
 }}
 $userSummary=Query $c @"
SELECT COALESCE(NULLIF(LTRIM(RTRIM(Role)),N''),N'(blank)') RoleName,
 COUNT(*) Total,
 SUM(CASE WHEN ISNULL(IsDisabled,0)=1 OR ISNULL(IsActive,1)=0 THEN 1 ELSE 0 END) InactiveOrDisabled
FROM dbo.Users GROUP BY COALESCE(NULLIF(LTRIM(RTRIM(Role)),N''),N'(blank)')
ORDER BY RoleName;
"@
 $residentSummary=Query $c @"
SELECT r.BuildingId,COUNT(*) Residents,
 SUM(CASE WHEN ISNULL(r.IsDisabled,0)=1 OR ISNULL(r.IsActive,1)=0 THEN 1 ELSE 0 END) InactiveOrDisabled,
 COUNT(DISTINCT r.ApartmentId) Apartments
FROM dbo.Residents r GROUP BY r.BuildingId ORDER BY r.BuildingId;
"@
 $buildingSummary=Query $c @"
SELECT b.Id BuildingId,COUNT(DISTINCT r.Id) ResidentRows,
 COUNT(DISTINCT r.ApartmentId) Apartments
FROM dbo.Buildings b LEFT JOIN dbo.Residents r ON r.BuildingId=b.Id
GROUP BY b.Id ORDER BY b.Id;
"@
 $adminSummary=Query $c @"
SELECT ba.BuildingId,COUNT(*) AdminLinks,COUNT(DISTINCT ba.AdminUid) DistinctAdminUids,
 SUM(CASE WHEN u.Uid IS NULL THEN 1 ELSE 0 END) UidsMissingFromUsers,
 SUM(CASE WHEN u.Uid IS NOT NULL AND LOWER(LTRIM(RTRIM(u.Role))) NOT IN(N'admin',N'buildingadmin') THEN 1 ELSE 0 END) RoleMismatch
FROM dbo.BuildingAdmins ba
LEFT JOIN dbo.Users u ON u.Uid=ba.AdminUid
GROUP BY ba.BuildingId ORDER BY ba.BuildingId;
"@
 $roleCounts=Query $c @"
SELECT
 SUM(CASE WHEN LOWER(LTRIM(RTRIM(Role))) IN(N'superadmin',N'super_admin',N'super admin') THEN 1 ELSE 0 END) SuperAdmins,
 SUM(CASE WHEN LOWER(LTRIM(RTRIM(Role))) IN(N'admin',N'buildingadmin') THEN 1 ELSE 0 END) Admins,
 COUNT(*) TotalUsers
FROM dbo.Users;
"@
 $overlap=[int](Query $c "SELECT COUNT(*) N FROM dbo.Users u INNER JOIN dbo.Residents r ON r.Uid=u.Uid WHERE NULLIF(u.Uid,N'') IS NOT NULL").Rows[0].N
 $mapUsers=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users' AND MappingStatus=1 AND AccountId IS NOT NULL").Rows[0].N
 $mapResidents=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents' AND MappingStatus=1 AND PersonId IS NOT NULL AND AccountId IS NULL").Rows[0].N
 $persons=[int](Query $c "SELECT COUNT(*) N FROM dbo.Persons").Rows[0].N
 $accounts=[int](Query $c "SELECT COUNT(*) N FROM dbo.UserAccounts").Rows[0].N
 $assignments=[int](Query $c "SELECT COUNT(*) N FROM dbo.UserRoleAssignments").Rows[0].N
 Log "Database: $db"
 Log "Users total: $($roleCounts.Rows[0].TotalUsers); SuperAdmins by role: $($roleCounts.Rows[0].SuperAdmins); Admins by role: $($roleCounts.Rows[0].Admins)"
 foreach($r in $userSummary.Rows){Log "Users role '$($r.RoleName)': total=$($r.Total); inactive/disabled=$($r.InactiveOrDisabled)"}
 Log "Residents total: $((Query $c 'SELECT COUNT(*) N FROM dbo.Residents').Rows[0].N)"
 foreach($r in $residentSummary.Rows){Log "Residents BuildingId=$($r.BuildingId): residents=$($r.Residents); apartments=$($r.Apartments); inactive/disabled=$($r.InactiveOrDisabled)"}
 Log "Users/Residents exact UID overlap: $overlap"
 foreach($r in $adminSummary.Rows){Log "BuildingAdmins BuildingId=$($r.BuildingId): links=$($r.AdminLinks); distinct AdminUid=$($r.DistinctAdminUids); missing Users UID=$($r.UidsMissingFromUsers); role mismatch=$($r.RoleMismatch)"}
 Log "Target Persons=$persons; UserAccounts=$accounts; verified Users mappings=$mapUsers; verified Resident mappings=$mapResidents; UserRoleAssignments=$assignments"
 Log 'READ-ONLY AUDIT COMPLETE - no data changed.'
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $lines=@('# Phase 30 - Identity and building scope audit','',"- Run: $(Get-Date -Format o)",'- Mode: READ ONLY','','## Results')
 $lines+=$log|%{"- $_"}
 $lines+=@('','## User-provided classification to validate',
 '- Residents are residents only, not administrators or super administrators.',
 '- Expected resident distribution: 4, 15, and 60 across three buildings.',
 '- Users expected: 2 SuperAdmins and 3 building Admins, one Admin per building.',
 '','## Safety and next decision',
 '- This audit reads counts, role labels, building identifiers, and relationship integrity only; it does not export names, contact details, credentials, or hashes.',
 '- No Persons, UserAccounts, credentials, role assignments, ownership, occupancy, or apartment access grants are created or changed.',
 '- Resident login accounts will be created only after exact identity/account policy is confirmed; residents are not assigned administrative roles.',
 '- Admin role assignments must be scoped to the exact BuildingId verified from BuildingAdmins; a missing third building-admin link must not be fabricated.',
 '- Temporary password provisioning must use BCrypt, force change at first login, and be limited to verified accounts.',
 '- Legacy source tables remain unchanged.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{
 $script=Join-Path $PSScriptRoot 'Invoke-Phase30-IdentityAndBuildingScopeAudit.ps1'
 git add -- $script $report
 if((git status --short -- $script $report).Count -gt 0){
  git commit --only -m "feat: add identity and building scope audit" -- $script $report
  git push origin development/database-redesign-enterprise
 }
}finally{Pop-Location}
