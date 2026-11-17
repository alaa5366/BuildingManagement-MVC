# Phase07 – Concurrency Review

**Status:** Implementation added; local build and unit tests passed (407/407); SQL Server engine smoke test added  
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

Local verification reported by the user on 2026-11-17: `dotnet build` succeeded and `dotnet test Tests/BuildingManagement.Tests` passed all 407 tests (0 failed, 0 skipped). The in-memory test verifies the forced-update state; SQL Server engine behavior is checked separately by `Diagnostics/Phase07-RowVersion-EngineSmokeTest.sql`.

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

Run `Diagnostics/Phase07-RowVersion-EngineSmokeTest.sql` against `BuildingManagement_RestoreTest`. It performs two conditional updates against one real `Buildings` row using the same original rowversion; the first must affect one row, the stale second update must affect zero rows, and the transaction is rolled back. This validates SQL Server's real rowversion predicate behavior without persisting changes. It is an engine smoke test, not a full EF Core two-context aggregate integration test; that remains a possible higher-confidence follow-up.

## Decision boundary

The concurrency correction is implemented; local build and all 407 unit tests passed. Phase07 remains pending the TEST-database rowversion smoke test and final review. No production change is authorized.
