// =====================================================================
//  EF Core entities مطابقة لسكربت BuildingManagement-Schema.sql
//  الأسماء بتنتهي بـ "Entity" عشان ماتتعارضش مع كلاسات Firestore
//  اللي في BuildingManagementMvc.Models (Building, Apartment, ...)
//  أثناء فترة الانتقال.
// =====================================================================
namespace BuildingManagementMvc.Data.Entities;

// ----------------------------- المستخدمين -----------------------------
// ----------------------------- العمارات -----------------------------
/// <summary>
/// Database entity for a building.
/// </summary>
public class BuildingEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the building number.
    /// </summary>
    public string BuildingNumber { get; set; } = "";
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the admin PIN.
    /// </summary>
    public string AdminPin { get; set; } = "";
    /// <summary>
    /// Gets or sets the data version.
    /// </summary>
    public string DataVersion { get; set; } = "3.2";
    /// <summary>
    /// Gets or sets the logo URL.
    /// </summary>
    public string LogoUrl { get; set; } = "";
    /// <summary>
    /// Gets or sets the admin WhatsApp.
    /// </summary>
    public string AdminWhatsapp { get; set; } = "";
    /// <summary>
    /// Gets or sets the payment label.
    /// </summary>
    public string PaymentLabel { get; set; } = "";
    /// <summary>
    /// Gets or sets the payment account number.
    /// </summary>
    public string PaymentAccountNumber { get; set; } = "";
    /// <summary>
    /// Gets or sets the payment phone.
    /// </summary>
    public string PaymentPhone { get; set; } = "";
    /// <summary>
    /// Gets or sets the payment notes.
    /// </summary>
    public string PaymentNotes { get; set; } = "";
    /// <summary>SQL Server rowversion used for optimistic concurrency.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the settings.
    /// </summary>
    public BuildingSettingsEntity? Settings { get; set; }
/// <summary>
    /// Gets or sets the floors.
    /// </summary>
    public List<FloorEntity> Floors { get; set; } = new();
    /// <summary>
    /// Gets or sets the apartments.
    /// </summary>
    public List<ApartmentEntity> Apartments { get; set; } = new();
    /// <summary>
    /// Gets or sets the categories.
    /// </summary>
    public List<FinancialCategoryEntity> Categories { get; set; } = new();
}

/// <summary>
/// Database entity for per-building settings (invoice policy, expense distribution, voting quorum).
/// </summary>
public class BuildingSettingsEntity
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; } = "";
    /// <summary>
    /// Gets or sets the address.
    /// </summary>
    public string Address { get; set; } = "";
    /// <summary>
    /// Gets or sets the WhatsApp number.
    /// </summary>
    public string WhatsappNumber { get; set; } = "";
    /// <summary>
    /// Gets or sets the invoice day of month.
    /// </summary>
    public int InvoiceDayOfMonth { get; set; } = 1;
    /// <summary>
    /// Gets or sets the expense distribution.
    /// </summary>
    public string ExpenseDistribution { get; set; } = "equal";
    /// <summary>
    /// Gets or sets the voting quorum percent.
    /// </summary>
    public int VotingQuorumPercent { get; set; } = 50;
    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public string Currency { get; set; } = "EGP";
    /// <summary>
    /// Gets or sets the updated date and time.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    /// <summary>
    /// Gets or sets the updated by.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the building.
    /// </summary>
    public BuildingEntity? Building { get; set; }
}

/// <summary>
/// Database entity for a floor of a building.
/// </summary>
public class FloorEntity
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    public string Label { get; set; } = "";
    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the building.
    /// </summary>
    public BuildingEntity? Building { get; set; }
    /// <summary>
    /// Gets or sets the apartments.
    /// </summary>
    public List<ApartmentEntity> Apartments { get; set; } = new();
}

/// <summary>
/// Database entity for an apartment, including its fee, status and the resident notification settings.
/// </summary>
public class ApartmentEntity
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the floor.
    /// </summary>
    public string FloorId { get; set; } = "";
    /// <summary>
    /// Gets or sets the number.
    /// </summary>
    public int Number { get; set; }
    /// <summary>
    /// Gets or sets the owner.
    /// </summary>
    public string Owner { get; set; } = "";
    /// <summary>
    /// Gets or sets the phone.
    /// </summary>
    public string Phone { get; set; } = "";
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string Email { get; set; } = "";
    /// <summary>
    /// Gets or sets the monthly fee.
    /// </summary>
    public decimal MonthlyFee { get; set; }
    /// <summary>
    /// Gets or sets the PIN.
    /// </summary>
    public string Pin { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is closed.
    /// </summary>
    public bool IsClosed { get; set; }
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    public string Label { get; set; } = "";
    /// <summary>
    /// Gets or sets the notes.
    /// </summary>
    public string Notes { get; set; } = "";
    /// <summary>
    /// Gets or sets the open date.
    /// </summary>
    public DateOnly? OpenDate { get; set; }
    /// <summary>
    /// Gets or sets the close date.
    /// </summary>
    public DateOnly? CloseDate { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether it is disabled.
    /// </summary>
    public bool IsDisabled { get; set; }
    /// <summary>
    /// Gets or sets the disabled reason.
    /// </summary>
    public string DisabledReason { get; set; } = "";

    // إعدادات الساكن (UserSettingsDoc)
    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    public string Language { get; set; } = "ar";
    /// <summary>
    /// Gets or sets a value indicating whether notify whats app is enabled.
    /// </summary>
    public bool NotifyWhatsApp { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether notify email is enabled.
    /// </summary>
    public bool NotifyEmail { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether notify in app is enabled.
    /// </summary>
    public bool NotifyInApp { get; set; } = true;
    /// <summary>
    /// Gets or sets the preferred payment method.
    /// </summary>
    public string PreferredPaymentMethod { get; set; } = "instapay";
    /// <summary>
    /// Gets or sets the settings updated date and time.
    /// </summary>
    public DateTime? SettingsUpdatedAt { get; set; }
    /// <summary>
    /// Gets or sets the settings updated by.
    /// </summary>
    public string? SettingsUpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the building.
    /// </summary>
    public BuildingEntity? Building { get; set; }
    /// <summary>
    /// Gets or sets the floor.
    /// </summary>
    public FloorEntity? Floor { get; set; }
}

// ----------------------------- المالية -----------------------------
/// <summary>
/// Database entity for an expense or revenue category.
/// </summary>
public class FinancialCategoryEntity
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the kind.
    /// </summary>
    public string Kind { get; set; } = "expense";        // expense | revenue
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the color.
    /// </summary>
    public string Color { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the building.
    /// </summary>
    public BuildingEntity? Building { get; set; }
}

/// <summary>
/// Database entity for a monthly expense.
/// </summary>
public class ExpenseEntity
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the month key.
    /// </summary>
    public string MonthKey { get; set; } = "";           // YYYY-MM
    /// <summary>
    /// Gets or sets the identifier of the category.
    /// </summary>
    public string? CategoryId { get; set; }
    /// <summary>
    /// Gets or sets the category kind.
    /// </summary>
    public string CategoryKind { get; set; } = "expense";
    /// <summary>
    /// Gets or sets the note.
    /// </summary>
    public string Note { get; set; } = "";
    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Gets or sets the expense date.
    /// </summary>
    public DateOnly ExpenseDate { get; set; }
    /// <summary>
    /// Gets or sets the receipt URL.
    /// </summary>
    public string? ReceiptUrl { get; set; }

    /// <summary>
    /// Gets or sets the category.
    /// </summary>
    public FinancialCategoryEntity? Category { get; set; }
}

/// <summary>
/// Database entity for a monthly revenue.
/// </summary>
public class RevenueEntity
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the month key.
    /// </summary>
    public string MonthKey { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the category.
    /// </summary>
    public string? CategoryId { get; set; }
    /// <summary>
    /// Gets or sets the category kind.
    /// </summary>
    public string CategoryKind { get; set; } = "revenue";
    /// <summary>
    /// Gets or sets the note.
    /// </summary>
    public string Note { get; set; } = "";
    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Gets or sets the revenue date.
    /// </summary>
    public DateOnly RevenueDate { get; set; }

    /// <summary>
    /// Gets or sets the category.
    /// </summary>
    public FinancialCategoryEntity? Category { get; set; }
}

/// <summary>
/// An apartment's share of one month's expenses/revenues together with the balance carried over from the previous month.
/// </summary>
public class MonthlyApartmentShareEntity
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the month key.
    /// </summary>
    public string MonthKey { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    public string ApartmentId { get; set; } = "";
    /// <summary>
    /// Gets or sets the expense share.
    /// </summary>
    public decimal ExpenseShare { get; set; }
    /// <summary>
    /// Gets or sets the revenue share.
    /// </summary>
    public decimal RevenueShare { get; set; }
    /// <summary>
    /// Gets or sets the carry over.
    /// </summary>
    public decimal CarryOver { get; set; }

    /// <summary>
    /// Gets or sets the apartment.
    /// </summary>
    public ApartmentEntity? Apartment { get; set; }
}

/// <summary>
/// Database entity for a payment (deposit) submitted for an apartment, with its status (pending, confirmed or cancelled).
/// </summary>
public class DepositEntity
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the month key.
    /// </summary>
    public string MonthKey { get; set; } = "";
    /// <summary>
    /// Gets or sets the number.
    /// </summary>
    public string Number { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    public string ApartmentId { get; set; } = "";
    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Gets or sets the note.
    /// </summary>
    public string Note { get; set; } = "";
    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = "pending";      // pending | confirmed | cancelled

    /// <summary>
    /// Gets or sets the receipt URL.
    /// </summary>
    public string? ReceiptUrl { get; set; }
    /// <summary>
    /// Gets or sets the receipt path.
    /// </summary>
    public string? ReceiptPath { get; set; }
    /// <summary>
    /// Gets or sets the receipt file name.
    /// </summary>
    public string? ReceiptFileName { get; set; }
    /// <summary>
    /// Gets or sets the receipt file size.
    /// </summary>
    public long? ReceiptFileSize { get; set; }
    /// <summary>
    /// Gets or sets the receipt file type.
    /// </summary>
    public string? ReceiptFileType { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether it is receipt success.
    /// </summary>
    public bool? ReceiptSuccess { get; set; }

    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the created by.
    /// </summary>
    public string CreatedBy { get; set; } = "";
    /// <summary>
    /// Gets or sets the confirmed date and time.
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }
    /// <summary>
    /// Gets or sets the confirmed by.
    /// </summary>
    public string? ConfirmedBy { get; set; }
    /// <summary>
    /// Gets or sets the cancelled date and time.
    /// </summary>
    public DateTime? CancelledAt { get; set; }
    /// <summary>
    /// Gets or sets the cancelled by.
    /// </summary>
    public string? CancelledBy { get; set; }
    /// <summary>
    /// Gets or sets the cancelled reason.
    /// </summary>
    public string? CancelledReason { get; set; }
    /// <summary>
    /// Gets or sets the updated date and time.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    /// <summary>
    /// Gets or sets the updated by.
    /// </summary>
    public string? UpdatedBy { get; set; }
    /// <summary>
    /// Gets or sets the update reason.
    /// </summary>
    public string? UpdateReason { get; set; }

    /// <summary>
    /// Gets or sets the apartment.
    /// </summary>
    public ApartmentEntity? Apartment { get; set; }
    /// <summary>
    /// Gets or sets the edit history.
    /// </summary>
    public List<DepositEditEntity> EditHistory { get; set; } = new();
}

/// <summary>
/// Edit history entry of a deposit (old/new amount, reason, who and when).
/// </summary>
public class DepositEditEntity
{
    /// <summary>
    /// Gets or sets the identifier of the edit.
    /// </summary>
    public long EditId { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the deposit.
    /// </summary>
    public string DepositId { get; set; } = "";
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Ts { get; set; }
    /// <summary>
    /// Gets or sets the by user.
    /// </summary>
    public string ByUser { get; set; } = "";
    /// <summary>
    /// Gets or sets the reason.
    /// </summary>
    public string Reason { get; set; } = "";
    /// <summary>
    /// Gets or sets the old amount.
    /// </summary>
    public decimal OldAmount { get; set; }
    /// <summary>
    /// Gets or sets the new amount.
    /// </summary>
    public decimal NewAmount { get; set; }
    /// <summary>
    /// Gets or sets the old note.
    /// </summary>
    public string OldNote { get; set; } = "";
    /// <summary>
    /// Gets or sets the new note.
    /// </summary>
    public string NewNote { get; set; } = "";

    /// <summary>
    /// Gets or sets the deposit.
    /// </summary>
    public DepositEntity? Deposit { get; set; }
}

/// <summary>
/// Database entity for a manual wallet adjustment made by an admin.
/// </summary>
public class WalletAdjustmentEntity
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    public string ApartmentId { get; set; } = "";
    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }                  // موجب أو سالب
    /// <summary>
    /// Gets or sets the reason.
    /// </summary>
    public string Reason { get; set; } = "";
    /// <summary>
    /// Gets or sets the month key.
    /// </summary>
    public string MonthKey { get; set; } = "";
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the created by.
    /// </summary>
    public string CreatedBy { get; set; } = "";

    /// <summary>
    /// Gets or sets the apartment.
    /// </summary>
    public ApartmentEntity? Apartment { get; set; }
}

// ❌ محذوف: ReceiptEntity (مش مستخدم — الـ Receipt مدمج في DepositEntity)

// ----------------------------- السجل والإشعارات -----------------------------
/// <summary>
/// Database entity for an audit log entry.
/// </summary>
public class AuditLogEntity
{
    /// <summary>
    /// Gets or sets the identifier of the audit.
    /// </summary>
    public long AuditId { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the source.
    /// </summary>
    public string? SourceId { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string? BuildingId { get; set; }              // NULL = حدث على مستوى النظام
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Ts { get; set; }
    /// <summary>
    /// Gets or sets the action.
    /// </summary>
    public string Action { get; set; } = "";
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    public string Label { get; set; } = "";
    /// <summary>
    /// Gets or sets the actor.
    /// </summary>
    public string Actor { get; set; } = "";
    /// <summary>
    /// Gets or sets the actor role.
    /// </summary>
    public string ActorRole { get; set; } = "";
    /// <summary>
    /// Gets or sets the details.
    /// </summary>
    public string Details { get; set; } = "";
    /// <summary>
    /// Gets or sets the month key.
    /// </summary>
    public string MonthKey { get; set; } = "";
    /// <summary>
    /// Gets or sets the building number.
    /// </summary>
    public string? BuildingNumber { get; set; }
    /// <summary>
    /// Gets or sets the apartment number.
    /// </summary>
    public int? AptNumber { get; set; }
    /// <summary>Stable apartment identifier for strict tenant isolation.</summary>
    public string? ApartmentId { get; set; }
    /// <summary>Severity of the audit event.</summary>
    public string Severity { get; set; } = "info";
}

/// <summary>
/// Database entity for an in-app notification.
/// </summary>
public class NotificationEntity
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Ts { get; set; }
    /// <summary>
    /// Gets or sets the recipient type.
    /// </summary>
    public string RecipientType { get; set; } = "";      // admin | resident
    /// <summary>
    /// Gets or sets the identifier of the recipient apartment.
    /// </summary>
    public string? RecipientApartmentId { get; set; }
    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public string Type { get; set; } = "info";
    /// <summary>
    /// Gets or sets the icon.
    /// </summary>
    public string Icon { get; set; } = "🔔";
    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string Title { get; set; } = "";
    /// <summary>
    /// Gets or sets the body.
    /// </summary>
    public string Body { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is read.
    /// </summary>
    public bool IsRead { get; set; }
    /// <summary>
    /// Gets or sets the read date and time.
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Gets or sets the recipient apartment.
    /// </summary>
    public ApartmentEntity? RecipientApartment { get; set; }
}

// ----------------------------- التصويت -----------------------------
/// <summary>
/// Database entity for a poll.
/// </summary>
public class PollEntity
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the question.
    /// </summary>
    public string Question { get; set; } = "";
    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = "";
    /// <summary>
    /// Gets or sets the created by.
    /// </summary>
    public string CreatedBy { get; set; } = "";
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the deadline.
    /// </summary>
    public DateTime? Deadline { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether it is closed.
    /// </summary>
    public bool IsClosed { get; set; }

    /// <summary>
    /// Gets or sets the options.
    /// </summary>
    public List<PollOptionEntity> Options { get; set; } = new();
}

/// <summary>
/// Database entity for one option of a poll.
/// </summary>
public class PollOptionEntity
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the poll.
    /// </summary>
    public string PollId { get; set; } = "";
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    public string Text { get; set; } = "";
    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the poll.
    /// </summary>
    public PollEntity? Poll { get; set; }

}

/// <summary>
/// Database entity for a vote (one per apartment per poll).
/// </summary>
public class PollVoteEntity
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the poll.
    /// </summary>
    public string PollId { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    public string ApartmentId { get; set; } = "";        // صوت واحد لكل شقة
    /// <summary>
    /// Gets or sets the identifier of the option.
    /// </summary>
    public string OptionId { get; set; } = "";
    /// <summary>
    /// Gets or sets the voted date and time.
    /// </summary>
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the option.
    /// </summary>
    public PollOptionEntity? Option { get; set; }
    /// <summary>
    /// Gets or sets the apartment.
    /// </summary>
    public ApartmentEntity? Apartment { get; set; }
}

// ----------------------------- الصيانة -----------------------------
/// <summary>
/// Database entity for a maintenance record.
/// </summary>
public class MaintenanceRecordEntity
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string Title { get; set; } = "";
    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = "";
    /// <summary>
    /// Gets or sets the maintenance date.
    /// </summary>
    public DateOnly MaintenanceDate { get; set; }
    /// <summary>
    /// Gets or sets the cost.
    /// </summary>
    public decimal Cost { get; set; }
    /// <summary>
    /// Gets or sets the vendor.
    /// </summary>
    public string Vendor { get; set; } = "";
    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = "open";         // open | inprogress | done | cancelled
    /// <summary>
    /// Gets or sets the created by.
    /// </summary>
    public string CreatedBy { get; set; } = "";
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets a value indicating whether it is added to expense.
    /// </summary>
    public bool AddedToExpense { get; set; }
    /// <summary>
    /// Gets or sets the added to expense date and time.
    /// </summary>
    public DateTime? AddedToExpenseAt { get; set; }
}

// ----------------------------- QR والتواجد -----------------------------
/// <summary>
/// Database entity for a stored QR access token.
/// </summary>
public class QrTokenEntity
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string Uid { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    public string ApartmentId { get; set; } = "";
    /// <summary>
    /// Gets or sets the expires date and time.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    /// <summary>
    /// Gets or sets the use type.
    /// </summary>
    public string UseType { get; set; } = "single";      // single | multi
    /// <summary>
    /// Gets or sets the issued date and time.
    /// </summary>
    public DateTime IssuedAt { get; set; }
    /// <summary>
    /// Gets or sets the created by.
    /// </summary>
    public string CreatedBy { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is used.
    /// </summary>
    public bool IsUsed { get; set; }
    /// <summary>
    /// Gets or sets the used date and time.
    /// </summary>
    public DateTime? UsedAt { get; set; }
    /// <summary>
    /// Gets or sets the used by.
    /// </summary>
    public string? UsedBy { get; set; }
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the apartment.
    /// </summary>
    public ApartmentEntity? Apartment { get; set; }
}

// ✅ جديد: QrUsage — سجل استخدام توكنات QR
/// <summary>
/// Usage counter of a QR token, used to enforce one-time and limited-use tokens.
/// </summary>
public class QrUsageEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";                 // Firestore doc id
    /// <summary>
    /// Gets or sets the identifier of the token.
    /// </summary>
    public string TokenId { get; set; } = "";            // qr-tokens/{uid}
    /// <summary>
    /// Gets or sets the use count.
    /// </summary>
    public int UseCount { get; set; }
    /// <summary>
    /// Gets or sets the first used date and time.
    /// </summary>
    public string FirstUsedAt { get; set; } = "";
    /// <summary>
    /// Gets or sets the last used date and time.
    /// </summary>
    public string LastUsedAt { get; set; } = "";
    /// <summary>
    /// Gets or sets the device fingerprint.
    /// </summary>
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Database entity for the last-seen presence of a user.
/// </summary>
public class PresenceEntity
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string Uid { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string? BuildingId { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    public string? ApartmentId { get; set; }
    /// <summary>
    /// Gets or sets the last seen.
    /// </summary>
    public DateTime LastSeen { get; set; }
}

// ----------------------------- إعدادات النظام والنسخ الاحتياطي -----------------------------
/// <summary>
/// Database entity for application-wide settings.
/// </summary>
public class GlobalSettingsEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "global";
    /// <summary>
    /// Gets or sets the app name.
    /// </summary>
    public string AppName { get; set; } = "نظام إدارة العمارات";
    /// <summary>
    /// Gets or sets the default language.
    /// </summary>
    public string DefaultLanguage { get; set; } = "ar";
    /// <summary>
    /// Gets or sets the WhatsApp API token.
    /// </summary>
    public string WhatsappApiToken { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the WhatsApp phone.
    /// </summary>
    public string WhatsappPhoneId { get; set; } = "";
    /// <summary>
    /// Gets or sets the cloudinary cloud name.
    /// </summary>
    public string CloudinaryCloudName { get; set; } = "";
    /// <summary>
    /// Gets or sets the cloudinary API key.
    /// </summary>
    public string CloudinaryApiKey { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether enable polls is enabled.
    /// </summary>
    public bool EnablePolls { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether enable maintenance is enabled.
    /// </summary>
    public bool EnableMaintenance { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether enable OCR is enabled.
    /// </summary>
    public bool EnableOcr { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether enable QR access is enabled.
    /// </summary>
    public bool EnableQrAccess { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether enable whats app is enabled.
    /// </summary>
    public bool EnableWhatsApp { get; set; } = true;
    /// <summary>
    /// Gets or sets the session timeout days.
    /// </summary>
    public int SessionTimeoutDays { get; set; } = 7;
    /// <summary>
    /// Gets or sets the max PIN attempts.
    /// </summary>
    public int MaxPinAttempts { get; set; } = 5;
    /// <summary>
    /// Gets or sets the updated date and time.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    /// <summary>
    /// Gets or sets the updated by.
    /// </summary>
    public string? UpdatedBy { get; set; }
    /// <summary>
    /// Gets or sets the storage mode.
    /// </summary>
    public string StorageMode { get; set; } = "Dual";
}

/// <summary>
/// Database entity for the scheduled-backup configuration.
/// </summary>
public class ScheduledBackupSettingsEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "default";
    /// <summary>
    /// Gets or sets a value indicating whether it is enabled.
    /// </summary>
    public bool Enabled { get; set; }
    /// <summary>
    /// Gets or sets the frequency.
    /// </summary>
    public string Frequency { get; set; } = "daily";
    /// <summary>
    /// Gets or sets the hour.
    /// </summary>
    public int Hour { get; set; } = 2;
    /// <summary>
    /// Gets or sets the day of week.
    /// </summary>
    public int? DayOfWeek { get; set; }
    /// <summary>
    /// Gets or sets the day of month.
    /// </summary>
    public int? DayOfMonth { get; set; }
    /// <summary>
    /// Gets or sets the collections JSON.
    /// </summary>
    public string CollectionsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the max backups to keep.
    /// </summary>
    public int MaxBackupsToKeep { get; set; } = 7;
    /// <summary>
    /// Gets or sets the last run date and time.
    /// </summary>
    public DateTime? LastRunAt { get; set; }
    /// <summary>
    /// Gets or sets the last run status.
    /// </summary>
    public string? LastRunStatus { get; set; }
    /// <summary>
    /// Gets or sets the updated date and time.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    /// <summary>
    /// Gets or sets the updated by.
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Database entity for one entry of the backup history.
/// </summary>
public class BackupHistoryEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = "";
    /// <summary>
    /// Gets or sets the file size.
    /// </summary>
    public long FileSize { get; set; }
    /// <summary>
    /// Gets or sets the collections JSON.
    /// </summary>
    public string CollectionsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the total documents.
    /// </summary>
    public int TotalDocuments { get; set; }
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the duration seconds.
    /// </summary>
    public double DurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = "success";
    /// <summary>
    /// Gets or sets the source.
    /// </summary>
    public string Source { get; set; } = "scheduled";
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

// ----------------------------- View (قراءة فقط) -----------------------------
/// <summary>
/// Keyless entity mapped to a database view that lists the wallet movements of apartments.
/// </summary>
public class ApartmentWalletMovementsView
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    public string ApartmentId { get; set; } = "";
    /// <summary>
    /// Gets or sets the apartment number.
    /// </summary>
    public int ApartmentNumber { get; set; }
    /// <summary>
    /// Gets or sets the confirmed deposits.
    /// </summary>
    public decimal ConfirmedDeposits { get; set; }
    /// <summary>
    /// Gets or sets the adjustments.
    /// </summary>
    public decimal Adjustments { get; set; }
    /// <summary>
    /// Gets or sets the net movements.
    /// </summary>
    public decimal NetMovements { get; set; }
}

/// <summary>
/// Pending change record used by the legacy synchronization tooling.
/// </summary>
public class SyncPendingChangeEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Gets or sets the entity type.
    /// </summary>
    public string EntityType { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the entity.
    /// </summary>
    public string EntityId { get; set; } = "";
    /// <summary>
    /// Gets or sets the operation.
    /// </summary>
    public string Operation { get; set; } = "";  // Insert | Update | Delete
    /// <summary>
    /// Gets or sets the payload.
    /// </summary>
    public string? Payload { get; set; }         // JSON
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets a value indicating whether it is applied.
    /// </summary>
    public bool Applied { get; set; }
    /// <summary>
    /// Gets or sets the applied date and time.
    /// </summary>
    public DateTime? AppliedAt { get; set; }
    /// <summary>
    /// Gets or sets the retry count.
    /// </summary>
    public int RetryCount { get; set; }
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Database entity for a DVR device (credentials are stored encrypted).
/// </summary>
public class DvrEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the IP address address.
    /// </summary>
    public string IpAddress { get; set; } = "";
    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    public int Port { get; set; } = 554;
    /// <summary>
    /// Gets or sets the brand.
    /// </summary>
    public string Brand { get; set; } = "";
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = "";
    /// <summary>
    /// Gets or sets the password encrypted.
    /// </summary>
    public string PasswordEncrypted { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated date and time.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the cameras.
    /// </summary>
    public List<CameraEntity> Cameras { get; set; } = new();
}

/// <summary>
/// Database entity for a camera attached to a DVR.
/// </summary>
public class CameraEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the DVR.
    /// </summary>
    public string DvrId { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the channel.
    /// </summary>
    public int Channel { get; set; }
    /// <summary>
    /// Gets or sets the RTSP path.
    /// </summary>
    public string RtspPath { get; set; } = "";
    /// <summary>
    /// Gets or sets the HLS URL.
    /// </summary>
    public string HlsUrl { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the DVR.
    /// </summary>
    public DvrEntity? Dvr { get; set; }
}
// =====================================================================
//  ResidentEntity — جدول الساكنين الجديد
//  بيفصل الساكن عن الشقة (Apartment) عشان يدعم:
//    - ساكنين متعددين في نفس الشقة
//    - ساكن واحد في أكتر من شقة
//    - Auth مستقل (مش Firebase)
// =====================================================================

/// <summary>
/// Database entity for a resident account linked to an apartment. The PIN is stored only as a BCrypt hash.
/// </summary>
public class ResidentEntity
{
    // ═══════════════════════════════════════════════════════════
    // 🔑 المفاتيح
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";              // GUID
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string Uid { get; set; } = "";              // UID داخلي (زي Firebase Auth)
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    public string ApartmentId { get; set; } = "";

    // ═══════════════════════════════════════════════════════════
    // 👤 بيانات شخصية
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the phone.
    /// </summary>
    public string Phone { get; set; } = "";             // موبايل
    /// <summary>
    /// Gets or sets the WhatsApp.
    /// </summary>
    public string Whatsapp { get; set; } = "";
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string Email { get; set; } = "";             // اختياري
    /// <summary>
    /// Gets or sets the photo URL.
    /// </summary>
    public string PhotoUrl { get; set; } = "";

    // ═══════════════════════════════════════════════════════════
    // 🔐 Auth (BCrypt)
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Gets or sets the PIN hash.
    /// </summary>
    public string PinHash { get; set; } = "";           // BCrypt hash للـ PIN (4 أرقام)
    /// <summary>
    /// Gets or sets the password hash.
    /// </summary>
    public string? PasswordHash { get; set; }            // (اختياري — للمستقبل)

    // ═══════════════════════════════════════════════════════════
    // 📊 حالة
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Gets or sets a value indicating whether it is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether it is disabled.
    /// </summary>
    public bool IsDisabled { get; set; }
    /// <summary>
    /// Gets or sets the disabled reason.
    /// </summary>
    public string DisabledReason { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is owner.
    /// </summary>
    public bool IsOwner { get; set; } = true;            // مالك ولا مستأجر
    /// <summary>
    /// Gets or sets a value indicating whether it is primary.
    /// </summary>
    public bool IsPrimary { get; set; } = true;          // الساكن الرئيسي للشقة

    // ═══════════════════════════════════════════════════════════
    // 📅 تواريخ
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Gets or sets the last login date and time.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the created by.
    /// </summary>
    public string? CreatedBy { get; set; }
    /// <summary>
    /// Gets or sets the updated date and time.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    /// <summary>
    /// Gets or sets the updated by.
    /// </summary>
    public string? UpdatedBy { get; set; }

    // ═══════════════════════════════════════════════════════════
    // 🔗 Navigation (لو محتاج)
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Gets or sets the apartment.
    /// </summary>
    public ApartmentEntity? Apartment { get; set; }
}

/// <summary>Building-scoped reusable WhatsApp message template.</summary>
public class SavedWhatsAppTemplateEntity
{
    public Guid Id { get; set; }
    public string BuildingId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedByUid { get; set; }
}
