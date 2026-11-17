# Phase10 – Backup and Restore Security Hardening

**Status:** IMPLEMENTATION IN PROGRESS — assessment completed; implementation and local validation pending  
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

## Validation record

Implementation and user-confirmed local validation will be recorded here. No local build/test or production operation is claimed by the assistant.
