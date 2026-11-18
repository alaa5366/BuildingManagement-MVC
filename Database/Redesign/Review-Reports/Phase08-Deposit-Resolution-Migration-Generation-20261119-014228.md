# Phase08 Deposit Resolution Constraint Migration Generation

- Timestamp: 2026-11-19 01:42:28
- Branch: development/database-redesign-enterprise
- EF CLI: Entity Framework Core .NET Command-line Tools 8.0.10
- Migration: 20261118234143_Phase08_PaymentDepositResolutionReasonIntegrity.cs
- Migration designer: 20261118234143_Phase08_PaymentDepositResolutionReasonIntegrity.Designer.cs
- Model snapshot: Migrations/AppDbContextModelSnapshot.cs
- Generated Up/Down constraint replacement source check: PASS
- Build: PASS (dotnet build)
- Tests: PASS (dotnet test)
- Database connection/update/migration execution: NOT PERFORMED
- Production database: NOT TARGETED

## Scope

The model now requires ResolutionReason to be NULL for Pending and Confirmed deposits, and non-empty for Cancelled and Reversed deposits. The generated migration replaces only CK_PaymentDeposits_Resolution.

No database was updated. SQL generation and RestoreTest application remain separate gated steps.

## Build summary

Build output (last 18 lines):
System.Object[]

## Test summary

Test output (last 24 lines):
System.Object[]
