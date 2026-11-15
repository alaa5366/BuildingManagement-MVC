<#
.SYNOPSIS
    Removes Firebase/Firestore comments from .cs files only.
.DESCRIPTION
    - Only touches comments (// and ///) in .cs files.
    - Skips: Migrations, wwwroot/lib, bin, obj, .Designer.cs
    - Creates backup before modifying.
	
	cd D:\projects\BuildingManagement-MVC
		.\Cleanup-Comments.ps1
		
	Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
		.\Cleanup-Comments.ps1
#>

$ProjectRoot = "D:\projects\BuildingManagement-MVC"

$ExcludeDirs = @("Migrations", "wwwroot\lib", "bin", "obj", ".vs", "node_modules")
$ExcludePatterns = @("*.Designer.cs", "*.g.cs", "*.AssemblyInfo.cs")
$Keywords = @("Firebase", "Firestore")

$TotalFiles = 0
$ModifiedFiles = 0
$TotalLinesRemoved = 0

$BackupDir = Join-Path "D:\projects\Backups-BM" "comments-cleanup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null

Write-Host "Searching in: $ProjectRoot" -ForegroundColor Cyan
Write-Host "Backup: $BackupDir" -ForegroundColor Cyan
Write-Host ""

$CsFiles = Get-ChildItem -Path $ProjectRoot -Filter "*.cs" -Recurse -File | Where-Object {
    $filePath = $_.FullName

    foreach ($dir in $ExcludeDirs) {
        if ($filePath -match [regex]::Escape($dir)) { return $false }
    }

    foreach ($pattern in $ExcludePatterns) {
        if ($_.Name -like $pattern) { return $false }
    }

    return $true
}

Write-Host "Files to scan: $($CsFiles.Count)" -ForegroundColor Yellow
Write-Host ""

foreach ($file in $CsFiles) {
    $TotalFiles++
    $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    $lines = $content -split "`r?`n"
    $newLines = @()
    $removedInFile = 0

    foreach ($line in $lines) {
        $trimmed = $line.Trim()
        $shouldRemove = $false

        # Match single-line comments: // or ///
        if ($trimmed -match '^\s*//+\s*(.*)$') {
            $commentText = $matches[1]
            foreach ($kw in $Keywords) {
                if ($commentText -match $kw) {
                    $shouldRemove = $true
                    break
                }
            }
        }

        if ($shouldRemove) {
            $removedInFile++
        } else {
            $newLines += $line
        }
    }

    if ($removedInFile -gt 0) {
        $backupPath = Join-Path $BackupDir ($file.FullName.Replace($ProjectRoot, "").TrimStart('\'))
        $backupDirForFile = Split-Path $backupPath -Parent
        New-Item -ItemType Directory -Path $backupDirForFile -Force | Out-Null
        Copy-Item -Path $file.FullName -Destination $backupPath -Force

        $newContent = $newLines -join "`r`n"
        Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8 -NoNewline

        $ModifiedFiles++
        $TotalLinesRemoved += $removedInFile
        Write-Host "  OK $($file.FullName.Replace($ProjectRoot, '').TrimStart('\')) -- $removedInFile lines" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Total files scanned: $TotalFiles" -ForegroundColor White
Write-Host "  Modified files: $ModifiedFiles" -ForegroundColor Green
Write-Host "  Lines removed: $TotalLinesRemoved" -ForegroundColor Yellow
Write-Host "  Backup: $BackupDir" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Done. Now run: dotnet build" -ForegroundColor Green