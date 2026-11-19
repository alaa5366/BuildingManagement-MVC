# Phase39 — Finance Command Validation

- Date: 2026-11-19
- Branch: `development/database-redesign-enterprise`
- Scope: validation at expense, revenue, and manual wallet-adjustment service boundaries.
- Data impact: application code and tests only. No database migration, SQL execution, or historical finance row mutation.

## Implemented

1. Added shared `FinanceCommandValidation` rules:
   - Month keys must be exact valid `yyyy-MM` calendar months.
   - Expense and revenue amounts must be positive and represent whole currency cents.
   - Transaction dates must be exact `yyyy-MM-dd` values and belong to the selected month.
   - A missing date defaults to the current UTC date; therefore it is accepted only when the selected month is the current UTC month. Historical-month entries must provide their actual date.
   - New expense/revenue entries must reference an active category in the same building.
2. Applied these checks in `ExpensesService.AddExpense` and `RevenuesService.AddRevenue` before a month is created or any collection is mutated.
3. Manual wallet adjustments now require:
   - an apartment belonging to the target building;
   - a valid month key;
   - a non-zero amount expressed to cents;
   - a non-empty reason;
   - a non-empty actor.
   Positive and negative adjustments remain supported.
4. `WalletController.AddAdjustment` now handles validation errors and returns feedback rather than allowing invalid input to escape as an unhandled exception.
5. Updated and added unit tests for valid and invalid amounts, malformed/mismatched dates, inactive categories, apartment membership, actor and reason requirements, and cent precision.

## Explicit non-changes

- No legacy finance data was normalized, deleted, or rewritten.
- Expense/revenue delete operations still physically remove aggregate entries. This remains a known auditability defect and must be replaced by a governed void/reversal workflow before ledger cutover; this phase does not pretend that problem is solved.
- Existing legacy records are not subjected to new-entry validation during read or migration.
- No ledger postings, allocation batches, financial periods, or balance changes were created.

## Verification

Run locally after pulling:
```powershell
git pull --ff-only
dotnet test
dotnet build --no-restore
```

The tests in this report have been authored but have not been executed by the assistant. The root solution originally omitted the test project, so `dotnet test` from the repository root built only the web application. Phase39 now adds the test project to `BuildingManagementMvc.slnx`; after pulling, run `dotnet test` again and confirm a `Passed` / `Failed` / `Total` summary before marking verification complete.
