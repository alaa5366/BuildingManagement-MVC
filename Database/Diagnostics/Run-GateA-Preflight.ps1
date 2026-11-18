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

# Automatically commit and push ONLY this run's result folder.
# Existing staged changes are treated as a safety stop so they cannot be
# accidentally included in the generated commit.
try {
    $repoRootOutput = & git -C $PSScriptRoot rev-parse --show-toplevel 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Git repository was not found from script directory. Results remain saved locally. Details: $repoRootOutput"
    }
    $repoRoot = ([string]$repoRootOutput).Trim()
    $repoRootFull = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $outputFull = [System.IO.Path]::GetFullPath($outputDir)

    if (-not $outputFull.StartsWith($repoRootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Safety stop: output folder is outside the Git repository. Results remain saved locally."
    }

    $branchOutput = & git -C $repoRoot branch --show-current 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to determine current Git branch. Details: $branchOutput"
    }
    $currentBranch = ([string]$branchOutput).Trim()
    $expectedBranch = "development/database-redesign-enterprise"
    if ($currentBranch -ne $expectedBranch) {
        throw "Safety stop: current branch is '$currentBranch'; switch to '$expectedBranch'. Results remain saved locally."
    }

    & git -C $repoRoot diff --cached --quiet
    $cachedExitCode = $LASTEXITCODE
    if ($cachedExitCode -eq 1) {
        throw "Safety stop: Git index already contains staged changes. Commit/preserve them separately; preflight results remain saved locally."
    }
    elseif ($cachedExitCode -ne 0) {
        throw "Unable to inspect Git staged changes. Results remain saved locally."
    }

    $relativeOutputPath = $outputFull.Substring($repoRootFull.Length).Replace('\', '/')
    Write-Host ""
    Write-Host "Staging only: $relativeOutputPath" -ForegroundColor Cyan
    & git -C $repoRoot add -- $relativeOutputPath
    if ($LASTEXITCODE -ne 0) {
        throw "git add failed. Results remain saved locally."
    }

    & git -C $repoRoot diff --cached --quiet
    $stagedExitCode = $LASTEXITCODE
    if ($stagedExitCode -eq 0) {
        Write-Host "No new Git changes to commit for this result folder." -ForegroundColor Yellow
    }
    elseif ($stagedExitCode -eq 1) {
        $commitMessage = "Add Gate A preflight results $stamp"
        & git -C $repoRoot commit -m $commitMessage
        if ($LASTEXITCODE -ne 0) {
            throw "Git commit failed. Results are saved and staged locally; no push was attempted."
        }

        Write-Host "Pushing $expectedBranch to origin ..." -ForegroundColor Cyan
        & git -C $repoRoot push origin $expectedBranch
        if ($LASTEXITCODE -ne 0) {
            throw "Git push failed. The commit is saved locally; retry: git push origin $expectedBranch"
        }

        Write-Host "Results committed and pushed successfully." -ForegroundColor Green
    }
    else {
        throw "Unable to inspect staged result changes. Results remain saved locally."
    }
}
catch {
    Write-Warning ("Automatic Git publish was not completed: " + $_.Exception.Message)
    Write-Warning "The preflight CSV/README files remain in the output folder shown above. No unrelated files were staged by this script."
}

