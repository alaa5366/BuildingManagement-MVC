namespace BuildingManagementMvc.Models;

// ============================================================
// Global Settings (Super Admin)
// ============================================================
/// <summary>
/// Application-wide settings (name, default language, features, security limits).
/// </summary>
public class GlobalSettings
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
    /// Gets or sets the feature flags.
    /// </summary>
    public FeatureFlags FeatureFlags { get; set; } = new();
    /// <summary>
    /// Gets or sets the security.
    /// </summary>
    public SecuritySettings Security { get; set; } = new();
    /// <summary>
    /// Gets or sets the updated date and time.
    /// </summary>
    public string? UpdatedAt { get; set; }
    /// <summary>
    /// Gets or sets the updated by.
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Switches that turn optional features on or off.
/// </summary>
public class FeatureFlags
{
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
}

/// <summary>
/// Security-related limits such as session duration and maximum PIN attempts.
/// </summary>
public class SecuritySettings
{
    /// <summary>
    /// Gets or sets the session timeout days.
    /// </summary>
    public int SessionTimeoutDays { get; set; } = 7;
    /// <summary>
    /// Gets or sets the max PIN attempts.
    /// </summary>
    public int MaxPinAttempts { get; set; } = 5;
}

// ============================================================
// Building Settings (Admin)
// ============================================================
/// <summary>
/// Per-building settings such as the invoice policy, expense distribution and voting quorum.
/// </summary>
public class BuildingSettings
{
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
    public string? UpdatedAt { get; set; }
    /// <summary>
    /// Gets or sets the updated by.
    /// </summary>
    public string? UpdatedBy { get; set; }
}

// ============================================================
// User Settings Doc
// ============================================================
/// <summary>
/// A resident's personal settings (language, notification channels, payment method).
/// </summary>
public class UserSettingsDoc
{
    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    public string Language { get; set; } = "ar";
    /// <summary>
    /// Gets or sets the notification preferences.
    /// </summary>
    public NotificationPreferences NotificationPreferences { get; set; } = new();
    /// <summary>
    /// Gets or sets the preferred payment method.
    /// </summary>
    public string PreferredPaymentMethod { get; set; } = "instapay";
    /// <summary>
    /// Gets or sets the updated date and time.
    /// </summary>
    public string? UpdatedAt { get; set; }
    /// <summary>
    /// Gets or sets the updated by.
    /// </summary>
    public string? UpdatedBy { get; set; }
}

// ============================================================
// Own Settings
// ============================================================
/// <summary>
/// The signed-in user's own settings page model (profile, PIN change and preferences).
/// </summary>
public class OwnSettings
{
    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    public string Language { get; set; } = "ar";
    /// <summary>
    /// Gets or sets the notification preferences.
    /// </summary>
    public NotificationPreferences NotificationPreferences { get; set; } = new();
    /// <summary>
    /// Gets or sets the preferred payment method.
    /// </summary>
    public string PreferredPaymentMethod { get; set; } = "instapay";
    /// <summary>
    /// Gets or sets the updated date and time.
    /// </summary>
    public string? UpdatedAt { get; set; }
    /// <summary>
    /// Gets or sets the updated by.
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Which notification channels (WhatsApp, e-mail, in-app) a user wants.
/// </summary>
public class NotificationPreferences
{
    /// <summary>
    /// Gets or sets a value indicating whether it is whats app.
    /// </summary>
    public bool WhatsApp { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether it is email.
    /// </summary>
    public bool Email { get; set; } = false;
    /// <summary>
    /// Gets or sets a value indicating whether it is in app.
    /// </summary>
    public bool InApp { get; set; } = true;
}