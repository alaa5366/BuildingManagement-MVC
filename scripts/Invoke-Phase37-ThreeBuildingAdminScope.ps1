[CmdletBinding()]
param(
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30",
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$reportDir = Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$report = Join-Path $reportDir "Phase37-BuildingAdminThreeBuildingScope-$stamp.md"
$log = [System.Collections.Generic.List[string]]::new()
function Log([string]$Message) { $log.Add($Message); Write-Host $Message }
function New-Command($Connection, $Transaction, [string]$Sql) {
    $command = $Connection.CreateCommand()
    $command.Transaction = $Transaction
    $command.CommandTimeout = 180
    $command.CommandText = $Sql
    return $command
}
$connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
$mode = if ($Apply) { 'APPLY' } else { 'DRY RUN' }
try {
    $connection.Open()
    $database = [string](New-Command $connection $null 'SELECT DB_NAME();').ExecuteScalar()
    if ($database -ne 'BuildingManagement_RestoreTest') { throw "Unsafe target database '$database'. Only BuildingManagement_RestoreTest is permitted." }

    $preflight = @"
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.Users',N'U') IS NULL OR OBJECT_ID(N'dbo.BuildingAdmins',N'U') IS NULL
 OR OBJECT_ID(N'dbo.Buildings',N'U') IS NULL OR OBJECT_ID(N'dbo.LegacyIdentityMap',N'U') IS NULL
 OR OBJECT_ID(N'dbo.UserAccounts',N'U') IS NULL OR OBJECT_ID(N'dbo.RoleDefinitions',N'U') IS NULL
 OR OBJECT_ID(N'dbo.RolePermissions',N'U') IS NULL OR OBJECT_ID(N'dbo.UserRoleAssignments',N'U') IS NULL
 THROW 51037, 'Required identity, source, building, or authorization tables are missing.', 1;

IF COL_LENGTH(N'dbo.Users',N'Uid') IS NULL OR COL_LENGTH(N'dbo.Users',N'Role') IS NULL
 OR COL_LENGTH(N'dbo.Users',N'IsDisabled') IS NULL OR COL_LENGTH(N'dbo.Users',N'IsActive') IS NULL
 OR COL_LENGTH(N'dbo.BuildingAdmins',N'AdminUid') IS NULL OR COL_LENGTH(N'dbo.BuildingAdmins',N'BuildingId') IS NULL
 THROW 51037, 'Required source columns are missing.', 1;

DECLARE @admins TABLE(AdminUid nvarchar(256) PRIMARY KEY, AccountId uniqueidentifier NOT NULL, BuildingId nvarchar(64) NOT NULL, MappingSource nvarchar(40) NOT NULL);
DECLARE @sourceAdminCount int, @buildingCount int, @explicitLinkCount int, @unmappedAccountCount int;

SELECT @sourceAdminCount=COUNT(*) FROM dbo.Users
WHERE LOWER(LTRIM(RTRIM(Role))) IN(N'admin',N'buildingadmin');
SELECT @buildingCount=COUNT(*) FROM dbo.Buildings;
SELECT @explicitLinkCount=COUNT(*) FROM dbo.BuildingAdmins;

IF @sourceAdminCount<>3 THROW 51037, 'Expected exactly three legacy building Admin accounts; refusing to infer identities.', 1;
IF @buildingCount<>3 THROW 51037, 'Expected exactly three target Buildings; refusing ordinal or guessed mapping.', 1;
IF @explicitLinkCount<>2 THROW 51037, 'Expected exactly two explicit BuildingAdmins links; source changed or links are ambiguous.', 1;
IF EXISTS(SELECT 1 FROM dbo.BuildingAdmins GROUP BY AdminUid HAVING COUNT(*)<>1)
 OR EXISTS(SELECT 1 FROM dbo.BuildingAdmins GROUP BY BuildingId HAVING COUNT(*)<>1)
 THROW 51037, 'Explicit BuildingAdmins links are not one-to-one.', 1;
IF EXISTS(
 SELECT 1 FROM dbo.BuildingAdmins ba
 LEFT JOIN dbo.Users u ON u.Uid=ba.AdminUid
 LEFT JOIN dbo.Buildings b ON CONVERT(nvarchar(64),b.Id)=CONVERT(nvarchar(64),ba.BuildingId)
 WHERE u.Uid IS NULL OR LOWER(LTRIM(RTRIM(u.Role))) NOT IN(N'admin',N'buildingadmin') OR b.Id IS NULL
) THROW 51037, 'An explicit source admin/building link does not resolve exactly.', 1;

IF EXISTS(
 SELECT 1 FROM dbo.Users u
 WHERE LOWER(LTRIM(RTRIM(u.Role))) IN(N'admin',N'buildingadmin')
 AND (ISNULL(u.IsDisabled,0)=1 OR ISNULL(u.IsActive,1)=0)
) THROW 51037, 'At least one source Admin is inactive/disabled; manual review required.', 1;

IF EXISTS(
 SELECT 1 FROM dbo.Users u
 WHERE LOWER(LTRIM(RTRIM(u.Role))) IN(N'admin',N'buildingadmin')
 AND (SELECT COUNT(*) FROM dbo.LegacyIdentityMap m
      WHERE m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users'
        AND m.SourceKey=CONVERT(nvarchar(256),u.Uid)
        AND m.MappingStatus=1 AND m.AccountId IS NOT NULL AND m.PersonId IS NOT NULL)<>1
) THROW 51037, 'Every Admin must have exactly one verified Users identity/account mapping.', 1;

IF EXISTS(
 SELECT 1 FROM dbo.Users u
 JOIN dbo.LegacyIdentityMap m ON m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users'
  AND m.SourceKey=CONVERT(nvarchar(256),u.Uid) AND m.MappingStatus=1
 JOIN dbo.UserAccounts a ON a.AccountId=m.AccountId
 WHERE LOWER(LTRIM(RTRIM(u.Role))) IN(N'admin',N'buildingadmin')
 AND a.AccountStatus NOT IN(0,1)
) THROW 51037, 'An Admin target account is locked/disabled; refusing role provisioning.', 1;

INSERT @admins(AdminUid,AccountId,BuildingId,MappingSource)
SELECT CONVERT(nvarchar(256),u.Uid),m.AccountId,CONVERT(nvarchar(64),ba.BuildingId),N'Explicit BuildingAdmins'
FROM dbo.BuildingAdmins ba
JOIN dbo.Users u ON u.Uid=ba.AdminUid
JOIN dbo.LegacyIdentityMap m ON m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users'
 AND m.SourceKey=CONVERT(nvarchar(256),u.Uid) AND m.MappingStatus=1
WHERE LOWER(LTRIM(RTRIM(u.Role))) IN(N'admin',N'buildingadmin');

IF EXISTS(
 SELECT 1 FROM dbo.Users u
 JOIN dbo.LegacyIdentityMap m ON m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users'
  AND m.SourceKey=CONVERT(nvarchar(256),u.Uid) AND m.MappingStatus=1
 WHERE LOWER(LTRIM(RTRIM(u.Role))) IN(N'admin',N'buildingadmin')
 AND NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba WHERE ba.AdminUid=u.Uid)
)
BEGIN
    -- Owner-confirmed rule: the sole Admin without a legacy BuildingAdmins row is Building 3.
    IF (SELECT COUNT(*) FROM dbo.Users u
        WHERE LOWER(LTRIM(RTRIM(u.Role))) IN(N'admin',N'buildingadmin')
          AND NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba WHERE ba.AdminUid=u.Uid))<>1
      THROW 51037, 'Expected exactly one unlinked Admin.', 1;
    IF (SELECT COUNT(*) FROM dbo.Buildings b
        WHERE NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba
          WHERE CONVERT(nvarchar(64),ba.BuildingId)=CONVERT(nvarchar(64),b.Id)))<>1
      THROW 51037, 'Expected exactly one building without an explicit BuildingAdmins link.', 1;

    INSERT @admins(AdminUid,AccountId,BuildingId,MappingSource)
    SELECT CONVERT(nvarchar(256),u.Uid),m.AccountId,remaining.BuildingId,N'Owner-confirmed Building 3; unique remainder'
    FROM dbo.Users u
    JOIN dbo.LegacyIdentityMap m ON m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users'
      AND m.SourceKey=CONVERT(nvarchar(256),u.Uid) AND m.MappingStatus=1
    CROSS APPLY (SELECT CONVERT(nvarchar(64),b.Id) BuildingId FROM dbo.Buildings b
      WHERE NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba
       WHERE CONVERT(nvarchar(64),ba.BuildingId)=CONVERT(nvarchar(64),b.Id))) remaining
    WHERE LOWER(LTRIM(RTRIM(u.Role))) IN(N'admin',N'buildingadmin')
      AND NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba WHERE ba.AdminUid=u.Uid);
END;

IF (SELECT COUNT(*) FROM @admins)<>3
 OR (SELECT COUNT(DISTINCT AccountId) FROM @admins)<>3
 OR (SELECT COUNT(DISTINCT BuildingId) FROM @admins)<>3
 THROW 51037, 'Resolved Admin-to-Building map is not exactly three unique accounts and three unique buildings.', 1;

IF EXISTS(
 SELECT 1 FROM @admins x JOIN dbo.UserRoleAssignments a ON a.AccountId=x.AccountId
 WHERE a.RevokedAtUtc IS NULL AND (a.ValidToUtc IS NULL OR a.ValidToUtc>SYSUTCDATETIME())
   AND NOT (a.RoleKey=N'building-admin' AND a.ScopeType=1
            AND a.ScopeBuildingId=x.BuildingId AND a.ScopeApartmentId IS NULL)
) THROW 51037, 'An Admin already has another active role assignment; resolve conflict before provisioning.', 1;

IF EXISTS(SELECT 1 FROM dbo.RoleDefinitions WHERE RoleKey=N'building-admin' AND IsSystemRole=1)
 THROW 51037, 'Existing building-admin role is incorrectly marked as a system role.', 1;

SELECT AdminUid,AccountId,BuildingId,MappingSource FROM @admins ORDER BY BuildingId;
SELECT COUNT(*) ExistingRoleAssignments FROM dbo.UserRoleAssignments;
"@
    $cmd = New-Command $connection $null $preflight
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($cmd)
    $data = [System.Data.DataSet]::new()
    [void]$adapter.Fill($data)
    if ($data.Tables.Count -lt 1 -or $data.Tables[0].Rows.Count -ne 3) { throw 'Preflight did not return exactly three Admin-to-Building mappings.' }
    Log "Database: $database"
    Log "Mode: $mode"
    Log 'Resolved three exact Admin accounts and three distinct Building scopes:'
    foreach ($row in $data.Tables[0].Rows) {
        Log ("AdminUid={0}; AccountId={1}; BuildingId={2}; Evidence={3}" -f $row.AdminUid,$row.AccountId,$row.BuildingId,$row.MappingSource)
    }

    $permissionKeys = @(
        'residents.manage','apartment.disable','dvrs.manage','cameras.view','cameras.manage',
        'reports.view','polls.manage','maintenance.manage','audit.view.building',
        'settings.manage.building','wallet.read','deposit.read','deposit.confirm','deposit.cancel',
        'wallet.adjust','expense.read','expense.post','expense.reverse','revenue.read',
        'revenue.post','revenue.reverse','finance.export','finance.period.close',
        'finance.period.reopen','notification.dispatch','integration.whatsapp.send',
        'integration.whatsapp.configure','apartment.access.grant.manage'
    )
    Log ("Building-scoped permission catalog entries: {0}" -f $permissionKeys.Count)
    Log 'System-only permissions are deliberately excluded: buildings/admins management, global audit/settings, logs, identity mapping, backup/restore/delete, secrets, impersonation, and maintenance execution.'
    Log 'SuperAdmin accounts are not modified by this phase.'

    if ($Apply) {
        $transaction = $connection.BeginTransaction()
        try {
            $sql = @"
SET NOCOUNT ON;
DECLARE @now datetime2(3)=SYSUTCDATETIME();
DECLARE @admins TABLE(AccountId uniqueidentifier PRIMARY KEY,BuildingId nvarchar(64) NOT NULL);
INSERT @admins(AccountId,BuildingId)
SELECT m.AccountId,CONVERT(nvarchar(64),b.Id)
FROM dbo.Users u
JOIN dbo.LegacyIdentityMap m ON m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users'
 AND m.SourceKey=CONVERT(nvarchar(256),u.Uid) AND m.MappingStatus=1
JOIN dbo.BuildingAdmins ba ON ba.AdminUid=u.Uid
JOIN dbo.Buildings b ON CONVERT(nvarchar(64),b.Id)=CONVERT(nvarchar(64),ba.BuildingId)
WHERE LOWER(LTRIM(RTRIM(u.Role))) IN(N'admin',N'buildingadmin');

INSERT @admins(AccountId,BuildingId)
SELECT m.AccountId,CONVERT(nvarchar(64),remaining.Id)
FROM dbo.Users u
JOIN dbo.LegacyIdentityMap m ON m.SourceSystem=N'legacy-sql' AND m.SourceTable=N'Users'
 AND m.SourceKey=CONVERT(nvarchar(256),u.Uid) AND m.MappingStatus=1
CROSS APPLY (SELECT b.Id FROM dbo.Buildings b
 WHERE NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba
  WHERE CONVERT(nvarchar(64),ba.BuildingId)=CONVERT(nvarchar(64),b.Id))) remaining
WHERE LOWER(LTRIM(RTRIM(u.Role))) IN(N'admin',N'buildingadmin')
 AND NOT EXISTS(SELECT 1 FROM dbo.BuildingAdmins ba WHERE ba.AdminUid=u.Uid);

IF (SELECT COUNT(*) FROM @admins)<>3 OR (SELECT COUNT(DISTINCT BuildingId) FROM @admins)<>3
 THROW 51038,'Admin/building mapping changed after preflight.',1;

IF NOT EXISTS(SELECT 1 FROM dbo.RoleDefinitions WHERE RoleKey=N'building-admin')
 INSERT dbo.RoleDefinitions(RoleKey,DisplayName,IsSystemRole,IsActive)
 VALUES(N'building-admin',N'Building Administrator',0,1);
ELSE
 UPDATE dbo.RoleDefinitions SET DisplayName=N'Building Administrator',IsSystemRole=0,IsActive=1
 WHERE RoleKey=N'building-admin';

DECLARE @permissions TABLE(PermissionKey nvarchar(100) PRIMARY KEY);
INSERT @permissions(PermissionKey) VALUES
(N'residents.manage'),(N'apartment.disable'),(N'dvrs.manage'),(N'cameras.view'),(N'cameras.manage'),
(N'reports.view'),(N'polls.manage'),(N'maintenance.manage'),(N'audit.view.building'),
(N'settings.manage.building'),(N'wallet.read'),(N'deposit.read'),(N'deposit.confirm'),(N'deposit.cancel'),
(N'wallet.adjust'),(N'expense.read'),(N'expense.post'),(N'expense.reverse'),(N'revenue.read'),
(N'revenue.post'),(N'revenue.reverse'),(N'finance.export'),(N'finance.period.close'),
(N'finance.period.reopen'),(N'notification.dispatch'),(N'integration.whatsapp.send'),
(N'integration.whatsapp.configure'),(N'apartment.access.grant.manage);

INSERT dbo.RolePermissions(RoleKey,PermissionKey,Effect)
SELECT N'building-admin',p.PermissionKey,1 FROM @permissions p
WHERE NOT EXISTS(SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleKey=N'building-admin' AND rp.PermissionKey=p.PermissionKey);

IF EXISTS(SELECT 1 FROM dbo.RolePermissions rp
 WHERE rp.RoleKey=N'building-admin' AND rp.Effect<>1)
 THROW 51038,'Building admin role contains a non-Allow permission effect.',1;

IF EXISTS(SELECT 1 FROM dbo.RolePermissions rp
 WHERE rp.RoleKey=N'building-admin'
 AND rp.PermissionKey NOT IN
 (N'residents.manage',N'apartment.disable',N'dvrs.manage',N'cameras.view',N'cameras.manage',
  N'reports.view',N'polls.manage',N'maintenance.manage',N'audit.view.building',
  N'settings.manage.building',N'wallet.read',N'deposit.read',N'deposit.confirm',N'deposit.cancel',
  N'wallet.adjust',N'expense.read',N'expense.post',N'expense.reverse',N'revenue.read',
  N'revenue.post',N'revenue.reverse',N'finance.export',N'finance.period.close',
  N'finance.period.reopen',N'notification.dispatch',N'integration.whatsapp.send',
  N'integration.whatsapp.configure',N'apartment.access.grant.manage))
 THROW 51038,'Building admin role contains permissions outside the approved building-only set.',1;

INSERT dbo.UserRoleAssignments
(RoleAssignmentId,AccountId,RoleKey,ScopeType,ScopeBuildingId,ScopeApartmentId,ValidFromUtc,ValidToUtc,GrantedAtUtc,GrantedByAccountId,GrantReason,RevokedAtUtc,RevokedByAccountId,RevocationReason)
SELECT NEWID(),a.AccountId,N'building-admin',1,a.BuildingId,NULL,@now,NULL,@now,NULL,
 N'Phase37: exact legacy Admin identity map; explicit BuildingAdmins link or owner-confirmed unique remaining Building 3 scope',NULL,NULL,NULL
FROM @admins a
WHERE NOT EXISTS(SELECT 1 FROM dbo.UserRoleAssignments existing
 WHERE existing.AccountId=a.AccountId AND existing.RoleKey=N'building-admin'
   AND existing.ScopeType=1 AND existing.ScopeBuildingId=a.BuildingId
   AND existing.ScopeApartmentId IS NULL AND existing.RevokedAtUtc IS NULL
   AND (existing.ValidToUtc IS NULL OR existing.ValidToUtc>@now));

IF (SELECT COUNT(*) FROM dbo.UserRoleAssignments a
 JOIN @admins x ON x.AccountId=a.AccountId
 WHERE a.RoleKey=N'building-admin' AND a.ScopeType=1
   AND a.ScopeBuildingId=x.BuildingId AND a.ScopeApartmentId IS NULL
   AND a.RevokedAtUtc IS NULL AND (a.ValidToUtc IS NULL OR a.ValidToUtc>@now))<>3
 THROW 51038,'Post-write verification did not find exactly three active building-scoped assignments.',1;

IF EXISTS(SELECT 1 FROM dbo.UserRoleAssignments a JOIN @admins x ON x.AccountId=a.AccountId
 WHERE a.RevokedAtUtc IS NULL AND (a.ValidToUtc IS NULL OR a.ValidToUtc>@now)
 AND (a.ScopeType<>1 OR a.ScopeBuildingId<>x.BuildingId OR a.ScopeApartmentId IS NOT NULL))
 THROW 51038,'Cross-building or system-wide active assignment detected for a Building Admin.',1;

COMMIT;
"@
            $applyCommand = New-Command $connection $transaction $sql
            [void]$applyCommand.ExecuteNonQuery()
            Log 'APPLY TRANSACTION COMMITTED.'
        }
        catch {
            try { $transaction.Rollback() } catch {}
            throw
        }
    }
    else {
        Log 'DRY RUN: no role, permission, or assignment rows changed. Re-run with -Apply after reviewing this report.'
    }
}
catch {
    Log "ERROR: $($_.Exception.Message)"
    throw
}
finally {
    if ($connection.State -ne 'Closed') { $connection.Close() }
    $lines = @(
        '# Phase 37 - Three building administrators, isolated scopes',
        '',
        "- Run: $(Get-Date -Format o)",
        "- Mode: $mode",
        '',
        '## Execution',
        ($log | ForEach-Object { "- $_" }),
        '',
        '## Security boundaries',
        '- Three legacy Users rows with role Admin/buildingadmin must resolve to three distinct target UserAccounts and three distinct Buildings.',
        '- Two administrators use exact BuildingAdmins source links.',
        '- The sole unlinked administrator is assigned to the sole building without a BuildingAdmins link, per the owner-approved Building 3 decision; no row ordering or guessed ID is used.',
        '- Every assignment is ScopeType=Building with exactly one ScopeBuildingId and no apartment scope.',
        '- The shared building-admin role contains only code-catalogued building-capable permissions; system-only and personal permissions are excluded.',
        '- SuperAdmin accounts and existing unrelated assignments are not modified.',
        '- The script is restricted to BuildingManagement_RestoreTest, transactional, idempotent, and dry-run by default.',
        '- Runtime authorization enforcement is a separate required Phase38; Phase37 database grants alone do not make the legacy perm-claim filters safe.'
    )
    Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
    Write-Host "Report: $report"
}
Push-Location $root
try {
    $scriptPath = Join-Path $PSScriptRoot 'Invoke-Phase37-ThreeBuildingAdminScope.ps1'
    git add -- $scriptPath $report
    if ((git status --short -- $scriptPath $report).Count -gt 0) {
        git commit --only -m "feat(authz): provision three isolated building admin scopes" -- $scriptPath $report
        git push origin development/database-redesign-enterprise
    }
}
finally { Pop-Location }
