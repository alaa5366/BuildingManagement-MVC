# QR Access, DVR, and Media Integration Review — 2026-11-18

**Branch:** `development/database-redesign-enterprise`  
**Review type:** static source review of QR issuance/consumption, token persistence, DVR credential handling, and MediaMTX orchestration.  
**Database impact:** none. No QR token was issued, no stream was configured, and no device credentials or environment secrets were accessed.

## 1. Executive summary

The current implementation has several good security controls:
- QR access tokens use HMAC-SHA256 signatures and fixed-time signature comparison.
- Tokens have expiry and a database record.
- QR GET confirmation is side-effect free, so link-preview bots do not consume single-use tokens.
- Single-use token consumption uses an atomic SQL compare-and-set.
- DVR source addresses are restricted to explicit private-network IP literals and RTSP paths are validated against traversal/absolute URLs.
- DVR passwords are encrypted in SQL and masked in the Details view.
- Camera deletion attempts to remove the MediaMTX path before deleting the SQL camera record.

Remaining findings:
1. QR link generation ignores the Boolean result of token persistence and can display a link even when the DB insert failed.
2. QR link duration has no upper bound; only non-positive values are replaced with 15 minutes.
3. QR token revocation exists as a service method but no caller/controller workflow was found.
4. QR login creates a synthetic NameIdentifier based on ApartmentId, not a Person/UserAccount/session identity; all users entering through the same apartment QR link collapse to the same application subject.
5. QR token is transported in the query string and hidden form field; the QR access page does not receive an explicit no-store policy in the reviewed middleware path.
6. DVR list/detail/edit services return full EF entities containing encrypted password fields rather than narrow read models.
7. Updating DVR IP/port/username/password does not update the existing MediaMTX RTSP path definitions.
8. Camera add/delete and DVR delete span SQL and MediaMTX without a durable orchestration/outbox state. Failure after one side succeeds can leave stale or missing stream configuration.
9. When AddCamera uses a generated default RTSP path, the controller registers that path with MediaMTX but persists the original empty `rtspPath` on the Camera row.
10. MediaMTX failure logs include response body text; sanitize before logging because provider errors may echo source configuration/credentials.

## 2. QR access

Evidence:
- `Services/QrAuthService.cs`
- `Services/QrTokenStoreService.cs`
- `Controllers/QrController.cs`
- `Controllers/QrAccessController.cs`
- `Services/QrGeneratorService.cs`

### 2.1 Token issuance must fail closed

`GenerateAccessLink` creates the signed token, calls `SaveTokenAsync`, but ignores its Boolean result. It then audits and renders the link regardless. If SQL persistence fails, the generated token cannot pass the later database check, yet the UI still tells the operator that a link was created.

The command must return a typed failure and must not render/audit success unless token persistence commits. Issuance, audit, and outbox evidence should be atomic.

### 2.2 Expiry and revocation

The action substitutes 15 minutes only when the requested duration is <= 0. There is no upper bound. The service accepts the requested duration and signs the resulting expiry.

Add a bounded policy (for example, short maximum duration with separate privileged approval for exceptions), validate one-time vs multi-use policy, and record explicit revoke state. `DeleteTokenAsync` has no caller in the reviewed source search; token revocation is not exposed as an operator workflow.

### 2.3 Synthetic identity and session lifecycle

On confirmation, the controller issues a resident cookie with NameIdentifier `qr-{ApartmentId}`, plus building/apartment claims. This is not an account/person identity and is not unique per human or login session. It cannot support individual accountability, multi-resident apartment access, account disablement, person-level notification privacy, or targeted session revocation.

The target should exchange a QR capability for a narrowly scoped, short-lived `ApartmentAccessSession` bound to an approved Person/UserAccount or explicitly represented guest identity, with issuer, purpose, device/session ID, expiry, revocation, and audit correlation. A QR capability should not become a permanent substitute for an authenticated resident account.

### 2.4 URL and cache exposure

The temporary token is embedded in `/QrAccess/Enter?token=...` and copied into a hidden POST form field. The reviewed global no-store middleware covers `/Account` and `/QrImage`, not `/QrAccess`. The global Referrer-Policy is strict-origin-when-cross-origin, which does not eliminate same-origin URL/referrer exposure.

Apply `Cache-Control: no-store`, `Referrer-Policy: no-referrer`, and a restrictive response policy to all capability-token pages; avoid third-party resources on those pages; consider fragment/POST exchange patterns that reduce query-string persistence. Ensure reverse-proxy/access logs redact token query values.

### 2.5 Stable apartment QR

The stable apartment QR encodes building ID, floor order, and apartment number and routes to the resident login page. This is a locator/pre-fill capability, not authentication by itself. Preserve that distinction and do not turn stable QR into an authentication bypass.

## 3. DVR credential and media integration

Evidence:
- `Services/DvrService.cs`
- `Services/MediaMtxService.cs`
- `Services/DvrNetworkPolicy.cs`
- `Controllers/DvrsController.cs`
- `Views/Dvrs/Index.cshtml`, `Details.cshtml`, `Edit.cshtml`

### 3.1 Narrow credential access

`GetByBuildingAsync` and `GetByIdAsync` return EF `DvrEntity` instances including `PasswordEncrypted`. The current views do not render the ciphertext, and Details masks the password, but the controller/view model contract still carries secret material unnecessarily.

`GetDecryptedPasswordAsync` is a public service method returning plaintext. It is currently used by the controller to construct a MediaMTX source URL.

Target:
- use explicit DVR list/detail/edit DTOs that omit PasswordEncrypted entirely;
- replace general plaintext-returning service methods with a narrowly scoped stream-provisioning capability;
- never expose decrypted credentials to controllers, views, logs, or general DTOs;
- audit every credential reveal/use and keep access to DVR secrets separate from ordinary camera management.

### 3.2 DVR update does not reconcile active streams

`UpdateAsync` changes the DVR address, port, brand, username, password, and active state in SQL. It does not update or delete existing MediaMTX paths. Existing camera streams can therefore continue using old RTSP credentials or old endpoint details after the database record has changed.

The target should model desired configuration and observed MediaMTX state separately, then reconcile them through a durable worker. A credential or endpoint change must create a provisioning job with retry and completion state.

### 3.3 Cross-system partial failure

Camera creation currently:
1. creates the SQL Camera row;
2. builds an RTSP URL using the decrypted DVR password;
3. registers a path in MediaMTX;
4. deletes the SQL row if MediaMTX registration fails.

If the compensation delete fails, an orphan SQL row remains. Camera deletion removes the MediaMTX path first and then deletes SQL; if SQL deletion fails, the DB record remains while the stream path is gone. DVR deletion removes paths sequentially; if a later path fails, earlier paths may already be deleted while the DVR and all camera rows remain.

These are distributed workflow operations and cannot be made atomic with a normal SQL transaction.

Target:
- persist a `MediaProvisioningOperation` / outbox record in SQL;
- model states Pending, Applying, Applied, RetryableFailure, PermanentFailure, and Compensating;
- reconcile desired vs observed state idempotently;
- retain the camera/DVR record until decommission is fully reconciled;
- expose partial/degraded state to operators rather than returning a generic success/failure.

### 3.4 RTSP path persistence mismatch

When the user leaves RTSP path blank, the controller builds a brand-specific default path for MediaMTX but passes the original blank value to `AddCameraAsync`. The SQL Camera row therefore stores an empty path even though MediaMTX was configured with a generated path.

Persist the effective normalized RTSP path used to provision MediaMTX, not the raw optional input.

### 3.5 Network policy

`DvrNetworkPolicy` requires IP literals in RFC1918 IPv4 or IPv6 ULA ranges, rejects loopback/unspecified/link-local/multicast, and rejects absolute/traversal RTSP paths. This is a strong SSRF baseline. Keep the allowlist explicit and additionally constrain permitted ports and target subnets by deployment/network policy. Avoid allowing a general admin to point the media server at arbitrary internal services.

## 4. Target boundaries

### QR and access capabilities
- `QrCapability` with bounded lifetime, audience/purpose, target, one-time/reusable policy, issuer, and revocation
- `ApartmentAccessSession` with Person/Account/Guest subject, session/device ID, expiry, and revocation
- one-time atomic exchange and safe no-store response policy

### DVR and media
- `DvrDevice` / `Camera` domain records without broad secret-bearing DTOs
- `DvrCredentialReference` in a secret vault
- `MediaDesiredConfiguration` and `MediaObservedConfiguration`
- transactional outbox `MediaProvisioningOperation`
- reconciliation worker, retry/dead-letter/compensation, and operator-visible state

## 5. Review boundaries

- Findings are based on checked-in source inspection only.
- No secrets, decrypted DVR credentials, or live MediaMTX configuration were accessed.
- No QR token was issued or consumed.
- No DVR, camera, MediaMTX path, or database record was changed.
- No build or tests were run.
- Gate A remains **NOT AUTHORIZED**.
