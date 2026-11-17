# Roadmap Phase08 — Authentication & Two-Factor Authentication

**Implementation branch:** `development/roadmap-phase08-authentication-2fa`  
**Base:** `development/phase11-audit-observability`  
**Status:** Implemented pending local build, tests, and TEST migration validation  
**Production:** Explicitly excluded

## Verified baseline

- Resident authentication uses apartment/building context plus PIN.
- Admin authentication uses building, phone, and PIN.
- Super-admin authentication uses email and password.
- Unified login can resolve multiple account contexts before selecting a role.
- Login throttling, anti-forgery, cookie protections, and session revalidation already exist.
- No TOTP enrollment, second-factor challenge, or one-time recovery-code workflow was present in the inspected Phase11 branch.

## Implemented scope

- RFC 6238 TOTP (SHA-1, six digits, 30-second step, ±1 step tolerance).
- Encrypted TOTP secret at rest using the existing AES-GCM encryption service.
- Ten one-time recovery codes; only SHA-256 hashes are stored.
- Replay protection using the last accepted TOTP step.
- Enrollment, verification, recovery-code display, and disable workflows for admins and super-admins.
- Second-factor challenge integrated with direct admin/super-admin login and unified/multi-context login.
- Authentication cookie is issued only after successful second-factor verification.
- Session cookies carry an explicit second-factor proof claim; old administrator sessions are rejected after 2FA enrollment (within the existing two-minute admin validation cache).
- Super-admin session validation checks active/disabled state and 2FA proof against the database.
- Original actor 2FA proof is preserved and revalidated across impersonation and nested impersonation restoration.
- Successful enrollment refreshes the current cookie with verified 2FA proof so the recovery-code page remains accessible.
- Residents and QR resident quick-login remain unchanged.
- Sidebar access to two-factor settings.
- EF migration adds four nullable/defaulted fields to the Users table.

## Rollout decision

Two-factor authentication is opt-in in this implementation. This avoids locking out current administrator accounts before enrollment communications and recovery procedures are agreed. Making it mandatory is a separate policy decision and must not be enabled without an enrollment/lockout plan.

## Validation required

1. Run `dotnet build`.
2. Run `dotnet test Tests/BuildingManagement.Tests`.
3. Apply migration `20261117130000_AddUserTwoFactor` to a disposable/restored TEST database only.
4. Validate enrollment with an authenticator app, login challenge, recovery-code single use, replay rejection, disable flow, and legacy sign-in for accounts without 2FA.
5. Confirm the Users row stores an encrypted secret and hashes only; never share the secret or recovery codes in diagnostics.

No production database, deployment, or secrets were accessed or changed.
