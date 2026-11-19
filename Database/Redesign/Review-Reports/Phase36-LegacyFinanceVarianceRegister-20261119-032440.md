# Phase 36 - Legacy finance variance register

- Run: 2026-11-19 03:24:40 (+02:00)
- Mode: APPLY (idempotent verification; no-op because the deterministic registration already existed)
- Database: BuildingManagement_RestoreTest

## Verified reconciliation differences

| Difference | Source amount | Existing apartment shares | Difference | Classification |
|---|---:|---:|---:|---|
| Expenses | 4,700.00 | 4,700.22 | +0.22 | LegacyRoundingVariance |
| Revenues | 27,173.00 | 27,173.04 | +0.04 | LegacyRoundingVariance |

- Registration state: ALREADY REGISTERED.
- Verified reconciliation difference rows: 2.
- The exact legacy building/month scope was resolved from source aggregates; no apartment-level attribution was inferred.
- No duplicate rows were created.

## Disposition and safeguards

- Preserve both differences as separate reconciliation items.
- Do not allocate either variance to apartments.
- No ledger posting, wallet adjustment, balance mutation, or legacy source correction was performed.
- This phase does not migrate financial business records into the target ledger.
