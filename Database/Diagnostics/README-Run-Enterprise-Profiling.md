# Run the enterprise database profiling (LocalDB)

This is the next read-only evidence step for Gate A. It is intentionally limited to the disposable local database **BuildingManagement_RestoreTest**.

## Files

- `DatabaseRedesign-ReadOnly-Profiling.sql` — reviewed aggregate-only SQL. It checks the target database name and stops if the active database is different.
- `Run-DatabaseRedesign-Profiling.ps1` — executes the SQL using Windows Integrated Authentication and exports every result set into a separate CSV file.

## Run

Open PowerShell in this folder and run:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Run-DatabaseRedesign-Profiling.ps1
```

Defaults:

- Server: `(localdb)\MSSQLLocalDB`
- Database: `BuildingManagement_RestoreTest`

The runner refuses a different database name. It does not request or save a password. It creates a timestamped folder under `DatabaseRedesign-Profiling-Results` and exports each SQL result set as `01-Profile-Result.csv`, `02-Profile-Result.csv`, etc.

## Safety and privacy

- The SQL contains SELECT/metadata statements only; it does not create or alter objects or modify application rows.
- It returns aggregate identity counts, apartment relationship counts, financial totals, poll integrity counts, camera/DVR scope counts, secret-presence counts, wallet-view metadata, FK actions, and migration history.
- It does not select names, phone numbers, email addresses, PINs, password hashes, API tokens, encrypted secret values, or recovery codes.
- BuildingId and ApartmentId are present in the apartment-level aggregate so relationships can be reconciled. Pseudonymize them before sharing if needed.
- The PowerShell runner uses a 5-second SQL lock timeout (set by the SQL batch), low deadlock priority, and a five-minute command timeout.

## Gate status

These profiling outputs are still required before Gate A can be approved for a disposable-database rehearsal. Do not run the additive Gate A DDL script yet. No production database work is authorized by this profiling step.
