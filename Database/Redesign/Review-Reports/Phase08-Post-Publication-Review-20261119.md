# Phase08 Post-Publication Review — 2026-11-19

- Branch: `development/database-redesign-enterprise`
- Reviewed commit: `e754736913db24e12cf5ac11228f6c1e520a7e46`
- Review mode: read-only source and migration inspection.
- Database changes: none. No migration was applied.

## Verified execution evidence

The committed local publication report records:
- `dotnet build --no-restore`: exit 0, 0 warnings, 0 errors.
- `dotnet test`: exit 0, 550 passed, 0 failed, 0 skipped.
- `dotnet ef migrations has-pending-model-changes`: exit 0, no pending model changes.
- Database update: not run.

These are accepted as machine-recorded local execution results; they were not independently rerun in this review environment.

## Findings

### F08-01 — Target resident authentication does not select a credential purpose
**Severity: High — resolve before enabling broader target-account login.**

`IdentityAccountAuthenticationService.AuthenticateResidentAsync` selects the latest non-revoked, non-expired credential for an account without filtering `CredentialType`. The target model explicitly permits credential types 0 and 1. Authentication must select the intended interactive password/PIN purpose explicitly; ordering by creation time is not a substitute for purpose selection. Add tests with both credential types present and prove that a newer credential of the other purpose cannot silently change the login secret.

### F08-02 — Target resident session validation does not re-check effective role assignment
**Severity: High — resolve before relying on immediate role revocation.**

`SessionRevalidator.ValidateTargetIdentityAccountAsync` currently checks account status, person archive state, and the security-stamp claim. It does not re-check that an active, non-revoked, currently effective `resident` role assignment still exists. Revoking or expiring that assignment alone therefore does not invalidate an already-issued cookie unless another operation also rotates the account security stamp. Make role assignment validation part of target-session validation and ensure role/credential/relationship changes rotate the stamp transactionally.

### F08-03 — Target login collapses a multi-apartment Person to one arbitrary occupancy
**Severity: Medium — resolve before multi-apartment resident rollout.**

The target identity model allows a Person to have multiple effective apartment relationships, but `AuthenticateResidentAsync` selects one occupancy and returns only one building/apartment context. This is not a complete implementation of the approved multi-apartment relationship model. Define an explicit context-selection flow or a safe multi-context principal; do not silently choose one context as the person's only access.

### F08-04 — Finance schema is a foundation, not an operational ledger
**Severity: High for financial cutover; expected scope limitation for this work package.**

The published finance entities/configuration establish tables, keys, checks, indexes, and restrictive relationships. The reviewed files do not yet establish the transactional posting, idempotent command, immutable-after-posting enforcement, deterministic allocation, period-close/reopen workflow, or reconciliation execution services. These must be implemented and tested before any finance workflow is exposed or any target ledger is treated as authoritative.

### F08-05 — Reversal-scope correction migration is structurally coherent
**Severity: Informational / positive.**

The correction migration replaces the single-column reversal foreign keys with composite keys scoped by transaction ID, period, and building. The model defines the matching alternate key. Its `Down` method restores the prior single-column foreign keys and indexes and the prior deposit-resolution check. No table drop or data-copy operation appears in this correction migration.

## Required next work

1. Correct F08-01 and F08-02 with focused tests; address F08-03 as part of the resident context workflow.
2. Keep finance schema additive and legacy tables authoritative. Implement the finance command/reconciliation layer only after the accounting policy decisions and parity fixtures are accepted.
3. Before any EF migration rehearsal, verify the exact `BuildingManagement_RestoreTest` identity, capture and verify a restorable backup, inspect migration history and target-object absence/partial state, then apply only to that disposable restore and record before/after evidence.
4. Do not apply any Phase08 migration to production or any other data-bearing database.

## Disposition

**Build/test/model-alignment evidence: passed as recorded.**  
**Source review: conditional; identity-session findings remain open.**  
**Finance schema implementation: reviewable additive foundation, not approved for operational use or cutover.**  
**Database migration application: not performed and remains blocked pending the RestoreTest preflight/backup/rehearsal gate.**
