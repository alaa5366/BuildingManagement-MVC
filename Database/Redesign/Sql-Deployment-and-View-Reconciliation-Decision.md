# SQL Deployment and View Reconciliation Decision

**Date:** 2026-11-18  
**Branch:** development/database-redesign-enterprise  
**Status:** Technical decision proposal; owner confirmation is required before changing deployment artifacts.

## 1. Confirmed source evidence

The reviewed branch contains these facts:

1. Data/AppDbContext.cs maps ApartmentWalletMovementsView as keyless and calls ToView("vw_ApartmentWalletMovements").
2. Migrations/AppDbContextModelSnapshot.cs also records the view mapping.
3. The inspected EF migration source files create ordinary tables/indexes but do not contain a corresponding CREATE VIEW vw_ApartmentWalletMovements operation.
4. BuildingManagement-FullSchema.sql contains the view definition.
5. The supplied restored-database view inventory reported zero SQL views, including vw_ApartmentWalletMovements.
6. The view definition calculates confirmed deposits plus wallet adjustments only. Its own SQL comment states that full wallet balance logic remains in WalletService; it is not a replacement for the complete wallet calculation.

Therefore EF model metadata, migration deployment, the FullSchema artifact, and the restored database are not currently a single consistent source of truth for this view.

## 2. Recommended deployment policy

**Recommendation: EF Core migrations are the authoritative, versioned schema deployment history for the application.**

Under this policy:

- Tables, indexes, constraints, views, triggers, and other application-owned SQL objects are created/changed through versioned EF migrations.
- A SQL view is created with explicit migration SQL (or a migration helper) and is included in migration Up/Down behavior.
- Deployment SQL scripts are generated from the migration chain for the target environment; operators do not independently apply a second hand-maintained schema script.
- BuildingManagement-FullSchema.sql becomes a generated/reference bootstrap artifact, not a competing authoritative schema definition.
- A CI/static validation checks that all mapped views exist in the migration deployment chain and that fresh-database migration produces the same object inventory.
- The restored database must be built from the same approved migration path before application smoke tests.

This is a recommendation, not an executed change.

## 3. Alternative if the deployment owner requires FullSchema as authority

If the actual deployment process is intentionally FullSchema-first, then:
- FullSchema must become the single versioned authoritative deployment artifact;
- EF migrations must be generated/reconciled to match it, or the application must explicitly stop treating EF migrations as an independent deployment path;
- drift detection must compare the live database to the FullSchema manifest;
- fresh restore and upgrade paths must both be tested.

Do not keep both paths independently editable.

## 4. View contract

Before deployment, verify:
- exact columns and SQL types returned by the view match ApartmentWalletMovementsView;
- Status = N'confirmed' is the correct persisted status value for all supported legacy rows;
- WalletAdjustments are intended to be included in NetMovements;
- no monthly fee, expense/revenue allocation, carryover, or closed-apartment logic is implied by this view;
- performance plans are acceptable for representative data and the view is not used as an unbounded aggregate read path;
- permissions to query the view do not bypass application-level building/apartment scope.

The view must be named and documented as a movement subtotal, not as a full wallet balance.

## 5. Required work items

1. Locate the canonical view definition and verify it against the live restore definition (currently absent).
2. Compare all view output columns to the mapped EF keyless type.
3. Add a versioned migration creating the view only after deployment authority is approved.
4. Add a matching Down operation that drops only the view introduced by that migration.
5. Generate a migration script for a fresh database and an upgrade database.
6. Apply both paths only on disposable restores and compare sys.views, definitions, dependencies, and query results.
7. Ensure FullSchema is generated from or validated against the migration chain.
8. Add a test/CI guard for any ToView mapping without a deployment definition.
9. Reconcile any existing environment where the migration history says current but the view is absent; repair only through a reviewed deployment operation.

## 6. Owner decision

**DEC-28:** Which artifact is authoritative for application-owned SQL schema deployment?

- **Option A (recommended):** EF Core migrations are authoritative; FullSchema is generated/reference-only.
- **Option B:** FullSchema is authoritative; EF migration deployment is reconciled or retired as an independent path.

Recommendation: Option A, because the repository already uses EF migrations and the missing view is precisely a case where an untracked SQL object creates drift. The decision affects deployment and CI policy, so it must be confirmed by the project owner.

## 7. Safety status

No migration was created or applied. No SQL view was created, dropped, or altered. No database was changed. No build or test was run. Gate A remains NOT AUTHORIZED.
