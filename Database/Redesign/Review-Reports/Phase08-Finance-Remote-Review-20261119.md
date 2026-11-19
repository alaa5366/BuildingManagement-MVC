# Phase08 Finance Corrections — Remote Review

- Reviewed: 2026-11-19
- Branch reviewed: `development/database-redesign-enterprise`
- Review basis: the committed execution report `Phase08-Finance-Corrections-20261119-004612.md` and the committed model snapshot / design documentation.
- Review mode: read-only remote inspection. No database was changed and no source file was modified by this review.

## Confirmed findings

1. The execution report embeds the generated `Phase08FinanceReversalScopeIntegrity` migration source, but its `Source diff` section is empty. Therefore the report does not provide an auditable source diff for the four claimed configuration corrections.
2. The embedded migration changes both reversal foreign keys to the composite scope `(ReversalTransactionId, PeriodId, BuildingId)` / `(ReversesTransactionId, PeriodId, BuildingId)`.
3. The embedded target model snapshot contains the alternate key `UQ_LedgerTransactions_Period_Building` on `(LedgerTransactionId, PeriodId, BuildingId)`. This is the required principal candidate key for those composite foreign keys; the target-key concern is therefore satisfied by the snapshot represented in the report.
4. The migration's `Down` method restores the prior single-column foreign keys and indexes and restores the prior check-constraint expression. No table drop or data-copy operation appears in this correction migration.
5. The stricter `CK_PaymentDeposits_Resolution` expression enforces null reversal metadata for Pending, Confirmed, and Cancelled states, and requires reversal metadata plus a non-empty resolution reason for Reversed state.

## Verification limits / blockers

- The actual correction source file `Data/FinanceModelConfiguration.cs`, the finance entity source file, and the generated migration files are not present on the inspected GitHub branch at the reviewed ref. They were reported as local uncommitted/untracked files in the user's local Git status.
- Consequently, this remote review cannot verify the actual source implementation, generated migration designer consistency, build output, test output, or the local database migration state.
- The execution report's summary claims successful builds/tests/model alignment, but the report's empty source diff prevents independent source-to-migration reconciliation. Treat those claims as reported results, not independently reproduced results.
- The correction migration must not be applied to any database until the exact source and migration artifacts are published and reviewed, and the migration is exercised against a disposable RestoreTest database with a backup/restore plan.

## Required next handoff

Publish the exact local files (without applying migrations) to this branch or a dedicated review branch:
- `Data/FinanceModelConfiguration.cs`
- `Data/FinanceEntities.cs`
- `Data/FinanceModelConfiguration.cs` (corrected source)
- `Migrations/20261118224524_Phase08FinanceReversalScopeIntegrity.cs`
- `Migrations/20261118224524_Phase08FinanceReversalScopeIntegrity.Designer.cs`
- `Migrations/AppDbContextModelSnapshot.cs`
- `Tests/BuildingManagement.Tests/FinanceModelConfigurationTests.cs`

Then attach a compact machine-generated verification record containing the exact command, exit code, and test totals for build, tests, and `dotnet ef migrations has-pending-model-changes`.

## Disposition

**Remote structural review: partially passed.** The composite principal key and reversible FK/check-constraint operations are visible in the embedded migration/model snapshot. **Implementation approval: withheld pending publication and independent verification of the exact local source and migration artifacts.** No database update is authorized by this review.
