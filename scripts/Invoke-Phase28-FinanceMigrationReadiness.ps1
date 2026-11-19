[CmdletBinding()]
param([string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30")
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase28-FinanceMigrationReadiness-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Query($c,[string]$sql){
 $cmd=$c.CreateCommand();$cmd.CommandTimeout=240;$cmd.CommandText=$sql
 $dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt);return ,$dt
}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
$sourceTables=@('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments')
$targetTables=@('FinancialPeriods','PaymentDeposits','LedgerTransactions','LedgerEntries','FinancialSourceLinks','FinancialAllocationBatches','FinancialAllocationLines','FinancialReconciliationRuns','FinancialReconciliationDifferences')
try{
 $c.Open()
 $db=[string](Query $c "SELECT DB_NAME() N").Rows[0].N
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @($sourceTables)+@($targetTables)+@('__EFMigrationsHistory')){
  $v=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N
  if($null -eq $v -or $v -is [DBNull]){throw "Missing required table dbo.$t"}
 }
 $migration=[int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261118213732_Phase08_FinancialLedgerFoundation'").Rows[0].N
 if($migration -ne 1){throw 'Phase08 finance foundation migration is not recorded.'}
 Log "Database: $db"
 Log 'Mode: READ ONLY; no source or target rows are changed.'
 Log ''
 Log '## Legacy source row counts'
 foreach($t in $sourceTables){
  $n=[long](Query $c "SELECT COUNT_BIG(*) N FROM dbo.[$t]").Rows[0].N
  Log "- $t = $n"
 }
 Log ''
 Log '## Target finance row counts'
 foreach($t in $targetTables){
  $n=[long](Query $c "SELECT COUNT_BIG(*) N FROM dbo.[$t]").Rows[0].N
  Log "- $t = $n"
 }
 Log ''
 Log '## Legacy source column inventory (names/types only; no data values)'
 foreach($t in $sourceTables){
  Log "### $t"
  $dt=Query $c @"
SELECT c.name AS ColumnName, ty.name AS DataType,
       CASE WHEN c.max_length=-1 THEN 'MAX'
            WHEN ty.name IN ('nchar','nvarchar') THEN CONVERT(varchar(12),c.max_length/2)
            WHEN ty.name IN ('char','varchar','binary','varbinary') THEN CONVERT(varchar(12),c.max_length)
            ELSE '' END AS MaxLength,
       c.precision, c.scale, c.is_nullable
FROM sys.columns c
JOIN sys.types ty ON c.user_type_id=ty.user_type_id
WHERE c.object_id=OBJECT_ID(N'dbo.$t')
ORDER BY c.column_id;
"@
  foreach($row in $dt.Rows){
   $nullable=if([bool]$row.is_nullable){'NULL'}else{'NOT NULL'}
   Log ("- {0}: {1}({2}), precision={3}, scale={4}, {5}" -f $row.ColumnName,$row.DataType,$row.MaxLength,$row.precision,$row.scale,$nullable)
  }
 }
 Log ''
 Log '## Source key and relationship constraints'
 $fk=Query $c @"
SELECT OBJECT_NAME(f.parent_object_id) AS SourceTable,
       COL_NAME(fc.parent_object_id,fc.parent_column_id) AS SourceColumn,
       OBJECT_NAME(f.referenced_object_id) AS ReferencedTable,
       COL_NAME(fc.referenced_object_id,fc.referenced_column_id) AS ReferencedColumn,
       f.name AS ConstraintName, f.is_disabled, f.is_not_trusted
FROM sys.foreign_keys f
JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=f.object_id
WHERE OBJECT_NAME(f.parent_object_id) IN ('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments')
ORDER BY SourceTable,ConstraintName,fc.constraint_column_id;
"@
 if($fk.Rows.Count -eq 0){Log '- No declared foreign keys found on the six legacy finance tables.'}
 else{foreach($row in $fk.Rows){Log ("- {0}.{1} -> {2}.{3}; constraint={4}; disabled={5}; untrusted={6}" -f $row.SourceTable,$row.SourceColumn,$row.ReferencedTable,$row.ReferencedColumn,$row.ConstraintName,$row.is_disabled,$row.is_not_trusted)}}
 Log ''
 Log '## Finance source aggregate profile'
 foreach($t in @('Deposits','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments')){
  $cols=(Query $c "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.$t')").Rows|%{$_.name}
  if($cols -contains 'BuildingId' -and $cols -contains 'MonthKey'){
   Log "### $t by BuildingId / MonthKey"
   if($cols -contains 'Amount'){
    $dt=Query $c "SELECT BuildingId,MonthKey,COUNT_BIG(*) AS Rows,SUM(CONVERT(decimal(38,4),Amount)) AS AmountTotal FROM dbo.[$t] GROUP BY BuildingId,MonthKey ORDER BY BuildingId,MonthKey"
    foreach($row in $dt.Rows){Log ("- Building={0}; Month={1}; Rows={2}; AmountTotal={3}" -f $row.BuildingId,$row.MonthKey,$row.Rows,$row.AmountTotal)}
   } elseif($cols -contains 'ExpenseShare' -and $cols -contains 'RevenueShare' -and $cols -contains 'CarryOver'){
    $dt=Query $c "SELECT BuildingId,MonthKey,COUNT_BIG(*) AS Rows,SUM(CONVERT(decimal(38,4),ExpenseShare)) AS ExpenseShareTotal,SUM(CONVERT(decimal(38,4),RevenueShare)) AS RevenueShareTotal,SUM(CONVERT(decimal(38,4),CarryOver)) AS CarryOverTotal FROM dbo.MonthlyApartmentShares GROUP BY BuildingId,MonthKey ORDER BY BuildingId,MonthKey"
    foreach($row in $dt.Rows){Log ("- Building={0}; Month={1}; Rows={2}; ExpenseShares={3}; RevenueShares={4}; CarryOver={5}" -f $row.BuildingId,$row.MonthKey,$row.Rows,$row.ExpenseShareTotal,$row.RevenueShareTotal,$row.CarryOverTotal)}
   }
  }
 }
 if(((Query $c "SELECT COUNT(*) N FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Deposits') AND name='Status'").Rows[0].N) -gt 0){
  Log '### Deposits by status'
  $dt=Query $c "SELECT CONVERT(nvarchar(80),Status) AS StatusValue,COUNT_BIG(*) AS Rows,SUM(CONVERT(decimal(38,4),Amount)) AS AmountTotal FROM dbo.Deposits GROUP BY Status ORDER BY Status"
  foreach($row in $dt.Rows){Log ("- Status={0}; Rows={1}; AmountTotal={2}" -f $row.StatusValue,$row.Rows,$row.AmountTotal)}
 }
 Log ''
 Log '## Migration blockers / interpretation'
 Log '- This phase is a read-only structural and aggregate profile; it does not perform a backfill.'
 Log '- Phase08 currently supplies target schema, not migrated finance business rows.'
 Log '- Do not synthesize FinancialPeriods, UTC timestamps, actor accounts, confirmed-deposit posting metadata, opening balances, or allocation lines until their legacy semantics are proven.'
 Log '- Preserve exact source keys, values, and timestamps; use FinancialSourceLinks for idempotent source identity when the approved mapping is ready.'
 Log '- Existing documented share variances and rounding behavior must be classified before any allocation or wallet projection is generated.'
 Log '- DepositEditHistory requires a separate immutable-history mapping; it must not be discarded or folded into current deposit state.'
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $lines=@('# Phase 28 - Finance migration readiness profile','',"- Run: $(Get-Date -Format o)",'- Mode: READ ONLY','','## Results')
 $lines+=$log
 $lines+=@('','## Safety','- No financial source or target rows were inserted, updated, or deleted.','- No balances were recalculated.','- No credentials or personal field values were selected.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{
 $script=Join-Path $PSScriptRoot 'Invoke-Phase28-FinanceMigrationReadiness.ps1'
 git add -- $script $report
 if((git status --short -- $script $report).Count -gt 0){
  git commit --only -m "docs: add Phase28 finance migration readiness profile" -- $script $report
  git push origin development/database-redesign-enterprise
 }
}finally{Pop-Location}
