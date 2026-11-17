/*
    Phase07 – Data Integrity Assessment
    READ-ONLY diagnostic. No INSERT/UPDATE/DELETE/DDL is executed.
    Run against BuildingManagement_RestoreTest only.
    SQL Server 2017+ compatible.

    IMPORTANT:
    Results are investigation signals, not automatic authorization to change data
    or add constraints. Review each result against application/business rules.
*/
SET NOCOUNT ON;

SELECT
    DB_NAME() AS DatabaseName,
    @@SERVERNAME AS ServerName,
    SYSDATETIMEOFFSET() AS CapturedAt,
    (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) AS UserTableCount;

-- Result set 2: domain values and numeric anomalies.
SELECT 'APARTMENT_NEGATIVE_MONTHLY_FEE' AS IssueCode,
       'Apartments' AS EntityName, BuildingId, Id AS RecordId,
       CONCAT('MonthlyFee=', CONVERT(varchar(40), MonthlyFee)) AS Details
FROM dbo.Apartments WHERE MonthlyFee < 0
UNION ALL
SELECT 'EXPENSE_NEGATIVE_AMOUNT', 'Expenses', BuildingId, Id,
       CONCAT('Amount=', CONVERT(varchar(40), Amount), '; MonthKey=', RTRIM(MonthKey))
FROM dbo.Expenses WHERE Amount < 0
UNION ALL
SELECT 'REVENUE_NEGATIVE_AMOUNT', 'Revenues', BuildingId, Id,
       CONCAT('Amount=', CONVERT(varchar(40), Amount), '; MonthKey=', RTRIM(MonthKey))
FROM dbo.Revenues WHERE Amount < 0
UNION ALL
SELECT 'DEPOSIT_NONPOSITIVE_AMOUNT', 'Deposits', BuildingId, Id,
       CONCAT('Amount=', CONVERT(varchar(40), Amount), '; Status=', Status)
FROM dbo.Deposits WHERE Amount <= 0
UNION ALL
SELECT 'DEPOSIT_UNKNOWN_STATUS', 'Deposits', BuildingId, Id,
       CONCAT('Status=', Status)
FROM dbo.Deposits WHERE Status NOT IN ('pending', 'confirmed', 'cancelled')
UNION ALL
SELECT 'CATEGORY_UNKNOWN_KIND', 'FinancialCategories', BuildingId, Id,
       CONCAT('Kind=', Kind)
FROM dbo.FinancialCategories WHERE Kind NOT IN ('expense', 'revenue')
ORDER BY IssueCode, BuildingId, RecordId;

-- Result set 3: malformed month keys (YYYY-MM).
SELECT 'Expenses' AS EntityName, BuildingId, Id AS RecordId, RTRIM(MonthKey) AS MonthKey
FROM dbo.Expenses
WHERE LEN(RTRIM(MonthKey)) <> 7
   OR TRY_CONVERT(date, RTRIM(MonthKey) + '-01', 23) IS NULL
UNION ALL
SELECT 'Revenues', BuildingId, Id, RTRIM(MonthKey)
FROM dbo.Revenues
WHERE LEN(RTRIM(MonthKey)) <> 7
   OR TRY_CONVERT(date, RTRIM(MonthKey) + '-01', 23) IS NULL
UNION ALL
SELECT 'Deposits', BuildingId, Id, RTRIM(MonthKey)
FROM dbo.Deposits
WHERE LEN(RTRIM(MonthKey)) <> 7
   OR TRY_CONVERT(date, RTRIM(MonthKey) + '-01', 23) IS NULL
UNION ALL
SELECT 'MonthlyApartmentShares', BuildingId, ApartmentId, RTRIM(MonthKey)
FROM dbo.MonthlyApartmentShares
WHERE LEN(RTRIM(MonthKey)) <> 7
   OR TRY_CONVERT(date, RTRIM(MonthKey) + '-01', 23) IS NULL
UNION ALL
SELECT 'WalletAdjustments', BuildingId, Id, RTRIM(MonthKey)
FROM dbo.WalletAdjustments
WHERE LEN(RTRIM(MonthKey)) <> 7
   OR TRY_CONVERT(date, RTRIM(MonthKey) + '-01', 23) IS NULL
ORDER BY EntityName, BuildingId, RecordId;

-- Result set 4: expense/revenue date and MonthKey disagreement.
-- Review only: the application may intentionally permit cross-month dates.
SELECT 'Expenses' AS EntityName, BuildingId, Id AS RecordId,
       RTRIM(MonthKey) AS MonthKey, ExpenseDate AS TransactionDate
FROM dbo.Expenses
WHERE CONVERT(char(7), ExpenseDate, 120) <> RTRIM(MonthKey)
UNION ALL
SELECT 'Revenues', BuildingId, Id, RTRIM(MonthKey), RevenueDate
FROM dbo.Revenues
WHERE CONVERT(char(7), RevenueDate, 120) <> RTRIM(MonthKey)
ORDER BY EntityName, BuildingId, RecordId;

-- Result set 5: multiple primary residents in the same apartment.
-- This is a business-rule review signal; do not delete or demote records automatically.
SELECT BuildingId, ApartmentId,
       COUNT(*) AS PrimaryResidentCount,
       SUM(CASE WHEN IsActive = 1 AND IsDisabled = 0 THEN 1 ELSE 0 END) AS ActivePrimaryResidentCount,
       STRING_AGG(CONVERT(nvarchar(max), Id), ', ') AS ResidentIds
FROM dbo.Residents
WHERE IsPrimary = 1
GROUP BY BuildingId, ApartmentId
HAVING COUNT(*) > 1
ORDER BY BuildingId, ApartmentId;

-- Result set 6: repeated deposit display numbers within one building/month.
-- IDs remain the row identity; this checks the human-facing number only.
SELECT BuildingId, RTRIM(MonthKey) AS MonthKey, Number,
       COUNT(*) AS DuplicateCount,
       STRING_AGG(CONVERT(nvarchar(max), Id), ', ') AS DepositIds
FROM dbo.Deposits
WHERE NULLIF(LTRIM(RTRIM(Number)), '') IS NOT NULL
GROUP BY BuildingId, RTRIM(MonthKey), Number
HAVING COUNT(*) > 1
ORDER BY BuildingId, MonthKey, Number;

-- Result set 7: settings outside obvious application bounds.
-- Treat as review signals until confirmed against product requirements.
SELECT 'BuildingSettings' AS EntityName, BuildingId AS RecordId,
       CONCAT('InvoiceDayOfMonth=', InvoiceDayOfMonth,
              '; VotingQuorumPercent=', VotingQuorumPercent) AS Details
FROM dbo.BuildingSettings
WHERE InvoiceDayOfMonth NOT BETWEEN 1 AND 31
   OR VotingQuorumPercent NOT BETWEEN 0 AND 100
UNION ALL
SELECT 'GlobalSettings', Id,
       CONCAT('SessionTimeoutDays=', SessionTimeoutDays,
              '; MaxPinAttempts=', MaxPinAttempts)
FROM dbo.GlobalSettings
WHERE SessionTimeoutDays <= 0 OR MaxPinAttempts <= 0
UNION ALL
SELECT 'ScheduledBackupSettings', Id,
       CONCAT('Hour=', [Hour], '; MaxBackupsToKeep=', MaxBackupsToKeep,
              '; Frequency=', Frequency)
FROM dbo.ScheduledBackupSettings
WHERE [Hour] NOT BETWEEN 0 AND 23
   OR MaxBackupsToKeep < 1
ORDER BY EntityName, RecordId;

-- Result set 8: FK and CHECK constraint enforcement state.
SELECT
    fk.name AS ConstraintName,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) AS SchemaName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    fk.is_disabled AS IsDisabled,
    fk.is_not_trusted AS IsNotTrusted,
    'FOREIGN KEY' AS ConstraintType
FROM sys.foreign_keys AS fk
WHERE fk.is_disabled = 1 OR fk.is_not_trusted = 1
UNION ALL
SELECT
    cc.name,
    OBJECT_SCHEMA_NAME(cc.parent_object_id),
    OBJECT_NAME(cc.parent_object_id),
    cc.is_disabled,
    cc.is_not_trusted,
    'CHECK'
FROM sys.check_constraints AS cc
WHERE cc.is_disabled = 1 OR cc.is_not_trusted = 1
ORDER BY ConstraintType, SchemaName, TableName, ConstraintName;

-- Result set 9: current CHECK constraints (inventory only).
SELECT
    OBJECT_SCHEMA_NAME(parent_object_id) AS SchemaName,
    OBJECT_NAME(parent_object_id) AS TableName,
    name AS ConstraintName,
    definition AS ConstraintDefinition,
    is_disabled AS IsDisabled,
    is_not_trusted AS IsNotTrusted
FROM sys.check_constraints
ORDER BY SchemaName, TableName, ConstraintName;
