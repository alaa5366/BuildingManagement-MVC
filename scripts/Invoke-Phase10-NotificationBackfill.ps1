[CmdletBinding()]
param([switch]$Apply,[string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30")
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase10-Notification-Backfill-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$x){$log.Add($x);Write-Host $x}
function Query($c,[string]$s){$cmd=$c.CreateCommand();$cmd.CommandText=$s;$cmd.CommandTimeout=180;$dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt);$dt}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
$c.Open()
$db=(Query $c "SELECT DB_NAME() N").Rows[0].N
if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
foreach($t in @('Notifications','NotificationMessages','NotificationRecipients','NotificationReadStates','LegacyDataMigrationMap','__EFMigrationsHistory')){$v=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') O").Rows[0].O;if($null -eq $v -or $v -is [DBNull]){throw "Missing table: $t"}}
$m=(Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119100000_Phase09_OperationalDomainSchema'").Rows[0].N
if([int]$m -ne 1){throw 'Phase09 is not applied'}
$src=[int](Query $c "SELECT COUNT(*) N FROM dbo.Notifications").Rows[0].N
$old=[int](Query $c "SELECT COUNT(*) N FROM dbo.NotificationMessages WHERE Origin='legacy.notifications.v1'").Rows[0].N
$pending=[int](Query $c "SELECT COUNT(*) N FROM dbo.Notifications s WHERE NOT EXISTS(SELECT 1 FROM dbo.NotificationMessages t WHERE t.LegacyNotificationId=CONCAT(s.BuildingId,N'/',s.Id))").Rows[0].N
Log "Database: $db";Log "Source: $src";Log "Existing migrated: $old";Log "Pending: $pending"
if(-not $Apply){Log 'DRY RUN - no data changed'}
else{
$tx=$c.BeginTransaction()
try{
$sql=@"
SET NOCOUNT ON;
;WITH S AS (SELECT n.*,CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.notifications.v1|',BuildingId,N'|',Id)),1,16)) Mid,CONCAT(BuildingId,N'/',Id) LKey FROM dbo.Notifications n)
INSERT dbo.NotificationMessages(MessageId,BuildingId,LegacyNotificationId,Title,Body,Channel,Origin,CreatedAtUtc,ExpiresAtUtc)
SELECT Mid,BuildingId,LKey,Title,Body,0,N'legacy.notifications.v1',Ts,NULL FROM S
WHERE NOT EXISTS(SELECT 1 FROM dbo.NotificationMessages t WHERE t.LegacyNotificationId=S.LKey);
;WITH S AS (SELECT n.*,CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.notifications.recipient.v1|',BuildingId,N'|',Id)),1,16)) Rid,CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.notifications.v1|',BuildingId,N'|',Id)),1,16)) Mid FROM dbo.Notifications n)
INSERT dbo.NotificationRecipients(RecipientId,MessageId,RecipientType,RecipientReference,ApartmentId,CreatedAtUtc)
SELECT Rid,Mid,CASE WHEN LOWER(RecipientType)=N'apartment' THEN 2 WHEN LOWER(RecipientType)=N'building' THEN 1 WHEN LOWER(RecipientType)=N'user' THEN 3 ELSE 0 END,COALESCE(NULLIF(RecipientApartmentId,N''),BuildingId),RecipientApartmentId,Ts FROM S
WHERE NOT EXISTS(SELECT 1 FROM dbo.NotificationRecipients r WHERE r.RecipientId=S.Rid);
;WITH S AS (SELECT n.*,CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.notifications.recipient.v1|',BuildingId,N'|',Id)),1,16)) Rid,CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.notifications.read.v1|',BuildingId,N'|',Id)),1,16)) ReadId FROM dbo.Notifications n)
INSERT dbo.NotificationReadStates(ReadStateId,RecipientId,ReadAtUtc,DismissedAtUtc)
SELECT ReadId,Rid,ReadAt,NULL FROM S WHERE IsRead=1 AND ReadAt IS NOT NULL
AND NOT EXISTS(SELECT 1 FROM dbo.NotificationReadStates x WHERE x.RecipientId=S.Rid);
;WITH S AS (SELECT n.*,CONCAT(BuildingId,N'/',Id) LKey,HASHBYTES('SHA2_256',CONCAT(BuildingId,N'|',Id,N'|',CONVERT(nvarchar(33),Ts,126),N'|',RecipientType,N'|',COALESCE(RecipientApartmentId,N''),N'|',Type,N'|',Icon,N'|',Title,N'|',Body,N'|',CONVERT(nvarchar(1),IsRead),N'|',COALESCE(CONVERT(nvarchar(33),ReadAt,126),N''))) FP FROM dbo.Notifications n)
INSERT dbo.LegacyDataMigrationMap(MigrationMapId,SourceSystem,SourceTable,SourceKey,TargetTable,TargetKey,SourceFingerprint,Outcome,Disposition,SafeNote)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONCAT(N'legacy.notifications.map.v1|',BuildingId,N'|',Id)),1,16)),N'BuildingManagement.Legacy',N'Notifications',LKey,N'NotificationMessages',LKey,FP,1,N'Migrated',N'Legacy recipient retained; person not inferred.'
FROM S WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap x WHERE x.SourceSystem=N'BuildingManagement.Legacy' AND x.SourceTable=N'Notifications' AND x.SourceKey=S.LKey);
"@
$cmd=$c.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandText=$sql;$cmd.CommandTimeout=300;[void]$cmd.ExecuteNonQuery();$tx.Commit()
}catch{$tx.Rollback();throw}
$after=[int](Query $c "SELECT COUNT(*) N FROM dbo.NotificationMessages WHERE Origin='legacy.notifications.v1'").Rows[0].N
$maps=[int](Query $c "SELECT COUNT(*) N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'Notifications'").Rows[0].N
Log "Target: $after";Log "Provenance: $maps"
if($after -ne $src -or $maps -ne $src){throw "Reconciliation failed: source=$src target=$after map=$maps"}
Log 'RECONCILIATION PASS'
}
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
if($c.State -ne 'Closed'){$c.Close()}
$mode=if($Apply){'APPLY'}else{'DRY RUN'}
$lines=@("# Phase 10 - Notification Backfill","","- Run: $(Get-Date -Format o)","- Mode: $mode","- Source rows retained: dbo.Notifications","","## Results")
$lines+=($log|ForEach-Object{"- $_"})
$lines+=@("","## Rules","- Source key: BuildingId/Id.","- No person identity inferred from apartment/building recipients.","- Read state copied only when IsRead=1 and ReadAt exists.","- Deterministic IDs and source map make reruns idempotent.","- No source rows are updated or deleted.")
Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
Write-Host "Report: $report"
}
Push-Location $root
try{git add -- scripts/Invoke-Phase10-NotificationBackfill.ps1 $report;if((git diff --cached --name-only).Count -gt 0){git commit -m "feat: backfill legacy notifications";git push origin development/database-redesign-enterprise}}finally{Pop-Location}
