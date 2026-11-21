# Architecture

## Application layers

| Folder | Responsibility |
|---|---|
| `Controllers/` | MVC request boundaries, authorization, validation and response selection |
| `Services/` | Application workflows: identity, authentication, invoices, QR, backup, OCR and supporting domain services |
| `Data/` | EF Core context/entities, target read models and canonical target financial workflows |
| `Models/` | Domain and application models |
| `ViewModels/` | UI input and presentation contracts |
| `Views/` | Razor pages, shared layout and in-app user guide |
| `Resources/` | Shared localization resources for Arabic, English, French and German |
| `Migrations/` | EF Core schema migrations |
| `Tests/` | xUnit tests, source-integrity rules and service/controller tests |
| `docs/` | DocFX source documentation and generated API reference |

## Identity model

Identity separates people, accounts, credentials, role assignments and apartment occupancy. A single account can carry multiple active roles and scopes. Resident access is apartment/occupancy scoped; BuildingAdmin access is building scoped; SuperAdmin is a global allow-listed role. The selected context is revalidated by the server and is never treated as a standalone authorization grant.

Resident and BuildingAdmin sign-in use a registered mobile number and PIN. SuperAdmin sign-in uses an allow-listed email and password. TOTP is applied when enabled or required by the account policy.

## Financial model

The target financial workflows use explicit source records and ledger posting. UI status values are not a substitute for ledger entries. Pending deposits do not count as confirmed payments. Posting must be idempotent, preserve source provenance and respect financial-period state. Closed or missing source periods block protected financial mutations.

Some legacy-compatible screens/services may still exist. Before modifying a legacy path, trace the controller action and actual service/data dependencies to establish whether it is still part of the active workflow.

## Concurrency and writes

Building aggregate writes use a per-building in-process write lock and SQL Server row-version concurrency protection. The row version is advanced for aggregate saves so child-only changes are detected as concurrent writes. Callers should reload and retry after a concurrency conflict rather than silently overwriting newer state.

Financial posting has separate idempotency and concurrency requirements; do not assume the building aggregate lock protects ledger commands.

## Cross-cutting flows

- **Request correlation:** server-generated correlation IDs connect request logs and relevant audit metadata. Do not trust caller-supplied correlation IDs.
- **Authentication:** target identity credentials and active role assignments are checked before a cookie is issued.
- **Role context:** a person may have multiple role/scope assignments. Context selection chooses among valid assignments; every request still requires server-side authorization.
- **Impersonation:** protected restoration data supports nested return to the prior actor. Restored cookies must satisfy the canonical target-identity and security-stamp contract.
- **Receipts:** Tesseract OCR proposes text/transaction fields. OCR output never confirms payment; an authorized reviewer must verify settlement.
- **Invoices:** PDFs and QR links use signed invoice verification. The anonymous verification view exposes limited metadata only.
- **Backups:** restore is a preview/confirm workflow bound to the user and reviewed archive digest; database application is transactional.
- **Audit and observability:** audit records track important actions; application logs support runtime diagnosis. Read paths must enforce tenant scope and avoid unbounded enrichment.

See the [technical reference](technical-reference.md) for implementation details and the [user handbook](user-handbook.md) for operating procedures.
