# Read-only Phase08 RestoreTest preflight plus verified COPY_ONLY backup.
# This script NEVER applies EF migrations or changes business rows.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$Repo = 'D:\Projects\BuildingManagement-MVC'
$Branch = 'development/database-redesign-enterprise'
$Database = 'BuildingManagement_RestoreTest'
$Server = '(localdb)\MSSQLLocalDB'
$Stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$BackupDir = Join-Path $env:LOCALAPPDATA 'BuildingManagement\Backups'
$BackupPath = Join-Path $BackupDir "BuildingManagement_RestoreTest-Phase08-$Stamp.bak"
$ReportDir = Join-Path $Repo 'Database\Redesign\Review-Reports'
$ReportPath = Join-Path $ReportDir "Phase08-RestoreTest-Preflight-$Stamp.md"

function Invoke-Git {
    param([Parameter(Mandatory = $true)][string[]]$GitArgs)
    $output = & git @GitArgs 2>&1
    $code = $LASTEXITCODE
    if ($code -ne 0) { throw "git $($GitArgs -join ' ') failed ($code): $($output -join [Environment]::NewLine)" }
    return (($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine).Trim()
}
function Invoke-Sql([string]$ConnectionString, [string]$Sql) {
    $connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 0
        $command.CommandText = $Sql
        $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
        $table = [System.Data.DataTable]::new()
        [void]$adapter.Fill($table)
        return ,$table
    } finally {
        $connection.Dispose()
    }
}

Set-Location $Repo
if (-not (Test-Path '.\BuildingManagementMVC.csproj')) { throw 'Repository path is not the BuildingManagement-MVC project.' }
if ((Invoke-Git -GitArgs @('branch','--show-current')) -ne $Branch) { throw "Switch to $Branch before running." }
# Preserve all pre-existing unstaged/untracked work. Refuse only staged changes,
# because the report-only commit must never include somebody else's staged files.
$staged = & git diff --cached --name-only
if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect Git index; nothing was changed.' }
if ($staged) { throw "Git index contains staged changes. Unstage or commit them separately before running; staged paths: $($staged -join ', ')" }
$preExistingChanges = & git status --short
if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect working tree; nothing was changed.' }
# No fetch/merge here: pull the reviewed branch before running this preflight.

$masterCs = "Server=$Server;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15;"
$dbCheck = Invoke-Sql $masterCs @"
SELECT name, state_desc, user_access_desc, recovery_model_desc
FROM sys.databases
WHERE name = N'$Database';
"@
if ($dbCheck.Rows.Count -ne 1) { throw "Database $Database was not found on $Server. No backup or changes were made." }
if ($dbCheck.Rows[0].state_desc -ne 'ONLINE') { throw "Target database is not ONLINE. No backup or changes were made." }

$dbCs = "Server=$Server;Database=$Database;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15;"
$tableInventory = Invoke-Sql $dbCs @"
SELECT t.name AS TableName, SUM(p.rows) AS RowCount
FROM sys.tables t
JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN (0,1)
GROUP BY t.name
ORDER BY t.name;
"@
$migrationHistory = Invoke-Sql $dbCs @"
SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;
"@
$financeTables = Invoke-Sql $dbCs @"
SELECT s.name AS SchemaName, t.name AS TableName
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id=t.schema_id
WHERE t.name IN
(N'FinancialPeriods',N'PaymentDeposits',N'LedgerTransactions',N'LedgerEntries',
 N'FinancialSourceLinks',N'FinancialAllocationBatches',N'FinancialAllocationLines',
 N'FinancialReconciliationRuns',N'FinancialReconciliationDifferences')
ORDER BY t.name;
"@

New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
$escapedBackup = $BackupPath.Replace("'","''")
$backupSql = "BACKUP DATABASE [$Database] TO DISK = N'$escapedBackup' WITH COPY_ONLY, INIT, CHECKSUM;"
Invoke-Sql $masterCs $backupSql | Out-Null
$verifySql = "RESTORE VERIFYONLY FROM DISK = N'$escapedBackup' WITH CHECKSUM;"
Invoke-Sql $masterCs $verifySql | Out-Null

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# Phase08 RestoreTest Preflight and Backup')
$lines.Add('')
$lines.Add("- Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
$lines.Add("- Server: $Server")
$lines.Add("- Database: $Database")
$lines.Add("- State: $($dbCheck.Rows[0].state_desc)")
$lines.Add("- User access: $($dbCheck.Rows[0].user_access_desc)")
$lines.Add("- Recovery model: $($dbCheck.Rows[0].recovery_model_desc)")
$lines.Add('')
$lines.Add('## Pre-existing local working-tree changes (preserved; not included in this report commit)')
if (-not $preExistingChanges) { $lines.Add('- None.') }
foreach ($change in $preExistingChanges) { $lines.Add("- `$change`") }
$lines.Add("- Backup: $BackupPath")
$lines.Add('- Backup type: COPY_ONLY')
$lines.Add('- RESTORE VERIFYONLY WITH CHECKSUM: PASSED')
$lines.Add('- EF migration application: NOT RUN')
$lines.Add('- Business data changes: NONE')
$lines.Add('')
$lines.Add('## Table inventory')
foreach ($row in $tableInventory.Rows) { $lines.Add("- $($row.TableName): $($row.RowCount)") }
$lines.Add('')
$lines.Add('## EF migration history')
foreach ($row in $migrationHistory.Rows) { $lines.Add("- $($row.MigrationId) (EF $($row.ProductVersion))") }
$lines.Add('')
$lines.Add('## Phase08 finance target tables')
if ($financeTables.Rows.Count -eq 0) { $lines.Add('- None found.') }
foreach ($row in $financeTables.Rows) { $lines.Add("- $($row.SchemaName).$($row.TableName)") }
$lines.Add('')
$lines.Add('## Gate status')
$lines.Add('- Database identity and ONLINE state verified.')
$lines.Add('- COPY_ONLY backup completed and verified by SQL Server.')
$lines.Add('- This report is preflight evidence only; it does not authorize migration application.')
[System.IO.File]::WriteAllLines($ReportPath,$lines,[System.Text.UTF8Encoding]::new($false))

$relative = $ReportPath.Substring($Repo.Length + 1).Replace('\','/')
Invoke-Git @('add','--',$relative) | Out-Null
Invoke-Git @('commit','-m',"docs: record Phase08 RestoreTest preflight $Stamp") | Out-Null
Invoke-Git @('push','origin',$Branch) | Out-Null

Write-Host "Preflight report pushed: $relative" -ForegroundColor Green
Write-Host "Verified backup: $BackupPath" -ForegroundColor Green
Write-Host 'No EF migration was applied and no business data was changed.'
