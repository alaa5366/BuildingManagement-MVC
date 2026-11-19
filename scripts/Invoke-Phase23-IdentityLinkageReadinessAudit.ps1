[CmdletBinding()]
param([string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30")
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase23-IdentityLinkageReadiness-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Scalar($c,[string]$sql){$cmd=$c.CreateCommand();$cmd.CommandTimeout=180;$cmd.CommandText=$sql;return $cmd.ExecuteScalar()}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open()
 $db=[string](Scalar $c "SELECT DB_NAME()")
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @('Users','Residents','LegacyIdentityMap','Persons','UserAccounts','LegacyOperationalRecords','__EFMigrationsHistory')){
  $v=Scalar $c "SELECT OBJECT_ID(N'dbo.$t',N'U')"
  if($null -eq $v -or $v -is [DBNull]){throw "Missing dbo.$t"}
 }
 $users=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Users")
 $residents=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Residents")
 $overlap=[int](Scalar $c "SELECT COUNT(*) FROM (SELECT DISTINCT u.Uid FROM dbo.Users u INNER JOIN dbo.Residents r ON r.Uid=u.Uid WHERE u.Uid IS NOT NULL AND LEN(u.Uid)>0) q")
 $duplicateUsers=[int](Scalar $c "SELECT COUNT(*) FROM (SELECT Uid FROM dbo.Users WHERE Uid IS NOT NULL AND LEN(Uid)>0 GROUP BY Uid HAVING COUNT(*)>1) q")
 $duplicateResidents=[int](Scalar $c "SELECT COUNT(*) FROM (SELECT Uid FROM dbo.Residents WHERE Uid IS NOT NULL AND LEN(Uid)>0 GROUP BY Uid HAVING COUNT(*)>1) q")
 $userMaps=[int](Scalar $c "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceTable=N'Users'")
 $residentMaps=[int](Scalar $c "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceTable=N'Residents'")
 $linkedUsers=[int](Scalar $c "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceTable=N'Users' AND MappingStatus=1 AND PersonId IS NOT NULL")
 $linkedResidents=[int](Scalar $c "SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceTable=N'Residents' AND MappingStatus=1 AND PersonId IS NOT NULL")
 $accounts=[int](Scalar $c "SELECT COUNT(*) FROM dbo.UserAccounts")
 $people=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Persons")
 Log "Database: $db"
 Log "Users source rows: $users; Residents source rows: $residents"
 Log "Exact Users/Residents UID overlap: $overlap"
 Log "Duplicate Users UIDs: $duplicateUsers; duplicate Residents UIDs: $duplicateResidents"
 Log "LegacyIdentityMap rows: Users=$userMaps; Residents=$residentMaps"
 Log "Linked identity dispositions: Users=$linkedUsers; Residents=$linkedResidents"
 Log "Target Persons=$people; UserAccounts=$accounts"
 Log "Unresolved Users map rows: $($userMaps-$linkedUsers); unresolved Residents map rows: $($residentMaps-$linkedResidents)"
 if($duplicateUsers -gt 0 -or $duplicateResidents -gt 0){Log 'LINKAGE BLOCKED: duplicate legacy UIDs require manual disposition.'}
 elseif($overlap -eq 0){Log 'LINKAGE BLOCKED: no exact cross-table UID matches; do not auto-link identities.'}
 else{Log 'LINKAGE REVIEW REQUIRED: exact UID overlap is only a candidate and is not proof of a shared human.'}
 Log 'READ-ONLY AUDIT COMPLETE - no source or target rows changed'
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $lines=@('# Phase 23 - Identity linkage readiness audit','',"- Run: $(Get-Date -Format o)",'- Mode: READ ONLY','','## Results')
 $lines+=$log|%{"- $_"}
 $lines+=@('','## Decision','- This phase performs counts only; it does not create Persons, UserAccounts, credentials, role assignments, or identity links.','- Exact UID equality is a review candidate only. Shared contact data, names, apartment membership, and role labels are not identity proof.','- Credential and MFA material is not selected, logged, or copied.','- Legacy source rows are never updated or deleted.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{
 $script=Join-Path $PSScriptRoot 'Invoke-Phase23-IdentityLinkageReadinessAudit.ps1'
 git add -- $script $report
 if((git status --short -- $script $report).Count -gt 0){git commit --only -m "docs: add identity linkage readiness audit" -- $script $report;git push origin development/database-redesign-enterprise}
}finally{Pop-Location}
