<#
  Builds a zip of the project that is safe to share: no secrets, keys, backups, logs,
  user-specific files, deployment profiles or helper scripts.
  Usage (from the project folder):  powershell -File 
  To Run
  1-	Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
  2-	tools\make-clean-zip.ps1 BuildingManagement-clean.zip (or use -Output ..\BuildingManagement-clean.zip)
#>
param([string]$Output = "..\BuildingManagement-clean.zip")

$root = Split-Path -Parent $PSScriptRoot
$exclude = @(
  '[\\/]\.vs[\\/]', '[\\/]\.git[\\/]', '[\\/]bin[\\/]', '[\\/]obj[\\/]',
  '[\\/]keys[\\/]', '[\\/]Backups[\\/]', '[\\/]logs[\\/]',
  'UpgradeLog.*\.htm$', '_UpgradeReport_Files', '\.user$', '\.pubxml$',
  '\.py$', 'REXX.*\.ps1$', '^[\\/][^\\/]+\.ps1$', 'migration_production\.sql$',
  'appsettings\.Development\.json$'
)

$files = Get-ChildItem -Path $root -Recurse -File -Force | Where-Object {
  $rel = $_.FullName.Substring($root.Length)
  -not ($exclude | Where-Object { $rel -match $_ })
}

$tmp = Join-Path $env:TEMP ("bm-clean-" + [guid]::NewGuid().ToString("N"))
foreach ($f in $files) {
  $dest = Join-Path $tmp $f.FullName.Substring($root.Length)
  New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
  Copy-Item $f.FullName $dest
}
if (Test-Path $Output) { Remove-Item $Output -Force }
Compress-Archive -Path (Join-Path $tmp '*') -DestinationPath $Output
Remove-Item $tmp -Recurse -Force
Write-Host "Created $Output ($($files.Count) files)"
