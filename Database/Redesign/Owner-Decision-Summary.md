# Owner Decision Summary — Approved Defaults

**Date:** 2026-11-18  
**Branch:** development/database-redesign-enterprise  
**Status:** The project owner approved the recommended defaults for DEC-01 through DEC-46. The complete binding record is [Owner Approval — Recommended Decision Defaults](Owner-Approval-Recommended-Defaults-2026-11-18.md).

## What this approval means

- Use the approved defaults as the baseline for architecture, domain contracts, implementation plans, and acceptance criteria.
- Do not reopen these decisions during routine implementation. A deviation requires a numbered decision change and renewed owner approval.
- Evidence-dependent decisions remain blocked until their evidence is available. Approval does not invent missing identity proof, ownership records, permission consumers, deployment topology, or accounting facts.
- DEC-15 preserves current wallet semantics during transition; it does not authorize a new accounting model or finance cutover.
- DEC-32/33 require configurable, versioned eligibility per poll; they do not impose one universal voter class.
- DEC-38 approves operational ballot separation only, not cryptographic unlinkability or certification.
- DEC-28 selects EF Core migrations as the authoritative SQL deployment path; FullSchema is generated/reference-only. View deployment reconciliation remains required.

## Execution authorization boundary

**Gate A remains NOT AUTHORIZED.** This decision approval does not authorize DDL, EF migrations, database changes, backfill, identity merges, credential rewrites, financial mutations, production changes, or cutover. Those require their own explicit execution approval and rehearsed gate.

Continue read-only reviews and design consolidation without waiting for another decision prompt.

## Full-project change authorization

The owner has explicitly authorized full-project redesign and implementation across all application layers and domains, including source code, persistence/migrations, tests, operations, and documentation. Routine per-file approvals are not required. The scope, safeguards, and separate production execution boundary are documented in [Owner Authorization — Full Project Redesign](Owner-Authorization-Full-Project-Redesign-2026-11-18.md).

Controlled schema/data migration testing is authorized only against a verified disposable restore/test database. Production/live changes, production deployment, irreversible cutover, and destructive actions on non-disposable data still require a specific execution approval. Gate A can be prepared and rehearsed on disposable restore; production application remains separately unauthorized.
