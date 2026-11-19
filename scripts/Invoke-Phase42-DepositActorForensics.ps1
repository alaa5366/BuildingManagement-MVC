[CmdletBinding()]
param(
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$reportDir = Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$report = Join-Path $reportDir ("Phase42-DepositActorForensics-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + ".md")
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
    foreach ($table in @('Deposits','Users','LegacyIdentityMap','UserAccounts','DepositEditHistory')) {
        $exists = Scalar $connection "SELECT OBJECT_ID(N'dbo.$table',N'U')"
        if ($null -eq $exists -or $exists -is [DBNull]) { throw "Required table dbo.$table is missing. No data was changed." }
    }

    $actors = ReadTable $connection @"
;WITH ActorValues AS (
    SELECT d.BuildingId,d.Id DepositId,N'ConfirmedBy' ActorField,
           NULLIF(LTRIM(RTRIM(d.ConfirmedBy)),N'') ActorValue
    FROM dbo.Deposits d WHERE NULLIF(LTRIM(RTRIM(d.ConfirmedBy)),N'') IS NOT NULL
    UNION ALL
    SELECT d.BuildingId,d.Id,N'CancelledBy',NULLIF(LTRIM(RTRIM(d.CancelledBy)),N'')
    FROM dbo.Deposits d WHERE NULLIF(LTRIM(RTRIM(d.CancelledBy)),N'') IS NOT NULL
    UNION ALL
    SELECT d.BuildingId,d.Id,N'CreatedBy',NULLIF(LTRIM(RTRIM(d.CreatedBy)),N'')
    FROM dbo.Deposits d WHERE NULLIF(LTRIM(RTRIM(d.CreatedBy)),N'') IS NOT NULL
    UNION ALL
    SELECT d.BuildingId,d.Id,N'UpdatedBy',NULLIF(LTRIM(RTRIM(d.UpdatedBy)),N'')
    FROM dbo.Deposits d WHERE NULLIF(LTRIM(RTRIM(d.UpdatedBy)),N'') IS NOT NULL
    UNION ALL
    SELECT h.BuildingId,h.DepositId,N'EditHistory.By',NULLIF(LTRIM(RTRIM(h.Actor)),N'')
    FROM dbo.DepositEditHistory h WHERE NULLIF(LTRIM(RTRIM(h.Actor)),N'') IS NOT NULL
),
DistinctActors AS (
    SELECT ActorField,ActorValue,COUNT(*) Occurrences,
           COUNT(DISTINCT BuildingId) BuildingCount,
           MIN(BuildingId) OneBuildingId
    FROM ActorValues
    GROUP BY ActorField,ActorValue
)
SELECT a.ActorField,a.ActorValue,a.Occurrences,a.BuildingCount,
       (SELECT COUNT(*) FROM dbo.Users u WHERE u.Uid=a.ActorValue) ExactUidMatches,
       (SELECT COUNT(*) FROM dbo.Users u WHERE u.Name=a.ActorValue) ExactNameMatches,
       (SELECT COUNT(*) FROM dbo.Users u WHERE u.Email=a.ActorValue) ExactEmailMatches,
       (SELECT COUNT(*) FROM dbo.Users u WHERE LTRIM(RTRIM(u.Name))=a.ActorValue) TrimmedNameMatches,
       (SELECT COUNT(*) FROM dbo.Users u WHERE LTRIM(RTRIM(u.Email))=a.ActorValue) TrimmedEmailMatches,
       (SELECT COUNT(*) FROM dbo.LegacyIdentityMap m
          JOIN dbo.UserAccounts ua ON ua.AccountId=m.AccountId
        WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users'
          AND m.SourceKey=a.ActorValue AND m.MappingStatus=1
          AND m.PersonId IS NOT NULL AND m.AccountId IS NOT NULL) ExactReviewedUidAccountMatches,
       STUFF((
          SELECT N', '+u.Uid
          FROM dbo.Users u
          WHERE u.Uid=a.ActorValue OR u.Name=a.ActorValue OR u.Email=a.ActorValue
             OR LTRIM(RTRIM(u.Name))=a.ActorValue OR LTRIM(RTRIM(u.Email))=a.ActorValue
          ORDER BY u.Uid
          FOR XML PATH(''),TYPE
       ).value('.','nvarchar(max)'),1,2,N'') CandidateUserUids
FROM DistinctActors a
ORDER BY a.ActorField,a.ActorValue;
"@

    Log "Distinct actor values: $($actors.Rows.Count)"
    foreach ($row in $actors.Rows) {
        Log ("Actor: Field={0}; Value={1}; Occurrences={2}; Buildings={3}; ExactUid={4}; ExactName={5}; ExactEmail={6}; TrimmedName={7}; TrimmedEmail={8}; ReviewedUidAccount={9}; CandidateUids={10}" -f $row.ActorField,$row.ActorValue,$row.Occurrences,$row.BuildingCount,$row.ExactUidMatches,$row.ExactNameMatches,$row.ExactEmailMatches,$row.TrimmedNameMatches,$row.TrimmedEmailMatches,$row.ExactReviewedUidAccountMatches,$row.CandidateUserUids)
    }

    $users = ReadTable $connection @"
SELECT u.Uid,u.Name,u.Email,u.Role,
       CASE WHEN EXISTS (
          SELECT 1 FROM dbo.LegacyIdentityMap m
          JOIN dbo.UserAccounts ua ON ua.AccountId=m.AccountId
          WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users'
            AND m.SourceKey=u.Uid AND m.MappingStatus=1
            AND m.PersonId IS NOT NULL AND m.AccountId IS NOT NULL
       ) THEN 1 ELSE 0 END HasReviewedAccount
FROM dbo.Users u
ORDER BY u.Uid;
"@
    Log "Legacy administrative users profiled: $($users.Rows.Count)"
    foreach ($row in $users.Rows) {
        Log ("User: Uid={0}; Name={1}; Role={2}; HasReviewedAccount={3}" -f $row.Uid,$row.Name,$row.Role,$row.HasReviewedAccount)
    }

    Log 'READ ONLY: no identity map, user, account, credential, deposit, or ledger data was changed.'
    Log 'Exact UID matches backed by a reviewed LegacyIdentityMap and UserAccounts row are the only directly resolvable account identity.'
    Log 'Name/email matches are investigative candidates only. They are not promoted to identity mappings and do not authorize financial posting.'
    Log 'Raw actor strings are preserved in this local report for exact comparison; no normalization or fuzzy matching is used to assign an account.'

    $lines = @(
        '# Phase 42 — Deposit actor forensics',
        '',
        "- Run: $(Get-Date -Format o)",
        "- Database: $database",
        '- Mode: READ ONLY',
        '',
        '## Actor values and exact candidate matches',
        ($actors | Format-Table -AutoSize -Wrap | Out-String).Trim(),
        '',
        '## Administrative user identity inventory',
        ($users | Format-Table -AutoSize -Wrap | Out-String).Trim(),
        '',
        '## Safeguards',
        '- No mapping or account was created or modified.',
        '- Exact UID plus reviewed identity mapping and existing UserAccounts row is the only accepted account-resolution path.',
        '- Exact name/email matches are diagnostic hints only; they are not proof of who performed a historical action.',
        '- No deposit state, financial period, ledger transaction, ledger entry, or wallet balance was changed.',
        ''
    )
    Set-Content -LiteralPath $report -Value $lines -Encoding UTF8
    Write-Host "Report: $report"
}
catch {
    Log "ERROR: $($_.Exception.Message)"
    throw
}
finally {
    if ($connection.State -ne 'Closed') { $connection.Close() }
}