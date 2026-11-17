# Phase06 – Index & Query Optimization Decision

**Status:** Accepted  
**Decision date:** 2026-11-17  
**Validation database:** `BuildingManagement_RestoreTest`  
**Production scope:** Explicitly excluded

## Evidence reviewed

The read-only diagnostic script `Diagnostics/Phase06_IndexQueryDiagnostics.sql` completed successfully against the TEST database and returned:

- Database/server capture details.
- Existing index definitions and usage counters.
- Foreign-key leading-column index coverage.
- Missing-index DMV recommendations.
- Table row counts.

The reported foreign keys all had an index beginning with the first foreign-key column. The supplied output did not contain actionable missing-index recommendations. The database contains a relatively small test data set, and index usage counters reflect only the observed test workload and DMV counter lifetime.

## Accepted decisions

1. Retain all existing indexes. Do not remove indexes based solely on zero or low seek counts from this test run.
2. Do not add indexes based solely on hypothetical or unvalidated recommendations.
3. Do not create a schema migration for index changes in Phase06.
4. Keep the diagnostic script read-only and retain its output as a baseline for later comparison.
5. Revisit index changes only when representative data volumes and workload measurements demonstrate a material benefit, considering both read and write costs.

## Candidate for future measurement

The resident lookup by `(BuildingId, ApartmentId)` orders results by `IsPrimary DESC, Name ASC`. A wider index including those ordering columns may be evaluated in a future performance test, but it is **not approved for implementation** in this phase.

## Acceptance boundary

This decision applies to the TEST database and the Phase06 repository work only. Production databases must not be modified as part of this phase.

## Outcome

**Phase06 is accepted as a diagnostics and assessment phase with no database schema changes.**
