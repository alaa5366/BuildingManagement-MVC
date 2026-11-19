[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$reportDir = Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$report = Join-Path $reportDir ("Phase29-FinanceEvidence-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + ".md")
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
    foreach ($t in @('Deposits','DepositEditHistory','Expenses','Revenues','MonthlyApartmentShares','WalletAdjustments','LegacyOperationalRecords','LegacyDataMigrationMap','__EFMigrationsHistory')) {
        $v = (Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N
        if ($null -eq $v -or $v -is [DBNull]) { throw "Required table dbo.$t is missing" }
    }
    foreach ($m in @('20261118213732_Phase08_FinancialLedgerFoundation','20261119100000_Phase09_OperationalDomainSchema','20261119200000_Phase20_IdentityProfileEvidence')) {
        $n = [int](Query $c "SELECT COUNT(*) N FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'$m'").Rows[0].N
        if ($n -ne 1) { throw "Required migration not applied: $m" }
    }
    $sql = @"
SET NOCOUNT ON;
CREATE TABLE #FinanceSource(
 SourceTable nvarchar(128) NOT NULL,
 SourceKey nvarchar(256) NOT NULL,
 BuildingId nvarchar(64) NULL,
 PayloadJson nvarchar(max) NOT NULL,
 SourceFingerprint varbinary(32) NOT NULL
);
INSERT #FinanceSource
SELECT N'Deposits',CONCAT(BuildingId,N'|',Id),BuildingId,
 (SELECT d.BuildingId,d.Id,d.MonthKey,d.Number,d.ApartmentId,d.Amount,d.Note,d.Status,d.ReceiptUrl,d.ReceiptPath,d.ReceiptFileName,d.ReceiptFileSize,d.ReceiptFileType,d.ReceiptSuccess,d.CreatedAt,d.CreatedBy,d.ConfirmedAt,d.ConfirmedBy,d.CancelledAt,d.CancelledBy,d.CancelledReason,d.UpdatedAt,d.UpdatedBy,d.UpdateReason
  FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT d.BuildingId,d.Id,d.MonthKey,d.Number,d.ApartmentId,d.Amount,d.Note,d.Status,d.ReceiptUrl,d.ReceiptPath,d.ReceiptFileName,d.ReceiptFileSize,d.ReceiptFileType,d.ReceiptSuccess,d.CreatedAt,d.CreatedBy,d.ConfirmedAt,d.ConfirmedBy,d.CancelledAt,d.CancelledBy,d.CancelledReason,d.UpdatedAt,d.UpdatedBy,d.UpdateReason FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)))
FROM dbo.Deposits d;
INSERT #FinanceSource
SELECT N'DepositEditHistory',CONVERT(nvarchar(256),EditId),BuildingId,
 (SELECT h.EditId,h.BuildingId,h.DepositId,h.Ts,h.ByUser,h.Reason,h.OldAmount,h.NewAmount,h.OldNote,h.NewNote FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT h.EditId,h.BuildingId,h.DepositId,h.Ts,h.ByUser,h.Reason,h.OldAmount,h.NewAmount,h.OldNote,h.NewNote FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)))
FROM dbo.DepositEditHistory h;
INSERT #FinanceSource
SELECT N'Expenses',CONCAT(BuildingId,N'|',Id),BuildingId,
 (SELECT e.BuildingId,e.Id,e.MonthKey,e.CategoryId,e.CategoryKind,e.Note,e.Amount,e.ExpenseDate,e.ReceiptUrl FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT e.BuildingId,e.Id,e.MonthKey,e.CategoryId,e.CategoryKind,e.Note,e.Amount,e.ExpenseDate,e.ReceiptUrl FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)))
FROM dbo.Expenses e;
INSERT #FinanceSource
SELECT N'Revenues',CONCAT(BuildingId,N'|',Id),BuildingId,
 (SELECT r.BuildingId,r.Id,r.MonthKey,r.CategoryId,r.CategoryKind,r.Note,r.Amount,r.RevenueDate FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT r.BuildingId,r.Id,r.MonthKey,r.CategoryId,r.CategoryKind,r.Note,r.Amount,r.RevenueDate FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)))
FROM dbo.Revenues r;
INSERT #FinanceSource
SELECT N'MonthlyApartmentShares',CONCAT(BuildingId,N'|',MonthKey,N'|',ApartmentId),BuildingId,
 (SELECT s.BuildingId,s.MonthKey,s.ApartmentId,s.ExpenseShare,s.RevenueShare,s.CarryOver FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT s.BuildingId,s.MonthKey,s.ApartmentId,s.ExpenseShare,s.RevenueShare,s.CarryOver FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)))
FROM dbo.MonthlyApartmentShares s;
INSERT #FinanceSource
SELECT N'WalletAdjustments',CONCAT(BuildingId,N'|',Id),BuildingId,
 (SELECT w.BuildingId,w.Id,w.ApartmentId,w.Amount,w.Reason,w.MonthKey,w.CreatedAt,w.CreatedBy FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT w.BuildingId,w.Id,w.ApartmentId,w.Amount,w.Reason,w.MonthKey,w.CreatedAt,w.CreatedBy FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)))
FROM dbo.WalletAdjustments w;
IF EXISTS(SELECT 1 FROM #FinanceSource GROUP BY SourceTable,SourceKey HAVING COUNT(*)>1)
 THROW 51001,'Duplicate legacy finance source keys detected.',1;
IF EXISTS(
 SELECT 1 FROM #FinanceSource s
 JOIN dbo.LegacyOperationalRecords t ON t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=s.SourceTable AND t.SourceKey=s.SourceKey
 WHERE t.SourceFingerprint<>s.SourceFingerprint
) THROW 51002,'Source drift detected for an already preserved finance record; no overwrite is permitted.',1;
IF EXISTS(
 SELECT 1 FROM #FinanceSource s
 JOIN dbo.LegacyDataMigrationMap m ON m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=s.SourceTable AND m.SourceKey=s.SourceKey
 LEFT JOIN dbo.LegacyOperationalRecords t ON t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=s.SourceTable AND t.SourceKey=s.SourceKey
 WHERE t.LegacyOperationalRecordId IS NULL OR m.SourceFingerprint<>s.SourceFingerprint
) THROW 51003,'Inconsistent existing finance provenance detected.',1;
SELECT s.SourceTable,COUNT(*) SourceRows,
 SUM(CASE WHEN t.LegacyOperationalRecordId IS NULL THEN 1 ELSE 0 END) PendingRows
FROM #FinanceSource s
LEFT JOIN dbo.LegacyOperationalRecords t ON t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=s.SourceTable AND t.SourceKey=s.SourceKey
GROUP BY s.SourceTable ORDER BY s.SourceTable;
"@
    $profile = Query $c $sql
    Log "Database: $db"
    $sourceTotal = 0; $pendingTotal = 0
    foreach ($r in $profile.Rows) {
        Log "$($r.SourceTable): source=$($r.SourceRows), pending=$($r.PendingRows)"
        $sourceTotal += [int]$r.SourceRows; $pendingTotal += [int]$r.PendingRows
    }
    Log "Total finance source rows: $sourceTotal"
    Log "Pending evidence rows: $pendingTotal"
    if (-not $Apply) { Log 'DRY RUN - no data changed' }
    else {
        $tx = $c.BeginTransaction()
        try {
            $applySql = @"
SET NOCOUNT ON;
CREATE TABLE #FinanceSource(
 SourceTable nvarchar(128) NOT NULL, SourceKey nvarchar(256) NOT NULL, BuildingId nvarchar(64) NULL,
 PayloadJson nvarchar(max) NOT NULL, SourceFingerprint varbinary(32) NOT NULL
);
INSERT #FinanceSource
SELECT N'Deposits',CONCAT(BuildingId,N'|',Id),BuildingId,
 (SELECT d.BuildingId,d.Id,d.MonthKey,d.Number,d.ApartmentId,d.Amount,d.Note,d.Status,d.ReceiptUrl,d.ReceiptPath,d.ReceiptFileName,d.ReceiptFileSize,d.ReceiptFileType,d.ReceiptSuccess,d.CreatedAt,d.CreatedBy,d.ConfirmedAt,d.ConfirmedBy,d.CancelledAt,d.CancelledBy,d.CancelledReason,d.UpdatedAt,d.UpdatedBy,d.UpdateReason FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT d.BuildingId,d.Id,d.MonthKey,d.Number,d.ApartmentId,d.Amount,d.Note,d.Status,d.ReceiptUrl,d.ReceiptPath,d.ReceiptFileName,d.ReceiptFileSize,d.ReceiptFileType,d.ReceiptSuccess,d.CreatedAt,d.CreatedBy,d.ConfirmedAt,d.ConfirmedBy,d.CancelledAt,d.CancelledBy,d.CancelledReason,d.UpdatedAt,d.UpdatedBy,d.UpdateReason FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)))
FROM dbo.Deposits d;
INSERT #FinanceSource
SELECT N'DepositEditHistory',CONVERT(nvarchar(256),EditId),BuildingId,
 (SELECT h.EditId,h.BuildingId,h.DepositId,h.Ts,h.ByUser,h.Reason,h.OldAmount,h.NewAmount,h.OldNote,h.NewNote FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT h.EditId,h.BuildingId,h.DepositId,h.Ts,h.ByUser,h.Reason,h.OldAmount,h.NewAmount,h.OldNote,h.NewNote FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)))
FROM dbo.DepositEditHistory h;
INSERT #FinanceSource
SELECT N'Expenses',CONCAT(BuildingId,N'|',Id),BuildingId,
 (SELECT e.BuildingId,e.Id,e.MonthKey,e.CategoryId,e.CategoryKind,e.Note,e.Amount,e.ExpenseDate,e.ReceiptUrl FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT e.BuildingId,e.Id,e.MonthKey,e.CategoryId,e.CategoryKind,e.Note,e.Amount,e.ExpenseDate,e.ReceiptUrl FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)))
FROM dbo.Expenses e;
INSERT #FinanceSource
SELECT N'Revenues',CONCAT(BuildingId,N'|',Id),BuildingId,
 (SELECT r.BuildingId,r.Id,r.MonthKey,r.CategoryId,r.CategoryKind,r.Note,r.Amount,r.RevenueDate FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT r.BuildingId,r.Id,r.MonthKey,r.CategoryId,r.CategoryKind,r.Note,r.Amount,r.RevenueDate FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)))
FROM dbo.Revenues r;
INSERT #FinanceSource
SELECT N'MonthlyApartmentShares',CONCAT(BuildingId,N'|',MonthKey,N'|',ApartmentId),BuildingId,
 (SELECT s.BuildingId,s.MonthKey,s.ApartmentId,s.ExpenseShare,s.RevenueShare,s.CarryOver FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT s.BuildingId,s.MonthKey,s.ApartmentId,s.ExpenseShare,s.RevenueShare,s.CarryOver FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)))
FROM dbo.MonthlyApartmentShares s;
INSERT #FinanceSource
SELECT N'WalletAdjustments',CONCAT(BuildingId,N'|',Id),BuildingId,
 (SELECT w.BuildingId,w.Id,w.ApartmentId,w.Amount,w.Reason,w.MonthKey,w.CreatedAt,w.CreatedBy FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT w.BuildingId,w.Id,w.ApartmentId,w.Amount,w.Reason,w.MonthKey,w.CreatedAt,w.CreatedBy FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)))
FROM dbo.WalletAdjustments w;
IF EXISTS(SELECT 1 FROM #FinanceSource GROUP BY SourceTable,SourceKey HAVING COUNT(*)>1) THROW 51001,'Duplicate source keys.',1;
IF EXISTS(SELECT 1 FROM #FinanceSource s JOIN dbo.LegacyOperationalRecords t ON t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=s.SourceTable AND t.SourceKey=s.SourceKey WHERE t.SourceFingerprint<>s.SourceFingerprint) THROW 51002,'Source drift detected.',1;
INSERT dbo.LegacyOperationalRecords(LegacyOperationalRecordId,SourceSystem,SourceTable,SourceKey,BuildingId,PayloadJson,SourceFingerprint)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.finance.evidence.v1|',s.SourceTable,N'|',s.SourceKey))),1,16)),
 N'BuildingManagement.Legacy',s.SourceTable,s.SourceKey,s.BuildingId,s.PayloadJson,s.SourceFingerprint
FROM #FinanceSource s
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyOperationalRecords t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=s.SourceTable AND t.SourceKey=s.SourceKey);
INSERT dbo.LegacyDataMigrationMap(MigrationMapId,SourceSystem,SourceTable,SourceKey,TargetTable,TargetKey,SourceFingerprint,Outcome,Disposition,SafeNote)
SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.finance.map.v1|',s.SourceTable,N'|',s.SourceKey))),1,16)),
 N'BuildingManagement.Legacy',s.SourceTable,s.SourceKey,N'LegacyOperationalRecords',CONVERT(nvarchar(36),t.LegacyOperationalRecordId),s.SourceFingerprint,1,N'PreservedAsLegacyFinancialEvidence',
 N'Exact legacy row retained as immutable evidence; not yet posted to ledger or used to recalculate balances.'
FROM #FinanceSource s
JOIN dbo.LegacyOperationalRecords t ON t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=s.SourceTable AND t.SourceKey=s.SourceKey
WHERE NOT EXISTS(SELECT 1 FROM dbo.LegacyDataMigrationMap m WHERE m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=s.SourceTable AND m.SourceKey=s.SourceKey);
"@
            $cmd = $c.CreateCommand(); $cmd.Transaction = $tx; $cmd.CommandTimeout = 300; $cmd.CommandText = $applySql
            [void]$cmd.ExecuteNonQuery()
            $tx.Commit()
        } catch { $tx.Rollback(); throw }
        $verify = Query $c @"
SELECT s.SourceTable,COUNT(*) SourceRows,
 SUM(CASE WHEN t.LegacyOperationalRecordId IS NOT NULL THEN 1 ELSE 0 END) EvidenceRows,
 SUM(CASE WHEN m.MigrationMapId IS NOT NULL THEN 1 ELSE 0 END) ProvenanceRows
FROM (
 SELECT N'Deposits' SourceTable,CONCAT(BuildingId,N'|',Id) SourceKey FROM dbo.Deposits
 UNION ALL SELECT N'DepositEditHistory',CONVERT(nvarchar(256),EditId) FROM dbo.DepositEditHistory
 UNION ALL SELECT N'Expenses',CONCAT(BuildingId,N'|',Id) FROM dbo.Expenses
 UNION ALL SELECT N'Revenues',CONCAT(BuildingId,N'|',Id) FROM dbo.Revenues
 UNION ALL SELECT N'MonthlyApartmentShares',CONCAT(BuildingId,N'|',MonthKey,N'|',ApartmentId) FROM dbo.MonthlyApartmentShares
 UNION ALL SELECT N'WalletAdjustments',CONCAT(BuildingId,N'|',Id) FROM dbo.WalletAdjustments
) s
LEFT JOIN dbo.LegacyOperationalRecords t ON t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceTable=s.SourceTable AND t.SourceKey=s.SourceKey
LEFT JOIN dbo.LegacyDataMigrationMap m ON m.SourceSystem=N'BuildingManagement.Legacy' AND m.SourceTable=s.SourceTable AND m.SourceKey=s.SourceKey
GROUP BY s.SourceTable ORDER BY s.SourceTable;
"@
        $ok = $true; $grand = 0
        foreach ($r in $verify.Rows) {
            Log "$($r.SourceTable): source=$($r.SourceRows), evidence=$($r.EvidenceRows), provenance=$($r.ProvenanceRows)"
            if ([int]$r.SourceRows -ne [int]$r.EvidenceRows -or [int]$r.SourceRows -ne [int]$r.ProvenanceRows) { $ok = $false }
            $grand += [int]$r.SourceRows
        }
        if (-not $ok -or $grand -ne $sourceTotal) { throw 'Finance evidence reconciliation failed.' }
        Log "Total reconciled: $grand"
        Log 'RECONCILIATION PASS'
    }
} catch {
    Log "ERROR: $($_.Exception.Message)"
    throw
} finally {
    if ($c.State -ne 'Closed') { $c.Close() }
    $mode = if ($Apply) { 'APPLY' } else { 'DRY RUN' }
    $lines = @('# Phase 29 - Finance evidence backfill','',"- Run: $(Get-Date -Format o)","- Mode: $mode",'','## Results')
    $lines += $log | ForEach-Object { "- $_" }
    $lines += @('','## Safeguards',
      '- All six legacy finance tables are preserved row-for-row with original source keys, JSON payloads, and SHA-256 fingerprints.',
      '- Existing evidence is never overwritten; source drift or inconsistent provenance aborts the transaction.',
      '- No financial periods, ledger transactions, ledger entries, payment postings, balances, or allocation lines are synthesized in this phase.',
      '- No source rows are updated or deleted.',
      '- Payloads and personal/financial row values are not written to this report.')
    Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
    Write-Host "Report: $report"
}
Push-Location $root
try {
    git add -- $PSCommandPath $report
    if ((git status --short -- $PSCommandPath $report).Count -gt 0) {
        git commit --only -m "feat: preserve legacy finance evidence" -- $PSCommandPath $report
        git push origin development/database-redesign-enterprise
    }
} finally { Pop-Location }
