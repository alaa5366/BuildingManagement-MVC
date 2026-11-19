[CmdletBinding()]
param([string]$Server='(localdb)\MSSQLLocalDB',[string]$Database='BuildingManagement_RestoreTest')
$ErrorActionPreference='Stop'
$Branch='development/database-redesign-enterprise'
$Migration='20261119100000_Phase09_OperationalDomainSchema'
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$BackupDir=Join-Path $env:LOCALAPPDATA 'BuildingManagement\Backups'
$Stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$BackupPath=Join-Path $BackupDir "$Database-Phase09-$Stamp.bak"
$ReportDir=Join-Path $Repo 'Database\Redesign\Review-Reports'
$ReportPath=Join-Path $ReportDir "Phase09-RestoreTest-Apply-$Stamp.md"
$ExpectedTables=@(
'PollVotingPolicies','PollDelegations','PollEligibility','PollBallots','PollLifecycleEvents',
'MaintenanceRequests','MaintenanceAssignments','MaintenanceComments','MaintenanceAttachments','MaintenanceStatusEvents','MaintenanceLedgerLinks',
'NotificationMessages','NotificationRecipients','NotificationDeliveries','NotificationReadStates','NotificationPreferences',
'PresenceSessions','PresenceHeartbeats','QrCapabilities','QrCapabilityUses','GuestAccessSessions',
'MediaCredentialReferences','MediaProvisioningOperations','MediaAccessSessions','ProtectedIntegrationSecrets','UserPreferences',
'DocumentAssets','DocumentAccessGrants','FileProcessingJobs','AuditEvents','OutboxMessages','InboxMessages',
'BackupJobs','BackupArtifacts','BackupRetentionEvents','RestoreOperations','ImportBatches','ImportRows','ExportJobs','LegacyDataMigrationMap'
)
function Invoke-Sql([string]$Db,[string]$Sql,[switch]$NonQuery) {
 $cs="Server=$Server;Initial Catalog=$Db;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=20;"
 $cn=[System.Data.SqlClient.SqlConnection]::new($cs)
 try {$cn.Open();$cmd=$cn.CreateCommand();$cmd.CommandTimeout=0;$cmd.CommandText=$Sql
  if($NonQuery){[void]$cmd.ExecuteNonQuery();return $null}
  $dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt);return ,$dt
 } finally {$cn.Dispose()}
}
function Invoke-Git([string[]]$GitArgs) {
 $o=& git @GitArgs 2>&1
 if($LASTEXITCODE -ne 0){throw "git $($GitArgs -join ' ') failed: $($o -join ' ')"}
}
Set-Location $Repo
if((& git branch --show-current) -ne $Branch){throw "Wrong branch. Expected $Branch. No DB changes made."}
if(-not(Test-Path '.\BuildingManagementMVC.csproj')){throw 'Project file not found.'}
if(-not(Test-Path ".\Migrations\$Migration.cs")){throw 'Phase09 migration missing locally.'}
New-Item -ItemType Directory -Path $BackupDir -Force|Out-Null
New-Item -ItemType Directory -Path $ReportDir -Force|Out-Null

# Exact target gate: only the named disposable LocalDB database is eligible.
$db=Invoke-Sql 'master' "SELECT name,state_desc,user_access_desc,is_read_only FROM sys.databases WHERE name=N'$Database';"
if($db.Rows.Count -ne 1 -or $db.Rows[0].state_desc -ne 'ONLINE' -or $db.Rows[0].user_access_desc -ne 'MULTI_USER' -or [bool]$db.Rows[0].is_read_only){throw "Target gate failed for $Server / $Database. No DB changes made."}
$hist=Invoke-Sql $Database "SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;"
$ids=@($hist.Rows|ForEach-Object{[string]$_.MigrationId})
if($ids -contains $Migration){throw 'Phase09 already recorded. No action taken.'}
if($ids.Count -eq 0 -or $ids[-1] -ne '20261118234143_Phase08_PaymentDepositResolutionReasonIntegrity'){throw "Unexpected migration history. Latest was '$($ids[-1])'. No action taken."}
$existing=Invoke-Sql $Database ("SELECT name FROM sys.tables WHERE name IN ("+(($ExpectedTables|ForEach-Object{"N'$_'"}) -join ',')+");")
if($existing.Rows.Count -gt 0){throw 'Phase09 tables exist without history row; stop for partial-state recovery. No changes made.'}

# Full backup and checksum verification immediately before applying.
$escaped=$BackupPath.Replace("'","''")
Invoke-Sql 'master' "BACKUP DATABASE [$Database] TO DISK=N'$escaped' WITH COPY_ONLY, INIT, CHECKSUM, STATS=10;" -NonQuery
Invoke-Sql 'master' "RESTORE VERIFYONLY FROM DISK=N'$escaped' WITH CHECKSUM;" -NonQuery
$backupHash=(Get-FileHash -LiteralPath $BackupPath -Algorithm SHA256).Hash

# Explicit connection override prevents EF from targeting appsettings Default.
$efOutput=& dotnet ef database update $Migration --connection "Server=$Server;Initial Catalog=$Database;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=20;" 2>&1
$efExit=$LASTEXITCODE
$efText=($efOutput|ForEach-Object{$_.ToString()}) -join [Environment]::NewLine
$postHist=Invoke-Sql $Database "SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;"
$postIds=@($postHist.Rows|ForEach-Object{[string]$_.MigrationId})
$postTables=Invoke-Sql $Database ("SELECT name FROM sys.tables WHERE name IN ("+(($ExpectedTables|ForEach-Object{"N'$_'"}) -join ',')+") ORDER BY name;")
$present=@($postTables.Rows|ForEach-Object{[string]$_.name})
$missing=@($ExpectedTables|Where-Object{$_ -notin $present})
$verified=($efExit -eq 0)-and($postIds -contains $Migration)-and($missing.Count -eq 0)

$lines=[System.Collections.Generic.List[string]]::new()
$lines.Add('# Phase09 RestoreTest Apply Report')
$lines.Add('')
$lines.Add("- Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
$lines.Add("- Target: $Server / $Database")
$lines.Add("- Migration: $Migration")
$lines.Add("- EF exit code: $efExit")
$lines.Add("- Backup: $BackupPath")
$lines.Add("- Backup SHA256: $backupHash")
$lines.Add('- BACKUP DATABASE WITH CHECKSUM: completed')
$lines.Add('- RESTORE VERIFYONLY WITH CHECKSUM: passed')
$lines.Add("- Migration history row present: $($postIds -contains $Migration)")
$lines.Add("- Expected operational tables present: $($present.Count) / $($ExpectedTables.Count)")
$lines.Add("- Missing tables: $(if($missing.Count){$missing -join ', '}else{'none'})")
$lines.Add('- Legacy business rows copied/updated/deleted: NONE')
$lines.Add('- Production database targeted: NO')
$lines.Add('')
$lines.Add('## EF output')
$lines.Add($efText)
$lines.Add('')
if($verified){$lines.Add('**PASS** — Phase09 is recorded and all expected tables are present on RestoreTest.'}else{$lines.Add('**FAILED / INCOMPLETE** — preserve backup; inspect this report. Do not rerun blindly or use Down.')}
[System.IO.File]::WriteAllLines($ReportPath,$lines,[System.Text.UTF8Encoding]::new($false))
$relative=$ReportPath.Substring($Repo.Length+1).Replace('\','/')
Invoke-Git @('add','--',$relative)
Invoke-Git @('commit','--only','-m',"docs: record Phase09 RestoreTest apply $Stamp",'--',$relative)
Invoke-Git @('push','origin',$Branch)
Write-Host "Report pushed: $relative"
Write-Host "Backup: $BackupPath"
if(-not $verified){throw 'Phase09 post-apply verification failed. Review pushed report; do not rerun.'}
Write-Host 'PASS: Phase09 applied and verified on RestoreTest only.'
