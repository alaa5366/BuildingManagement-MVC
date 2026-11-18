# Identity Resolution and Authorization Review — 2026-11-18

**Branch:** `development/database-redesign-enterprise`  
**Scope:** restored-database aggregate evidence plus repository source review. No identity rows were linked, merged, or changed.

## 1. Current evidence

The previously reviewed read-only profile reports:

- 79 Residents and 5 Users.
- Zero exact UID matches between Residents and Users.
- Four duplicate normalized-phone groups among Residents and one among Users.
- Zero duplicate normalized-email groups in the inspected sets.
- No Residents without an Apartment and no Apartments without a Building.
- Two apartments have an Owner flag row but no active primary/resident row under the profiling predicate; the other 77 have one active primary/resident row.

The zero UID matches means there is no evidence-supported automatic cross-table link between the current Residents and Users sets. It does not prove that the rows describe different humans: the two legacy authentication paths may have issued different identifiers, and phone/email equality is not sufficient to establish identity.

Duplicate phone groups must be treated as shared-contact candidates, not duplicate-person proof. Family members can share a phone or WhatsApp number. The absence of duplicate email groups does not prove unique human identity.

## 2. Source-code observations

- `Users` and `Residents` remain separate data/authentication models.
- Resident sign-in resolves a resident by building/apartment and normalized phone, then verifies `Residents.PinHash`.
- Admin sign-in resolves `Users` by phone and `BuildingAdmins`; SuperAdmin sign-in resolves `Users` by configured email and role.
- `ResidentSqlStore.GetByUidAsync` returns a single row, while the target design must allow a Person to have multiple apartment relationships without creating multiple accounts. The migration must therefore separate legacy resident-row identity from person/account identity.
- `UserSqlStore.UpsertAsync` inserts missing `BuildingAdmins` links but does not remove existing links that are absent from a submitted BuildingIds list. Do not assume that every historical link has been reliably revoked solely because it is absent from an update payload; review explicit admin-removal flows and audit evidence before interpreting assignments as currently authorized.
- `UserPermissions` stores per-user permissions. It cannot be backfilled into role-wide `RolePermissions` without broadening access. Preserve it through scoped account overrides.

## 3. Identity resolution policy

1. Never merge by phone, WhatsApp, email, name, apartment number, or combinations of these fields alone.
2. Exact UID equality, when present and unique on both sides, is a candidate link only; conflicts and shared-account evidence still require review.
3. Preserve every source row and its source key. A Person may map to multiple Residents relationship rows, but at most one UserAccount may belong to that Person.
4. Unresolved rows remain separate/unlinked. Do not create artificial matches merely to satisfy the target account model.
5. Keep ownership, occupancy, access grants, and account status as separate concepts.
6. Keep apartment owner text and IsOwner flags as evidence only; do not create legal ownership shares or dates from them.

## 4. Missing migration-provenance artifact

The mapping specification refers to an `IdentityMigrationMap`, but the current Gate A object list/script does not yet include a durable source-to-target identity mapping table. This is a design inconsistency that must be fixed before Gate A is frozen.

Recommended additive table concept: `LegacyIdentityMap`, with:
- SourceSystem / SourceTable / SourceKey (unique source-row identity).
- Target PersonId and optional AccountId (nullable while unresolved).
- MappingStatus (Unreviewed, Linked, Separate, Rejected, NeedsManualReview).
- MatchMethod / EvidenceCode, reviewer, reviewed timestamp, and reason.
- CreatedAtUtc, UpdatedAtUtc, RowVersion.
- Restrictive FKs to Persons and UserAccounts; no cascade deletion.
- Unique key on (SourceSystem, SourceTable, SourceKey), plus indexes by PersonId and AccountId.

The map must support many legacy Residents rows resolving to one Person while preserving the one-account-per-Person invariant. It must not force an account for every Person or every Resident row. Define transaction-level validation so a User source row cannot map to multiple accounts and no Person receives more than one interactive account.

This is a proposed design correction only; it has not been added to the Gate A SQL or executed.

## 5. Authorization migration implications

- Map each verified `BuildingAdmins` row to a Building-scoped role assignment, retaining source-row provenance.
- Map each `Users.Role = superadmin` only to a System-scoped role assignment after account status and identity review.
- Preserve each legacy `UserPermissions` row as an account-scoped override with an explicit scope and effect. Do not turn it into a global role permission.
- Reconcile explicit removal/revocation history before treating legacy BuildingAdmins links as active.
- The target must distinguish a login account's enabled/disabled state from a person's occupancy and apartment access state.

## 6. Gate decision

Identity linking is **not ready for automated backfill**. The current evidence supports a conservative staged process: create candidate Person records only from reviewed groups, retain unresolved legacy rows in the map, and defer account creation/linking until a reviewer has established the person-level identity.

Before backfill:
- Inventory all current and historical admin-building assignments and their revocation paths.
- Produce a privacy-safe candidate queue with source keys and conflict codes; do not export personal values into Git.
- Approve the LegacyIdentityMap schema and its uniqueness/transaction rules.
- Define who can approve merges and how corrections are audited.

No DDL/DML, EF migration, identity merge, role backfill, credential rewrite, or application cutover was performed.


## 7. Gate A provenance schema correction completed — 2026-11-18

The earlier finding is now reflected in the additive design:

- Added `dbo.LegacyIdentityMap` to `GateA-Additive-Identity-Foundation.sql`.
- Added it to the read-only Gate A preflight object inventory; expected target object count is now 13.
- Updated the target data dictionary and legacy mapping document to use the same name and schema.
- Initial source allow-list is Users and Residents. Resident relationship rows may map many-to-one to a Person. A Users source row maps to a Person and one Account; a filtered unique index prevents duplicate source-user mappings to the same Account.
- The database FKs independently validate Person and Account existence. The migration service must additionally verify that a linked Users mapping's AccountId belongs to its PersonId in one transaction.
- Pending/manual-review/rejected source rows remain unresolved; no auto-merge is permitted.

This closes the documentation/schema inventory inconsistency at design level only. The revised preflight has not yet been run, the Gate A DDL has not been executed, and no source rows have been backfilled.


## 8. Admin access lifecycle and deletion-path review — 2026-11-18

A follow-up source inspection of `Data/UserSqlStore.cs`, `Services/UsersService.cs`, and `Services/AdminManagementService.cs` confirms two important legacy behaviors:

- `UserSqlStore.UpsertAsync` adds missing `BuildingAdmins` links for an admin, but does not remove links omitted from the submitted `BuildingIds` list. The update path therefore cannot be interpreted as a complete replacement/revocation operation.
- `AdminManagementService.DeleteAsync` performs physical deletes from `BuildingAdmins`, `UserPermissions`, and `Users` using three `ExecuteDeleteAsync` calls. No explicit transaction surrounds those calls in this method, and the audit event is written only after the deletes. A failure between commands or during audit can leave a partially deleted account with incomplete audit evidence.

### Target transition requirements

1. Replace hard deletion with account disable/revocation and retained identity/provenance/authorization history. Any legally required erasure must be a separately approved, audited privacy workflow, not ordinary admin deletion.
2. Make building assignment updates explicit: distinguish full replacement from additive grant, and record each grant/revocation with actor, reason, and timestamp.
3. Perform account status changes, role/access revocation, security-stamp rotation, and durable audit/outbox recording atomically where possible. If audit is external, use a transactional outbox.
4. Before interpreting historical `BuildingAdmins` rows as active access, reconcile the existing delete/update paths and audit trail. A missing row is not proof of why or when access ended.
5. Add tests for partial failures, concurrent assignment changes, session invalidation, and attempts to reuse a disabled/deleted legacy identity.

This is a source-code finding and target requirement only. No application source was modified and no account data was changed.


## 9. Legacy permission scope semantics — follow-up source review

Reviewed `Attributes/HasPermissionAttribute.cs`, `Controllers/PermissionsController.cs`, `Models/AdminPermissions.cs`, `Data/UserSqlMapper.cs`, and `Services/SessionRevalidator.cs`.

- The legacy permission matrix stores a flat list of permission strings per User. `PermissionsController.Toggle` and `ApplyTemplate` update that account-wide list; no BuildingId or ApartmentId is stored with an individual permission row.
- `HasPermissionAttribute` checks authentication and the presence of a matching `perm` claim. It does not itself evaluate resource scope. SuperAdmin bypasses the permission-claim check in this attribute.
- The permission catalogue includes keys with different implied scopes (for example, global settings, building settings, own settings, all/building/own audit views), but the key name is not a substitute for a verified resource-scope record.
- `SessionRevalidator` reloads the admin's flat permissions and checks that claimed building IDs remain in the account's current BuildingAdmins list. This is a session-level membership check, not a per-permission scope model.

### Migration rule

Do not mechanically assign every legacy permission to System scope or copy it to every Building scope. Before mapping each permission key, classify its resource scope and inspect the consuming controller/service for actual resource-level checks. Where the legacy semantics are account-wide but its effective building context is ambiguous, retain the source permission as unresolved in the migration review queue until a business rule is approved. Preserve the exact legacy permission key and source row provenance.

The target authorization evaluator must independently resolve: account status, system/building/apartment/personal scope, role grants, direct allow/deny overrides, validity, and the requested resource. UI hiding and a permission claim are not sufficient authorization.

No application source was changed and no permission data was migrated.


## 10. Permission mutation, session invalidation, and audit consistency — 2026-11-18

Follow-up inspection of `Services/AdminManagementService.cs`, `Services/SessionRevalidator.cs`, `Data/UserSqlStore.cs`, `Controllers/PermissionsController.cs`, `Attributes/HasPermissionAttribute.cs`, and the cookie authentication setup in `Program.cs` found the following:

### Existing protections

- Cookie authentication invokes `SessionRevalidator.ValidateAsync` through `OnValidatePrincipal`.
- For ordinary admins, validation reloads the account, checks active/disabled state and verifies that every BuildingId claim still exists in the current BuildingAdmins-derived account document.
- Permission changes are reloaded and refreshed into the cookie's `perm` claims; a changed permission list causes principal renewal.
- `SyncPermissionsAsync`, `UpdateAdminAsync`, `DeactivateAsync`, `ActivateAsync`, and `DeleteAsync` call `InvalidateAdmin` after their database writes.
- SuperAdmin validation queries the account by UID, email, and role and checks active/disabled status plus the configured email allow-list and 2FA requirement.
- Impersonated sessions are separately revalidated against the initiating actor and target building/apartment, with a shorter cache and no last-known-good fallback.

### Confirmed gaps and limits

1. **Invalidation is process-local.** `InvalidateAdmin` increments an in-memory `ConcurrentDictionary` version. The validation cache is also `IMemoryCache` with a two-minute lifetime. In a multi-instance deployment, a mutation on instance A does not increment instance B's version or evict B's cached result. A stale authorization decision may therefore survive on another instance until its cache expires.
2. **PIN change does not invalidate sessions.** `ChangePinAsync` writes the new PIN and audit event but does not call `InvalidateAdmin`. Existing authenticated cookies remain valid; whether PIN rotation should revoke all sessions must be an explicit security policy, but the current behavior is not an enforced global session-revocation policy.
3. **SuperAdmin has no shared revocation version.** Its principal is checked against SQL on validation, but there is no security-stamp/session-version claim or shared revocation mechanism. A password/PIN/credential change and global emergency revoke need a defined account-wide session policy.
4. **Audit is not atomic with account mutation.** Admin create/update/activate/deactivate/permission-sync and PIN-change operations write audit after the account write. If audit persistence fails, the state change may already have committed without its corresponding durable audit event. The delete path is more exposed because its three physical delete statements are not wrapped in an explicit transaction in the service method.
5. **Building membership semantics remain incomplete.** The upsert only adds valid BuildingAdmins rows when the supplied BuildingIds list is nonempty. It does not remove omitted links and cannot represent an explicit revoke-all through an empty list. The session validator rejects a cookie if any claimed building is no longer assigned, but it does not refresh newly added building claims into an existing cookie.
6. **Invalidation coverage is not centralized.** The current calls are embedded in selected AdminManagementService methods. Other write paths (including direct user-store writes, role changes, credential resets, and future scope-grant services) can bypass them unless every mutation is routed through one lifecycle service or a database-backed version mechanism.
7. **Authorization remains claim-string based.** `HasPermissionAttribute` checks a permission claim and allows SuperAdmin to bypass that check. It does not perform a resource-level building/apartment scope evaluation. This reinforces the need for a centralized authorization evaluator before scoped roles are introduced.
8. **Cache identity and deployment behavior need an explicit contract.** The two-minute cache and 15-minute last-known-good fallback are not a substitute for immediate revocation. Ordinary admin lookup failures may reuse a last-known-good result for a verified-2FA session. Any target design must decide which actions fail closed immediately and must never allow an account-disable or emergency-revocation event to be hidden by stale cached authorization.

### Required target lifecycle

- Introduce a monotonically increasing account security/session version (or equivalent revocation stamp) persisted in the authoritative database. Include the version in the authenticated principal and compare it on every validation through a shared/distributed cache or authoritative lookup with a deliberately bounded staleness policy.
- Increment the version for credential/PIN replacement, account disable, role changes, permission grants/revocations, building/apartment scope changes, MFA policy/enrollment changes, recovery-code/security reset, and administrator-initiated global sign-out.
- Use a single account/authorization lifecycle service for all mutation paths. Do not rely on developers remembering to call an in-process invalidation helper.
- Model building access updates explicitly as grant, revoke, or replace-all operations. Empty replacement must mean zero assignments; additive grant must never silently imply replacement.
- Persist the business mutation and an audit/outbox event in the same database transaction. A dispatcher can publish the outbox asynchronously; audit delivery failure must not erase the durable event.
- Replace routine hard deletion with disable/revoke and retained provenance. Keep legally approved erasure as a separate controlled workflow.
- Make authorization evaluation resource-aware: account state, role assignment, scope type and ID, direct allow/deny, validity interval, requested action, and target resource must all be evaluated server-side. UI state and `perm` claims may optimize presentation but are not the authorization authority.
- Define emergency revocation semantics separately from normal cache refresh, including all application instances and active impersonation sessions.

### Verification scenarios required before implementation approval

- Permission revoke on instance A is rejected on instance B without waiting for the ordinary two-minute cache TTL.
- Building revoke invalidates only the affected scope and cannot leave an existing cookie authorized for the removed building.
- Empty replace-all removes every building assignment; additive grant preserves existing assignments.
- PIN/credential rotation and MFA reset follow the approved session-revocation policy.
- Audit/outbox failure rolls back the mutation or leaves a durable pending event in the same transaction.
- Concurrent grant/revoke operations do not resurrect stale assignments.
- Disabling an account rejects ordinary and impersonated sessions across all instances.
- SuperAdmin emergency revoke works independently of the configured email allow-list.
- Every controller/service operation checks the requested resource scope; a valid permission key alone cannot authorize access to another building or apartment.

This review is documentation-only. No application source, database schema, rows, credentials, or sessions were changed. No build or runtime tests were executed.


### Additional audit persistence and authentication observations

A final inspection of `Services/AuditLogger.cs`, `Services/IAuditLogger.cs`, `Services/SqlAuthService.cs`, `Controllers/AccountController.cs`, and cookie configuration in `Program.cs` adds these details:

- `AuditLogger.LogAsync` creates a separate DbContext and calls `SaveChangesAsync` after the business operation has already completed in the calling service. It catches every exception, logs a warning, and does not rethrow or return a delivery result. Therefore callers cannot distinguish a persisted audit event from a failed audit write; the admin mutation can report success while its audit event is absent.
- The audit writer currently has no transactional outbox or durable retry record. A warning in application logs is not equivalent to a durable audit event.
- The authentication cookie has a seven-day expiry with sliding renewal. Cookie validation runs on requests, but the application has no persisted account security-stamp/session-version claim in the inspected configuration.
- Admin sign-in binds the initial cookie to the building selected at login. Later validation checks those existing building claims against current assignments; it does not itself issue newly added building claims into the principal.
- The impersonation backup stack is stored in a time-limited protected cookie (two-hour protection window) and logs enter/exit through the same best-effort audit logger. Its security review must include revoking active impersonation when the initiating account is disabled, credential-rotated, or emergency-revoked.

**Correction to audit failure handling:** the current service methods may not merely leave an audit write failure unreported by the business layer; the shared logger intentionally swallows the exception after warning. This makes the transactional outbox requirement a correctness and accountability requirement, not just a reliability enhancement.


## 11. Authentication, MFA, and credential lifecycle — 2026-11-18

Reviewed `AccountController`, `SqlAuthService`, `TwoFactorService`, `PinMigrationService`, `SessionRevalidator`, `UserSqlStore`, and the authentication/session registrations in `Program.cs`.

### Existing controls worth preserving

- Login routes use antiforgery validation and a named login rate-limit policy.
- Admin sign-in is scoped to the selected building; the initial admin cookie carries only that building claim.
- The unified login flow holds an accepted primary-authentication result in a five-minute pending second-factor state and does not issue the authenticated cookie until TOTP/recovery verification succeeds.
- TOTP secrets are encrypted at rest; recovery codes are stored as SHA-256 hashes and removed when consumed; TOTP accepted-step replay is rejected.
- Admin/SuperAdmin cookies are explicitly limited to 12 hours with refresh disabled at issuance; resident cookies use the global seven-day sliding configuration.
- Two-factor setup requires proof of possession of the authenticator secret before enabling it. Disable requires a valid TOTP or recovery code.

### Confirmed gaps / risks

1. **MFA is currently opt-in, not mandatory.** `TwoFactorService` describes opt-in administrator MFA. `CompleteSignInAsync` only challenges accounts for which `TwoFactorEnabled` is already true. This does not meet the target policy that administrators and SuperAdmin must enroll and verify MFA before receiving privileged access.
2. **MFA changes do not revoke other sessions.** Enable/disable operations update the user row but do not increment a persisted security version or invoke session invalidation. Enabling MFA refreshes only the current cookie with `twoFactorVerified=true`; disabling MFA leaves other cookies and their claims to be reconciled by later validation.
3. **MFA mutation lacks durable audit.** The service logs enable/disable to application logs only. No durable security audit/outbox event is written for enrollment, disablement, recovery-code consumption, or security reset.
4. **TOTP/recovery-code serialization is only process-local.** `KeyedLock` serializes operations within one process. With multiple app instances, concurrent use of the same TOTP step or recovery code can race unless the database update uses an atomic conditional update/concurrency token. The current code reads the user row, mutates JSON/step values, and saves; no cross-instance concurrency contract was found in this service.
5. **Pending login and MFA setup rely on ASP.NET session state.** The application registers `AddDistributedMemoryCache`, which is in-process distributed-cache implementation, and stores pending sign-in results, setup secrets, and context-selection data in Session. In a multi-instance deployment, session affinity or a truly shared distributed session store is required; otherwise a challenge can disappear or become inconsistent after routing to another instance.
6. **Login throttling is not demonstrably shared across instances.** The configured ASP.NET rate-limiter policies use their in-process limiter implementation; `LoginThrottle` is registered singleton. Cluster-wide abuse controls need a shared store/atomic counters and account+IP dimensions, not just per-process limits.
7. **Credential and MFA lifecycle are not centralized.** PIN changes, MFA enable/disable, recovery-code consumption, role/permission changes, and account disablement do not all flow through one security lifecycle operation that atomically records the mutation, audit event, and session-version increment.
8. **Startup credential migration is an automatic write path.** `Program.cs` invokes `PinMigrationService.MigrateLegacySecretsAsync()` during application startup. The method hashes nonempty non-BCrypt User/Resident values and clears obsolete building PIN data; this can mutate restored or production data simply by starting the application. This path must be replaced by an explicitly approved, audited, transactional migration with dry-run, per-record outcome, verified destination, backup/rollback, and no clearing before verification. Do not run the application against the restore merely to inspect it.
9. **Role transitions need explicit security semantics.** `UserSqlStore.UpsertAsync` accepts role values for admin, superadmin, and resident and persists the role; a role transition must revoke existing sessions and re-evaluate MFA requirements, permissions, building scope, and identity linkage atomically.
10. **Cookie policy needs a single source of truth.** The global cookie handler has a seven-day sliding expiry, while admin/SuperAdmin issuance overrides it to 12 hours and non-refreshable. Target session policy should be explicit per account class and enforced consistently on renewal and revocation, not inferred from issuance paths.

### Approved target decisions for the unified design

- MFA policy is role/account-class driven: mandatory for every administrator and SuperAdmin; optional for resident accounts unless a future sensitive action requires step-up authentication. No privileged admin session is issued until enrollment and verification are complete.
- MFA enrollment, disablement, factor replacement, recovery-code regeneration/consumption, credential rotation, account disablement, role/scope/permission change, and emergency sign-out all update the same persisted account security version.
- TOTP accepted-step and recovery-code consumption must be atomic across instances. Use a dedicated normalized recovery-code table (one row per code, consumed timestamp/status) and a conditional update/concurrency token for TOTP step; do not rely on a process-local lock as the integrity boundary.
- Pending authentication challenges are short-lived, one-use, bound to the account and authentication transaction, and stored in a shared server-side store. Never put the primary credential or plaintext long-lived secret into an authenticated cookie.
- Rate limiting and login lockout must use shared atomic counters with separate account-identifier and IP/device dimensions, progressive delay, safe reset, and auditable security events.
- Remove automatic credential migration from application startup. Migration must be a separately invoked, approved job; classify unknown legacy formats as manual reset/re-enrollment, never blindly hash unknown values or clear source data before successful verification.
- Security events are durable and transactional with the account mutation (outbox pattern); application logs remain operational telemetry, not the authoritative audit record.
- Keep account-class session lifetimes configurable and shorter for privileged accounts, but enforce revocation through the persisted security version rather than relying on cookie expiry.

### Verification scenarios required

- Admin and SuperAdmin cannot reach privileged routes without completing mandatory MFA, including newly created accounts and role promotion.
- Disabling MFA cannot silently weaken an admin account's policy; an administrator cannot self-disable a mandatory factor without an approved recovery/re-enrollment path.
- Two concurrent requests using one TOTP step or one recovery code result in exactly one success across different app instances.
- MFA enrollment/disable/reset increments the account security version and invalidates every old session, including impersonation.
- A pending MFA challenge works across application instances and expires/consumes exactly once.
- Cluster-wide login throttling blocks attempts consistently regardless of which instance receives them.
- Application startup performs no credential or identity data migration.
- Credential migration dry-run is read-only; apply mode verifies destination before source retirement and can be rolled back from a tested backup.
- Role promotion/demotion recalculates scope and MFA requirements and revokes all prior claims.

This section is a source review and target-design decision record only. No application source, database schema, records, credentials, or active sessions were changed. No build, migration, or runtime test was executed.


## 12. Consolidated implementation package and release gates — 2026-11-18

To avoid fragmenting the work into many small feature phases, implement the approved identity/security redesign as one coordinated package with internal workstreams and hard release gates. Workstreams may be developed and reviewed independently, but the package is not considered complete until all cross-workstream invariants and end-to-end tests pass.

### Package workstreams

1. **Identity foundation and provenance:** Person, one interactive account per Person, credential/factor records, LegacyIdentityMap, explicit manual match dispositions, no fuzzy auto-merge.
2. **Authorization model:** normalized role definitions/permissions, explicit scope types (System, Building, Apartment, Personal), direct allow/deny overrides, validity windows, centralized resource-aware evaluator, System-only SuperAdmin.
3. **Relationship/access lifecycle:** dated ownership and occupancy, explicit apartment access grants, grant/revoke/replace-all semantics, overlap/share validation, account disable and retention instead of routine hard deletion.
4. **Credential and MFA lifecycle:** verified credential replacement, mandatory admin/SuperAdmin MFA, optional resident MFA, atomic TOTP/recovery-code consumption, audited recovery/reset, no startup migration.
5. **Session and impersonation security:** persisted account security version, shared cross-instance revocation, bounded cache policy, session version validation, actor+target scope validation, revocation of impersonation on actor/target security changes.
6. **Audit and operational reliability:** transactional outbox, durable security events, correlation IDs, retry/dead-letter monitoring, export/retention controls, no best-effort-only security audit.
7. **Legacy migration and financial continuity:** source-to-target mapping, staged dry-run/apply, per-row outcomes, reconciliation, no destructive cleanup until verified; preserve old financial ledgers and wallet behavior until explicit parity and external-dependency confirmation.
8. **UI and API integration:** user settings for MFA, permission/scope administration, relationship management, clear audit history, consistent server-side authorization on every endpoint and background operation.

### Mandatory release gates

- **Gate 0 — design freeze:** complete source-to-target mappings, permission catalogue/scope matrix, GrantType catalogue, retention rules, MFA recovery policy, session revocation policy, and financial reconciliation dispositions.
- **Gate 1 — schema/preflight:** re-run read-only preflight against the disposable restore; validate all required target objects absent and exact types/collations/FKs/indexes; independently review SQL and rollback. No partial Gate A execution.
- **Gate 2 — additive foundation:** after separate explicit authorization only, create additive schema on a disposable restore. No production target and no destructive alteration.
- **Gate 3 — service invariants:** implement transactions, concurrency controls, centralized evaluator/lifecycle, outbox, and security-version revocation before any identity or permission backfill.
- **Gate 4 — migration rehearsal:** dry-run first; manual identity dispositions; staged apply; verify counts, links, permissions, balances, MFA, and audit; prove rollback from backup.
- **Gate 5 — compatibility and cutover:** dual-read/controlled dual-write only where designed; compare old/new authorization decisions; cut over behind a reversible switch; retain legacy data until acceptance.
- **Gate 6 — operational acceptance:** multi-instance security tests, restore/rollback rehearsal, load/performance tests, security review, monitoring/alerting, and signed acceptance.

### Non-negotiable safety rules

- No production DDL/DML, migration, backfill, or cutover without a distinct explicit approval after Gate 0 and Gate 1 evidence.
- No automatic identity merge based on phone, email, name, apartment, or fuzzy similarity.
- No conversion of flat legacy permissions into system-wide or every-building grants without controller/service evidence and approved scope.
- No silent financial recalculation, carry-over netting, or historical share rewrite.
- No application startup side effects that migrate or clear credentials.
- No routine hard delete of accounts or identity records; use disable/revoke and retained provenance.
- No claim of tests, builds, SQL execution, or rollback success unless actually run and evidence recorded.

The user has approved the unified target design and continued review. This is not authorization to execute Gate A or mutate any database.


## 13. Controller/action resource-scope matrix and permission enforcement gaps — 2026-11-18

A focused controller/action review was performed against the branch source for the authorization filters, permission catalogue, audit log, settings, resident endpoints, financial endpoints, cameras, invoices, notifications, impersonation, and apartment/resident administration. This is a source-level review; no code or data was changed.

### Current enforcement pattern

- `[Authorize]` establishes authentication/role gates, but does not by itself establish a building/apartment resource boundary.
- `AdminPermissionAttribute` checks a `perm` claim only when the principal is in the `admin` role. Its current implementation returns without denying when the user is authenticated but is not an admin. This includes residents and any other authenticated role.
- `HasPermissionAttribute` checks a matching `perm` claim for non-SuperAdmin users and lets SuperAdmin pass. It also does not evaluate a requested building/apartment resource.
- Therefore custom permission attributes are coarse feature gates, not the target resource-aware authorization evaluator. Controller/service checks must still bind the operation to the target resource.

### Permission/action/scope matrix (observed legacy behavior)

| Feature / endpoints reviewed | Current role or permission gate | Effective resource boundary observed | Target scope |
|---|---|---|---|
| Buildings administration — `BuildingsController` | Mostly SuperAdmin role; selected details action also permits admin | Admin Details compares route building ID with the building claim; create/list and SuperAdmin management are system-level | System for create/delete/global inventory; Building for admin read limited to assigned building |
| Admin accounts and permission assignment — `AdminsController`, `PermissionsController` | SuperAdmin role plus `admins.manage` | Global account/assignment administration | System |
| Resident administration — `AdminResidentsController` | Admin/SuperAdmin plus `residents.manage` | Admin building claim or SuperAdmin-selected building; resident record actions check the resident's BuildingId | Building, with person/account actions separated in target |
| Apartment administration — `AdminAptsController` | Admin plus `residents.manage` | Uses the building claim and loads the apartment from that building | Building for apartment operations; apartment state is the target resource |
| Apartment disable/enable — `AdminAptsController.DisableApartment/EnableApartment` | Inherited class-level `residents.manage` | Building-scoped apartment lookup; no separate `apartment.disable` action gate was found in the reviewed controller | Building + Apartment, with a dedicated destructive/status-transition permission |
| Wallet administration — `WalletController` | Admin plus `wallet.manage` | Loads the claimed building; apartment adjustment target is resolved within that building | Building + Apartment for every movement/adjustment |
| Resident wallet — `ResidentWalletController` | Resident role | Uses both building and apartment claims and resolves the apartment within that building | Personal account + Apartment |
| Expenses/revenues/reports — `ExpensesController`, `RevenuesController`, `ReportsController` | Admin plus feature permission | Uses the building claim as the data root | Building |
| Polls — `PollsController`, `ResidentPollsController` | Admin feature permission for management; resident role for voting | Admin loads claimed building; resident vote uses claimed building and apartment | Building for poll management; Apartment/eligible occupant for ballot eligibility |
| Maintenance — `MaintenanceController`, `ResidentMaintenanceController` | Admin feature permission or resident role | Resident endpoint loads maintenance log for the claimed building; no apartment filter was observed in the reviewed action | Building for shared maintenance; Apartment/Personal only if records are private or individually assigned |
| Building/global/personal settings — `SettingsController` | Global: SuperAdmin + `settings.manage.global`; Building: Admin/SuperAdmin + `settings.manage.building`; Own: authenticated user | Building POST explicitly compares admin building claim; Own service receives current user, role, building and apartment claims | System / Building / Personal |
| Audit log — `AuditLogController` | Explicit role+permission check in both Index and ExportCsv | Resident filters are forced to own building+apartment; admin filters are forced to claimed building; SuperAdmin can filter globally | System / Building / Apartment (resident own) |
| Invoice download — `InvoiceController` | Admin/Resident/SuperAdmin role | Resident apartment is taken from claims, ignoring supplied aptId; admin target apartment is resolved within claimed building; SuperAdmin without building context can resolve an apartment by aptId | Apartment + Personal entitlement; System for SuperAdmin |
| Notifications — `NotificationsController` | Admin/Resident role | Uses claimed building and recipient type; residents must have apartment claim | Building for admin notifications; Apartment/Personal for resident notifications |
| Camera live/HLS — `CamerasController` | Class-level `[Authorize]` + `AdminPermission(cameras.view)` | Live checks requested building against building claim for non-SuperAdmin; HLS resolves camera first then checks camera.BuildingId against claim | Building + camera resource; resident viewing must be an explicit resident access policy, not an accidental bypass of an admin permission filter |
| Impersonation — `ImpersonationController` | Admin/SuperAdmin role, action-specific checks | Target apartment is resolved inside selected/claimed building; SuperAdmin can choose building; actor and target are separately represented in claims/restore context | System/Building/Apartment with actor+target policy and auditable session lifecycle |
| Anonymous QR access — `QrAccessController` | Anonymous entry, signed token validation, POST antiforgery and token-use check | Token is consumed before creating a resident principal for the token's building/apartment | Token-scoped one-time capability; resulting session is Apartment-scoped |
| QR image endpoint — `QrImageController` | Admin/SuperAdmin role | Accepts HTTP(S) data and renders QR image; it does not itself grant access | No business-data scope; protect token-bearing output from cache/log leakage |

### Confirmed gaps requiring implementation decisions

1. **AdminPermission non-admin pass-through:** the implementation's `if (!user.IsInRole("admin")) return;` means it does not deny residents. The class-level `[Authorize]` on `CamerasController` allows any authenticated role to reach the actions, while the filter does not enforce `cameras.view` for residents. The controller comments explicitly describe resident live viewing, so this may be an intended resident capability; however, it must be modeled as a distinct resident apartment/building access policy. Do not treat the current admin permission attribute as enforcing it. Add tests for resident, admin-with-permission, admin-without-permission, SuperAdmin, and any unsupported authenticated role.
2. **Apartment disable permission is not separately enforced:** `AdminPermissions.DisableApartment = "apartment.disable"` exists, but the reviewed `AdminAptsController.DisableApartment` and `EnableApartment` actions inherit the class-level `residents.manage` requirement. The dedicated permission key is not used at those action declarations. Decide whether disabling an apartment requires both resident-management and a distinct apartment-status permission, then enforce and test it server-side.
3. **Audit-log permission keys are more precise than the generic permission attributes:** the controller correctly uses explicit role+claim logic and forces the resource filters for residents/admins. Preserve this as a baseline behavior during migration; do not replace it with a generic claim-only attribute.
4. **Building scope must be checked after resolving the target resource:** the reviewed financial and apartment controllers commonly load the current building from the session claim and resolve child IDs inside that object. The target service layer must retain this invariant so a forged/foreign apartment, camera, deposit, poll, or invoice ID cannot cross the building boundary.
5. **Read-only versus mutation permissions are currently coarse:** examples include a single `wallet.manage` covering balance viewing, deposit confirmation/cancellation, and manual adjustment, and `residents.manage` covering resident CRUD plus apartment status/QR operations. The target catalogue should distinguish read, create, update, financial confirmation, adjustment, disable/revoke, export, and sensitive-data actions where risk differs.
6. **Scope cannot be inferred from the permission key or UI category:** the catalogue groups permissions into Admin/Financial/Services/System, but those are presentation categories, not scope types. `settings.manage.global`, `.building`, and `.own` imply different scopes; `audit.view.all/building/own` are role/resource variants. Persist explicit ScopeType and ScopeId in target assignments/overrides.
7. **Personal scope is not a synonym for resident apartment scope:** own profile/preferences are account-personal, while wallet, occupancy and voting eligibility are apartment/relationship scoped. The target evaluator must resolve both independently.
8. **Impersonation must not inherit the target user's authority:** authorization should evaluate the initiating actor's permission to enter the target scope, then restrict the impersonated session to the target resource and explicitly allowed actions. Every action and exit/revocation path must preserve actor attribution.

### Required authorization test matrix

- For each sensitive endpoint, test anonymous, resident, admin without permission, admin with permission in the correct building, admin with permission but wrong building, SuperAdmin, disabled account, and impersonated principal.
- Repeat the same tests for both HTML and export/download/API endpoints; a hidden button or GET-page restriction does not secure a POST or export.
- Use foreign BuildingId, ApartmentId, CameraId, DepositId, PollId, and ResidentId values to verify server-side ownership/scope binding.
- Test action-specific separation: view vs mutate, confirm vs cancel, financial adjustment, apartment disable/enable, export, QR generation, camera stream URL retrieval, and secret access.
- Test that role/permission/scope revocation is effective across application instances and invalidates impersonated sessions.
- Add reflection-based coverage that every controller/action has a role gate and that every custom permission attribute is paired with the intended role policy; add behavioral tests for the filter's non-admin path.

### Gate 0 deliverables updated

The permission catalogue must now include a controller/action/resource matrix, a resident camera policy, apartment disable/enable policy, read-vs-write separation for high-impact financial and identity actions, and explicit decisions for building-wide resident maintenance visibility. Any legacy permission with no confirmed consuming endpoint or unclear scope remains unmapped/manual-review; do not automatically grant it at System or all-Buildings scope.

This section records source observations and target requirements only. No application source, schema, database rows, credentials, or sessions were changed. No build or runtime tests were executed.


### Existing authorization-test coverage review

Reviewed `Tests/BuildingManagement.Tests/ControllerAuthorizationTests.cs` and `SourceIntegrityTests.cs`.

- The reflection guard verifies that controllers/actions carry `[Authorize]` or `[AllowAnonymous]`; a class-level `[Authorize]` is accepted for every action, but the test does not prove the correct role or resource scope.
- The sensitive-controller role test currently covers only Expenses, Revenues, Wallet, and Backup.
- The custom-permission test verifies that selected attributes are declared on AdminResidents, Cameras, and Building Settings. It does not instantiate either custom filter or test what happens for resident/non-admin principals.
- The audit-log test validates the static role+permission decision helper, but does not exercise query scoping or CSV export against foreign resource IDs.
- No test in the inspected file asserts that `DisableApartment` / `EnableApartment` requires `apartment.disable`.

**Required test additions:** behavioral filter tests using AuthorizationFilterContext for AdminPermission and HasPermission; action-specific permission tests for apartment disable/enable; table-driven role/scope tests across all sensitive controllers; and endpoint tests for foreign building/apartment/resource identifiers on both read and mutation/export actions. The tests should assert deny-by-default for unsupported authenticated roles, not merely presence of an authorization attribute.

This is test-source inspection only; no tests were executed.


## 14. Service boundaries, import/export, backup and background execution review — 2026-11-18

Reviewed `DvrService`, `ResidentsService`, `WalletService`, `ExcelImportService`, `BackupService`, `ScheduledBackupService`, `BackupScheduler`, `BackupController`, `DvrsController`, and resident administration call sites. Findings below distinguish existing HTTP protections from service-level guarantees. No application code or data was changed.

### Service-level authorization and resource ownership

1. **DVR service methods are ID-based and have no actor/scope contract.** `DvrService.GetByIdAsync`, `UpdateAsync`, `DeleteAsync`, `DeleteCameraAsync`, and `GetDecryptedPasswordAsync` accept resource IDs without a caller, permission, or expected BuildingId. The reviewed `DvrsController` currently resolves the DVR and checks `CanManageAsync(dvr.BuildingId)` before edit/delete/secret-use operations, and constrains list queries by BuildingId. This is a controller-enforced boundary, not a service invariant. Future application-service APIs should accept an authorized building context or perform resource authorization centrally; raw credential decryption should not be a generally callable service method.
2. **Resident service mutations also rely on callers to enforce scope.** `ResidentsService.UpdateAsync`, `ChangePinAsync`, `SetDisabledAsync`, and `DeleteAsync` take a resident ID and actor label but do not independently validate the actor's role/building or persist an authorization decision. `AdminResidentsController` currently checks the resident's BuildingId before these calls. `SetApartmentPinAsync` has multiple call paths, including admin apartment management, Excel import, and a signed PIN-setup flow; those are distinct use cases and should become explicit application commands with their own proofs/authorization rather than one broad mutation API.
3. **WalletService is a domain calculator/mutator over an already loaded Building object.** It has no principal or scope evaluator, which is acceptable only if the application boundary guarantees that the Building aggregate was loaded for the authorized scope and every ApartmentId belongs to that aggregate. Preserve this as an explicit invariant; do not pass arbitrary IDs directly from request payloads into wallet operations.
4. **Repositories/stores and service methods are not security boundaries merely because controllers currently guard them.** The target design should separate request-facing authorized application services from internal persistence/domain helpers, and avoid exposing sensitive operations such as PIN replacement, secret decryption, restore, and financial transitions through generic ID-only methods.

### Backup and restore findings

1. **Direct restore overload bypasses the preview-session binding.** `BackupService.RestoreSessionAsync` consumes a one-use in-memory restore session bound to the initiating user and digest. However, a separate public `RestoreAsync(tempFilePath, selectedTables, overwrite, userId)` calls the restore core directly and bypasses the session/user binding. Repository search found only test call sites for this overload, not an HTTP production route. Keep it private/internal or replace it with an explicitly test-only seam; all production restore paths must consume a user-bound, one-use, expiring preview session.
2. **Restore preview sessions are process-local static memory.** The session dictionary and lock are not shared across instances. A preview on instance A followed by restore on instance B fails; multi-instance deployments require a shared, protected, one-use challenge/session store. The existing user binding, expiry, digest verification, safe temp-path validation, collection allowlist, and transaction-per-restore logic are useful controls to preserve.
3. **Backup creation can report success for incomplete output.** `CreateBackupAsync` silently skips unknown collection names and catches per-collection read/serialization errors, but sets `result.Success = true` after the loop even when `result.Errors` contains failures. A caller may therefore receive a partial archive presented as successful. Validate the entire requested collection set before work and distinguish complete, partial, and failed outcomes; do not label partial archives as a successful full backup.
4. **Backup and scheduled-backup services accept caller identity as a string, not an authenticated actor.** `CreateBackupAsync`, `SaveSettingsAsync`, `DeleteBackupAsync`, and restore APIs trust the supplied userId/role label for audit metadata. The current BackupController has class-level SuperAdmin authorization, but the service cannot establish who authorized the operation. Use an authenticated actor context supplied by a trusted application boundary; never accept a role string as proof.
5. **Backup file deletion spans filesystem and SQL without an atomic unit.** `DeleteBackupAsync` deletes the file before deleting its history row and then logs. A later SQL failure can leave stale history; the reverse ordering can leave an untracked file. Use a durable deletion state/outbox/reconciliation workflow and idempotent retries. Audit must be committed with the durable operation record, not treated as a best-effort afterthought.
6. **Restore direct and create/preview paths are sensitive system operations.** They must be SuperAdmin-only with mandatory MFA/step-up, explicit collection allowlists, fresh confirmation, durable audit, backup/rollback checks, and an emergency revocation path. Downloading an unencrypted backup is also a sensitive export and should be separately permissioned/audited.

### Excel import and export

1. **Import is a multi-step, non-atomic workflow.** `BackupController.ConfirmImport` creates the Building first, loads it, builds Floors/Apartments, calls `SaveFullAsync`, then applies each supplied apartment PIN through separate `SetApartmentPinAsync` calls. A failure after Building creation or during a later PIN assignment can leave a partially imported building and only some credentials initialized. Pre-validate all data, then use one transaction/application command for the full import (or a durable staged import with explicit resumable states); never report success until all steps and PIN outcomes are verified.
2. **The import confirmation payload is not a durable, auditable import job.** It is serialized into TempData between upload and confirmation. The target should bind an immutable import preview to the initiating account, expiry, file/data digest, and one-use confirmation; record row-level outcomes and idempotency so refresh/replay cannot duplicate a building.
3. **PINs are present in the parsed import model.** The export intentionally blanks Admin PIN and Apartment PIN cells, which is a good control. The import pipeline must keep PINs out of logs, audit metadata, exception text, filenames, and general-purpose persisted preview payloads; hash/verify each destination before marking the import complete. Avoid retaining plaintext PINs longer than necessary.
4. **Export surfaces have different risk levels.** `ReportsController.ExportCsv` exports financial aggregates and apartment balances and records an audit event; `BackupController.ExportJson/ExportExcel/ExportAllJson` are covered by class-level SuperAdmin role and backup permission. Treat these as distinct export permissions, scope and data-classification policies; do not infer that view permission implies export permission. Continue CSV formula-injection protection via `CsvExportSafety.Cell`.
5. **Building exports include contact/ownership data.** The Excel export includes owner and WhatsApp values. It omits PINs; preserve that omission and apply explicit export authorization, data minimization, access logging, and retention controls to personally identifying/contact data.

### Background jobs and cross-instance behavior

1. **Scheduled backup execution has no distributed lease/idempotency claim.** `BackupScheduler` wakes every 30 minutes and calls `ShouldRunAsync` followed by `RunScheduledBackupAsync`. The decision and run are not an atomic claim; multiple app instances can all observe the same due schedule and create duplicate backups/history entries. The timestamp-based filename also has minute precision and can collide. Add a persisted distributed lease/claim with owner, expiry, schedule occurrence key, and idempotent completion; use unique operation IDs rather than a minute-only filename.
2. **Scheduled backup operations are callable service methods without caller authorization.** The worker needs a trusted system identity, while manual `RunNow`, settings changes, file deletion and download need authenticated SuperAdmin authorization and step-up. Separate worker-only interfaces from request-facing administrative commands; do not expose a public `forceRun` flag as a general-purpose application capability.
3. **Backup completion spans file, history, schedule state, rotation and audit.** A crash can occur between file write, history insert, LastRun update, rotation and audit. Introduce an operation state machine and reconciliation so incomplete runs are detectable and safely retried. Multi-instance rotation must not delete a file currently being downloaded or created.
4. **Presence heartbeat is an authenticated but intentionally CSRF-exempt low-impact write.** It writes only the caller's claim-derived presence key and timestamps. Keep the exception narrowly scoped, rate-limit it, and ensure the caller cannot submit another user's identity/building/apartment. Do not generalize `IgnoreAntiforgeryToken` to other mutations.

### Consolidated target decisions and tests

- All request-facing commands resolve the actor from the authenticated principal and authorize the exact resource (Building, Apartment, Person, Account, or System) before mutation/read/export.
- Internal domain services may remain principal-free only when they are not directly exposed as request handlers and their caller passes a verified aggregate/context; raw secret, PIN, restore and destructive APIs require narrower internal interfaces.
- Use transaction + outbox for database changes and durable operation records for cross-resource/file workflows; add idempotency keys and explicit partial/failure states.
- Use shared one-use preview sessions and distributed leases for restore/import/scheduled jobs in multi-instance deployment.
- Add tests for direct service misuse, foreign BuildingId/ApartmentId/DvrId, role and permission denial, export-vs-view separation, restore user/session mismatch and replay, preview across instances, backup partial errors, import failure after every stage, duplicate scheduler execution, crash/retry around file/SQL/audit boundaries, and secret redaction.
- No source code, schema, database rows, credentials, backup files, or runtime state were changed. No build or tests were executed.


### Existing service/background test coverage review

Inspected `BackupServiceTests.cs`, `BackupRestorePersistenceTests.cs`, `ScheduledBackupServiceTests.cs`, `ResidentsServiceTests.cs`, and `ResidentsServiceSetPinTests.cs`.

- Backup tests already cover valid ZIP creation, preview validation, user-bound one-use restore sessions, replay rejection, digest tampering, archive-entry validation, and session count limits. Preserve these tests.
- `CreateBackupAsync_UnknownTable_SkipsIt` explicitly asserts that an unknown collection is skipped while `Success` remains true. This test currently locks in the partial-success behavior identified above; replace it with a fail-closed validation expectation and add a separate explicit partial-read failure test.
- Restore persistence tests cover insert/skip/overwrite and malformed-later-collection rollback behavior. The direct `RestoreAsync` overload is used by tests, so narrowing it requires a dedicated internal test seam rather than weakening production session binding.
- Scheduled backup tests cover input validation and normalization, but do not test concurrent scheduler instances, lease acquisition/expiry, duplicate occurrence suppression, crash recovery, file/SQL reconciliation, or rotation races.
- Resident service tests validate hashing and data mutations, but do not test actor role, building scope, resource mismatch, authorization denial, audit atomicity, or session/security-version revocation. These are not domain-unit test substitutes.
- No `ExcelImportServiceTests.cs` or `DvrServiceTests.cs` file was found at the inspected paths. Import transaction/replay and DVR secret/resource authorization therefore need new focused test suites.
- These were test-source inspections only. No tests were executed.

**Test work required before implementation acceptance:** replace the unknown-collection success test; add restore overload visibility/authorization guard tests; add cross-instance scheduler lease/idempotency tests; import transaction failure injection at every write stage and replay tests; service-level foreign-resource and actor-scope tests for DVR/resident/financial operations; and audit/outbox failure/retry tests.


## 15. Deletion, archival, cascade, and retention lifecycle review — 2026-11-18

Reviewed `Data/AppDbContext.cs`, `Data/BuildingSqlStore.cs`, `Data/BuildingSqlMapper.cs`, `Services/BuildingsService.cs`, `Services/ResidentsService.cs`, `Services/AdminManagementService.cs`, `Services/PollsService.cs`, and the existing resident-service tests.

### Confirmed destructive paths and relationship behavior

1. **Building deletion is a direct physical delete.** `BuildingsService.DeleteAsync` delegates to `BuildingSqlStore.DeleteAsync`, which executes `ExecuteDeleteAsync` against `Buildings`. There is no archive/retirement state, pre-delete impact report, explicit transaction orchestration in the service, or service-level retention policy in this path. The controller redirects without surfacing a structured deletion result.

2. **Building relationships mix cascade and restrictive behavior.** The EF model configures building-level cascade for settings, building-admin links, floors, apartments, financial categories, expenses, revenues, monthly shares, deposits, wallet adjustments, notifications, polls, maintenance records, QR tokens, and DVRs. AuditLog uses `SetNull` for BuildingId, which is a useful preservation behavior. However, several child-to-apartment relationships use `NoAction` (monthly shares, deposits, wallet adjustments, notifications, poll votes, and QR tokens), and Apartment-to-Floor also uses `NoAction`. A building delete or aggregate child removal can therefore encounter FK blockers depending on dependent rows and the actual deployed constraints. This is a source-model conclusion; no delete was executed against the restore database.

3. **Apartment removal is not a safe generic delete.** The mapper's `Sync` helper physically removes every existing row absent from an incoming aggregate. It is used for floors, apartments, categories, expenses, revenues, monthly shares, deposits, polls, poll options/votes, maintenance, and other collections. Removing an apartment also cascades to Residents through the configured composite FK, while financial and operational dependants configured with `NoAction` may block the save. A missing row in an in-memory aggregate is therefore currently interpreted as deletion, not as "not loaded" or "not authorized to edit".

4. **Aggregate replacement is broader than a single entity update.** `BuildingSqlMapper.SaveAsync` wraps its writes in a transaction and validates the Building rowversion, which are positive protections. But the incoming Building aggregate is still treated as the complete desired state for many child collections. If a caller submits a partial/filtered aggregate, omitted records can be physically removed. The mapper comment explicitly documents this overwrite behavior. All callers must be inventoried and classified as full-aggregate versus partial projections before any retention-safe redesign.

5. **Residents are physically deleted.** `ResidentsService.DeleteAsync` loads a Resident, calls `Remove`, and saves; its `deletedBy` argument is not persisted in the shown method. The current test `DeleteAsync_RemovesResident` explicitly expects the row to disappear. Because Residents is the apartment relationship/authentication record in the legacy model, physical deletion loses the relationship and its source context rather than ending a dated occupancy/access interval.

6. **Admin deletion is physical and multi-statement.** The previously documented `AdminManagementService.DeleteAsync` removes BuildingAdmins, UserPermissions, and Users; it also invalidates only the local session validator and writes audit afterward. This must be replaced with a retained account state and explicit access/role revocation lifecycle.

7. **Poll deletion is not protected by a ballot-retention lifecycle.** `PollsService.Delete` removes a poll from the aggregate list without checking whether voting has started or preserving a final immutable result. The mapper cascades Poll -> PollOptions -> PollVotes. A normal poll delete can therefore remove ballot rows and options. This is incompatible with the approved future secret-ballot and immutable election/audit history requirements.

8. **Additional dependent history is cascade-deleted.** DepositEditHistory cascades from Deposits; deleting a deposit removes its edit history. DVR deletion cascades to Cameras. Poll option deletion cascades to PollVotes. QR usage rows are not configured with a shown FK to QrTokens in the inspected model, so orphaned usage history may remain after token deletion; that may be desirable for audit but requires an explicit retention/identity policy. The model alone does not establish whether all deployed constraints match.

9. **Audit retention is inconsistent by design.** AuditLog is append-only in the aggregate mapper and its BuildingId is set null when the building is deleted. This preserves the row but can lose its direct building FK association; immutable building identifier/number and a retained subject reference should remain available for audit interpretation. AuditLog append-only in this mapper does not make all other audit writers durable or atomic; the separate audit/outbox findings in sections 10–14 still apply.

10. **Backup retention is file-oriented, not a full archival contract.** Scheduled rotation deletes old ZIP files based on filesystem creation time and a keep-count. Manual backup deletion removes the file and then deletes SQL history rows, so a failure can leave file/history inconsistency. There is no demonstrated legal hold, retention class, protected immutable archive, or restore verification policy in this path.

### Required target deletion and retention policy

- Replace ordinary deletion of Buildings, Apartments, Residents/occupancy, Persons, Accounts, Polls, Deposits, and other historically meaningful records with explicit lifecycle transitions: Active -> Suspended/Closed/Archived -> Retained, with separately governed legal erasure where required.
- A building should normally be retired/closed, not physically deleted. It must remain addressable for financial statements, ownership/occupancy history, audit, backup, and legal retention. Physical erasure must require a dependency report, retention/hold checks, explicit authorization, durable audit, and a tested restore/rollback procedure.
- Apartment closure must end occupancy/access intervals and disable apartment credentials without deleting historical ownership, occupancy, votes, financial shares, deposits, wallet adjustments, invoices, maintenance, notifications, or audit references.
- Resident deletion must become relationship termination and account/access revocation, preserving Person and account identity where legally allowed. A resident's move-out must not delete a Person who may occupy another apartment.
- Polls with any ballot must be immutable after opening/first vote except for controlled close/cancel metadata. Ballots and eligibility evidence need separate retention domains; deleting a poll must never silently erase votes.
- Posted/confirmed financial entries, cancellations, adjustments, monthly shares, carryovers, and deposit edit history must be append-only or corrected by compensating entries. A cancelled deposit is not equivalent to a deleted deposit. Draft/unposted records may have a separately approved correction policy.
- Keep restrictive FKs for legally significant history, but do not rely on FK errors as the retention strategy. Define explicit archive/retention states, parent-child lifecycle rules, and controlled purge jobs.
- Introduce a deletion-impact preview that enumerates affected records by domain and counts, including cross-building links, active residents, owners, accounts, financial history, votes, cameras, QR tokens, and backup artifacts. The preview must be revalidated at commit time to avoid TOCTOU races.
- Make every lifecycle transition transactional with an outbox/audit record and account security-version update where access is affected. Use idempotent operations and concurrency tokens.
- Define retention classes, legal holds, data minimization/anonymization rules, encrypted archive storage, retention expiry, and a separate irreversible purge approval. Preserve source-to-target provenance for migrated records.
- Change aggregate persistence contracts: explicit commands for add/update/close/remove; never infer deletion from omission in a partial DTO/projection. Keep full replacement only for an explicitly named, validated full-aggregate command with completeness proof.
- Replace cascade-delete defaults for historical domains with restrictive references or soft lifecycle semantics. Keep cascade only for disposable dependants whose loss is explicitly acceptable and tested.

### Tests required

- Building retirement preserves all finance, ownership, occupancy, audit, voting, and operational history.
- Physical deletion is blocked while legal hold/retention or dependent records exist.
- Apartment closure ends access but retains historical financial/occupancy links.
- Removing an apartment from a partial aggregate cannot delete it or its residents.
- Full aggregate replacement is accepted only with explicit completeness/version proof.
- Deleting/closing a resident does not erase their Person or other apartment relationships.
- Poll deletion/option edits are rejected after voting starts; closed ballot results remain immutable and auditable.
- Deposit deletion cannot cascade away edit history; posted financial records require compensating entries.
- DVR/camera retirement follows device evidence-retention rules.
- QR token expiry/revocation preserves the required usage/audit trail without permitting replay.
- Backup file/history rotation and legal holds remain consistent under crashes and concurrent workers.
- Failure injection at every lifecycle/audit/outbox stage proves atomicity or durable resumability.

This section is source-review evidence only. No application source, schema, database rows, backup files, or runtime state were changed. No build or tests were executed. Gate A remains **NOT AUTHORIZED**.


## 16. Runtime configuration, deployment, cryptographic key, and operational-readiness review — 2026-11-18

Reviewed `Program.cs`, `Services/ConfigSecrets.cs`, `Services/PinMigrationService.cs`, `Services/BackupScheduler.cs`, and `Services/SessionRevalidator.cs`.

### Confirmed configuration and deployment behavior

1. **Startup performs data mutation.** `Program.cs` unconditionally invokes `PinMigrationService.MigrateLegacySecretsAsync()` during application startup. That method hashes legacy User and Resident PIN values, clears Building.AdminPin, creates a primary Resident from an Apartment plaintext PIN when no Residents exist for that apartment, then clears Apartment.Pin. The operation is not exposed as a reviewed migration job with a dry-run/approval checkpoint. The startup catch logs failure and allows the application to continue. Some work may already have been saved before a later stage fails. This is a deployment-time write path and must be removed from ordinary startup before the redesigned identity system can be rehearsed safely.

2. **Configuration secrets are required, but key lifecycle is not yet an operational design.** `ConfigSecrets.Require` rejects missing values and placeholder values. The application requires Cloudinary, Excel, QR, encryption, and SuperAdmin allow-list configuration. This is a useful fail-fast baseline. However, the inspected startup wiring does not by itself define secret rotation, key versioning, dual-key decrypt during rotation, emergency key revocation, per-environment isolation, or an audited recovery procedure.

3. **Data Protection keys are persisted to a local filesystem path by default.** The default production path is under the current process user's LocalApplicationData; a configurable `DataProtection:KeysPath` can override it. This may work for one persistent host, but multiple instances need a deliberately shared, access-controlled, durable key ring and identical application name. Instance-local keys can make authentication cookies/session-protected payloads unreadable after load balancing or redeployment. The encryption key protecting the XML key ring also needs a managed rotation/recovery policy.

4. **Distributed cache registration is in-memory.** `AddDistributedMemoryCache` is configured. Despite the interface name, this is process-local and is not a shared distributed store. Session state and pending workflows relying on it cannot be assumed to survive instance changes or restarts. This aligns with the earlier findings for MFA pending challenges, restore sessions, and cross-instance security controls.

5. **Rate limiting is process-local and IP-keyed.** The configured `lookup` and `login` policies use in-process fixed-window limiters keyed by RemoteIpAddress. They do not coordinate across application instances. Correct client-IP attribution depends on trusted forwarded-header configuration at the hosting boundary; never accept arbitrary forwarded headers from untrusted clients. Login policy is only one layer and must be combined with account/device-aware, shared atomic throttling and credential-stuffing detection.

6. **The background backup scheduler is hosted by every application instance.** `BackupScheduler` is registered as a hosted service and wakes every 30 minutes. No distributed leader/lease is present in the worker itself. The previously documented scheduler service lacks a cross-instance atomic claim, so horizontally scaled deployments can trigger duplicate scheduled runs. Use a persisted lease/occurrence key and idempotent operation state.

7. **Authentication/session caches are not uniformly fail-closed.** Admin validation has a two-minute in-memory cache and a last-known-good path for verified-2FA sessions. Resident validation also caches for two minutes and may use a last-known-good result for up to 15 minutes if SQL lookup fails. Impersonation validation deliberately has no last-known-good fallback and uses a shorter cache. The target must apply one explicit policy matrix by operation risk: emergency disable/revoke, financial write, secret access, backup/restore, and ordinary read must not inherit stale authorization accidentally.

8. **Health endpoints distinguish liveness and readiness.** `/health/live` avoids the SQL readiness check; `/health/ready` and the compatibility `/health` endpoint include it. This is a positive deployment pattern. Before exposure, confirm that response details reveal no connection strings, schema names, secrets, or sensitive infrastructure information and that readiness is used by deployment orchestration.

9. **Request protection has useful defaults but needs a deployment contract.** Global automatic antiforgery validation is registered for MVC controllers, and production uses exception handling, HSTS, and HTTPS redirection. The application also configures forwarded headers and a request-size limit. Review the exact trusted proxy/network settings, cookie Secure/HttpOnly/SameSite behavior, CSP exceptions, upload limits by endpoint, and API/anonymous route exceptions as part of the deployment acceptance suite.

10. **Cancellation and operational observability need consistency.** Some persistence paths (notably the aggregate mapper) accept CancellationToken and use it in EF operations, while many service/controller workflows do not expose request cancellation. Long-running import, export, restore, and report operations should propagate cancellation while preserving transactional cleanup and durable operation state. Logs must carry correlation/operation IDs and redact credentials, PINs, tokens, recovery material, connection strings, and financial personal data.

### Required target controls

- Remove all data-changing migration work from application startup. Replace it with an explicit, separately invoked, versioned migration command that supports dry-run, approval, bounded batches, per-record outcome, idempotency, concurrency checks, secure logs, backup verification, and rollback/recovery. Do not clear any source secret until the destination credential is verified and the record outcome is durable.
- Use a supported secret manager/key vault for production secrets, with versioned keys, controlled rotation, dual-read transition where needed, emergency revocation, access auditing, and documented recovery. Never print secret values or include them in diagnostic exports.
- Configure a durable shared Data Protection key ring for all instances, protected by managed key encryption, with restricted filesystem/secret-store permissions and a tested restore procedure. Ensure application name and key-ring access are consistent across slots/instances.
- Replace process-local distributed-memory state with an actual shared store where cross-instance continuity is required: authentication/session challenges, restore previews, rate-limit counters, revocation versions, scheduler leases, and one-time tokens.
- Define trusted reverse-proxy headers and enforce HTTPS/cookie security at the edge and application layers. Verify the remote IP used by throttling cannot be spoofed.
- Add a durable scheduler lease, occurrence idempotency key, crash recovery, and backup-file/SQL-history reconciliation.
- Create an operational runbook for key rotation, secret compromise, account emergency revocation, database outage, restore, partial import, backup corruption, and failed migration.
- Make readiness/liveness responses safe and deployment-aware; add checks for shared key-ring access, required secret configuration, database schema compatibility, and critical background-worker state without disclosing secret values.
- Propagate cancellation and operation IDs through long-running workflows; a cancellation must not leave an ambiguous partially committed state.
- Test multi-instance behavior, restart/redeploy behavior, key-ring recovery, proxy/IP handling, secret rotation, startup with migration disabled, and fail-closed authorization during database outages.

This is a source/configuration review only. No application source, configuration values, secrets, database rows, or runtime state were changed. No application was started and no build/tests were run. Gate A remains **NOT AUTHORIZED**.


## 17. Final cross-domain consistency, test-gap, and implementation-readiness assessment — 2026-11-18

This final pass reconciles the source reviews in sections 1–16 with the blueprint, target data dictionary, legacy mapping, Gate A SQL, Gate A constraint review, and the test suites. It is a static review; it does not certify runtime behavior or authorize implementation.

### Cross-artifact consistency

- The central target principles are consistent across the blueprint, data dictionary, mapping, and Gate A design: Person-centric identity, at most one interactive account per Person, independent ownership/occupancy/access, scoped authorization, apartment-scoped finance, secret-ballot separation, restrictive historical FKs, and additive migration.
- The LegacyIdentityMap correction is now reflected in the Gate A SQL, preflight inventory, mapping, and target dictionary. Its remaining service invariant is to validate that a Users source row's AccountId belongs to the same PersonId within one transaction.
- Account-specific legacy permissions are represented separately from role-wide permissions through AccountPermissionOverrides; the source permission key must still be classified into a verified scope before any backfill.
- Gate A SQL and the constraint review correctly distinguish database-enforceable rules from transactional service invariants. In particular, interval overlap, ownership-share totals, role/scope compatibility, account/person mapping pairs, and grant revocation cannot be inferred from FKs alone.
- The schema review still has an explicit unresolved ApartmentAccessGrants GrantType catalogue and lifecycle/revocation policy. Do not freeze the physical schema until this is resolved.
- The blueprint and target dictionary require archive/retention semantics, but the current legacy code still physically deletes or cascades several of those same domains. The target design is directionally correct; implementation must not inherit the current delete semantics.
- The checked-in FullSchema SQL, EF model/migrations, and inspected restore database are separate evidence sources. Never treat one as proof of the others. Every proposed migration must reconcile all three against a fresh read-only inventory of the exact disposable restore.

### Product and policy decisions still required before design freeze

1. Identity resolution: who approves Person merges, what evidence is acceptable, and how unresolved/shared-contact cases remain separate.
2. Ownership: whether shares must total exactly 100%, whether partial/unknown ownership is valid, and how conflicting effective periods are resolved.
3. Occupancy: trusted effective-date sources, overlap policy, optional primary occupant behavior, and move-in/move-out approval.
4. Apartment access: final GrantType catalogue, who may grant/revoke, delegation rules, expiry, emergency access, and audit reason requirements.
5. Authorization: approved role catalogue, role-to-scope compatibility, permission catalogue, direct allow/deny precedence, and resource-level enforcement matrix.
6. Account lifecycle: disabled/suspended/locked/archived states, account recovery, person retention, duplicate account resolution, and legal erasure authority.
7. Credentials: legacy PIN reset/re-enrollment policy, resident/admin authentication methods, password policy, external identity provider support, and credential compromise response.
8. MFA: mandatory administrator/SuperAdmin enrollment timing, resident step-up actions, recovery-code replacement, lost-device recovery, and emergency break-glass process.
9. Sessions: global versus per-device sign-out, revocation latency across instances, behavior during database outage, impersonation expiry, and actor/target permission separation.
10. Preferences: apartment defaults versus person-specific language, notification, and payment preferences, including precedence and migration compatibility.
11. Finance: treatment of carryovers, monthly shares, confirmed/cancelled deposits, adjustments, rounding variances, and any historical wallet-view/report dependencies.
12. Voting: legacy apartment-attributed vote retention, future eligibility snapshot, representative delegation, and required assurance level for secret-ballot unlinkability.
13. Retention: legal retention periods/holds by data class, building retirement, financial immutability, audit retention, backup archive and purge policy.
14. Operational deployment: shared session/cache/key-ring provider, secret manager, key rotation, trusted proxy configuration, scheduler lease, and multi-instance recovery behavior.
15. Time and money conventions: authoritative business timezone, UTC storage/presentation rules, decimal precision/rounding, and effective-time semantics.
16. External compatibility: identify any reports, SQL jobs, integrations, mobile clients, exports, or scripts that directly depend on legacy tables/views before retiring or changing them.

These decisions must be recorded as approved business rules, not silently inferred from legacy code or existing row shapes.

### Test coverage gaps confirmed in the current test source

- `BuildingsServiceTests` covers summaries and lookup behavior; no building deletion/retirement test was found.
- `ResidentsServiceTests.DeleteAsync_RemovesResident` explicitly expects physical deletion. It must be replaced with lifecycle/retention assertions when the new behavior is implemented.
- `PollsServiceTests.Delete_ExistingPoll_RemovesIt` explicitly expects a poll to be removed. Add a rule that deletion is rejected after opening/ballots and that retained results remain immutable.
- No cascade-retention or aggregate-partial-payload deletion guard tests were found in the inspected test paths.
- `SessionRevalidatorCacheInvalidationTests` verifies the local in-process version increments; it does not prove cross-instance revocation. Impersonation tests cover several actor/target and fail-closed cases, but do not replace a distributed security-version test.
- Controller authorization tests protect metadata and selected permission checks; they do not constitute full route/resource authorization tests across all roles, buildings, apartments, export paths, and direct service calls.
- The earlier service review found missing/insufficient test suites for DVR authorization, import failure/replay, scheduled backup concurrency, audit/outbox failure, and service-level actor/resource scope.
- No tests were executed during this review. Test names and assertions were inspected only.

### Consolidated implementation acceptance checklist

Before any application implementation is accepted:

- [ ] Freeze all policy decisions above and approve a versioned ERD/data dictionary.
- [ ] Reconcile exact SQL Server metadata, EF model, migration history, generated FullSchema, views, triggers, SQL jobs, and external consumers.
- [ ] Define every domain lifecycle and delete/retention matrix, including all cascade and restrictive FK actions.
- [ ] Complete identity candidate dispositions without fuzzy auto-merges; preserve provenance.
- [ ] Approve permission-to-scope mapping and a resource/action authorization matrix.
- [ ] Finalize ApartmentAccessGrants types and revocation/audit lifecycle.
- [ ] Define the account security-version, MFA recovery, session revocation, distributed cache, and outage policy.
- [ ] Define finance reconciliation tolerances and resolve all carryover, deposit, adjustment, and wallet-report dependencies.
- [ ] Finalize poll eligibility and ballot separation, immutable lifecycle, and legacy-vote treatment.
- [ ] Remove all startup data migration and replace it with an explicit, reviewed, restartable migration command.
- [ ] Implement transactional domain mutations plus durable audit/outbox and idempotent background operations.
- [ ] Add multi-instance, concurrency, failure-injection, retention, foreign-resource, and rollback tests.
- [ ] Rehearse additive schema, backfill, compatibility, cutover, and reverse recovery on a disposable restored copy.
- [ ] Run the full build/test/security/regression suite and document actual outputs; then perform user-approved smoke tests.
- [ ] Obtain separate explicit approval before any Gate A DDL execution, and separate approval again for backfill/cutover. Production changes require their own release approval.

### Final review disposition

The discovery and static source review is now complete through the currently available repository and supplied restore evidence. The design direction is viable, but the application is **not implementation-ready for identity cutover** until the policy decisions, retention model, transactional authorization/security services, finance reconciliation, and deployment controls above are closed.

**Gate A remains NOT AUTHORIZED.** No DDL/DML, EF migration, source-code change, backfill, cutover, production access, or destructive operation was performed. No build, test suite, or runtime validation was executed in this review.
