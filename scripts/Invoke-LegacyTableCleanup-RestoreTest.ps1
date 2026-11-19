[CmdletBinding()]
param(
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30",
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
$expectedDatabase = 'BuildingManagement_RestoreTest'
$protected = @('Buildings','Floors','Apartments','FinancialCategories','BuildingSettings','__EFMigrationsHistory')
$legacyAllowList = @(
    'Users','Residents','BuildingAdmins','UserPermissions',
    'Deposits','DepositEditHistory','WalletAdjustments','Expenses','Revenues','MonthlyApartmentShares',
    'Polls','PollOptions','PollVotes','Notifications','Presence','MaintenanceRecords',
    'QrTokens','QrUsages','Dvrs','Cameras',
    'BackupHistory','ScheduledBackupSettings','GlobalSettings','AuditLog',
    'LoginThrottle','SyncPendingChanges'
)
$conn = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try {
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = 'SELECT DB_NAME();'
    $actual = [string]$cmd.ExecuteScalar()
    if ($actual -cne $expectedDatabase) { throw "SAFETY STOP: expected '$expectedDatabase' but connected to '$actual'. No changes made." }

    $cmd.CommandText = "SELECT t.name FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name=N'dbo' ORDER BY t.name;"
    $reader = $cmd.ExecuteReader()
    $existing = @()
    while ($reader.Read()) { $existing += [string]$reader['name'] }
    $reader.Close()

    $protectedPresent = @($existing | Where-Object { $_ -in $protected })
    $protectedMissing = @($protected | Where-Object { $_ -notin $existing })
    $toDrop = @($legacyAllowList | Where-Object { $_ -in $existing })
    $notFound = @($legacyAllowList | Where-Object { $_ -notin $existing })

    Write-Host "Database: $actual"
    Write-Host "Protected tables present: $($protectedPresent -join ', ')"
    if ($protectedMissing.Count) { Write-Warning "Protected tables not found: $($protectedMissing -join ', ')" }
    Write-Host "Legacy tables found for deletion ($($toDrop.Count)):"
    $toDrop | ForEach-Object { Write-Host "  dbo.$_" }
    if ($notFound.Count) { Write-Host "Legacy tables already absent: $($notFound -join ', ')" }

    if (-not $Apply) {
        Write-Host 'DRY RUN ONLY. Re-run with -Apply to perform the one-batch cleanup.'
        return
    }
    if ($protectedPresent.Count -ne $protected.Count) { throw 'SAFETY STOP: one or more protected tables are missing. Review target schema before applying.' }
    if ($toDrop.Count -eq 0) { Write-Host 'Nothing to delete.'; return }

    $values = ($toDrop | ForEach-Object { "(N'" + $_.Replace("'","''") + "')" }) -join ("," + [Environment]::NewLine)
    $sql = @"
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Legacy TABLE (TableName sysname PRIMARY KEY);
    INSERT INTO @Legacy(TableName) VALUES
$values;

    DECLARE @DropFK nvarchar(max) = N'';
    SELECT @DropFK = @DropFK +
        N'ALTER TABLE ' + QUOTENAME(ps.name) + N'.' + QUOTENAME(pt.name) +
        N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
    FROM sys.foreign_keys fk
    JOIN sys.tables pt ON pt.object_id=fk.parent_object_id
    JOIN sys.schemas ps ON ps.schema_id=pt.schema_id
    JOIN sys.tables rt ON rt.object_id=fk.referenced_object_id
    JOIN sys.schemas rs ON rs.schema_id=rt.schema_id
    WHERE (ps.name=N'dbo' AND pt.name IN (SELECT TableName FROM @Legacy))
       OR (rs.name=N'dbo' AND rt.name IN (SELECT TableName FROM @Legacy));
    IF LEN(@DropFK)>0 EXEC sys.sp_executesql @DropFK;

    DECLARE @DropTables nvarchar(max) = N'';
    SELECT @DropTables = @DropTables +
        N'DROP TABLE dbo.' + QUOTENAME(t.name) + N';' + CHAR(10)
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id=t.schema_id
    JOIN @Legacy l ON l.TableName=t.name
    WHERE s.name=N'dbo'
      AND t.name NOT IN (N'Buildings',N'Floors',N'Apartments',
                         N'FinancialCategories',N'BuildingSettings',N'__EFMigrationsHistory');
    IF LEN(@DropTables)>0 EXEC sys.sp_executesql @DropTables;

    IF EXISTS (
        SELECT 1 FROM (VALUES
            (N'Buildings'),(N'Floors'),(N'Apartments'),
            (N'FinancialCategories'),(N'BuildingSettings'),(N'__EFMigrationsHistory')
        ) p(TableName)
        WHERE OBJECT_ID(N'dbo.' + p.TableName,N'U') IS NULL
    ) THROW 51001, 'Protected table missing after cleanup; transaction will roll back.', 1;

    IF EXISTS (SELECT 1 FROM @Legacy l WHERE OBJECT_ID(N'dbo.' + l.TableName,N'U') IS NOT NULL)
        THROW 51002, 'One or more approved legacy tables remain; transaction will roll back.', 1;

    COMMIT TRANSACTION;
    SELECT N'SUCCESS' AS Result, COUNT(*) AS DroppedLegacyTables FROM @Legacy;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
"@
    $applyCmd = $conn.CreateCommand()
    $applyCmd.CommandTimeout = 900
    $applyCmd.CommandText = $sql
    $result = $applyCmd.ExecuteReader()
    while ($result.Read()) { Write-Host "$($result['Result']): $($result['DroppedLegacyTables']) legacy tables dropped." }
    $result.Close()
}
finally {
    if ($conn.State -ne 'Closed') { $conn.Close() }
}
