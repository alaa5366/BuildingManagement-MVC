[CmdletBinding()]
param(
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$reportDir = Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$report = Join-Path $reportDir ("Phase41-DepositReadiness-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + ".md")
$log = [System.Collections.Generic.List[string]]::new()
function Log([string]$s) { $log.Add($s); Write-Host $s }
function ReadTable($connection, [string]$sql) {
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 300
    $command.CommandText = $sql
    $table = [System.Data.DataTable]::new()
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    [void]$adapter.Fill($table)
    return ,$table
}
function Scalar($connection, [string]$sql) {
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 300
    $command.CommandText = $sql
    return $command.ExecuteScalar()
}
$connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try {
    $connection.Open()
    $database = [string](Scalar $connection "SELECT DB_NAME()")
    if ($database -ne 'BuildingManagement_RestoreTest') { throw "Target database mismatch: $database" }

    foreach ($table in @('Deposits','Apartments','Buildings','LegacyIdentityMap','UserAccounts','PaymentDeposits','FinancialPeriods','__EFMigrationsHistory')) {
        $exists = Scalar $connection "SELECT OBJECT_ID(N'dbo.$table',N'U')"
        if ($null -eq $exists -or $exists -is [DBNull]) { throw "Required table dbo.$table is missing. No data was changed." }
    }
    $requiredMigrations = @(
        '20261118213732_Phase08_FinancialLedgerFoundation',
        '20261119200000_Phase20_IdentityProfileEvidence',
        '20261119220000_Phase34_SystemReviewedIdentityEvidence'
    )
    foreach ($migration in $requiredMigrations) {
        $applied = [int](Scalar $connection "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'$migration'")
        if ($applied -ne 1) { throw "Required migration $migration is not applied. No data was changed." }
    }

    $profile = ReadTable $connection @"
;WITH D AS (
    SELECT d.BuildingId,d.Id,d.MonthKey,d.Number,d.ApartmentId,d.Amount,d.Status,
           d.CreatedAt,d.CreatedBy,d.ConfirmedAt,d.ConfirmedBy,d.CancelledAt,d.CancelledBy,d.CancelledReason,
           CONCAT(d.BuildingId,N'|',d.Id) SourceKey
    FROM dbo.Deposits d
),
ActorCheck AS (
    SELECT d.*,
      CASE WHEN d.Status=N'confirmed' AND EXISTS (
          SELECT 1 FROM dbo.LegacyIdentityMap m
          JOIN dbo.UserAccounts ua ON ua.AccountId=m.AccountId
          WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users'
            AND m.SourceKey=d.ConfirmedBy AND m.MappingStatus=1
            AND m.PersonId IS NOT NULL AND m.AccountId IS NOT NULL
      ) THEN 1 ELSE 0 END ConfirmedActorResolved,
      CASE WHEN LOWER(LTRIM(RTRIM(d.Status)))=N'cancelled' AND EXISTS (
          SELECT 1 FROM dbo.LegacyIdentityMap m
          JOIN dbo.UserAccounts ua ON ua.AccountId=m.AccountId
          WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users'
            AND m.SourceKey=d.CancelledBy AND m.MappingStatus=1
            AND m.PersonId IS NOT NULL AND m.AccountId IS NOT NULL
      ) THEN 1 ELSE 0 END CancelledActorResolved,
      CASE WHEN EXISTS (SELECT 1 FROM dbo.Apartments otherA WHERE otherA.Id=d.ApartmentId AND otherA.BuildingId<>d.BuildingId) THEN 1 ELSE 0 END ApartmentBuildingMismatch,
      CASE WHEN EXISTS (SELECT 1 FROM dbo.PaymentDeposits p WHERE p.SourceSystem=N'legacy-sql' AND p.SourceKey=d.SourceKey) THEN 1 ELSE 0 END AlreadyInTarget,
      CASE WHEN NOT EXISTS (SELECT 1 FROM dbo.FinancialPeriods fp WHERE fp.BuildingId=d.BuildingId AND fp.PeriodKey=d.MonthKey) THEN 1 ELSE 0 END MissingTargetPeriod
    FROM D d
)
SELECT
    COUNT(*) TotalDeposits,
    SUM(CASE WHEN LOWER(LTRIM(RTRIM(Status)))=N'pending' THEN 1 ELSE 0 END) PendingCount,
    SUM(CASE WHEN LOWER(LTRIM(RTRIM(Status)))=N'confirmed' THEN 1 ELSE 0 END) ConfirmedCount,
    SUM(CASE WHEN LOWER(LTRIM(RTRIM(Status)))=N'cancelled' THEN 1 ELSE 0 END) CancelledCount,
    SUM(CASE WHEN LOWER(LTRIM(RTRIM(Status))) NOT IN (N'pending',N'confirmed',N'cancelled') THEN 1 ELSE 0 END) UnknownStatusCount,
    SUM(CASE WHEN Amount<=0 THEN 1 ELSE 0 END) NonPositiveAmountCount,
    SUM(CASE WHEN Amount<>ROUND(Amount,2) THEN 1 ELSE 0 END) SubCentAmountCount,
    SUM(CASE WHEN NULLIF(LTRIM(RTRIM(MonthKey)),N'') IS NULL OR MonthKey NOT LIKE '[0-9][0-9][0-9][0-9]-[01][0-9]' OR SUBSTRING(MonthKey,6,2) NOT BETWEEN '01' AND '12' THEN 1 ELSE 0 END) InvalidMonthKeyCount,
    SUM(CASE WHEN a.Id IS NULL THEN 1 ELSE 0 END) MissingApartmentCount,
    SUM(ApartmentBuildingMismatch) ApartmentBuildingMismatchCount,
    SUM(CASE WHEN LOWER(LTRIM(RTRIM(Status)))=N'confirmed' AND (ConfirmedAt IS NULL OR NULLIF(LTRIM(RTRIM(ConfirmedBy)),N'') IS NULL) THEN 1 ELSE 0 END) ConfirmedMissingStampOrActorCount,
    SUM(CASE WHEN LOWER(LTRIM(RTRIM(Status)))=N'confirmed' AND ConfirmedActorResolved=0 THEN 1 ELSE 0 END) ConfirmedActorUnresolvedCount,
    SUM(CASE WHEN LOWER(LTRIM(RTRIM(Status)))=N'cancelled' AND (CancelledAt IS NULL OR NULLIF(LTRIM(RTRIM(CancelledBy)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(CancelledReason)),N'') IS NULL) THEN 1 ELSE 0 END) CancelledMissingStampActorOrReasonCount,
    SUM(CASE WHEN LOWER(LTRIM(RTRIM(Status)))=N'cancelled' AND CancelledActorResolved=0 THEN 1 ELSE 0 END) CancelledActorUnresolvedCount,
    SUM(CASE WHEN LOWER(LTRIM(RTRIM(Status)))=N'pending' AND (ConfirmedAt IS NOT NULL OR ConfirmedBy IS NOT NULL OR CancelledAt IS NOT NULL OR CancelledBy IS NOT NULL) THEN 1 ELSE 0 END) PendingHasResolutionMetadataCount,
    SUM(AlreadyInTarget) AlreadyInTargetCount,
    SUM(MissingTargetPeriod) MissingTargetPeriodCount
FROM ActorCheck D
LEFT JOIN dbo.Apartments a ON a.BuildingId=D.BuildingId AND a.Id=D.ApartmentId;
"@

    $summary = $profile.Rows[0]
    foreach ($column in $profile.Columns) { Log "$($column.ColumnName): $($summary[$column.ColumnName])" }

    $byScope = ReadTable $connection @"
;WITH ScopeCheck AS (
    SELECT d.BuildingId,d.MonthKey,d.Status,d.Amount,
      CASE WHEN EXISTS (
          SELECT 1 FROM dbo.LegacyIdentityMap m
          JOIN dbo.UserAccounts ua ON ua.AccountId=m.AccountId
          WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users'
            AND m.SourceKey=d.ConfirmedBy AND m.MappingStatus=1
            AND m.PersonId IS NOT NULL AND m.AccountId IS NOT NULL
      ) THEN 1 ELSE 0 END ConfirmedActorResolved,
      CASE WHEN EXISTS (
          SELECT 1 FROM dbo.LegacyIdentityMap m
          JOIN dbo.UserAccounts ua ON ua.AccountId=m.AccountId
          WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users'
            AND m.SourceKey=d.CancelledBy AND m.MappingStatus=1
            AND m.PersonId IS NOT NULL AND m.AccountId IS NOT NULL
      ) THEN 1 ELSE 0 END CancelledActorResolved
    FROM dbo.Deposits d
)
SELECT BuildingId,MonthKey,Status,COUNT(*) DepositCount,
       SUM(Amount) AmountTotal,
       SUM(CASE WHEN LOWER(LTRIM(RTRIM(Status)))=N'confirmed' AND ConfirmedActorResolved=0 THEN 1 ELSE 0 END) ConfirmedActorUnresolved,
       SUM(CASE WHEN LOWER(LTRIM(RTRIM(Status)))=N'cancelled' AND CancelledActorResolved=0 THEN 1 ELSE 0 END) CancelledActorUnresolved
FROM ScopeCheck
GROUP BY BuildingId,MonthKey,Status
ORDER BY BuildingId,MonthKey,Status;
"@
    Log "Scope groups: $($byScope.Rows.Count)"
    foreach ($row in $byScope.Rows) {
        Log ("Scope: Building={0}; Month={1}; Status={2}; Count={3}; Amount={4}; ConfirmedActorUnresolved={5}; CancelledActorUnresolved={6}" -f $row.BuildingId,$row.MonthKey,$row.Status,$row.DepositCount,$row.AmountTotal,$row.ConfirmedActorUnresolved,$row.CancelledActorUnresolved)
    }

    $periodProfile = ReadTable $connection @"
SELECT b.Id BuildingId, b.BuildingNumber,
       COUNT(fp.PeriodId) ExistingPeriodCount,
       SUM(CASE WHEN fp.PeriodId IS NOT NULL AND NULLIF(LTRIM(RTRIM(fp.TimeZoneId)),N'') IS NULL THEN 1 ELSE 0 END) PeriodsMissingTimeZone
FROM dbo.Buildings b
LEFT JOIN dbo.FinancialPeriods fp ON fp.BuildingId=b.Id
GROUP BY b.Id,b.BuildingNumber
ORDER BY b.BuildingNumber;
"@
    foreach ($row in $periodProfile.Rows) {
        Log ("PeriodProfile: Building={0}; Number={1}; ExistingPeriods={2}; PeriodsMissingTimeZone={3}" -f $row.BuildingId,$row.BuildingNumber,$row.ExistingPeriodCount,$row.PeriodsMissingTimeZone)
    }

    Log 'READ ONLY: no INSERT, UPDATE, DELETE, ledger posting, period creation, or wallet balance mutation was performed.'
    Log 'Actor resolution rule: exact legacy Users UID only, with reviewed MappingStatus=1 and a real UserAccounts row. Display-name/email/fuzzy matching is deliberately excluded.'
    Log 'Timestamp note: legacy DepositEntity timestamps are SQL DateTime/DateTime? values, not free-text fields. Their original timezone semantics are not established by this scan.'
    Log 'Period note: a present FinancialPeriod is not sufficient by itself; its timezone and UTC boundaries still require business-owner validation.'

    $log | Set-Content -LiteralPath $report -Encoding UTF8
    @(
        '# Phase 41 — Deposit readiness audit',
        '',
        "- Run: $(Get-Date -Format o)",
        "- Database: $database",
        '- Mode: READ ONLY',
        '',
        '## Aggregate findings',
        ($profile | Out-String).Trim(),
        '',
        '## Scope groups',
        ($byScope | Format-Table -AutoSize | Out-String).Trim(),
        '',
        '## Building period profile',
        ($periodProfile | Format-Table -AutoSize | Out-String).Trim(),
        '',
        '## Interpretation',
        '- Exact UID identity mappings only; no display-name, email, or fuzzy actor resolution.',
        '- Source DateTime fields are inspected for null/presence, but timezone meaning is not inferred.',
        '- Missing periods, unknown statuses, unresolved actors, invalid apartment scope, and malformed amounts remain blocked.',
        '- No source row, target row, status, period, ledger entry, or wallet balance is changed.',
        ''
    ) | Add-Content -LiteralPath $report -Encoding UTF8
    Write-Host "Report: $report"
} catch {
    Log "ERROR: $($_.Exception.Message)"
    throw
} finally {
    if ($connection.State -ne 'Closed') { $connection.Close() }
}
