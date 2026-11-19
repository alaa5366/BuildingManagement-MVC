# Authorization Evaluation Contract and Precedence

**Date:** 2026-11-18  
**Branch:** `development/database-redesign-enterprise`  
**Status:** Proposed security contract. Owner decisions PERM-01 through PERM-05 remain open. No authorization code or grants were changed.

## 1. Purpose

Define one deterministic authorization algorithm for the redesigned application. It replaces the current split behavior of role attributes, permission attributes, role claims, building claims, and controller-specific checks.

A permission decision is always made for a concrete operation and a resolved resource. A role string or `perm` claim by itself is never sufficient.

## 2. Required evaluation input

Every evaluation must receive:
- actor AccountId;
- resolved PersonId for a human account;
- account/session state and security stamp;
- MFA assurance level and verification time;
- requested permission/action key;
- resource type and canonical resource identifier;
- resolved BuildingId and ApartmentId where applicable;
- current UTC time from the injected clock;
- source channel and correlation ID.

The client must not be allowed to choose the effective scope by posting a BuildingId/ApartmentId without the server resolving the target resource and confirming its ownership chain.

## 3. Evaluation order

1. **Authenticate:** require a valid authenticated principal and a server-validated session.
2. **Account state:** reject pending, locked, disabled, archived, or otherwise non-authorized accounts.
3. **Session assurance:** require current MFA/step-up for operations marked sensitive (e.g. secret access, restore, secret reveal, destructive maintenance).
4. **Resolve actor:** load the account's Person relationship; reject broken/missing identity linkage for human accounts.
5. **Resolve resource:** load a safe resource descriptor and its canonical scope (System, Building, Apartment, Personal). Do not authorize against client-supplied scope claims.
6. **Validate permission key:** unknown, inactive, malformed, or retired keys fail closed.
7. **Collect applicable role grants:** only assignments active at the current instant, not revoked, with an active role definition, matching the resource scope.
8. **Collect applicable account overrides:** only overrides active at the current instant, not revoked, for the exact permission and applicable scope.
9. **Apply precedence:** any applicable explicit Deny wins. Otherwise an applicable Allow from role permission or account override grants the capability. If no Allow exists, deny.
10. **Apply mandatory policy guards:** account status, resource ownership, separation-of-duties, step-up MFA, dual approval, and other hard controls cannot be overridden by a permission Allow.
11. **Record decision safely:** log correlation, actor, action, scope, outcome, and reason code; do not log secrets or sensitive payloads.

## 4. Scope matching rules

| Grant scope | Applies to | Does not apply to |
|---|---|---|
| System | System resources and all resources explicitly included by the permission's scope policy | Operations explicitly excluded by policy, e.g. high-risk secret access requiring separate grant/step-up |
| Building | The specified building and child resources whose canonical BuildingId matches | Other buildings or system administration |
| Apartment | The exact specified BuildingId + ApartmentId and child resources within that apartment | Sibling apartments or other buildings |
| Personal | The actor's own PersonId/account-owned personal resource | Other people, apartment-wide resources, or building-wide operations |

A permission definition must declare which scopes are legal. For example, `reports.view` may allow Building scope; `settings.manage.own` must allow Personal scope only. A scope that is not legal for a permission is rejected, even if a row exists.

## 5. Precedence and conflict rules

Recommended deterministic rule:

- Hard security guard failure => Deny.
- Any applicable Deny from a role permission or account override => Deny.
- Otherwise any applicable Allow => Allow.
- Otherwise => Deny.

Additional rules:
- More-specific Allow never overrides an applicable Deny. This avoids accidental privilege escalation through a narrower grant.
- A Personal grant is resolved using AccountId -> PersonId; the request cannot nominate another PersonId.
- System role assignment is not a magic bypass. It contributes explicit system-scoped permissions.
- SuperAdmin is not automatically exempt from account status, session revocation, MFA step-up, audit, or resource existence checks.
- AccountPermissionOverrides remain distinct from RolePermissions. Legacy per-user grants must never be promoted to role-wide grants.
- Explicit account Deny overrides all inherited role Allows.
- Expired/revoked grants are ignored; overlapping duplicate grants are rejected at write time and resolved conservatively at read time.

## 6. Permission definition contract

Every permission key must have a code-owned definition:

- stable key and version;
- human-readable purpose and risk level;
- allowed scope types;
- resource resolver;
- protected command/query list;
- whether MFA step-up is required;
- whether reason text is mandatory;
- whether dual approval is required;
- whether export/download is a separate capability;
- whether resident self-service uses a separate policy;
- audit event type and safe metadata schema.

No permission may be inserted by a client or accepted solely because a database row contains an arbitrary string.

## 7. Privileged operations

The following should be considered for separate grants and step-up:
- impersonation / secret access;
- backup restore and deletion;
- system maintenance/migration tools;
- integration secret reveal or rotation;
- global settings and security policy changes;
- financial period reopen or high-value wallet adjustment.

Recommended controls:
- mandatory fresh MFA step-up;
- mandatory reason;
- narrow target scope and short expiry;
- durable before/after audit;
- explicit typed outcome;
- dual approval for restore, secret reveal, or irreversible high-impact operations where operationally feasible.

## 8. Cache and revocation contract

- Authorization decisions may be cached only with a bounded TTL and explicit version/invalidation mechanism.
- Role grant, permission override, account status, Person-account linkage, MFA state, and security-stamp changes invalidate affected sessions/decisions.
- The maximum revocation delay must be an explicit security requirement, not an accidental cache duration.
- If deployment scales out, invalidation and session state must be shared; process-local caches/locks are not sufficient.
- During database unavailability, do not silently extend privileged last-known-good authorization beyond the approved fail-closed policy.

## 9. Test matrix

- Active vs disabled/locked/pending accounts.
- Anonymous, invalid, expired, revoked, and impersonated sessions.
- Admin/SuperAdmin/resident/system-job actors.
- System/Building/Apartment/Personal scopes.
- Same-building vs cross-building resource identifiers.
- Parent building grant applied to child apartment resource.
- Apartment grant not applied to sibling apartment.
- Personal grant cannot target another Person.
- Role Allow + account Deny => Deny.
- Role Deny + account Allow => Deny.
- Multiple role grants with conflicting effects => Deny.
- Expired and revoked assignments.
- Unknown permission key, role key, scope type, or resource resolver => Deny.
- Stale permission cache after grant/revoke/account disable.
- Sensitive action without fresh MFA step-up => RequiresStepUp.
- Dual-control action with same actor as requester and approver => Deny.
- Authorization failure does not disclose resource existence or sensitive metadata.

## 10. Owner decisions

- **PERM-01:** SuperAdmin default privilege model. Recommendation: no unconditional bypass; explicit System role grants, with separate grants/step-up for secret access, restore, and maintenance.
- **PERM-02:** Split finance read/confirm/cancel/adjust permissions. Recommendation: yes.
- **PERM-03:** Meaning of audit.view.own. Recommendation: Personal means own Person/account events; apartment-wide visibility requires explicit Apartment scope.
- **PERM-04:** Separate WhatsApp dispatch from integration configuration/secret administration. Recommendation: yes.
- **PERM-05:** Retire or rebuild missing import/tools/sync workflows. Recommendation: do not migrate grants until a supported use case exists.

## 11. Verification status

This is a proposed evaluation contract based on the reviewed code and permission catalogue. It does not modify claims, filters, roles, or grants. No tests were run. Gate A remains **NOT AUTHORIZED**.
