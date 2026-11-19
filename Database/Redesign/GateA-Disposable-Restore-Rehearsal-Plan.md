# Gate A Disposable-Restore Rehearsal Plan

**Date:** 2026-11-18  
**Branch:** `development/database-redesign-enterprise`  
**Status:** Preparation plan only. **Do not execute Gate A from this document.**

## 1. Purpose

Define the controlled procedure that will be used only after:
- the complete metadata and FK parity matrix is reviewed;
- all blocking technical issues in the consistency review are closed or explicitly accepted;
- owner decisions affecting Gate A are recorded;
- the owner gives explicit Gate A execution approval.

The first rehearsal target remains a disposable restore of `BuildingManagement_RestoreTest`, never production.

## 2. Known baseline

From the supplied Gate A preflight results:
- SQL Server: `(localdb)\\MSSQLLocalDB`
- Database: `BuildingManagement_RestoreTest`
- All 13 proposed Gate A tables were absent at the captured preflight.
- Existing Buildings.Id, Apartments.BuildingId, and Apartments.Id were reported as non-null `nvarchar(64)`, 128 bytes, with matching `SQL_Latin1_General_CP1_CI_AS` collation.
- Required legacy PK/index structures were present in the preflight.
- This is metadata evidence only; it does not authorize DDL and does not replace a fresh pre-execution check.

## 3. Hard stop conditions

The rehearsal must stop before DDL if any of the following is true:

1. Database name is not exactly `BuildingManagement_RestoreTest`.
2. SQL instance is not the approved local disposable instance.
3. Current Git branch is not `development/database-redesign-enterprise`.
4. Working tree or Git index has unrelated staged/unstaged changes, unless the script is explicitly designed to preserve them without staging.
5. The restore is not confirmed disposable and restorable.
6. Any of the 13 Gate A table names already exists in a partial set.
7. A full set exists but its schema/constraints do not match the reviewed expected schema.
8. Legacy key types, lengths, collation, nullability, or FK key order differ from the latest approved metadata.
9. Any source table or row count differs from the frozen baseline without a documented reason.
10. Any startup application path may mutate legacy PIN data before evidence is captured.
11. The operator has not received explicit approval for this exact rehearsal run.

No credential prompt, SQL password, or secret should be included in the script. Use Integrated Security only.

## 4. Pre-execution evidence bundle

Generate a timestamped folder under `Database/Diagnostics/GateA-Rehearsal-Results/<timestamp>/` containing:

- `00-README.txt` — instance, database, branch, script version/hash, operator confirmation, and scope.
- `01-Database-Identity.csv` — database name, server/instance, SQL version, compatibility level, recovery model, read-only state.
- `02-Existing-GateA-Objects.csv` — all 13 target object names and existence.
- `03-Legacy-Key-Metadata.csv` — exact SQL types, max length, collation, nullability, key ordinal for Buildings/Apartments keys.
- `04-Legacy-FK-Metadata.csv` — FK columns, referenced columns, update/delete actions, trust/disabled state.
- `05-EF-Migration-History.csv` — migration IDs and product versions.
- `06-View-Inventory.csv` — view definitions, dependencies, and object state.
- `07-Source-Counts.csv` — row counts for identity, apartment, finance, poll, audit, and relationship tables.
- `08-Constraint-Inventory.csv` — PK, unique, check, default, and FK constraints.
- `09-Index-Inventory.csv` — index definitions and filters for touched legacy tables.
- `10-Startup-Mutation-Guard.txt` — source/config evidence that legacy startup PIN mutation is disabled for the rehearsal.

Never export credential hashes, PINs, TOTP secrets, API secrets, raw personal data, receipt contents, or row-level financial details into Git diagnostics.

## 5. Script behavior requirements

The eventual local PowerShell rehearsal script must:

- use `Microsoft.Data.SqlClient` or the installed SqlServer module with Integrated Security;
- assert the exact database and instance before any write;
- check branch and Git cleanliness before generating results;
- run read-only preflight and export each result set to a separate CSV;
- compare against the approved metadata manifest and stop on mismatch;
- require an explicit operator confirmation token/phrase in the local console only after all checks pass;
- execute only the reviewed Gate A SQL file and only after a separate approval gate is recorded;
- wrap DDL in the SQL transaction already defined in the reviewed script;
- capture SQL errors and transaction outcome without logging secrets;
- run postflight checks and compare exact table/column/index/FK/check-constraint inventory;
- prove legacy tables and row counts are unchanged;
- never run a backfill or application startup migration;
- stage only its own timestamped diagnostics folder and the explicitly approved result manifest;
- commit/push only after the operator confirms the generated report contains no sensitive values.

The script must not auto-pull, auto-merge, reset, clean, or stage unrelated repository changes.

## 6. Post-execution acceptance checks

A rehearsal is successful only if all are true:

1. Exactly 13 new Gate A tables exist with reviewed columns and SQL types.
2. All expected PK, unique, FK, CHECK, and default constraints exist and are trusted/enabled.
3. Every legacy FK uses exact target key type/length/collation and correct composite key order.
4. No legacy table, column, index, row, or value was modified.
5. No source data was inserted into Persons/UserAccounts/relationships.
6. The transaction committed once; a forced failure rehearsal rolls back all newly created objects.
7. A second idempotent run is tested only after schema parity verification; it must not silently accept drift.
8. EF Core model/migrations are still unchanged at this stage.
9. The application is not started against the evidence-preserving restore until the startup PIN mutation is safely disabled.
10. The postflight package is committed to the approved branch and reviewed before any next step.

## 7. Rollback

- The primary rollback for the first rehearsal is discard the disposable database and recreate it from the frozen restore.
- Do not use ad hoc DROP statements against any non-disposable database.
- If a transaction fails, confirm no Gate A objects were committed.
- If a committed rehearsal is later rejected, restore the disposable database from the pre-rehearsal backup rather than attempting manual object-by-object deletion.
- Preserve the result package and failure evidence for review.

## 8. Current status and authorization

- The proposed SQL file includes a guard against a partial pre-existing set of the 13 Gate A tables.
- A complete existing set still requires independent schema-parity validation; existence alone is not proof of correctness.
- The current supplied preflight reported all 13 tables absent, but a fresh pre-execution run is mandatory.
- No PowerShell rehearsal script has been issued by this plan.
- No DDL has been executed.
- No Gate A authorization has been granted.
- **Gate A remains NOT AUTHORIZED.**

The next safe task is to finish the Gate A column/constraint parity matrix and close the remaining design blockers. The owner may review the decision register while this independent work continues.
