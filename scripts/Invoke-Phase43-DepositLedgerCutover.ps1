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
$report = Join-Path $reportDir "Phase43-DepositLedgerCutover-$stamp.md"
$log = [System.Collections.Generic.List[string]]::new()
function Log([string]$s) { $log.Add($s); Write-Host $s }
function New-Command($c,[string]$sql) {
    $cmd=$c.CreateCommand(); $cmd.CommandTimeout=300; $cmd.CommandText=$sql; return $cmd
}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
$mode=if($Apply){'APPLY'}else{'DRY RUN'}
try {
    $c.Open()
    $db=[string](New-Command $c 'SELECT DB_NAME();').ExecuteScalar()
    if($db -ne 'BuildingManagement_RestoreTest') { throw "Unsafe target '$db'. This phase is restricted to BuildingManagement_RestoreTest." }

    $sql=@"
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.Deposits',N'U') IS NULL OR OBJECT_ID(N'dbo.Apartments',N'U') IS NULL
 OR OBJECT_ID(N'dbo.Buildings',N'U') IS NULL OR OBJECT_ID(N'dbo.UserRoleAssignments',N'U') IS NULL
 OR OBJECT_ID(N'dbo.UserAccounts',N'U') IS NULL OR OBJECT_ID(N'dbo.FinancialPeriods',N'U') IS NULL
 OR OBJECT_ID(N'dbo.PaymentDeposits',N'U') IS NULL OR OBJECT_ID(N'dbo.LedgerTransactions',N'U') IS NULL
 OR OBJECT_ID(N'dbo.LedgerEntries',N'U') IS NULL OR OBJECT_ID(N'dbo.LegacyOperationalRecords',N'U') IS NULL
 OR OBJECT_ID(N'dbo.FinancialSourceLinks',N'U') IS NULL
 THROW 51043,'Required legacy, identity, evidence, or finance target table is missing.',1;

IF EXISTS(SELECT 1 FROM dbo.Deposits WHERE MonthKey NOT IN(N'2026-09',N'2026-10'))
 THROW 51043,'A deposit is outside the owner-approved September/October 2026 scope.',1;
IF EXISTS(SELECT 1 FROM dbo.Deposits WHERE LOWER(LTRIM(RTRIM(Status))) NOT IN(N'confirmed',N'cancelled'))
 THROW 51043,'Pending or unknown deposit status requires a separate disposition; no row was changed.',1;
IF EXISTS(SELECT 1 FROM dbo.Deposits WHERE Amount<=0 OR Amount<>ROUND(Amount,2))
 THROW 51043,'Invalid deposit amount detected.',1;
IF EXISTS(SELECT 1 FROM dbo.Deposits d LEFT JOIN dbo.Apartments a
 ON a.BuildingId=d.BuildingId AND a.Id=d.ApartmentId WHERE a.Id IS NULL)
 THROW 51043,'A deposit apartment/building reference is invalid.',1;
IF EXISTS(SELECT 1 FROM dbo.Deposits WHERE LOWER(LTRIM(RTRIM(Status)))=N'confirmed' AND ConfirmedAt IS NULL)
 THROW 51043,'A confirmed deposit has no source confirmation timestamp.',1;
IF EXISTS(SELECT 1 FROM dbo.Deposits WHERE LOWER(LTRIM(RTRIM(Status)))=N'cancelled' AND COALESCE(CancelledAt,CreatedAt) IS NULL)
 THROW 51043,'A cancelled deposit has neither cancellation nor submission timestamp.',1;

IF EXISTS(
 SELECT d.BuildingId
 FROM dbo.Deposits d
 CROSS APPLY (SELECT COUNT(*) Cnt FROM dbo.UserRoleAssignments a
   WHERE a.RoleKey=N'building-admin' AND a.ScopeType=1
    AND a.ScopeBuildingId=d.BuildingId AND a.ScopeApartmentId IS NULL
    AND a.RevokedAtUtc IS NULL AND (a.ValidToUtc IS NULL OR a.ValidToUtc>SYSUTCDATETIME())) x
 GROUP BY d.BuildingId HAVING MAX(x.Cnt)<>1
) THROW 51043,'Every deposit building must resolve to exactly one active building-admin account.',1;

IF EXISTS(
 SELECT d.BuildingId,d.Id
 FROM dbo.Deposits d
 LEFT JOIN dbo.LegacyOperationalRecords e ON e.SourceSystem=N'BuildingManagement.Legacy'
  AND e.SourceTable=N'Deposits' AND e.SourceKey=CONCAT(d.BuildingId,N'|',d.Id)
 WHERE e.LegacyOperationalRecordId IS NULL OR DATALENGTH(e.SourceFingerprint)<>32
) THROW 51043,'Phase29 immutable deposit evidence/fingerprint is missing.',1;

IF EXISTS(
 SELECT SourceSystem,SourceType,SourceKey FROM dbo.FinancialSourceLinks
 GROUP BY SourceSystem,SourceType,SourceKey HAVING COUNT(*)>1
) THROW 51043,'Duplicate financial source links detected.',1;

CREATE TABLE #S(
 SourceKey nvarchar(256) NOT NULL PRIMARY KEY,
 BuildingId nvarchar(64) NOT NULL,
 ApartmentId nvarchar(64) NOT NULL,
 MonthKey char(7) NOT NULL,
 Amount decimal(19,4) NOT NULL,
 LegacyStatus nvarchar(30) NOT NULL,
 SubmittedAtUtc datetime2(3) NOT NULL,
 ConfirmedAtUtc datetime2(3) NULL,
 CancelledAtUtc datetime2(3) NULL,
 ActorAccountId uniqueidentifier NOT NULL,
 PeriodId uniqueidentifier NOT NULL,
 PaymentDepositId uniqueidentifier NOT NULL,
 LedgerTransactionId uniqueidentifier NULL,
 SourceFingerprint varbinary(32) NOT NULL
);
INSERT #S(SourceKey,BuildingId,ApartmentId,MonthKey,Amount,LegacyStatus,SubmittedAtUtc,ConfirmedAtUtc,CancelledAtUtc,ActorAccountId,PeriodId,PaymentDepositId,LedgerTransactionId,SourceFingerprint)
SELECT
 CONCAT(d.BuildingId,N'|',d.Id),d.BuildingId,d.ApartmentId,d.MonthKey,d.Amount,
 LOWER(LTRIM(RTRIM(d.Status))),
 CAST(CAST(d.CreatedAt AS datetime2(3)) AT TIME ZONE N'Egypt Standard Time' AT TIME ZONE N'UTC' AS datetime2(3)),
 CASE WHEN LOWER(LTRIM(RTRIM(d.Status)))=N'confirmed'
  THEN CAST(CAST(d.ConfirmedAt AS datetime2(3)) AT TIME ZONE N'Egypt Standard Time' AT TIME ZONE N'UTC' AS datetime2(3)) END,
 CASE WHEN LOWER(LTRIM(RTRIM(d.Status)))=N'cancelled'
  THEN CAST(CAST(COALESCE(d.CancelledAt,d.CreatedAt) AS datetime2(3)) AT TIME ZONE N'Egypt Standard Time' AT TIME ZONE N'UTC' AS datetime2(3)) END,
 admin.AccountId,
 CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.finance.period.v1|',d.BuildingId,N'|',d.MonthKey))),1,16)),
 CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.finance.payment-deposit.v1|',d.BuildingId,N'|',d.Id))),1,16)),
 CASE WHEN LOWER(LTRIM(RTRIM(d.Status)))=N'confirmed'
  THEN CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.finance.deposit-ledger.v1|',d.BuildingId,N'|',d.Id))),1,16)) END,
 e.SourceFingerprint
FROM dbo.Deposits d
JOIN dbo.LegacyOperationalRecords e ON e.SourceSystem=N'BuildingManagement.Legacy'
 AND e.SourceTable=N'Deposits' AND e.SourceKey=CONCAT(d.BuildingId,N'|',d.Id)
CROSS APPLY(
 SELECT TOP(1) a.AccountId
 FROM dbo.UserRoleAssignments a
 WHERE a.RoleKey=N'building-admin' AND a.ScopeType=1
  AND a.ScopeBuildingId=d.BuildingId AND a.ScopeApartmentId IS NULL
  AND a.RevokedAtUtc IS NULL AND (a.ValidToUtc IS NULL OR a.ValidToUtc>SYSUTCDATETIME())
 ORDER BY a.GrantedAtUtc DESC,a.RoleAssignmentId
) admin;

IF (SELECT COUNT(*) FROM #S)<>(SELECT COUNT(*) FROM dbo.Deposits)
 THROW 51043,'Not every legacy deposit was included in the cutover set.',1;
IF EXISTS(SELECT 1 FROM #S WHERE SubmittedAtUtc IS NULL OR (LegacyStatus=N'confirmed' AND ConfirmedAtUtc IS NULL)
 OR (LegacyStatus=N'cancelled' AND CancelledAtUtc IS NULL))
 THROW 51043,'A required timestamp could not be normalized using Egypt local time.',1;

SELECT N'Deposits' SourceType,COUNT(*) SourceRows,
 SUM(CASE WHEN LegacyStatus=N'confirmed' THEN 1 ELSE 0 END) ConfirmedRows,
 SUM(CASE WHEN LegacyStatus=N'cancelled' THEN 1 ELSE 0 END) CancelledRows,
 SUM(CASE WHEN LegacyStatus=N'confirmed' THEN Amount ELSE 0 END) ConfirmedAmount,
 SUM(CASE WHEN LegacyStatus=N'cancelled' THEN Amount ELSE 0 END) CancelledAmount,
 COUNT(DISTINCT BuildingId) Buildings,COUNT(DISTINCT MonthKey) Months
FROM #S;

IF @Apply=1
BEGIN
 BEGIN TRY
  BEGIN TRANSACTION;
  INSERT dbo.FinancialPeriods(PeriodId,BuildingId,PeriodKey,TimeZoneId,Status,StartsAtUtc,EndsAtUtc,CreatedAtUtc)
  SELECT DISTINCT s.PeriodId,s.BuildingId,s.MonthKey,N'Africa/Cairo',0,
   CAST(CAST(DATEFROMPARTS(CONVERT(int,LEFT(s.MonthKey,4)),CONVERT(int,RIGHT(s.MonthKey,2)),1) AS datetime2(3)) AT TIME ZONE N'Egypt Standard Time' AT TIME ZONE N'UTC' AS datetime2(3)),
   CAST(CAST(DATEADD(month,1,DATEFROMPARTS(CONVERT(int,LEFT(s.MonthKey,4)),CONVERT(int,RIGHT(s.MonthKey,2)),1)) AS datetime2(3)) AT TIME ZONE N'Egypt Standard Time' AT TIME ZONE N'UTC' AS datetime2(3)),
   SYSUTCDATETIME()
  FROM #S s
  WHERE NOT EXISTS(SELECT 1 FROM dbo.FinancialPeriods p WHERE p.BuildingId=s.BuildingId AND p.PeriodKey=s.MonthKey);

  INSERT dbo.LedgerTransactions(LedgerTransactionId,PeriodId,BuildingId,Kind,Status,IdempotencyKey,SourceSystem,SourceType,SourceKey,ActorAccountId,ActorReference,Reason,CreatedAtUtc,PostedAtUtc)
  SELECT s.LedgerTransactionId,s.PeriodId,s.BuildingId,1,1,
   CONCAT(N'legacy-deposit:',s.SourceKey),N'BuildingManagement.Legacy',N'Deposits',s.SourceKey,
   s.ActorAccountId,N'Building Admin',N'Legacy confirmed deposit migration',s.ConfirmedAtUtc,s.ConfirmedAtUtc
  FROM #S s
  WHERE s.LegacyStatus=N'confirmed'
   AND NOT EXISTS(SELECT 1 FROM dbo.LedgerTransactions t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceType=N'Deposits' AND t.SourceKey=s.SourceKey);

  INSERT dbo.LedgerEntries(LedgerEntryId,LedgerTransactionId,PeriodId,BuildingId,ApartmentId,EntryKind,SignedAmount,CurrencyCode,Description,EffectiveAtUtc,CreatedAtUtc)
  SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.finance.deposit-entry.v1|',s.SourceKey))),1,16)),
   s.LedgerTransactionId,s.PeriodId,s.BuildingId,s.ApartmentId,1,s.Amount,N'EGP',
   N'Legacy confirmed deposit '+s.SourceKey,s.ConfirmedAtUtc,SYSUTCDATETIME()
  FROM #S s
  WHERE s.LegacyStatus=N'confirmed'
   AND NOT EXISTS(SELECT 1 FROM dbo.LedgerEntries le WHERE le.LedgerTransactionId=s.LedgerTransactionId AND le.EntryKind=1);

  INSERT dbo.FinancialSourceLinks(FinancialSourceLinkId,SourceSystem,SourceType,SourceKey,LedgerTransactionId,SourceFingerprint)
  SELECT CONVERT(uniqueidentifier,SUBSTRING(HASHBYTES('SHA2_256',CONVERT(varbinary(max),CONCAT(N'legacy.finance.source-link.v1|',s.SourceKey))),1,16)),
   N'BuildingManagement.Legacy',N'Deposits',s.SourceKey,s.LedgerTransactionId,s.SourceFingerprint
  FROM #S s
  WHERE s.LegacyStatus=N'confirmed'
   AND NOT EXISTS(SELECT 1 FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Deposits' AND l.SourceKey=s.SourceKey);

  INSERT dbo.PaymentDeposits(PaymentDepositId,PeriodId,BuildingId,ApartmentId,PostedTransactionId,Amount,CurrencyCode,Status,SubmittedAtUtc,ConfirmedAtUtc,ConfirmedByAccountId,CancelledAtUtc,CancelledByAccountId,ResolutionReason,SourceSystem,SourceKey)
  SELECT s.PaymentDepositId,s.PeriodId,s.BuildingId,s.ApartmentId,s.LedgerTransactionId,s.Amount,N'EGP',
   CASE WHEN s.LegacyStatus=N'confirmed' THEN 1 ELSE 2 END,s.SubmittedAtUtc,s.ConfirmedAtUtc,
   CASE WHEN s.LegacyStatus=N'confirmed' THEN s.ActorAccountId END,
   s.CancelledAtUtc,CASE WHEN s.LegacyStatus=N'cancelled' THEN s.ActorAccountId END,
   CASE WHEN s.LegacyStatus=N'cancelled' THEN N'إلغاء للتجربة الأولية للنظام' END,
   N'BuildingManagement.Legacy',s.SourceKey
  FROM #S s
  WHERE NOT EXISTS(SELECT 1 FROM dbo.PaymentDeposits p WHERE p.SourceSystem=N'BuildingManagement.Legacy' AND p.SourceKey=s.SourceKey);

  IF (SELECT COUNT(*) FROM dbo.PaymentDeposits p JOIN #S s ON p.SourceSystem=N'BuildingManagement.Legacy' AND p.SourceKey=s.SourceKey)<>(SELECT COUNT(*) FROM #S)
   THROW 51043,'Payment deposit row-count reconciliation failed.',1;
  IF EXISTS(SELECT 1 FROM #S s WHERE s.LegacyStatus=N'confirmed' AND
    (NOT EXISTS(SELECT 1 FROM dbo.LedgerTransactions t WHERE t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceType=N'Deposits' AND t.SourceKey=s.SourceKey AND t.Status=1)
     OR NOT EXISTS(SELECT 1 FROM dbo.LedgerEntries le WHERE le.LedgerTransactionId=s.LedgerTransactionId AND le.EntryKind=1 AND le.SignedAmount=s.Amount)
     OR NOT EXISTS(SELECT 1 FROM dbo.FinancialSourceLinks l WHERE l.SourceSystem=N'BuildingManagement.Legacy' AND l.SourceType=N'Deposits' AND l.SourceKey=s.SourceKey)))
   THROW 51043,'Confirmed deposit ledger/provenance verification failed.',1;
  IF EXISTS(SELECT 1 FROM #S s JOIN dbo.PaymentDeposits p ON p.SourceSystem=N'BuildingManagement.Legacy' AND p.SourceKey=s.SourceKey
    WHERE p.BuildingId<>s.BuildingId OR p.ApartmentId<>s.ApartmentId OR p.Amount<>s.Amount
     OR p.Status<>CASE WHEN s.LegacyStatus=N'confirmed' THEN 1 ELSE 2 END)
   THROW 51043,'Target deposit does not match its legacy source row.',1;
  COMMIT TRANSACTION;
 END TRY
 BEGIN CATCH
  IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
  THROW;
 END CATCH;
END;

SELECT
 (SELECT COUNT(*) FROM #S) SourceRows,
 (SELECT COUNT(*) FROM dbo.PaymentDeposits p JOIN #S s ON p.SourceSystem=N'BuildingManagement.Legacy' AND p.SourceKey=s.SourceKey) TargetDeposits,
 (SELECT COUNT(*) FROM dbo.LedgerTransactions t JOIN #S s ON t.SourceSystem=N'BuildingManagement.Legacy' AND t.SourceType=N'Deposits' AND t.SourceKey=s.SourceKey) ConfirmedLedgerTransactions,
 (SELECT COUNT(*) FROM dbo.LedgerEntries le JOIN #S s ON le.LedgerTransactionId=s.LedgerTransactionId AND s.LegacyStatus=N'confirmed') ConfirmedLedgerEntries,
 (SELECT COUNT(*) FROM dbo.FinancialPeriods p JOIN (SELECT DISTINCT BuildingId,MonthKey FROM #S) s ON p.BuildingId=s.BuildingId AND p.PeriodKey=s.MonthKey) TargetPeriods;
"@
    $cmd=New-Command $c $sql
    $p=$cmd.Parameters.Add('@Apply',[System.Data.SqlDbType]::Bit); $p.Value=[bool]$Apply
    $ds=[System.Data.DataSet]::new()
    $adapter=[System.Data.SqlClient.SqlDataAdapter]::new($cmd)
    [void]$adapter.Fill($ds)
    if($ds.Tables.Count -lt 2) { throw 'Phase43 did not return both source profile and target verification.' }
    Log "Database: $db"
    Log "Mode: $mode"
    foreach($r in $ds.Tables[0].Rows) {
        Log ("Source deposits={0}; confirmed={1}; cancelled={2}; confirmed amount={3}; cancelled amount={4}; buildings={5}; months={6}" -f $r.SourceRows,$r.ConfirmedRows,$r.CancelledRows,$r.ConfirmedAmount,$r.CancelledAmount,$r.Buildings,$r.Months)
    }
    foreach($r in $ds.Tables[1].Rows) {
        Log ("Target deposits={0}; confirmed ledger transactions={1}; confirmed ledger entries={2}; target periods={3}" -f $r.TargetDeposits,$r.ConfirmedLedgerTransactions,$r.ConfirmedLedgerEntries,$r.TargetPeriods)
        if($Apply -and ([int]$r.SourceRows -ne [int]$r.TargetDeposits -or [int]$r.ConfirmedLedgerTransactions -ne [int]$r.ConfirmedLedgerEntries)) {
            throw 'Phase43 post-commit verification failed.'
        }
    }
    Log 'Admin actor policy: exactly one active building-scoped Admin account per building; SuperAdmin is never selected.'
    Log 'Cancellation disposition: owner-approved initial system testing reason; cancelled deposits create no ledger movement.'
    Log 'Timestamp policy: legacy SQL DateTime values are interpreted as Egypt local time and converted to UTC; source values remain immutable.'
    if(-not $Apply) { Log 'DRY RUN: no target rows were changed. Re-run with -Apply to execute the transactional cutover.' }
    else { Log 'PHASE43 APPLY AND RECONCILIATION COMPLETED.' }
}
catch { Log "ERROR: $($_.Exception.Message)"; throw }
finally {
    if($c.State -ne 'Closed') { $c.Close() }
    $lines=@('# Phase 43 — Deposit and ledger cutover','',"- Run: $(Get-Date -Format o)","- Database: $($db)","- Mode: $mode",'','## Results')
    $lines += $log | ForEach-Object { "- $_" }
    $lines += @('','## Guarantees',
      '- Restricted to BuildingManagement_RestoreTest; a single SQL transaction protects all writes.',
      '- Every legacy deposit is represented in PaymentDeposits with the same source key, building, apartment, amount, and state.',
      '- Confirmed deposits create one posted deposit transaction, one positive apartment ledger entry, and one source fingerprint link.',
      '- Cancelled deposits remain cancelled, have no ledger posting, and carry the owner-approved initial-testing disposition.',
      '- One active building-scoped Admin account is used for each building; no SuperAdmin account is selected.',
      '- Source Deposits, Phase29 evidence payloads, hashes, and historical edit rows are never updated or deleted.',
      '- Periods are limited to source MonthKey values in September/October 2026 and use Africa/Cairo boundaries.',
      '- Deterministic identifiers and source uniqueness make reruns idempotent.')
    Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
    Write-Host "Report: $report"
}
Push-Location $root
try {
    git add -- $PSCommandPath $report
    if((git status --short -- $PSCommandPath $report).Count -gt 0) {
        git commit --only -m "feat(finance): cut over legacy deposits to ledger" -- $PSCommandPath $report
        git push origin development/database-redesign-enterprise
    }
} finally { Pop-Location }
