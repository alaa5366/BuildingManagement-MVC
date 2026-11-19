# Publishes the approved local Phase08/identity implementation files and a compact verification report.
# Safe: no database update, reset, rebase, force push, or deletion.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$Repo = 'D:\Projects\BuildingManagement-MVC'
$Branch = 'development/database-redesign-enterprise'
$Stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$ReportDir = Join-Path $Repo 'Database\Redesign\Review-Reports'
$ReportPath = Join-Path $ReportDir "Phase08-Local-Publication-$Stamp.md"
$Files = @(
'Controllers/AccountController.cs','Data/AppDbContext.cs','Data/FinanceEntities.cs','Data/FinanceModelConfiguration.cs',
'Migrations/AppDbContextModelSnapshot.cs',
'Migrations/20261118213732_Phase08_FinancialLedgerFoundation.cs',
'Migrations/20261118213732_Phase08_FinancialLedgerFoundation.Designer.cs',
'Migrations/20261118224524_Phase08FinanceReversalScopeIntegrity.cs',
'Migrations/20261118224524_Phase08FinanceReversalScopeIntegrity.Designer.cs',
'Program.cs','Services/AuthResult.cs','Services/IdentityAccountAuthenticationService.cs','Services/SessionRevalidator.cs',
'Tests/BuildingManagement.Tests/FinanceModelConfigurationTests.cs',
'Tests/BuildingManagement.Tests/IdentityAccountAuthenticationServiceTests.cs',
'Database/Redesign/Financial-Ledger-and-Wallet-Target-Design.md',
'Database/Redesign/Phase06-Identity-Authorization-Migration-Recheck-2026-11-18.md',
'Database/Redesign/README.md','Database/Redesign/Unified-Implementation-Plan-2026-11-18.md',
'Database/Redesign/Full-Database-Migration-Baseline-2026-11-18.md',
'Database/Redesign/GateA-Phase06-Review.sql','Phase08-Finance-Review.txt'
)
function Invoke-Native {
 param([string]$File,[string[]]$Arguments)
 $oldEap=$ErrorActionPreference
 $hasNative=Test-Path Variable:PSNativeCommandUseErrorActionPreference
 $oldNative=$null
 if($hasNative){$oldNative=$PSNativeCommandUseErrorActionPreference;$PSNativeCommandUseErrorActionPreference=$false}
 try {
  $ErrorActionPreference='Continue'
  $o=& $File @Arguments 2>&1
  $ec=$LASTEXITCODE
  [pscustomobject]@{Command="$File $($Arguments -join ' ')";ExitCode=$ec;Output=(($o|ForEach-Object{$_.ToString()}) -join [Environment]::NewLine).Trim()}
 } finally {
  $ErrorActionPreference=$oldEap
  if($hasNative){$PSNativeCommandUseErrorActionPreference=$oldNative}
 }
}
Set-Location $Repo
if(-not(Test-Path '.\BuildingManagementMVC.csproj')){throw 'Project file missing.'}
$b=Invoke-Native 'git' @('branch','--show-current')
if($b.ExitCode -ne 0 -or $b.Output.Trim() -ne $Branch){throw "Wrong branch. Expected $Branch."}
$s=Invoke-Native 'git' @('diff','--cached','--name-only')
if($s.ExitCode -ne 0){throw $s.Output}
if($s.Output){throw "Pre-existing staged changes detected: $($s.Output)"}
$f=Invoke-Native 'git' @('fetch','origin',$Branch)
if($f.ExitCode -ne 0){throw "Fetch failed: $($f.Output)"}
$m=Invoke-Native 'git' @('merge','--ff-only','@{u}')
if($m.ExitCode -ne 0){throw "Fast-forward sync failed; no reset/rewrite performed: $($m.Output)"}
$missing=@($Files|Where-Object{-not(Test-Path(Join-Path $Repo $_))})
if($missing.Count){throw "Expected files missing; nothing staged: $($missing -join '; ')"}
$results=[System.Collections.Generic.List[object]]::new()
$build=Invoke-Native 'dotnet' @('build','.\BuildingManagementMVC.csproj','--no-restore');$results.Add($build)
$tests=Invoke-Native 'dotnet' @('test','.\Tests\BuildingManagement.Tests\BuildingManagement.Tests.csproj');$results.Add($tests)
$ef=Invoke-Native 'dotnet' @('ef','migrations','has-pending-model-changes','--project','.\BuildingManagementMVC.csproj','--startup-project','.\BuildingManagementMVC.csproj','--no-build');$results.Add($ef)
New-Item -ItemType Directory -Path $ReportDir -Force|Out-Null
$lines=[System.Collections.Generic.List[string]]::new()
$lines.Add('# Phase08 Local Implementation Publication');$lines.Add('')
$lines.Add("- Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')");$lines.Add("- Branch: $Branch")
$lines.Add("- HEAD: $((Invoke-Native 'git' @('rev-parse','HEAD')).Output)")
$lines.Add('- Database update: NOT RUN');$lines.Add('')
$lines.Add('## Verification')
foreach($r in $results){$lines.Add("### $($r.Command)");$lines.Add("- Exit code: $($r.ExitCode)");$lines.Add('Output:');$lines.Add($r.Output);$lines.Add('')}
$lines.Add('## Published paths');$lines.AddRange([string[]]$Files);$lines.Add('')
$lines.Add('## Safety');$lines.Add('- No database was modified.');$lines.Add('- No reset, rebase, force push, deletion, or migration application was performed.')
$lines.Add('- Only the explicit allowlist and this report are staged.')
[System.IO.File]::WriteAllLines($ReportPath,$lines,[System.Text.UTF8Encoding]::new($false))
$rel=$ReportPath.Substring($Repo.Length+1).Replace('\','/')
$add=Invoke-Native 'git' (@('add','--')+$Files+@($rel))
if($add.ExitCode -ne 0){throw "git add failed: $($add.Output)"}
$c=Invoke-Native 'git' @('commit','-m',"Publish Phase08 finance and identity implementation $Stamp")
if($c.ExitCode -ne 0){throw "Commit failed: $($c.Output)"}
$p=Invoke-Native 'git' @('push','origin',$Branch)
if($p.ExitCode -ne 0){throw "Push failed: $($p.Output)"}
Write-Host "Published implementation and report: $rel" -ForegroundColor Green
Write-Host "Build=$($build.ExitCode); Tests=$($tests.ExitCode); EF=$($ef.ExitCode); database update NOT RUN"
if($build.ExitCode -ne 0 -or $tests.ExitCode -ne 0 -or $ef.ExitCode -ne 0){throw 'Publication completed but verification failed. Do not apply migrations; inspect the pushed report.'}
