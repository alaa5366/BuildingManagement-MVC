# Phase07 – Acceptance Decision

**Status:** Accepted  
**Decision date:** 2026-11-17  
**Branch:** `development/phase07-application-data-integrity`  
**Database scope:** TEST only (`BuildingManagement_RestoreTest`)  
**Production:** Explicitly excluded

## Evidence accepted

1. Read-only data-integrity diagnostics completed against the TEST database. All checks were clean except one RevenueDate/MonthKey difference; code review confirmed that transaction date and accounting month are intentionally independent in the application.
2. Cross-instance aggregate concurrency gap was corrected in `Data/BuildingSqlMapper.cs`: existing aggregates require a valid 8-byte rowversion, and every aggregate save forces a parent-row update in the same transaction so SQL Server's rowversion protects child-only changes.
3. User-reported local build succeeded.
4. User-reported test suite result: 407 passed, 0 failed, 0 skipped.
5. User-reported SQL Server rowversion smoke test result: PASS. The first conditional update affected one row; the stale update affected zero rows; the transaction was rolled back.

## Accepted decisions

- Keep the RevenueDate/MonthKey model independent; do not add a date-month equality constraint.
- Keep the concurrency correction using the existing Buildings.RowVersion; no schema migration is required.
- Preserve all TEST data. No cleanup or data repair was authorized or performed.
- No index changes were made under Phase07.
- Keep the read-only diagnostics and SQL Server rowversion smoke-test scripts as regression evidence.

## Scope and limitations

- This acceptance covers source changes, the reported local build/test run, and the TEST database diagnostics/smoke test.
- The SQL smoke test validates SQL Server's rowversion predicate behavior, but is not a full EF Core two-context aggregate integration test. That deeper test may be added as future hardening.
- Production was not accessed, changed, migrated, or validated. This decision grants no production deployment or migration authorization.

## Outcome

**Phase07 is accepted.** Proceed to the next development phase from this branch unless a new issue is identified.
