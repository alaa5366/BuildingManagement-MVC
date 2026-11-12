namespace BuildingManagementMvc.Models;

// ============================================================
// Global Settings (Super Admin)
// ============================================================
public class GlobalSettings
{
    public string Id { get; set; } = "global";
    public string AppName { get; set; } = "نظام إدارة العمارات";
    public string DefaultLanguage { get; set; } = "ar";
    public string WhatsappApiToken { get; set; } = "";
    public string WhatsappPhoneId { get; set; } = "";
    public string CloudinaryCloudName { get; set; } = "";
    public string CloudinaryApiKey { get; set; } = "";
    public FeatureFlags FeatureFlags { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public string? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class FeatureFlags
{
    public bool EnablePolls { get; set; } = true;
    public bool EnableMaintenance { get; set; } = true;
    public bool EnableOcr { get; set; } = true;
    public bool EnableQrAccess { get; set; } = true;
    public bool EnableWhatsApp { get; set; } = true;
}

public class SecuritySettings
{
    public int SessionTimeoutDays { get; set; } = 7;
    public int MaxPinAttempts { get; set; } = 5;
}

// ============================================================
// Building Settings (Admin)
// ============================================================
public class BuildingSettings
{
    public string DisplayName { get; set; } = "";
    public string Address { get; set; } = "";
    public string WhatsappNumber { get; set; } = "";
    public int InvoiceDayOfMonth { get; set; } = 1;
    public string ExpenseDistribution { get; set; } = "equal";
    public int VotingQuorumPercent { get; set; } = 50;
    public string Currency { get; set; } = "EGP";
    public string? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

// ============================================================
// User Settings Doc
// ============================================================
public class UserSettingsDoc
{
    public string Language { get; set; } = "ar";
    public NotificationPreferences NotificationPreferences { get; set; } = new();
    public string PreferredPaymentMethod { get; set; } = "instapay";
    public string? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

// ============================================================
// Own Settings
// ============================================================
public class OwnSettings
{
    public string Language { get; set; } = "ar";
    public NotificationPreferences NotificationPreferences { get; set; } = new();
    public string PreferredPaymentMethod { get; set; } = "instapay";
    public string? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class NotificationPreferences
{
    public bool WhatsApp { get; set; } = true;
    public bool Email { get; set; } = false;
    public bool InApp { get; set; } = true;
}