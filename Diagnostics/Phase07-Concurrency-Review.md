# Phase07 – Concurrency Review

**Status:** Implementation added; local build/test verification pending  
**Branch:** `development/phase07-application-data-integrity`  
**Database changes:** None  
**Migration:** None  
**Production:** Not accessed

## Confirmed architecture

- Building writes use a full in-memory aggregate followed by `SaveFullAsync`.
- MVC write requests are serialized per building within one application process by `BuildingWriteLockFilter` and `KeyedLock`.
- The `Buildings` table already has a SQL Server `rowversion` column, mapped as an EF Core concurrency token.
- `BuildingSqlMapper.LoadAsync` carries the rowversion into the domain `Building` aggregate.

## Defect identified

Before this review, the mapper compared the incoming rowversion with the current parent row, but a save that changed only child tables (for example, a deposit, revenue, notification, or poll vote) did not necessarily modify the `Buildings` row.

SQL Server only advances a rowversion when its row is updated. Consequently, two application instances could both load the same aggregate version, change child rows, and pass the pre-save comparison if the parent row had not changed. The rowversion check therefore did not reliably cover child-only aggregate writes across instances.

There was also a permissive path for existing buildings with a missing/empty rowversion: the comparison was skipped.

## Implemented correction

In `Data/BuildingSqlMapper.cs`:

1. Existing aggregates must carry an 8-byte SQL Server rowversion. Missing or stale tokens raise `DbUpdateConcurrencyException`.
2. Every existing aggregate save explicitly marks the parent `DataVersion` property modified, even when its value is unchanged.
3. This forces an UPDATE of the parent row in the same transaction as child changes. EF Core includes the original rowversion in the UPDATE predicate; SQL Server advances the rowversion on success.
4. If another application instance wins first, the competing parent UPDATE affects zero rows and EF Core raises `DbUpdateConcurrencyException`, rolling back the transaction, including child-table changes.
5. The caller must reload and retry; the mapper does not silently merge or overwrite a stale aggregate.

No schema change is required because `Buildings.RowVersion` already exists in the model, migration, and full schema script.

## Tests added

`Tests/BuildingManagement.Tests/BuildingSqlMapperConcurrencyTests.cs` covers:

- matching rowversion accepted;
- missing rowversion rejected;
- stale rowversion rejected;
- child-only save path forces the parent entity into Modified state.

The test suite must be run locally. The in-memory test verifies the forced-update state; an end-to-end multi-instance race still requires SQL Server integration testing to validate the database-generated rowversion behavior.

## Locking boundary

`BuildingWriteLockFilter` remains useful for reducing same-process contention and providing orderly MVC request execution. It is not a distributed lock and is not relied upon for cross-instance correctness.

Direct store/service paths that bypass the MVC filter remain protected when they use `BuildingSqlMapper.SaveAsync` with a valid aggregate token.

## Required verification

Run locally on the development branch:

```powershell
dotnet build
dotnet test Tests/BuildingManagement.Tests
```

Expected: build succeeds and all existing plus new tests pass. No database migration or TEST database modification is required for this code change.

For full integration confidence, run a two-context/two-process SQL Server test: load the same building twice, save a child-only change from the first context, then attempt to save the stale aggregate from the second. The second save must throw `DbUpdateConcurrencyException`, and its child changes must be rolled back.

## Decision boundary

The concurrency correction is implemented in source and documented, but Phase07 remains pending until the user runs build/tests and reports the result. No production change is authorized.
