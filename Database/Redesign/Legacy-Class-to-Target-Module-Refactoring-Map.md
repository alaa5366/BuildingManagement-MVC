# Legacy Class to Target Module Refactoring Map

**Date:** 2026-11-18  
**Branch:** `development/database-redesign-enterprise`  
**Status:** Static-source disposition plan based on the reviewed branch and Sections 1–31. Names are mapped only where observed in the source/review inventory. This is not a code-change list and does not authorize implementation.

## 1. Refactoring policy

The current code is a source of behavior and evidence, not a fixed architecture to preserve. Every class receives one of four dispositions:

- **Replace:** new application/domain design owns the behavior; legacy implementation is retired after cutover.
- **Split:** behavior is decomposed across domain, application, and infrastructure boundaries.
- **Adapt:** retain a useful adapter/driver behind a target port.
- **Retire:** remove unsupported/dead workflow after confirming no supported user path depends on it.

No legacy class should be retained merely to avoid changing a view or controller. During migration, compatibility adapters may call new use cases, but must not become a second source of business rules.

## 2. Identity, authentication and authorization

| Legacy class / area | Target disposition | Target owner(s) | Refactoring notes |
|---|---|---|---|
| `UsersService` | Split | Identity & Accounts | Separate Person profile, UserAccount lifecycle, credentials, role assignments, direct permission overrides, and account status. |
| `ResidentsService` | Split | Property Relationships + Identity | Resident row becomes source relationship evidence; separate Person, account, occupancy, ownership, access grant, and credential reset use cases. Do not map each resident row to a new Person automatically. |
| `SqlAuthService` | Replace | Authentication | One account-based authentication orchestrator; normalize identifiers only; never trim passwords/PINs. |
| `TwoFactorService` | Split | Authentication & Sessions | Separate enrollment, TOTP verification/replay state, recovery-code consumption, secret vault, and MFA policy. |
| `SessionRevalidator` | Replace | Authentication & Sessions | Central revocation/security-stamp/role-scope validation; explicit impersonation session type; fail closed on unknown roles. |
| `AdminPermissionAttribute` | Retire | Authorization | Replace claim-based filter with centralized policy evaluator and resource-scope authorization. |
| `HasPermissionAttribute` | Retire | Authorization | Same; role gates, permission scope, account state, and resource ownership evaluated together. |
| `AdminPermissions` | Split | Authorization + Web metadata | Separate stable permission catalog, localized labels, UI grouping, role templates, and endpoint/use-case policy mapping. BackendInfo is documentation metadata, not authorization. |
| `AdminManagementService` | Split | Identity + Authorization | Account lifecycle, scoped role grants, overrides, invitations, disable/revoke and permission review become separate use cases. |
| `ImpersonationService` / `ImpersonationController` | Replace | Privileged Access | Dedicated short-lived delegated session with target scope, reason, step-up MFA, server-side revocation, live initiator revalidation, and durable audit. |

## 3. Property and building operations

| Legacy class / area | Target disposition | Target owner(s) | Refactoring notes |
|---|---|---|---|
| `BuildingsService` | Split | Property + Building Administration + Read Models | Separate building lifecycle, apartment/floor lifecycle, building-admin assignments, relationship services, and summary queries. Retire broad `GetByIdAsync` aggregate hydration for screens that need projections only. |
| `BuildingSqlStore` / `BuildingSqlMapper` | Adapt then split | Infrastructure Persistence | Replace aggregate-per-building/N+1 loading with scoped repositories and dedicated query projections. Keep SQL mapping details out of Application/Domain. |
| `AdminResidentsController` | Replace HTTP adapter | Web / Property & Identity use cases | Thin endpoints for resident relationship changes, person/account actions, credential resets, disable/revoke. No direct aggregate logic. |
| `AdminAptsController` | Replace HTTP adapter | Web / Property | Apartment lifecycle and QR/access grants are separate use cases. Do not reset a shared apartment PIN as a proxy for individual account credentials. |
| `BuildingsController` | Replace HTTP adapter | Web / Property | Building/apartment/floor commands; QR PDF generation moves to QR/document delivery; scope enforced in Application. |
| `AdminsController` / `PermissionsController` | Replace HTTP adapters | Web / Identity & Authorization | Separate account management from role/permission policy administration. |

## 4. Finance, wallet and reporting

| Legacy class / area | Target disposition | Target owner(s) | Refactoring notes |
|---|---|---|---|
| `WalletService` | Split / replace | Financial Ledger | Deposit state transitions, wallet balance projection, adjustments, period rules, and allocation become distinct components. |
| `ExpensesService` | Replace | Financial Ledger | Validated command handlers, immutable postings, category/building/period checks, reversal instead of physical delete. |
| `RevenuesService` | Replace | Financial Ledger | Same ledger lifecycle and reversal semantics as expenses. |
| `ReportsService` | Replace | Finance Read Models | Purpose-built SQL projections with explicit period semantics; do not hydrate the entire building aggregate or label net revenue as wallet balance. |
| `BuildingSqlStore.SaveFullAsync` / aggregate reconciliation | Retire as universal write path | Persistence | Replace with aggregate-specific repositories and explicit commands; preserve concurrency and source provenance. |
| `WalletAdjustments` handling | Split | Financial Ledger | Dedicated command with reason, actor, limits, period, concurrency, and immutable audit. |
| `WalletService.CurrentMonthKey` | Replace | Financial Period | Use explicit business timezone and period service; no hidden dependence on server local month. |

## 5. Polls, maintenance, notifications and presence

| Legacy class / area | Target disposition | Target owner(s) | Refactoring notes |
|---|---|---|---|
| `PollsService` | Split | Polling & Secret Ballots | Poll lifecycle/policy, eligibility, delegation, credential issuance, ballot box, and tally are separate components. |
| `PollsController` | Replace HTTP adapter | Web / Polling | Admin management only; no voter identity/choice logging. |
| `ResidentPollsController` | Replace HTTP adapter | Web / Polling | Eligibility-driven vote command; do not log option choice or apartment-linked vote details for secret polls. |
| `MaintenanceService` | Split | Maintenance + Finance | Request workflow separated from cost posting; ledger source reference and reversal required. |
| `MaintenanceController` / `ResidentMaintenanceController` | Replace HTTP adapters | Web / Maintenance | Explicit resident visibility and admin scope; commands delegate to Application. |
| `NotificationsService` | Split | Notifications | Recipient resolution, dispatch, provider delivery, inbox/read state, and preferences separated. |
| `NotificationsController` | Replace HTTP adapter | Web / Notifications | Separate self-inbox read from building-wide dispatch permission. |
| `PresenceService` | Replace | Presence | Decide whether presence counts Person/account/device/apartment; handle multiple sessions without overwriting one apartment key. |

## 6. QR, capability tokens and import workflows

| Legacy class / area | Target disposition | Target owner(s) | Refactoring notes |
|---|---|---|---|
| `QrTokenStoreService` | Replace | QR & Capability Tokens | Verify target and policy before one-time consumption; atomic use limits, revocation, signed authorization fields, replay controls and redacted telemetry. |
| `QrController` | Replace HTTP adapter | Web / QR & Capability Tokens | Admin issuance and QR management; target scope resolved server-side, strict validity/use limits. |
| `QrAccessController` | Replace HTTP adapter | Web / QR & Capability Tokens + Sessions | Stable guest/resident session handoff; no synthetic resident UID that fails normal revalidation; no-store/referrer/rate-limit protections. |
| Quick QR actions in `BuildingsController` / `AdminAptsController` | Split | QR & Capability Tokens | Central issuer/verifier owns token rules; controllers only call use cases. |
| `ExcelImportService` | Split | Import & Migration Workbench | Staged batch, file digest, target binding, row-level validation, explicit preview/approval, no coercion/defaults, idempotent commit and reconciliation. |
| `MigrateFromApartmentsAsync` dormant path | Retire or implement as explicit migration use case | Import & Migration Workbench | Empty success result must not be surfaced as successful migration; require typed result and row-level evidence. |

## 7. Files, OCR, cameras and integrations

| Legacy class / area | Target disposition | Target owner(s) | Refactoring notes |
|---|---|---|---|
| `CloudinaryService` | Adapt or replace provider | Private Document Storage | Private/authenticated storage, signed short-lived download, orphan compensation, retention and access policy. |
| `TransactionParser` | Adapt | Receipt OCR | OCR is advisory extraction only; never confirms a deposit. |
| `ImageSniffer` | Replace/strengthen | Document Validation | Full decode, format allowlist, dimensions/pixel limits, frame count, decompression/processing limits. |
| `ExcelImportService` | Split | Import Workflows | Strict parsing, staged batch, explicit target, row-level errors; remove silent defaults/padding/truncation. |
| `DvrService` | Split | Camera & Media | Safe metadata DTOs, credential vault, SQL/media outbox orchestration, decommissioning state/retries. |
| `DvrsController` / `CamerasController` | Replace HTTP adapters | Web / Camera & Media | Resource-scope authorization and independently authorized media sessions. |
| `WhatsAppTemplateService` | Adapt behind provider port | Integrations | Separate template generation, dispatch permission, provider configuration, secret management and delivery outcome. |
| `SettingsService` | Split | Settings + Integration Secrets + Preferences | Read-only GET, write-only secrets, separate system/building/person preferences. |

## 8. Backup, audit, logs and runtime orchestration

| Legacy class / area | Target disposition | Target owner(s) | Refactoring notes |
|---|---|---|---|
| `BackupService` / `ScheduledBackupService` | Split | Backup & Restore | Orchestrator, artifact store, integrity verifier, retention reconciler, restore drill runner; typed outcomes. |
| `BackupScheduler` | Adapt | Background Operations | Durable lease if scale-out; prevent overlap; calendar scheduling must not skip short months. |
| `BackupFileProtector` | Adapt after cryptographic review | Security Infrastructure | Versioned authenticated encryption; reject invalid artifacts; no silent plaintext fallback for new backups. |
| `AuditLogger` / `IAuditLogger` | Replace | Durable Audit | Transactional outbox-backed audit; durable outcomes for identity, finance, security, impersonation and backup. |
| `AuditLogQueryService` | Adapt / replace query | Audit Read Model | Scoped bounded paging, stable ordering, safe CSV, export audit, no full scans. |
| `ApplicationLogQueryService` | Adapt | Observability | Keep diagnostic logs distinct from business audit; bounded indexed search and redaction. |
| `SqlMirrorHealth` / `BuildingSqlStore` | Adapt then retire where redundant | Infrastructure / Health | Health signals should be typed and safe; remove process-local state assumptions where deployment topology requires shared state. |
| `Program.cs` startup migration invocation | Replace before runtime testing | Migration Operations | Remove/feature-gate legacy PIN mutation; migrations run only as explicit, approved, checkpointed operations. |

## 9. Legacy workflows currently missing from the reviewed branch inventory

The permission catalog's BackendInfo references these classes/workflows, but the reviewed branch inventory did not contain the named controllers/services:

- `BuildingImportController`, `BuildingWizardController`, `ToolsController`;
- `MigrationService`, `DbMaintenanceService`;
- `SyncController`, `DbSyncService`, `ReverseSyncService`.

Disposition: do not invent replacements or migrate grants automatically. Confirm whether the workflows are intentionally retired, hidden behind another supported path, or need to be rebuilt as new modules. This is tracked in the permission-scope catalogue and owner decision PERM-05.

## 10. Controller and View redesign contract

All MVC controllers are HTTP adapters only:
1. authenticate and bind request;
2. validate transport-level shape and antiforgery;
3. invoke one application command/query;
4. map typed outcomes to HTTP/UI result;
5. never calculate money, merge identity, resolve permissions, call provider SDKs, or hydrate broad EF graphs.

All Views receive dedicated view models/read models:
- no EF entities;
- no secret-bearing settings models;
- no direct permission decisions based only on menu visibility;
- no direct file URLs where storage is private;
- no client-supplied building/apartment scope trusted without server resolution.

A controller can remain physically named during transition, but its business logic and dependencies must move to the target use cases.

## 11. Migration order for legacy class replacement

1. Build shared Application foundation: actor, clock, correlation, typed results, validation, authorization port, transaction/outbox.
2. Replace identity/authentication and session validation; keep a narrow compatibility adapter.
3. Replace scoped authorization and admin assignment lifecycle.
4. Replace Person/property relationship services and account access.
5. Replace finance commands and read models only after wallet parity contract.
6. Replace polls and secret ballot as a separate cutover.
7. Replace maintenance, notifications and presence.
8. Replace private documents/OCR and camera/media integration.
9. Replace backup/audit/log operations and settings/integrations.
10. Rebuild MVC views and navigation around new read models/use cases.
11. Retire old aggregate write paths, old auth paths, and unsupported workflows only after telemetry/parity/rollback acceptance.

## 12. Acceptance evidence per class family

- Dependency graph contains no Domain -> EF/HTTP/vendor references.
- Controller tests prove role + permission + resource scope for allowed and denied cases.
- Service tests prove domain invariants and typed outcomes.
- SQL integration tests prove concurrency, FK, CHECK, filtered index, and rollback behavior.
- Finance tests compare old/new totals per apartment/building/month and explain every variance.
- Identity tests prove no auto-merge, one account per Person, and review provenance.
- Security tests prove no secrets in DTO/HTML/log/export, session revocation, and MFA step-up.
- External integration tests prove idempotency, retry, compensation, and reconciliation.
- Performance tests prove bounded paging and query budgets using representative restored data.

## 13. Status

This is a source-to-target refactoring plan. It does not change classes, controllers, views, database objects, or configuration. No build or test run is claimed. Owner decisions remain tracked in the decision register and permission catalogue. Gate A remains **NOT AUTHORIZED**.
