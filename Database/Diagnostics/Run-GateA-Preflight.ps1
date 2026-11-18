[CmdletBinding()]
param(
    [string]$ServerInstance = "(localdb)\MSSQLLocalDB",
    [string]$Database = "BuildingManagement_RestoreTest",
    [string]$OutputRoot = (Join-Path $PSScriptRoot "GateA-Preflight-Results")
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$expectedDatabase = "BuildingManagement_RestoreTest"
if ($Database -ne $expectedDatabase) {
    throw "Safety stop: this preflight runner is restricted to $expectedDatabase."
}

$sqlPath = Join-Path $PSScriptRoot "GateA-Preflight-Validation.sql"
if (-not (Test-Path -LiteralPath $sqlPath -PathType Leaf)) {
    throw "Preflight SQL file not found: $sqlPath"
}

$sql = Get-Content -LiteralPath $sqlPath -Raw
if ($sql -notmatch '(?im)^\s*USE\s+\[BuildingManagement_RestoreTest\]\s*;') {
    throw "Safety stop: preflight SQL does not target the approved review database."
}
if ($sql -notmatch "DB_NAME\(\)\s*<>\s*N'BuildingManagement_RestoreTest'") {
    throw "Safety stop: expected database guard is missing from the SQL script."
}

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
$builder["Application Name"] = "BuildingManagement GateA ReadOnly Preflight"

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
    if ($actualDatabase -ne $expectedDatabase) {
        throw "Safety stop: connected to '$actualDatabase', expected '$expectedDatabase'."
    }

    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 180
    $reader = $command.ExecuteReader()

    do {
        if ($reader.FieldCount -gt 0) {
            $resultIndex++
            $rows = New-Object 'System.Collections.Generic.List[object]'
            $fieldCount = $reader.FieldCount
            $fieldNames = @(for ($i = 0; $i -lt $fieldCount; $i++) { $reader.GetName($i) })

            while ($reader.Read()) {
                $values = New-Object 'object[]' $fieldCount
                [void]$reader.GetValues($values)
                $record = [ordered]@{}
                for ($i = 0; $i -lt $fieldCount; $i++) {
                    $value = $values[$i]
                    if ($value -is [System.DBNull]) { $value = $null }
                    $record[$fieldNames[$i]] = $value
                }
                $rows.Add([pscustomobject]$record)
            }

            $fileName = "{0:D2}-GateA-Preflight-Result.csv" -f $resultIndex
            $filePath = Join-Path $outputDir $fileName
            if ($rows.Count -gt 0) {
                $rows | Export-Csv -LiteralPath $filePath -NoTypeInformation -Encoding UTF8
            }
            else {
                $header = ($fieldNames | ForEach-Object { '"' + ($_ -replace '"','""') + '"' }) -join ','
                Set-Content -LiteralPath $filePath -Value $header -Encoding UTF8
            }

            $totalRows += $rows.Count
            Write-Host ("  {0}: {1} row(s)" -f $fileName, $rows.Count) -ForegroundColor Green
        }
    } while ($reader.NextResult())

    $readme = @"
BuildingManagement Enterprise Redesign — Gate A Read-only Preflight
Generated: $(Get-Date -Format o)
Server instance: $ServerInstance
Database: $actualDatabase
Result sets exported: $resultIndex
Total rows: $totalRows

Purpose:
- Verify the exact legacy key SQL types, lengths, and collations.
- Verify Buildings/Apartments key and index metadata.
- Compare referenced key-column collations.
- Capture the database default collation.
- Confirm whether any Gate A target objects already exist.
- Capture the applied EF Core migration history.

Privacy and safety:
- Windows Integrated Security is used; no password is requested or saved.
- The runner is restricted to BuildingManagement_RestoreTest.
- The SQL batch is read-only and queries SQL Server metadata only.
- No application row data or secret values are exported.
- No database objects or application data are changed.

Review:
- TYPE_LENGTH_OK_REVIEW_COLLATION is not a final pass by itself.
- Compare all referenced column collations with each other and with the database default.
- Any missing key/index, collation mismatch, existing/partial Gate A object, or unexpected migration history blocks Gate A until reviewed.
"@
    Set-Content -LiteralPath (Join-Path $outputDir "README.txt") -Value $readme -Encoding UTF8

    Write-Host ""
    Write-Host "Gate A preflight completed. Result sets: $resultIndex; total rows: $totalRows" -ForegroundColor Green
    Write-Host "Output: $outputDir" -ForegroundColor Cyan
}
catch {
    Write-Error ("Gate A preflight failed: " + $_.Exception.Message)
    Write-Host "No database changes were made." -ForegroundColor Yellow
    throw
}
finally {
    if ($null -ne $reader) { $reader.Dispose() }
    if ($connection.State -ne [System.Data.ConnectionState]::Closed) { $connection.Close() }
    $connection.Dispose()
}
