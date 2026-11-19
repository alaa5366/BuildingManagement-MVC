[CmdletBinding()]
param([switch]$Apply,[string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30")
$ErrorActionPreference='Stop';$root=Split-Path -Parent $PSScriptRoot;$dir=Join-Path $root 'Database/Redesign/Review-Reports';New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase21-GlobalSettings-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md");$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Query($c,[string]$sql){$cmd=$c.CreateCommand();$cmd.CommandTimeout=180;$cmd.CommandText=$sql;$dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt);return ,$dt}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open();$db=(Query $c "SELECT DB_NAME() N").Rows[0].N;if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @('GlobalSettings','GlobalApplicationSettings','__EFMigrationsHistory')){$x=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N;if($null -eq $x -or $x -is [DBNull]){throw "Missing dbo.$t"}}
 $m=[int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119210000_Phase21_GlobalApplicationSettings'").Rows[0].N;if($m -ne 1){throw 'Phase21 migration not applied'}
 $source=[int](Query $c "SELECT COUNT(*) N FROM dbo.GlobalSettings").Rows[0].N;if($source -ne 1){throw "Expected exactly one GlobalSettings row, found $source"}
 $bad=[int](Query $c "SELECT COUNT(*) N FROM dbo.GlobalSettings WHERE Id IS NULL OR LEN(Id)>64 OR LEN(AppName)>200 OR LEN(DefaultLanguage)>16 OR SessionTimeoutDays NOT BETWEEN 1 AND 365 OR MaxPinAttempts NOT BETWEEN 1 AND 100 OR LEN(StorageMode)>32 OR LEN(UpdatedBy)>160").Rows[0].N
 if($bad){throw "Preflight blocked: $bad settings rows violate target limits."}
 Log "Database: $db";Log "Source global settings: $source"
 if(-not $Apply){Log 'DRY RUN - no data changed'}
 else{
  $tx=$c.BeginTransaction()
  try{
   $cmd=$c.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandText=@"
INSERT dbo.GlobalApplicationSettings(SettingsId,AppName,DefaultLanguage,EnablePolls,EnableMaintenance,EnableOcr,EnableQrAccess,EnableWhatsApp,SessionTimeoutDays,MaxPinAttempts,StorageMode,UpdatedAtUtc,UpdatedByReference)
SELECT Id,AppName,DefaultLanguage,EnablePolls,EnableMaintenance,EnableOcr,EnableQrAccess,EnableWhatsApp,SessionTimeoutDays,MaxPinAttempts,StorageMode,UpdatedAt,UpdatedBy
FROM dbo.GlobalSettings s
WHERE NOT EXISTS(SELECT 1 FROM dbo.GlobalApplicationSettings t WHERE t.SettingsId=s.Id);
"@
   [void]$cmd.ExecuteNonQuery();$tx.Commit()
  }catch{$tx.Rollback();throw}
  $target=[int](Query $c "SELECT COUNT(*) N FROM dbo.GlobalApplicationSettings").Rows[0].N
  $match=[int](Query $c "SELECT COUNT(*) N FROM dbo.GlobalSettings s JOIN dbo.GlobalApplicationSettings t ON t.SettingsId=s.Id AND t.AppName=s.AppName AND t.DefaultLanguage=s.DefaultLanguage AND t.EnablePolls=s.EnablePolls AND t.EnableMaintenance=s.EnableMaintenance AND t.EnableOcr=s.EnableOcr AND t.EnableQrAccess=s.EnableQrAccess AND t.EnableWhatsApp=s.EnableWhatsApp AND t.SessionTimeoutDays=s.SessionTimeoutDays AND t.MaxPinAttempts=s.MaxPinAttempts AND t.StorageMode=s.StorageMode AND (t.UpdatedAtUtc=s.UpdatedAt OR (t.UpdatedAtUtc IS NULL AND s.UpdatedAt IS NULL)) AND (t.UpdatedByReference=s.UpdatedBy OR (t.UpdatedByReference IS NULL AND s.UpdatedBy IS NULL))").Rows[0].N
  Log "Target settings: $target";Log "Exact safe-field matches: $match"
  if($target -ne 1 -or $match -ne 1){throw 'Reconciliation failed.'};Log 'RECONCILIATION PASS'
 }
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()};$mode=if($Apply){'APPLY'}else{'DRY RUN'}
 $lines=@('# Phase 21 - Global application settings backfill','',"- Run: $(Get-Date -Format o)","- Mode: $mode",'','## Results');$lines+=$log|%{"- $_"}
 $lines+=@('','## Secret handling','- WhatsApp API token and Cloudinary API key are not copied.','- Provider credentials require explicit vault encryption/key-version handling and rotation; no plaintext or unverifiable ciphertext is migrated.','- All non-secret operational settings are copied exactly.','- Source GlobalSettings is never modified or deleted.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8;Write-Host "Report: $report"
}
Push-Location $root
try{$sp=Join-Path $PSScriptRoot 'Invoke-Phase21-GlobalSettingsBackfill.ps1';git add -- $sp $report;if((git status --short -- $sp $report).Count -gt 0){git commit --only -m "feat: migrate safe global application settings" -- $sp $report;git push origin development/database-redesign-enterprise}}finally{Pop-Location}
