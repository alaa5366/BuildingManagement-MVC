[CmdletBinding()]
param(
    [string]$ServerInstance = "(localdb)\MSSQLLocalDB",
    [string]$Database = "BuildingManagement_RestoreTest",
    [string]$OutputRoot = (Join-Path $PSScriptRoot "Financial-Reconciliation-Results")
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$expectedDatabase = "BuildingManagement_RestoreTest"
$expectedBranch = "development/database-redesign-enterprise"
if ($Database -ne $expectedDatabase) { throw "Safety stop: restricted to $expectedDatabase." }

$sqlPath = Join-Path $PSScriptRoot "Financial-Reconciliation-ReadOnly-Validation.sql"
if (-not (Test-Path -LiteralPath $sqlPath -PathType Leaf)) { throw "SQL file not found: $sqlPath" }
$sql = Get-Content -LiteralPath $sqlPath -Raw
if ($sql -notmatch '(?im)^\s*USE\s+\[BuildingManagement_RestoreTest\]\s*;') { throw "Safety stop: expected USE guard missing." }
if ($sql -notmatch "DB_NAME\(\)\s*<>\s*N'BuildingManagement_RestoreTest'") { throw "Safety stop: DB_NAME guard missing." }
if ($sql -match '(?im)^\s*(INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP|TRUNCATE)\b') {
    throw "Safety stop: DDL/DML statement detected in diagnostic SQL."
}
if ($sql -notmatch '(?im)^\s*SELECT\s+DB_NAME\(\)') { throw "Safety stop: database identity result missing." }
if ($sql -notmatch 'MonthlyApartmentShares' -or $sql -notmatch 'WalletAdjustments') { throw "Safety stop: expected financial reconciliation sources missing." }

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outputDir = Join-Path $OutputRoot $stamp
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

$builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$builder["Data Source"] = $ServerInstance
$builder["Initial Catalog"] = $Database
$builder["Integrated Security"] = $true
$builder["MultipleActiveResultSets"] = $true
$builder["TrustServerCertificate"] = $true
$builder["Connect Timeout"] = 15
$builder["Application Name"] = "BuildingManagement Financial Reconciliation ReadOnly Diagnostic"

$connection = New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
$reader = $null
$resultIndex = 0
$totalRows = 0
$actualDatabase = $null
try {
    Write-Host "Connecting to $ServerInstance / $Database ..." -ForegroundColor Cyan
    $connection.Open()
    $check = $connection.CreateCommand()
    $check.CommandText = "SELECT DB_NAME();"
    $check.CommandTimeout = 15
    $actualDatabase = [string]$check.ExecuteScalar()
    if ($actualDatabase -ne $expectedDatabase) { throw "Safety stop: connected to '$actualDatabase', expected '$expectedDatabase'." }

    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 180
    $reader = $command.ExecuteReader()
    do {
        if ($reader.FieldCount -gt 0) {
            $resultIndex++
            $rows = New-Object 'System.Collections.Generic.List[object]'
            $fieldCount = $reader.FieldCount
            $fieldNames = @(for ($i=0; $i -lt $fieldCount; $i++) { $reader.GetName($i) })
            while ($reader.Read()) {
                $values = New-Object 'object[]' $fieldCount
                [void]$reader.GetValues($values)
                $record = [ordered]@{}
                for ($i=0; $i -lt $fieldCount; $i++) {
                    $value = $values[$i]
                    if ($value -is [System.DBNull]) { $value = $null }
                    $record[$fieldNames[$i]] = $value
                }
                $rows.Add([pscustomobject]$record)
            }
            $fileName = "{0:D2}-Financial-Reconciliation-Result.csv" -f $resultIndex
            $filePath = Join-Path $outputDir $fileName
            if ($rows.Count -gt 0) { $rows | Export-Csv -LiteralPath $filePath -NoTypeInformation -Encoding UTF8 }
            else {
                $header = ($fieldNames | ForEach-Object { '"' + ($_ -replace '"','""') + '"' }) -join ','
                Set-Content -LiteralPath $filePath -Value $header -Encoding UTF8
            }
            $totalRows += $rows.Count
            Write-Host ("  {0}: {1} row(s)" -f $fileName, $rows.Count) -ForegroundColor Green
        }
    } while ($reader.NextResult())

    $readme = @"
BuildingManagement Financial and Wallet Read-only Reconciliation
Generated: $(Get-Date -Format o)
Server instance: $ServerInstance
Database: $actualDatabase
Result sets exported: $resultIndex
Total rows: $totalRows

Purpose:
- Compare expense/revenue source-ledger totals with persisted apartment-share totals by building/month.
- Export per-apartment stored shares, carry-over, confirmed deposits, adjustments, and CURRENT apartment fee/closed state.
- Calculate a deterministic candidate equal allocation ordered by ApartmentId for comparison only.
- Identify missing monthly share rows and share rows without a matching apartment.

Privacy and safety:
- Windows Integrated Security; no password is requested or saved.
- Restricted to BuildingManagement_RestoreTest and guarded in SQL.
- SELECT/metadata-only; no stored shares are recalculated or changed.
- No resident names, phone numbers, emails, PINs, hashes, or credentials are exported.
- CurrentMonthlyFee and CurrentlyClosed are current values, not historical snapshots.
- Candidate allocations use ApartmentId ordering and are NOT proof of the historical allocation order or historical eligibility.

Review:
- Prioritize building/month variances already found in the profiling assessment.
- Compare per-apartment stored shares with candidate values, missing rows, and current eligibility.
- Do not update or overwrite any stored monthly share based on this diagnostic alone.
"@
    Set-Content -LiteralPath (Join-Path $outputDir "README.txt") -Value $readme -Encoding UTF8
    Write-Host "Diagnostic completed. Result sets: $resultIndex; total rows: $totalRows" -ForegroundColor Green
    Write-Host "Output: $outputDir" -ForegroundColor Cyan
}
catch {
    Write-Error ("Diagnostic failed: " + $_.Exception.Message)
    Write-Host "No database changes were made." -ForegroundColor Yellow
    throw
}
finally {
    if ($null -ne $reader) { $reader.Dispose() }
    if ($connection.State -ne [System.Data.ConnectionState]::Closed) { $connection.Close() }
    $connection.Dispose()
}

# Commit and push ONLY this run's result folder.
try {
    $repoRootOutput = & git -C $PSScriptRoot rev-parse --show-toplevel 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Git repository not found. Results remain local. $repoRootOutput" }
    $repoRoot = ([string]$repoRootOutput).Trim()
    $repoRootFull = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $outputFull = [System.IO.Path]::GetFullPath($outputDir)
    if (-not $outputFull.StartsWith($repoRootFull,[System.StringComparison]::OrdinalIgnoreCase)) { throw "Output folder is outside repository. Results remain local." }

    $branchOutput = & git -C $repoRoot branch --show-current 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Cannot determine Git branch. $branchOutput" }
    if (([string]$branchOutput).Trim() -ne $expectedBranch) { throw "Switch to '$expectedBranch'. Results remain local." }

    & git -C $repoRoot diff --cached --quiet
    if ($LASTEXITCODE -eq 1) { throw "Git index already has staged changes. Results remain local." }
    elseif ($LASTEXITCODE -ne 0) { throw "Cannot inspect Git index. Results remain local." }

    $relativeOutputPath = $outputFull.Substring($repoRootFull.Length).Replace('\','/')
    & git -C $repoRoot add -- $relativeOutputPath
    if ($LASTEXITCODE -ne 0) { throw "git add failed. Results remain local." }

    & git -C $repoRoot diff --cached --quiet
    if ($LASTEXITCODE -eq 1) {
        & git -C $repoRoot commit -m "Add financial reconciliation diagnostics $stamp"
        if ($LASTEXITCODE -ne 0) { throw "Git commit failed; results remain saved/staged." }
        & git -C $repoRoot push origin $expectedBranch
        if ($LASTEXITCODE -ne 0) { throw "Git push failed; local commit remains. Retry git push origin $expectedBranch" }
        Write-Host "Results committed and pushed successfully." -ForegroundColor Green
    }
    elseif ($LASTEXITCODE -eq 0) { Write-Host "No new result files to commit." -ForegroundColor Yellow }
    else { throw "Cannot inspect staged result changes." }
}
catch {
    Write-Warning ("Automatic Git publish was not completed: " + $_.Exception.Message)
    Write-Warning "Results remain in the output folder; unrelated files were not staged."
}
