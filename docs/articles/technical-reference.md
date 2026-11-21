# Technical Reference

This document is the implementation-oriented reference for the current ASP.NET Core 8 MVC application. It describes the observed source structure and current workflows; it is not a promise that every legacy subsystem has already been retired.

## 1. Runtime and solution layout

| Area | Responsibility |
|---|---|
| `Controllers/` | MVC endpoints, authorization boundaries, model binding and response selection |
| `Services/` | Application services for authentication, accounts, invoices, backups, OCR, QR, wallet and supporting workflows |
| `Data/` | EF Core context, entities, query/read services and canonical target finance workflows |
| `Models/` | Domain and UI-facing models |
| `ViewModels/` | View-specific input and presentation contracts |
| `Views/` | Razor UI; shared layout, role screens and in-app Help |
| `Resources/` | Shared resource keys and Arabic, English, French and German translations |
| `Migrations/` | EF Core schema migrations |
| `Tests/BuildingManagement.Tests/` | xUnit tests, source-integrity checks and service/controller behavior tests |
| `docs/` | DocFX documentation source |

Target framework: .NET 8. The web project and test project are separate. Run validation from the repository root:

```powershell
dotnet build .\BuildingManagementMVC.csproj --configuration Release
dotnet test .\Tests\BuildingManagement.Tests\BuildingManagement.Tests.csproj --configuration Release
git diff --check
```

If a change introduces an EF Core schema migration, apply it only to the intended TEST database as part of the same validation cycle:

```powershell
dotnet ef database update
```

Do not run database updates against production as part of local QA.

## 2. Identity and authentication

The target identity model separates a person from an account, credentials, role assignments and apartment occupancy. Relevant entities include `PersonEntity`, `UserAccountEntity`, `UserCredentialEntity`, `UserRoleAssignmentEntity`, `RoleDefinitionEntity` and `ApartmentOccupancyEntity`.

### Sign-in contracts

- Resident and BuildingAdmin: normalized mobile number plus PIN/credential.
- SuperAdmin: allow-listed email plus password.
- SuperAdmin assignments must be active global assignments; building roles are scoped assignments.
- Active role assignments and occupancy validity are checked against UTC validity windows.
- Multiple matching non-SuperAdmin people for a normalized phone are rejected rather than disclosing matching identities.
- Credentials are verified through the configured password hasher and supported hash algorithm identifiers.

### Role and scope model

One account can carry multiple assignments. A Resident assignment is scoped to a building/apartment or the supported building-wide resident scope; a BuildingAdmin assignment is scoped to one or more buildings. Authorization must evaluate the selected role and scope for each request. Do not infer building access from a person record or from a phone number.

The role-context selection UI is a presentation of available active assignments, not an authorization grant. The server must revalidate the selected assignment and scope before issuing or using the session.

### Session validation and impersonation

Authentication cookies carry identity and scope claims. The session revalidator checks the current account/security stamp and role/scope authority according to the configured cache policy. Impersonation stores a protected restoration stack so nested contexts can return to the previous actor. Restored first-party identities must include the canonical target-identity marker and original security stamp; otherwise session revalidation rejects the restored cookie.

Do not use impersonation as a bypass for authorization. Target building/apartment membership and the initiating actor's authority must be checked on the server.

## 3. Authorization and tenant isolation

- Controller actions must declare authorization explicitly with `[Authorize]` or `[AllowAnonymous]`.
- Building and apartment identifiers supplied by the client are untrusted input.
- Every read/write path must derive or validate the effective building/apartment scope against the authenticated principal and active role assignment.
- SuperAdmin global access is distinct from BuildingAdmin scope.
- Resident access is limited to the apartment(s) granted by active occupancy and role assignments.
- Tests should include same-user multi-role access, cross-building denial, cross-apartment denial, revoked/expired assignments and nested impersonation restoration.

## 4. Wallet and financial workflows

The target finance path uses explicit deposit/expense/revenue workflows and ledger-backed posting rather than treating a UI status field as the accounting record. Read models project wallet balances, deposit history and statements for the permitted scope.

Key invariants:
- A pending deposit is not a confirmed payment.
- Posting must be idempotent against its source reference.
- Confirm, cancel and reverse are distinct transitions.
- A closed or missing source financial period blocks protected mutations.
- Reversal/cancellation must preserve source provenance and audit history.
- Do not silently mutate financial data during application startup, migration or QA.
- Legacy wallet surfaces may remain for compatibility; verify the actual controller/service path before changing legacy code.

For financial changes, test duplicate submission, concurrent confirmation, cancellation vs confirmation races, repeated reversal, closed-period rejection, source-reference uniqueness and building isolation against a disposable SQL Server test database where SQL semantics matter.

## 5. Receipt uploads and OCR

The current OCR service normalizes supported uploads through SkiaSharp (including WebP) to PNG, applies a 30-megapixel decode ceiling and reads the stream from position zero. It uses Tesseract LSTM with English and Arabic language data: the fast models provide the initial Auto/SingleBlock pass, and the larger accuracy models are used for difficult captures. When required, the service applies grayscale conversion, deskew, controlled upscaling, adaptive Sauvola/Otsu thresholding and optional orientation detection. Candidate selection combines Tesseract mean confidence with receipt-content signals. Model files live under `App_Data/tessdata`, outside the public web root; keep them in build/publish output.

The transaction parser handles Arabic/Persian digits, currency labels and separators, amount labels, references, dates, sender/recipient text and success/failure/pending language. Failure signals take precedence over generic success wording. Unknown or pending states must not be promoted to confirmed payments.

Important limitations:
- Tesseract mean confidence is not field-level accuracy or proof that a value is correct.
- The current service includes basic deskew and thresholding but not perspective correction or advanced illumination normalization.
- The repository includes synthetic image-level regression fixtures for success/failure/pending, Arabic/English and PNG/WebP decoding, plus separate parser text fixtures. These validate controlled examples; they do not establish accuracy on genuine provider screenshots.
- Real image evaluation requires a permissioned, anonymized corpus from multiple providers and phone/display conditions.
- OCR must remain advisory; a human or trusted provider reconciliation confirms settlement.

## 6. Invoice and QR verification

Invoice PDFs and QR images use a signed verification URL. The verification endpoint is anonymous but rate limited, rejects invalid/oversized invoice identifiers and invalid signatures, and reads the issued invoice snapshot. The public verification view intentionally exposes only minimal invoice metadata; do not add resident names, phone numbers, balances or other personal/financial details.

## 7. Backup and restore

The restore pipeline is designed as preview then confirm:
1. Upload and validate the archive.
2. Create a server-side restore session bound to the previewing user.
3. Retain the reviewed archive digest and opaque session identifier.
4. Enforce expiry and single-use semantics.
5. Revalidate the same archive bytes/digest at restore time.
6. Validate ZIP entry names, allowed collections, size limits and row shape.
7. Apply the restore in a database transaction and roll back on failure.

Restore tests must use a disposable database or isolated test fixture. Do not run a restore against production for UI testing.

## 8. Audit and observability

Audit records capture actor, role, building/apartment scope, action, severity and sanitized metadata. Application logs provide operational diagnostics and request correlation. The Audit Log read path is paged; receipt enrichment must remain scoped to the event's building and apartment and should not be added to unbounded export paths.

Never log credentials, PINs, TOTP secrets, recovery codes, full payment credentials or unredacted private receipt data. Correlation IDs are server generated and are useful for joining a support report to application events.

## 9. Localization contract

The UI supports `ar`, `en`, `fr` and `de`. Razor views use the shared localization helper and `Resources/Shared.<culture>.resx` files, with `Shared.resx` as the neutral fallback.

When adding a literal `Loc.T("Key")` or `Loc.Js("Key")` reference:
1. Add the key to the neutral resource.
2. Add a translation to every supported culture.
3. Preserve format placeholders such as `{0}` and `{1}`.
4. Keep HTML out of plain text values unless the caller explicitly renders trusted localized markup.
5. Run the localization coverage audit and XML parse validation.

The current coverage script is `scripts/Repair-ViewLocalizationCoverage.ps1`. It checks literal localization references in all Razor views; dynamic key construction still requires manual review.

## 10. Security and operational constraints

See [Security](security.md) for secrets, data protection, anti-forgery, rate limiting, headers, QR tokens and restore details. Do not:
- commit secrets or print secret values into logs;
- reset/stash/overwrite unrelated working-tree changes during maintenance;
- mutate production or financial data for QA;
- test destructive restore behavior on a live database;
- treat an OCR result, UI state or audit log as the authoritative financial source.

## 11. Current verification baseline

Record each validation run with branch, commit/worktree state, exact command, exit code, test counts and any environment limitations. A Release build does not prove the application can start if required environment secrets are absent. Browser/UI checks must be reported separately from unit tests and source-level checks.


## Verification baseline (2026-11-21)

- Release build: succeeded with 0 warnings and 0 errors.
- Full automated test suite: 719 passed, 0 failed, 0 skipped.
- OCR image regression subset: 8 passed, including Arabic/English synthetic receipts, success/failure/pending, WebP normalization, invalid image handling and 90-degree rotation.
- Publish verification: English, Arabic and orientation models were present in the published `App_Data/tessdata` directory, including the fast-model set.
- Localization audit: 91 Razor views, 1,057 distinct literal `Loc.T`/`Loc.Js` keys; all five resource files have zero missing keys, duplicate keys or replacement characters.
- Documentation relative-link audit: 0 broken relative links.
- DocFX 2.81.0 was installed and the documentation site build completed successfully with zero warnings and zero errors; the missing API navigation TOC was added at docs/api/toc.yml.
- No database connection, migration or data mutation was performed during this documentation/OCR pass.

## Mandatory administrator MFA and SQL Server restore validation

The privileged sign-in policy requires TOTP MFA for active Admin and SuperAdmin role contexts. A privileged sign-in does not receive an authentication cookie until an unenrolled account completes the ten-minute enrollment flow or an enrolled account completes the five-minute verification challenge. The pending sign-in is held in server-side session state. Role assignments are revalidated before enrollment completes, including the SuperAdmin allow-list and active system-scope assignment.

Session revalidation rejects privileged sessions without both an enabled target-schema MFA record and the twoFactorVerified claim. This also applies to the original actor session during impersonation. Self-service MFA disable is blocked in both the controller and service layer while this policy is active. Accounts with resident and administrator roles retain resident-only sign-in behavior; MFA is required when selecting the administrator context.

Real SQL Server backup/restore tests live in Tests/BuildingManagement.SqlServerIntegration, separate from the fast InMemory unit suite. They use the BM_SQLSERVER_TEST_CONNECTION environment variable and must target an isolated, migration-updated TEST database only. The current integration coverage verifies a persisted backup/restore round trip and a later-collection foreign-key failure that rolls back earlier writes and reports zero restored rows.

Validation baseline for this change:
- Release application, unit-test, and SQL Server integration project builds: zero warnings and zero errors.
- Unit suite: 729 passed, zero failed, zero skipped, including resident-plus-admin, SuperAdmin MFA, and translation CSV formula-injection regressions.
- SQL Server integration suite: 2 passed, zero failed, zero skipped when BM_SQLSERVER_TEST_CONNECTION targets the isolated test database; both tests are automatically skipped when that variable is absent.
- SQL Server migration chain was applied to the isolated LocalDB instance BM_QA_20261121 and database BM_QA_Restore_20261121; no production database was used.
- DocFX 2.81.0 documentation build: zero warnings and zero errors.
- MFA resource audit: 27 MFA keys in each of the five resource files, with no duplicate keys or empty values.
- CSV formula-injection coverage: application-log and translation exports use the shared sanitizer; audit-log and financial-report exports already use it.
