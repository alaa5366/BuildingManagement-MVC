# Phase08 SQL generation and safety review only.
# This script NEVER applies EF migrations and NEVER changes business rows.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$Repo = 'D:\Projects\BuildingManagement-MVC'
$Branch = 'development/database-redesign-enterprise'
$Server = '(localdb)\MSSQLLocalDB'
$Database = 'BuildingManagement_RestoreTest'
$FromMigration = '20261118184850_Phase07_OwnershipRecordProvenance'
$ToMigration = '20261118224524_Phase08FinanceReversalScopeIntegrity'
$BackupPath = Join-Path $env:LOCALAPPDATA 'BuildingManagement\Backups\BuildingManagement_RestoreTest-Phase08-20261119-011703.bak'
$Stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$ReportDir = Join-Path $Repo 'Database\Redesign\Review-Reports'
$ReportPath = Join-Path $ReportDir "Phase08-SQL-Generation-Review-$Stamp.md"
$SqlPath = Join-Path $env:TEMP "Phase08-$Stamp.sql"

function Invoke-Git {
    param([Parameter(Mandatory = $true)][string[]]$GitArgs)
    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & git @GitArgs 2>&1
        $code = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previous
    }
    if ($code -ne 0) {
        throw "git $($GitArgs -join ' ') failed ($code): $($output -join [Environment]::NewLine)"
    }
    return (($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine).Trim()
}

function Invoke-Sql {
    param([string]$ConnectionString, [string]$Sql)
    $connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 120
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
if (-not (Test-Path '.\BuildingManagementMVC.csproj')) { throw 'Project file not found at the expected repository path.' }
if ((Invoke-Git -GitArgs @('branch','--show-current')) -ne $Branch) { throw "Expected branch $Branch. No files or database objects were changed." }

# Preserve all existing staged, unstaged, and untracked changes.
$preExistingChanges = & git status --short
if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect Git working tree.' }

if (-not (Test-Path -LiteralPath $BackupPath -PathType Leaf)) {
    throw "Verified preflight backup is missing: $BackupPath. No migration SQL was generated."
}

$masterCs = "Server=$Server;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15;"
$dbCheck = Invoke-Sql $masterCs @"
SELECT name, state_desc
FROM sys.databases
WHERE name = N'$Database';
"@
if ($dbCheck.Rows.Count -ne 1 -or $dbCheck.Rows[0].state_desc -ne 'ONLINE') {
    throw "Exact RestoreTest database is not present and ONLINE on $Server."
}

$dbCs = "Server=$Server;Database=$Database;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15;"
$history = Invoke-Sql $dbCs @"
SELECT MigrationId
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;
"@
$ids = @($history.Rows | ForEach-Object { [string]$_.MigrationId })
if ($ids.Count -ne 6 -or $ids[-1] -ne $FromMigration -or ($ids -contains $ToMigration)) {
    throw "Migration history gate failed. Expected exactly six migrations ending in $FromMigration and no Phase08 target."
}

# Re-verify the existing backup immediately before SQL generation.
$escapedBackup = $BackupPath.Replace("'","''")
Invoke-Sql $masterCs "RESTORE VERIFYONLY FROM DISK = N'$escapedBackup' WITH CHECKSUM;" | Out-Null

# Generate SQL only. No Database.Migrate(), Update-Database, or SQL execution is used.
$previous = $ErrorActionPreference
try {
    $ErrorActionPreference = 'Continue'
    $efOutput = & dotnet ef migrations script $FromMigration $ToMigration --project '.\BuildingManagementMVC.csproj' --startup-project '.\BuildingManagementMVC.csproj' --output $SqlPath 2>&1
    $efExit = $LASTEXITCODE
} finally {
    $ErrorActionPreference = $previous
}
if ($efExit -ne 0) {
    throw "dotnet ef migrations script failed ($efExit): $($efOutput -join [Environment]::NewLine)"
}
if (-not (Test-Path -LiteralPath $SqlPath -PathType Leaf)) { throw 'EF did not create the SQL output file.' }

$sql = [System.IO.File]::ReadAllText($SqlPath)
if ([string]::IsNullOrWhiteSpace($sql)) { throw 'Generated SQL file is empty.' }

# Required Phase08 objects and scope constraints must be present.
$required = @(
    'FinancialPeriods','FinancialReconciliationRuns','FinancialReconciliationDifferences',
    'LedgerTransactions','LedgerEntries','FinancialAllocationBatches',
    'FinancialAllocationLines','FinancialSourceLinks','PaymentDeposits',
    'FK_LedgerTransactions_LedgerTransactions_ReversesTransactionId_PeriodId_BuildingId',
    'FK_PaymentDeposits_LedgerTransactions_ReversalTransactionId_PeriodId_BuildingId'
)
$missing = @($required | Where-Object { $sql.IndexOf($_,[StringComparison]::OrdinalIgnoreCase) -lt 0 })
if ($missing.Count -gt 0) { throw "Generated SQL is missing required migration objects: $($missing -join ', ')" }

# This Phase08 range is additive. The only expected drops are the two old
# reversal FKs, their two old indexes, and the replaced resolution check.
$forbidden = @(
    '(?im)^\s*DROP\s+TABLE\b',
    '(?im)^\s*ALTER\s+TABLE\b.*\bDROP\s+COLUMN\b',
    '(?im)^\s*TRUNCATE\s+TABLE\b',
    '(?im)^\s*DELETE\s+FROM\b',
    '(?im)^\s*UPDATE\s+\['
)
foreach ($pattern in $forbidden) {
    if ([regex]::IsMatch($sql,$pattern)) { throw "Unexpected destructive/data-changing SQL detected: $pattern" }
}

$dropConstraints = @([regex]::Matches($sql,'(?im)^\s*ALTER\s+TABLE\b[^\r\n]*\bDROP\s+CONSTRAINT\s+\[([^\]]+)\]') | ForEach-Object { $_.Groups[1].Value })
$allowedConstraints = @(
    'FK_LedgerTransactions_LedgerTransactions_ReversesTransactionId',
    'FK_PaymentDeposits_LedgerTransactions_ReversalTransactionId',
    'CK_PaymentDeposits_Resolution'
)
$unexpectedConstraints = @($dropConstraints | Where-Object { $_ -notin $allowedConstraints })
if ($unexpectedConstraints.Count -gt 0) { throw "Unexpected DROP CONSTRAINT operation(s): $($unexpectedConstraints -join ', ')" }
if (@($dropConstraints | Select-Object -Unique).Count -ne 3) { throw 'Expected exactly the three reviewed constraint replacements were not found.' }

$dropIndexes = @([regex]::Matches($sql,'(?im)^\s*DROP\s+INDEX\s+\[([^\]]+)\]') | ForEach-Object { $_.Groups[1].Value })
$allowedIndexes = @('IX_PaymentDeposits_ReversalTransactionId','IX_LedgerTransactions_ReversesTransactionId')
$unexpectedIndexes = @($dropIndexes | Where-Object { $_ -notin $allowedIndexes })
if ($unexpectedIndexes.Count -gt 0) { throw "Unexpected DROP INDEX operation(s): $($unexpectedIndexes -join ', ')" }
if (@($dropIndexes | Select-Object -Unique).Count -ne 2) { throw 'Expected exactly the two reviewed reversal indexes to be replaced.' }

$sqlHash = (Get-FileHash -LiteralPath $SqlPath -Algorithm SHA256).Hash
$sqlLines = (Get-Content -LiteralPath $SqlPath).Count
New-Item -ItemType Directory -Path $ReportDir -Force | Out-Null
$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# Phase08 EF SQL Generation and Safety Review')
$lines.Add('')
$lines.Add("- Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
$lines.Add("- Branch: $Branch")
$lines.Add("- Server: $Server")
$lines.Add("- Database: $Database (ONLINE)")
$lines.Add("- Migration range: $FromMigration -> $ToMigration")
$lines.Add("- Migration history gate: PASS (six rows; latest is Phase07; no Phase08 target)")
$lines.Add("- Backup: $BackupPath")
$lines.Add('- RESTORE VERIFYONLY WITH CHECKSUM: PASSED immediately before generation')
$lines.Add("- Generated SQL: $SqlPath")
$lines.Add("- SQL SHA256: $sqlHash")
$lines.Add("- SQL line count: $sqlLines")
$lines.Add('')
$lines.Add('## Static validation')
$lines.Add('- Required nine Phase08 tables and both composite reversal foreign keys: present.')
$lines.Add('- DROP TABLE / DROP COLUMN / TRUNCATE / DELETE / UPDATE statements: none.')
$lines.Add("- DROP CONSTRAINT: $($dropConstraints -join ', ') (reviewed replacement set only).")
$lines.Add("- DROP INDEX: $($dropIndexes -join ', ') (reviewed reversal indexes only).")
$lines.Add('- EF migration execution: NOT RUN.')
$lines.Add('- Business data changes: NONE.')
$lines.Add('')
$lines.Add('## Existing local changes preserved')
if (-not $preExistingChanges) { $lines.Add('- None.') }
foreach ($change in $preExistingChanges) { $lines.Add("- $change") }
$lines.Add('')
$lines.Add('## Gate')
$lines.Add('SQL generation and static checks passed. This report is not authorization to apply the SQL.')
$lines.Add('Next: inspect the generated SQL file locally, then use a disposable restore for apply/rollback testing.')
[System.IO.File]::WriteAllLines($ReportPath,$lines,[System.Text.UTF8Encoding]::new($false))

$relative = $ReportPath.Substring($Repo.Length + 1).Replace('\','/')
Invoke-Git -GitArgs @('add','--',$relative) | Out-Null
Invoke-Git -GitArgs @('commit','--only','-m',"docs: record Phase08 SQL generation review $Stamp",'--',$relative) | Out-Null
Invoke-Git -GitArgs @('push','origin',$Branch) | Out-Null

Write-Host "SQL generation review pushed: $relative" -ForegroundColor Green
Write-Host "Generated SQL (NOT APPLIED): $SqlPath" -ForegroundColor Green
Write-Host "SQL SHA256: $sqlHash"
Write-Host 'No EF migration was applied and no business data was changed.'
