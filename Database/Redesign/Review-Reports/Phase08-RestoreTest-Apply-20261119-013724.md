# Phase08 RestoreTest Migration Apply Report

- Timestamp: 2026-11-19 01:37:24
- Server: (localdb)\MSSQLLocalDB
- Database: BuildingManagement_RestoreTest
- SQL artifact: Database/Redesign/Generated-Sql/Phase08-20261119-012912.sql
- SQL SHA256: 93FB742951A1EA322419B0358E792757B0B4DED500AB01E407DAA42DFB858005
- Backup: C:\Users\alaa5\AppData\Local\BuildingManagement\Backups\BuildingManagement_RestoreTest-Phase08-20261119-011703.bak
- Backup RESTORE VERIFYONLY WITH CHECKSUM: PASSED immediately before execution
- Executed GO-separated batches: 60 / 60
- SQL batch execution completed: True
- Post-apply verification: True
- Migration 1 history row: True
- Migration 2 history row: True
- Finance tables present: 9 / 9
- Both composite reversal FKs enabled and trusted: True
- Payment-deposit resolution check enabled and trusted: True
- Business data rows copied/updated/deleted: NONE
- Production database: NOT TARGETED

## Migration history after execution
- 20261116120818_InitialCreate
- 20261117012657_Phase04_StandardizeKeyColumnTypes
- 20261117050000_Phase05_RemoveRedundantResidentIndexes
- 20261117130000_AddUserTwoFactor
- 20261118182110_Phase06_IdentityAuthorizationFoundation
- 20261118184850_Phase07_OwnershipRecordProvenance
- 20261118213732_Phase08_FinancialLedgerFoundation
- 20261118224524_Phase08FinanceReversalScopeIntegrity

## Finance tables after execution
- FinancialAllocationBatches
- FinancialAllocationLines
- FinancialPeriods
- FinancialReconciliationDifferences
- FinancialReconciliationRuns
- FinancialSourceLinks
- LedgerEntries
- LedgerTransactions
- PaymentDeposits

## Disposition
**PASS** â€” both Phase08 migrations are present and the required schema objects are enabled/trusted on the disposable RestoreTest database.
