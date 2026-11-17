# Architecture

## Layers

| Folder | Responsibility |
|---|---|
| `Controllers/` | MVC controllers. Thin: read the request, call a service, return a view, redirect or JSON. |
| `Services/` | Business logic (wallet, authentication, residents, backups, QR tokens, reports...). |
| `Data/` | EF Core: `AppDbContext`, entities (`Data/Entities.cs`), stores and the mapper between entities and the domain models. |
| `Models/` | Domain models. `Building` is the aggregate root: floors, apartments, months, deposits, adjustments. |
| `ViewModels/` | Models that exist only to feed views. |
| `Views/` | Razor views. Every view starts with a header comment that names its controller action and model. |
| `Resources/` | Shared resource files (`Shared.resx` is the English fallback; `.ar`, `.fr`, `.de` override it). |
| `Attributes/` | Authorization filters for admin permissions. |
| `Tests/` | xUnit tests (not part of the web project build). |

## Roles

* **Super admin** – signs in with an e-mail and password; manages buildings, admins, backups and global settings.
* **Admin** – manages one building; the actions available depend on the admin's permissions.
* **Resident** – signs in with a phone number and a PIN; sees the apartment wallet, polls and maintenance.

An admin session is bound to the single building the admin signed in to.

## Money

All money values are `decimal`. Expense and revenue shares are split equally between the open apartments
and rounded to two decimals; a balance is the carried-over balance plus confirmed deposits, minus the
monthly fee and the expense share, plus the revenue share and manual adjustments. Pending and
cancelled deposits never count.

## Writes

`BuildingSqlStore.SaveFullAsync` saves the whole building aggregate in a transaction. The MVC
`BuildingWriteLockFilter` serializes write requests for the same building inside one application
instance. Cross-instance protection is provided by SQL Server `Buildings.RowVersion`: every existing
aggregate save forces a parent-row update, so child-only changes also advance the token. A stale or
missing token is rejected with `DbUpdateConcurrencyException`; callers must reload and retry rather
than silently overwriting another writer.

## Main flows

* **Request correlation** — each HTTP request receives a server-generated 32-character correlation ID. It is returned in `X-Correlation-ID`, included in structured logging scope, and attached to audit JSON details for events written during that request. Incoming client correlation headers are ignored.

* **Unified login** – one form for e-mail/phone plus password/PIN. When several accounts match, the user
  chooses one (`ChooseContext`).
* **Welcome message** – contains a one-time *set your PIN* link (`/Account/SetPin`). PINs are never sent or stored in clear text.
* **Quick QR login** – the link opens a confirmation page; only the button (POST) consumes the token.
* **Impersonation** – a protected cookie keeps a stack of identities so super admins and admins can return to their own account. Sessions are limited to two hours. Each impersonated request revalidates the initiating account, its building membership and (for admins) its permission snapshot, as well as the target building/apartment. Validation is cached for at most 30 seconds and fails closed if the authority store is unavailable; nested restoration preserves the original initiating identity.
* **Backups** – scheduled backups are encrypted on disk; the Backup page downloads them as normal ZIP files.
