using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Models;

// ============================================================
// Global Settings (Super Admin)
// ============================================================
[FirestoreData]
public class GlobalSettings
{
    [FirestoreDocumentId]
    public string Id { get; set; } = "global";

    [FirestoreProperty("appName")]
    public string AppName { get; set; } = "نظام إدارة العمارات";

    [FirestoreProperty("defaultLanguage")]
    public string DefaultLanguage { get; set; } = "ar";

    [FirestoreProperty("whatsappApiToken")]
    public string WhatsappApiToken { get; set; } = "";

    [FirestoreProperty("whatsappPhoneId")]
    public string WhatsappPhoneId { get; set; } = "";

    [FirestoreProperty("cloudinaryCloudName")]
    public string CloudinaryCloudName { get; set; } = "";

    [FirestoreProperty("cloudinaryApiKey")]
    public string CloudinaryApiKey { get; set; } = "";

    [FirestoreProperty("firebaseProjectId")]
    public string FirebaseProjectId { get; set; } = "";

    [FirestoreProperty("featureFlags")]
    public FeatureFlags FeatureFlags { get; set; } = new();

    [FirestoreProperty("security")]
    public SecuritySettings Security { get; set; } = new();

    [FirestoreProperty("updatedAt")]
    public string? UpdatedAt { get; set; }

    [FirestoreProperty("updatedBy")]
    public string? UpdatedBy { get; set; }
}

[FirestoreData]
public class FeatureFlags
{
    [FirestoreProperty("enablePolls")] public bool EnablePolls { get; set; } = true;
    [FirestoreProperty("enableMaintenance")] public bool EnableMaintenance { get; set; } = true;
    [FirestoreProperty("enableOcr")] public bool EnableOcr { get; set; } = true;
    [FirestoreProperty("enableQrAccess")] public bool EnableQrAccess { get; set; } = true;
    [FirestoreProperty("enableWhatsApp")] public bool EnableWhatsApp { get; set; } = true;
}

[FirestoreData]
public class SecuritySettings
{
    [FirestoreProperty("sessionTimeoutDays")] public int SessionTimeoutDays { get; set; } = 7;
    [FirestoreProperty("maxPinAttempts")] public int MaxPinAttempts { get; set; } = 5;
}

// ============================================================
// Building Settings (Admin)
// ============================================================
[FirestoreData]
public class BuildingSettings
{
    [FirestoreProperty("displayName")]
    public string DisplayName { get; set; } = "";

    [FirestoreProperty("address")]
    public string Address { get; set; } = "";

    [FirestoreProperty("whatsappNumber")]
    public string WhatsappNumber { get; set; } = "";

    [FirestoreProperty("invoiceDayOfMonth")]
    public int InvoiceDayOfMonth { get; set; } = 1;

    [FirestoreProperty("expenseDistribution")]
    public string ExpenseDistribution { get; set; } = "equal";

    [FirestoreProperty("votingQuorumPercent")]
    public int VotingQuorumPercent { get; set; } = 50;

    [FirestoreProperty("currency")]
    public string Currency { get; set; } = "EGP";

    [FirestoreProperty("updatedAt")]
    public string? UpdatedAt { get; set; }

    [FirestoreProperty("updatedBy")]
    public string? UpdatedBy { get; set; }
}

// ============================================================
// ✅ جديد: User Settings Doc (بيتخزن جوه users/{uid}.settings)
// ============================================================
[FirestoreData]
public class UserSettingsDoc
{
    [FirestoreProperty("language")]
    public string Language { get; set; } = "ar";

    [FirestoreProperty("notificationPreferences")]
    public NotificationPreferences NotificationPreferences { get; set; } = new();

    [FirestoreProperty("preferredPaymentMethod")]
    public string PreferredPaymentMethod { get; set; } = "instapay";

    [FirestoreProperty("updatedAt")]
    public string? UpdatedAt { get; set; }

    [FirestoreProperty("updatedBy")]
    public string? UpdatedBy { get; set; }
}

// ============================================================
// Own Settings — ViewModel اللي بيُستخدم في الـ View والـ Controller
// ============================================================
public class OwnSettings
{
    public string Language { get; set; } = "ar";
    public NotificationPreferences NotificationPreferences { get; set; } = new();
    public string PreferredPaymentMethod { get; set; } = "instapay";
    public string? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

[FirestoreData]
public class NotificationPreferences
{
    [FirestoreProperty("whatsapp")] public bool WhatsApp { get; set; } = true;
    [FirestoreProperty("email")] public bool Email { get; set; } = false;
    [FirestoreProperty("inApp")] public bool InApp { get; set; } = true;
}