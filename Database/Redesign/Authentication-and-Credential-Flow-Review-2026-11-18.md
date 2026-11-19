# Authentication and Credential Flow Review — 2026-11-18

**Repository:** `alaa5366/BuildingManagement-MVC`  
**Branch:** `development/database-redesign-enterprise`  
**Scope:** source-code review of `Program.cs`, `PinMigrationService`, `SecretVerifier`, `SqlAuthService`, `AccountController`, `ResidentsService`, `SessionRevalidator`, and `SecretVerifierTests`.  
**Execution:** repository source inspection only. No application startup, test execution, SQL DDL/DML, credential rewrite, or database connection was performed by this review.

## 1. Executive decision

The current restored database contains five Users PIN hashes and 79 Resident PIN hashes that are all BCrypt-shaped (60 characters), but that does not prove that the stored hashes are valid or that users can authenticate with them.

The existing login path verifies credentials using BCrypt. The existing startup migration is not an acceptable long-term migration mechanism: it mutates credentials and clears legacy fields during application startup. The next implementation must separate authentication, credential verification, and credential migration.

**Do not run the application against the disposable restore merely to inspect credentials.** `Program.cs` invokes `PinMigrationService.MigrateLegacySecretsAsync()` during startup.

## 2. Findings

### AUTH-01 — Credential migration runs automatically at application startup
**Severity: High / migration-safety blocker**

`Program.cs` resolves `PinMigrationService` and calls `MigrateLegacySecretsAsync()` during startup. The routine:

- Re-hashes non-empty Users PIN values that do not have a recognized BCrypt prefix.
- Re-hashes non-empty Residents.PinHash values that do not have a recognized BCrypt prefix.
- Clears non-empty `Buildings.AdminPin` values.
- Processes legacy `Apartments.Pin` values and clears the apartment field after creating or finding resident rows.

This is a data migration hidden inside normal application boot. It has no explicit operator approval, durable per-record migration audit, or per-record proof that the destination credential was verified before the source was cleared. The apartment path can assign the apartment PIN to residents with empty PinHash; if residents already have non-empty hashes, the apartment PIN is still cleared without proving those credentials are equivalent.

The latest restore showed Apartments.Pin empty for 79/79 apartments and Buildings.AdminPin empty for 3/3 buildings, so those source-clear branches had no current rows to process in this snapshot. That does not make the startup behavior safe for another database or future data.

**Required direction:** remove credential mutation from startup in a separately reviewed code change. Use an explicit, operator-invoked, auditable migration/recovery workflow. Never clear a source credential until a documented destination verification condition is met; where the original secret is unavailable, use reset/re-enrollment rather than guessing.

### AUTH-02 — BCrypt detection checks only a prefix
**Severity: High / credential-integrity blocker**

`SecretVerifier.IsBcrypt` returns true when a value starts with `$2a$`, `$2b$`, or `$2y$`. It does not validate the complete BCrypt encoding or prove that the BCrypt library can parse and verify it.

The same prefix-only concept is duplicated in `PinMigrationService.IsBcrypt`. Consequently, a malformed value with a BCrypt-looking prefix can be skipped by migration, while `SecretVerifier.Verify` later catches a BCrypt exception and returns false. The existing `SecretVerifierTests.IsBcrypt_recognizes_prefixes` test treats short prefix-only samples as BCrypt, so it currently codifies recognition rather than validity.

**Required direction:** separate concepts explicitly:
- `LooksLikeBcrypt`: syntax/shape classification only.
- `CanParseBcrypt`: a non-mutating parser/verification-library compatibility check, with aggregate-only output.
- `VerifySecret`: true credential verification requiring the user's candidate secret.

Do not label a hash cryptographically valid based on prefix, length, or a dummy candidate. A dummy candidate can help detect parser exceptions, but a false verification result does not prove the original PIN is correct.

### AUTH-03 — Direct resident login does not filter IsActive before credential verification
**Severity: Medium / authentication-flow consistency**

`SqlAuthService.SignInResidentAsync` selects a resident by phone and `!IsDisabled`, but does not include `IsActive` in the selection predicate. The unified login query does filter `IsActive`. `SessionRevalidator.ValidateResidentAsync` later requires the resident to be active and not disabled, so a disabled-by-status-but-not-flagged resident may pass the initial credential check and then have the newly issued session rejected on validation.

**Required direction:** make every authentication entry point apply the same account-state rules before creating an authenticated result. Keep session revalidation as defense in depth, not as the first point where an inactive account is rejected.

### AUTH-04 — Missing-PIN responses differ from invalid-credential responses
**Severity: Medium / account-state disclosure and consistency**

Resident and admin login paths return `pin-not-set` when a matching account has an empty credential field. `AccountController.MapError` deliberately maps wrong PIN / wrong credentials / not-in-building to one generic message, but maps `pin-not-set` to a distinct “PIN not set; contact admin” message. Unknown accounts also take the dummy BCrypt path, while matching accounts with an empty PIN return without equivalent work.

This may be a deliberate support message, but it reveals that an account/context exists and has no configured PIN, and creates a timing difference.

**Required direction:** decide the desired disclosure policy. For public authentication, prefer one generic response and comparable work for unknown, empty-credential, and invalid-credential cases. Provide PIN setup/reset through a separate authenticated or verified recovery process.

### AUTH-05 — Unified account selection is server-session-backed; revalidation is present
**Severity: Review note / no confirmed bypass**

The unified login validates the submitted credential in `FindAllContextsAsync`, then stores matching contexts in ASP.NET Session under a random GUID token. The selection POST retrieves the context from server-side Session; the client supplies only the token and selected context ID. The chosen context is removed after successful selection. This is not a client-posted arbitrary `UserContext` object.

`SignInFromContextAsync` itself constructs an AuthResult from the selected context without a fresh database lookup. However, the issued cookie is subsequently checked by `SessionRevalidator`, which validates resident active/disabled state, admin role/status/building membership and superadmin allow-list/status. Therefore the source review does **not** establish an authentication bypass here.

**Hardening direction:** keep selection state server-side, make the selection token single-use and short-lived, and re-check account status, role, and scope immediately before issuing the cookie. Preserve the existing cookie revalidation.

### AUTH-06 — Current MFA enforcement is opt-in per administrator
**Severity: High / target-architecture gap**

`AccountController.CompleteSignInAsync` sends admins and SuperAdmins through the second-factor challenge only when `TwoFactorService.IsEnabledAsync(uid)` returns true. `SessionRevalidator` also allows a session when 2FA is not enabled. This is consistent with the current opt-in behavior, but not with the approved target rule that MFA is mandatory for admin and SuperAdmin accounts and optional for residents.

**Required direction:** implement an enrollment/grace/recovery rollout before enforcing mandatory MFA. Enforcement must be server-side and consistent across direct login, unified login, context selection, QR/impersonation paths where applicable, session revalidation, and recovery. Do not simply flip a global flag without ensuring administrators have enrolled and recovery codes are available.

### AUTH-07 — One Users.Pin column serves different credential semantics
**Severity: Medium / target-model mismatch**

The same `Users.Pin` column is used for admin PINs and SuperAdmin password verification. Admin PIN creation uses a PIN-specific hasher path; SuperAdmin login treats the field as a password. The reviewed hashing calls use BCrypt work factor 10 for these PIN/password values. The target identity design should not preserve this ambiguity.

**Required direction:** represent credential purpose/type explicitly in the target credential model (for example, administrator PIN vs SuperAdmin password), with policy, rotation, recovery, and audit semantics defined per credential type. Do not infer the credential type solely from the user's role at migration time without reviewed mapping.

## 3. Safe credential verification strategy

The aggregate SQL diagnostic cannot cryptographically verify the 84 stored values because it does not possess the original PIN/password inputs. The safe next steps are:

1. Keep stored hashes in the database; never export them to CSV, logs, Git, or chat.
2. Add a local, read-only verification utility using the repository's pinned `BCrypt.Net-Next 4.2.0` library. It may test whether each stored value can be parsed by the installed verifier using a fixed dummy candidate, but must report only counts of parseable / malformed / unsupported values. A false result is expected for a valid hash when the dummy candidate is not the user's secret.
3. Treat successful login with the user's actual supplied credential as the only evidence that the credential matches. If future work-factor upgrades are needed, re-hash only after successful verification during login, with concurrency protection and no source clearing.
4. For malformed/unverifiable hashes or users who cannot authenticate, use an authenticated reset or secure re-enrollment flow. Never reinterpret an arbitrary non-BCrypt string as plaintext PIN without evidence.
5. Replace startup mutation with an explicit migration command that is dry-run-first, transactionally bounded, restartable, audited, backup-aware, and non-destructive by default.

## 4. Required authentication test matrix

Add automated tests before changing authentication behavior:

- BCrypt valid hash verifies; wrong candidate fails; malformed prefix-shaped values fail safely.
- Legacy plaintext remains rejected when the emergency switch is false.
- Empty PIN, inactive account, disabled account, wrong scope, and unknown account follow the approved generic-response policy.
- Direct resident login and unified resident login enforce identical active/disabled rules.
- Admin context selection cannot issue a session after account deactivation, role change, or building-membership revocation.
- Context selection token expires and is single-use.
- Admin and SuperAdmin cannot obtain a session without completed MFA after the mandatory-MFA rollout gate.
- Resident MFA remains optional unless the resident explicitly enables it.
- Failed credential migration leaves all source fields unchanged and writes a safe aggregate/audit outcome.

The existing test project already references the application and BCrypt package transitively, and `SecretVerifierTests.cs` exists. The tests should be extended to test malformed BCrypt verification semantics rather than treating prefix recognition as validity.

## 5. Work boundary and next gate

This document records source-review findings only. No source code was changed, no tests were executed, no application was started, no database was modified, and no credential was read out or exported by this review.

**Next implementation gate:** prepare a dedicated authentication-hardening change set (startup migration isolation, strict credential classification, consistent account-state checks, safe context revalidation, and tests) as a separate code change. Keep Gate A DDL, EF migrations, credential rewrite, backfill, and production cutover blocked until this change set is reviewed and tested on the disposable restore.
