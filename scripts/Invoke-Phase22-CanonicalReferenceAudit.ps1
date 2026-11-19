[CmdletBinding()]
param([string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30")
$ErrorActionPreference='Stop';$root=Split-Path -Parent $PSScriptRoot;$dir=Join-Path $root 'Database/Redesign/Review-Reports';New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase22-CanonicalReferenceAudit-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md");$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Query($c,[string]$sql){$cmd=$c.CreateCommand();$cmd.CommandTimeout=180;$cmd.CommandText=$sql;$dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt);return ,$dt}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open();$db=(Query $c "SELECT DB_NAME() N").Rows[0].N;if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @('Buildings','BuildingSettings','FinancialCategories','__EFMigrationsHistory')){$x=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N;if($null -eq $x -or $x -is [DBNull]){throw "Missing canonical table dbo.$t"}}
 $badSettings=[int](Query $c "SELECT COUNT(*) N FROM dbo.BuildingSettings s WHERE NOT EXISTS(SELECT 1 FROM dbo.Buildings b WHERE b.Id=s.BuildingId)").Rows[0].N
 $badCategories=[int](Query $c "SELECT COUNT(*) N FROM dbo.FinancialCategories f WHERE NOT EXISTS(SELECT 1 FROM dbo.Buildings b WHERE b.Id=f.BuildingId)").Rows[0].N
 $settings=[int](Query $c "SELECT COUNT(*) N FROM dbo.BuildingSettings").Rows[0].N;$categories=[int](Query $c "SELECT COUNT(*) N FROM dbo.FinancialCategories").Rows[0].N
 Log "Database: $db";Log "Canonical BuildingSettings rows: $settings";Log "Canonical FinancialCategories rows: $categories";Log "Orphan BuildingSettings: $badSettings";Log "Orphan FinancialCategories: $badCategories"
 if($badSettings -or $badCategories){throw 'Canonical reference integrity failed.'}
 Log 'CANONICAL TABLE RECONCILIATION PASS - no duplicate target copies required'
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $lines=@('# Phase 22 - Canonical reference table audit','',"- Run: $(Get-Date -Format o)",'- Mode: READ ONLY','','## Results');$lines+=$log|%{"- $_"}
 $lines+=@('','## Mapping decision','- Buildings, BuildingSettings, and FinancialCategories remain the canonical shared tables; creating duplicate copies would split reference identity.','- This audit is read-only and does not alter or delete source rows.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8;Write-Host "Report: $report"
}
Push-Location $root
try{$sp=Join-Path $PSScriptRoot 'Invoke-Phase22-CanonicalReferenceAudit.ps1';git add -- $sp $report;if((git status --short -- $sp $report).Count -gt 0){git commit --only -m "docs: add canonical reference integrity audit" -- $sp $report;git push origin development/database-redesign-enterprise}}finally{Pop-Location}
