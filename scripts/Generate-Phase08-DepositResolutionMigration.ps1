[CmdletBinding()]
param(
    [string]$RepoPath = "D:\Projects\BuildingManagement-MVC",
    [string]$MigrationName = "Phase08_PaymentDepositResolutionReasonIntegrity"
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ExpectedBranch = "development/database-redesign-enterprise"
$Project = Join-Path $RepoPath "BuildingManagementMVC.csproj"
$TestProject = Join-Path $RepoPath "Tests\BuildingManagement.Tests\BuildingManagement.Tests.csproj"
$ReportDirectory = Join-Path $RepoPath "Database\Redesign\Review-Reports"

function Invoke-Git {
    param([string[]]$Arguments)
    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = @(& git -C $RepoPath @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    } finally { $ErrorActionPreference = $previous }
    if ($exitCode -ne 0) {
        throw ("git " + ($Arguments -join " ") + " failed:" + [Environment]::NewLine + (($output | ForEach-Object ToString) -join [Environment]::NewLine))
    }
    return $output
}
function Invoke-DotNet {
    param([string[]]$Arguments)
    Write-Host ("> dotnet " + ($Arguments -join " ")) -ForegroundColor Cyan
    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = @(& dotnet @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    } finally { $ErrorActionPreference = $previous }
    $output | ForEach-Object { Write-Host $_ }
    if ($exitCode -ne 0) { throw "dotnet command failed with exit code $exitCode." }
    return @($output | ForEach-Object ToString)
}

if (-not (Test-Path -LiteralPath $Project -PathType Leaf)) { throw "Project not found: $Project" }
if (-not (Test-Path -LiteralPath $TestProject -PathType Leaf)) { throw "Test project not found: $TestProject" }
$root = (Invoke-Git -Arguments @("rev-parse","--show-toplevel") | Select-Object -First 1).Trim()
if ([IO.Path]::GetFullPath($root) -ne [IO.Path]::GetFullPath($RepoPath)) { throw "RepoPath is not the Git root." }
$branch = (Invoke-Git -Arguments @("branch","--show-current") | Select-Object -First 1).Trim()
if ($branch -ne $ExpectedBranch) { throw "Expected branch '$ExpectedBranch'; found '$branch'." }

$existing = @(Get-ChildItem -LiteralPath (Join-Path $RepoPath "Migrations") -Filter "*_$MigrationName.cs" -File -ErrorAction SilentlyContinue)
if ($existing.Count -gt 0) { throw "Migration '$MigrationName' already exists. Refusing to generate a duplicate." }

$efVersion = Invoke-DotNet -Arguments @("ef","--version")
Invoke-DotNet -Arguments @("restore",$Project)
Invoke-DotNet -Arguments @("ef","migrations","add",$MigrationName,"--project",$Project,"--startup-project",$Project,"--context","AppDbContext","--output-dir","Migrations")

$migrationFiles = @(Get-ChildItem -LiteralPath (Join-Path $RepoPath "Migrations") -Filter "*_$MigrationName.cs" -File)
if ($migrationFiles.Count -ne 1) { throw "Expected exactly one generated migration source; found $($migrationFiles.Count)." }
$migrationPath = $migrationFiles[0].FullName
$designerPath = [IO.Path]::ChangeExtension($migrationPath, ".Designer.cs")
$snapshotPath = Join-Path $RepoPath "Migrations\AppDbContextModelSnapshot.cs"
if (-not (Test-Path -LiteralPath $designerPath -PathType Leaf)) { throw "Generated migration designer is missing." }
if (-not (Test-Path -LiteralPath $snapshotPath -PathType Leaf)) { throw "EF model snapshot is missing." }

$migrationText = [IO.File]::ReadAllText($migrationPath)
if ($migrationText -notmatch 'DropCheckConstraint' -or $migrationText -notmatch 'AddCheckConstraint' -or
    $migrationText -notmatch 'CK_PaymentDeposits_Resolution') {
    throw "Generated migration does not contain the expected check-constraint replacement. Review manually; nothing was committed."
}
if ($migrationText -notmatch 'protected override void Down' -or $migrationText -notmatch 'protected override void Up') {
    throw "Generated migration Up/Down methods are incomplete."
}

$buildOutput = Invoke-DotNet -Arguments @("build",$Project,"--no-restore")
$testOutput = Invoke-DotNet -Arguments @("test",$TestProject,"--no-restore")

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportRelative = "Database/Redesign/Review-Reports/Phase08-Deposit-Resolution-Migration-Generation-$timestamp.md"
$reportPath = Join-Path $RepoPath ($reportRelative.Replace("/", "\"))
$report = @(
    "# Phase08 Deposit Resolution Constraint Migration Generation",
    "",
    "- Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "- Branch: $branch",
    "- EF CLI: $($efVersion -join ' ')",
    "- Migration: $($migrationFiles[0].Name)",
    "- Migration designer: $([IO.Path]::GetFileName($designerPath))",
    "- Model snapshot: Migrations/AppDbContextModelSnapshot.cs",
    "- Generated Up/Down constraint replacement source check: PASS",
    "- Build: PASS (dotnet build)",
    "- Tests: PASS (dotnet test)",
    "- Database connection/update/migration execution: NOT PERFORMED",
    "- Production database: NOT TARGETED",
    "",
    "## Scope",
    "",
    "The model now requires ResolutionReason to be NULL for Pending and Confirmed deposits, and non-empty for Cancelled and Reversed deposits. The generated migration replaces only CK_PaymentDeposits_Resolution.",
    "",
    "No database was updated. SQL generation and RestoreTest application remain separate gated steps.",
    "",
    "## Build summary",
    "",
    "Build output (last 18 lines):",
    ($buildOutput | Select-Object -Last 18),
    "",
    "## Test summary",
    "",
    "Test output (last 24 lines):",
    ($testOutput | Select-Object -Last 24)
) -join [Environment]::NewLine
New-Item -ItemType Directory -Path $ReportDirectory -Force | Out-Null
[IO.File]::WriteAllText($reportPath, $report + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))

$commitPaths = @(
    "Migrations/$($migrationFiles[0].Name)",
    "Migrations/$([IO.Path]::GetFileName($designerPath))",
    "Migrations/AppDbContextModelSnapshot.cs",
    $reportRelative
)
Invoke-Git -Arguments (@("add","--") + $commitPaths) | Out-Null
Invoke-Git -Arguments (@("commit","--only","-m","Phase08: enforce payment deposit resolution reason integrity","--") + $commitPaths) | Out-Null
Invoke-Git -Arguments @("push","origin",$ExpectedBranch) | Out-Null

Write-Host ""
Write-Host "Migration generated, build/tests passed, and exact migration artifacts/report pushed." -ForegroundColor Green
Write-Host "Report: $reportRelative"
Write-Host "No SQL was executed and no database was modified."
