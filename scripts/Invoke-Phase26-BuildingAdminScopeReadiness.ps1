[CmdletBinding()]
param([string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30")
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase26-BuildingAdminScopeReadiness-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Scalar($c,[string]$sql){$cmd=$c.CreateCommand();$cmd.CommandTimeout=180;$cmd.CommandText=$sql;return $cmd.ExecuteScalar()}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open()
 $db=[string](Scalar $c "SELECT DB_NAME()")
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @('BuildingAdmins','Users','Buildings','UserAccounts','LegacyIdentityMap','UserRoleAssignments','__EFMigrationsHistory')){
  $v=Scalar $c "SELECT OBJECT_ID(N'dbo.$t',N'U')"
  if($null -eq $v -or $v -is [DBNull]){throw "Missing dbo.$t"}
 }
 foreach($col in @('BuildingId','AdminUid')){
  $v=Scalar $c "SELECT COL_LENGTH(N'dbo.BuildingAdmins',N'$col')"
  if($null -eq $v -or $v -is [DBNull]){throw "Missing dbo.BuildingAdmins.$col"}
 }
 $rows=[int](Scalar $c "SELECT COUNT(*) FROM dbo.BuildingAdmins")
 $distinctAdmins=[int](Scalar $c "SELECT COUNT(DISTINCT AdminUid) FROM dbo.BuildingAdmins WHERE AdminUid IS NOT NULL")
 $distinctBuildings=[int](Scalar $c "SELECT COUNT(DISTINCT BuildingId) FROM dbo.BuildingAdmins WHERE BuildingId IS NOT NULL")
 $unmatchedBuildings=[int](Scalar $c "SELECT COUNT(*) FROM dbo.BuildingAdmins ba WHERE NOT EXISTS(SELECT 1 FROM dbo.Buildings b WHERE CONVERT(nvarchar(64),b.Id)=CONVERT(nvarchar(64),ba.BuildingId))")
 $unmapped=[int](Scalar $c "SELECT COUNT(*) FROM dbo.BuildingAdmins ba WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyIdentityMap m WHERE m.SourceTable=N'Users' AND m.SourceKey=CONVERT(nvarchar(256),ba.AdminUid) AND m.AccountId IS NOT NULL AND m.MappingStatus=1)")
 $mapped=[int](Scalar $c "SELECT COUNT(*) FROM dbo.BuildingAdmins ba WHERE EXISTS(SELECT 1 FROM dbo.LegacyIdentityMap m WHERE m.SourceTable=N'Users' AND m.SourceKey=CONVERT(nvarchar(256),ba.AdminUid) AND m.AccountId IS NOT NULL AND m.MappingStatus=1)")
 $roles=[int](Scalar $c "SELECT COUNT(*) FROM dbo.UserRoleAssignments")
 Log "Database: $db"
 Log "BuildingAdmins source rows: $rows; distinct AdminUid: $distinctAdmins; distinct BuildingId: $distinctBuildings"
 Log "Rows with no exact target Building: $unmatchedBuildings"
 Log "Rows with verified User->Account mapping: $mapped; unresolved account mappings: $unmapped"
 Log "Existing target UserRoleAssignments: $roles"
 if($unmapped -gt 0){Log 'ROLE MIGRATION BLOCKED: admin accounts are not linked to verified target accounts.'}
 elseif($unmatchedBuildings -gt 0){Log 'ROLE MIGRATION BLOCKED: building references need reconciliation.'}
 else{Log 'STRUCTURAL READINESS ONLY: explicit BuildingAdmins rows and account mappings exist; role semantics and active status still require review.'}
 Log 'READ-ONLY AUDIT COMPLETE - no role assignments or permissions created.'
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $lines=@('# Phase 26 - Building admin scope readiness audit','',"- Run: $(Get-Date -Format o)",'- Mode: READ ONLY','','## Results')
 $lines+=$log|%{"- $_"}
 $lines+=@('','## Decision','- BuildingAdmins rows are the only source for building-scoped administrator scope.','- No system-wide role is inferred from a building admin row.','- No UserRoleAssignments, permissions, or account links are created.','- Account mapping requires a verified LegacyIdentityMap Users row with AccountId and MappingStatus=1.','- Legacy source rows are never updated or deleted.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{
 $script=Join-Path $PSScriptRoot 'Invoke-Phase26-BuildingAdminScopeReadiness.ps1'
 git add -- $script $report
 if((git status --short -- $script $report).Count -gt 0){git commit --only -m "docs: add building admin scope readiness audit" -- $script $report;git push origin development/database-redesign-enterprise}
}finally{Pop-Location}
