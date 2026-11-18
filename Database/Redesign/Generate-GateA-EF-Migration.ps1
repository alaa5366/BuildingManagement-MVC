[CmdletBinding()]
param(
    [string]$RepoPath = "D:\Projects\BuildingManagement-MVC",
    [string]$MigrationName = "Phase06_IdentityAuthorizationFoundation"
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ExpectedBranch = "development/database-redesign-enterprise"
$Project = Join-Path $RepoPath "BuildingManagementMVC.csproj"
$TestProject = Join-Path $RepoPath "Tests\BuildingManagement.Tests\BuildingManagement.Tests.csproj"
function Invoke-Git {
    param([string[]]$Arguments)
    $previous = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = @(& git -C $RepoPath @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    } finally { $ErrorActionPreference = $previous }
    if ($exitCode -ne 0) { throw ("git " + ($Arguments -join " ") + " failed: " + (($output | ForEach-Object ToString) -join [Environment]::NewLine)) }
    return $output
}
function Invoke-DotNet {
    param([string[]]$Arguments)
    Write-Host ("> dotnet " + ($Arguments -join ' ')) -ForegroundColor Cyan
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet command failed with exit code $LASTEXITCODE." }
}
if (-not (Test-Path -LiteralPath $Project -PathType Leaf)) { throw "Project not found: $Project" }
if (-not (Test-Path -LiteralPath $TestProject -PathType Leaf)) { throw "Test project not found: $TestProject" }
$root = (Invoke-Git -Arguments @("rev-parse","--show-toplevel") | Select-Object -First 1).Trim()
if ([IO.Path]::GetFullPath($root) -ne [IO.Path]::GetFullPath($RepoPath)) { throw "RepoPath is not the Git root." }
$branch = (Invoke-Git -Arguments @("branch","--show-current") | Select-Object -First 1).Trim()
if ($branch -ne $ExpectedBranch) { throw "Expected branch '$ExpectedBranch'; found '$branch'." }
$status = @(Invoke-Git -Arguments @("status","--porcelain"))
if (($status -join "").Trim()) { throw "Working tree must be clean before generating the migration." }
$efVersion = & dotnet ef --version 2>&1
if ($LASTEXITCODE -ne 0) { throw "EF CLI unavailable. Install: dotnet tool install --global dotnet-ef --version 8.0.10" }
Write-Host "EF CLI: $efVersion"
Invoke-DotNet -Arguments @("ef","migrations","add",$MigrationName,"--project",$Project,"--startup-project",$Project,"--context","AppDbContext","--output-dir","Migrations")
Invoke-DotNet -Arguments @("build",$Project,"--no-restore")
Invoke-DotNet -Arguments @("test",$TestProject,"--no-restore")
Write-Host "Migration generation, build, and tests completed. No database was connected to or modified." -ForegroundColor Green
Write-Host "Review the migration Up/Down operations and compare them with GateA-Additive-Identity-Foundation.sql."
$approval = Read-Host "Type COMMIT-REVIEWED-MIGRATION to stage the identity model and migration files, then push"
if ($approval -cne "COMMIT-REVIEWED-MIGRATION") { Write-Host "Generated files remain local and uncommitted."; exit 0 }
$paths = @("Data\AppDbContext.cs","Data\IdentityEntities.cs","Data\IdentityModelConfiguration.cs","Migrations")
Invoke-Git -Arguments (@("add","--") + $paths) | Out-Null
$staged = @(Invoke-Git -Arguments @("diff","--cached","--name-only"))
$allowed = @("Data/AppDbContext.cs","Data/IdentityEntities.cs","Data/IdentityModelConfiguration.cs")
$unexpected = @($staged | Where-Object {
    $normalized = $_.Replace("\","/")
    ($allowed -notcontains $normalized) -and ($normalized -notlike "Migrations/*")
})
if ($unexpected.Count -gt 0) { throw "Unexpected staged paths: $($unexpected -join ', ')" }
if (-not ($staged | Where-Object { $_ -like "Migrations/*_Phase06_IdentityAuthorizationFoundation.cs" })) { throw "Expected Phase06 migration source was not staged." }
Invoke-Git -Arguments @("commit","-m","Phase06: add identity and authorization foundation") | Out-Null
Invoke-Git -Arguments @("push","origin",$ExpectedBranch) | Out-Null
if (@(Invoke-Git -Arguments @("status","--porcelain")).Count -gt 0) { throw "Push completed but working tree is not clean." }
Write-Host "Identity EF migration, model snapshot, and verified build/test results committed and pushed." -ForegroundColor Green
