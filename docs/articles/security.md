# Security

This page records the decisions behind the security code so they survive comment clean-ups.

## Secrets

* Secrets (`Encryption:SecretKey`, `Qr:SecretKey`, `Excel:SecretKey`, database connection string, Cloudinary, ...) come from environment variables or user secrets. `appsettings.json` only contains placeholders; the application refuses to start with a missing or placeholder secret.
* **Never change `Encryption:SecretKey` after deployment.** It protects stored DVR passwords, the scheduled backups and the ASP.NET data-protection key ring. A new key makes all of them unreadable and signs every user out.

## Sign-in

* PINs and passwords are BCrypt hashes. Plain-text comparison is off (`Auth:AllowLegacyPlainPins` is an emergency switch). On startup, plain-text PINs are hashed and obsolete plain-text columns are cleared.
* `LoginThrottle` locks an account after 5 failures and an IP address after 20, for 15 minutes. Account keys are normalized (so `0100…` and `+20100…` share one counter) and hashed. Counters are mirrored to the `LoginThrottle` table so restarts do not reset them.
* `X-Forwarded-For` is trusted only from the proxies listed in `Proxy:KnownProxies`. Without that setting only `X-Forwarded-Proto` is honored. Behind a reverse proxy, set it, otherwise every user shares the proxy's IP address.
* Error messages for wrong phone, wrong PIN and unknown account are identical, and a dummy hash is computed for unknown accounts so timing does not reveal them.
* Admin and super admin sessions last 12 hours and do not slide; resident sessions last 7 days. Residents are re-validated against the database.

## Tokens and QR

* Quick-login and set-PIN tokens are AES-GCM sealed (`v2.` prefix), carry a purpose (`login` or `setpin`) and are consumed atomically, so a one-time token can never be used twice. Opening the link only shows a confirmation page.
* QR images are generated locally by `/QrImage/Png`. URLs containing tokens are never sent to a third-party service.
* Printed (stable) QR codes keep their original signature format so existing printouts still work.

## Data at rest

* Scheduled backups are written encrypted (AES-GCM, `BMENC1` header). Older plain files are still readable.
* The ASP.NET data-protection key ring is encrypted with AES-GCM. Set `DataProtection:KeysPath` to a folder outside the publish folder.
* `logs`, `Backups` and `keys` are hidden from the web server by `web.config`. Do not share these folders.

## Backup restore

* A restore only accepts the temporary file created by the preview step (`restore-<32 hex digits>.zip` directly in the temp folder). Any other path is rejected.
* The browser receives only an opaque restore-session ID. The session is bound to the previewing user, expires after 20 minutes, is single-use, and is limited to 2 active sessions per user / 10 process-wide.
* The server stores a SHA-256 digest of the reviewed archive. Restore verifies it in fixed time while holding the same read handle used to process the ZIP, so replacing the temporary file after preview is rejected.
* ZIP validation rejects duplicate entries, directories and path-bearing names, unknown files/collections, oversized archives (including actual decompressed bytes read, not only ZIP metadata), and JSON rows that do not contain a non-empty string `id` and object `data`. The accepted format remains the application's root-level `README.md` and known `<collection>.json` files.
* Tables are restored parents first and saved table by table inside one database transaction. If anything fails, the whole restore is rolled back.
* "Overwrite" updates existing rows in place. It never deletes rows first, because the database cascades deletes to child rows (floors, apartments, expenses, deposits...) that may not be part of the restore.
* Detailed exceptions are written to server logs; browser responses use generic restore errors.
* Scheduled-backup settings are validated server-side (frequency/day/hour, known collections, and retention 1–30); rotation clamps retention defensively so invalid stored values cannot purge every scheduled backup.
* Identity columns (audit log, deposit edit history) get new values on restore; the audit log is append-only.
* Restoring an old backup that still holds plain-text PINs: the startup migration hashes them at the next start, so those users can sign in again after a restart.

## Request hardening

* Global anti-forgery validation for POST requests.
* Request limits: 5 MB body by default; backup restore (100 MB), Excel import (10 MB) and receipt images (15 MB) have their own limits.
* Receipt uploads are checked by their real file signature (JPEG, PNG, GIF, WebP, HEIC).
* Rate limits: 30 requests/minute/IP on sign-in POSTs and 120/minute/IP on the public floor/apartment lookups.
* Security headers and a Content-Security-Policy. `Security:CspMode` = `enforce` (default), `report-only` or `off`.
* Client libraries (video.js, Chart.js, Bootstrap Icons) are served from `wwwroot/lib`. Restore them with LibMan (see below).
* `AllowedHosts` in `appsettings.Production.json` lists the real host names. Add a custom domain there before pointing it at the site, otherwise requests are rejected with 400.

## Authorization

Every controller action must have `[Authorize]` or `[AllowAnonymous]`. `ControllerAuthorizationTests` and `SourceIntegrityTests` fail the build's test run when one is missing or hidden inside a comment.

## Deployment checklist

1. Set the secrets as environment variables or user secrets.
2. Set `DataProtection:KeysPath`, `Proxy:KnownProxies` (when behind a proxy) and `AllowedHosts`.
3. Run `dotnet libman restore` (or Restore Client-Side Libraries in Visual Studio) so `wwwroot/lib` contains video.js, Chart.js and Bootstrap Icons.
4. Run `dotnet test Tests/BuildingManagement.Tests`.
5. Publish, then check: sign-in (resident, admin, super admin), a welcome message with its set-PIN link, a receipt upload, a backup download and restore, the live cameras page and the reports page.


## Administrator two-factor authentication

* Admin and super-admin accounts can opt in to TOTP-based two-factor authentication from **Account → Two-factor authentication**. Resident sign-in is unchanged.
* Enrollment requires a current authenticator code before the setting is enabled. The TOTP secret is encrypted using the existing `Encryption:SecretKey`; the plaintext secret is never stored in the database.
* Ten one-time recovery codes are generated at enrollment and displayed once. Only SHA-256 hashes are stored. Each recovery code is consumed on successful use.
* TOTP uses RFC 6238, six digits, a 30-second period, and a ±1 time-step clock-skew window. An accepted time-step cannot be replayed.
* When enabled, admin/super-admin sign-in does not issue the authentication cookie until the second factor succeeds. The pending sign-in is server-side session state with a five-minute expiry. The challenge is rate limited and protected by anti-forgery validation.
* Administrator sessions carry an explicit second-factor verification claim. Session revalidation rejects pre-enrollment cookies after the database reports 2FA enabled (within the existing two-minute admin revalidation cache); super-admin checks are performed against the database on cookie validation. The original actor's verification state is preserved and rechecked during impersonation and restoration.
* Two-factor authentication is opt-in in this release to avoid locking out existing administrators before a managed enrollment rollout. A mandatory policy must be explicitly approved and communicated before deployment.
* The migration `20261117130000_AddUserTwoFactor` adds the required columns to `Users`. Apply it to TEST and validate before any production planning; no production database was changed by this branch.
