[CmdletBinding()]
param([string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30")
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase27-FullLegacyReconciliationAudit-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Query($c,[string]$sql){$cmd=$c.CreateCommand();$cmd.CommandTimeout=240;$cmd.CommandText=$sql;$dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt);return ,$dt}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open()
 $db=[string](Query $c "SELECT DB_NAME() N").Rows[0].N
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @('LegacyDataMigrationMap','__EFMigrationsHistory')){$v=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N;if($null -eq $v -or $v -is [DBNull]){throw "Missing dbo.$t"}}
 $tables=@('Buildings','Floors','Apartments','BuildingSettings','FinancialCategories','Users','Residents','UserPermissions','BuildingAdmins','Persons','UserAccounts','UserCredentials','RoleDefinitions','RolePermissions','UserRoleAssignments','AccountPermissionOverrides','UserTwoFactorSettings','UserRecoveryCodes','LegacyIdentityMap','ApartmentOwnerships','ApartmentOccupancies','ApartmentAccessGrants','Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments','Polls','PollOptions','PollVotes','MaintenanceRecords','Notifications','Presence','Dvrs','Cameras','QrTokens','QrUsages','AuditLog','BackupHistory','ScheduledBackupSettings','GlobalSettings','SyncPendingChanges','LoginThrottle')
 $results=@()
 foreach($name in $tables){
  $exists=(Query $c "SELECT OBJECT_ID(N'dbo.$name',N'U') N").Rows[0].N
  if($null -eq $exists -or $exists -is [DBNull]){Log "${name}: source/target table missing";continue}
  $count=[int](Query $c "SELECT COUNT_BIG(*) N FROM dbo.[$name]").Rows[0].N
  $mapSql="SELECT COUNT_BIG(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceTable=@t"
  $cmd=$c.CreateCommand();$cmd.CommandText=$mapSql;$p=$cmd.Parameters.Add("@t",[System.Data.SqlDbType]::NVarChar,128);$p.Value=$name
  $mapped=[int]$cmd.ExecuteScalar()
  $results+= [pscustomobject]@{Table=$name;Rows=$count;Provenance=$mapped}
  Log "${name}: rows=$count; provenance=$mapped"
 }
 $totalRows=($results|Measure-Object Rows -Sum).Sum
 $totalMap=($results|Measure-Object Provenance -Sum).Sum
 Log "TOTAL listed table rows=$totalRows; provenance rows=$totalMap"
 Log 'Counts are structural reconciliation indicators only; provenance rows do not prove semantic identity or authorization correctness.'
 Log 'No data values were selected. No source or target rows were modified.'
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $lines=@('# Phase 27 - Full legacy reconciliation audit','',"- Run: $(Get-Date -Format o)",'- Mode: READ ONLY','','## Results')
 $lines+=$log|%{"- $_"}
 $lines+=@('','## Interpretation','- A row count and provenance count are not necessarily expected to match for canonical reference tables, aggregated evidence, or intentionally excluded sensitive data.','- Review zero-provenance rows and count differences against the approved mapping policy before any new backfill.','- This phase performs no migration and changes no source or target data.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{
 $script=Join-Path $PSScriptRoot 'Invoke-Phase27-FullLegacyReconciliationAudit.ps1'
 git add -- $script $report
 if((git status --short -- $script $report).Count -gt 0){git commit --only -m "docs: add full legacy reconciliation audit" -- $script $report;git push origin development/database-redesign-enterprise}
}finally{Pop-Location}
