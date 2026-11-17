# Phase10 – Backup and Restore Security Hardening

**Status:** IMPLEMENTED — local build/test validation pending  
**Branch:** `development/phase10-backup-restore-security`  
**Base:** `development/phase09-session-revocation-hardening`  
**Database:** No schema or data changes authorized  
**Production:** Explicitly excluded

## Objective

Harden the backup restore workflow so the archive reviewed by a super-admin is exactly the archive later restored, temporary restore resources are bounded, malformed or ambiguous ZIP files are rejected before confirmation, and internal exceptions are not exposed to the browser.

## Scope

### A. Restore-session lifecycle

- Preserve user binding, 20-minute expiry, and single-use consumption.
- Add a global and per-user active-session bound.
- Clean expired session records and their temp files on preview/restore activity.
- Ensure session admission failure removes the just-created temp file.

### B. Preview-to-restore integrity

- Calculate SHA-256 for the validated temporary archive and keep it only in server-side session state.
- Recompute and compare the digest in fixed time immediately before restore.
- Fail closed and consume/delete the session file on missing, changed, or expired archive data.

### C. Strict archive validation

- Reject duplicate entries, directory entries, path-bearing names, unknown files, and unknown collection names.
- Permit the generated format: optional `README.md` plus one root-level JSON file per known collection.
- Validate JSON root and every row envelope (`id` non-empty string and `data` object) before creating a session.
- Retain existing compressed/expanded size and entry-count limits.
- Reuse the same archive validation rules during restore so preview and execution cannot diverge.

### D. Safe error handling

- Log detailed server-side exceptions.
- Return stable, generic user-facing errors for upload/preview/restore failures.
- Preserve existing localization and normal success details.

### E. Scheduled-backup configuration and retention

- Validate schedule frequency, UTC hour, weekly/monthly day ranges, and selected collections server-side.
- Deduplicate collection selections and reject unknown collection names.
- Enforce retention between 1 and 30; defensively clamp during rotation to protect against invalid legacy database values.

## Required tests

- Existing valid and legacy plaintext ZIP previews continue to work.
- Duplicate entries, path entries, unknown entries, malformed row envelopes, and invalid JSON are rejected.
- Session remains bound to its user, expires, and is single-use.
- Per-user/global session admission limits are enforced and expired sessions are cleaned.
- Modified temp archive is rejected before any database write.
- Existing restore transaction, overwrite/merge behavior, and full regression tests remain green.

## Guardrails

- Do not change the backup JSON/ZIP format.
- Do not change restore ordering or overwrite semantics.
- Do not introduce a migration or SQL script.
- Do not access or modify Production.
- Keep Phase09 and earlier accepted branches unchanged.
- Run the complete .NET build and test suite locally before Phase10 acceptance.

## Implementation delivered

- Added strict archive validation shared by preview and restore.
- Rejected duplicate entries, path-bearing names, directory entries, unknown files/collections, invalid JSON roots, and malformed row envelopes; enforced actual decompressed-byte limits while parsing, not only ZIP metadata.
- Bound each restore session to the SHA-256 digest of the reviewed archive and verify it in fixed time using the same open file handle used for restore.
- Validated scheduled backup settings server-side and protected rotation from invalid retention values.
- Preserved 20-minute expiry and single-use user binding; added limits of 2 active sessions per user and 10 process-wide, with expired-session cleanup.
- Sanitized backup create/preview/restore and scheduled-settings exception responses while retaining server-side diagnostic logging.
- Added regression tests for duplicate entries, path-bearing entries, malformed row envelopes, archive tampering, and per-user session limits.
- Updated the security architecture documentation.

## Validation record

The assistant has not run the local .NET build or test suite. User-confirmed local results are pending. No database or production operation was performed.
