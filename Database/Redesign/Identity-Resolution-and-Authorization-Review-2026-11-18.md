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
