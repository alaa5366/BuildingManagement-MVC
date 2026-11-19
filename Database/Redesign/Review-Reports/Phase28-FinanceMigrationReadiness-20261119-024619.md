# Phase 28 - Finance migration readiness profile

- Run: 2026-11-19T02:46:20.4391283+02:00
- Mode: READ ONLY

## Results
Database: BuildingManagement_RestoreTest
Mode: READ ONLY; no source or target rows are changed.

## Legacy source row counts
- Deposits = 26
- DepositEditHistory = 1
- Expenses = 12
- Revenues = 4
- MonthlyApartmentShares = 113
- WalletAdjustments = 2

## Target finance row counts
- FinancialPeriods = 0
- PaymentDeposits = 0
- LedgerTransactions = 0
- LedgerEntries = 0
- FinancialSourceLinks = 0
- FinancialAllocationBatches = 0
- FinancialAllocationLines = 0
- FinancialReconciliationRuns = 0
- FinancialReconciliationDifferences = 0

## Legacy source column inventory (names/types only; no data values)
### Deposits
- BuildingId: nvarchar(64), precision=0, scale=0, NOT NULL
- Id: nvarchar(64), precision=0, scale=0, NOT NULL
- MonthKey: char(7), precision=0, scale=0, NOT NULL
- Number: nvarchar(MAX), precision=0, scale=0, NOT NULL
- ApartmentId: nvarchar(64), precision=0, scale=0, NOT NULL
- Amount: decimal(), precision=18, scale=2, NOT NULL
- Note: nvarchar(MAX), precision=0, scale=0, NOT NULL
- Status: nvarchar(MAX), precision=0, scale=0, NOT NULL
- ReceiptUrl: nvarchar(MAX), precision=0, scale=0, NULL
- ReceiptPath: nvarchar(MAX), precision=0, scale=0, NULL
- ReceiptFileName: nvarchar(MAX), precision=0, scale=0, NULL
- ReceiptFileSize: bigint(), precision=19, scale=0, NULL
- ReceiptFileType: nvarchar(MAX), precision=0, scale=0, NULL
- ReceiptSuccess: bit(), precision=1, scale=0, NULL
- CreatedAt: datetime2(), precision=19, scale=0, NOT NULL
- CreatedBy: nvarchar(MAX), precision=0, scale=0, NOT NULL
- ConfirmedAt: datetime2(), precision=19, scale=0, NULL
- ConfirmedBy: nvarchar(MAX), precision=0, scale=0, NULL
- CancelledAt: datetime2(), precision=19, scale=0, NULL
- CancelledBy: nvarchar(MAX), precision=0, scale=0, NULL
- CancelledReason: nvarchar(MAX), precision=0, scale=0, NULL
- UpdatedAt: datetime2(), precision=19, scale=0, NULL
- UpdatedBy: nvarchar(MAX), precision=0, scale=0, NULL
- UpdateReason: nvarchar(MAX), precision=0, scale=0, NULL
### DepositEditHistory
- EditId: bigint(), precision=19, scale=0, NOT NULL
- BuildingId: nvarchar(64), precision=0, scale=0, NOT NULL
- DepositId: nvarchar(64), precision=0, scale=0, NOT NULL
- Ts: datetime2(), precision=19, scale=0, NOT NULL
- ByUser: nvarchar(MAX), precision=0, scale=0, NOT NULL
- Reason: nvarchar(MAX), precision=0, scale=0, NOT NULL
- OldAmount: decimal(), precision=18, scale=2, NOT NULL
- NewAmount: decimal(), precision=18, scale=2, NOT NULL
- OldNote: nvarchar(MAX), precision=0, scale=0, NOT NULL
- NewNote: nvarchar(MAX), precision=0, scale=0, NOT NULL
### Expenses
- BuildingId: nvarchar(64), precision=0, scale=0, NOT NULL
- Id: nvarchar(64), precision=0, scale=0, NOT NULL
- MonthKey: char(7), precision=0, scale=0, NOT NULL
- CategoryId: nvarchar(64), precision=0, scale=0, NULL
- CategoryKind: nvarchar(10), precision=0, scale=0, NOT NULL
- Note: nvarchar(MAX), precision=0, scale=0, NOT NULL
- Amount: decimal(), precision=18, scale=2, NOT NULL
- ExpenseDate: date(), precision=10, scale=0, NOT NULL
- ReceiptUrl: nvarchar(MAX), precision=0, scale=0, NULL
### Revenues
- BuildingId: nvarchar(64), precision=0, scale=0, NOT NULL
- Id: nvarchar(64), precision=0, scale=0, NOT NULL
- MonthKey: char(7), precision=0, scale=0, NOT NULL
- CategoryId: nvarchar(64), precision=0, scale=0, NULL
- CategoryKind: nvarchar(10), precision=0, scale=0, NOT NULL
- Note: nvarchar(MAX), precision=0, scale=0, NOT NULL
- Amount: decimal(), precision=18, scale=2, NOT NULL
- RevenueDate: date(), precision=10, scale=0, NOT NULL
### MonthlyApartmentShares
- BuildingId: nvarchar(64), precision=0, scale=0, NOT NULL
- MonthKey: char(7), precision=0, scale=0, NOT NULL
- ApartmentId: nvarchar(64), precision=0, scale=0, NOT NULL
- ExpenseShare: decimal(), precision=18, scale=2, NOT NULL
- RevenueShare: decimal(), precision=18, scale=2, NOT NULL
- CarryOver: decimal(), precision=18, scale=2, NOT NULL
### WalletAdjustments
- BuildingId: nvarchar(64), precision=0, scale=0, NOT NULL
- Id: nvarchar(64), precision=0, scale=0, NOT NULL
- ApartmentId: nvarchar(64), precision=0, scale=0, NOT NULL
- Amount: decimal(), precision=18, scale=2, NOT NULL
- Reason: nvarchar(MAX), precision=0, scale=0, NOT NULL
- MonthKey: char(7), precision=0, scale=0, NOT NULL
- CreatedAt: datetime2(), precision=19, scale=0, NOT NULL
- CreatedBy: nvarchar(MAX), precision=0, scale=0, NOT NULL

## Source key and relationship constraints
- DepositEditHistory.BuildingId -> Deposits.BuildingId; constraint=FK_DepositEditHistory_Deposits_BuildingId_DepositId; disabled=False; untrusted=False
- DepositEditHistory.DepositId -> Deposits.Id; constraint=FK_DepositEditHistory_Deposits_BuildingId_DepositId; disabled=False; untrusted=False
- Deposits.BuildingId -> Apartments.BuildingId; constraint=FK_Deposits_Apartments_BuildingId_ApartmentId; disabled=False; untrusted=False
- Deposits.ApartmentId -> Apartments.Id; constraint=FK_Deposits_Apartments_BuildingId_ApartmentId; disabled=False; untrusted=False
- Deposits.BuildingId -> Buildings.Id; constraint=FK_Deposits_Buildings_BuildingId; disabled=False; untrusted=False
- Expenses.BuildingId -> Buildings.Id; constraint=FK_Expenses_Buildings_BuildingId; disabled=False; untrusted=False
- Expenses.BuildingId -> FinancialCategories.BuildingId; constraint=FK_Expenses_FinancialCategories_BuildingId_CategoryKind_CategoryId; disabled=False; untrusted=False
- Expenses.CategoryKind -> FinancialCategories.Kind; constraint=FK_Expenses_FinancialCategories_BuildingId_CategoryKind_CategoryId; disabled=False; untrusted=False
- Expenses.CategoryId -> FinancialCategories.Id; constraint=FK_Expenses_FinancialCategories_BuildingId_CategoryKind_CategoryId; disabled=False; untrusted=False
- MonthlyApartmentShares.BuildingId -> Apartments.BuildingId; constraint=FK_MonthlyApartmentShares_Apartments_BuildingId_ApartmentId; disabled=False; untrusted=False
- MonthlyApartmentShares.ApartmentId -> Apartments.Id; constraint=FK_MonthlyApartmentShares_Apartments_BuildingId_ApartmentId; disabled=False; untrusted=False
- MonthlyApartmentShares.BuildingId -> Buildings.Id; constraint=FK_MonthlyApartmentShares_Buildings_BuildingId; disabled=False; untrusted=False
- Revenues.BuildingId -> Buildings.Id; constraint=FK_Revenues_Buildings_BuildingId; disabled=False; untrusted=False
- Revenues.BuildingId -> FinancialCategories.BuildingId; constraint=FK_Revenues_FinancialCategories_BuildingId_CategoryKind_CategoryId; disabled=False; untrusted=False
- Revenues.CategoryKind -> FinancialCategories.Kind; constraint=FK_Revenues_FinancialCategories_BuildingId_CategoryKind_CategoryId; disabled=False; untrusted=False
- Revenues.CategoryId -> FinancialCategories.Id; constraint=FK_Revenues_FinancialCategories_BuildingId_CategoryKind_CategoryId; disabled=False; untrusted=False
- WalletAdjustments.BuildingId -> Apartments.BuildingId; constraint=FK_WalletAdjustments_Apartments_BuildingId_ApartmentId; disabled=False; untrusted=False
- WalletAdjustments.ApartmentId -> Apartments.Id; constraint=FK_WalletAdjustments_Apartments_BuildingId_ApartmentId; disabled=False; untrusted=False
- WalletAdjustments.BuildingId -> Buildings.Id; constraint=FK_WalletAdjustments_Buildings_BuildingId; disabled=False; untrusted=False

## Finance source aggregate profile
### Deposits by BuildingId / MonthKey
- Building=zxhRd7cwZ07rZotXeQ17; Month=2026-09; Rows=20; AmountTotal=11475.0000
- Building=ZzgYia4qMZ23N8qhiCOV; Month=2026-09; Rows=1; AmountTotal=16780.0000
- Building=ZzgYia4qMZ23N8qhiCOV; Month=2026-10; Rows=5; AmountTotal=6712.0000
### Expenses by BuildingId / MonthKey
- Building=LmkUDOJcjlvjiGmluz6c; Month=2026-10; Rows=8; AmountTotal=4700.0000
- Building=ZzgYia4qMZ23N8qhiCOV; Month=2026-09; Rows=1; AmountTotal=900.0000
- Building=ZzgYia4qMZ23N8qhiCOV; Month=2026-10; Rows=3; AmountTotal=2700.0000
### Revenues by BuildingId / MonthKey
- Building=LmkUDOJcjlvjiGmluz6c; Month=2026-10; Rows=1; AmountTotal=27173.0000
- Building=ZzgYia4qMZ23N8qhiCOV; Month=2026-09; Rows=1; AmountTotal=1800.0000
- Building=ZzgYia4qMZ23N8qhiCOV; Month=2026-10; Rows=2; AmountTotal=4500.0000
### MonthlyApartmentShares by BuildingId / MonthKey
- Building=LmkUDOJcjlvjiGmluz6c; Month=2026-10; Rows=60; ExpenseShares=4700.2200; RevenueShares=27173.0400; CarryOver=0.0000
- Building=zxhRd7cwZ07rZotXeQ17; Month=2026-09; Rows=15; ExpenseShares=0.0000; RevenueShares=0.0000; CarryOver=0.0000
- Building=zxhRd7cwZ07rZotXeQ17; Month=2026-10; Rows=15; ExpenseShares=0.0000; RevenueShares=0.0000; CarryOver=3319.0000
- Building=zxhRd7cwZ07rZotXeQ17; Month=2026-11; Rows=15; ExpenseShares=0.0000; RevenueShares=0.0000; CarryOver=1519.0000
- Building=ZzgYia4qMZ23N8qhiCOV; Month=2026-09; Rows=4; ExpenseShares=900.0000; RevenueShares=1800.0000; CarryOver=0.0000
- Building=ZzgYia4qMZ23N8qhiCOV; Month=2026-10; Rows=4; ExpenseShares=2700.0000; RevenueShares=4500.0000; CarryOver=17030.0000
### WalletAdjustments by BuildingId / MonthKey
- Building=ZzgYia4qMZ23N8qhiCOV; Month=2026-11; Rows=2; AmountTotal=200.0000
### Deposits by status
- Status=cancelled; Rows=16; AmountTotal=6356.0000
- Status=confirmed; Rows=10; AmountTotal=28611.0000

## Migration blockers / interpretation
- This phase is a read-only structural and aggregate profile; it does not perform a backfill.
- Phase08 currently supplies target schema, not migrated finance business rows.
- Do not synthesize FinancialPeriods, UTC timestamps, actor accounts, confirmed-deposit posting metadata, opening balances, or allocation lines until their legacy semantics are proven.
- Preserve exact source keys, values, and timestamps; use FinancialSourceLinks for idempotent source identity when the approved mapping is ready.
- Existing documented share variances and rounding behavior must be classified before any allocation or wallet projection is generated.
- DepositEditHistory requires a separate immutable-history mapping; it must not be discarded or folded into current deposit state.

## Safety
- No financial source or target rows were inserted, updated, or deleted.
- No balances were recalculated.
- No credentials or personal field values were selected.
