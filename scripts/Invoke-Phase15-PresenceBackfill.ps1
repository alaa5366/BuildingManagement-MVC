[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$reportDir = Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$report = Join-Path $reportDir ("Phase15-Presence-Backfill-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + ".md")
$log = [System.Collections.Generic.List[string]]::new()
function Log([string]$Message) { $log.Add($Message); Write-Host $Message }
function Query($Connection, [string]$Sql) {
    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = 180
    $table = [System.Data.DataTable]::new()
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    [void]$adapter.Fill($table)
    return ,$table
}

$connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try {
    $connection.Open()
    $database = (Query $connection "SELECT DB_NAME() AS DatabaseName").Rows[0].DatabaseName
    if ($database -ne 'BuildingManagement_RestoreTest') { throw "Target database mismatch: $database" }

    foreach ($tableName in @('Presence', 'PresenceSessions', 'PresenceHeartbeats', 'LegacyDataMigrationMap', '__EFMigrationsHistory')) {
        $exists = (Query $connection "SELECT OBJECT_ID(N'dbo.$tableName', N'U') AS ObjectId").Rows[0].ObjectId
        if ($null -eq $exists -or $exists -is [DBNull]) { throw "Missing required table: dbo.$tableName" }
    }
    $migrationCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261119150000_Phase15_PresenceLegacyObservation'").Rows[0].N
    if ($migrationCount -ne 1) { throw 'Phase15 schema migration is not applied.' }

    $invalid = [int](Query $connection @"
SELECT COUNT(*) AS N
FROM dbo.Presence
WHERE Uid IS NULL OR LEN(Uid)=0 OR LEN(Uid)>128
   OR (BuildingId IS NOT NULL AND LEN(BuildingId)>64)
   OR (ApartmentId IS NOT NULL AND LEN(ApartmentId)>64)
   OR LastSeen IS NULL;
"@).Rows[0].N
    if ($invalid -gt 0) { throw "Preflight blocked: $invalid Presence row(s) have invalid keys, scope lengths, or missing timestamps." }

    $sourceCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.Presence").Rows[0].N
    $mappedCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'Presence'").Rows[0].N
    $pendingCount = [int](Query $connection @"
SELECT COUNT(*) AS N FROM dbo.Presence s
WHERE NOT EXISTS (SELECT 1 FROM dbo.LegacyDataMigrationMap m
 WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Presence' AND m.SourceKey=s.Uid);
"@).Rows[0].N
    Log "Database: $database"
    Log "Source rows: $sourceCount"
    Log "Already mapped: $mappedCount"
    Log "Pending: $pendingCount"

    if (-not $Apply) {
        Log 'DRY RUN - no data changed'
    } else {
        $transaction = $connection.BeginTransaction()
        try {
            $sql = @"
SET NOCOUNT ON;
;WITH S AS (
 SELECT p.*,
  CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.presence.session.v1|',p.Uid))),1,16)) AS SessionId,
  HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT p.Uid,p.ApartmentId,p.BuildingId,p.LastSeen FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES))) AS Fingerprint
 FROM dbo.Presence p
)
INSERT dbo.PresenceSessions
 (PresenceSessionId,AccountId,LegacyUid,BuildingId,ApartmentId,DeviceReference,StartedAtUtc,LastSeenAtUtc,ExpiresAtUtc,EndedAtUtc,Status)
SELECT s.SessionId,NULL,s.Uid,s.BuildingId,s.ApartmentId,NULL,NULL,s.LastSeen,
       DATEADD(MINUTE,2,s.LastSeen),NULL,0
FROM S s
WHERE NOT EXISTS (
 SELECT 1 FROM dbo.LegacyDataMigrationMap m
 WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Presence' AND m.SourceKey=s.Uid
);

;WITH S AS (
 SELECT p.*,
  CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.presence.session.v1|',p.Uid))),1,16)) AS SessionId,
  HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT p.Uid,p.ApartmentId,p.BuildingId,p.LastSeen FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES))) AS Fingerprint
 FROM dbo.Presence p
)
INSERT dbo.LegacyDataMigrationMap
 (MigrationMapId,SourceSystem,SourceTable,SourceKey,TargetTable,TargetKey,SourceFingerprint,Outcome,Disposition,SafeNote)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.presence.map.v1|',s.Uid))),1,16)),
 N'BuildingManagement.Legacy',N'Presence',s.Uid,N'PresenceSessions',CONVERT(nvarchar(36),s.SessionId),
 s.Fingerprint,1,N'MigratedWithLegacyMetadata',
 N'Last-seen observation only; no account identity, session start, device, or historical heartbeat inferred.'
FROM S s
WHERE NOT EXISTS (
 SELECT 1 FROM dbo.LegacyDataMigrationMap m
 WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Presence' AND m.SourceKey=s.Uid
);
"@
            $command = $connection.CreateCommand()
            $command.Transaction = $transaction
            $command.CommandText = $sql
            $command.CommandTimeout = 300
            [void]$command.ExecuteNonQuery()
            $transaction.Commit()
        } catch { $transaction.Rollback(); throw }

        $targetCount = [int](Query $connection @"
SELECT COUNT(*) AS N FROM dbo.PresenceSessions t
JOIN dbo.LegacyDataMigrationMap m ON m.TargetKey=CONVERT(nvarchar(36),t.PresenceSessionId)
WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Presence';
"@).Rows[0].N
        $provenanceCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'Presence'").Rows[0].N
        $heartbeatCount = [int](Query $connection @"
SELECT COUNT(*) AS N FROM dbo.PresenceHeartbeats h
JOIN dbo.PresenceSessions s ON s.PresenceSessionId=h.PresenceSessionId
JOIN dbo.LegacyDataMigrationMap m ON m.TargetKey=CONVERT(nvarchar(36),s.PresenceSessionId)
WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=N'Presence';
"@).Rows[0].N
        Log "Target sessions: $targetCount"
        Log "Provenance rows: $provenanceCount"
        Log "Synthetic heartbeat rows: $heartbeatCount"
        if ($targetCount -ne $sourceCount -or $provenanceCount -ne $sourceCount -or $heartbeatCount -ne 0) {
            throw "Reconciliation failed: source=$sourceCount target=$targetCount provenance=$provenanceCount heartbeat=$heartbeatCount"
        }
        Log 'RECONCILIATION PASS'
    }
} catch {
    Log "ERROR: $($_.Exception.Message)"
    throw
} finally {
    if ($connection.State -ne 'Closed') { $connection.Close() }
    $mode = if ($Apply) { 'APPLY' } else { 'DRY RUN' }
    $lines = @(
      '# Phase 15 - Presence Backfill','',"- Run: $(Get-Date -Format o)","- Mode: $mode",
      '- Source rows retained: dbo.Presence','','## Results'
    )
    $lines += $log | ForEach-Object { "- $_" }
    $lines += @(
      '','## Preservation rules',
      '- One legacy last-seen row maps to one PresenceSessions row with a deterministic ID.',
      '- StartedAtUtc remains NULL because the legacy source has no session-start timestamp.',
      '- AccountId and DeviceReference remain NULL; legacy UID is retained only as a source reference.',
      '- ExpiresAtUtc is LastSeen + the application two-minute online threshold; no historical heartbeat rows are fabricated.',
      '- The source Presence table is never updated or deleted.'
    )
    Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
    Write-Host "Report: $report"
}
Push-Location $root
try {
    $scriptPath = Join-Path $PSScriptRoot 'Invoke-Phase15-PresenceBackfill.ps1'
    git add -- $scriptPath $report
    if ((git status --short -- $scriptPath $report).Count -gt 0) {
        git commit --only -m "feat: backfill legacy presence observations" -- $scriptPath $report
        git push origin development/database-redesign-enterprise
    }
} finally { Pop-Location }
