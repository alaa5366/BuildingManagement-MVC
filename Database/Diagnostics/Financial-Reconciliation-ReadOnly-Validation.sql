/*
Financial / wallet reconciliation diagnostic — READ ONLY.
Target: disposable BuildingManagement_RestoreTest only.
Returns source-ledger totals, stored share snapshots, per-apartment detail,
and a deterministic candidate allocation for comparison only.
Never updates or recalculates stored financial rows.
*/
USE [BuildingManagement_RestoreTest];
IF DB_NAME() <> N'BuildingManagement_RestoreTest'
    THROW 51000, 'Wrong database. This diagnostic is allowed only on BuildingManagement_RestoreTest.', 1;
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Buildings', N'U') IS NULL
 OR OBJECT_ID(N'dbo.Apartments', N'U') IS NULL
 OR OBJECT_ID(N'dbo.Expenses', N'U') IS NULL
 OR OBJECT_ID(N'dbo.Revenues', N'U') IS NULL
 OR OBJECT_ID(N'dbo.MonthlyApartmentShares', N'U') IS NULL
 OR OBJECT_ID(N'dbo.Deposits', N'U') IS NULL
 OR OBJECT_ID(N'dbo.WalletAdjustments', N'U') IS NULL
    THROW 51001, 'Required financial tables are missing. No diagnostic result was produced.', 1;

-- 1. Database identity.
SELECT DB_NAME() AS DatabaseName, @@SERVERNAME AS ServerName,
       SYSUTCDATETIME() AS CheckedAtUtc,
       N'READ_ONLY_FINANCIAL_RECONCILIATION' AS CheckType;

-- 2. Building/month ledger totals vs persisted monthly share snapshots.
;WITH MonthKeys AS
(
    SELECT BuildingId, MonthKey FROM dbo.Expenses
    UNION SELECT BuildingId, MonthKey FROM dbo.Revenues
    UNION SELECT BuildingId, MonthKey FROM dbo.MonthlyApartmentShares
    UNION SELECT BuildingId, MonthKey FROM dbo.Deposits
    UNION SELECT BuildingId, MonthKey FROM dbo.WalletAdjustments
),
Ledger AS
(
    SELECT BuildingId, MonthKey,
           SUM(CAST(Amount AS decimal(38,2))) AS ExpenseLedgerTotal,
           COUNT_BIG(*) AS ExpenseRows
    FROM dbo.Expenses GROUP BY BuildingId, MonthKey
),
RevenueLedger AS
(
    SELECT BuildingId, MonthKey,
           SUM(CAST(Amount AS decimal(38,2))) AS RevenueLedgerTotal,
           COUNT_BIG(*) AS RevenueRows
    FROM dbo.Revenues GROUP BY BuildingId, MonthKey
),
Shares AS
(
    SELECT BuildingId, MonthKey,
           SUM(CAST(ExpenseShare AS decimal(38,2))) AS ExpenseShareTotal,
           SUM(CAST(RevenueShare AS decimal(38,2))) AS RevenueShareTotal,
           SUM(CAST(CarryOver AS decimal(38,2))) AS CarryOverTotal,
           COUNT_BIG(*) AS ShareRows
    FROM dbo.MonthlyApartmentShares GROUP BY BuildingId, MonthKey
),
AptCounts AS
(
    SELECT BuildingId, COUNT_BIG(*) AS ApartmentCount,
           SUM(CASE WHEN IsClosed = 0 THEN CONVERT(bigint,1) ELSE CONVERT(bigint,0) END) AS CurrentOpenApartmentCount,
           SUM(CASE WHEN IsClosed = 1 THEN CONVERT(bigint,1) ELSE CONVERT(bigint,0) END) AS CurrentClosedApartmentCount
    FROM dbo.Apartments GROUP BY BuildingId
),
DepositTotals AS
(
    SELECT BuildingId, MonthKey,
           SUM(CASE WHEN Status = N'confirmed' THEN CAST(Amount AS decimal(38,2)) ELSE 0 END) AS ConfirmedDeposits,
           SUM(CASE WHEN Status = N'pending' THEN CAST(Amount AS decimal(38,2)) ELSE 0 END) AS PendingDeposits,
           SUM(CASE WHEN Status = N'cancelled' THEN CAST(Amount AS decimal(38,2)) ELSE 0 END) AS CancelledDeposits,
           COUNT_BIG(*) AS DepositRows
    FROM dbo.Deposits GROUP BY BuildingId, MonthKey
),
Adjustments AS
(
    SELECT BuildingId, MonthKey,
           SUM(CAST(Amount AS decimal(38,2))) AS AdjustmentTotal,
           COUNT_BIG(*) AS AdjustmentRows
    FROM dbo.WalletAdjustments GROUP BY BuildingId, MonthKey
)
SELECT m.BuildingId, m.MonthKey,
       COALESCE(l.ExpenseLedgerTotal,0) AS ExpenseLedgerTotal,
       COALESCE(s.ExpenseShareTotal,0) AS ExpenseShareTotal,
       COALESCE(s.ExpenseShareTotal,0)-COALESCE(l.ExpenseLedgerTotal,0) AS ExpenseShareDelta,
       COALESCE(l.ExpenseRows,0) AS ExpenseRows,
       COALESCE(r.RevenueLedgerTotal,0) AS RevenueLedgerTotal,
       COALESCE(s.RevenueShareTotal,0) AS RevenueShareTotal,
       COALESCE(s.RevenueShareTotal,0)-COALESCE(r.RevenueLedgerTotal,0) AS RevenueShareDelta,
       COALESCE(r.RevenueRows,0) AS RevenueRows,
       COALESCE(s.ShareRows,0) AS ShareRows,
       COALESCE(a.ApartmentCount,0) AS ApartmentCount,
       COALESCE(a.CurrentOpenApartmentCount,0) AS CurrentOpenApartmentCount,
       COALESCE(a.CurrentClosedApartmentCount,0) AS CurrentClosedApartmentCount,
       COALESCE(d.ConfirmedDeposits,0) AS ConfirmedDeposits,
       COALESCE(d.PendingDeposits,0) AS PendingDeposits,
       COALESCE(d.CancelledDeposits,0) AS CancelledDeposits,
       COALESCE(d.DepositRows,0) AS DepositRows,
       COALESCE(s.CarryOverTotal,0) AS CarryOverTotal,
       COALESCE(x.AdjustmentTotal,0) AS AdjustmentTotal,
       COALESCE(x.AdjustmentRows,0) AS AdjustmentRows
FROM MonthKeys m
LEFT JOIN Ledger l ON l.BuildingId=m.BuildingId AND l.MonthKey=m.MonthKey
LEFT JOIN RevenueLedger r ON r.BuildingId=m.BuildingId AND r.MonthKey=m.MonthKey
LEFT JOIN Shares s ON s.BuildingId=m.BuildingId AND s.MonthKey=m.MonthKey
LEFT JOIN AptCounts a ON a.BuildingId=m.BuildingId
LEFT JOIN DepositTotals d ON d.BuildingId=m.BuildingId AND d.MonthKey=m.MonthKey
LEFT JOIN Adjustments x ON x.BuildingId=m.BuildingId AND x.MonthKey=m.MonthKey
ORDER BY m.BuildingId, m.MonthKey;

-- 3. Per-apartment stored financial inputs. MonthlyFee and IsClosed are CURRENT
-- apartment values, intentionally labelled as such (not historical snapshots).
;WITH MonthKeys AS
(
    SELECT BuildingId, MonthKey FROM dbo.Expenses
    UNION SELECT BuildingId, MonthKey FROM dbo.Revenues
    UNION SELECT BuildingId, MonthKey FROM dbo.MonthlyApartmentShares
    UNION SELECT BuildingId, MonthKey FROM dbo.Deposits
    UNION SELECT BuildingId, MonthKey FROM dbo.WalletAdjustments
),
Deposits AS
(
    SELECT BuildingId, MonthKey, ApartmentId,
           SUM(CASE WHEN Status=N'confirmed' THEN CAST(Amount AS decimal(38,2)) ELSE 0 END) AS ConfirmedDeposits
    FROM dbo.Deposits GROUP BY BuildingId, MonthKey, ApartmentId
),
Adjustments AS
(
    SELECT BuildingId, MonthKey, ApartmentId,
           SUM(CAST(Amount AS decimal(38,2))) AS AdjustmentTotal
    FROM dbo.WalletAdjustments GROUP BY BuildingId, MonthKey, ApartmentId
)
SELECT m.BuildingId, m.MonthKey, a.Id AS ApartmentId, a.Number AS ApartmentNumber,
       a.MonthlyFee AS CurrentMonthlyFee, a.IsClosed AS CurrentlyClosed,
       s.ExpenseShare AS StoredExpenseShare,
       s.RevenueShare AS StoredRevenueShare,
       s.CarryOver AS StoredCarryOver,
       COALESCE(d.ConfirmedDeposits,0) AS ConfirmedDeposits,
       COALESCE(x.AdjustmentTotal,0) AS AdjustmentTotal,
       CASE WHEN s.ApartmentId IS NULL THEN CONVERT(bit,0) ELSE CONVERT(bit,1) END AS HasStoredShareRow,
       CAST(COALESCE(s.CarryOver,0)
          + COALESCE(d.ConfirmedDeposits,0)
          - CASE WHEN a.IsClosed=0 THEN a.MonthlyFee ELSE 0 END
          - COALESCE(s.ExpenseShare,0)
          + COALESCE(s.RevenueShare,0)
          + COALESCE(x.AdjustmentTotal,0) AS decimal(38,2)) AS CurrentCodeBalanceReconstruction
FROM MonthKeys m
JOIN dbo.Apartments a ON a.BuildingId=m.BuildingId
LEFT JOIN dbo.MonthlyApartmentShares s
  ON s.BuildingId=m.BuildingId AND s.MonthKey=m.MonthKey AND s.ApartmentId=a.Id
LEFT JOIN Deposits d
  ON d.BuildingId=m.BuildingId AND d.MonthKey=m.MonthKey AND d.ApartmentId=a.Id
LEFT JOIN Adjustments x
  ON x.BuildingId=m.BuildingId AND x.MonthKey=m.MonthKey AND x.ApartmentId=a.Id
ORDER BY m.BuildingId, m.MonthKey, a.Number, a.Id;

-- 4. Candidate deterministic equal distribution using CURRENT open apartments.
-- This is a comparison candidate only, not proof of the historical allocation order.
;WITH MonthKeys AS
(
    SELECT BuildingId, MonthKey FROM dbo.Expenses
    UNION SELECT BuildingId, MonthKey FROM dbo.Revenues
    UNION SELECT BuildingId, MonthKey FROM dbo.MonthlyApartmentShares
),
Totals AS
(
    SELECT m.BuildingId, m.MonthKey,
           COALESCE(e.TotalAmount,0) AS ExpenseTotal,
           COALESCE(r.TotalAmount,0) AS RevenueTotal
    FROM MonthKeys m
    LEFT JOIN
    (
        SELECT BuildingId, MonthKey, SUM(CAST(Amount AS decimal(38,2))) AS TotalAmount
        FROM dbo.Expenses GROUP BY BuildingId, MonthKey
    ) e ON e.BuildingId=m.BuildingId AND e.MonthKey=m.MonthKey
    LEFT JOIN
    (
        SELECT BuildingId, MonthKey, SUM(CAST(Amount AS decimal(38,2))) AS TotalAmount
        FROM dbo.Revenues GROUP BY BuildingId, MonthKey
    ) r ON r.BuildingId=m.BuildingId AND r.MonthKey=m.MonthKey
),
Eligible AS
(
    SELECT t.BuildingId, t.MonthKey, a.Id AS ApartmentId, a.Number AS ApartmentNumber,
           t.ExpenseTotal, t.RevenueTotal,
           ROW_NUMBER() OVER (PARTITION BY t.BuildingId,t.MonthKey ORDER BY a.Id) AS CandidateOrder,
           COUNT_BIG(*) OVER (PARTITION BY t.BuildingId,t.MonthKey) AS EligibleCount
    FROM Totals t
    JOIN dbo.Apartments a ON a.BuildingId=t.BuildingId AND a.IsClosed=0
),
Candidate AS
(
    SELECT e.*,
           CAST(ROUND(e.ExpenseTotal/NULLIF(e.EligibleCount,0),2) AS decimal(38,2)) AS ExpenseBaseShare,
           CAST(ROUND(e.RevenueTotal/NULLIF(e.EligibleCount,0),2) AS decimal(38,2)) AS RevenueBaseShare
    FROM Eligible e
)
SELECT c.BuildingId, c.MonthKey, c.ApartmentId, c.ApartmentNumber,
       c.CandidateOrder, c.EligibleCount,
       c.ExpenseTotal, c.RevenueTotal,
       CASE WHEN c.CandidateOrder<c.EligibleCount THEN c.ExpenseBaseShare
            ELSE c.ExpenseTotal-(c.ExpenseBaseShare*(c.EligibleCount-1)) END AS CandidateExpenseShare,
       CASE WHEN c.CandidateOrder<c.EligibleCount THEN c.RevenueBaseShare
            ELSE c.RevenueTotal-(c.RevenueBaseShare*(c.EligibleCount-1)) END AS CandidateRevenueShare,
       s.ExpenseShare AS StoredExpenseShare,
       s.RevenueShare AS StoredRevenueShare,
       COALESCE(s.ExpenseShare,0) -
         CASE WHEN c.CandidateOrder<c.EligibleCount THEN c.ExpenseBaseShare
              ELSE c.ExpenseTotal-(c.ExpenseBaseShare*(c.EligibleCount-1)) END AS ExpenseStoredMinusCandidate,
       COALESCE(s.RevenueShare,0) -
         CASE WHEN c.CandidateOrder<c.EligibleCount THEN c.RevenueBaseShare
              ELSE c.RevenueTotal-(c.RevenueBaseShare*(c.EligibleCount-1)) END AS RevenueStoredMinusCandidate,
       N'CANDIDATE_ONLY_ORDER_BY_APARTMENT_ID_NOT_HISTORICAL_PROOF' AS CandidatePolicy
FROM Candidate c
LEFT JOIN dbo.MonthlyApartmentShares s
  ON s.BuildingId=c.BuildingId AND s.MonthKey=c.MonthKey AND s.ApartmentId=c.ApartmentId
ORDER BY c.BuildingId,c.MonthKey,c.CandidateOrder;

-- 5. Missing share rows and share rows without a matching apartment.
;WITH MonthKeys AS
(
    SELECT BuildingId, MonthKey FROM dbo.Expenses
    UNION SELECT BuildingId, MonthKey FROM dbo.Revenues
    UNION SELECT BuildingId, MonthKey FROM dbo.MonthlyApartmentShares
)
SELECT m.BuildingId, m.MonthKey, a.Id AS ApartmentId, a.Number AS ApartmentNumber,
       N'MISSING_MONTHLY_SHARE_ROW' AS Issue
FROM MonthKeys m
JOIN dbo.Apartments a ON a.BuildingId=m.BuildingId
LEFT JOIN dbo.MonthlyApartmentShares s
  ON s.BuildingId=m.BuildingId AND s.MonthKey=m.MonthKey AND s.ApartmentId=a.Id
WHERE s.ApartmentId IS NULL
UNION ALL
SELECT s.BuildingId, s.MonthKey, s.ApartmentId, CAST(NULL AS int),
       N'SHARE_ROW_WITHOUT_MATCHING_APARTMENT'
FROM dbo.MonthlyApartmentShares s
LEFT JOIN dbo.Apartments a ON a.BuildingId=s.BuildingId AND a.Id=s.ApartmentId
WHERE a.Id IS NULL
ORDER BY BuildingId, MonthKey, ApartmentId;
