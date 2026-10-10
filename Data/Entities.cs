// =====================================================================
//  EF Core entities مطابقة لسكربت BuildingManagement-Schema.sql
//  الأسماء بتنتهي بـ "Entity" عشان ماتتعارضش مع كلاسات Firestore
//  اللي في BuildingManagementMvc.Models (Building, Apartment, ...)
//  أثناء فترة الانتقال.
// =====================================================================
namespace BuildingManagementMvc.Data.Entities;

// ----------------------------- المستخدمين -----------------------------
public class UserEntity
{
    public string Uid { get; set; } = "";
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Whatsapp { get; set; } = "";
    public string Pin { get; set; } = "";
    public string PhotoUrl { get; set; } = "";
    public string Role { get; set; } = "";               // superadmin | admin | resident
    public bool IsDisabled { get; set; }
    public string DisabledReason { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public List<UserPermissionEntity> Permissions { get; set; } = new();
    public List<BuildingAdminEntity> BuildingAdmins { get; set; } = new();
}

public class UserPermissionEntity
{
    public string Uid { get; set; } = "";
    public string Permission { get; set; } = "";
    public UserEntity? User { get; set; }
}

// ----------------------------- العمارات -----------------------------
public class BuildingEntity
{
    public string Id { get; set; } = "";
    public string BuildingNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string AdminPin { get; set; } = "";
    public string DataVersion { get; set; } = "3.2";
    public string LogoUrl { get; set; } = "";
    public string AdminWhatsapp { get; set; } = "";
    public string PaymentLabel { get; set; } = "";
    public string PaymentAccountNumber { get; set; } = "";
    public string PaymentPhone { get; set; } = "";
    public string PaymentNotes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public BuildingSettingsEntity? Settings { get; set; }
    public List<BuildingAdminEntity> Admins { get; set; } = new();
    public List<FloorEntity> Floors { get; set; } = new();
    public List<ApartmentEntity> Apartments { get; set; } = new();
    public List<FinancialCategoryEntity> Categories { get; set; } = new();
}

public class BuildingSettingsEntity
{
    public string BuildingId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Address { get; set; } = "";
    public string WhatsappNumber { get; set; } = "";
    public int InvoiceDayOfMonth { get; set; } = 1;
    public string ExpenseDistribution { get; set; } = "equal";
    public int VotingQuorumPercent { get; set; } = 50;
    public string Currency { get; set; } = "EGP";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public BuildingEntity? Building { get; set; }
}

public class BuildingAdminEntity
{
    public string BuildingId { get; set; } = "";
    public string AdminUid { get; set; } = "";
    public BuildingEntity? Building { get; set; }
    public UserEntity? User { get; set; }
}

public class FloorEntity
{
    public string BuildingId { get; set; } = "";
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public int SortOrder { get; set; }

    public BuildingEntity? Building { get; set; }
    public List<ApartmentEntity> Apartments { get; set; } = new();
}

public class ApartmentEntity
{
    public string BuildingId { get; set; } = "";
    public string Id { get; set; } = "";
    public string FloorId { get; set; } = "";
    public int Number { get; set; }
    public string Owner { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public decimal MonthlyFee { get; set; }
    public string Pin { get; set; } = "";
    public bool IsClosed { get; set; }
    public string Label { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateOnly? OpenDate { get; set; }
    public DateOnly? CloseDate { get; set; }
    public bool IsDisabled { get; set; }
    public string DisabledReason { get; set; } = "";

    // إعدادات الساكن (UserSettingsDoc)
    public string Language { get; set; } = "ar";
    public bool NotifyWhatsApp { get; set; } = true;
    public bool NotifyEmail { get; set; }
    public bool NotifyInApp { get; set; } = true;
    public string PreferredPaymentMethod { get; set; } = "instapay";
    public DateTime? SettingsUpdatedAt { get; set; }
    public string? SettingsUpdatedBy { get; set; }

    public BuildingEntity? Building { get; set; }
    public FloorEntity? Floor { get; set; }
}

// ----------------------------- المالية -----------------------------
public class FinancialCategoryEntity
{
    public string BuildingId { get; set; } = "";
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "expense";        // expense | revenue
    public string Name { get; set; } = "";
    public string Color { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public BuildingEntity? Building { get; set; }
}

public class ExpenseEntity
{
    public string BuildingId { get; set; } = "";
    public string Id { get; set; } = "";
    public string MonthKey { get; set; } = "";           // YYYY-MM
    public string? CategoryId { get; set; }
    public string CategoryKind { get; set; } = "expense";
    public string Note { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public string? ReceiptUrl { get; set; }

    public FinancialCategoryEntity? Category { get; set; }
}

public class RevenueEntity
{
    public string BuildingId { get; set; } = "";
    public string Id { get; set; } = "";
    public string MonthKey { get; set; } = "";
    public string? CategoryId { get; set; }
    public string CategoryKind { get; set; } = "revenue";
    public string Note { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly RevenueDate { get; set; }

    public FinancialCategoryEntity? Category { get; set; }
}

public class MonthlyApartmentShareEntity
{
    public string BuildingId { get; set; } = "";
    public string MonthKey { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public decimal ExpenseShare { get; set; }
    public decimal RevenueShare { get; set; }
    public decimal CarryOver { get; set; }

    public ApartmentEntity? Apartment { get; set; }
}

public class DepositEntity
{
    public string BuildingId { get; set; } = "";
    public string Id { get; set; } = "";
    public string MonthKey { get; set; } = "";
    public string Number { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Note { get; set; } = "";
    public string Status { get; set; } = "pending";      // pending | confirmed | cancelled

    public string? ReceiptUrl { get; set; }
    public string? ReceiptPath { get; set; }
    public string? ReceiptFileName { get; set; }
    public long? ReceiptFileSize { get; set; }
    public string? ReceiptFileType { get; set; }
    public bool? ReceiptSuccess { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "";
    public DateTime? ConfirmedAt { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public string? CancelledReason { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public string? UpdateReason { get; set; }

    public ApartmentEntity? Apartment { get; set; }
    public List<DepositEditEntity> EditHistory { get; set; } = new();
}

public class DepositEditEntity
{
    public long EditId { get; set; }
    public string BuildingId { get; set; } = "";
    public string DepositId { get; set; } = "";
    public DateTime Ts { get; set; }
    public string ByUser { get; set; } = "";
    public string Reason { get; set; } = "";
    public decimal OldAmount { get; set; }
    public decimal NewAmount { get; set; }
    public string OldNote { get; set; } = "";
    public string NewNote { get; set; } = "";

    public DepositEntity? Deposit { get; set; }
}

public class WalletAdjustmentEntity
{
    public string BuildingId { get; set; } = "";
    public string Id { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public decimal Amount { get; set; }                  // موجب أو سالب
    public string Reason { get; set; } = "";
    public string MonthKey { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "";

    public ApartmentEntity? Apartment { get; set; }
}

// ❌ محذوف: ReceiptEntity (مش مستخدم — الـ Receipt مدمج في DepositEntity)

// ----------------------------- السجل والإشعارات -----------------------------
public class AuditLogEntity
{
    public long AuditId { get; set; }
    public string? SourceId { get; set; }
    public string? BuildingId { get; set; }              // NULL = حدث على مستوى النظام
    public DateTime Ts { get; set; }
    public string Action { get; set; } = "";
    public string Label { get; set; } = "";
    public string Actor { get; set; } = "";
    public string ActorRole { get; set; } = "";
    public string Details { get; set; } = "";
    public string MonthKey { get; set; } = "";
    public string? BuildingNumber { get; set; }
    public int? AptNumber { get; set; }
}

public class NotificationEntity
{
    public string BuildingId { get; set; } = "";
    public string Id { get; set; } = "";
    public DateTime Ts { get; set; }
    public string RecipientType { get; set; } = "";      // admin | resident
    public string? RecipientApartmentId { get; set; }
    public string Type { get; set; } = "info";
    public string Icon { get; set; } = "🔔";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public ApartmentEntity? RecipientApartment { get; set; }
}

// ----------------------------- التصويت -----------------------------
public class PollEntity
{
    public string BuildingId { get; set; } = "";
    public string Id { get; set; } = "";
    public string Question { get; set; } = "";
    public string Description { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? Deadline { get; set; }
    public bool IsClosed { get; set; }

    public List<PollOptionEntity> Options { get; set; } = new();
}

public class PollOptionEntity
{
    public string BuildingId { get; set; } = "";
    public string PollId { get; set; } = "";
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public int SortOrder { get; set; }

    public PollEntity? Poll { get; set; }
    public List<PollVoteEntity> Votes { get; set; } = new();
}

public class PollVoteEntity
{
    public string BuildingId { get; set; } = "";
    public string PollId { get; set; } = "";
    public string ApartmentId { get; set; } = "";        // صوت واحد لكل شقة
    public string OptionId { get; set; } = "";
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;

    public PollOptionEntity? Option { get; set; }
    public ApartmentEntity? Apartment { get; set; }
}

// ----------------------------- الصيانة -----------------------------
public class MaintenanceRecordEntity
{
    public string BuildingId { get; set; } = "";
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateOnly MaintenanceDate { get; set; }
    public decimal Cost { get; set; }
    public string Vendor { get; set; } = "";
    public string Status { get; set; } = "open";         // open | inprogress | done | cancelled
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool AddedToExpense { get; set; }
    public DateTime? AddedToExpenseAt { get; set; }
}

// ----------------------------- QR والتواجد -----------------------------
public class QrTokenEntity
{
    public string Uid { get; set; } = "";
    public string BuildingId { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public string UseType { get; set; } = "single";      // single | multi
    public DateTime IssuedAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? UsedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApartmentEntity? Apartment { get; set; }
}

// ✅ جديد: QrUsage — سجل استخدام توكنات QR
public class QrUsageEntity
{
    public string Id { get; set; } = "";                 // Firestore doc id
    public string TokenId { get; set; } = "";            // qr-tokens/{uid}
    public int UseCount { get; set; }
    public string FirstUsedAt { get; set; } = "";
    public string LastUsedAt { get; set; } = "";
    public string? DeviceFingerprint { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PresenceEntity
{
    public string Uid { get; set; } = "";
    public string? BuildingId { get; set; }
    public string? ApartmentId { get; set; }
    public DateTime LastSeen { get; set; }
}

// ----------------------------- إعدادات النظام والنسخ الاحتياطي -----------------------------
public class GlobalSettingsEntity
{
    public string Id { get; set; } = "global";
    public string AppName { get; set; } = "نظام إدارة العمارات";
    public string DefaultLanguage { get; set; } = "ar";
    public string WhatsappApiToken { get; set; } = "";
    public string WhatsappPhoneId { get; set; } = "";
    public string CloudinaryCloudName { get; set; } = "";
    public string CloudinaryApiKey { get; set; } = "";
    public string FirebaseProjectId { get; set; } = "";
    public bool EnablePolls { get; set; } = true;
    public bool EnableMaintenance { get; set; } = true;
    public bool EnableOcr { get; set; } = true;
    public bool EnableQrAccess { get; set; } = true;
    public bool EnableWhatsApp { get; set; } = true;
    public int SessionTimeoutDays { get; set; } = 7;
    public int MaxPinAttempts { get; set; } = 5;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public string StorageMode { get; set; } = "Dual";
}

public class ScheduledBackupSettingsEntity
{
    public string Id { get; set; } = "default";
    public bool Enabled { get; set; }
    public string Frequency { get; set; } = "daily";
    public int Hour { get; set; } = 2;
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public string CollectionsJson { get; set; } = "[]";
    public int MaxBackupsToKeep { get; set; } = 7;
    public DateTime? LastRunAt { get; set; }
    public string? LastRunStatus { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class BackupHistoryEntity
{
    public string Id { get; set; } = "";
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public string CollectionsJson { get; set; } = "[]";
    public int TotalDocuments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public double DurationSeconds { get; set; }
    public string Status { get; set; } = "success";
    public string Source { get; set; } = "scheduled";
    public string? ErrorMessage { get; set; }
}

// ----------------------------- View (قراءة فقط) -----------------------------
public class ApartmentWalletMovementsView
{
    public string BuildingId { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public int ApartmentNumber { get; set; }
    public decimal ConfirmedDeposits { get; set; }
    public decimal Adjustments { get; set; }
    public decimal NetMovements { get; set; }
}

public class SyncPendingChangeEntity
{
    public long Id { get; set; }
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Operation { get; set; } = "";  // Insert | Update | Delete
    public string? Payload { get; set; }         // JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Applied { get; set; }
    public DateTime? AppliedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class DvrEntity
{
    public string Id { get; set; } = "";
    public string BuildingId { get; set; } = "";
    public string Name { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public int Port { get; set; } = 554;
    public string Brand { get; set; } = "";
    public string Username { get; set; } = "";
    public string PasswordEncrypted { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<CameraEntity> Cameras { get; set; } = new();
}

public class CameraEntity
{
    public string Id { get; set; } = "";
    public string DvrId { get; set; } = "";
    public string BuildingId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Channel { get; set; }
    public string RtspPath { get; set; } = "";
    public string HlsUrl { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DvrEntity? Dvr { get; set; }
}
// =====================================================================
//  ResidentEntity — جدول الساكنين الجديد
//  بيفصل الساكن عن الشقة (Apartment) عشان يدعم:
//    - ساكنين متعددين في نفس الشقة
//    - ساكن واحد في أكتر من شقة
//    - Auth مستقل (مش Firebase)
// =====================================================================

public class ResidentEntity
{
    // ═══════════════════════════════════════════════════════════
    // 🔑 المفاتيح
    // ═══════════════════════════════════════════════════════════
    public string Id { get; set; } = "";              // GUID
    public string Uid { get; set; } = "";              // UID داخلي (زي Firebase Auth)
    public string BuildingId { get; set; } = "";
    public string ApartmentId { get; set; } = "";

    // ═══════════════════════════════════════════════════════════
    // 👤 بيانات شخصية
    // ═══════════════════════════════════════════════════════════
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";             // موبايل
    public string Whatsapp { get; set; } = "";
    public string Email { get; set; } = "";             // اختياري
    public string PhotoUrl { get; set; } = "";

    // ═══════════════════════════════════════════════════════════
    // 🔐 Auth (BCrypt)
    // ═══════════════════════════════════════════════════════════
    public string PinHash { get; set; } = "";           // BCrypt hash للـ PIN (4 أرقام)
    public string? PasswordHash { get; set; }            // (اختياري — للمستقبل)

    // ═══════════════════════════════════════════════════════════
    // 📊 حالة
    // ═══════════════════════════════════════════════════════════
    public bool IsActive { get; set; } = true;
    public bool IsDisabled { get; set; }
    public string DisabledReason { get; set; } = "";
    public bool IsOwner { get; set; } = true;            // مالك ولا مستأجر
    public bool IsPrimary { get; set; } = true;          // الساكن الرئيسي للشقة

    // ═══════════════════════════════════════════════════════════
    // 📅 تواريخ
    // ═══════════════════════════════════════════════════════════
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // ═══════════════════════════════════════════════════════════
    // 🔗 Navigation (لو محتاج)
    // ═══════════════════════════════════════════════════════════
    public ApartmentEntity? Apartment { get; set; }
}