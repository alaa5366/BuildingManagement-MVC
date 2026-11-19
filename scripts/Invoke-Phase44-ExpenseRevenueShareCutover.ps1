[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$reportDir = Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$report = Join-Path $reportDir "Phase44-ExpenseRevenueShareCutover-$stamp.md"
$log = [System.Collections.Generic.List[string]]::new()
function Log([string]$s) { $log.Add($s); Write-Host $s }
function New-Command($c,[string]$sql) {
    $cmd=$c.CreateCommand(); $cmd.CommandTimeout=300; $cmd.CommandText=$sql; return $cmd
}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
$mode=if($Apply){'APPLY'}else{'DRY RUN'}
$db=''
try {
    $c.Open()
    $db=[string](New-Command $c 'SELECT DB_NAME();').ExecuteScalar()
    if($db -ne 'BuildingManagement_RestoreTest') { throw "Unsafe target '$db'. This phase is restricted to BuildingManagement_RestoreTest." }
    $sql=@"
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.Expenses',N'U') IS NULL OR OBJECT_ID(N'dbo.Revenues',N'U') IS NULL
 OR OBJECT_ID(N'dbo.MonthlyApartmentShares',N'U') IS NULL OR OBJECT_ID(N'dbo.Apartments',N'U') IS NULL
 OR OBJECT_ID(N'dbo.UserRoleAssignments',N'U') IS NULL OR OBJECT_ID(N'dbo.LegacyOperationalRecords',N'U') IS NULL
 OR OBJECT_ID(N'dbo.FinancialPeriods',N'U') IS NULL OR OBJECT_ID(N'dbo.LedgerTransactions',N'U') IS NULL
 OR OBJECT_ID(N'dbo.LedgerEntries',N'U') IS NULL OR OBJECT_ID(N'dbo.FinancialSourceLinks',N'U') IS NULL
 THROW 51044,'Required legacy, evidence, identity, or ledger table is missing.',1;

-- Out-of-scope finance rows are intentionally preserved as immutable legacy evidence.
-- This cutover posts only the owner-approved September/October 2026 periods.

IF EXISTS(SELECT 1 FROM dbo.MonthlyApartmentShares s LEFT JOIN dbo.Apartments a
 ON a.BuildingId=s.BuildingId AND a.Id=s.ApartmentId WHERE a.Id IS NULL)
 THROW 51044,'A monthly share references a missing apartment/building.',1;

IF EXISTS(
 SELECT x.BuildingId FROM (
  SELECT BuildingId FROM dbo.Expenses WHERE MonthKey IN(N'2026-09',N'2026-10')
  UNION SELECT BuildingId FROM dbo.Revenues WHERE MonthKey IN(N'2026-09',N'2026-10')
  UNION SELECT BuildingId FROM dbo.MonthlyApartmentShares WHERE MonthKey IN(N'2026-09',N'2026-10')
 ) x
 OUTER APPLY(SELECT COUNT(*) Cnt FROM dbo.UserRoleAssignments a
  WHERE a.RoleKey=N'building-admin' AND a.ScopeType=1 AND a.ScopeBuildingId=x.BuildingId
   AND a.ScopeApartmentId IS NULL AND a.RevokedAtUtc IS NULL
   AND (a.ValidToUtc IS NULL OR a.ValidToUtc>SYSUTCDATETIME())) q
 WHERE q.Cnt<>1
) THROW 51044,'Each finance building must have exactly one active building-scoped Admin actor.',1;

IF EXISTS(
 SELECT N'Expenses' SourceType,CONCAT(e.BuildingId,N'|',e.Id) SourceKey
 FROM dbo.Expenses e LEFT JOIN dbo.LegacyOperationalRecords p
  ON p.SourceSystem=N'BuildingManagement.Legacy' AND p.SourceTable=N'Expenses'
  AND p.SourceKey=CONCAT(e.BuildingId,N'|',e.Id)
 WHERE p.LegacyOperationalRecordId IS NULL OR DATALENGTH(p.SourceFingerprint)<>32
 UNION ALL
 SELECT N'Revenues',CONCAT(r.BuildingId,N'|',r.Id)
 FROM dbo.Revenues r LEFT JOIN dbo.LegacyOperationalRecords p
  ON p.SourceSystem=N'BuildingManagement.Legacy' AND p.SourceTable=N'Revenues'
  AND p.SourceKey=CONCAT(r.BuildingId,N'|',r.Id)
 WHERE p.LegacyOperationalRecordId IS NULL OR DATALENGTH(p.SourceFingerprint)<>32
) THROW 51044,'Phase29 immutable source evidence/fingerprint is missing for an expense or revenue.',1;

IF EXISTS(
 SELECT 1 FROM dbo.FinancialSourceLinks GROUP BY SourceSystem,SourceType,SourceKey HAVING COUNT(*)>1
) THROW 51044,'Duplicate financial source links detected.',1;

CREATE TABLE #Ops(
 BuildingId nvarchar(64) NOT NULL, MonthKey char(7) NOT NULL, Kind tinyint NOT NULL,
 SourceRows int NOT NULL, SourceAmount decimal(19,4) NOT NULL, EffectiveAtUtc datetime2(3) NOT NULL,
 PeriodId uniqueidentifier NOT NULL, TransactionId uniqueidentifier NOT NULL, ActorAccountId uniqueidentifier NOT NULL,
 PRIMARY KEY(BuildingId,MonthKey,Kind)
);
INSERT #Ops
SELECT e.BuildingId,e.MonthKey,2,COUNT(*),SUM(e.Amount),
 CAST(CAST(MIN(e.ExpenseDate) AS datetime2(3)) AT TIME ZONE N'Egypt Standard Time' AT TIME ZONE N'UTC' AS datetime2(3)),
 CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.finance.period.v1|',e.BuildingId,N'|',e.MonthKey))),1,16)),
 CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.finance.expense-ledger.v1|',e.BuildingId,N'|',e.MonthKey))),1,16)),
 admin.AccountId
FROM dbo.Expenses e
CROSS APPLY(SELECT TOP(1) a.AccountId FROM dbo.UserRoleAssignments a
 WHERE a.RoleKey=N'building-admin' AND a.ScopeType=1 AND a.ScopeBuildingId=e.BuildingId
 AND a.ScopeApartmentId IS NULL AND a.RevokedAtUtc IS NULL
 AND (a.ValidToUtc IS NULL OR a.ValidToUtc>SYSUTCDATETIME())
 ORDER BY a.GrantedAtUtc DESC,a.RoleAssignmentId) admin
WHERE e.MonthKey IN(N'2026-09',N'2026-10')
GROUP BY e.BuildingId,e.MonthKey,admin.AccountId;

INSERT #Ops
SELECT r.BuildingId,r.MonthKey,3,COUNT(*),SUM(r.Amount),
 CAST(CAST(MIN(r.RevenueDate) AS datetime2(3)) AT TIME ZONE N'Egypt Standard Time' AT TIME ZONE N'UTC' AS datetime2(3)),
 CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.finance.period.v1|',r.BuildingId,N'|',r.MonthKey))),1,16)),
 CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.finance.revenue-ledger.v1|',r.BuildingId,N'|',r.MonthKey))),1,16)),
 admin.AccountId
FROM dbo.Revenues r
CROSS APPLY(SELECT TOP(1) a.AccountId FROM dbo.UserRoleAssignments a
 WHERE a.RoleKey=N'building-admin' AND a.ScopeType=1 AND a.ScopeBuildingId=r.BuildingId
 AND a.ScopeApartmentId IS NULL AND a.RevokedAtUtc IS NULL
 AND (a.ValidToUtc IS NULL OR a.ValidToUtc>SYSUTCDATETIME())
 ORDER BY a.GrantedAtUtc DESC,a.RoleAssignmentId) admin
WHERE r.MonthKey IN(N'2026-09',N'2026-10')
GROUP BY r.BuildingId,r.MonthKey,admin.AccountId;

CREATE TABLE #Shares(
 BuildingId nvarchar(64) NOT NULL, MonthKey char(7) NOT NULL, Kind tinyint NOT NULL,
 ShareRows int NOT NULL, AllocatedAmount decimal(19,4) NOT NULL,
 PRIMARY KEY(BuildingId,MonthKey,Kind)
);
INSERT #Shares
SELECT BuildingId,MonthKey,2,COUNT(*),SUM(ExpenseShare)
FROM dbo.MonthlyApartmentShares WHERE MonthKey IN(N'2026-09',N'2026-10') GROUP BY BuildingId,MonthKey
UNION ALL
SELECT BuildingId,MonthKey,3,COUNT(*),SUM(RevenueShare)
FROM dbo.MonthlyApartmentShares WHERE MonthKey IN(N'2026-09',N'2026-10') GROUP BY BuildingId,MonthKey;

IF EXISTS(
 SELECT 1 FROM #Ops o LEFT JOIN #Shares s
 ON s.BuildingId=o.BuildingId AND s.MonthKey=o.MonthKey AND s.Kind=o.Kind
 WHERE s.BuildingId IS NULL OR o.SourceAmount=0 OR s.AllocatedAmount=0
) THROW 51044,'An expense/revenue source group has no non-zero historical apartment shares; no inferred allocation is allowed.',1;
IF EXISTS(
 SELECT 1 FROM #Shares s LEFT JOIN #Ops o
 ON o.BuildingId=s.BuildingId AND o.MonthKey=s.MonthKey AND o.Kind=s.Kind
 WHERE o.BuildingId IS NULL AND s.AllocatedAmount<>0
) THROW 51044,'Non-zero historical apartment shares have no matching expense/revenue source group.',1;
IF EXISTS(
 SELECT 1 FROM #Ops o JOIN #Shares s
 ON s.BuildingId=o.BuildingId AND s.MonthKey=o.MonthKey AND s.Kind=o.Kind
 WHERE o.SourceAmount<>s.AllocatedAmount
 AND NOT (
   o.BuildingId=(SELECT TOP(1) BuildingId FROM dbo.FinancialReconciliationDifferences d
    WHERE d.Classification=N'LegacyRoundingVariance' AND d.DifferenceType LIKE N'%Expense%' AND d.PeriodKey=o.MonthKey)
   AND o.Kind=2 AND ABS(o.SourceAmount-s.AllocatedAmount)=0.2200
 )
 AND NOT (
   o.BuildingId=(SELECT TOP(1) BuildingId FROM dbo.FinancialReconciliationDifferences d
    WHERE d.Classification=N'LegacyRoundingVariance' AND d.DifferenceType LIKE N'%Revenue%' AND d.PeriodKey=o.MonthKey)
   AND o.Kind=3 AND ABS(o.SourceAmount-s.AllocatedAmount)=0.0400
 )
) THROW 51044,'Unregistered expense/revenue allocation variance detected. Review it before posting.',1;

SELECT o.BuildingId,o.MonthKey,o.Kind,o.SourceRows,o.SourceAmount,s.ShareRows,s.AllocatedAmount,
 o.SourceAmount-s.AllocatedAmount DifferenceAmount
FROM #Ops o JOIN #Shares s ON s.BuildingId=o.BuildingId AND s.MonthKey=o.MonthKey AND s.Kind=o.Kind
ORDER BY o.BuildingId,o.MonthKey,o.Kind;

IF @Apply=1
BEGIN
 BEGIN TRY
  BEGIN TRANSACTION;
  INSERT dbo.FinancialPeriods(PeriodId,BuildingId,PeriodKey,TimeZoneId,Status,StartsAtUtc,EndsAtUtc,CreatedAtUtc)
  SELECT DISTINCT o.PeriodId,o.BuildingId,o.MonthKey,N'Africa/Cairo',0,
   CAST(CAST(DATEFROMPARTS(CONVERT(int,LEFT(o.MonthKey,4)),CONVERT(int,RIGHT(o.MonthKey,2)),1) AS datetime2(3)) AT TIME ZONE N'Egypt Standard Time' AT TIME ZONE N'UTC' AS datetime2(3)),
   CAST(CAST(DATEADD(month,1,DATEFROMPARTS(CONVERT(int,LEFT(o.MonthKey,4)),CONVERT(int,RIGHT(o.MonthKey,2)),1)) AS datetime2(3)) AT TIME ZONE N'Egypt Standard Time' AT TIME ZONE N'UTC' AS datetime2(3)),
   SYSUTCDATETIME()
  FROM #Ops o WHERE NOT EXISTS(SELECT 1 FROM dbo.FinancialPeriods p WHERE p.BuildingId=o.BuildingId AND p.PeriodKey=o.MonthKey);

  IF EXISTS(SELECT 1 FROM #Ops o JOIN dbo.FinancialPeriods p ON p.BuildingId=o.BuildingId AND p.PeriodKey=o.MonthKey
   WHERE p.PeriodId<>o.PeriodId)
   THROW 51044,'Existing financial period uses a different deterministic identifier; manual reconciliation is required.',1;

  INSERT dbo.LedgerTransactions(LedgerTransactionId,PeriodId,BuildingId,Kind,Status,IdempotencyKey,ActorAccountId,ActorReference,Reason,CreatedAtUtc,PostedAtUtc)
  SELECT o.TransactionId,o.PeriodId,o.BuildingId,o.Kind,1,
   CONCAT(N'legacy-',CASE WHEN o.Kind=2 THEN N'expense:' ELSE N'revenue:' END,o.BuildingId,N':',o.MonthKey),
   o.ActorAccountId,N'Building Admin',
   CASE WHEN o.Kind=2 THEN N'Legacy monthly expenses cutover using preserved apartment shares'
    ELSE N'Legacy monthly revenues cutover using preserved apartment shares' END,
   o.EffectiveAtUtc,o.EffectiveAtUtc
  FROM #Ops o
  WHERE NOT EXISTS(SELECT 1 FROM dbo.LedgerTransactions t WHERE t.IdempotencyKey=
   CONCAT(N'legacy-',CASE WHEN o.Kind=2 THEN N'expense:' ELSE N'revenue:' END,o.BuildingId,N':',o.MonthKey));

  IF EXISTS(SELECT 1 FROM #Ops o JOIN dbo.LedgerTransactions t ON t.IdempotencyKey=
   CONCAT(N'legacy-',CASE WHEN o.Kind=2 THEN N'expense:' ELSE N'revenue:' END,o.BuildingId,N':',o.MonthKey)
   WHERE t.LedgerTransactionId<>o.TransactionId OR t.PeriodId<>o.PeriodId OR t.BuildingId<>o.BuildingId OR t.Kind<>o.Kind OR t.Status<>1)
   THROW 51044,'An existing expense/revenue transaction conflicts with the deterministic source.',1;

  INSERT dbo.LedgerEntries(LedgerEntryId,LedgerTransactionId,PeriodId,BuildingId,ApartmentId,EntryKind,SignedAmount,CurrencyCode,Description,EffectiveAtUtc,CreatedAtUtc)
  SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.finance.share-entry.v1|',s.BuildingId,N'|',s.MonthKey,N'|',s.ApartmentId,N'|',o.Kind))),1,16)),
   o.TransactionId,o.PeriodId,s.BuildingId,s.ApartmentId,
   CASE WHEN o.Kind=2 THEN 3 ELSE 4 END,
   CASE WHEN o.Kind=2 THEN -s.ExpenseShare ELSE s.RevenueShare END,N'EGP',
   CASE WHEN o.Kind=2 THEN N'Historical expense share' ELSE N'Historical revenue share' END,
   o.EffectiveAtUtc,SYSUTCDATETIME()
  FROM #Ops o JOIN dbo.MonthlyApartmentShares s ON s.BuildingId=o.BuildingId AND s.MonthKey=o.MonthKey
  WHERE ((o.Kind=2 AND s.ExpenseShare<>0) OR (o.Kind=3 AND s.RevenueShare<>0))
  AND NOT EXISTS(
   SELECT 1 FROM dbo.LedgerEntries le WHERE le.LedgerEntryId=
    CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.finance.share-entry.v1|',s.BuildingId,N'|',s.MonthKey,N'|',s.ApartmentId,N'|',o.Kind))),1,16))
  );

  INSERT dbo.FinancialSourceLinks(FinancialSourceLinkId,SourceSystem,SourceType,SourceKey,LedgerTransactionId,SourceFingerprint)
  SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.finance.source-link.v1|',e.BuildingId,N'|',e.Id))),1,16)),
   N'BuildingManagement.Legacy',N'Expenses',CONCAT(e.BuildingId,N'|',e.Id),o.TransactionId,p.SourceFingerprint
  FROM dbo.Expenses e JOIN #Ops o ON o.BuildingId=e.BuildingId AND o.MonthKey=e.MonthKey AND o.Kind=2
  JOIN dbo.LegacyOperationalRecords p ON p.SourceSystem=N'BuildingManagement.Legacy' AND p.SourceTable=N'Expenses' AND p.SourceKey=CONCAT(e.BuildingId,N'|',e.Id)
  WHERE NOT EXISTS(SELECT 1 FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Expenses' AND l.SourceKey=CONCAT(e.BuildingId,N'|',e.Id));

  INSERT dbo.FinancialSourceLinks(FinancialSourceLinkId,SourceSystem,SourceType,SourceKey,LedgerTransactionId,SourceFingerprint)
  SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.finance.source-link.v1|',r.BuildingId,N'|',r.Id))),1,16)),
   N'BuildingManagement.Legacy',N'Revenues',CONCAT(r.BuildingId,N'|',r.Id),o.TransactionId,p.SourceFingerprint
  FROM dbo.Revenues r JOIN #Ops o ON o.BuildingId=r.BuildingId AND o.MonthKey=r.MonthKey AND o.Kind=3
  JOIN dbo.LegacyOperationalRecords p ON p.SourceSystem=N'BuildingManagement.Legacy' AND p.SourceTable=N'Revenues' AND p.SourceKey=CONCAT(r.BuildingId,N'|',r.Id)
  WHERE NOT EXISTS(SELECT 1 FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Revenues' AND l.SourceKey=CONCAT(r.BuildingId,N'|',r.Id));

  IF EXISTS(SELECT 1 FROM #Ops o WHERE
   (SELECT COUNT(*) FROM dbo.LedgerEntries le WHERE le.LedgerTransactionId=o.TransactionId AND le.EntryKind=CASE WHEN o.Kind=2 THEN 3 ELSE 4 END)
   < (SELECT COUNT(*) FROM dbo.MonthlyApartmentShares s WHERE s.BuildingId=o.BuildingId AND s.MonthKey=o.MonthKey
      AND CASE WHEN o.Kind=2 THEN s.ExpenseShare ELSE s.RevenueShare END<>0))
   THROW 51044,'Ledger allocation entry reconciliation failed.',1;

  IF EXISTS(SELECT 1 FROM dbo.Expenses e JOIN #Ops o ON o.BuildingId=e.BuildingId AND o.MonthKey=e.MonthKey AND o.Kind=2
   WHERE NOT EXISTS(SELECT 1 FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Expenses' AND l.SourceKey=CONCAT(e.BuildingId,N'|',e.Id)))
   OR EXISTS(SELECT 1 FROM dbo.Revenues r JOIN #Ops o ON o.BuildingId=r.BuildingId AND o.MonthKey=r.MonthKey AND o.Kind=3
   WHERE NOT EXISTS(SELECT 1 FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Revenues' AND l.SourceKey=CONCAT(r.BuildingId,N'|',r.Id)))
   THROW 51044,'Expense/revenue source-link reconciliation failed.',1;
  COMMIT TRANSACTION;
 END TRY
 BEGIN CATCH
  IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
  THROW;
 END CATCH;
END;

SELECT
 (SELECT COUNT(*) FROM dbo.Expenses) ExpenseSourceRows,
 (SELECT COUNT(*) FROM dbo.Revenues) RevenueSourceRows,
 (SELECT COUNT(*) FROM dbo.MonthlyApartmentShares) MonthlyShareSourceRows,
 (SELECT COUNT(*) FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Expenses' AND EXISTS(SELECT 1 FROM dbo.Expenses e WHERE e.MonthKey IN(N'2026-09',N'2026-10') AND l.SourceKey=CONCAT(e.BuildingId,N'|',e.Id))) LinkedExpenseRows,
 (SELECT COUNT(*) FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Revenues' AND EXISTS(SELECT 1 FROM dbo.Revenues r WHERE r.MonthKey IN(N'2026-09',N'2026-10') AND l.SourceKey=CONCAT(r.BuildingId,N'|',r.Id))) LinkedRevenueRows,
 (SELECT COUNT(*) FROM dbo.LedgerEntries le JOIN #Ops o ON o.TransactionId=le.LedgerTransactionId) CreatedShareLedgerEntries,
 (SELECT COUNT(*) FROM dbo.Expenses WHERE MonthKey NOT IN(N'2026-09',N'2026-10')) OutOfScopeExpenses,
 (SELECT COUNT(*) FROM dbo.Revenues WHERE MonthKey NOT IN(N'2026-09',N'2026-10')) OutOfScopeRevenues,
 (SELECT COUNT(*) FROM dbo.MonthlyApartmentShares WHERE MonthKey NOT IN(N'2026-09',N'2026-10')) OutOfScopeShareRows;
"@
    $cmd=New-Command $c $sql
    $p=$cmd.Parameters.Add('@Apply',[System.Data.SqlDbType]::Bit); $p.Value=[bool]$Apply
    $ds=[System.Data.DataSet]::new()
    $adapter=[System.Data.SqlClient.SqlDataAdapter]::new($cmd)
    [void]$adapter.Fill($ds)
    if($ds.Tables.Count -lt 2) { throw 'Phase44 did not return both source allocation profile and target verification.' }
    Log "Database: $db"
    Log "Mode: $mode"
    foreach($r in $ds.Tables[0].Rows) {
        $kind=if([int]$r.Kind -eq 2){'Expense'}else{'Revenue'}
        Log ("{0}: Building={1}; Month={2}; source rows={3}; source amount={4}; apartment share rows={5}; allocated={6}; variance={7}" -f $kind,$r.BuildingId,$r.MonthKey,$r.SourceRows,$r.SourceAmount,$r.ShareRows,$r.AllocatedAmount,$r.DifferenceAmount)
    }
    foreach($r in $ds.Tables[1].Rows) {
        Log ("All source rows: expenses={0}; revenues={1}; monthly shares={2}" -f $r.ExpenseSourceRows,$r.RevenueSourceRows,$r.MonthlyShareSourceRows)
        Log ("September/October links: expenses={0}; revenues={1}; share ledger entries={2}" -f $r.LinkedExpenseRows,$r.LinkedRevenueRows,$r.CreatedShareLedgerEntries)
        Log ("Outside posting scope (retained in immutable evidence): expenses={0}; revenues={1}; monthly shares={2}" -f $r.OutOfScopeExpenses,$r.OutOfScopeRevenues,$r.OutOfScopeShareRows)
        if($Apply -and ([int]$r.LinkedExpenseRows -ne [int]($r.ExpenseSourceRows-$r.OutOfScopeExpenses) -or [int]$r.LinkedRevenueRows -ne [int]($r.RevenueSourceRows-$r.OutOfScopeRevenues))) {
            throw 'Phase44 post-cutover source-link reconciliation failed.'
        }
    }
    Log 'Allocation policy: use original MonthlyApartmentShares only; no historical reallocation or invented apartment attribution.'
    Log 'Variance policy: preserve the registered +0.22 expense and +0.04 revenue rounding variances without balancing entries.'
    Log 'Actor policy: exactly one active building-scoped Admin per building; SuperAdmin is never selected.'
    Log 'Only September/October 2026 are posted. Other dated source rows remain preserved in Phase29 immutable evidence for a separate non-posting archive/reconciliation phase.'
    if(-not $Apply) { Log 'DRY RUN: no target rows were changed. Review the report, then rerun with -Apply.' }
    else { Log 'PHASE44 APPLY AND RECONCILIATION COMPLETED.' }
}
catch { Log "ERROR: $($_.Exception.Message)"; throw }
finally {
    if($c.State -ne 'Closed') { $c.Close() }
    $lines=@('# Phase 44 — Expense, revenue and historical apartment-share cutover','',"- Run: $(Get-Date -Format o)","- Database: $db","- Mode: $mode",'','## Results')
    $lines += $log | ForEach-Object { "- $_" }
    $lines += @('','## Guarantees',
      '- Restricted to BuildingManagement_RestoreTest; writes are protected by one SQL transaction.',
      '- Expenses and revenues are represented by building/month ledger transactions and linked to every original source row using immutable fingerprints.',
      '- Apartment wallet entries are generated only from the exact historical MonthlyApartmentShares rows; no historical reallocation is inferred.',
      '- Expense shares reduce wallet balances; revenue shares increase wallet balances.',
      '- Registered legacy rounding variances are preserved and are never assigned to apartments or balanced with invented entries.',
      '- Source rows and Phase29 evidence are never updated or deleted.',
      '- September/October 2026 are the only permitted posting periods; all other source rows remain preserved as immutable evidence and are not posted.',
      '- Deterministic IDs and idempotency keys make reruns safe.')
    Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
    Write-Host "Report: $report"
}
Push-Location $root
try {
    git add -- $PSCommandPath $report
    if((git status --short -- $PSCommandPath $report).Count -gt 0) {
        git commit --only -m "feat(finance): migrate legacy expenses revenues and apartment shares" -- $PSCommandPath $report
        git push origin development/database-redesign-enterprise
    }
} finally { Pop-Location }
