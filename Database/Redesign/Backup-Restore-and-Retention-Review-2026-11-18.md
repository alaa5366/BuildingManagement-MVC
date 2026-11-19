# Backup, Restore, and Retention Review — 2026-11-18

**Branch:** `development/database-redesign-enterprise`  
**Review type:** static source review of backup services, restore flow, scheduler, controller, and persistence entities.  
**Database impact:** none. No backup was created, no restore was run, and no files or database records were changed.

## 1. Executive summary

The backup implementation already includes meaningful safeguards: encrypted-at-rest backup files, archive size limits, archive validation, restore preview, per-user preview sessions, SHA-256 binding between preview and restore, safe temporary-file naming, FK-ordered restoration, and a database transaction for relational restore.

The remaining issues are mostly operational and lifecycle-related:

1. Restore preview sessions are held in a static in-process dictionary. Preview and execution must reach the same application process; this fails across multiple instances/restarts and does not provide durable session ownership.
2. Scheduled backup execution has no distributed lock/lease. Multiple application instances can all pass `ShouldRunAsync` before any instance updates `LastRunAt`, producing duplicate concurrent backups.
3. Backup file names have minute precision (`scheduled-yyyy-MM-dd-HHmm.zip`). Two runs in the same minute can overwrite the same file while creating multiple history rows with the same filename.
4. Retention deletes old scheduled files but does not delete or mark the corresponding BackupHistory rows, so history can refer to files that no longer exist.
5. Manual delete returns success and deletes history rows even when the requested file does not exist, because a null path does not prevent the history delete/audit path.
6. Backups are stored under the application content root (or web-root parent) on local disk. This is not a durable/shared storage contract for containers, ephemeral hosts, or scale-out.
7. Backup history records `FileSize = bytes.Length` while the file written to disk is the protected/encrypted payload, so the recorded size may differ from actual on-disk size.
8. The scheduler stores its run time in UTC and schedules by UTC hour/day; no explicit deployment timezone policy is captured.

## 2. Positive safeguards to preserve

- Backup creation validates requested collection names against the known collection catalog.
- Restore preview reads with a hard 100 MB limit before and after decryption.
- Preview validates the archive before issuing a restore session.
- Restore sessions are bound to the requesting user and expire after 20 minutes.
- The restore session stores a SHA-256 digest; execution verifies the same file contents while keeping one read handle open through archive processing.
- Restore uses a fixed FK-oriented collection order and rejects unknown selected collection names.
- Relational restore runs inside a database transaction; row/collection errors cause rollback rather than partial commit.
- Existing records are updated in place when overwrite is enabled rather than being deleted/reinserted, reducing accidental cascade deletion.
- Backup files are encrypted at rest; the download endpoint decrypts only for the authenticated superadmin download response.

These controls should be retained and strengthened rather than replaced.

## 3. Restore session and scale-out behavior

`BackupService` stores `RestoreSession` records in a static `ConcurrentDictionary`, protected by a static lock. Sessions contain the temporary path, user ID, expiry, and SHA-256. The service caps active sessions globally and per user.

This works only inside one process:
- A process restart loses all sessions, although temporary files may remain until external cleanup.
- In a multi-instance deployment, the preview request may reach instance A and ExecuteRestore may reach instance B, which has no matching session.
- Session limits and cleanup are per process, not global.

**Target direction:** persist restore-session metadata in a shared store with a one-time atomic consume operation, bind it to user + tenant/environment + archive hash, and store the reviewed archive in shared protected storage. Retain one-time execution, expiry, and hash verification.

## 4. Scheduler concurrency and idempotency

`ShouldRunAsync` reads settings and compares `LastRunAt` with the current UTC date/time. `RunScheduledBackupAsync` separately reads settings, creates the backup, writes a file, inserts BackupHistory, and finally updates LastRunAt.

There is no atomic claim or distributed lease around this sequence. Two instances can both pass the time check before either writes the new LastRunAt. A manual RunNow can overlap a scheduled run as well.

**Target direction:**
- Add a database-backed lease/lock with owner, expiry, heartbeat, and fencing token.
- Atomically claim the scheduled run before generating a backup.
- Give every run a unique RunId and file key; never use minute-only filenames.
- Make history insertion and run-state transition idempotent.
- Distinguish manual, scheduled, retry, and recovery runs.
- Decide whether the scheduler is active on every instance or in a dedicated worker.

## 5. Retention and backup history integrity

`RotateBackupsAsync` removes files beyond the configured count but does not update/delete corresponding BackupHistory rows. This creates stale history records that appear to describe available backups.

`DeleteBackupAsync` deletes the file only if a safe path exists, but proceeds to delete matching BackupHistory rows and log a successful deletion even if the file was already absent. Its return value can therefore indicate success when no backup file was actually removed.

**Target direction:**
- Model backup lifecycle states: Creating, Available, Failed, RetentionPending, Deleted, Missing, Corrupt.
- Use RunId as the stable identity; filename is only storage metadata.
- Make file deletion and history transition a recoverable workflow (filesystem/object storage and SQL cannot share one atomic transaction).
- Retention should mark a record pending, delete the object, then mark it deleted; reconciliation should detect orphan files and missing objects.
- Never erase history evidence when deleting the binary. Keep who/when/reason and integrity metadata according to retention policy.

## 6. Storage, integrity, and observability

The current backup directory is derived from application content/web root and stored on local disk. In production this needs an explicit storage abstraction:
- durable shared volume or object storage;
- encryption key lifecycle independent of application instance;
- object versioning/immutability where required;
- checksums and encryption metadata;
- access logging and restore authorization;
- periodic restore drills to an isolated environment.

The history file size currently uses the unprotected archive byte count, while the disk file is written after `BackupFileProtector.Protect`. Store both logical archive size and physical protected-object size with explicit field names.

The current scheduler uses UTC for its schedule. Document this as an intentional UTC policy or add an explicit timezone setting; do not rely on server-local time assumptions.

## 7. Restore semantics and safeguards still to specify

The restore service supports selected collections and overwrite/skip behavior. It processes selected collections in a fixed order and rolls back all relational changes if any row/collection error occurs. This is safer than partial silent restore, but the user-facing contract should make the following explicit:

- Which collection combinations are valid when parents/dependencies are omitted.
- Whether restore is an additive merge or a replacement (current behavior is upsert/skip, not full database replacement).
- How identity, credentials, two-factor secrets, external references, and encrypted values are restored across environments.
- How restore interacts with active sessions, caches, background workers, QR tokens, and outbox deliveries.
- Whether a restore operation must place the application in maintenance/read-only mode and invalidate all active sessions.
- How the system records pre-restore state and supports rollback/recovery after a technically successful but semantically invalid restore.

Do not treat successful row persistence as proof that the restored application is operationally consistent.

## 8. Target domain boundaries

- `BackupRun` and `BackupRunCollection`
- `BackupObject` storage abstraction
- `BackupArtifactIntegrity` (hash, encryption version, key reference, size)
- `BackupRetentionPolicy` and `BackupRetentionEvent`
- `RestoreSession` shared one-time state
- `RestorePlan` with dependency validation and preview hash
- `RestoreExecution` with idempotency, progress, audit, and recovery checkpoints
- `BackupReconciliationWorker` for stale history/orphan object detection
- distributed scheduler lease and dedicated worker ownership

## 9. Review boundaries

- Findings are based on checked-in source inspection only.
- No backup or restore operation was executed.
- No files or database records were modified.
- No build or tests were run.
- Gate A remains **NOT AUTHORIZED**.
