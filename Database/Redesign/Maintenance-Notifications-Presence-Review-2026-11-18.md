# Maintenance, Notifications, and Presence Domain Review — 2026-11-18

**Branch:** `development/database-redesign-enterprise`  
**Review type:** static source review of services, controllers, EF entities, and the aggregate persistence path.  
**Database impact:** none. No DDL, DML, migrations, builds, or runtime tests were performed.

## 1. Executive summary

The same aggregate-first compatibility pattern extends beyond Finance and Voting. Maintenance records, notifications, and presence are still modeled around building/apartment collections rather than independent business identities and durable workflows.

Confirmed findings:
1. A maintenance record can be converted to an expense, but the link to the generated Expense is not stored. Later edits/deletion of the maintenance record can leave a detached or inconsistent expense.
2. Once `AddedToExpense` is true, editing the maintenance record's cost/date/title does not update or reverse the expense already created.
3. Maintenance deletion is allowed regardless of whether its cost has already been posted as an expense.
4. Notifications are addressed to an apartment, not an individual resident account/person. Multiple resident accounts in one apartment therefore share the same notification read state.
5. Presence records are keyed by a composite of account/apartment/building, but the building query collapses results into a dictionary keyed only by ApartmentId. Multiple active accounts in one apartment overwrite one another in the returned result.
6. Presence read/write errors are swallowed and converted to empty/zero results, which can make a persistence outage look like “nobody online”.
7. The notification and maintenance commands are persisted through full Building aggregate synchronization rather than independently scoped repositories and outbox processing.

## 2. Maintenance domain

Evidence:
- `Services/MaintenanceService.cs`
- `Controllers/MaintenanceController.cs`
- `Data/Entities.cs` (`MaintenanceRecordEntity`)
- `Data/BuildingSqlMapper.cs`
- `Models/Building.cs`

### 2.1 Missing durable link between maintenance work and financial expense

`AddCostToExpenses` creates a new Expense using the maintenance record's cost/title/vendor/date and sets `AddedToExpense = true` with a timestamp. It does not store the generated Expense.Id on the maintenance record or create a dedicated MaintenanceExpenseLink entity.

The Boolean is not a durable relational link. It cannot answer which expense was created, whether that expense was later reversed/deleted, or whether a correction is required.

### 2.2 Posted maintenance data can diverge from its expense

`MaintenanceService.Update` allows title, description, date, cost, vendor, and status to be changed without checking `AddedToExpense`. If the record has already been converted to an expense, the expense remains unchanged while the maintenance record changes.

`MaintenanceService.Delete` removes the maintenance record without checking `AddedToExpense`. The linked expense (which has no stored FK/reference back to maintenance) can remain with no traceable source record.

**Target direction:**
- Model maintenance work and financial posting as separate but explicitly linked aggregates.
- Store a unique `MaintenanceExpenseLink` (MaintenanceRecordId, ExpenseId, PostedAt, PostedBy, status).
- Once posted, either freeze financial source fields or require a correction workflow that reverses/replaces the expense and records the relationship.
- Use archive/cancel rather than physical delete for maintenance records with financial impact.

### 2.3 Status and cost lifecycle

Maintenance status is a free-form string constrained in service code to open/inprogress/done/cancelled. There is no explicit transition graph, status history, assignment/approval, or completion evidence in the reviewed model. Cost can be zero and is accepted; negative cost is rejected. A record can be posted to expenses only when Cost > 0.

The target needs explicit work-order lifecycle, actor/time history, vendor/quotation/invoice evidence where required, and a controlled financial-posting state.

## 3. Notifications domain

Evidence:
- `Services/NotificationsService.cs`
- `Controllers/NotificationsController.cs`
- `Data/Entities.cs` (`NotificationEntity`)
- `Data/BuildingSqlMapper.cs`

### 3.1 Apartment-scoped delivery/read state

Notifications use `RecipientType = resident` and `RecipientId = ApartmentId`. Read and mark-all-read operations filter by apartment. This means notification ownership is shared by every resident account currently associated with that apartment.

This conflicts with the target Person/Account model and prevents individual delivery preferences, independent read state, and privacy boundaries for multiple people sharing an apartment.

**Target direction:** address notifications to a Person or UserAccount, with an optional Apartment/Building context. Preserve recipient snapshot and delivery channel separately from the business event.

### 3.2 In-app notification is coupled to the aggregate

Notification creation is currently an in-memory append to `Building.Notifications`, persisted when the caller later executes `SaveFullAsync`. This makes delivery dependent on every caller remembering to save the aggregate and creates coupling between business state changes and notification persistence.

The target should use a transactional outbox written in the same database transaction as the business event. A background dispatcher should deliver in-app/email/WhatsApp messages with idempotency, retry state, provider response, and dead-letter handling. Read state should be stored separately from the immutable notification event.

### 3.3 Read operations are full-aggregate operations

The notification controller loads the full Building aggregate before listing or marking notifications read. Marking one notification read therefore participates in the same broad aggregate read/modify/save lifecycle and rowversion contention as unrelated building operations.

Target notification queries and read-state commands should be narrowly scoped and paginated at the database level.

## 4. Presence domain

Evidence:
- `Services/PresenceService.cs`
- `Controllers/PresenceController.cs`
- `Data/Entities.cs` (`PresenceEntity`)

### 4.1 Multiple accounts in one apartment are collapsed

Heartbeat identity is built from account UID, apartment ID, and building ID. This permits separate presence records for separate accounts. However, `GetByBuildingAsync` stores each result in a dictionary keyed by ApartmentId. If two accounts in the same apartment are online, the later row overwrites the earlier one in the returned dictionary.

The result is apartment presence, not account presence. The UI cannot reliably show which account(s) are online, and counts may differ from distinct apartment counts.

Target API contracts should distinguish:
- online accounts;
- online people;
- apartments with at least one online account;
- active sessions/devices.

### 4.2 Failure is represented as a valid empty state

Heartbeat exceptions are logged and swallowed. Online-count failures return zero; list failures return an empty dictionary. This is resilient for request availability, but the caller cannot distinguish “no active users” from “presence storage unavailable”.

Return a typed result or health indicator and surface degraded state separately from a true zero count.

### 4.3 Session/device model is missing

The current record stores a last-seen timestamp and account/apartment/building scope. It does not represent a durable session/device identifier, client type, session revocation, or concurrent sessions per account. A user with multiple browser/device sessions is collapsed according to the current composite key.

The target should model presence as expiring session/device leases, use server-side account scope, and define cleanup/TTL behavior. Any scale-out deployment must rely on shared database/cache semantics rather than process-local state.

## 5. Target boundaries

### Maintenance
- `MaintenanceWorkOrder`
- `MaintenanceStatusHistory`
- `MaintenanceAttachment`
- `Vendor` / `MaintenanceQuote` where required
- `MaintenanceExpenseLink`
- explicit archive/cancel and financial correction workflows

### Notifications
- immutable `NotificationEvent`
- `NotificationRecipient` (Person/Account scoped)
- `NotificationReadState`
- `NotificationDeliveryAttempt`
- transactional `OutboxMessage`
- provider adapters with idempotent retry/dead-letter behavior

### Presence
- `UserSession` / `DeviceSession`
- `PresenceLease` with expiry and heartbeat
- account/person/apartment rollups as separate query projections
- explicit degraded/unknown state when presence storage is unavailable

## 6. Review boundaries

- Findings are based on checked-in source inspection only.
- No database was queried in this phase.
- No code or database changes were made.
- No build or test run was performed.
- Gate A remains **NOT AUTHORIZED**.
