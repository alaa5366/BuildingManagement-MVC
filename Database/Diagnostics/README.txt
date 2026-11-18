BuildingManagement Enterprise Redesign â€” Aggregate Profiling
Generated: 2026-11-18T14:49:00.0342616+02:00
Server instance: (localdb)\MSSQLLocalDB
Database: BuildingManagement_RestoreTest
Result sets exported: 15
Total aggregate rows: 142

Privacy:
- The SQL emits aggregate counts, identifiers needed for reconciliation,
  and financial totals; it does not select names, phone numbers, emails, PINs,
  password hashes, API tokens, encrypted secret values, or recovery codes.
- BuildingId and ApartmentId appear in the apartment-level aggregation because
  they are required to reconcile tenancy counts. Pseudonymize them before
  sharing if they are considered sensitive in your environment.

Safety:
- Windows Integrated Security is used; no password is requested or saved.
- The runner is restricted to BuildingManagement_RestoreTest.
- The SQL batch contains SELECT/metadata queries only.
- No database objects or application data are changed.
