# Security, Audit, and Operations Review — 2026-11-18

**Branch:** `development/database-redesign-enterprise`  
**Review type:** static source review of settings, audit, application-log, startup, Data Protection, health, and rate-limit code.  
**Database impact:** none. No secrets were read from a live environment and no configuration/database changes were made.

## 1. Executive summary

The project has several good foundations: role/permission checks on the application-log and global-settings screens, antiforgery filters, login and log-query rate limits, a minimal health response, forwarded-header trust configuration, Data Protection key encryption, and audit rows that are appended rather than synchronized/deleted with the Building aggregate.

The remaining design issues:
1. Global WhatsApp API token and Cloudinary API key are stored in GlobalSettings and returned by `GetGlobalAsync` to the MVC view model; the Global settings view renders their plaintext values into HTML input fields.
2. Legacy PIN migration is executed during application startup and mutates Users, Residents, Buildings, and Apartments. It is idempotent for already-hashed values, but deployment startup is still coupled to a data migration with no explicit operator-controlled migration phase or cutover gate.
3. Data Protection keys default to a local filesystem path under the content root in Development or LocalApplicationData in non-Development. Multi-instance/container deployments need a shared durable key ring and stable application identity.
4. Application logs are read directly from local `logs/serilog` files, scanning up to the newest 14 files on each query. This is not a centralized, multi-instance log store and can return an empty result when the directory is unavailable.
5. The CSP currently allows `unsafe-inline` scripts/styles and broad HTTP/HTTPS media/connect/frame sources. This is a compatibility policy, not a hardened strict CSP.
6. Health endpoints are mapped without endpoint authorization; the current response is intentionally minimal (status, duration, check names/statuses), so this is an explicit operational exposure to review rather than a confirmed sensitive-data leak.
7. Rate limiting is endpoint-specific: login POST and application-log lookup are covered; it is not a global limiter and does not by itself protect every sensitive command or upload endpoint.
8. AuditLogService caps the in-memory aggregate list at 1,000 entries, while the SQL mapper separately preserves append-only audit rows. This distinction must remain explicit in the target design; never reintroduce delete-on-absence synchronization for audit evidence.

## 2. Global secret handling

Evidence:
- `Services/SettingsService.cs` — `GetGlobalAsync` and `SaveGlobalAsync`
- `Models/SettingsModels.cs` — `GlobalSettings`
- `Views/Settings/Global.cshtml`
- `Data/Entities.cs` — `GlobalSettingsEntity`

`GetGlobalAsync` maps `WhatsappApiToken` and `CloudinaryApiKey` from the database into the `GlobalSettings` model. The view renders both values directly in input elements. The controller is restricted to superadmin plus the global-settings permission, which is a meaningful access control, but the secrets are still exposed to the browser DOM and any client-side compromise/session capture.

**Target direction:**
- Secret values must be write-only: return a configured/not-configured indicator, never the stored secret.
- Use a dedicated secret provider (environment/secret manager) or encrypted secret store with key rotation and access audit.
- Allow replace/clear operations without echoing the current value.
- Do not mix public configuration, feature flags, and secret material in one view model/entity.
- Rotate any secret that has been rendered into a client page if exposure is suspected.

## 3. Startup data migration

Evidence:
- `Program.cs`
- `Services/PinMigrationService.cs`

Program startup invokes `MigrateLegacySecretsAsync` before serving requests. It:
- hashes plaintext admin/superadmin PIN/password values;
- hashes resident PIN values that are not already BCrypt;
- clears legacy Building.AdminPin values;
- creates a primary resident account from legacy Apartment.Pin when no resident exists, or assigns a hash to residents with blank PIN hashes;
- clears Apartment.Pin after the resident credential exists.

The implementation is mostly idempotent because BCrypt values are skipped and apartment PINs are cleared only after a resident account exists. However, this remains a data migration executed implicitly by every application instance at startup. Startup can race in scale-out, and a deployment rollback does not automatically restore the legacy values.

**Target direction:** move this into a versioned, explicit migration command with dry-run, preflight counts, transactional batches, durable progress, conflict handling, backup checkpoint, and an operator-approved cutover. Application startup should validate required migration state, not mutate identity records.

## 4. Data Protection and encryption-key lifecycle

Evidence:
- `Program.cs`

The application persists Data Protection keys to a configured path, defaulting to a local folder when no path is configured, and encrypts the key ring using an application encryption secret. This is preferable to ephemeral keys, but it does not define a shared key ring for multiple instances or a durable volume/object store for container restarts.

Target requirements:
- stable shared key ring across instances and deployments;
- explicit key rotation and retirement policy;
- backup/recovery of key material independent of database backups;
- separate purposes/keys for Data Protection, backup-file encryption, QR signing, Excel signing, and application secret encryption;
- tested recovery when the encryption secret is rotated or unavailable.

## 5. Application log explorer

Evidence:
- `Services/ApplicationLogQueryService.cs`
- `Controllers/ApplicationLogsController.cs`

Positive controls:
- Controller requires admin/superadmin role and the ViewApplicationLogs permission.
- Response caching is disabled.
- Lookup rate limiting is applied.
- Page/page-size and text fields are bounded.
- The parser redacts common secret assignments, connection strings, bearer/JWT values, email, phone, filesystem paths, and IP addresses before returning log entries.

Limitations:
- Reads only the newest 14 local Serilog files.
- Enumerates and scans log content for each query rather than using an indexed centralized store.
- If the directory cannot be enumerated/read, it logs a warning and returns an empty page; the UI may not distinguish unavailable logs from no matches.
- Redaction is regex-based and cannot guarantee removal of every secret format or sensitive payload.

Target direction: centralized structured log ingestion with indexed time/source/correlation fields, tenant-aware authorization, explicit data classification, retention, export auditing, and a visible degraded-source state. Redaction must happen at ingestion as well as presentation.

## 6. Audit evidence

Evidence:
- `Services/AuditLogService.cs`
- `Data/BuildingSqlMapper.cs`
- `Services/AuditLogQueryService.cs`

The aggregate helper limits its in-memory list to the latest 1,000 entries, but the SQL mapper explicitly loads aggregate-originated audit rows by SourceId and appends only unknown SourceIds. It does not use the generic delete-on-absence Sync helper for AuditLog. Direct IAuditLogger rows are queried separately and are not re-appended on every aggregate save.

Preserve this append-only distinction. The target design should unify audit event contracts and actor context, but should never treat audit evidence as a replaceable child collection. Audit retention, privacy redaction, tamper evidence, and legal hold should be policy-driven.

## 7. Health, proxy, and HTTP security

Evidence:
- `Program.cs`
- `Services/HealthCheckResponseWriter.cs`

- `/health/live` and `/health/ready` (plus backward-compatible `/health`) are mapped without explicit authorization. The writer intentionally exposes only aggregate status, duration, and check names/statuses, not exception details. Decide whether readiness is public, network-restricted, or authenticated at the ingress.
- Forwarded headers trust only configured KnownProxies for X-Forwarded-For/Proto/Host. Without configured proxies, only X-Forwarded-Proto is processed. Preserve strict proxy trust; do not accept arbitrary forwarded host/IP values.
- CSP permits inline scripts/styles and HTTP/HTTPS in several resource directives. Reduce this incrementally using nonces/hashes and explicit trusted origins; remove HTTP sources where production HTTPS is required.
- HSTS and HTTPS redirection are enabled outside Development.
- Rate limiting is configured for login and lookup endpoints; sensitive operations and resource-intensive upload/restore paths need endpoint-specific quotas and concurrency limits.

## 8. Target platform boundaries

- `SecretStore` / `SecretReference` and secret rotation
- `IdentityMigrationRun` with explicit dry-run/apply/cutover states
- shared `DataProtectionKeyRing` infrastructure
- centralized `ApplicationTelemetry` pipeline
- append-only `AuditEvent` with immutable actor/target/scope metadata
- explicit `HealthReadinessPolicy` and ingress restrictions
- endpoint-specific rate-limit and concurrency policy catalog

## 9. Review boundaries

- Findings are based on checked-in source inspection only.
- No production secrets or environment values were accessed.
- No migration, DDL, DML, build, or test run was performed.
- No code or configuration was changed.
- Gate A remains **NOT AUTHORIZED**.
