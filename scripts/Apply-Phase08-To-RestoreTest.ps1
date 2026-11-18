# Apply the reviewed Phase08 SQL only to the disposable RestoreTest database.
# This script does not use Database.Migrate() and never targets another database.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$Repo = 'D:\Projects\BuildingManagement-MVC'
$Branch = 'development/database-redesign-enterprise'
$Server = '(localdb)\MSSQLLocalDB'
$Database = 'BuildingManagement_RestoreTest'
$SqlRelative = 'Database\Redesign\Generated-Sql\Phase08-20261119-012912.sql'
$SqlPath = Join-Path $Repo $SqlRelative
$ExpectedSqlHash = 'CFA0D13D61AE0389660C56462987A3B94B723AAE50207CC6A89BD1D6A8D5F616'
$BackupPath = Join-Path $env:LOCALAPPDATA 'BuildingManagement\Backups\BuildingManagement_RestoreTest-Phase08-20261119-011703.bak'
$FromMigration = '20261118184850_Phase07_OwnershipRecordProvenance'
$Migration1 = '20261118213732_Phase08_FinancialLedgerFoundation'
$Migration2 = '20261118224524_Phase08FinanceReversalScopeIntegrity'
$FinanceTables = @(
    'FinancialPeriods','FinancialReconciliationRuns','FinancialReconciliationDifferences',
    'LedgerTransactions','FinancialAllocationBatches','FinancialSourceLinks',
    'LedgerEntries','PaymentDeposits','FinancialAllocationLines'
)
$Stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$ReportDir = Join-Path $Repo 'Database\Redesign\Review-Reports'
$ReportPath = Join-Path $ReportDir "Phase08-RestoreTest-Apply-$Stamp.md"
$Applied = $false
$ExecutionError = $null
$BatchCount = 0

function Invoke-Git {
    param([Parameter(Mandatory=$true)][string[]]$GitArgs)
    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & git @GitArgs 2>&1
        $code = $LASTEXITCODE
    } finally { $ErrorActionPreference = $previous }
    if ($code -ne 0) { throw "git $($GitArgs -join ' ') failed ($code): $($output -join [Environment]::NewLine)" }
    return (($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine).Trim()
}
function New-Connection([string]$DatabaseName) {
    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()
    $builder.DataSource = $Server
    $builder.InitialCatalog = $DatabaseName
    $builder.IntegratedSecurity = $true
    $builder.TrustServerCertificate = $true
    $builder.ConnectTimeout = 15
    return [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
}
function Invoke-Table([string]$DatabaseName,[string]$Sql) {
    $connection = New-Connection $DatabaseName
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 120
        $command.CommandText = $Sql
        $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
        $table = [System.Data.DataTable]::new()
        [void]$adapter.Fill($table)
        return ,$table
    } finally { $connection.Dispose() }
}
function Invoke-NonQuery([System.Data.SqlClient.SqlConnection]$Connection,[string]$Sql) {
    $command = $Connection.CreateCommand()
    $command.CommandTimeout = 0
    $command.CommandText = $Sql
    [void]$command.ExecuteNonQuery()
}
function Get-PostState {
    $history = Invoke-Table $Database "SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;"
    $tables = Invoke-Table $Database ("SELECT name FROM sys.tables WHERE name IN (" + (($FinanceTables | ForEach-Object { "N'$_'" }) -join ',') + ") ORDER BY name;")
    $fks = Invoke-Table $Database @"
SELECT name, is_disabled, is_not_trusted
FROM sys.foreign_keys
WHERE name IN
(N'FK_LedgerTransactions_LedgerTransactions_ReversesTransactionId_PeriodId_BuildingId',
 N'FK_PaymentDeposits_LedgerTransactions_ReversalTransactionId_PeriodId_BuildingId');
"@
    $checks = Invoke-Table $Database @"
SELECT name, is_disabled, is_not_trusted
FROM sys.check_constraints
WHERE name = N'CK_PaymentDeposits_Resolution';
"@
    return [pscustomobject]@{ History=$history; Tables=$tables; ForeignKeys=$fks; Checks=$checks }
}

Set-Location $Repo
if (-not (Test-Path -LiteralPath '.\BuildingManagementMVC.csproj')) { throw 'Expected project file was not found.' }
if ((Invoke-Git -GitArgs @('branch','--show-current')) -ne $Branch) { throw "Switch to $Branch. No database changes were made." }
if (-not (Test-Path -LiteralPath $SqlPath -PathType Leaf)) { throw "Reviewed SQL artifact is missing: $SqlPath" }
if (-not (Test-Path -LiteralPath $BackupPath -PathType Leaf)) { throw "Required verified backup is missing: $BackupPath" }

$sqlHash = (Get-FileHash -LiteralPath $SqlPath -Algorithm SHA256).Hash
if ($sqlHash -ne $ExpectedSqlHash) { throw "SQL SHA256 mismatch. Expected $ExpectedSqlHash; found $sqlHash. Nothing executed." }

# Confirm the exact server/database identity and state from master.
$master = New-Connection 'master'
try {
    $master.Open()
    $cmd = $master.CreateCommand()
    $cmd.CommandText = "SELECT name,state_desc,user_access_desc,is_read_only FROM sys.databases WHERE name=N'$Database';"
    $reader = $cmd.ExecuteReader()
    $dbRows = @()
    while ($reader.Read()) {
        $dbRows += [pscustomobject]@{
            Name=[string]$reader['name']; State=[string]$reader['state_desc']
            Access=[string]$reader['user_access_desc']; ReadOnly=[bool]$reader['is_read_only']
        }
    }
    $reader.Close()
} finally { $master.Dispose() }
if ($dbRows.Count -ne 1 -or $dbRows[0].State -ne 'ONLINE' -or $dbRows[0].Access -ne 'MULTI_USER' -or $dbRows[0].ReadOnly) {
    throw "Exact target gate failed for $Server / $Database. Nothing executed."
}

$historyBefore = Invoke-Table $Database "SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;"
$ids = @($historyBefore.Rows | ForEach-Object { [string]$_.MigrationId })
if ($ids.Count -ne 6 -or $ids[-1] -ne $FromMigration -or ($ids -contains $Migration1) -or ($ids -contains $Migration2)) {
    throw "Migration history gate failed. Expected exactly six migrations ending at Phase07; nothing executed."
}
$existingTables = Invoke-Table $Database ("SELECT name FROM sys.tables WHERE name IN (" + (($FinanceTables | ForEach-Object { "N'$_'" }) -join ',') + ");")
if ($existingTables.Rows.Count -ne 0) { throw "One or more Phase08 finance tables already exist. Partial-state recovery is required; nothing executed." }

# Re-verify the exact backup immediately before applying the reviewed SQL.
$master = New-Connection 'master'
try {
    $master.Open()
    $backupEscaped = $BackupPath.Replace("'","''")
    Invoke-NonQuery $master "RESTORE VERIFYONLY FROM DISK=N'$backupEscaped' WITH CHECKSUM;"
} finally { $master.Dispose() }

$sql = [System.IO.File]::ReadAllText($SqlPath)
$batches = [regex]::Split($sql,'(?im)^\s*GO\s*(?:--.*)?\s*$')
$batches = @($batches | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($batches.Count -lt 20) { throw "Unexpected SQL batch count ($($batches.Count)); nothing executed." }

Write-Host "Target verified: $Server / $Database" -ForegroundColor Cyan
Write-Host "Backup checksum verified: $BackupPath" -ForegroundColor Cyan
Write-Host "SQL SHA256 verified: $sqlHash" -ForegroundColor Cyan
Write-Host "Executing $($batches.Count) reviewed GO-separated batches..." -ForegroundColor Yellow

$connection = New-Connection $Database
try {
    $connection.Open()
    for ($i=0; $i -lt $batches.Count; $i++) {
        Invoke-NonQuery $connection $batches[$i]
        $BatchCount++
    }
    $Applied = $true
} catch {
    $ExecutionError = $_.Exception.Message
    # Roll back only a transaction that is still active on this connection.
    # A previously committed EF migration transaction is intentionally not undone.
    try { Invoke-NonQuery $connection "IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;" } catch {}
} finally { $connection.Dispose() }

$post = Get-PostState
$postIds = @($post.History.Rows | ForEach-Object { [string]$_.MigrationId })
$postTableNames = @($post.Tables.Rows | ForEach-Object { [string]$_.name })
$bothMigrationsPresent = ($postIds -contains $Migration1) -and ($postIds -contains $Migration2)
$allTablesPresent = @($FinanceTables | Where-Object { $_ -notin $postTableNames }).Count -eq 0
$bothFksTrusted = $post.ForeignKeys.Rows.Count -eq 2 -and @($post.ForeignKeys.Rows | Where-Object { [bool]$_.is_disabled -or [bool]$_.is_not_trusted }).Count -eq 0
$checkTrusted = $post.Checks.Rows.Count -eq 1 -and -not [bool]$post.Checks.Rows[0].is_disabled -and -not [bool]$post.Checks.Rows[0].is_not_trusted
$Verified = $Applied -and $bothMigrationsPresent -and $allTablesPresent -and $bothFksTrusted -and $checkTrusted

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# Phase08 RestoreTest Migration Apply Report')
$lines.Add('')
$lines.Add("- Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
$lines.Add("- Server: $Server")
$lines.Add("- Database: $Database")
$lines.Add("- SQL artifact: $($SqlRelative.Replace('\','/'))")
$lines.Add("- SQL SHA256: $sqlHash")
$lines.Add("- Backup: $BackupPath")
$lines.Add('- Backup RESTORE VERIFYONLY WITH CHECKSUM: PASSED immediately before execution')
$lines.Add("- Executed GO-separated batches: $BatchCount / $($batches.Count)")
$lines.Add("- SQL batch execution completed: $Applied")
$lines.Add("- Post-apply verification: $Verified")
$lines.Add("- Migration 1 history row: $($postIds -contains $Migration1)")
$lines.Add("- Migration 2 history row: $($postIds -contains $Migration2)")
$lines.Add("- Finance tables present: $($postTableNames.Count) / $($FinanceTables.Count)")
$lines.Add("- Both composite reversal FKs enabled and trusted: $bothFksTrusted")
$lines.Add("- Payment-deposit resolution check enabled and trusted: $checkTrusted")
$lines.Add('- Business data rows copied/updated/deleted: NONE')
$lines.Add('- Production database: NOT TARGETED')
if ($ExecutionError) { $lines.Add("- Execution error: $ExecutionError") }
$lines.Add('')
$lines.Add('## Migration history after execution')
foreach ($id in $postIds) { $lines.Add("- $id") }
$lines.Add('')
$lines.Add('## Finance tables after execution')
foreach ($name in $postTableNames) { $lines.Add("- $name") }
$lines.Add('')
$lines.Add('## Disposition')
if ($Verified) {
    $lines.Add('**PASS** — both Phase08 migrations are present and the required schema objects are enabled/trusted on the disposable RestoreTest database.')
} else {
    $lines.Add('**INCOMPLETE / FAILED** — do not rerun this script. Preserve the verified backup and inspect the recorded migration history/object inventory. The first EF migration transaction may have committed even if the second failed.')
    $lines.Add('Restore from the verified backup to a separate disposable database for recovery/rehearsal; do not run Down against this retained RestoreTest database.')
}
New-Item -ItemType Directory -Path $ReportDir -Force | Out-Null
[System.IO.File]::WriteAllLines($ReportPath,$lines,[System.Text.UTF8Encoding]::new($false))
$relative = $ReportPath.Substring($Repo.Length+1).Replace('\','/')
Invoke-Git -GitArgs @('add','--',$relative) | Out-Null
Invoke-Git -GitArgs @('commit','--only','-m',"docs: record Phase08 RestoreTest apply result $Stamp",'--',$relative) | Out-Null
Invoke-Git -GitArgs @('push','origin',$Branch) | Out-Null

Write-Host "Apply report pushed: $relative" -ForegroundColor Green
if (-not $Verified) { throw 'Phase08 apply/post-verification did not fully pass. Do not rerun; inspect the pushed report and restore backup in a separate disposable target.' }
Write-Host 'Phase08 migrations applied and post-verified on RestoreTest only.' -ForegroundColor Green
Write-Host 'No production database was targeted and no legacy business rows were changed.'
