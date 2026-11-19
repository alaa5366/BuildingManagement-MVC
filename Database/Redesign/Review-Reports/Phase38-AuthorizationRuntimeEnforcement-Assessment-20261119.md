# Phase38 — Runtime Authorization Enforcement Assessment

Date: 2026-11-19  
Branch: `development/database-redesign-enterprise`  
Precondition: Phase37 committed three distinct building-scoped Admin assignments in `BuildingManagement_RestoreTest`.

## Executive result

Phase37 is complete at the target-data layer. Runtime isolation is **not yet proven or enforced end-to-end**. Do not treat the three assignment rows as sufficient protection until the MVC request pipeline and every resource-bearing use case evaluate those assignments.

## Confirmed implementation facts

1. `Attributes/HasPermissionAttribute.cs` and `Attributes/AdminPermissionAttribute.cs` are synchronous MVC authorization filters. They authorize from role and `perm` claims. Neither calls the target-model evaluator.
2. `Services/AuthorizationEvaluator.cs` already evaluates current `UserRoleAssignments`, `RoleDefinitions`, `RolePermissions`, account status, security stamp, explicit denies, and MFA freshness. It requires a server-resolved `ResolvedAuthorizationResource`.
3. The evaluator's actor lookup parses `ClaimTypes.NameIdentifier` as a GUID and looks up `UserAccounts.AccountId`. Existing legacy Admin session flows use a legacy UID and `UsersService`/legacy `Users` records. A direct filter swap would therefore fail closed for legacy Admin sessions unless an explicit, verified identity bridge is added.
4. `Program.cs` already registers `IAuthorizationPermissionCatalog` and `AuthorizationEvaluator` as singleton and scoped services respectively.
5. `SessionRevalidator` still validates legacy Admin sessions from legacy user records, claimed `buildingId` values, and legacy permission lists with a short cache. It does not currently validate the new target-model role assignment scope.
6. `AuditLogController` currently scopes Admin queries from the `buildingId` claim. That is not sufficient evidence that the claim matches the authoritative Phase37 assignment. Other resource controllers must be checked for the same pattern and for IDOR through submitted resource identifiers.

## Required Phase38 work — ordered

### A. Establish a trusted actor bridge
- Resolve the authenticated legacy Admin UID to exactly one active target `UserAccount` through the canonical identity-linkage records.
- Reject missing, duplicate, ambiguous, inactive, archived, or stale mappings.
- Never interpret an arbitrary UID as an AccountId or trust a client-supplied account identifier.
- Preserve the two existing SuperAdmin accounts and their system-wide access through an explicit, separately verified policy path. Do not grant system scope to ordinary Admins.

### B. Make authorization asynchronous and current-data-backed
- Replace claim-only permission decisions on target-model protected operations with an asynchronous authorization service/filter.
- Evaluate current assignment validity, revocation, role active state, permission effect, scope, security stamp, and MFA freshness.
- Fail closed on unknown permission keys, missing actor bridge, missing resources, invalid resource shape, and database/evaluation errors.
- Avoid relying on stale permission claims as authorization authority. Claims may remain presentation hints only.

### C. Resolve every protected resource on the server
- For apartment, resident, camera, DVR, expense, revenue, wallet/deposit, maintenance, poll, and audit-log operations, resolve the target row's BuildingId from the database before evaluation.
- Do not authorize from a submitted BuildingId, ApartmentId, route value, hidden field, or legacy building claim alone.
- For apartment resources, verify the apartment belongs to the resolved building.
- For list/export endpoints, constrain the database query by the authoritative building scope before materializing rows.
- Missing resources and mismatched parent-child relationships must fail closed.

### D. Session and revocation behavior
- Update session validation so role assignment revocations and scope changes take effect promptly.
- Do not permit the two-minute legacy permission cache to preserve revoked building scope.
- Bind session identity to the current target AccountId/security stamp after the trusted bridge is established.
- Keep SuperAdmin validation independent and preserve existing MFA controls.

### E. Verification suite
Must cover:
- Each of the three Admins can access only its own Building.
- Each Admin is denied for the other two Buildings, including direct object IDs, POST actions, exports, and nested apartment IDs.
- A missing or ambiguous UID-to-AccountId mapping is denied.
- Revoked/expired assignment and inactive role are denied without waiting for stale permission claims.
- Explicit Deny overrides Allow.
- Missing/deleted resource and mismatched apartment/building relationship are denied.
- Both existing SuperAdmins retain system-wide access and are not downgraded.
- MFA-required permissions deny stale/missing MFA proof.

## Important implementation constraint

Do **not** replace the two attributes with a generic evaluator call that supplies `ScopeType.System` or takes a building identifier from the request. The evaluator is intentionally resource-aware; each action must supply a server-resolved resource. A blanket change without this work risks either breaking valid sessions or creating a false sense of isolation.

## Phase38 acceptance criteria

- No protected Admin controller/service path relies solely on `perm` or `buildingId` claims.
- All resource-bearing paths have server-side BuildingId resolution and scope checks.
- The three Admin negative cross-building tests pass for read and write operations.
- Both SuperAdmin regression tests pass.
- Revocation and MFA tests pass.
- Build and full automated tests pass in the user's environment; do not report them as passed until actual command output is available.
- Phase38 report records exact files, tests, and evidence; no live/production database mutation is part of this phase.
