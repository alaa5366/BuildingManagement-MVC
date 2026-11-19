# Owner Authorization — Full Project Redesign and Change Scope

**Date:** 2026-11-18  
**Repository:** `alaa5366/BuildingManagement-MVC`  
**Branch:** `development/database-redesign-enterprise`  
**Status:** Approved by project owner.

## Authorization

The owner explicitly authorizes a **full-project redesign and implementation**, not only documentation or isolated fixes. The approved scope covers the entire application and its supporting architecture, including:

- all domain models, entities, DTOs, value objects, contracts, validation, and domain rules;
- all Controllers, Services, repositories, middleware, background workers, and dependency injection;
- all Views, UI flows, client-side behavior, accessibility, and localization;
- authentication, Person/UserAccount lifecycle, credentials, MFA, authorization, permission catalog, and access grants;
- finance, deposits, expenses, revenues, apartment wallets, ledger, periods, reporting, and reconciliation;
- polls, voter eligibility, ballot secrecy, delegation, and audit evidence;
- maintenance, notifications, presence, QR access, cameras, DVR, MediaMTX, and HLS authorization;
- backups, restore, retention, audit, application logs, health, observability, platform security, and integrations;
- database schema design, EF Core models/configurations/migrations, indexes, constraints, SQL views, seed/reference data, and data migration tooling;
- automated tests, integration tests, security tests, performance tests, diagnostics, deployment/runbooks, and technical documentation.

The implementation may replace or remove legacy components when the approved target design and migration path are ready. Compatibility must be preserved only where required for safe transition, evidence retention, or an explicitly approved business rule. Do not retain legacy design merely because it exists.

## Execution rules

1. Continue end-to-end without waiting for routine approvals on individual files, classes, services, controllers, views, tests, or documentation. Use the approved DEC-01 through DEC-46 defaults as binding requirements.
2. Work in coherent, reviewable phases on `development/database-redesign-enterprise` (or a dedicated child branch when a phase needs isolation). Keep commits focused and document phase acceptance criteria.
3. Build and test changes when the required environment is available. Report exact commands/results and distinguish verified results from static review. Never claim a build, test, migration, restore, or runtime validation that was not actually performed.
4. Create and validate EF migrations and data-migration tooling as project artifacts. Rehearse schema/data changes only against disposable restore/test databases after preflight verification.
5. Preserve legacy financial, identity, audit, ballot, and ownership evidence until explicit mapping, reconciliation, retention, and rollback criteria are met. No silent deletion, identity merge, balance rewrite, or evidence loss.
6. Do not expose secrets in source control, logs, DTOs, views, reports, or diagnostic output. Do not request or commit credentials.
7. Maintain rollback plans, backup checkpoints, idempotency, concurrency controls, and operational observability for every stateful cutover.

## Environment and irreversible-operation boundary

This authorization covers full-project source-code changes and the creation of database migration/design artifacts. It also authorizes controlled execution and testing against a verified **disposable restore/test database**, with strict database-name and environment guards.

It does **not** authorize changes to a production/live customer database, production deployment, destructive operation on non-disposable data, irreversible identity/credential/financial cutover, or external service/device changes. Those remain separate execution gates requiring a specific environment, impact, backup/rollback plan, and explicit approval immediately before execution.

**Gate A:** The design and implementation work may proceed, including preparing and validating its migration against a disposable restore. Applying Gate A to any non-disposable or production database remains unauthorized without a separate explicit gate approval.

## Definition of done

A redesigned domain is not complete merely because code compiles. Each phase must include:
- approved domain contract and invariants;
- authorization and tenant/building/apartment isolation tests;
- persistence constraints and concurrency behavior;
- migration/backfill/reconciliation and rollback plan where applicable;
- audit, privacy, retention, and operational behavior;
- unit/integration/security/performance tests appropriate to risk;
- documented known limitations and verified evidence.

This authorization supersedes any prior interpretation that the project is limited to read-only review or documentation-only work. The owner-approved DEC defaults remain in force; changing a default still requires a numbered decision amendment.
