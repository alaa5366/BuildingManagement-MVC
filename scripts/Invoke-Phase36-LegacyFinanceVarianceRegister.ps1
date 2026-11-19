[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$reportDir = Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$report = Join-Path $reportDir ("Phase36-LegacyFinanceVarianceRegister-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + ".md")
$runId = [guid]'36363636-3636-4636-8636-363636363636'
$expenseId = [guid]'36363636-3636-4636-8636-000000000001'
$revenueId = [guid]'36363636-3636-4636-8636-000000000002'
$log = [System.Collections.Generic.List[string]]::new()
function Log([string]$s) { $log.Add($s); Write-Host $s }
function Query($c, [string]$sql, $tx = $null) {
    $cmd = $c.CreateCommand(); $cmd.CommandTimeout = 300; $cmd.CommandText = $sql
    if ($null -ne $tx) { $cmd.Transaction = $tx }
    $dt = [System.Data.DataTable]::new()
    $da = [System.Data.SqlClient.SqlDataAdapter]::new($cmd)
    [void]$da.Fill($dt); return ,$dt
}
$c = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try {
    $c.Open()
    $db = [string](Query $c "SELECT DB_NAME() N").Rows[0].N
    if ($db -ne 'BuildingManagement_RestoreTest') { throw "Target database mismatch: $db" }
    $required = @('Expenses','Revenues','MonthlyApartmentShares','LegacyOperationalRecords','FinancialReconciliationRuns','FinancialReconciliationDifferences','__EFMigrationsHistory')
    foreach ($t in $required) {
        $v = (Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N
        if ($null -eq $v -or $v -is [DBNull]) { throw "Required table dbo.$t is missing" }
    }
    $migration = [int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20261118213732_Phase08_FinancialLedgerFoundation'").Rows[0].N
    if ($migration -ne 1) { throw 'Phase08 financial reconciliation schema migration is not applied.' }
    $profile = Query $c @"
;WITH SourceTotals AS (
    SELECT N'ExpenseShareVariance' DifferenceType, e.BuildingId, e.MonthKey PeriodKey,
           SUM(e.Amount) SourceAmount
    FROM dbo.Expenses e GROUP BY e.BuildingId,e.MonthKey
    UNION ALL
    SELECT N'RevenueShareVariance', r.BuildingId, r.MonthKey, SUM(r.Amount)
    FROM dbo.Revenues r GROUP BY r.BuildingId,r.MonthKey
),
ShareTotals AS (
    SELECT s.BuildingId,s.MonthKey PeriodKey,
           SUM(s.ExpenseShare) ExpenseShares,SUM(s.RevenueShare) RevenueShares
    FROM dbo.MonthlyApartmentShares s GROUP BY s.BuildingId,s.MonthKey
),
Candidates AS (
    SELECT x.DifferenceType,x.BuildingId,x.PeriodKey,x.SourceAmount,
           CASE WHEN x.DifferenceType=N'ExpenseShareVariance' THEN y.ExpenseShares ELSE y.RevenueShares END TargetAmount,
           CASE WHEN x.DifferenceType=N'ExpenseShareVariance' THEN y.ExpenseShares-x.SourceAmount ELSE y.RevenueShares-x.SourceAmount END DifferenceAmount
    FROM SourceTotals x JOIN ShareTotals y ON y.BuildingId=x.BuildingId AND y.PeriodKey=x.PeriodKey
)
SELECT DifferenceType,BuildingId,PeriodKey,SourceAmount,TargetAmount,DifferenceAmount
FROM Candidates
WHERE (DifferenceType=N'ExpenseShareVariance' AND DifferenceAmount=CONVERT(decimal(19,4),0.22))
   OR (DifferenceType=N'RevenueShareVariance' AND DifferenceAmount=CONVERT(decimal(19,4),0.04))
ORDER BY DifferenceType;
"@
    $expense = @($profile.Rows | Where-Object { $_.DifferenceType -eq 'ExpenseShareVariance' })
    $revenue = @($profile.Rows | Where-Object { $_.DifferenceType -eq 'RevenueShareVariance' })
    if ($expense.Count -ne 1 -or $revenue.Count -ne 1) {
        throw "Expected exactly one evidenced expense variance (+0.22) and one revenue variance (+0.04); found expense=$($expense.Count), revenue=$($revenue.Count). No write performed."
    }
    $e = $expense[0]; $r = $revenue[0]
    if ([decimal]$e.SourceAmount -ne 4700 -or [decimal]$e.TargetAmount -ne 4700.22 -or
        [decimal]$r.SourceAmount -ne 27173 -or [decimal]$r.TargetAmount -ne 27173.04) {
        throw 'Variance totals differ from the reviewed Phase28 evidence. No write performed.'
    }
    $evidence = Query $c @"
SELECT COUNT(*) N
FROM dbo.LegacyOperationalRecords
WHERE SourceSystem=N'BuildingManagement.Legacy'
  AND SourceTable IN (N'Expenses',N'Revenues',N'MonthlyApartmentShares');
"@
    if ([int]$evidence.Rows[0].N -lt 129) { throw 'Expected Phase29 expense/revenue/share evidence is incomplete. No write performed.' }
    $existing = Query $c "SELECT COUNT(*) N FROM dbo.FinancialReconciliationRuns WHERE ReconciliationRunId='$runId'"
    if ([int]$existing.Rows[0].N -eq 0) {
        Log 'Registration state: NOT YET REGISTERED'
    } else {
        $check = Query $c "SELECT COUNT(*) N FROM dbo.FinancialReconciliationDifferences WHERE ReconciliationRunId='$runId' AND ReconciliationDifferenceId IN ('$expenseId','$revenueId')"
        if ([int]$check.Rows[0].N -ne 2) { throw 'The deterministic Phase36 run exists but its two expected difference rows are incomplete. Manual review required.' }
        Log 'Registration state: ALREADY REGISTERED (idempotent; no duplicate rows will be created)'
    }
    Log "Database: $db"
    Log 'Expense: source total=4700.00; existing apartment shares=4700.22; variance=+0.22'
    Log 'Revenue: source total=27173.00; existing apartment shares=27173.04; variance=+0.04'
    Log 'Scope: exact legacy building/month discovered from source aggregates; no apartment-level attribution.'
    Log 'Disposition: Preserve as a documented legacy rounding/allocation variance; no auto-allocation, ledger posting, wallet adjustment, or source correction.'
    if (-not $Apply) {
        Log 'DRY RUN - no data changed'
    } elseif ([int]$existing.Rows[0].N -eq 0) {
        $tx = $c.BeginTransaction()
        try {
            $cmd = $c.CreateCommand(); $cmd.Transaction = $tx; $cmd.CommandTimeout = 300
            $cmd.CommandText = @"
INSERT dbo.FinancialReconciliationRuns
    (ReconciliationRunId,RuleVersion,SourceSystem,StartedAtUtc,CompletedAtUtc,Status,StartedByAccountId,SafeSummary)
VALUES
    ('$runId',N'phase36-legacy-variance-register-v1',N'legacy-sql',SYSUTCDATETIME(),SYSUTCDATETIME(),1,NULL,
     N'Registered two reviewed historical source-vs-share variances as non-corrective reconciliation items. No allocations or ledger postings were generated.');

INSERT dbo.FinancialReconciliationDifferences
    (ReconciliationDifferenceId,ReconciliationRunId,DifferenceType,BuildingId,ApartmentId,PeriodKey,SourceType,SourceKey,
     SourceAmount,TargetAmount,DifferenceAmount,Classification,Disposition,CreatedAtUtc,ReviewedByAccountId,ReviewedAtUtc)
VALUES
    ('$expenseId','$runId',N'ExpenseShareVariance',@ExpenseBuilding,NULL,@ExpensePeriod,N'ExpensesVsMonthlyApartmentShares',NULL,
     4700.00,4700.22,0.22,N'LegacyRoundingVariance',
     N'Preserve as a separate historical reconciliation variance. Do not allocate to apartments, post to ledger, change wallet balances, or modify source records.',SYSUTCDATETIME(),NULL,NULL),
    ('$revenueId','$runId',N'RevenueShareVariance',@RevenueBuilding,NULL,@RevenuePeriod,N'RevenuesVsMonthlyApartmentShares',NULL,
     27173.00,27173.04,0.04,N'LegacyRoundingVariance',
     N'Preserve as a separate historical reconciliation variance. Do not allocate to apartments, post to ledger, change wallet balances, or modify source records.',SYSUTCDATETIME(),NULL,NULL);
"@
            $p = $cmd.Parameters.Add('@ExpenseBuilding',[System.Data.SqlDbType]::NVarChar,64); $p.Value=[string]$e.BuildingId
            $p = $cmd.Parameters.Add('@ExpensePeriod',[System.Data.SqlDbType]::Char,7); $p.Value=[string]$e.PeriodKey
            $p = $cmd.Parameters.Add('@RevenueBuilding',[System.Data.SqlDbType]::NVarChar,64); $p.Value=[string]$r.BuildingId
            $p = $cmd.Parameters.Add('@RevenuePeriod',[System.Data.SqlDbType]::Char,7); $p.Value=[string]$r.PeriodKey
            [void]$cmd.ExecuteNonQuery()
            $tx.Commit()
            Log 'APPLY TRANSACTION COMMITTED'
        } catch { $tx.Rollback(); throw }
    } elseif ($Apply) {
        Log 'No-op: this deterministic Phase36 registration is already present.'
    }
    $verify = Query $c "SELECT DifferenceType,Classification,SourceAmount,TargetAmount,DifferenceAmount FROM dbo.FinancialReconciliationDifferences WHERE ReconciliationRunId='$runId' ORDER BY DifferenceType"
    if ($Apply -and $verify.Rows.Count -ne 2) { throw 'Post-apply verification failed: expected exactly two registered variance rows.' }
    if ($Apply) { Log "Verified reconciliation difference rows: $($verify.Rows.Count)" }
    $log | Set-Content -LiteralPath $report -Encoding UTF8
    @(
        '# Phase 36 - Legacy finance variance register',
        '',
        "- Run: $(Get-Date -Format o)",
        "- Mode: $(if($Apply){'APPLY'}else{'READ ONLY / DRY RUN'})",
        '',
        '## Registered historical differences',
        '- Expenses: source total 4,700.00; existing apartment shares 4,700.22; variance +0.22.',
        '- Revenues: source total 27,173.00; existing apartment shares 27,173.04; variance +0.04.',
        '',
        'Both differences are separate reconciliation items classified as LegacyRoundingVariance. They are not assigned to apartments and are not posted to the ledger or wallet.',
        '',
        '## Safeguards',
        '- Source rows and the 158 Phase29 finance evidence records are read only.',
        '- No FinancialPeriod, LedgerTransaction, LedgerEntry, AllocationBatch, AllocationLine, deposit posting, wallet adjustment, or balance is created.',
        '- The register uses one deterministic run ID and deterministic difference IDs; rerunning is idempotent.',
        '- Building and period scope are resolved from matching source aggregates; no apartment scope is inferred.',
        '- No accounting correction or write-off is implied; the variance remains visible and unresolved for future accounting-owner disposition.',
        ''
    ) | Add-Content -LiteralPath $report -Encoding UTF8
    Write-Host "Report: $report"
} catch {
    Log "ERROR: $($_.Exception.Message)"
    throw
} finally {
    if ($c.State -ne 'Closed') { $c.Close() }
}
