[CmdletBinding()]
param(
    [string]$SourceConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30",
    [string]$TargetConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$reportDir = Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$report = Join-Path $reportDir ("Phase51-LegacyDependencyRetirementAudit-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + ".md")

function Open-Checked([string]$cs,[string]$expected) {
    $cn = [System.Data.SqlClient.SqlConnection]::new($cs)
    $cn.Open()
    $cmd = $cn.CreateCommand(); $cmd.CommandText = 'SELECT DB_NAME()'
    $actual = [string]$cmd.ExecuteScalar()
    if ($actual -ne $expected) { $cn.Close(); throw "Expected database '$expected', connected to '$actual'." }
    return $cn
}
function Read-Table($cn,[string]$sql) {
    $cmd=$cn.CreateCommand(); $cmd.CommandTimeout=900; $cmd.CommandText=$sql
    $dt=[System.Data.DataTable]::new()
    $da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd)
    [void]$da.Fill($dt)
    return ,$dt
}
function Q([string]$n) { '[' + $n.Replace(']',']]') + ']' }

$source=$null; $target=$null
try {
    $source=Open-Checked $SourceConnectionString 'BuildingManagement'
    $target=Open-Checked $TargetConnectionString 'BuildingManagement_RestoreTest'

    $sourceTables=Read-Table $source @"
SELECT s.name SchemaName,t.name TableName
FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
WHERE t.is_ms_shipped=0 ORDER BY s.name,t.name;
"@
    $targetTables=Read-Table $target @"
SELECT s.name SchemaName,t.name TableName
FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
WHERE t.is_ms_shipped=0 ORDER BY s.name,t.name;
"@
    $targetFks=Read-Table $target @"
SELECT fk.name ForeignKeyName, ps.name ParentSchema, pt.name ParentTable,
       rs.name ReferencedSchema, rt.name ReferencedTable, fk.is_disabled, fk.is_not_trusted
FROM sys.foreign_keys fk
JOIN sys.tables pt ON pt.object_id=fk.parent_object_id
JOIN sys.schemas ps ON ps.schema_id=pt.schema_id
JOIN sys.tables rt ON rt.object_id=fk.referenced_object_id
JOIN sys.schemas rs ON rs.schema_id=rt.schema_id
ORDER BY ps.name,pt.name,fk.name;
"@

    $legacyNames=@($sourceTables.Rows | ForEach-Object { [string]$_.TableName } | Sort-Object -Unique)
    $scanExtensions=@('.cs','.cshtml','.sql','.ps1','.json')
    $excluded=@('\.git\\','\\bin\\','\\obj\\','\\packages\\')
    $files=Get-ChildItem -LiteralPath $root -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -in $scanExtensions -and -not ($_.FullName -match '\\(bin|obj|\.git|packages)\\') }
    $references=[System.Collections.Generic.List[object]]::new()
    foreach($file in $files) {
        $lines=Get-Content -LiteralPath $file.FullName
        for($i=0;$i -lt $lines.Count;$i++) {
            foreach($name in $legacyNames) {
                if($lines[$i] -match ('(?<![A-Za-z0-9_])' + [regex]::Escape($name) + '(?![A-Za-z0-9_])')) {
                    $relative=$file.FullName.Substring($root.Length).TrimStart('\','/')
                    $references.Add([pscustomobject]@{Table=$name;File=$relative;Line=$i+1;Text=$lines[$i].Trim()})
                }
            }
        }
    }

    $targetFkTouches=[System.Collections.Generic.List[object]]::new()
    foreach($fk in $targetFks.Rows) {
        if($legacyNames -contains [string]$fk.ParentTable -or $legacyNames -contains [string]$fk.ReferencedTable) {
            $targetFkTouches.Add($fk)
        }
    }

    $targetSet=@{}
    foreach($t in $targetTables.Rows){$targetSet["$($t.SchemaName).$($t.TableName)"]=$true}
    $linesOut=@(
        '# Phase 51 — Legacy application dependency and retirement audit','',
        "- Run: $(Get-Date -Format o)",
        '- Mode: READ ONLY; no DDL or DML is executed.',
        '- Source: BuildingManagement (connection identity validated).',
        '- Target: BuildingManagement_RestoreTest (connection identity validated).',
        '- Purpose: identify application references and target foreign keys that must be migrated or removed before legacy tables can be retired.',
        '',
        '## Inventory','',
        "- Source user tables: $($sourceTables.Rows.Count)",
        "- Target user tables: $($targetTables.Rows.Count)",
        "- Source table names found in application/repository files: $($references.Count) references",
        "- Target foreign keys touching source table names: $($targetFkTouches.Count)",
        '',
        '## Per-table application references','',
        '| Legacy table | Repository references | Target same-name table |',
        '|---|---:|---|'
    )
    foreach($name in $legacyNames) {
        $count=@($references | Where-Object Table -eq $name).Count
        $same=if($targetSet.ContainsKey("dbo.$name")){'present'}else{'missing'}
        $linesOut += "| $name | $count | $same |"
    }
    $linesOut += @('','## Target foreign-key dependencies touching legacy names','','| FK | Parent | Referenced | Disabled | Untrusted |','|---|---|---|---|---|')
    foreach($fk in $targetFkTouches) {
        $linesOut += "| $($fk.ForeignKeyName) | $($fk.ParentSchema).$($fk.ParentTable) | $($fk.ReferencedSchema).$($fk.ReferencedTable) | $($fk.is_disabled) | $($fk.is_not_trusted) |"
    }
    $linesOut += @('','## Source-code reference locations','','| Legacy table | File | Line | Reference |','|---|---|---:|---|')
    foreach($r in $references) {
        $safe=$r.Text.Replace('|','\|')
        if($safe.Length -gt 240){$safe=$safe.Substring(0,240)+'…'}
        $linesOut += "| $($r.Table) | $($r.File) | $($r.Line) | $safe |"
    }
    $linesOut += @('','## Retirement decision','','- This phase does not delete, rename, truncate, or alter any table, constraint, row, or database setting.',
        '- A source table is not eligible for deletion merely because a same-name target table or a migration-map row exists.',
        '- Application references must be removed or redirected to verified canonical target services/entities before retirement.',
        '- Every FK touching a source table name must be reviewed against the target model; do not disable constraints as a shortcut.',
        '- The 32-table source inventory includes infrastructure metadata and operational data; preserve __EFMigrationsHistory and any runtime-owned objects until their owners are explicitly migrated.',
        '- The old source database is never modified by this phase.',
        '',
        '**Result:** dependency inventory only. This report is not a deletion authorization.'
    )
    Set-Content -LiteralPath $report -Value ($linesOut -join [Environment]::NewLine) -Encoding UTF8
    Write-Host "PHASE51 READ-ONLY DEPENDENCY AUDIT COMPLETE. Report: $report"
    Write-Host "Source tables: $($sourceTables.Rows.Count); target tables: $($targetTables.Rows.Count); application references: $($references.Count); target FK dependencies: $($targetFkTouches.Count)"
}
finally {
    if($source -and $source.State -ne 'Closed'){$source.Close()}
    if($target -and $target.State -ne 'Closed'){$target.Close()}
}
Push-Location $root
try {
    git add -- $PSCommandPath $report
    if((git status --short -- $PSCommandPath $report).Count -gt 0) {
        git commit --only -m "feat(migration): audit legacy table application dependencies" -- $PSCommandPath $report
        git push origin development/database-redesign-enterprise
    }
} finally { Pop-Location }
