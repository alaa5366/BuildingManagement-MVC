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


### 18. SQL views, reporting projections, and observability/log-center architecture review — 2026-11-18

This section records a repository-wide static inventory of view declarations and EF mappings found through source search. It does **not** claim a complete inventory of the live SQL Server database: the supplied restore evidence previously confirmed that `vw_ApartmentWalletMovements` and `vw_ApartmentWalletBalance` were absent, but no all-views result set from `sys.views` has yet been supplied. A dedicated read-only SQL inventory is still required before any view is changed or retired.

#### 18.1 Repository inventory

- The only `CREATE VIEW` declaration found in `BuildingManagement-FullSchema.sql` is `dbo.vw_ApartmentWalletMovements`.
- `Data/AppDbContext.cs` exposes one keyless query type, `ApartmentWalletMovementsView`, mapped with `HasNoKey()` and `ToView("vw_ApartmentWalletMovements")`.
- `Data/Entities.cs` contains the corresponding projection type.
- The EF model snapshot and migration designer snapshots repeat the mapping metadata. Those references are **not** SQL view-creation statements.
- No repository definition, EF mapping, or consumer for `vw_ApartmentWalletBalance` was found. The earlier restore inspection also reported both wallet view names absent.
- No additional `CREATE VIEW`, `CREATE OR ALTER VIEW`, or `ToView(...)` declaration was found in the searched repository sources. This is a source-search result, not proof that the live database has no other views or that external SQL modules do not depend on them.

#### 18.2 Current wallet view assessment

`vw_ApartmentWalletMovements` returns one row per apartment and aggregates confirmed deposits plus wallet adjustments. It is a movement summary, not the authoritative apartment balance or a complete statement. It does not include carryover, monthly charges/shares, expense/revenue distribution, month selection, or a full accounting reconciliation. The existing code review found that `WalletService` calculates wallet behavior independently and no runtime consumer of this view was identified.

Consequences:
- Do not rename this view or expand its meaning while retaining the old name.
- Do not create `vw_ApartmentWalletBalance` from assumptions; its intended semantics and external dependencies are unknown.
- Decide whether the projection is still useful. If retained, give it a precise contract and make its grain, currency/decimal rules, status inclusion, and as-of semantics explicit.
- The authoritative balance must have one defined calculation path, with reconciliation against confirmed deposits, adjustments, monthly shares, carryover, expenses/revenues, reversals, and rounding. A view must not silently duplicate a different balance formula from `WalletService`.
- Keep historical financial rows immutable or reversibly corrected; do not let view refactoring change historical totals.

#### 18.3 Target view and reporting architecture

The redesign should produce a deliberate, versioned set of read-only reporting projections rather than adding ad-hoc views. Candidate domains (subject to actual query/report needs and profiling) are:

1. **Apartment wallet statement** — transaction-level rows with source type/id, effective date, posted date, month, signed amount, status, reversal/reference, and deterministic ordering. This is a statement projection, not just a total.
2. **Apartment wallet balance** — one row per apartment and as-of date/month, with separately exposed opening/carryover, confirmed receipts, adjustments, allocated charges, expenses/revenues where applicable, closing balance, and reconciliation status. Business rules must be approved first.
3. **Building financial period summary** — period-level totals and reconciliation deltas, preserving source totals and rounding evidence.
4. **Apartment occupancy/ownership/access projection** — current and historical effective-dated relationships, with explicit distinction between owner, occupant, and temporary access; never collapse these into a single resident flag.
5. **Authorization/reporting projection** — optional administrative read model for effective grants and scope, generated from the centralized evaluator's rules; it must not become a second authorization engine.
6. **Audit and operational event read models** — searchable, paged projections for the log center, kept distinct from financial and domain views.

These are candidates, not approved object names or committed SQL. Prefer a small number of stable contracts with explicit versioning (for breaking changes use a versioned view/API contract or compatibility window). Use views for relational projection, not for secret decryption, authorization decisions, state transitions, or write workflows.

#### 18.4 Performance, security, and maintainability rules

- Inspect actual execution plans, row counts, logical reads, and Query Store evidence before adding indexes or rewriting a view. Avoid scalar functions and row-by-row correlated work on large reporting paths where a grouped/relational plan is better.
- Aggregate child tables before joining them to avoid fan-out multiplication (e.g., deposits × adjustments × shares).
- Use deterministic keys and ordering for paging; include stable tie-breakers.
- Make date boundaries sargable and define business timezone/UTC conversion explicitly.
- Keep decimal precision and rounding policy consistent with the financial ledger; never use floating point for money.
- Apply resource scope in the authorized query/application layer. A view that returns all buildings is not safe merely because the caller hides rows in the UI. If SQL row-level security is considered, document identity propagation, connection pooling, bypass paths, and test coverage first.
- Grant application principals only required SELECT access; keep DDL ownership separate. Views are not automatically a security boundary.
- Track each view's owner, purpose, grain, source tables, filters, freshness, consumers, expected row volume, indexes relied upon, and deprecation path.
- Do not use `SELECT *`; keep column contracts explicit and test them.

#### 18.5 Log-center / Igor-style observability direction

The requested log experience should be treated as a first-class **Enterprise System Monitoring & Log Center**, not as a collection of SQL views over the existing AuditLog table. It should bring together:
- structured application logs (level, timestamp, service/environment/instance, category, event id, exception);
- correlation/trace identifiers across HTTP request, database work, background jobs, import/export, backup/restore, and security events;
- a distinct append-only business/security audit trail with actor, action, target, scope, outcome, reason, and before/after metadata where safe;
- performance telemetry (request duration, SQL duration, query count, slow-query thresholds, job duration, failure/retry counts);
- system health and dependency status;
- searchable filters, time ranges, severity, event category, actor, building/resource scope, correlation id, and export/retention controls.

Important separation:
- Application diagnostics answer **what the software did and why it failed**.
- Security/business audit answers **who performed which sensitive action, against what resource, and with what outcome**.
- Financial ledger/reporting answers **what monetary event occurred and how balances reconcile**.
These streams may be correlated by trace/correlation and domain event identifiers, but must not be merged into one mutable table or treated as interchangeable.

The log center should use structured logging and a supported centralized sink/store with retention and access controls. Avoid logging PINs, passwords, MFA seeds/recovery codes, tokens, connection strings, DVR credentials, ballot choices, or unnecessary personal/financial payloads. Sensitive values must be redacted before the sink. Use bounded-cardinality dimensions; do not put arbitrary user input or IDs into metric labels. Audit writes for critical mutations should use a transactional outbox or equivalent durable mechanism; ordinary diagnostic logs must not be mistaken for durable audit evidence.

A future UI should provide paged server-side search, saved filters, event detail with redaction, correlation/trace drill-down, slow-operation views, health panels, and controlled export. SuperAdmin access must be protected by MFA/step-up and itself audited. The UI must not issue unrestricted database-wide queries on each page load.

#### 18.6 Required read-only database inventory before view redesign

The next diagnostics run must export separate result sets for:
- all user-visible and system SQL views (schema/name, create/modify dates, object id);
- each view's full definition and whether encrypted/definition unavailable;
- SQL dependency edges from `sys.sql_expression_dependencies` plus unresolved references;
- indexed views and their index definitions;
- referencing modules (procedures, functions, triggers) and synonyms;
- SQL Agent jobs/steps and external report/integration references where available;
- row counts and approximate size for source tables;
- existing Query Store/query-statistics evidence for view consumers, where enabled and permitted.

Compare this inventory with EF mappings, migrations, `BuildingManagement-FullSchema.sql`, controllers/services/repositories, report/export code, scheduled jobs, and external consumers. A view may only be removed after its consumers and compatibility window are verified.

#### 18.7 Current disposition

Repository source inventory: **one declared SQL view and one EF keyless mapping found**. Live database inventory: **not yet complete**. Wallet view semantics: **insufficient for an authoritative balance**. The target is a coherent reporting/read-model layer plus a separate structured log center; no new view SQL is approved yet.

No source code, SQL view, schema, database row, or runtime setting was changed. No build, tests, execution plans, or live SQL inventory were run in this section. Gate A remains **NOT AUTHORIZED**.


#### 18.8 Live restore inventory completed — 2026-11-18

Reviewed the read-only export at `Database/Diagnostics/View-Inventory-Results/20261118-163035/` (generated 16:30:36 +02:00) from `(localdb)\\MSSQLLocalDB`, database `BuildingManagement_RestoreTest`, branch `development/database-redesign-enterprise`.

**Observed results:**

- `01-Views.csv` contains no rows: the restored database currently has **zero SQL views**.
- `02-View-Definitions.csv` contains no rows: no view definitions were returned.
- `03-Dependency-Edges.csv` and `04-View-Consumers.csv` contain no rows: no SQL-module-to-view dependency or referencing-module rows were returned by this inventory.
- `05-Indexed-View-Indexes.csv` contains no rows; there are no indexed views in this restore.
- `06-Synonyms.csv` contains no rows.
- `09-QueryStore-View-Usage.csv` contains no rows matching the inventory query's `vw_` / `ApartmentWallet` criteria.
- Query Store is `READ_WRITE`, desired state `READ_WRITE`, capture mode `AUTO`, current storage approximately 1 MB, maximum 1000 MB.
- The source-table size export reports 3,150 rows in `dbo.AuditLog`, 237 in `dbo.Apartments`, 237 in `dbo.Residents`, 204 in `dbo.Notifications`, 113 in `dbo.MonthlyApartmentShares`, 78 in `dbo.Deposits`, 36 in `dbo.Expenses`, 12 in `dbo.Revenues`, and 6 in `dbo.WalletAdjustments`. These are approximate storage/row-count metadata, not a financial reconciliation.

**Interpretation and scope boundary:**

This live result supersedes the pending-live-inventory wording in sections 18.6–18.7: the dedicated `sys.views` export has now completed and found no SQL views in this restored database. It does **not** invalidate the separate repository finding that `BuildingManagement-FullSchema.sql` declares `dbo.vw_ApartmentWalletMovements` and `AppDbContext` contains a keyless mapping for it. The source artifact, EF model, and restored database are therefore not aligned. The previously named `vw_ApartmentWalletMovements` and `vw_ApartmentWalletBalance` are both absent from this restore; do not assume the first is deployable or that the second exists.

The empty Query Store result means only that this query found no captured matching text under the current Query Store state and permissions. It does not prove that no application code, dynamic SQL, external report, SQL Agent job, SSRS report, or separately deployed database consumes or expects a view. The runner README explicitly excludes SQL Agent jobs/steps, SSRS catalogs, dynamic SQL references, and external application consumers. Those require a separate inventory before any compatibility/removal decision.

**Next design action:** reconcile the source SQL artifact and EF mapping with the actual deployment/migration path. Determine whether the wallet movement projection is intended to be deployed, is legacy dead metadata, or is supplied by an external deployment package. Trace all report/export consumers and validate the required columns and semantics before creating or removing any view. Do not create a wallet-balance view until the balance formula and financial source-of-truth rules are approved.

No schema or data changes were issued by the inventory script. No build or tests were run. **Gate A remains NOT AUTHORIZED.**


#### 18.9 EF migration/deployment-path cross-check — 2026-11-18

A follow-up repository search and direct inspection of the four checked-in migration source files confirms:

- `Data/AppDbContext.cs` exposes `DbSet<ApartmentWalletMovementsView>` and maps it with `HasNoKey()` / `ToView("vw_ApartmentWalletMovements")`.
- `Data/Entities.cs` defines the keyless projection type.
- The EF model snapshot and migration designer files repeat the mapping metadata, as expected.
- The actual migration operation files `InitialCreate`, `Phase04_StandardizeKeyColumnTypes`, `Phase05_RemoveRedundantResidentIndexes`, and `AddUserTwoFactor` contain no `CREATE VIEW` or reference to `vw_ApartmentWalletMovements`.
- The only checked-in executable SQL definition located by the repository search is in `BuildingManagement-FullSchema.sql`.

**Finding:** the EF mapping is metadata only; it does not create the SQL object. The current checked-in EF migration chain does not deploy the view. A database created or upgraded solely through these migrations can therefore lack the object while the DbContext still advertises the keyless query type. The FullSchema script appears to be a separate deployment path and must be reconciled with the EF migration path. Do not treat model snapshot/designer entries as a migration operation.

**Required next decision:** choose one supported deployment source of truth. Either (a) add a deliberate, reviewed, idempotent migration for the view and test fresh-install plus upgrade paths, or (b) remove/retire the unused mapping and source SQL definition after proving there are no runtime/report consumers. Do not do either until a consumer search and runtime usage decision are complete. No application source or migration was changed in this review.


## 19. Reporting, wallet read paths, audit queries, and export review — 2026-11-18

This section is a static source review of `ReportsController`, `ReportsService`, `WalletService`, `WalletController`, `ResidentWalletController`, `AuditLogController`, `AuditLogQueryService`, `AuditLogger`, `CsvExportSafety`, `BuildingSqlStore`, `BuildingSqlMapper`, and the relevant EF mappings. No runtime query plans or fresh application tests were executed.

### 19.1 Financial reporting is built from a hydrated aggregate, not SQL read models

- `ReportsController.Index` and `ExportCsv` load the building through `BuildingsService.GetByIdAsync(BuildingId)`, then call `ReportsService.BuildReport`.
- `ReportsService.BuildReport` iterates the in-memory `Building.Months`, apartments, categories, expenses, and revenues. It computes each month's confirmed deposits by iterating apartments and calling `WalletService.TotalConfirmedDeposits`; wallet balances and top debtors/creditors are also computed in memory.
- `GetReportMonths` supports `all`, `year`, `12m`, and otherwise defaults to a six-month window. An arbitrary invalid range is not rejected; it falls through to the six-month behavior.
- The current report's `NetBalance` is `TotalCollected - TotalExpenses + TotalRevenues`. It is a period cash-flow-style arithmetic total, not the sum of apartment wallet balances and not a complete balance-sheet/liability measure. It does not directly include monthly fees, opening/carryover balances, or manual wallet adjustments.
- The report's wallet chart and debtor/creditor lists instead call `ComputeWalletBalance` for the current month. These are different measures with different time semantics: selected range for summary and current month for wallet positions.
- `AvgMonthly` divides the selected-range net total by the number of selected month keys, including months without a corresponding `MonthData` record in rolling ranges. This should be explicitly described as an average over calendar months, not only months with activity.
- `ExportCsv` rebuilds the same report and exports its summary/monthly/category sections. The export must retain the exact metric definitions and selected range used by the UI.

**Design consequence:** define separate, named contracts such as `FinancialActivitySummary` (period inflows/outflows), `ApartmentWalletStatement` (posted events), and `ApartmentWalletBalance` (as-of position). Do not name a cash-flow metric simply `Balance` without a clear definition. A future SQL read model must reproduce approved business formulas, not merely aggregate similarly named columns.

### 19.2 Wallet balance rules and historical dependencies

The inspected `ComputeWalletBalance` formula is:

`carry-over + confirmed deposits - monthly fee (unless apartment closed) - expense distribution + revenue distribution + wallet adjustments`.

Additional observations:

- Pending and cancelled deposits are excluded from confirmed collections and balances.
- Expense and revenue distributions are stored in monthly aggregate dictionaries and are calculated over open apartments. The distribution routine rounds shares to cents and assigns the remaining cents to the final apartment in the current apartment ordering.
- `ComputeWalletBalance` returns zero if the apartment or month is absent; it does not synthesize a missing month.
- `ComputeCarryOverForNextMonth` uses the prior month's computed wallet balance. `RecomputeCarryOverFrom` recalculates later existing months in key order.
- `TotalsOf` returns both `Balance = collected + revenues - expenses` and `TotalWallet = sum of apartment wallet balances`. The former is not the latter. Consumers must not treat them as interchangeable.
- The source code uses mutable in-memory aggregate dictionaries and lists. A relational projection must define how missing distributions, closed apartments, apartment additions/removals, month gaps, historical corrections, and carry-over recalculation are handled.
- The current view definition in `BuildingManagement-FullSchema.sql` only describes confirmed deposits plus adjustments; it cannot replace the wallet formula.

**Required finance decisions before SQL view design:** authoritative month-close policy; whether a monthly fee is assessed for closed/part-month apartments; treatment of apartments added or closed after month start; effective-date and ordering policy for distributions; adjustment/reversal semantics; month gaps; carry-over snapshots vs recomputation; and rounding/remainder allocation independent of incidental row order. Reconciliation tests must compare per-apartment balances and building totals over multiple consecutive months, including corrections to earlier months.

### 19.3 Aggregate loading and scalability implications

The current `BuildingSqlMapper.LoadAsync` hydrates one building by issuing separate queries for the building row and many child collections (floors, apartments, categories, expenses, revenues, monthly shares, deposits, deposit edits, adjustments, notifications, polls/options/votes, maintenance, admins, and capped aggregate-originated audit rows). It then materializes the aggregate in memory for existing services.

This is a compatibility architecture, not a query-oriented reporting architecture. A report request therefore pays for a broad building load even when the page needs only a few monthly totals. Query count and materialized row volume grow with building history and operational activity. A future read path should query only the required period and building scope, aggregate at the correct grain, and page detail rows in SQL. It must not replace the aggregate write path prematurely.

The load path uses separate queries rather than a single multi-collection join, which avoids one obvious join fan-out pattern, but it still has multiple round trips and potentially large in-memory collections. The actual cost must be measured with realistic larger datasets and execution telemetry before selecting a replacement.

### 19.4 Audit log query and export behavior

- `AuditLogController` requires role-scoped audit permissions and derives resident building/apartment scope and admin building scope from claims before calling the query service. The CSV action applies the same scope construction.
- `AuditLogger.QueryPageAsync` applies SQL-side filters, runs a separate `CountAsync`, clamps page size to 1–200, and uses deterministic `Ts DESC, AuditId DESC` ordering before `Skip/Take`. This is the correct baseline pattern for the UI, although deep OFFSET pages can become expensive as history grows.
- The non-paged `QueryAsync` clamps its limit to 1–50,000 and orders only by `Ts DESC` (without the `AuditId` tie-breaker). Any CSV export built on this path is bounded but can have unstable ordering for equal timestamps and can still materialize a large result set. A future export should use a stable keyset/cursor or bounded streaming strategy, enforce an explicit date/range ceiling, and preserve resource scope.
- `AuditLogger.LogAsync` creates its own DbContext and saves the audit row independently. Exceptions are caught and logged as warnings. This confirms the previously recorded durability gap: the business mutation and audit insert are not one atomic transaction/outbox operation. Critical audit events can be lost while the main operation succeeds.
- The logger validates the building and apartment association before storing the apartment number/id. This avoids attaching an apartment identifier to an unrelated or nonexistent building, but does not itself guarantee audit durability.
- Correlation id is added to serialized metadata when an Activity tag is present. It is useful correlation metadata, not a replacement for a durable event id or transactional outbox.

**Log Center consequence:** retain separate stores/contracts for diagnostic application logs, durable security/business audit, and financial ledger events. Add a stable event identifier, actor/resource scope, correlation/trace identifiers, outcome, and safe structured metadata. Critical audit writes need a transactional outbox or equivalent durable commit path. Search/export must be bounded, scoped, paged, and redacted.

### 19.5 CSV export controls

The inspected report and audit CSV paths call `CsvExportSafety.Cell` for each cell, and the repository contains focused `CsvExportSafetyTests`. This is a positive control against spreadsheet formula injection. It does not prove every export endpoint uses the helper. A repository-wide export inventory and tests for every endpoint are still required, including backup/JSON exports and any dynamically generated CSV.

Export review acceptance criteria:
- every user-controlled text cell is neutralized consistently;
- numeric/date serialization uses invariant formats and explicit timezone rules;
- columns and metric definitions are versioned/documented;
- exports preserve the same resource scope as their page;
- large exports have bounded ranges/streaming and are audited;
- secrets, PINs, MFA material, tokens, camera credentials, and private ballot data are excluded.

### 19.6 Next work items and disposition

1. Inventory every export endpoint and verify its permission, scope, formula-injection defense, range limit, and audit behavior.
2. Trace report and wallet UI consumers to identify the exact required fields and metric labels.
3. Build finance golden-case tests for multi-month carry-over, closed/open apartments, adjustments, cancelled/pending/confirmed deposits, zero/negative distributions, rounding remainders, and historical corrections.
4. Design read models only after metric definitions and reconciliation rules are approved; prefer period-filtered SQL projections over loading the full building aggregate for report pages.
5. Keep audit durability and Log Center architecture as a separate workstream from financial reporting and views.

No application code, migration, schema, or database data was changed. No build, automated tests, runtime profiling, or execution plans were run for this section. **Gate A remains NOT AUTHORIZED.**


## 20. Backup and data-export endpoint review — 2026-11-18

Static inspection of `BackupController`, its class-level authorization, and the export/backup actions extends the CSV review to JSON, Excel, and encrypted backup downloads.

### 20.1 Authorization boundary

The controller is class-protected by `[Authorize(Roles = "superadmin")]` and `[HasPermission(AdminPermissions.ManageBackup)]`. This applies to its backup, restore, import, and export actions, including `DownloadBackup`, `ExportJson`, `ExportAllJson`, and `ExportExcel`. This is a strong coarse boundary; it is not a substitute for action-level resource scope if exports are later exposed to building administrators.

### 20.2 Export formats and data exposure

- `ExportJson(id)` loads the full building aggregate and serializes it. It is a broad export of building-domain data, not a narrow reporting projection. The current aggregate mapper explicitly rehydrates `AdminPin` and apartment `Pin` as empty strings, which is an important safeguard for this path. This guarantee depends on every future loader preserving the blank-secret contract.
- `ExportAllJson()` exports only a wrapper containing timestamp, actor label, count, and building summaries with counts; it is not a full multi-building data dump.
- `ExportExcel(id)` exports building identity/contact information, apartment owner/phone/monthly fee/status/notes, and category data. The Admin PIN and apartment PIN columns are deliberately blank. The endpoint does not currently export financial ledger details.
- `DownloadBackup(fileName)` reads the stored encrypted backup, decrypts it in application memory, and returns a ZIP. This is the most sensitive export path because the archive may contain many database collections and personal/financial records.
- The inspected export actions do not inject an audit logger or record a dedicated export event in their own controller methods. Do not assume that access is durably audited merely because the route is SuperAdmin-only. Confirm whether middleware or another layer records these downloads before accepting the audit requirement as met.
- JSON and XLSX exports do not pass through `CsvExportSafety`; CSV formula-injection rules are not directly applicable to JSON, while XLSX needs its own explicit cell-type/formula-injection tests for user-controlled strings.

### 20.3 Backup/restore and import observations

- Backup creation is a POST with antiforgery protection and delegates selected collection names to `BackupService.CreateBackupAsync`. The previously documented behavior remains: unknown collection names can be silently skipped while the operation still reports success; this needs explicit rejection or an accurately reported partial result.
- Backup downloads are GET actions. Because they return decrypted archive bytes, they should be audited as sensitive data access and protected against accidental caching; consider response cache controls and a dedicated download event.
- Excel import validates the uploaded workbook, stores the parsed model in TempData, then confirmation creates the building and saves its aggregate before assigning apartment PINs one by one. A failure during subsequent PIN assignment can leave a partially completed import. This remains a transactional workflow gap.
- Import confirmation trusts the parsed TempData payload as its staged input; the future design should bind the preview to the initiating account, target, expiry, immutable content digest, and one-use state, then commit the building and all associated identity/PIN records atomically or provide explicit compensating rollback.
- The Excel template/import format contains PIN fields, so logs, TempData, exception messages, temporary storage, and backup/restore processing must be reviewed for secret exposure. The current export deliberately blanks PIN values, but that does not make the import pipeline safe by itself.

### 20.4 Required acceptance criteria

1. Record an append-only audit event for each backup create/download/delete, JSON/XLSX export, full export, restore preview/execute, and import preview/confirm. Include actor, operation, target/scope, outcome, timestamp, correlation id, and export format; never include archive contents or PINs.
2. Set safe response cache headers for decrypted backup and personal/financial exports.
3. Verify filename/path validation and that a caller cannot select a path outside the managed backup directory.
4. Test every export with secret canaries to prove PINs, passwords, MFA seeds/recovery codes, tokens, and DVR credentials are absent.
5. Test XLSX cells containing formula-like prefixes and ensure user-controlled content is serialized as text, never formulas.
6. Reject unknown backup collection names or return a clearly marked partial result; never silently claim a complete backup.
7. Make import commit atomic, or implement a durable staged import state with explicit failure/rollback and retry behavior.
8. Add authorization tests for all actions, including direct URL calls, not only navigation visibility.

No application code, backup archive, schema, or database row was changed. No build or tests were run. **Gate A remains NOT AUTHORIZED.**


## 21. Reporting, wallet, audit, and export test-coverage review — 2026-11-18

Inspected test source files (test names and assertions statically; no tests were executed during this review).

### 21.1 Existing coverage worth preserving

- `ReportsServiceTests` covers month labels and shifts, all/year/6m/12m selection, report totals for deposits/expenses/revenues, net calculation, debtors/creditors, and monthly series.
- `WalletServiceTests` has broad unit coverage for month initialization, deposit lifecycle, rounding remainder allocation, pending/cancelled/confirmed deposit handling, wallet formula components, carry-over, distributions, and aggregate totals.
- `AuditLoggerTests` verifies database-side page filtering, correlation metadata, and rejection of AuditLog updates/deletes.
- `CsvExportSafetyTests` covers formula-like values and negative numeric values.

### 21.2 Material gaps

- Report tests do not demonstrate that period cash-flow totals are intentionally distinct from total wallet balances; add explicit contract tests and naming/label assertions.
- No inspected report tests cover invalid `range` values, future months, gaps in historical month keys, large histories, or the exact CSV content/encoding/escaping and audit event behavior.
- Wallet unit tests validate the current in-memory algorithm, but do not establish parity with a future SQL projection, month-close rules, historical correction/recalculation under realistic multi-month data, or stability of cent remainder allocation when apartment ordering changes.
- The inspected AuditLogger tests do not cover controller role/scope behavior, CSV export scope parity, export maximums, stable equal-timestamp ordering, deep paging, audit insert failure behavior, or atomicity with a business mutation.
- CSV safety helper tests are not end-to-end tests of every CSV endpoint. No inspected tests cover XLSX formula-like strings, JSON secret canaries, ZIP backup confidentiality, decrypted backup response cache headers, or direct URL authorization for each backup/export action.
- No BackupController test file was found by the repository search. This does not prove there are no indirect tests, but the critical export/import controller workflows lack an obvious focused test suite.

### 21.3 Required regression suite before read-model or Log Center implementation

1. **Report metric contracts:** selected range, missing months, zero-activity months, cash-flow vs wallet balances, average denominator, and stable output labels.
2. **Wallet golden cases:** 3–12 consecutive months, late deposit confirmation, cancelled/pending deposits, prior-month adjustment, closed/reopened apartments, apartment addition/removal, month gaps, and exact rounding reconciliation.
3. **SQL read-model parity:** compare every proposed SQL result against the current WalletService oracle on deterministic fixtures before replacing any existing path.
4. **Audit query/controller:** role matrix, cross-building and cross-apartment negative tests, filters, equal timestamps, page boundaries, export limits, and audit failure/outbox behavior.
5. **Export security:** direct action authorization, secret-canary scans for JSON/XLSX/ZIP, CSV and XLSX formula injection, cache headers, filename/path traversal, and export audit records.
6. **Import failure injection:** fail after building persistence and at each PIN assignment boundary; prove atomic rollback or durable retry/compensation.

No tests/build were executed. This section reports source coverage only and does not claim runtime pass/fail. **Gate A remains NOT AUTHORIZED.**


## 22. Invoice, QR, and document delivery review — 2026-11-18

A repository-wide search for file-return actions found additional PDF/PNG delivery paths beyond reports and backup: invoice PDF, building QR-code PDF, apartment QR PNG, and the generic QR-image endpoint.

### 22.1 Invoice PDF

- `InvoiceController` allows SuperAdmin, Admin, and Resident.
- Resident target apartment is forced from the `apartmentId` claim; an Admin with a building claim can request an apartment only if it exists in the loaded building; a SuperAdmin without a building claim can resolve the building from a supplied apartment id.
- The controller records an `invoice_download` audit event with building/apartment, actor, role, invoice number, and correlation metadata through `IAuditLogger`.
- The audit write is still best-effort and separate from the read/download, as covered in section 19.4.
- The requested month is accepted as a string and defaults only when blank. Add strict `yyyy-MM` validation and explicit handling of missing/future periods so malformed or unexpected values cannot silently select/create unintended in-memory month state.
- Add `Cache-Control: private, no-store` for invoices because they contain apartment financial/personal information.

### 22.2 QR image and QR PDF

- `QrController` is Admin-only with the resident-management permission and uses the current building claim to load apartment QR data. It records QR access-link generation in the aggregate audit log.
- `QrImageController.Png` is Admin/SuperAdmin-only, validates a maximum input length and HTTP(S) prefix, and correctly sets `Cache-Control: private, no-store` because the QR URL may contain a token. Its endpoint accepts a URL supplied by the caller rather than resolving a server-side QR resource; maintain strict host/URL policy if this endpoint is ever exposed beyond trusted admins.
- `BuildingsController.QrCodesPdf` is SuperAdmin-only and generates a PDF containing QR codes for a building. The inspected action does not set private/no-store cache headers or write a dedicated download audit event. QR codes can encode access/login links, so treat the PDF as sensitive, apply no-store headers, and audit generation/download with building scope.
- The apartment QR PNG action in `QrController` is building-scoped and protected by the class permission. Its download should receive the same cache and audit review where the QR payload can grant access.

### 22.3 File-delivery inventory now known from source search

The inspected file-return endpoints include:
- Reports CSV and Audit Log CSV.
- Backup ZIP creation and decrypted scheduled-backup ZIP download.
- Building JSON, all-building summary JSON, and building XLSX.
- Invoice PDF.
- Building QR-code PDF, apartment QR PNG, and generic QR PNG.
- QR image responses are the only inspected paths explicitly setting `private, no-store`; invoice, QR PDF, and some sensitive exports need consistent cache policy review.

This search is source-code search, not proof that no additional custom response/download mechanism exists. Search for custom middleware, JavaScript-generated downloads, external storage links, and third-party integration endpoints before closing the export inventory.

No application source, schema, or data was changed. No build/tests were run. **Gate A remains NOT AUTHORIZED.**



## 23. Resident lifecycle, voting privacy, maintenance, notifications, presence, and camera access — 2026-11-18

Static source review of `AdminResidentsController`, `ResidentsService`, `PollsController`, `ResidentPollsController`, `PollsService`, `MaintenanceController`, `ResidentMaintenanceController`, `MaintenanceService`, `NotificationsController`, `NotificationsService`, `PresenceController`, `PresenceService`, `CamerasController`, and the related EF mappings. No runtime behavior was exercised.

### 23.1 Resident identity and lifecycle

- Resident CRUD is building-scoped in the controller: Admin uses the `buildingId` claim and SuperAdmin may select a building. Detail/edit/disable/enable/delete first load the resident and compare its `BuildingId` with the caller's scope.
- `ResidentsService.GetByIdAsync` loads by resident ID alone; scope is therefore an application/controller responsibility and must remain centralized when new service callers are added.
- `ResidentsService.DeleteAsync` physically deletes the resident row and does not use its `deletedBy` argument. The service injects `IAuditLogger`, but the inspected resident CRUD/PIN methods do not call it. The controller also does not write durable audit events for create/edit/PIN/disable/enable/delete. This is a material lifecycle/audit gap: prefer retained Person/Account records with explicit relationship end dates, account disable/revoke, and append-only audit history rather than deleting the person record.
- `SetDisabledAsync` changes `IsDisabled`, while `CreateAsync` sets `IsActive`; these are separate state fields. Define one authoritative account-state model and explicit transition rules so disabled/inactive states cannot drift or be interpreted differently by login, authorization, and admin screens.
- PIN reset hashes the supplied PIN with BCrypt, but `SetApartmentPinAsync` applies the same hash to every resident of an apartment. This preserves the legacy shared-apartment-secret behavior rather than establishing an individual credential per person. The target design must not silently migrate this into one shared account credential; define credential ownership and reset scope per Person/Account.
- The SuperAdmin-only `RunMigration` action reports a successful migration result, but the inspected `ResidentsService.MigrateFromApartmentsAsync` implementation currently returns a new empty `MigrationResult` without performing migration. The UI can therefore display a completed/zero-entry result while no records were migrated. Treat this as a nonfunctional legacy migration entry point; remove/disable it or replace it with a separately designed, dry-run-first, evidence-backed migration workflow before relying on it.
- Resident creation accepts `buildingId` and `apartmentId`; the service inserts both values and relies on database relationship constraints for consistency. Keep explicit service-level validation that the apartment belongs to the selected building, with a clear error, rather than making controller correctness depend only on a composite FK.

### 23.2 Voting integrity and secrecy

- The current voting model is explicitly one vote per apartment: the in-memory `Poll.Votes` dictionary maps apartment ID to option ID; the SQL `PollVotes` key is (BuildingId, PollId, ApartmentId), and each row stores OptionId. This is an identifiable ballot, not a secret ballot.
- More critically, `ResidentPollsController.Vote` writes an aggregate audit entry containing the poll question and selected option text, with the apartment number as the audit target. Even if the vote table were later anonymized, this audit trail currently discloses how a specific apartment voted.
- The target secret-ballot design must separate eligibility/one-vote enforcement from ballot content: issue an opaque single-use eligibility token or equivalent, store a ballot without apartment/person/account identifiers, and ensure audit/log/notification/export paths never contain the selected option or a linkable ballot identifier. Keep any eligibility-consumption record separate from the encrypted/anonymized ballot store and define the threat model for administrators and database operators.
- Admin poll deletion removes the poll from the aggregate, and the SQL mapper synchronizes child options/votes. Do not use ordinary delete for polls with votes if historical results or legal/accountability retention is required; close/archive and retain the poll definition and aggregate results under an explicit retention policy.
- Resident poll listing filters only `Closed == false`, whereas vote acceptance additionally checks deadline expiry. As a result, expired polls may remain visible as apparently open even though the vote action rejects them. Align display state, deadline validation, and server-side vote eligibility; parse deadlines strictly with a defined timezone/UTC contract.

### 23.3 Maintenance and financial linkage

- Resident maintenance view currently returns every non-cancelled maintenance record for the resident's entire building, without apartment-level filtering. This may be intended as a shared-building notice board, but it is a product/data-classification decision: distinguish building-wide work from apartment-specific maintenance and filter accordingly if records can contain private unit/vendor/complaint details.
- `AddToExpenses` creates a monthly expense, marks the maintenance record `AddedToExpense`, and recalculates distribution. The flag prevents a second conversion through this method, but the created expense is not visibly linked by a durable MaintenanceRecordId in the inspected domain workflow.
- The maintenance record can still be edited or deleted after conversion. Editing its cost/vendor/title does not update or reverse the already-created expense; deleting the maintenance record does not reverse the expense. This can leave the operational record and financial ledger inconsistent. Introduce an immutable conversion reference and a controlled correction/reversal workflow; never silently rewrite a posted financial expense.
- Date handling uses permissive `DateTime.TryParse` with `AssumeUniversal`; the controller's Add/Update paths also accept a caller-provided month for expense conversion. Require strict date/month formats, UTC/local semantics, and policy checks for future/backdated posting periods.
- Audit events for maintenance add/update/delete/conversion are pushed into the building aggregate before `SaveFullAsync`; this is not the same as a separately durable transactional audit/outbox. Preserve the action details and actor but move critical audit/ledger linkage into a transaction/outbox boundary.

### 23.4 Notifications and recipient isolation

- Notifications are stored as a building-level collection and recipient-scoped by `RecipientType` plus apartment `RecipientId`. The controller derives resident recipient identity from the apartment claim, and the service's resident read/mark-read methods require the same apartment ID; this is a useful isolation invariant to preserve.
- The service caps the entire building's notification list at 200, not 200 per recipient or a time-based retention window. A high-volume event affecting many apartments can evict older notifications for unrelated recipients. Define per-recipient retention and durable unread guarantees instead of a single shared in-memory-style cap.
- Notifications currently identify residents by apartment rather than person/account. Multiple residents in one apartment share the same notification inbox and read state; moving occupants and ownership changes also make historical recipient meaning ambiguous. The new model should define whether each notification belongs to a Person, Account, Apartment, or building audience, and preserve the recipient snapshot/history at send time.
- Mark-read operations persist by saving the full building aggregate. For a high-volume system, use targeted, recipient-scoped SQL updates with concurrency protection and bounded paging rather than loading and saving the whole aggregate.

### 23.5 Presence, privacy, and camera access

- `PresenceService.BuildDocId` uses `{buildingId}_{apartmentId}` whenever both claims exist, instead of the authenticated user ID. Consequently, multiple people/accounts in one apartment overwrite the same presence row; online count is effectively apartment/session presence, not unique people or accounts. The UI and data contract must say which concept it represents. If user-level presence is required, key by AccountId and derive apartment/building scope from current authorized relationships.
- Heartbeat trusts the building/apartment claims and does not independently validate current account status or relationship validity in the service. Ensure session revalidation and relationship revocation invalidate or reject stale heartbeat claims.
- Heartbeat is the inspected controller's only `IgnoreAntiforgeryToken` exception. It only updates last-seen presence, so the impact is limited, but cookie-authenticated cross-site requests can still make a user appear online. Prefer antiforgery protection unless a tested alternative (e.g. same-site authenticated beacon) is required.
- Presence cleanup deletes records older than 30 days. Define the privacy/retention policy and avoid turning presence into a long-term activity history; expose only the minimum fields and authorized building-level aggregates.
- Camera live view and HLS URL endpoints enforce building scope for Admin/Resident and allow SuperAdmin cross-building access. The Admin permission attribute applies to admin principals; resident access is separately permitted by the controller's role/building logic. This should be an explicit policy decision: residents of one apartment currently appear eligible to view all active cameras in their building, not only cameras assigned to their unit.
- `GetHlsUrl` returns the configured HLS base URL plus a camera-specific stream path. Apply private/no-store response headers and ensure the media server independently enforces authorization; a hidden/unpredictable stream path is not sufficient access control. Test direct HLS playlist/segment access after logout, account disable, building reassignment, and permission revocation.

### 23.6 Required acceptance tests

1. Resident cross-building IDOR tests for details/edit/PIN/disable/enable/delete, plus service-level apartment/building consistency tests.
2. Resident lifecycle audit tests for every state change and PIN reset; prove physical deletion is blocked or explicitly retained under an approved erasure policy.
3. A test proving the legacy resident migration endpoint cannot report success while doing no work; replace it with deterministic dry-run and execution evidence before enabling it.
4. Secret-ballot tests that inspect SQL rows, aggregate audit, application logs, exports, and backups to prove no selected option is linkable to an apartment/person/account.
5. Poll deadline tests for malformed, timezone-boundary, expired, future, closed, and reopened polls; listing state must match vote eligibility.
6. Maintenance conversion tests proving posted expense linkage, idempotency, cost edits/deletes after posting, correction/reversal, and ledger reconciliation.
7. Notification isolation tests for same-apartment multiple residents, cross-apartment mark-read attempts, retention/eviction, and concurrent updates.
8. Presence tests for two accounts in one apartment, account disable/revocation, stale claims, CSRF behavior, retention, and correct online-count semantics.
9. Camera tests for resident policy, admin permission, cross-building access, direct HLS playlist/segment authorization, cache headers, and immediate revocation.

No application code, schema, data, or configuration was changed. No build or tests were run; this section is a static source review only. **Gate A remains NOT AUTHORIZED.**


## 24. Quick QR lifecycle, PIN activation, and DVR/media-server consistency — 2026-11-18

Static review of `AdminAptsController`, `BuildingsController.GenerateQuickQr`, `AccountController` quick-login/set-PIN routes, `QrSecurityService`, `DvrsController`, `DvrService`, `DvrNetworkPolicy`, and `MediaMtxService`. No tokens were generated/consumed and no external media server was contacted.

### 24.1 Quick-login QR controls

- The Quick QR token format uses AES-GCM authenticated encryption; validation checks token format, authenticated decryption, expiry, usage count, and optional device fingerprint. Consumption uses a conditional SQL update and handles the first-use insert race. This is a strong foundation worth preserving.
- The QR confirmation GET intentionally does not consume the token, protecting against link-preview scanners; the explicit confirmation POST consumes it atomically before issuing the resident cookie. Preserve this separation.
- There are two QR generation entry points with inconsistent validation. `BuildingsController.GenerateQuickQr` bounds validity to 1–1440 minutes and max uses to 1–1000. `AdminAptsController.GenerateQuickQr` passes `validityMinutes` and `maxUses` directly into the token without equivalent bounds. For a non-one-time token, a non-positive max-use value becomes effectively unlimited in `TryConsumeQuickTokenAsync`; a non-positive validity can produce an already-expired token, while an excessively large value can create a long-lived credential. Centralize validation in one service and apply it to every controller.
- `AdminAptsController.GenerateWelcome` substitutes 24 hours for non-positive values but has no upper bound for `validityHours`. Apply a documented maximum and validate all token parameters server-side, not only in the view.
- The Quick QR payload contains building/apartment identifiers and a purpose. `ResolveSetPinAsync` checks the purpose is exactly `setpin`; the normal quick-login resolver must similarly reject non-login purposes. Keep purpose-specific validation in a shared token service, not duplicated controller code.

### 24.2 One-time set-PIN workflow

- The anonymous SetPin POST validates a four-digit PIN and confirmation, then consumes the one-time token before calling `SetApartmentPinAsync`. If the database update fails after consumption, the token is permanently spent and the resident cannot retry with the same link. Make token consumption and credential update a single transaction/atomic state transition, or use a durable staged state that permits safe retry without permitting replay.
- The current `SetApartmentPinAsync` changes the PIN hash for every resident associated with the apartment. This means a one-time link is effectively resetting a shared apartment credential for all resident rows, not activating one named Person/Account. The target design must bind the activation token to an explicit intended account/person and enforce one-account credential ownership.
- The token is placed in a query-string URL. Apply strict no-store/referrer policy on confirmation and SetPin pages, redact token query strings from reverse-proxy/application telemetry, and ensure no third-party analytics or external resources receive the token through the Referer header.
- Add explicit audit/outbox records for token issuance, successful consumption, failed credential activation, and administrative revocation, without storing the bearer token itself.

### 24.3 DVR credentials and media-server lifecycle

- DVR credentials are encrypted at rest through `IEncryptionService`; the network policy restricts DVR source endpoints to private IP literals and validates port bounds. RTSP paths are constrained to relative paths with length/character/traversal checks. Preserve these protections and add deployment key-rotation/recovery tests.
- `DvrService.GetByBuildingAsync` and `GetByIdAsync` return full `DvrEntity` objects, including the encrypted password field. Even though the inspected views do not need the ciphertext to display connection details, do not pass secret-bearing persistence entities to views or API responses. Project to a dedicated safe DTO that omits `PasswordEncrypted`.
- `GetDecryptedPasswordAsync` is a general service method returning the plaintext DVR password. Keep it narrowly scoped to the media-server integration path, avoid logging it or embedding it in exception messages, and audit privileged credential rotation/retrieval without recording the value.
- Adding a camera creates its SQL row and then registers the MediaMTX path; if registration fails, the controller deletes the SQL row. This is a useful compensation pattern, but failure-injection tests should verify both sides and retry behavior.
- Editing a DVR updates SQL connection fields and encrypted password, but the inspected update workflow does not re-register or update existing MediaMTX paths. Existing streams may therefore continue using stale RTSP connection settings after a credential/IP/port change. Make the update a coordinated operation: validate, stage media-server update, persist desired state, and reconcile failures with retry/outbox.
- Deleting a DVR removes MediaMTX paths one at a time and only then deletes the SQL DVR row. If a later path deletion fails, earlier paths may already be gone while SQL still retains all camera records; retries must be idempotent and reconcile actual media-server state. The service then physically deletes the DVR row. Prefer an explicit Decommissioning/Disabled state, a durable cleanup job, and retained audit history before final purge.
- Camera deletion similarly removes the MediaMTX path before deleting the SQL row. Add a durable operation state so a database failure after external deletion can be reconciled instead of leaving a camera record that appears active but has no stream.
- The inspected DVR create/update/delete and camera create/delete paths do not visibly write dedicated durable audit events. Add actor/building/device/camera identifiers, operation, outcome, and correlation ID to the audit/outbox; never include plaintext or encrypted credentials.

### 24.4 Required acceptance tests

1. Shared QR-option validator tests for negative, zero, boundary, and excessive validity/max-use values across both generation endpoints.
2. Purpose-confusion tests proving a set-PIN token cannot log in and a login token cannot set a PIN.
3. Concurrency and failure-injection tests for one-time token consumption, including DB failure after consume and safe retry semantics.
4. Set-PIN tests proving one account/person is affected, not every resident in an apartment; secret query-string redaction and no-store/referrer headers.
5. DTO/view tests proving DVR encrypted password fields never reach HTML/API output; secret-canary tests for logs and exceptions.
6. DVR update tests proving MediaMTX uses the new connection details after credential/IP/port changes.
7. Partial-failure tests for multi-camera DVR deletion, camera deletion, MediaMTX outages, SQL failures, retries, and reconciliation.
8. Authorization and audit tests for every DVR/camera action, including cross-building IDs and credential rotation.

No application code, schema, data, configuration, QR token, or media-server state was changed. No build/tests were run. **Gate A remains NOT AUTHORIZED.**


## 25. Financial command validation, deposit state transitions, and ledger correction — 2026-11-18

Static source review of `WalletController`, `WalletService`, `ExpensesController`, `ExpensesService`, `RevenuesController`, `RevenuesService`, and the permission catalog. No financial command was executed.

### 25.1 Deposit lifecycle

- Deposit creation enforces amount > 0 and apartment existence in `WalletService.CreateDeposit`; confirmation/cancellation accept only pending deposits and stamp actor/time. Pending edits record old/new amount/note, reason, actor, and timestamp, and reject confirmed/cancelled deposits. These are important state rules to retain.
- `WalletController.ConfirmDeposit` and `CancelDeposit` call `GetOrCreateMonth(building, month)` through the service before locating the deposit. The caller-supplied month therefore influences the aggregate lookup and carry-over recomputation. Define a strict `yyyy-MM` parser and resolve the deposit's authoritative month from its persisted row rather than trusting a posted month value.
- Confirmation/cancellation, audit aggregate event, notification, and `SaveFullAsync` are currently grouped in one aggregate save call, but there is no explicit idempotency key or concurrency token on the in-memory transition path shown here. Two simultaneous admin requests can race on a stale aggregate. Add rowversion/conditional state transition and ensure only one command can move Pending to Confirmed/Cancelled.
- `UpdateDeposit` correctly restricts edits to pending deposits and writes an edit-history entry. The controller's pre-check and service's status check should be consolidated in one command service with a transaction and immutable before/after event.
- Receipt fields include URL/path/name/size/type/success. Treat receipt path and URL as sensitive untrusted input: validate storage ownership, content type/size, download authorization, malware scanning, and no-store behavior. Do not let user-supplied receipt paths become filesystem paths.

### 25.2 Manual wallet adjustments

- `AddAdjustment` accepts any non-zero decimal amount and delegates to `AddWalletAdjustment`; the inspected service does not itself validate that the apartment exists, that the month key is valid, or that the amount/reason meets an approved limit/policy. Enforce these invariants in the domain command, not only the form.
- The adjustment audit detail contains amount and reason but the controller call does not visibly pass the apartment ID as a structured target. Persist building, apartment, month, signed amount, reason, actor, approval context, and correlation ID as structured immutable data.
- Define whether large or exceptional adjustments require dual approval, whether an adjustment can be reversed only by a compensating entry, and whether a posted adjustment can ever be edited/deleted. Preserve every adjustment as a ledger event.

### 25.3 Expense and revenue corrections

- Expense/revenue Add controllers check amount > 0 and non-empty category ID, but the inspected services simply create the item and do not independently validate the category belongs to the building, is active/appropriate-kind, or that the supplied date and month key follow a strict format.
- Both delete controllers call the service delete method but ignore its Boolean result, then recalculate distributions, write an audit event, and save regardless. A nonexistent ID can therefore produce a misleading "deleted" audit event. Audit only a successful state transition and include the deleted row's former amount, category, note/reference, month, and actor.
- Expense/revenue deletion physically removes the item from the month aggregate. For a financial ledger, prefer reversal/void entries with reason and actor, preserving the original posting and recomputing distributions through a traceable correction event.
- The selected month key and transaction date are independent fields in the current model. Keep their semantics explicit: posting period is not necessarily the same as transaction date, but both must be validated and retained.

### 25.4 Required acceptance tests

1. Strict month/date validation and cross-month deposit-ID tampering tests.
2. Concurrent confirmation/cancellation tests proving a deposit can transition out of Pending exactly once.
3. Deposit edit history tests including audit/outbox failure and receipt metadata/path validation.
4. Adjustment tests for nonexistent/cross-building apartment IDs, invalid month keys, decimal precision/limits, approvals, and compensating reversal.
5. Expense/revenue category ownership/kind/active validation and date/month validation tests at the service boundary.
6. Delete tests proving missing IDs do not create success audit events and successful corrections preserve the original financial row.
7. Ledger reconciliation tests proving every correction can be traced from original posting through reversal/replacement and wallet recalculation.

No application code, schema, data, or configuration was changed. No build/tests were run. **Gate A remains NOT AUTHORIZED.**


## 26. Global/building settings, integration secrets, and impersonation authorization — 2026-11-18

Static review of `SettingsController`, `SettingsService`, `GlobalSettings` models/views, `ImpersonationController`, `ImpersonationService`, and `AdminPermissions`. No settings were changed and no impersonation session was initiated.

### 26.1 Integration secrets exposed through settings read model

- `SettingsService.GetGlobalAsync` copies the stored WhatsApp API token and Cloudinary API key directly into the `GlobalSettings` view model. `Views/Settings/Global.cshtml` renders both values into ordinary HTML input `value` attributes. Thus a user authorized to view global settings receives the stored secret in page HTML/browser DOM; this expands exposure through browser extensions, screenshots, client-side inspection, and accidental page capture.
- `SaveGlobalAsync` assigns the posted values directly back to storage, with null converted to an empty string. A masked/blank UI cannot safely mean "keep existing value" without an explicit update contract; conversely, displaying the real token makes the secret unnecessarily retrievable. Replace with write-only secret inputs: GET returns only configured/not-configured metadata, POST preserves existing value when the secret field is omitted, and explicit rotate/clear operations are separate audited commands.
- The inspected entity/model path stores WhatsApp token and Cloudinary API key as ordinary settings strings; no encryption/decryption wrapper is visible in this service. Treat them as plaintext-at-rest until a separate storage/key-management path proves otherwise. Encrypt integration credentials using a managed key, support key rotation, and keep secrets out of exports, audit metadata, logs, and backups where feasible.
- The global settings GET creates and saves a default row when none exists. A read endpoint therefore has a write side effect and can race under concurrent first access. Use a controlled initialization/migration or an idempotent insert with unique-key handling.
- Global security limits are accepted from the posted model; HTML min/max attributes are not server-side validation. Validate session timeout and PIN-attempt bounds in the service/domain layer and define whether these settings actually govern the corresponding authentication components.
- Building settings save accepts `InvoiceDayOfMonth`, `VotingQuorumPercent`, `ExpenseDistribution`, and currency strings without visible domain-range/allowlist validation in `SettingsService`. Validate day-of-month, quorum range, supported distribution modes, and currency catalogue before persistence.
- Resident own-settings are stored on the Apartment row, not an individual resident/account. Multiple residents in one apartment therefore share language, notification preferences, and preferred payment method. The target identity model should move personal preferences to Account/Person and reserve Apartment-level preferences for truly shared unit settings.
- `UpdateResidentProfileAsync` updates every resident row in the apartment with the same name/phone and PIN hash. This confirms the current profile and credential actions are apartment-wide, not individual-person operations. Preserve legacy mapping but do not carry shared PIN/profile mutation semantics into the new Person/Account design.
- `UpdateAdminProfileAsync` reads a user through `UsersService` and submits a PIN update through `SetAsync`; ensure the credential write path hashes before persistence and never stores the posted PIN in a generic profile dictionary or audit/event payload. The service source indicates it passes the raw new PIN to `UsersService.SetAsync`; confirm the downstream implementation always hashes it and add a persistence canary test.

### 26.2 Impersonation permission and recovery-stack integrity

- The permission catalog defines `SecretAccess = "secret.access"` and maps it to impersonation actions, but the inspected `EnterResidentWallet`, `EnterResidentHome`, and `EnterBuildingAdmin` actions enforce roles and antiforgery without an explicit `HasPermission(SecretAccess)`/equivalent check. The role check alone is not the declared least-privilege permission contract. Add a dedicated authorization policy that validates actor role, permission, target scope, and recent MFA/step-up state.
- `ImpersonationService.PushBackup` refuses to push when the stack reaches depth 5, but returns no success/failure result. The controller proceeds to issue the impersonated authentication cookie regardless. At maximum nesting depth, the actor can enter another identity without a matching recovery snapshot; Exit may then fail to restore the correct identity. Make push return an explicit result and fail closed before issuing any impersonated cookie.
- The backup stack is stored in a two-hour protected cookie and copied into request Items. Data Protection protects confidentiality/integrity, and the cookie is HttpOnly/SameSite=Lax; however, its `Secure` flag is set from `Request.IsHttps`, meaning it is not Secure on HTTP deployments. Production must enforce HTTPS and set Secure unconditionally outside local development. Consider cookie size limits as the stack stores permissions and actor metadata.
- The cookie stack is client-carried and has no server-side revocation/session record. A still-valid protected backup cookie can restore the actor snapshot until expiry even if the actor is disabled or permissions are revoked during the two-hour window. Bind impersonation to a server-side session/revocation version and revalidate the initiating account and target relationship on every transition.
- `LogEnterAsync` records `userRole: "admin"` for resident impersonation regardless of whether the initiating actor is admin or SuperAdmin; `LogExitAsync` likewise hardcodes admin. The SuperAdmin-to-building-admin path has a separate logger, but the resident entry/exit audit role can be inaccurate. Record immutable actor account ID and actual actor role separately from the effective impersonated role, target, reason/ticket, and correlation ID.
- Audit calls happen after the authentication cookie is issued. Since the audit logger is best-effort/separate-context, a failed audit write can leave a live privileged impersonation session without a durable audit trail. Require durable audit/outbox acceptance before the transition is considered successful; if that cannot be guaranteed, fail closed.
- The backup snapshot includes permissions and MFA-verification flags. Restoring those claims from a two-hour-old snapshot can restore stale permissions/MFA state. Do not treat serialized claims as authoritative; re-resolve actor role, permissions, account status, and MFA policy at exit/restore time.
- The code deliberately supports nested impersonation. Define whether nested impersonation is necessary; if retained, use explicit parent-child session IDs, bounded depth with fail-closed behavior, and actor/target chain audit rather than relying solely on a client-side stack.

### 26.3 Required acceptance tests

1. HTML/API secret-canary tests proving WhatsApp/Cloudinary secrets never appear in GET responses, DOM, logs, audit metadata, or exports.
2. Settings secret-preservation tests: omitted field preserves current secret; explicit rotate/clear changes it; encrypted-at-rest and key-rotation tests.
3. Server-side validation tests for session timeout, PIN attempt limits, invoice day, voting quorum, distribution mode, and currency.
4. Two residents in one apartment must have independent personal preferences and credentials after migration; apartment-level shared settings remain separately modeled.
5. Verify downstream `UsersService.SetAsync` hashes PIN before persistence; raw-PIN canary must never appear in database/log/audit.
6. Impersonation tests proving every entry action requires SecretAccess plus current MFA/step-up and exact target scope.
7. Stack-depth boundary test proving a failed push cannot issue an impersonated cookie; test malformed/expired/tampered cookie recovery.
8. Disable/revoke actor and change permissions during impersonation; all subsequent operations and Exit/restore must revalidate or safely terminate.
9. Audit failure injection: no impersonation transition may succeed without durable actor/role/target/reason/time/correlation audit.
10. HTTPS deployment tests require Secure cookies and ensure restored permissions/MFA are reloaded from authoritative state.

No application code, schema, data, configuration, credentials, or sessions were changed. No build/tests were run. **Gate A remains NOT AUTHORIZED.**


### 26.4 Follow-up verification

- Follow-up inspection of `UsersService.SetAsync` confirms the `"pin"` field is passed through `SecretVerifier.HashIfPlain` before `UserSqlStore.UpsertAsync`. Therefore the raw PIN in `UpdateAdminProfileAsync` is not, by itself, evidence of plaintext persistence on this path; retain a persistence canary test to protect this behavior.
- The `SettingsController.Own` POST is available to any authenticated role and has no explicit `HasPermission(ManageSettingsOwn)` attribute. This may be an intentional self-service policy, but it differs from the permission catalog's declared mapping. Decide explicitly whether self-service is role-intrinsic or permission-controlled, then align the catalog and authorization tests.
- The `SettingsController.Own` POST saves preferences and profile data in separate try/catch blocks and reports partial success. This is user-friendly for independent settings, but it is not an atomic profile/credential change. Define which operations may partially succeed; credential changes should have a single authoritative transaction and explicit outcome.


## 27. Startup PIN migration and SQL mirror consistency — 2026-11-18

Static review of `PinMigrationService`, `BuildingSqlStore`, `UserSqlStore`, and `SqlMirrorHealth`. The startup migration was inspected as source only; it was not invoked.

### 27.1 Startup migration is a live data mutation

- `MigrateLegacySecretsAsync` performs real writes: hashes plaintext admin/SuperAdmin PINs, hashes resident PIN values, clears all non-empty `Buildings.AdminPin` values, creates a resident row for apartments with a legacy PIN but no resident rows, copies the apartment PIN hash to residents with blank hashes, and clears `Apartments.Pin`.
- This method has no dry-run mode, no explicit operator approval, no per-row decision report, and no durable migration ledger mapping old source keys to created/updated identities. Generated resident Id/Uid values are random, so they cannot by themselves establish continuity with a known person/account.
- The operation is split across multiple SaveChanges/ExecuteUpdate boundaries and has no encompassing transaction. Failure can leave users/residents hashed while building PINs or apartment PINs remain, or clear building PINs before apartment migration completes. A second application instance can run the same startup work concurrently; no distributed migration lock or durable one-time migration marker is visible here.
- The method clears legacy PIN columns as part of application startup. This is a destructive credential cutover and must not run automatically on application startup, especially during restore-database inspection. Replace it with a separate migration command: inventory-only dry run, explicit approval, stable source-to-target mapping, transaction/batches with checkpointing, collision handling, per-row outcome, rollback/recovery plan, and post-migration authentication verification.
- `MigrateAllAsync(dryRun)` is a different admin/SuperAdmin-only helper. It does not cover resident/apartment/building PIN data and is not a substitute for a complete dry-run of `MigrateLegacySecretsAsync`. Its result detail includes user email; keep PII in protected operator output and avoid broad logs.
- The BCrypt prefix checks accept only the expected prefixes but do not validate full hash format/cost or verify that the stored value matches a known credential. Treat shape-only counts as unverified, not as proof of successful credential migration.

### 27.2 SQL mirror health and aggregate persistence

- `SqlMirrorHealth` is static in-process state: failure count, last error, and timestamps reset on process restart and are not shared across application instances. It is useful as a local diagnostic signal, not an authoritative system-health or synchronization ledger.
- `RecordFailure` stores `Exception.Message` as `LastError`. If surfaced to an operational page, database/provider exception text may expose schema, server, query, or connection details. Return sanitized error codes/correlation IDs to ordinary UI and keep detailed exceptions in restricted logs with secret redaction.
- `BuildingSqlStore.GetAllAsync` first loads IDs then calls the aggregate loader once per building. The source comment documents 16 queries per building; this is an N+1 round-trip pattern for all-building operations. Use summary projections for lists and bounded paging; reserve aggregate loading for a single building workflow.
- `BuildingSqlStore.SaveFullAsync` delegates whole-aggregate reconciliation to `BuildingSqlMapper.SaveAsync`; warnings are logged, but callers need explicit save outcome and concurrency-conflict semantics. Preserve the existing Buildings.RowVersion optimistic concurrency control and test that a stale aggregate cannot overwrite a newer financial/identity update.
- `UserSqlStore.UpsertAsync` synchronizes permissions as a replacement set, but building-admin links are additive only: it inserts missing links and does not remove links omitted from the new `BuildingIds` collection. This can retain stale building access after an apparent permission replacement. Introduce explicit grant/revoke semantics and a transactionally audited replacement command.
- `UserSqlStore.UpsertAsync` uses an in-process `KeyedLock`; this does not serialize writes across multiple application instances. Database unique constraints/rowversion and retry handling remain necessary for cross-instance correctness.
- `SqlMirrorHealth.RecordSuccess` updates only last-success time; it does not reset failure count/last error. The UI must not present the failure count as current consecutive failures unless the semantics are changed/documented.

### 27.3 Required acceptance tests

1. Startup test proving no migration or destructive PIN clearing runs merely by starting the web application.
2. Dry-run parity tests proving every row that execution would alter is reported with source key, target identity, decision, and reason without exposing PIN values.
3. Failure injection at every save/clear boundary and multi-instance concurrency tests; reruns must be idempotent and recoverable.
4. Mapping tests for apartment PIN to existing resident(s), no resident, multiple residents, conflicting PINs, inactive residents, and source identity continuity.
5. Authentication verification before/after migration, with rollback/recovery evidence and no plaintext secret in logs/results.
6. Multi-instance SQL mirror tests for concurrent user/building writes, optimistic concurrency conflicts, and duplicate relationship prevention.
7. Building-admin link replacement tests proving omitted grants are revoked only when explicit full-replacement semantics are requested.
8. Health dashboard tests proving process-local status is labeled as instance-local and provider error details are sanitized.

No migration, database write, application startup, build, or test was performed. No code/schema/data/configuration was changed. **Gate A remains NOT AUTHORIZED.**


### 27.4 Startup wiring confirmed

- Follow-up inspection of `Program.cs` confirms the web application invokes `PinMigrationService.MigrateLegacySecretsAsync()` during startup inside a try/catch. This is not merely an unused service method: starting the application against a database can execute the credential migration and clear legacy PIN columns. A caught exception does not roll back earlier commits in the method.
- Operational safeguard: do not start the application against the disposable restore database (or any database whose legacy PIN state must remain unchanged) during this review. First take/verify a disposable restore copy and remove/feature-gate the startup mutation in a separately approved code change; then run the explicit migration workflow only against its designated disposable target.


### 28. Runtime orchestration, scheduled backups, application logs, health endpoints, and sync inventory

#### 28.1 Scheduled backup execution and multi-instance safety

- BackupScheduler is registered as a hosted service in Program.cs. Every running application process therefore starts its own scheduler. The loop checks every 30 minutes after an initial 30-second delay.
- No distributed lease, SQL application lock, unique run record, or conditional claim is visible around ShouldRunAsync and RunScheduledBackupAsync. In a multi-instance deployment, two processes can both observe the same due settings and run concurrently.
- Scheduled file names have minute precision (scheduled-yyyy-MM-dd-HHmm.zip) and are written with File.WriteAllBytesAsync, which overwrites an existing same-name file. Concurrent runs in the same minute can overwrite one another while each may add a separate BackupHistory row.
- Scheduling is based on DateTime.UtcNow and the configured hour is interpreted as UTC, but the settings model/UI contract does not visibly make the timezone explicit. Operators may reasonably interpret the hour as local time.
- Monthly scheduling requires now.Day == DayOfMonth. A setting for day 29, 30, or 31 will not run in months that do not contain that day; day 31 therefore skips February, April, June, September, and November rather than running on the last day.
- ShouldRunAsync and the subsequent backup are separate read/execute operations. There is no persisted claim/lease to prevent a second process or overlapping manual/scheduled invocation from passing the same due check.
- The run writes the encrypted file, inserts history, updates last-run settings, rotates files, and writes audit as separate operations. A crash between these steps can leave an orphan file, a history row without a file, or a completed backup whose status/audit was not finalized. Define recoverable states and reconcile files/history at startup or via a maintenance job.
- RunScheduledBackupAsync catches failures internally. This keeps the hosted loop alive, but callers receive no structured result indicating whether the run succeeded; the method should return a typed outcome and keep the failure reason sanitized.
- The scheduler checks cancellation on the recurring delay, but the initial 30-second delay is outside the loop's cancellation handling. Verify graceful shutdown behavior with a host-stop test.

#### 28.2 Backup retention and history consistency

- RotateBackupsAsync clamps retention to 1–30 and deletes older scheduled-*.zip files, which is a positive safety control.
- Rotation does not delete, mark, or reconcile matching BackupHistory rows. The history query later computes FileExists, so retained metadata can accumulate indefinitely with FileExists=false.
- GetHistoryAsync accepts a caller-provided limit without a service-level clamp; the controller must be checked as the only guard and the service should enforce a bounded maximum itself.
- DeleteBackupAsync removes matching history rows even when the file path does not exist, then returns success and logs a deletion event. Make the result distinguish file-deleted, history-only cleanup, not-found, and failure; do not report a successful deletion for an unknown artifact.
- BackupFileProtector.Unprotect deliberately returns non-header files unchanged for legacy compatibility. This means old unencrypted backup files remain accepted. Inventory and migrate legacy files, record encryption version/key ID, and define a retirement date rather than allowing indefinite silent plaintext compatibility.
- Add integrity/recovery checks for file write completion, decryptability, archive validation, database history insertion, and restore round-trip. Encryption alone does not prove that a backup is complete or restorable.

#### 28.3 Application log query cost and retention visibility

- ApplicationLogQueryService.QueryPageAsync enumerates up to 14 files and scans their contents for every query, then retains the newest bounded result window in memory. This is bounded in retained event count, but the I/O and parsing cost still grows with the size of those files and repeats for each request.
- The writer uses daily rolling plus a 10 MB size limit and a 14-file retention count. A busy day can produce multiple rollover files; the query service independently takes only the 14 most recently modified files. Consequently, some files still present under the writer's retention policy may be excluded from the explorer when rollover produces more than 14 files.
- The UI's maximum reachable window is 100 pages × 100 rows, but it still scans the selected files to compute total matches. The per-IP lookup limiter is 120 requests/minute, which may permit expensive repeated full scans. Consider a short-lived parsed-event cache, incremental index, stricter query rate/cost budget, and cancellation/timeout tests.
- The controller parses date-only filters as UTC midnight/end-of-day. The UI must label this clearly; otherwise a user in a local timezone can misinterpret the selected calendar dates.
- The controller converts invalid date strings to null rather than returning a validation error. A malformed from or to value can therefore silently remove that filter and broaden the search. Reject malformed non-empty dates.
- Redaction is a useful defense-in-depth layer for displayed logs (secrets, connection strings, bearer/JWT tokens, email, phone, IP, and filesystem paths). It is regex-based and cannot guarantee removal of arbitrary application-specific secrets or secrets embedded in encoded/structured payloads. Add a structured allowlist at log creation for sensitive events and a secret-canary regression suite.
- Request logging enriches events with host, scheme, remote IP, and user agent. These values are operationally useful but can be identifying; document access, retention, and purpose, and minimize or pseudonymize where feasible. UI redaction does not remove values from the underlying retained log files.
- Log queries read local filesystem paths under each instance's content root. In a multi-instance deployment the explorer may show only the current instance's local files, not a centralized view. Either label the instance explicitly or centralize logs with an instance identifier and access controls.

#### 28.4 Health endpoints, rate limiting, and shared runtime state

- /health/live, /health/ready, and backward-compatible /health are mapped without visible authorization metadata. The response is intentionally minimal (overall status, duration, check names/statuses) and does not expose exception text; nevertheless, readiness reveals whether the database check is healthy. Restrict access at the reverse proxy/network boundary when the deployment does not require public probing.
- The lookup and login fixed-window rate limiters partition by RemoteIpAddress and are in-process. They are not a cross-instance quota. In the default forwarded-header configuration, X-Forwarded-For is not enabled unless known proxies are configured; behind a reverse proxy, multiple clients may therefore share the proxy address and be throttled together. Verify the trusted-proxy configuration in each deployment and never trust arbitrary forwarded headers.
- AddDistributedMemoryCache is registered, but that implementation is process-local memory, not a distributed cache. Session and other cache-backed state will not be shared across instances and can disappear on recycle. This must be treated as a single-instance limitation unless replaced with a real shared provider.
- The application persists Data Protection keys to a configured filesystem path (with a local default). Multi-instance deployments must share a protected, durable key ring; otherwise authentication/session-protected payloads and encrypted cookies can fail across nodes or after ephemeral storage loss. Verify filesystem ACLs, backup, key rotation, and cross-node compatibility.
- CSP is configurable as enforce/report-only/off, but the current policy permits unsafe-inline scripts and allows HTTP origins for media, connections, and frames. This weakens the protection expected from CSP on HTTPS deployments. Inventory actual external dependencies, remove HTTP allowances where possible, use nonces/hashes for inline scripts, and test in report-only before tightening.
- Liveness/readiness are separated correctly: liveness does not run the database check and readiness does. Preserve this distinction and add deployment smoke tests for database outage, recovery, and proxy behavior.

#### 28.5 Synchronization feature inventory mismatch

- The current branch contains ManageSync in the permission catalog and maps it to a SyncController with SyncAdmins, SyncResidents, SyncPasswords, SyncSql, and ReverseSync actions, and DbSyncService/ReverseSyncService names.
- The current Controllers and Services directory inventories do not contain SyncController, DbSyncService, or ReverseSyncService. The only directly related data type found is Data/SyncReport.cs, whose summary still describes Firestore-to-SQL synchronization.
- This is a catalog/documentation/runtime mismatch, not proof that an unlisted external sync process does not exist. Confirm whether synchronization was intentionally retired or moved outside this repository. If retired, remove stale permission mapping and UI claims; if external, document its owner, source of truth, execution mechanism, credentials, idempotency, conflict policy, and monitoring. Do not reintroduce bidirectional sync without an explicit authority-of-record design.

#### 28.6 Required acceptance tests

1. Start two application instances and prove only one scheduler lease can claim a due run; verify no duplicate history rows or same-minute file overwrite.
2. Test daily/weekly/monthly schedules across UTC boundaries, daylight-saving/local-time expectations, restarts, missed windows, and month lengths 28/29/30/31.
3. Inject failures after file creation, history insert, last-run update, rotation, and audit; verify deterministic recovery and accurate status.
4. Verify retention keeps exactly the configured number of scheduled artifacts and reconciles database history without deleting manual backups.
5. Test missing/corrupt/legacy-unencrypted files, wrong encryption key, truncated ciphertext, and a full decrypt/restore round trip.
6. Generate more than 14 rolled log files and prove explorer coverage matches the documented retention/visibility policy.
7. Benchmark log search at maximum retained file size and concurrent lookup traffic; verify cancellation, memory bounds, and query limits.
8. Submit malformed date filters and verify a validation error rather than an unfiltered search.
9. Add secret canaries in message, exception, JSON properties, URLs, and multiline events; verify no secret is displayed and document what remains on disk.
10. Verify health endpoint exposure, reverse-proxy forwarded-header trust, per-IP limiter behavior, and multi-instance limiter semantics.
11. Run authentication/session/Data Protection tests across two instances and after process restart using the production-like key-ring configuration.
12. Verify CSP in report-only mode against all required application workflows, then remove unnecessary inline and HTTP allowances.
13. Confirm whether sync is retired or externally hosted; if retained, produce an authoritative inventory and repeatable end-to-end sync tests.

Static source review only. No application, backup job, synchronization job, database, filesystem artifact, credential, or configuration was changed. No build or tests were run. **Gate A remains NOT AUTHORIZED.**


#### 28.7 Manual backup endpoint outcome and download-cache controls

- BackupController.RunNow awaits RunScheduledBackupAsync and then unconditionally sets the success message. RunScheduledBackupAsync catches its own exceptions and returns normally; it also returns early when scheduling is disabled and forceRun is true but no collections are configured. The UI can therefore report “Backup Created” when no file was created or when the internal run failed. Change the service contract to return a typed result (Created/Skipped/Failed) and map it accurately in the controller.
- DeleteBackupAsync returns true after deleting any matching history rows even if no corresponding file existed. For an arbitrary safe-looking filename, the file path can be null while the method still deletes history by filename and emits a success audit event. Require an existing owned scheduled artifact or explicitly model history-only cleanup as a separate operation; distinguish NotFound from Deleted.
- DownloadBackup decrypts the on-disk protected file and returns the plaintext ZIP. The controller does not visibly apply no-store response headers to this download. Apply Cache-Control: no-store, private and Pragma: no-cache to backup downloads and generated backup ZIP responses; ensure reverse proxies/CDNs do not cache them.
- Backup creation and restore preview/execute are privileged, high-impact operations. Preserve the existing SuperAdmin + ManageBackup authorization and antiforgery protections, and add explicit durable audit outcomes for success, rejection, failure, and download; do not rely solely on generic request logs.

Additional acceptance tests: RunNow with scheduling disabled, empty collection list, injected backup exception, and successful run must produce four distinct accurate outcomes; DeleteBackup for missing, malformed, existing, and history-only records must not falsely report deletion; backup ZIP responses must be verified no-store at the application and proxy layers.
