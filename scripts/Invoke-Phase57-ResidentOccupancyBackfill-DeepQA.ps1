# Resident occupancy backfill — DeepQA only
# Reconstructs current occupancy from exact, preserved legacy Residents evidence.
# Read-only by default. -Apply is restricted to BuildingManagement_DeepQA_20261120.
[CmdletBinding()]
param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$entry = dotnet user-secrets list | Where-Object { $_ -match '^ConnectionStrings:Default\s*=' }
if (-not $entry) { throw 'ConnectionStrings:Default user secret was not found.' }
$connectionString = ($entry -split '=', 2)[1].Trim()
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
if ($builder['Initial Catalog'] -ne 'BuildingManagement_RestoreTest') {
    throw 'Unexpected base catalog. This rehearsal only derives its connection from the known RestoreTest secret.'
}
$builder['Initial Catalog'] = 'BuildingManagement_DeepQA_20261120'
if ($builder['Initial Catalog'] -ne 'BuildingManagement_DeepQA_20261120') {
    throw 'Target database validation failed.'
}

$connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
try {
    $connection.Open()
    $dbCommand = $connection.CreateCommand()
    $dbCommand.CommandText = 'SELECT DB_NAME()'
    $databaseName = [string]$dbCommand.ExecuteScalar()
    if ($databaseName -ne 'BuildingManagement_DeepQA_20261120') {
        throw "Refusing to run against unexpected database: $databaseName"
    }

    $preflight = @'
WITH SourceRows AS
(
    SELECT m.PersonId, m.SourceKey,
           JSON_VALUE(l.PayloadJson, '$.BuildingId') AS BuildingId,
           JSON_VALUE(l.PayloadJson, '$.ApartmentId') AS ApartmentId,
           TRY_CONVERT(bit, JSON_VALUE(l.PayloadJson, '$.IsActive')) AS IsActive,
           TRY_CONVERT(bit, JSON_VALUE(l.PayloadJson, '$.IsDisabled')) AS IsDisabled,
           TRY_CONVERT(bit, JSON_VALUE(l.PayloadJson, '$.IsOwner')) AS IsOwner,
           TRY_CONVERT(bit, JSON_VALUE(l.PayloadJson, '$.IsPrimary')) AS IsPrimary
    FROM dbo.LegacyIdentityMap AS m
    INNER JOIN dbo.LegacyOperationalRecords AS l
        ON l.SourceSystem = N'BuildingManagement.Legacy'
       AND l.SourceTable = N'Residents'
       AND m.SourceKey = JSON_VALUE(l.PayloadJson, '$.Id')
    WHERE m.SourceSystem = N'legacy-sql'
      AND m.SourceTable = N'Residents'
      AND m.MappingStatus = 2
      AND m.PersonId IS NOT NULL
      AND m.AccountId IS NULL
)
SELECT
    (SELECT COUNT(*) FROM dbo.LegacyIdentityMap
      WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents' AND MappingStatus=2) AS MappedResidents,
    COUNT(*) AS ExactSourceMatches,
    COUNT(DISTINCT R.PersonId) AS DistinctPersons,
    SUM(CASE WHEN p.PersonId IS NOT NULL AND p.IsArchived=0 THEN 1 ELSE 0 END) AS ValidPersons,
    SUM(CASE WHEN a.Id IS NOT NULL THEN 1 ELSE 0 END) AS ExactApartmentMatches,
    COUNT(DISTINCT CONCAT(R.BuildingId, N'|', R.ApartmentId)) AS DistinctApartments,
    SUM(CASE WHEN R.IsActive=1 AND ISNULL(R.IsDisabled,0)=0 THEN 1 ELSE 0 END) AS ActiveResidents,
    SUM(CASE WHEN R.IsActive=0 OR R.IsDisabled=1 THEN 1 ELSE 0 END) AS InactiveResidents,
    SUM(CASE WHEN R.IsActive IS NULL OR R.IsDisabled IS NULL OR R.IsOwner IS NULL OR R.IsPrimary IS NULL THEN 1 ELSE 0 END) AS InvalidFlags,
    SUM(CASE WHEN R.IsOwner=1 THEN 1 ELSE 0 END) AS OwnerResidents,
    SUM(CASE WHEN R.IsPrimary=1 THEN 1 ELSE 0 END) AS PrimaryResidents,
    SUM(CASE WHEN o.PersonId IS NOT NULL THEN 1 ELSE 0 END) AS PersonsAlreadyOccupied
FROM SourceRows AS R
LEFT JOIN dbo.Persons AS p ON p.PersonId=R.PersonId
LEFT JOIN dbo.Apartments AS a ON a.BuildingId=R.BuildingId AND a.Id=R.ApartmentId
LEFT JOIN (SELECT DISTINCT PersonId FROM dbo.ApartmentOccupancies) AS o ON o.PersonId=R.PersonId;
'@

    $command = $connection.CreateCommand()
    $command.CommandText = $preflight
    $reader = $command.ExecuteReader()
    if (-not $reader.Read()) { throw 'Preflight returned no result.' }
    $result = [ordered]@{}
    for ($i=0; $i -lt $reader.FieldCount; $i++) {
        $result[$reader.GetName($i)] = if ($reader.IsDBNull($i)) { 0 } else { [int]$reader.GetValue($i) }
    }
    $reader.Close()

    foreach ($key in $result.Keys) { Write-Output "$key=$($result[$key])" }

    if ($result.MappedResidents -ne 79 -or
        $result.ExactSourceMatches -ne 79 -or
        $result.DistinctPersons -ne 79 -or
        $result.ValidPersons -ne 79 -or
        $result.ExactApartmentMatches -ne 79 -or
        $result.DistinctApartments -ne 79 -or
        $result.ActiveResidents -ne 77 -or
        $result.InactiveResidents -ne 2 -or
        $result.InvalidFlags -ne 0 -or
        $result.PersonsAlreadyOccupied -ne 0) {
        throw 'Preflight did not match the reviewed 79-row resident source profile; no changes were made.'
    }

    if (-not $Apply) {
        Write-Output 'DRY RUN COMPLETE — no database rows changed.'
        return
    }

    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    try {
        $insert = @'
SET NOCOUNT ON;
DECLARE @now datetime2(3)=SYSUTCDATETIME();

WITH SourceRows AS
(
    SELECT m.PersonId, m.SourceKey,
           JSON_VALUE(l.PayloadJson, '$.BuildingId') AS BuildingId,
           JSON_VALUE(l.PayloadJson, '$.ApartmentId') AS ApartmentId,
           TRY_CONVERT(bit, JSON_VALUE(l.PayloadJson, '$.IsActive')) AS IsActive,
           TRY_CONVERT(bit, JSON_VALUE(l.PayloadJson, '$.IsDisabled')) AS IsDisabled,
           TRY_CONVERT(bit, JSON_VALUE(l.PayloadJson, '$.IsOwner')) AS IsOwner,
           TRY_CONVERT(bit, JSON_VALUE(l.PayloadJson, '$.IsPrimary')) AS IsPrimary
    FROM dbo.LegacyIdentityMap AS m
    INNER JOIN dbo.LegacyOperationalRecords AS l
        ON l.SourceSystem = N'BuildingManagement.Legacy'
       AND l.SourceTable = N'Residents'
       AND m.SourceKey = JSON_VALUE(l.PayloadJson, '$.Id')
    INNER JOIN dbo.Persons AS p ON p.PersonId=m.PersonId AND p.IsArchived=0
    INNER JOIN dbo.Apartments AS a
        ON a.BuildingId=JSON_VALUE(l.PayloadJson, '$.BuildingId')
       AND a.Id=JSON_VALUE(l.PayloadJson, '$.ApartmentId')
    WHERE m.SourceSystem=N'legacy-sql'
      AND m.SourceTable=N'Residents'
      AND m.MappingStatus=2
      AND m.PersonId IS NOT NULL
      AND m.AccountId IS NULL
      AND TRY_CONVERT(bit, JSON_VALUE(l.PayloadJson, '$.IsActive'))=1
      AND ISNULL(TRY_CONVERT(bit, JSON_VALUE(l.PayloadJson, '$.IsDisabled')),0)=0
)
INSERT dbo.ApartmentOccupancies
(
    OccupancyId, BuildingId, ApartmentId, PersonId, OccupancyType,
    IsPrimary, ValidFromUtc, ValidToUtc, RecordedAtUtc, RecordedByAccountId, Notes
)
SELECT NEWID(), SourceRows.BuildingId, SourceRows.ApartmentId, SourceRows.PersonId,
       CASE WHEN SourceRows.IsOwner=1 THEN 1 ELSE 2 END,
       SourceRows.IsPrimary, @now, NULL, @now, NULL,
       CONCAT(N'Phase57 exact legacy snapshot; SourceSystem=BuildingManagement.Legacy; SourceTable=Residents; SourceKey=', SourceRows.SourceKey,
              N'; active resident at migration snapshot.')
FROM SourceRows
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.ApartmentOccupancies existing
    WHERE existing.PersonId=SourceRows.PersonId
      AND existing.BuildingId=SourceRows.BuildingId
      AND existing.ApartmentId=SourceRows.ApartmentId
);
IF @@ROWCOUNT<>77 THROW 51057, 'Expected exactly 77 current resident occupancies.', 1;
'@
        $command = $connection.CreateCommand()
        $command.Transaction = $transaction
        $command.CommandTimeout = 180
        $command.CommandText = $insert
        [void]$command.ExecuteNonQuery()
        $transaction.Commit()
    }
    catch {
        try { $transaction.Rollback() } catch {}
        throw
    }

    $verify = $connection.CreateCommand()
    $verify.CommandText = @'
SELECT COUNT(*) TotalOccupancies,
       COUNT(DISTINCT PersonId) DistinctPersons,
       SUM(CASE WHEN ValidToUtc IS NULL THEN 1 ELSE 0 END) OpenOccupancies,
       SUM(CASE WHEN OccupancyType=1 THEN 1 ELSE 0 END) OwnerOccupancies
FROM dbo.ApartmentOccupancies;
'@
    $reader = $verify.ExecuteReader()
    if ($reader.Read()) {
        for ($i=0; $i -lt $reader.FieldCount; $i++) {
            Write-Output "$($reader.GetName($i))=$($reader.GetValue($i))"
        }
    }
    $reader.Close()
    Write-Output 'APPLY COMPLETE — DeepQA only.'
}
finally {
    if ($connection.State -ne 'Closed') { $connection.Close() }
    $connection.Dispose()
}
