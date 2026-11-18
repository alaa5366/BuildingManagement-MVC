[CmdletBinding()]
param(
    [string]$RepoPath = "D:\Projects\BuildingManagement-MVC",
    [string]$SqlInstance = "(localdb)\MSSQLLocalDB",
    [string]$DatabaseName = "BuildingManagement_RestoreTest"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ExpectedBranch = "development/database-redesign-enterprise"
$RelativeOutputRoot = "Database\Diagnostics\Redesign-Schema-Parity-Results"
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$RelativeOutputPath = Join-Path $RelativeOutputRoot $Timestamp
$OutputPath = Join-Path $RepoPath $RelativeOutputPath
$ErrorsPath = Join-Path $OutputPath "Query-Errors.txt"

function Invoke-Git {
    param([string[]]$Arguments)
    $result = & git -C $RepoPath @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        $details = $result -join [Environment]::NewLine
        throw ("Git command failed: git " + ($Arguments -join " ") + [Environment]::NewLine + $details)
    }
    return $result
}

function Invoke-ReadOnlyQuery {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql
    )
    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = 120
    $reader = $null
    $table = New-Object System.Data.DataTable
    try {
        $reader = $command.ExecuteReader()
        $table.Load($reader)
        return ,$table
    }
    finally {
        if ($reader) { $reader.Dispose() }
        $command.Dispose()
    }
}

function Export-DataTableCsv {
    param(
        [System.Data.DataTable]$Table,
        [string]$Path
    )
    $records = @(
        foreach ($row in $Table.Rows) {
            $values = [ordered]@{}
            foreach ($column in $Table.Columns) {
                $value = $row[$column.ColumnName]
                if ($value -is [System.DBNull]) { $value = $null }
                $values[$column.ColumnName] = $value
            }
            [pscustomobject]$values
        }
    )

    if ($records.Count -gt 0) {
        $records | Export-Csv -LiteralPath $Path -NoTypeInformation -Encoding UTF8
    }
    else {
        $headers = @($Table.Columns | ForEach-Object {
            '"' + ($_.ColumnName -replace '"', '""') + '"'
        })
        Set-Content -LiteralPath $Path -Value ($headers -join ',') -Encoding UTF8
    }
}

if (-not (Test-Path -LiteralPath $RepoPath -PathType Container)) {
    throw "Repository path not found: $RepoPath"
}
$topLevel = (Invoke-Git -Arguments @("rev-parse", "--show-toplevel") | Select-Object -First 1).Trim()
if ([System.IO.Path]::GetFullPath($topLevel) -ne [System.IO.Path]::GetFullPath($RepoPath)) {
    throw "RepoPath does not match the Git working tree root. Actual root: $topLevel"
}
$currentBranch = (Invoke-Git -Arguments @("branch", "--show-current") | Select-Object -First 1).Trim()
if ($currentBranch -ne $ExpectedBranch) {
    throw "Wrong branch. Expected '$ExpectedBranch'; found '$currentBranch'."
}
$status = Invoke-Git -Arguments @("status", "--porcelain")
$statusText = @($status) -join ""
if (-not [string]::IsNullOrWhiteSpace($statusText)) {
    throw "Working tree/index is not clean. Commit or safely stash unrelated work before running. Nothing was changed."
}
if (Test-Path -LiteralPath $OutputPath) {
    throw "Output folder already exists: $OutputPath"
}
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

$csb = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$csb["Data Source"] = $SqlInstance
$csb["Initial Catalog"] = $DatabaseName
$csb["Integrated Security"] = $true
$csb["Connect Timeout"] = 20
$csb["Application Name"] = "BuildingManagement-Redesign-SchemaParity-ReadOnly"
$csb["Encrypt"] = $false
$csb["TrustServerCertificate"] = $true
$connection = New-Object -TypeName System.Data.SqlClient.SqlConnection -ArgumentList $csb.ConnectionString

$queries = [ordered]@{
"01-Database-Identity.csv" = @"
SET NOCOUNT ON;
SELECT DB_NAME() AS DatabaseName,
 CAST(SERVERPROPERTY('ServerName') AS nvarchar(256)) AS ServerName,
 CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(128)) AS ProductVersion,
 CAST(SERVERPROPERTY('Edition') AS nvarchar(256)) AS Edition,
 d.compatibility_level AS CompatibilityLevel,
 d.collation_name AS DatabaseCollation,
 d.recovery_model_desc AS RecoveryModel,
 d.state_desc AS DatabaseState,
 d.is_read_only AS IsReadOnly,
 d.snapshot_isolation_state_desc AS SnapshotIsolationState,
 d.is_read_committed_snapshot_on AS ReadCommittedSnapshotOn
FROM sys.databases d WHERE d.database_id = DB_ID();
"@
"02-Table-Inventory-And-RowCounts.csv" = @"
SET NOCOUNT ON;
SELECT s.name AS SchemaName, t.name AS TableName,
 SUM(CASE WHEN p.index_id IN (0,1) THEN p.rows ELSE 0 END) AS ApproximateRows,
 t.create_date AS CreateDate, t.modify_date AS ModifyDate
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
LEFT JOIN sys.partitions p ON p.object_id = t.object_id
GROUP BY s.name, t.name, t.create_date, t.modify_date
ORDER BY s.name, t.name;
"@
"03-GateA-Object-Inventory.csv" = @"
SET NOCOUNT ON;
WITH Expected(TableName) AS
(
 SELECT v.TableName FROM (VALUES
 (N'Persons'),(N'UserAccounts'),(N'UserCredentials'),(N'UserTwoFactorSettings'),
 (N'UserRecoveryCodes'),(N'RoleDefinitions'),(N'RolePermissions'),
 (N'UserRoleAssignments'),(N'ApartmentOwnerships'),(N'ApartmentOccupancies'),
 (N'ApartmentAccessGrants'),(N'AccountPermissionOverrides'),(N'LegacyIdentityMap')
 ) v(TableName)
)
SELECT e.TableName,
 CASE WHEN t.object_id IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS ExistsInDatabase,
 t.create_date AS CreateDate, t.modify_date AS ModifyDate
FROM Expected e
LEFT JOIN sys.tables t ON t.schema_id = SCHEMA_ID(N'dbo') AND t.name = e.TableName
ORDER BY e.TableName;
"@
"04-Column-Metadata-All-Tables.csv" = @"
SET NOCOUNT ON;
SELECT s.name AS SchemaName, t.name AS TableName, c.column_id AS ColumnOrdinal,
 c.name AS ColumnName, ty.name AS SqlType, c.max_length AS MaxLengthBytes,
 c.precision AS NumericPrecision, c.scale AS NumericScale, c.is_nullable AS IsNullable,
 c.collation_name AS CollationName, c.is_identity AS IsIdentity, c.is_computed AS IsComputed,
 cc.definition AS ComputedDefinition, dc.definition AS DefaultDefinition
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN sys.computed_columns cc ON cc.object_id = c.object_id AND cc.column_id = c.column_id
LEFT JOIN sys.default_constraints dc ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
ORDER BY s.name, t.name, c.column_id;
"@
"05-Primary-And-Unique-Keys.csv" = @"
SET NOCOUNT ON;
SELECT s.name AS SchemaName, t.name AS TableName, kc.name AS ConstraintName,
 kc.type_desc AS ConstraintType, i.name AS IndexName, ic.key_ordinal AS KeyOrdinal,
 c.name AS ColumnName, i.is_unique AS IsUnique, i.is_primary_key AS IsPrimaryKey,
 i.has_filter AS HasFilter, i.filter_definition AS FilterDefinition
FROM sys.key_constraints kc
JOIN sys.tables t ON t.object_id = kc.parent_object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.indexes i ON i.object_id = kc.parent_object_id AND i.index_id = kc.unique_index_id
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.key_ordinal > 0
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
ORDER BY s.name, t.name, kc.name, ic.key_ordinal;
"@
"06-Foreign-Key-Metadata.csv" = @"
SET NOCOUNT ON;
SELECT ps.name AS ParentSchema, pt.name AS ParentTable, fk.name AS ForeignKeyName,
 fkc.constraint_column_id AS ColumnOrdinal, pc.name AS ParentColumn,
 rs.name AS ReferencedSchema, rt.name AS ReferencedTable, rc.name AS ReferencedColumn,
 fk.delete_referential_action_desc AS DeleteAction,
 fk.update_referential_action_desc AS UpdateAction,
 fk.is_disabled AS IsDisabled, fk.is_not_trusted AS IsNotTrusted,
 fk.is_not_for_replication AS IsNotForReplication
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.tables pt ON pt.object_id = fk.parent_object_id
JOIN sys.schemas ps ON ps.schema_id = pt.schema_id
JOIN sys.columns pc ON pc.object_id = pt.object_id AND pc.column_id = fkc.parent_column_id
JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
JOIN sys.schemas rs ON rs.schema_id = rt.schema_id
JOIN sys.columns rc ON rc.object_id = rt.object_id AND rc.column_id = fkc.referenced_column_id
ORDER BY ps.name, pt.name, fk.name, fkc.constraint_column_id;
"@
"07-Check-Constraints.csv" = @"
SET NOCOUNT ON;
SELECT s.name AS SchemaName, t.name AS TableName, cc.name AS ConstraintName,
 cc.definition AS Definition, cc.is_disabled AS IsDisabled,
 cc.is_not_trusted AS IsNotTrusted, cc.is_not_for_replication AS IsNotForReplication
FROM sys.check_constraints cc
JOIN sys.tables t ON t.object_id = cc.parent_object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
ORDER BY s.name, t.name, cc.name;
"@
"08-Default-Constraints.csv" = @"
SET NOCOUNT ON;
SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName,
 dc.name AS ConstraintName, dc.definition AS Definition
FROM sys.default_constraints dc
JOIN sys.tables t ON t.object_id = dc.parent_object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
ORDER BY s.name, t.name, c.column_id;
"@
"09-Index-Inventory.csv" = @"
SET NOCOUNT ON;
SELECT s.name AS SchemaName, t.name AS TableName, i.name AS IndexName,
 i.type_desc AS IndexType, i.is_unique AS IsUnique, i.is_primary_key AS IsPrimaryKey,
 i.is_unique_constraint AS IsUniqueConstraint, i.is_disabled AS IsDisabled,
 i.has_filter AS HasFilter, i.filter_definition AS FilterDefinition,
 ic.key_ordinal AS KeyOrdinal, ic.is_included_column AS IsIncludedColumn,
 c.name AS ColumnName, ic.is_descending_key AS IsDescending
FROM sys.indexes i
JOIN sys.tables t ON t.object_id = i.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.index_id > 0
ORDER BY s.name, t.name, i.name, ic.is_included_column, ic.key_ordinal, ic.index_column_id;
"@
"10-View-Inventory.csv" = @"
SET NOCOUNT ON;
SELECT s.name AS SchemaName, v.name AS ViewName, v.create_date AS CreateDate,
 v.modify_date AS ModifyDate,
 OBJECTPROPERTYEX(v.object_id, 'IsSchemaBound') AS IsSchemaBound,
 OBJECTPROPERTYEX(v.object_id, 'IsEncrypted') AS IsEncrypted
FROM sys.views v JOIN sys.schemas s ON s.schema_id = v.schema_id
ORDER BY s.name, v.name;
"@
"11-View-Definitions.csv" = @"
SET NOCOUNT ON;
SELECT s.name AS SchemaName, v.name AS ViewName, OBJECT_DEFINITION(v.object_id) AS Definition
FROM sys.views v JOIN sys.schemas s ON s.schema_id = v.schema_id
ORDER BY s.name, v.name;
"@
"12-SQL-Dependencies.csv" = @"
SET NOCOUNT ON;
SELECT OBJECT_SCHEMA_NAME(d.referencing_id) AS ReferencingSchema,
 OBJECT_NAME(d.referencing_id) AS ReferencingObject, o.type_desc AS ReferencingType,
 d.referenced_server_name AS ReferencedServer, d.referenced_database_name AS ReferencedDatabase,
 d.referenced_schema_name AS ReferencedSchema, d.referenced_entity_name AS ReferencedEntity,
 d.is_schema_bound_reference AS IsSchemaBound, d.is_ambiguous AS IsAmbiguous
FROM sys.sql_expression_dependencies d
LEFT JOIN sys.objects o ON o.object_id = d.referencing_id
ORDER BY ReferencingSchema, ReferencingObject, ReferencedSchema, ReferencedEntity;
"@
"13-Legacy-Key-Metadata.csv" = @"
SET NOCOUNT ON;
SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName,
 ty.name AS SqlType, c.max_length AS MaxLengthBytes, c.precision AS NumericPrecision,
 c.scale AS NumericScale, c.is_nullable AS IsNullable, c.collation_name AS CollationName,
 i.name AS KeyIndexName, ic.key_ordinal AS KeyOrdinal
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN sys.index_columns ic ON ic.object_id = t.object_id AND ic.column_id = c.column_id AND ic.key_ordinal > 0
LEFT JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE (t.name = N'Buildings' AND c.name = N'Id')
 OR (t.name = N'Apartments' AND c.name IN (N'BuildingId', N'Id'))
ORDER BY s.name, t.name, c.column_id, ic.key_ordinal;
"@
"14-EF-Migration-History.csv" = @"
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
 SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
ELSE
 SELECT CAST(N'__EFMigrationsHistory not found' AS nvarchar(200)) AS Status;
"@
"15-Database-Options.csv" = @"
SET NOCOUNT ON;
SELECT name AS DatabaseName, compatibility_level AS CompatibilityLevel,
 collation_name AS CollationName, recovery_model_desc AS RecoveryModel,
 is_read_committed_snapshot_on AS ReadCommittedSnapshotOn,
 snapshot_isolation_state_desc AS SnapshotIsolationState,
 is_auto_close_on AS AutoCloseOn, is_auto_shrink_on AS AutoShrinkOn,
 is_query_store_on AS QueryStoreOn
FROM sys.databases WHERE database_id = DB_ID();
"@
}

$queryErrors = New-Object System.Collections.Generic.List[string]
$exported = New-Object System.Collections.Generic.List[string]
try {
    $connection.Open()
    if ($connection.Database -ne $DatabaseName) {
        throw "Connected to unexpected database '$($connection.Database)'. Expected '$DatabaseName'."
    }
    if ($connection.DataSource -ine $SqlInstance) {
        throw "Connected to unexpected SQL instance '$($connection.DataSource)'. Expected '$SqlInstance'."
    }
    foreach ($entry in $queries.GetEnumerator()) {
        try {
            $table = Invoke-ReadOnlyQuery -Connection $connection -Sql $entry.Value
            $filePath = Join-Path $OutputPath $entry.Key
            Export-DataTableCsv -Table $table -Path $filePath
            $exported.Add($entry.Key)
            Write-Host ("  {0}: {1} row(s)" -f $entry.Key, $table.Rows.Count)
        }
        catch {
            $message = "{0}: {1}" -f $entry.Key, $_.Exception.Message
            $queryErrors.Add($message)
            Write-Warning $message
        }
    }
}
finally {
    if ($connection.State -ne [System.Data.ConnectionState]::Closed) { $connection.Close() }
    $connection.Dispose()
}

$readme = @"
Redesign schema parity diagnostics
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz")
Repository: $RepoPath
Branch: $currentBranch
SQL instance: $SqlInstance
Database: $DatabaseName
Authentication: Windows Integrated Security
Scope: READ-ONLY metadata and approximate table row counts; no business row values or secrets exported.
Queries exported: $($exported.Count) / $($queries.Count)
"@
Set-Content -LiteralPath (Join-Path $OutputPath "00-README.txt") -Value $readme -Encoding UTF8

if ($queryErrors.Count -gt 0) {
    Set-Content -LiteralPath $ErrorsPath -Value ($queryErrors -join [Environment]::NewLine) -Encoding UTF8
    throw "One or more diagnostics queries failed. Results remain local and were NOT staged or pushed. Review $ErrorsPath."
}
if ($exported.Count -ne $queries.Count) {
    throw "Expected $($queries.Count) CSV files but exported $($exported.Count). Results remain local and were NOT staged or pushed."
}

Write-Host ""
Write-Host "Read-only diagnostics completed. Review the generated CSV files before Git commit."
Write-Host "Output: $OutputPath"
$approval = Read-Host "Type COMMIT-REVIEWED-RESULTS to stage this diagnostics folder and push it"
if ($approval -cne "COMMIT-REVIEWED-RESULTS") {
    Write-Host "Commit/push skipped. Results remain local at: $OutputPath"
    exit 0
}

$relativeForGit = ($RelativeOutputPath -replace '\\', '/')
Invoke-Git -Arguments @("add", "--", $relativeForGit) | Out-Null
$staged = Invoke-Git -Arguments @("diff", "--cached", "--name-only")
$unexpected = @($staged | Where-Object { $_ -notlike "$($relativeForGit)/*" })
if ($unexpected.Count -gt 0) {
    throw "Unexpected staged paths detected. Nothing will be committed: $($unexpected -join ', ')"
}
$commitMessage = "Diagnostics: collect redesign schema parity $Timestamp"
Invoke-Git -Arguments @("commit", "-m", $commitMessage) | Out-Null
Invoke-Git -Arguments @("push", "origin", $ExpectedBranch) | Out-Null
$finalStatus = Invoke-Git -Arguments @("status", "--porcelain")
$finalStatusText = @($finalStatus) -join ""
if (-not [string]::IsNullOrWhiteSpace($finalStatusText)) {
    throw "Push completed but working tree is not clean. Review Git status manually."
}
Write-Host ""
Write-Host "Diagnostics committed and pushed successfully."
Write-Host "Folder: $relativeForGit"
