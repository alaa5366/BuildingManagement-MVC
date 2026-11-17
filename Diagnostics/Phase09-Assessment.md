# Phase09 – Session Revocation and Impersonation Assessment

**Review baseline:** `development/phase08-security-authorization-review`  
**Current branch:** `development/phase09-session-revocation-hardening`  
**Review type:** Read-only source inspection  
**Production / database:** Not accessed

## Confirmed findings

### Finding 1 — Universal impersonation bypass

In `Services/SessionRevalidator.cs`, any principal carrying `impersonated=true` receives `Result(true, null)` before role, UID, building, or account state is checked.

**Impact:** Disabling or deactivating the initiating account, removing its building membership, or changing its permissions does not revoke the impersonated session through this validator. The effective upper bound is currently the two-hour impersonation cookie lifetime, assuming the cookie is not otherwise replaced.

### Finding 2 — Synthetic resident identity

`Controllers/ImpersonationController.cs` builds resident impersonation claims with a synthetic UID (`imp-{apt.Id}`) and stores the initiating UID in `impersonatedBy`. The synthetic UID is not a resident database identity, so ordinary resident validation cannot be applied directly.

**Required design:** Validate the initiating identity and its current authority for the target building/apartment, while separately validating that the target apartment still exists and remains within the permitted scope.

### Finding 3 — Super-admin impersonation path

The building-admin impersonation path creates an admin-role principal using the super-admin UID and adds `originalRole=superadmin`. Current validation accepts this path based on claims/email configuration rather than revalidating a persisted super-admin account lifecycle.

**Required design:** Establish the authoritative super-admin eligibility source and how removal/disablement is expected to revoke sessions. The configured email allow-list alone may not represent the full account lifecycle.

### Finding 4 — Existing normal-session outage behavior

Normal admin and resident validation caches results for two minutes and can use a last-known-good result for up to fifteen minutes during database errors. This is a documented availability trade-off. Phase09 must not accidentally broaden this fallback or change ordinary-session behavior without dedicated tests.

### Finding 5 — Restoration stack is not an authorization substitute

The protected, two-hour backup stack preserves the prior UID, role, building IDs, and permissions. It is used to restore the previous identity. Current authorization must still be checked against current account state; a valid protected snapshot can be stale.

## Recommended implementation sequence

1. Add explicit initiating-identity metadata to every impersonation transition, with compatibility behavior defined for existing cookies.
2. Add a dedicated validator for impersonated principals; validate the initiator, target scope, and nested-stack relationship.
3. Wire that validator into cookie principal validation without treating synthetic resident IDs as real resident UIDs.
4. Add tests for all entry/exit paths, nested impersonation, revocation, malformed claims, expiry, and database failures.
5. Only then remove the unconditional bypass.
6. Run the full build/test suite and record user-confirmed results.

## Decisions intentionally not made in this assessment

- Immediate database lookup on every request versus a bounded cache.
- Whether super-admin status is controlled by configuration, database state, or both.
- Whether database outages should terminate impersonated sessions immediately or honor a short last-known-good interval.
- Compatibility policy for impersonation cookies issued before the new claims are introduced.

These decisions affect security and availability and must be resolved in implementation with tests; they must not be guessed from the current code.

## Conclusion

This is a concrete, high-priority session-lifecycle gap, but a blind removal of the bypass would break synthetic resident impersonation. Phase09 is therefore scoped around an explicit initiating-identity model and tested revocation, not a superficial change to the early return.
