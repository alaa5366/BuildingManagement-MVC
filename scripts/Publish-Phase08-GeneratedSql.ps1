$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$Repo = 'D:\Projects\BuildingManagement-MVC'
$Branch = 'development/database-redesign-enterprise'
$SourceSql = Join-Path $env:TEMP 'Phase08-20261119-012912.sql'
$ExpectedSha256 = 'CFA0D13D61AE0389660C56462987A3B94B723AAE50207CC6A89BD1D6A8D5F616'
$Destination = Join-Path $Repo 'Database\Redesign\Generated-Sql\Phase08-20261119-012912.sql'

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

Set-Location $Repo
if ((Invoke-Git -GitArgs @('branch','--show-current')) -ne $Branch) {
    throw "Expected branch $Branch. No files changed."
}
if (-not (Test-Path -LiteralPath $SourceSql -PathType Leaf)) {
    throw "Generated SQL file not found: $SourceSql. Re-run the SQL generation review only if this file was removed."
}
$actualHash = (Get-FileHash -LiteralPath $SourceSql -Algorithm SHA256).Hash
if ($actualHash -ne $ExpectedSha256) {
    throw "SQL hash mismatch. Expected $ExpectedSha256; found $actualHash. Nothing published."
}
$sql = [System.IO.File]::ReadAllText($SourceSql)
if ([string]::IsNullOrWhiteSpace($sql) -or $sql.Length -lt 1000) {
    throw 'Generated SQL is empty or unexpectedly small.'
}
if (Test-Path -LiteralPath $Destination) {
    throw "Destination already exists; refusing to overwrite: $Destination"
}
New-Item -ItemType Directory -Path (Split-Path -Parent $Destination) -Force | Out-Null
[System.IO.File]::WriteAllText($Destination,$sql,[System.Text.UTF8Encoding]::new($false))
$relative = $Destination.Substring($Repo.Length + 1).Replace('\','/')
Invoke-Git -GitArgs @('add','--',$relative) | Out-Null
Invoke-Git -GitArgs @('commit','--only','-m','review: publish generated Phase08 SQL for inspection','--',$relative) | Out-Null
Invoke-Git -GitArgs @('push','origin',$Branch) | Out-Null
Write-Host "Generated SQL published for source review: $relative" -ForegroundColor Green
Write-Host "SHA256 verified: $actualHash"
Write-Host 'This operation only publishes the SQL text. It does not execute SQL or apply migrations.'
