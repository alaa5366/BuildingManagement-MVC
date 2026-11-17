# Phase10 – Backup and Restore Security Assessment

**Review baseline:** `development/phase09-session-revocation-hardening`  
**Current branch:** `development/phase10-backup-restore-security`  
**Review type:** Source assessment with implementation update  
**Production / database:** Not accessed

## Existing protections confirmed

1. Backup restore is restricted to a temporary file named `restore-<32 hex digits>.zip` directly under the operating-system temp directory.
2. The browser receives an opaque restore-session identifier, not the server-side path.
3. Restore sessions are bound to the initiating user, expire after 20 minutes, and are consumed once through an atomic dictionary removal.
4. Upload size is capped at 100 MiB; the archive is capped at 100 MiB and declared expanded entry size at 500 MiB, with at most 100 ZIP entries and 100 MiB per entry.
5. Restore collections are processed in a fixed parent-before-child order and relational restores use a database transaction.
6. Existing rows are updated in place when overwrite is enabled; they are not deleted first.
7. Scheduled backup files are AES-GCM protected at rest, while legacy plaintext ZIP backups remain readable.

## Findings requiring hardening

### F10-01 — Restore-session storage has no admission bound

The process-wide `ConcurrentDictionary` has an expiry timestamp, but no global or per-user active-session limit. Expired sessions are cleaned only when another successful preview is created or the same session is used. A privileged user can accumulate many temporary archives and dictionary entries within the 20-minute window.

**Required direction:** Bound active sessions, clean expired entries on preview and restore paths, and ensure a rejected/new failed session does not leave an orphan file. Preserve the existing 20-minute expiry and one-time semantics.

### F10-02 — The preview does not cryptographically bind the reviewed archive to the later restore

The session stores a path and user ID, but no digest of the bytes that were inspected. The restore later reopens that path. The opaque token and path pattern prevent browser path injection, but do not detect replacement or modification of the temporary file between preview and execution.

**Required direction:** Store a SHA-256 digest in the server-side session and verify the file using a fixed-time comparison immediately before restore. A mismatch must fail closed and delete/consume the temporary session file.

### F10-03 — ZIP structure validation is permissive

Preview ignores non-root and unknown entries, and does not reject duplicate entry names or duplicate collection files. It deserializes a generic JSON shape but does not verify every row has the required `id` string and `data` object before presenting the preview. This can make preview and restore interpret the same archive differently.

**Required direction:** Validate archive entry names, uniqueness, allowed file types, known collection names, and row envelope structure before creating a restore session. Keep compatibility with the application's generated format (`README.md` plus known `<collection>.json` files).

### F10-04 — Restore errors can expose implementation details

The controller appends exception messages to user-visible TempData, and preview returns raw exception messages. These may disclose local paths, framework details, or database information.

**Required direction:** Keep detailed exceptions in server logs and return stable, generic user-facing errors. Do not change the authorization model.

## Explicitly out of scope

- No database schema/data changes or migrations.
- No production access, deployment, or configuration changes.
- No redesign of backup encryption keys or legacy backup compatibility.
- No distributed restore-session store or multi-instance architecture change.
- No change to collection ordering, overwrite semantics, or existing backup file format.

## Conclusion

The implementation already has useful safeguards. Phase10 should strengthen the boundary between preview and execution, bound temporary restore state, validate the archive consistently, and avoid leaking internal exception details without changing the backup format or restore business behavior.

## Implementation update

The Phase10 branch now includes the first hardening implementation:

- A shared archive validator is applied during both preview and restore.
- Restore sessions retain the SHA-256 digest of the reviewed archive; restore verifies it in fixed time on the same open file handle used to read the ZIP.
- Active sessions are capped at two per user and ten per process; expired sessions and their files are cleaned during preview/restore activity.
- Browser-facing backup create/preview/restore errors no longer append raw exception messages.
- Regression coverage was added for duplicate ZIP entries, path-bearing entries, malformed row envelopes, archive replacement, and session admission limits.

**Still pending:** user-run local build and complete test suite. This branch is not accepted until those results are confirmed.
