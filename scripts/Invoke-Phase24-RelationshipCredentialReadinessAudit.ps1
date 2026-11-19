[CmdletBinding()]
param([string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30")
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase24-RelationshipCredentialReadiness-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Scalar($c,[string]$sql){$cmd=$c.CreateCommand();$cmd.CommandTimeout=180;$cmd.CommandText=$sql;return $cmd.ExecuteScalar()}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open()
 $db=[string](Scalar $c "SELECT DB_NAME()")
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @('Residents','Apartments','Buildings','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')){
  $v=Scalar $c "SELECT OBJECT_ID(N'dbo.$t',N'U')"
  if($null -eq $v -or $v -is [DBNull]){throw "Missing dbo.$t"}
 }
 $residents=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Residents")
 $owners=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Residents WHERE IsOwner=1")
 $primary=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Residents WHERE IsPrimary=1")
 $ownerWithNoUid=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Residents WHERE IsOwner=1 AND (Uid IS NULL OR LEN(Uid)=0)")
 $badApartment=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Residents r WHERE NOT EXISTS(SELECT 1 FROM dbo.Apartments a WHERE a.BuildingId=r.BuildingId AND a.Id=r.ApartmentId)")
 $multiplePrimary=[int](Scalar $c "SELECT COUNT(*) FROM (SELECT BuildingId,ApartmentId FROM dbo.Residents WHERE IsPrimary=1 GROUP BY BuildingId,ApartmentId HAVING COUNT(*)>1) q")
 $credentialColumns=@()
 foreach($col in @('PinHash','PasswordHash')){
  $exists=Scalar $c "SELECT COL_LENGTH(N'dbo.Residents',N'$col')"
  if($null -ne $exists -and $exists -isnot [DBNull]){
   $n=[int](Scalar $c "SELECT COUNT(*) FROM dbo.Residents WHERE [$col] IS NOT NULL AND LEN(CONVERT(nvarchar(max),[$col]))>0")
   $credentialColumns+=("$col populated rows: $n")
  }
 }
 Log "Database: $db"
 Log "Residents source rows: $residents"
 Log "Residents marked IsOwner: $owners; IsPrimary: $primary"
 Log "Owner rows without UID: $ownerWithNoUid"
 Log "Resident rows without exact apartment reference: $badApartment"
 Log "Apartments with multiple IsPrimary residents: $multiplePrimary"
 foreach($x in $credentialColumns){Log $x}
 Log 'No credential values were selected or logged.'
 if($badApartment -gt 0){Log 'RELATIONSHIP REVIEW BLOCKED: orphan apartment references exist.'}
 if($multiplePrimary -gt 0){Log 'PRIMARY OCCUPANCY REVIEW REQUIRED: multiple primary residents exist for one apartment.'}
 Log 'READ-ONLY PROFILE COMPLETE - no ownership, occupancy, credentials, or identity links were created.'
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $lines=@('# Phase 24 - Relationship and credential readiness audit','',"- Run: $(Get-Date -Format o)",'- Mode: READ ONLY','','## Results')
 $lines+=$log|%{"- $_"}
 $lines+=@('','## Decision','- IsOwner is only a legacy flag; it does not prove legal title, share percentage, evidence, or effective dates.','- IsPrimary is only a legacy flag; it does not establish occupancy start/end dates.','- No ownership, occupancy, access grant, Person, or Account rows are created.','- Credential profiling reports counts only; PIN/password values and hashes are never emitted or copied.','- Legacy source rows are never updated or deleted.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{
 $script=Join-Path $PSScriptRoot 'Invoke-Phase24-RelationshipCredentialReadinessAudit.ps1'
 git add -- $script $report
 if((git status --short -- $script $report).Count -gt 0){git commit --only -m "docs: add relationship and credential readiness audit" -- $script $report;git push origin development/database-redesign-enterprise}
}finally{Pop-Location}
