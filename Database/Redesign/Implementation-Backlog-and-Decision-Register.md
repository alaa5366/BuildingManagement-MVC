# Implementation Backlog and Decision Register

**Date:** 2026-11-18  
**Branch:** `development/database-redesign-enterprise`  
**Status:** Planning artifact only. No implementation or database execution is authorized by this backlog.

## 1. Delivery rules

- Execute work in dependency order; do not parallelize changes that alter the same domain contract.
- Each epic requires a design review, implementation plan, tests, operational notes, and acceptance evidence.
- No epic may silently change the meaning of legacy financial, identity, or access data.
- Database changes start only after the relevant gate is explicitly approved and rehearsed on a disposable restore.
- Do not combine broad UI rewrites with foundational data migrations in one unreviewable change.
- Keep the legacy path available until its replacement has passed parity and rollback criteria.

## 2. Epic backlog

| ID | Epic | Priority | Dependencies | Completion evidence |
|---|---|---:|---|---|
| ARCH-01 | Architecture and domain boundary freeze | P0 | None | Approved module map, dependency rules, aggregate ownership, glossary |
| DATA-01 | Reconcile schema and deployment truth | P0 | ARCH-01 | Live inventory vs EF model/snapshot/history vs FullSchema vs views/triggers report |
| DATA-02 | Finalize target ERD and data dictionary | P0 | DATA-01 | Key types/lengths/collations, constraints, delete rules, indexes approved |
| DATA-03 | Identity mapping and provenance contract | P0 | DATA-02 | LegacyIdentityMap rules, uniqueness, review queue, restart semantics |
| DATA-04 | Finance source mapping and parity contract | P0 | DATA-01 | Per-building/apartment/month totals, variances explained, rounding policy |
| SEC-01 | Authorization scope catalogue | P0 | ARCH-01 | Every permission key classified by scope/resource/consumer; ambiguous grants held |
| SEC-02 | Security baseline and secret inventory | P0 | ARCH-01 | Secret locations, key management, redaction, rate-limit, no-store, threat model |
| APP-01 | Application foundation | P1 | ARCH-01 | Composition root, typed outcomes, validation, clock/current actor, correlation, cancellation |
| APP-02 | Transactional audit and outbox | P1 | APP-01, SEC-02 | Atomic audit/outbox tests, idempotent worker, retry/dead-letter/compensation |
| ID-01 | Person and account domain | P1 | DATA-02, DATA-03, APP-01 | One-account-per-person invariant and legacy provenance tests |
| ID-02 | Authentication and credential policies | P1 | ID-01, SEC-02 | No secret trimming; PIN/password policy; lockout/throttling; safe credential migration |
| ID-03 | MFA and session lifecycle | P1 | ID-02, SEC-01 | Admin/SuperAdmin mandatory enrollment, recovery, replay prevention, revocation SLA |
| AUTH-01 | Scoped role assignment and permission evaluator | P1 | SEC-01, ID-01 | System/building/apartment/personal matrix, deny precedence, fail-closed tests |
| PROP-01 | Ownership, occupancy and apartment access | P1 | ID-01, AUTH-01 | Temporal overlap/share constraints, explicit access grants, retained history |
| FIN-01 | Financial ledger and command state machines | P1 | APP-01, DATA-04 | Idempotency, concurrency, reversals, strict periods, no physical delete |
| FIN-02 | Wallet projections and reconciliation | P1 | FIN-01 | Legacy/target parity, deterministic rounding, documented variance sign-off |
| POLL-01 | Eligibility, delegation and secret ballot | P1 | ID-01, PROP-01, AUTH-01 | No voter-choice join path, duplicate prevention, secrecy and tally tests |
| OPS-01 | Maintenance, notifications and presence | P2 | PROP-01, AUTH-01, APP-02 | Explicit visibility/recipient/counting semantics and bounded queries |
| DOC-01 | Private document and receipt pipeline | P1 | APP-02, SEC-02 | Private storage, decoded-content limits, authorized download, orphan compensation |
| INT-01 | OCR and import hardening | P2 | DOC-01, APP-01 | OCR advisory-only, per-user limits, no silent coercion/defaults |
| MEDIA-01 | Camera/DVR/media orchestration | P2 | AUTH-01, APP-02, SEC-02 | Narrow credential access, retryable provisioning/decommissioning |
| SET-01 | Settings and integration secret vault | P2 | APP-01, SEC-02 | Write-only secrets, encrypted storage, no GET side effects |
| OPS-02 | Backup, restore and retention | P1 | APP-02, SEC-02 | Typed outcomes, artifact/history reconciliation, restore drill, no-store downloads |
| OBS-01 | Audit and application log center | P2 | APP-02, AUTH-01 | Scoped query, correlation, retention, safe redaction, query budgets |
| WEB-01 | New controllers, view models and views | P2 | Relevant module APIs | Thin controllers, no EF entities/secrets, antiforgery and authorization coverage |
| CUT-01 | Compatibility adapters and module cutover | P1 | Module acceptance | Feature-flagged rollout, parity, rollback, no legacy write drift |
| RET-01 | Legacy retirement | P3 | All cutovers | Zero unexplained variances, retention approval, explicit decommission approval |

## 3. First execution sequence

1. **ARCH-01:** freeze module boundaries and shared vocabulary.
2. **DATA-01:** close the live-schema/EF/FullSchema/view deployment discrepancies. The view inventory finding must be reconciled with the checked-in schema artifact before choosing a deployment mechanism.
3. **DATA-02:** reconcile exact key metadata; do not infer physical types from the proposed ERD.
4. **SEC-01:** classify every permission key by its actual controller/service/resource consumer.
5. **DATA-03:** finalize LegacyIdentityMap and identity review workflow.
6. **DATA-04:** define the authoritative financial reconciliation baseline, including the already-observed small rounding variances and unexplained source-period cases.
7. Only after these artifacts are reviewed: prepare a Gate A rehearsal plan and request explicit approval.

## 4. Decision register

Status values: **Open** = requires business/technical approval; **Proposed** = a safe default is recommended but not yet accepted; **Evidence required** = cannot be decided from current source/profile; **Blocked** = dependent work must not proceed.

| ID | Decision | Recommended default | Status / blocking impact |
|---|---|---|---|
| DEC-01 | Human identity proof and merge authority | No automatic merge; designated reviewer approves with evidence and durable audit | Open; blocks identity backfill |
| DEC-02 | Shared phone/email handling | Treat as shared-contact candidate, not identity equality | Proposed; blocks fuzzy matching |
| DEC-03 | Person without account | Allow Person/ownership/occupancy without interactive account | Proposed; confirm notice delivery |
| DEC-04 | Account cardinality | Zero or one interactive account per Person; enforce transactionally and uniquely | Approved principle; schema/implementation pending |
| DEC-05 | Ownership evidence and shares | Require explicit source evidence, 0–100 share, approved total policy, effective dates | Open; blocks ownership backfill |
| DEC-06 | Primary occupancy | At most one active primary occupant per apartment at an instant; primary may be absent | Approved principle; temporal enforcement design pending |
| DEC-07 | Temporary/emergency access | Explicit dated grant with issuer, reason, scope and revocation | Open; blocks access migration |
| DEC-08 | Legacy permission scope | Classify each permission by actual resource consumer; ambiguous grants remain unresolved | Evidence required; blocks permission backfill |
| DEC-09 | SuperAdmin semantics | System-scoped role assignment on ordinary account, not separate identity type | Approved principle |
| DEC-10 | Admin MFA rollout | Mandatory with staged enrollment, recovery, bootstrap and monitored exception process | Approved direction; operational details open |
| DEC-11 | Resident MFA | Optional, with safe recovery | Proposed |
| DEC-12 | Voting subject | Poll explicitly declares Person or Apartment subject; Apartment requires representative delegation | Open; blocks poll redesign |
| DEC-13 | Ballot secrecy | Separate eligibility/nullifier from anonymous ballot choice; no direct join key | Approved principle; cryptographic design review required |
| DEC-14 | Ballot retention/correction | Retain immutable ballot evidence and documented annul/re-poll process; never edit individual choice | Open |
| DEC-15 | Financial accounting basis | Preserve apartment wallet semantics until accounting owner signs off on ledger model | Evidence required; blocks finance cutover |
| DEC-16 | Opening balance and period close | Explicit opening entries and closed-period correction via reversal/adjustment | Proposed |
| DEC-17 | Rounding remainder | Deterministic documented allocation, independent of incidental query ordering | Open; blocks financial parity |
| DEC-18 | Financial deletion/erasure | Posted entries are reversed/voided, not physically deleted | Approved target principle |
| DEC-19 | Maintenance visibility | Default to apartment/resource scope unless an explicit building-wide category is approved | Open |
| DEC-20 | Notification ownership/read state | Define recipient as Person/account/apartment per notification type | Open |
| DEC-21 | Presence meaning | Count explicit subject type (person/account/device/apartment); expose label in UI | Open |
| DEC-22 | Receipt storage and retention | Private object storage, short-lived authorized downloads, documented retention | Open |
| DEC-23 | OCR role | Advisory extraction only; never confirms payment | Approved principle |
| DEC-24 | Media authorization | Enforce at application and media-server layers; no direct credential DTO exposure | Approved principle |
| DEC-25 | Backup deletion semantics | Reconcile artifact and history; typed partial/missing states, audited operator action | Open |
| DEC-26 | Audit retention/tamper evidence | Separate security/business audit from diagnostic logs; define retention and privileged access | Open |
| DEC-27 | Deployment topology | Treat current deployment as single instance until proven otherwise; design ports for future shared state | Evidence required |
| DEC-28 | SQL view deployment | One authoritative, versioned mechanism; reconcile migration chain and FullSchema first | Blocked by DATA-01 |
| DEC-29 | Startup legacy PIN migration | Remove/feature-gate automatic startup mutation; require dry-run/checkpoint/operator approval | P0 blocker before runtime validation on preserved legacy data |
| DEC-30 | Health endpoint exposure | Minimal liveness public to trusted infrastructure; readiness details restricted to monitoring network | Open |

## 5. Definition of Done for every epic

An epic is complete only when all applicable items are true:

- Domain invariants are explicit and tested.
- Authorization is tested at the use-case and endpoint boundaries, including negative cases.
- Persistence constraints and transaction behavior are integration-tested against SQL Server.
- Query performance is measured on representative restored data with bounded result sizes.
- Audit records are durable, correlated, privacy-safe, and tested for failure behavior.
- External side effects are idempotent and have retry/compensation paths.
- Migration/reconciliation evidence exists when legacy data is involved.
- Rollback and operational runbooks are written and rehearsed where applicable.
- No secret or personal/financial row-level data is committed to Git diagnostics.
- The owner accepts the phase exit evidence.

## 6. Current gate state

- Architecture and backlog documents are planning artifacts.
- Gate A DDL remains **NOT AUTHORIZED**.
- No EF entities, migrations, database objects, or application runtime paths were changed by this planning work.
- No backfill, identity merge, credential rewrite, finance mutation, or cutover was performed.
- The next permissible work is further read-only reconciliation and finalization of the decision register; implementation begins only after the relevant approvals.
