[CmdletBinding()]
param([string]$OutputDirectory = "Database/Redesign/Review-Reports")
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$reportDir = Join-Path $root $OutputDirectory
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$report = Join-Path $reportDir ("Phase53-RefinedLegacyRuntimeReferenceAudit-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + ".md")
$legacyNames = @(
'__EFMigrationsHistory','Apartments','AuditLog','BackupHistory','BuildingAdmins','Buildings','BuildingSettings',
'Cameras','DepositEditHistory','Deposits','Dvrs','Expenses','FinancialCategories','Floors','GlobalSettings',
'LoginThrottle','MaintenanceRecords','MonthlyApartmentShares','Notifications','PollOptions','Polls','PollVotes',
'Presence','QrTokens','QrUsages','Residents','Revenues','ScheduledBackupSettings','SyncPendingChanges',
'UserPermissions','Users','WalletAdjustments')
$extensions = @('.cs','.cshtml','.razor','.json','.config','.xml','.ps1')
$skipDirectory = '\\(bin|obj|\.git|packages|node_modules|TestResults|\.vs)\\'
$allFiles = Get-ChildItem -LiteralPath $root -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Extension -in $extensions -and $_.FullName -notmatch $skipDirectory }
$runtime = [System.Collections.Generic.List[object]]::new()
$tests = [System.Collections.Generic.List[object]]::new()
$docs = [System.Collections.Generic.List[object]]::new()
$migrations = [System.Collections.Generic.List[object]]::new()
foreach ($file in $allFiles) {
    $relative = $file.FullName.Substring($root.Length).TrimStart('\','/')
    $category = if ($relative -match '(^|[/\\])(Tests?|.*Tests?)([/\\]|$)|\.Tests\.') { 'Test' }
        elseif ($relative -match '(^|[/\\])(Database|docs?|Documentation|Review-Reports)([/\\]|$)|Schema\.sql$|FullSchema\.sql$') { 'Docs' }
        elseif ($relative -match '(^|[/\\])(Migrations)([/\\]|$)') { 'Migration' }
        else { 'Runtime' }
    $lines = Get-Content -LiteralPath $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        foreach ($name in $legacyNames) {
            if ($lines[$i] -match ('(?<![A-Za-z0-9_])' + [regex]::Escape($name) + '(?![A-Za-z0-9_])')) {
                $entry = [pscustomobject]@{ Table=$name; File=$relative; Line=$i+1; Text=$lines[$i].Trim() }
                switch ($category) {
                    'Runtime' { $runtime.Add($entry) }
                    'Test' { $tests.Add($entry) }
                    'Docs' { $docs.Add($entry) }
                    'Migration' { $migrations.Add($entry) }
                }
            }
        }
    }
}
$out = [System.Collections.Generic.List[string]]::new()
$out.Add('# Phase 53 — Refined legacy runtime reference audit')
$out.Add("")
$out.Add("- Run: $(Get-Date -Format o)")
$out.Add('- Mode: READ ONLY repository scan; no database connection and no database changes.')
$out.Add('- Runtime excludes tests, documentation/schema snapshots, generated migrations, build output and package folders.')
$out.Add('- Text references are review leads, not proof of an EF mapping or live call path.')
$out.Add("")
$out.Add('## Summary')
$out.Add("")
$out.Add("- Runtime references: $($runtime.Count)")
$out.Add("- Test references: $($tests.Count)")
$out.Add("- Documentation/schema references: $($docs.Count)")
$out.Add("- Generated migration references: $($migrations.Count)")
$out.Add("")
$out.Add('| Legacy table | Runtime refs | Test refs | Docs/schema refs | Migration refs |')
$out.Add('|---|---:|---:|---:|---:|')
foreach ($name in $legacyNames) {
    $r=@($runtime | Where-Object Table -eq $name).Count
    $t=@($tests | Where-Object Table -eq $name).Count
    $d=@($docs | Where-Object Table -eq $name).Count
    $m=@($migrations | Where-Object Table -eq $name).Count
    $out.Add("| $name | $r | $t | $d | $m |")
}
foreach ($section in @(
    @{ Name='Runtime reference locations'; Items=$runtime },
    @{ Name='Test reference locations'; Items=$tests },
    @{ Name='Documentation/schema reference locations'; Items=$docs }
)) {
    $out.Add("")
    $out.Add("## $($section.Name)")
    $out.Add("")
    $out.Add('| Table | File | Line | Reference |')
    $out.Add('|---|---|---:|---|')
    foreach ($item in $section.Items) {
        $safe=$item.Text.Replace('|','\|')
        if ($safe.Length -gt 200) { $safe=$safe.Substring(0,200)+'…' }
        $out.Add("| $($item.Table) | $($item.File) | $($item.Line) | $safe |")
    }
}
$out.Add("")
$out.Add('## Decision rules')
$out.Add("")
$out.Add('- Runtime references must be resolved by an owning-module cutover; zero text references alone does not authorize DROP.')
$out.Add('- Test-only references indicate tests still encode legacy contracts and must be migrated or intentionally retired.')
$out.Add('- Documentation/schema references are not runtime blockers, but schema snapshots and migration history remain controlled artifacts.')
$out.Add('- Buildings, Floors and Apartments are foundational and excluded from retirement regardless of counts.')
$out.Add('- __EFMigrationsHistory is infrastructure metadata and is never part of legacy-domain retirement.')
$out.Add('- This audit does not authorize DDL. Retirement still requires target data parity, FK review, EF mapping review, module cutover and rollback acceptance.')
Set-Content -LiteralPath $report -Value ($out -join [Environment]::NewLine) -Encoding UTF8
Write-Host "PHASE53 READ-ONLY REFERENCE AUDIT COMPLETE. Report: $report"
Write-Host "Runtime: $($runtime.Count); tests: $($tests.Count); docs/schema: $($docs.Count); migrations: $($migrations.Count)"
Push-Location $root
try {
    git add -- $PSCommandPath $report
    if ((git status --short -- $PSCommandPath $report).Count -gt 0) {
        git commit --only -m "feat(migration): refine legacy runtime dependency audit" -- $PSCommandPath $report
        git push origin development/database-redesign-enterprise
    }
} finally { Pop-Location }
