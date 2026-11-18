/*
Enterprise Database Redesign — Read-only Data Profiling
Target: BuildingManagement_RestoreTest
Expected server: ZOS-AAM\LOCALDB#8B6B20E4

SAFETY:
- Read-only SELECT/metadata statements only.
- Does not print names, phone numbers, email addresses, PINs, password hashes,
  API tokens, encrypted secret values, recovery codes, or payment details.
- Run against the local disposable restored test database only: BuildingManagement_RestoreTest.
- The script checks DB_NAME() and aborts if another database is selected.
- Uses a 5-second lock timeout and low deadlock priority.
- Review/export all result sets with the redesign baseline.
*/
USE [BuildingManagement_RestoreTest];
IF DB_NAME() <> N'BuildingManagement_RestoreTest'
    THROW 51000, 'Wrong database. Profiling is allowed only on BuildingManagement_RestoreTest.', 1;
SET NOCOUNT ON;
SET LOCK_TIMEOUT 5000;
SET DEADLOCK_PRIORITY LOW;
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

SELECT
    DB_NAME() AS DatabaseName,
    @@SERVERNAME AS ServerName,
    SYSUTCDATETIME() AS ProfiledAtUtc,
    'READ_ONLY_PROFILE' AS ProfileType;

-- 1. Identity cardinality and matching quality (counts only; no PII returned).
;WITH ResidentUidGroups AS
(
    SELECT Uid
    FROM dbo.Residents
    WHERE NULLIF(LTRIM(RTRIM(Uid)), '') IS NOT NULL
    GROUP BY Uid
    HAVING COUNT_BIG(*) > 1
),
UserUidGroups AS
(
    SELECT Uid
    FROM dbo.Users
    WHERE NULLIF(LTRIM(RTRIM(Uid)), '') IS NOT NULL
    GROUP BY Uid
    HAVING COUNT_BIG(*) > 1
),
ResidentEmailGroups AS
(
    SELECT LOWER(LTRIM(RTRIM(Email))) AS NormalizedValue
    FROM dbo.Residents
    WHERE NULLIF(LTRIM(RTRIM(Email)), '') IS NOT NULL
    GROUP BY LOWER(LTRIM(RTRIM(Email)))
    HAVING COUNT_BIG(*) > 1
),
UserEmailGroups AS
(
    SELECT LOWER(LTRIM(RTRIM(Email))) AS NormalizedValue
    FROM dbo.Users
    WHERE NULLIF(LTRIM(RTRIM(Email)), '') IS NOT NULL
    GROUP BY LOWER(LTRIM(RTRIM(Email)))
    HAVING COUNT_BIG(*) > 1
),
ResidentPhoneGroups AS
(
    SELECT LTRIM(RTRIM(Phone)) AS NormalizedValue
    FROM dbo.Residents
    WHERE NULLIF(LTRIM(RTRIM(Phone)), '') IS NOT NULL
    GROUP BY LTRIM(RTRIM(Phone))
    HAVING COUNT_BIG(*) > 1
),
UserPhoneGroups AS
(
    SELECT LTRIM(RTRIM(Phone)) AS NormalizedValue
    FROM dbo.Users
    WHERE NULLIF(LTRIM(RTRIM(Phone)), '') IS NOT NULL
    GROUP BY LTRIM(RTRIM(Phone))
    HAVING COUNT_BIG(*) > 1
)
SELECT 'Residents' AS Entity, 'TotalRows' AS Metric, COUNT_BIG(*) AS MetricValue FROM dbo.Residents
UNION ALL SELECT 'Users', 'TotalRows', COUNT_BIG(*) FROM dbo.Users
UNION ALL SELECT 'Residents', 'DuplicateUidGroups', COUNT_BIG(*) FROM ResidentUidGroups
UNION ALL SELECT 'Users', 'DuplicateUidGroups', COUNT_BIG(*) FROM UserUidGroups
UNION ALL SELECT 'Residents', 'UidWithoutMatchingUser', COUNT_BIG(*)
    FROM dbo.Residents r
    WHERE NULLIF(LTRIM(RTRIM(r.Uid)), '') IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.Users u WHERE u.Uid = r.Uid)
UNION ALL SELECT 'Residents', 'UidMatchedToUser', COUNT_BIG(*)
    FROM dbo.Residents r
    WHERE NULLIF(LTRIM(RTRIM(r.Uid)), '') IS NOT NULL
      AND EXISTS (SELECT 1 FROM dbo.Users u WHERE u.Uid = r.Uid)
UNION ALL SELECT 'Residents', 'DuplicateNormalizedEmailGroups', COUNT_BIG(*) FROM ResidentEmailGroups
UNION ALL SELECT 'Users', 'DuplicateNormalizedEmailGroups', COUNT_BIG(*) FROM UserEmailGroups
UNION ALL SELECT 'Residents', 'DuplicatePhoneGroups', COUNT_BIG(*) FROM ResidentPhoneGroups
UNION ALL SELECT 'Users', 'DuplicatePhoneGroups', COUNT_BIG(*) FROM UserPhoneGroups
ORDER BY Entity, Metric;

-- 2. Apartment references and occupancy/ownership candidates.
SELECT
    'ResidentsWithoutApartment' AS Metric,
    COUNT_BIG(*) AS MetricValue
FROM dbo.Residents r
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.Apartments a
    WHERE a.BuildingId = r.BuildingId AND a.Id = r.ApartmentId
)
UNION ALL
SELECT
    'ApartmentsWithoutBuilding',
    COUNT_BIG(*)
FROM dbo.Apartments a
WHERE NOT EXISTS (SELECT 1 FROM dbo.Buildings b WHERE b.Id = a.BuildingId)
UNION ALL
SELECT
    'ApartmentsWithoutValidFloor',
    COUNT_BIG(*)
FROM dbo.Apartments a
WHERE NULLIF(LTRIM(RTRIM(a.FloorId)), '') IS NOT NULL
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.Floors f
      WHERE f.BuildingId = a.BuildingId AND f.Id = a.FloorId
  )
UNION ALL
SELECT
    'ApartmentsWithLegacyOwnerText',
    COUNT_BIG(*)
FROM dbo.Apartments
WHERE NULLIF(LTRIM(RTRIM(Owner)), '') IS NOT NULL
UNION ALL
SELECT
    'ApartmentsWithAtLeastOneResidentFlaggedOwner',
    COUNT_BIG(*)
FROM dbo.Apartments a
WHERE EXISTS
(
    SELECT 1 FROM dbo.Residents r
    WHERE r.BuildingId = a.BuildingId
      AND r.ApartmentId = a.Id
      AND r.IsOwner = 1
)
UNION ALL
SELECT
    'ApartmentsWithLegacyOwnerTextButNoResidentOwnerFlag',
    COUNT_BIG(*)
FROM dbo.Apartments a
WHERE NULLIF(LTRIM(RTRIM(a.Owner)), '') IS NOT NULL
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.Residents r
      WHERE r.BuildingId = a.BuildingId
        AND r.ApartmentId = a.Id
        AND r.IsOwner = 1
  )
UNION ALL
SELECT
    'ApartmentsWithMultipleCurrentPrimaryResidents',
    COUNT_BIG(*)
FROM
(
    SELECT BuildingId, ApartmentId
    FROM dbo.Residents
    WHERE IsPrimary = 1 AND IsActive = 1 AND IsDisabled = 0
    GROUP BY BuildingId, ApartmentId
    HAVING COUNT_BIG(*) > 1
) d
ORDER BY Metric;

-- 3. Apartment-level occupancy candidate counts; no personal values.
SELECT
    BuildingId,
    ApartmentId,
    COUNT_BIG(*) AS ResidentRows,
    SUM(CASE WHEN IsOwner = 1 THEN 1 ELSE 0 END) AS OwnerFlagRows,
    SUM(CASE WHEN IsPrimary = 1 AND IsActive = 1 AND IsDisabled = 0 THEN 1 ELSE 0 END) AS ActivePrimaryRows,
    SUM(CASE WHEN IsActive = 1 AND IsDisabled = 0 THEN 1 ELSE 0 END) AS ActiveResidentRows
FROM dbo.Residents
GROUP BY BuildingId, ApartmentId
ORDER BY BuildingId, ApartmentId;

-- 4. Financial baseline: aggregate amounts only, by building/month/status.
-- Preserve these totals as pre-migration reconciliation evidence.
SELECT
    'Deposits' AS SourceTable,
    BuildingId,
    MonthKey,
    Status AS StatusOrCategory,
    COUNT_BIG(*) AS [TotalRows],
    SUM(Amount) AS TotalAmount
FROM dbo.Deposits
GROUP BY BuildingId, MonthKey, Status
UNION ALL
SELECT
    'Expenses',
    BuildingId,
    MonthKey,
    CategoryKind,
    COUNT_BIG(*),
    SUM(Amount)
FROM dbo.Expenses
GROUP BY BuildingId, MonthKey, CategoryKind
UNION ALL
SELECT
    'Revenues',
    BuildingId,
    MonthKey,
    CategoryKind,
    COUNT_BIG(*),
    SUM(Amount)
FROM dbo.Revenues
GROUP BY BuildingId, MonthKey, CategoryKind
ORDER BY SourceTable, BuildingId, MonthKey, StatusOrCategory;

SELECT
    BuildingId,
    MonthKey,
    COUNT_BIG(*) AS ApartmentShareRows,
    SUM(ExpenseShare) AS TotalExpenseShare,
    SUM(RevenueShare) AS TotalRevenueShare,
    SUM(CarryOver) AS TotalCarryOver
FROM dbo.MonthlyApartmentShares
GROUP BY BuildingId, MonthKey
ORDER BY BuildingId, MonthKey;

SELECT
    BuildingId,
    COUNT_BIG(*) AS AdjustmentRows,
    SUM(Amount) AS NetAdjustmentAmount
FROM dbo.WalletAdjustments
GROUP BY BuildingId
ORDER BY BuildingId;

-- 5. Poll integrity. Historical votes remain legacy apartment-attributed votes.
SELECT
    COUNT_BIG(*) AS TotalLegacyPollVotes,
    COUNT_BIG(DISTINCT PollId) AS PollsWithVotes,
    COUNT_BIG(DISTINCT ApartmentId) AS ApartmentsWithVotes
FROM dbo.PollVotes;

SELECT
    COUNT_BIG(*) AS VotesWithoutMatchingOption
FROM dbo.PollVotes v
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.PollOptions o
    WHERE o.BuildingId = v.BuildingId
      AND o.PollId = v.PollId
      AND o.Id = v.OptionId
);

SELECT
    COUNT_BIG(*) AS VotesWithoutMatchingPoll
FROM dbo.PollVotes v
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Polls p
    WHERE p.BuildingId = v.BuildingId
      AND p.Id = v.PollId
);

-- 6. Camera/DVR tenant integrity.
SELECT
    COUNT_BIG(*) AS TotalCameras,
    SUM(CASE WHEN d.Id IS NULL THEN 1 ELSE 0 END) AS CamerasWithoutDvr,
    SUM(CASE WHEN d.Id IS NOT NULL AND d.BuildingId <> c.BuildingId THEN 1 ELSE 0 END) AS CamerasWithCrossBuildingDvr
FROM dbo.Cameras c
LEFT JOIN dbo.Dvrs d ON d.Id = c.DvrId;

-- 7. Security inventory: counts/presence only. Never select secret values.
SELECT
    (SELECT COUNT_BIG(*) FROM dbo.Users WHERE NULLIF(LTRIM(RTRIM(Pin)), '') IS NOT NULL) AS UsersWithLegacyPlainPin,
    (SELECT COUNT_BIG(*) FROM dbo.Apartments WHERE NULLIF(LTRIM(RTRIM(Pin)), '') IS NOT NULL) AS ApartmentsWithLegacyPlainPin,
    (SELECT COUNT_BIG(*) FROM dbo.Residents WHERE NULLIF(LTRIM(RTRIM(PinHash)), '') IS NOT NULL) AS ResidentsWithPinHash,
    (SELECT COUNT_BIG(*) FROM dbo.Residents WHERE NULLIF(LTRIM(RTRIM(PasswordHash)), '') IS NOT NULL) AS ResidentsWithPasswordHash,
    (SELECT COUNT_BIG(*) FROM dbo.GlobalSettings
      WHERE NULLIF(LTRIM(RTRIM(WhatsappApiToken)), '') IS NOT NULL) AS GlobalSettingsWithWhatsappToken,
    (SELECT COUNT_BIG(*) FROM dbo.GlobalSettings
      WHERE NULLIF(LTRIM(RTRIM(CloudinaryApiKey)), '') IS NOT NULL) AS GlobalSettingsWithCloudinaryApiKey,
    (SELECT COUNT_BIG(*) FROM dbo.Dvrs
      WHERE NULLIF(LTRIM(RTRIM(PasswordEncrypted)), '') IS NOT NULL) AS DvrsWithEncryptedPassword;

-- 8. Wallet view presence and module metadata (no business values).
SELECT
    o.name,
    o.type_desc,
    OBJECT_SCHEMA_NAME(o.object_id) AS SchemaName
FROM sys.objects o
WHERE o.name IN ('vw_ApartmentWalletMovements', 'vw_ApartmentWalletBalance')
ORDER BY o.name;

-- 9. Current FK delete actions for the tables that need retention review.
SELECT
    fk.name AS ForeignKeyName,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ParentSchema,
    OBJECT_NAME(fk.parent_object_id) AS ParentTable,
    OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ReferencedSchema,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    fk.delete_referential_action_desc AS OnDelete,
    fk.is_disabled AS IsDisabled,
    fk.is_not_trusted AS IsNotTrusted
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.parent_object_id) IN
(
    'Residents','Deposits','DepositEditHistory','Expenses','Revenues',
    'MonthlyApartmentShares','WalletAdjustments','PollVotes','PollOptions',
    'Cameras','QrTokens','QrUsages','AuditLog'
)
ORDER BY ParentTable, ForeignKeyName;

-- 10. Confirm migration history for this exact restored database.
SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;
