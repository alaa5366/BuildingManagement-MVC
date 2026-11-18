# Phase08 Deposit Resolution SQL Review

- Server/database gate: PASS ((localdb)\MSSQLLocalDB / BuildingManagement_RestoreTest)
- EF history gate: PASS (8 rows; latest 20261118224524_Phase08FinanceReversalScopeIntegrity; target absent)
- Existing CK_PaymentDeposits_Resolution: enabled and trusted
- Backup RESTORE VERIFYONLY WITH CHECKSUM: PASS
- Migration range: 20261118224524_Phase08FinanceReversalScopeIntegrity -> 20261118234143_Phase08_PaymentDepositResolutionReasonIntegrity
- Generated SQL SHA256: B2BE045776C149982B2E7A2E204F39778346AC4A30753191B483D02F3B95D0B2
- DROP CONSTRAINT: CK_PaymentDeposits_Resolution only
- ADD CONSTRAINT: CK_PaymentDeposits_Resolution only
- DROP TABLE/COLUMN, TRUNCATE, DELETE, UPDATE: none
- EF migration execution: NOT PERFORMED
- Production: NOT TARGETED

SQL replaces one check constraint only. Review report and SQL artifact before running the dedicated RestoreTest apply gate.
