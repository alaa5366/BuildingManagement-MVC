[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$reportDir = Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$report = Join-Path $reportDir ("Phase14-ScheduledBackupSettings-Backfill-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + ".md")
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

    foreach ($tableName in @('ScheduledBackupSettings', 'BackupScheduleConfigurations', 'LegacyDataMigrationMap', '__EFMigrationsHistory')) {
        $exists = (Query $connection "SELECT OBJECT_ID(N'dbo.$tableName', N'U') AS ObjectId").Rows[0].ObjectId
        if ($null -eq $exists -or $exists -is [DBNull]) { throw "Missing required table: dbo.$tableName" }
    }

    $migrationCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261119140000_Phase14_BackupScheduleConfiguration'").Rows[0].N
    if ($migrationCount -ne 1) { throw 'Phase14 schema migration is not applied.' }

    $invalid = [int](Query $connection @"
SELECT COUNT(*) AS N
FROM dbo.ScheduledBackupSettings
WHERE Id IS NULL OR LEN(Id) = 0
   OR Hour < 0 OR Hour > 23
   OR (DayOfWeek IS NOT NULL AND (DayOfWeek < 0 OR DayOfWeek > 6))
   OR (DayOfMonth IS NOT NULL AND (DayOfMonth < 1 OR DayOfMonth > 31))
   OR MaxBackupsToKeep < 1 OR MaxBackupsToKeep > 30
   OR Frequency NOT IN (N'daily', N'weekly', N'monthly')
   OR (Frequency = N'weekly' AND DayOfWeek IS NULL)
   OR (Frequency = N'monthly' AND DayOfMonth IS NULL)
   OR CollectionsJson IS NULL OR ISJSON(CollectionsJson) <> 1
   OR LEN(Id) > 20 OR LEN(Frequency) > 20
   OR LEN(LastRunStatus) > 30 OR LEN(UpdatedBy) > 128;
"@).Rows[0].N
    if ($invalid -gt 0) { throw "Preflight blocked: $invalid ScheduledBackupSettings row(s) contain invalid or unsupported values. No normalization or truncation is performed." }

    $sourceCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.ScheduledBackupSettings").Rows[0].N
    $mappedCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'ScheduledBackupSettings'").Rows[0].N
    $pendingCount = [int](Query $connection @"
SELECT COUNT(*) AS N
FROM dbo.ScheduledBackupSettings s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.LegacyDataMigrationMap m
    WHERE m.SourceSystem=N'BuildingManagement.Legacy'
      AND m.SourceTable=N'ScheduledBackupSettings'
      AND m.SourceKey=s.Id
);
"@).Rows[0].N

    Log "Database: $database"
    Log "Source rows: $sourceCount"
    Log "Already mapped: $mappedCount"
    Log "Pending: $pendingCount"

    if (-not $Apply) {
        Log 'DRY RUN - no data changed'
    }
    else {
        $transaction = $connection.BeginTransaction()
        try {
            $sql = @"
SET NOCOUNT ON;

;WITH SourceRows AS (
    SELECT s.*,
        CONVERT(uniqueidentifier, SUBSTRING(HASHBYTES('SHA2_256', CONVERT(varbinary(max), CONCAT(N'legacy.backup.schedule.v1|', s.Id))), 1, 16)) AS NewConfigurationId,
        HASHBYTES('SHA2_256', CONVERT(varbinary(max), (
            SELECT s.Id, s.Enabled, s.Frequency, s.Hour, s.DayOfWeek, s.DayOfMonth,
                   s.CollectionsJson, s.MaxBackupsToKeep, s.LastRunAt, s.LastRunStatus,
                   s.UpdatedAt, s.UpdatedBy
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
        ))) AS SourceFingerprint
    FROM dbo.ScheduledBackupSettings s
)
INSERT dbo.BackupScheduleConfigurations
    (ConfigurationId, LegacySettingId, IsEnabled, Frequency, ScheduledHour, DayOfWeek,
     DayOfMonth, CollectionsJson, MaxBackupsToKeep, LastRunAtUtc, LastRunStatus,
     UpdatedAtUtc, UpdatedByReference)
SELECT s.NewConfigurationId, s.Id, s.Enabled, s.Frequency, s.Hour, s.DayOfWeek,
       s.DayOfMonth, s.CollectionsJson, s.MaxBackupsToKeep, s.LastRunAt,
       s.LastRunStatus, s.UpdatedAt, s.UpdatedBy
FROM SourceRows s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.LegacyDataMigrationMap m
    WHERE m.SourceSystem=N'BuildingManagement.Legacy'
      AND m.SourceTable=N'ScheduledBackupSettings'
      AND m.SourceKey=s.Id
);

;WITH SourceRows AS (
    SELECT s.*,
        CONVERT(uniqueidentifier, SUBSTRING(HASHBYTES('SHA2_256', CONVERT(varbinary(max), CONCAT(N'legacy.backup.schedule.v1|', s.Id))), 1, 16)) AS NewConfigurationId,
        HASHBYTES('SHA2_256', CONVERT(varbinary(max), (
            SELECT s.Id, s.Enabled, s.Frequency, s.Hour, s.DayOfWeek, s.DayOfMonth,
                   s.CollectionsJson, s.MaxBackupsToKeep, s.LastRunAt, s.LastRunStatus,
                   s.UpdatedAt, s.UpdatedBy
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
        ))) AS SourceFingerprint
    FROM dbo.ScheduledBackupSettings s
)
INSERT dbo.LegacyDataMigrationMap
    (MigrationMapId, SourceSystem, SourceTable, SourceKey, TargetTable, TargetKey,
     SourceFingerprint, Outcome, Disposition, SafeNote)
SELECT
    CONVERT(uniqueidentifier, SUBSTRING(HASHBYTES('SHA2_256', CONVERT(varbinary(max), CONCAT(N'legacy.backup.schedule.map.v1|', s.Id))), 1, 16)),
    N'BuildingManagement.Legacy', N'ScheduledBackupSettings', s.Id,
    N'BackupScheduleConfigurations', CONVERT(nvarchar(36), s.NewConfigurationId),
    s.SourceFingerprint, 1, N'MigratedWithLegacyMetadata',
    LEFT(CONCAT(N'Legacy setting ID=', s.Id, N'; scheduling fields copied without normalization; source JSON preserved exactly.'), 500)
FROM SourceRows s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.LegacyDataMigrationMap m
    WHERE m.SourceSystem=N'BuildingManagement.Legacy'
      AND m.SourceTable=N'ScheduledBackupSettings'
      AND m.SourceKey=s.Id
);
"@
            $command = $connection.CreateCommand()
            $command.Transaction = $transaction
            $command.CommandText = $sql
            $command.CommandTimeout = 300
            [void]$command.ExecuteNonQuery()
            $transaction.Commit()
        }
        catch {
            $transaction.Rollback()
            throw
        }

        $targetCount = [int](Query $connection @"
SELECT COUNT(*) AS N
FROM dbo.BackupScheduleConfigurations t
INNER JOIN dbo.LegacyDataMigrationMap m
    ON m.TargetKey=CONVERT(nvarchar(36), t.ConfigurationId)
WHERE m.SourceSystem=N'BuildingManagement.Legacy'
  AND m.SourceTable=N'ScheduledBackupSettings';
"@).Rows[0].N
        $provenanceCount = [int](Query $connection "SELECT COUNT(*) AS N FROM dbo.LegacyDataMigrationMap WHERE SourceSystem=N'BuildingManagement.Legacy' AND SourceTable=N'ScheduledBackupSettings'").Rows[0].N
        Log "Target configurations: $targetCount"
        Log "Provenance rows: $provenanceCount"
        if ($targetCount -ne $sourceCount -or $provenanceCount -ne $sourceCount) {
            throw "Reconciliation failed: source=$sourceCount target=$targetCount provenance=$provenanceCount"
        }
        Log 'RECONCILIATION PASS'
    }
}
catch {
    Log "ERROR: $($_.Exception.Message)"
    throw
}
finally {
    if ($connection.State -ne 'Closed') { $connection.Close() }
    $mode = if ($Apply) { 'APPLY' } else { 'DRY RUN' }
    $lines = @(
        '# Phase 14 - ScheduledBackupSettings Backfill',
        '',
        "- Run: $(Get-Date -Format o)",
        "- Mode: $mode",
        '- Source rows retained: dbo.ScheduledBackupSettings',
        '',
        '## Results'
    )
    $lines += $log | ForEach-Object { "- $_" }
    $lines += @(
        '',
        '## Preservation rules',
        '- The legacy schedule configuration is migrated to its own BackupScheduleConfigurations table; it is not represented as a BackupJobs execution.',
        '- Frequency, hour, day selectors, retention count, timestamps, updater reference, and CollectionsJson are copied without normalization.',
        '- Unsupported frequency values, invalid schedule ranges, invalid JSON, and target-length violations block the run; no truncation or silent correction occurs.',
        '- Deterministic configuration IDs and LegacyDataMigrationMap provenance make the operation idempotent.',
        '- The source ScheduledBackupSettings row is never updated or deleted.'
    )
    Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
    Write-Host "Report: $report"
}

Push-Location $root
try {
    git add -- $PSScriptRoot\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 $report
    if ((git status --short -- $PSScriptRoot\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 $report).Count -gt 0) {
        git commit --only -m "feat: backfill legacy backup schedule settings" -- $PSScriptRoot\Invoke-Phase14-ScheduledBackupSettingsBackfill.ps1 $report
        git push origin development/database-redesign-enterprise
    }
}
finally { Pop-Location }
