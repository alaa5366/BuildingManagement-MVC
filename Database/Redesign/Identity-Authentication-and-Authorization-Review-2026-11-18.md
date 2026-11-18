# Identity, Authentication, and Authorization Review — 2026-11-18

**Branch:** `development/database-redesign-enterprise`  
**Review type:** static source review of account login, credential verification, context selection, MFA, role/permission claims, user/resident stores, and resident administration.  
**Authorization context:** full-project redesign is owner-authorized. This review records the first implementation workstream under that authorization.  
**Database impact:** none. No credentials were tested and no account data was changed.

## 1. Executive summary

The existing application has useful foundations: antiforgery validation on credential POSTs, login rate limiting, BCrypt verification, account disable checks in several paths, server-side context selection stored in session, MFA replay protection, and building-scope checks in resident administration.

However, the current identity model is not yet compatible with the approved Person / UserAccount / effective-dated access architecture. The highest-priority findings are:

1. Admin/superadmin accounts are stored in `Users`, while residents are stored separately in `Residents`; there is no canonical Person identity or single account lifecycle.
2. Unified login returns one context per admin-building assignment and one per resident record, but the resident context identifier is derived from building/apartment rather than the resident account ID. This can collapse multiple resident accounts in one apartment into one selectable context.
3. Resident sign-in paths are inconsistent: direct resident login checks disabled state but not `IsActive`; unified login checks both.
4. Apartment PIN operations update every resident in an apartment to the same verifier. This is shared apartment authentication, not individual credentials.
5. `AdminPermissionAttribute` automatically allows SuperAdmin and has an emergency configuration bypass. Login cookie construction also injects all SuperAdmin permissions. This conflicts with approved DEC-39: no implicit SuperAdmin bypass.
6. Admin/superadmin MFA is only challenged when MFA is already enabled. The approved policy requires mandatory enrollment and enforcement with staged bootstrap/recovery.
7. Role, building, apartment, and permission claims are copied into a cookie at sign-in. There is no central evidence in the reviewed path that every authorization request re-evaluates account status, grants, or membership changes. Revocation therefore depends on cookie/session lifetime and other refresh mechanisms.
8. Startup PIN migration mutates credentials and legacy apartment/building PIN data. This must be removed from startup and replaced by the approved explicit migration workflow.
9. Resident create/edit/disable/delete operations act directly on the Resident row and do not model Person, occupancy/ownership history, account separation, or effective-dated apartment grants.
10. Legacy `Users` and `Residents` records contain overlapping identity/contact/credential concepts, while `UserSqlStore` mirrors role/permission/building collections with replace-style synchronization. This is not a safe long-term source of truth for effective-dated grants.

## 2. Current identity storage and lifecycle

Evidence:
- `Data/Entities.cs`: `UserEntity`, `UserPermissionEntity`, `BuildingAdminEntity`, `ResidentEntity`
- `Data/UserSqlStore.cs`
- `Services/UsersService.cs`
- `Services/ResidentsService.cs`

Current separation:
- `UserEntity` stores admin/superadmin credentials, role, disable state, MFA data, and profile/contact data.
- `ResidentEntity` stores resident identity/contact data, apartment link, PIN/password verifiers, active/disabled state, owner/primary flags, and login timestamps.
- Admin building membership is stored in `BuildingAdmins`.
- Admin permissions are stored in `UserPermissions`.
- Resident role is not represented as an account row in `Users`; resident authentication is performed from `Residents`.

Target:
- Introduce Person as the human identity record.
- Introduce UserAccount as the separately managed interactive account and credentials.
- Allow a Person without an account and one account at most per Person.
- Represent roles as scoped, effective-dated assignments rather than one mutable role string.
- Represent System/Building/Apartment/Personal permissions and overrides as explicit grants with provenance, expiry, revocation, and actor.
- Represent ownership, occupancy, tenancy, and temporary access independently from account identity.
- Keep legacy rows immutable as migration evidence until matching and reconciliation are approved.

## 3. Unified login and context selection

Evidence:
- `Controllers/AccountController.cs`
- `Services/SqlAuthService.cs`

Unified login:
- Uses email for SuperAdmin and normalized phone for Admin/Resident.
- Verifies a single supplied credential across matching account candidates.
- Stores matching contexts in server-side session and returns a context-selection token.
- Admin context is generated per building assignment.
- Resident context ID is `resident_{BuildingId}_{ApartmentId}`, not `ResidentEntity.Uid`.

Consequences:
- Two or more resident records for the same apartment can produce colliding context IDs; selection can resolve to the first matching context rather than a distinct human/account.
- A shared phone/PIN can authenticate several resident records and create ambiguous contexts.
- The context object is serialized into session and later trusted by `SignInFromContextAsync`; account/grant state should be revalidated at selection/commit time rather than trusting an earlier snapshot.
- The direct resident login and unified login use different eligibility filters (`IsActive` is missing from the direct path).
- Credential normalization and phone matching must be centralized and explicit. Do not silently trim or normalize secrets in a way that changes their verifier semantics.

Target:
- Authenticate a specific UserAccount; resolve active grants after credential verification.
- Make context IDs opaque and unique per account + scope assignment.
- Revalidate account state, credential version, MFA state, and selected scope before issuing a cookie.
- Do not use phone equality as identity proof or auto-linking evidence.
- Use the same policy service for all login entry points.

## 4. Credential model and apartment PIN behavior

Evidence:
- `Services/ResidentsService.cs`
- `Services/SqlAuthService.cs`
- `Controllers/AccountController.cs`
- `Services/PinMigrationService.cs`

The resident service exposes both `SetApartmentPinAsync` and `UpdateResidentPinAsync`; the apartment-wide method assigns the same hash to every resident in the apartment and creates a primary resident if none exists. The legacy SetPin link calls this apartment-wide method. Resident creation/change methods write a verifier on an individual Resident row, so two different credential semantics coexist.

Target:
- Credentials belong to UserAccount, not Apartment or Person.
- A PIN reset targets one account, requires proof/authorization, records actor/reason, revokes sessions, and may require MFA/step-up for administrators.
- No apartment-wide credential reset. Any future shared access code must be a separate, explicitly scoped access capability with expiry and rotation, never an account credential.
- Password, PIN, external identity provider, and recovery factors are separate credential types with policy, verifier version, throttling, and lifecycle.
- Legacy apartment PINs must be mapped only with evidence and a controlled enrollment/reset path.

## 5. Authorization and SuperAdmin

Evidence:
- `Attributes/AdminPermissionAttribute.cs`
- `Models/AdminPermissions.cs`
- `Controllers/AccountController.cs`
- `Data/UserSqlStore.cs`
- `Controllers/AdminResidentsController.cs`

The current `AdminPermissionAttribute` returns early for any role other than admin, which implicitly lets SuperAdmin through. It also supports `Auth:EnforceAdminPermissions=false` as an emergency bypass. The login cookie assigns `AdminPermissions.SuperAdminAll` to SuperAdmin regardless of explicit persisted grants. These are all legacy implicit-bypass behaviors.

Approved DEC-39 requires:
- no implicit bypass based on role name;
- explicit System-scope grants for SuperAdmin actions;
- separate explicit grants and fresh MFA for secrets, restore, global secret operations, and system maintenance;
- actor and target scope checks on every operation.

The target authorization engine must evaluate:
1. authenticated account and active session;
2. active Person/account state and credential/session version;
3. active role assignment at the requested time;
4. permission grant at the requested scope and target resource;
5. building/apartment membership and effective dates;
6. explicit deny/override precedence;
7. step-up/MFA and separation-of-duties requirements;
8. audit context and reason for privileged operations.

The current attribute remains a legacy compatibility mechanism during transition only; do not build new domains on it.

## 6. MFA and session lifecycle

Evidence:
- `Controllers/AccountController.cs`
- `Services/TwoFactorService.cs`
- `Data/Entities.cs`

Current MFA is available for admin/superadmin but optional: the sign-in flow challenges only if `IsEnabledAsync` returns true. DEC-10 now requires mandatory administrator MFA, with enrollment and controlled bootstrap. Residents remain optional under DEC-11.

Target requirements:
- no privileged session until required MFA enrollment/verification is complete;
- short-lived enrollment state, single-use recovery codes, replay prevention, revocation, and audited recovery;
- explicit trusted-device policy if introduced;
- session registry with device/session ID, issued/last-seen/revoked timestamps, account/credential version, and scope;
- revoke all sessions on account disable, credential reset, role/grant removal, suspected compromise, or Person/account merge;
- rotate cookie/session identifiers on authentication and privilege changes;
- revalidate high-risk authorization against current grants, not stale cookie permission claims.

## 7. Resident management and access history

Evidence:
- `Controllers/AdminResidentsController.cs`
- `Services/ResidentsService.cs`

Resident administration currently creates, edits, disables, and deletes Resident rows. It uses `IsOwner`, `IsPrimary`, `IsActive`, and apartment FK as current-state flags. It does not preserve effective-dated ownership/occupancy or a history of who was entitled to access an apartment.

Target:
- effective-dated OwnershipInterest and Occupancy records with provenance;
- ApartmentAccessGrant for family, tenant, caretaker, emergency, and temporary access;
- separate Person profile and account lifecycle;
- no hard delete for identity/access evidence; close/revoke with reason and actor;
- access evaluation based on the event time and current grant state;
- account-to-apartment association can be many-to-many only through explicit grants, not inferred from one Resident row.

## 8. Immediate implementation sequence

1. Add characterization tests for all current login paths, context selection, disable/active behavior, PIN changes, MFA, permission attributes, and building scope. These tests must capture current behavior without legitimizing unsafe rules.
2. Implement the target authorization contract and policy evaluator in parallel with the approved permission catalog; add tests for explicit System/Building/Apartment/Personal scope and SuperAdmin no-bypass.
3. Implement account/session versioning and mandatory administrator MFA enrollment/step-up.
4. Implement Person/UserAccount and grant/assignment persistence using the approved Gate A design, with mapping records and evidence states.
5. Build a dry-run identity reconciliation report before any backfill; ambiguous records remain unresolved.
6. Replace legacy login/context flows with account-centric authentication and explicit scope selection.
7. Replace apartment-wide PIN methods and startup PIN migration with per-account credential lifecycle and explicit migration/reset.
8. Replace resident CRUD with effective-dated Person, ownership, occupancy, and ApartmentAccessGrant workflows.
9. Remove legacy authorization paths only after all controllers/services/views have moved and regression tests prove parity or intentional policy change.

## 9. Review boundaries and next work

- Findings are based on checked-in source inspection.
- No credentials were tested and no account/identity records were changed.
- No migration or database operation was executed.
- No build or tests were run as part of this review.
- Full-project implementation is owner-authorized. Database rehearsal must use a verified disposable restore; production/live cutover remains separately gated.
- Continue next with controller-by-controller authorization coverage and a permission-consumer matrix, then implement the target policy engine and tests.
