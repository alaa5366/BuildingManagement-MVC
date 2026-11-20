using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Business logic for personal, building and global settings.
/// </summary>
public class SettingsService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly IAuditLogger _audit;
    private readonly BuildingsService _buildings;
    private readonly IEncryptionService _encryption;
    private readonly IPasswordHasher _hasher;
    private readonly SessionRevalidator? _sessionRevalidator;
    private readonly ILogger<SettingsService> _logger;
    private const string GlobalId = "global";
    private const string GlobalSecretScope = "global";
    private const string GlobalSecretKeyVersion = "application-secret-v1";

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class.
    /// </summary>
    public SettingsService(
        IDbContextFactory<AppDbContext> sqlFactory,
        IAuditLogger audit,
        BuildingsService buildings,
        IEncryptionService encryption,
        IPasswordHasher hasher,
        ILogger<SettingsService> logger,
        SessionRevalidator? sessionRevalidator = null)
    {
        _sqlFactory = sqlFactory;
        _audit = audit;
        _buildings = buildings;
        _encryption = encryption;
        _hasher = hasher;
        _logger = logger;
        _sessionRevalidator = sessionRevalidator;
    }

    // ============================================================
    // Global Settings (الدوال الأصلية)
    // ============================================================
    /// <summary>Reads application settings from the canonical settings and protected-secret tables.</summary>
    public async Task<GlobalSettings> GetGlobalAsync()
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var entity = await db.GlobalApplicationSettings.AsNoTracking()
            .SingleOrDefaultAsync(item => item.SettingsId == GlobalId);
        if (entity is null)
        {
            entity = new GlobalApplicationSettingsEntity
            {
                SettingsId = GlobalId,
                AppName = "نظام إدارة العمارات",
                DefaultLanguage = "ar",
                EnablePolls = true,
                EnableMaintenance = true,
                EnableOcr = true,
                EnableQrAccess = true,
                EnableWhatsApp = true,
                SessionTimeoutDays = 7,
                MaxPinAttempts = 5,
                StorageMode = "local",
                ImportedAtUtc = DateTime.UtcNow
            };
            db.GlobalApplicationSettings.Add(entity);
            await db.SaveChangesAsync();
        }

        return new GlobalSettings
        {
            Id = entity.SettingsId,
            AppName = entity.AppName,
            DefaultLanguage = entity.DefaultLanguage,
            WhatsappApiToken = await ReadGlobalSecretAsync(db, "whatsapp", "ApiToken"),
            WhatsappPhoneId = await ReadGlobalSecretAsync(db, "whatsapp", "PhoneId"),
            CloudinaryCloudName = await ReadGlobalSecretAsync(db, "cloudinary", "CloudName"),
            CloudinaryApiKey = await ReadGlobalSecretAsync(db, "cloudinary", "ApiKey"),
            FeatureFlags = new FeatureFlags
            {
                EnablePolls = entity.EnablePolls,
                EnableMaintenance = entity.EnableMaintenance,
                EnableOcr = entity.EnableOcr,
                EnableQrAccess = entity.EnableQrAccess,
                EnableWhatsApp = entity.EnableWhatsApp
            },
            Security = new SecuritySettings
            {
                SessionTimeoutDays = entity.SessionTimeoutDays,
                MaxPinAttempts = entity.MaxPinAttempts
            },
            UpdatedAt = entity.UpdatedAtUtc?.ToString("o"),
            UpdatedBy = entity.UpdatedByReference
        };
    }

    /// <summary>Saves non-secret settings and encrypts integration credentials in their dedicated store.</summary>
    public async Task SaveGlobalAsync(GlobalSettings settings, string userId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var entity = await db.GlobalApplicationSettings
            .SingleOrDefaultAsync(item => item.SettingsId == GlobalId);
        if (entity is null)
        {
            entity = new GlobalApplicationSettingsEntity
            {
                SettingsId = GlobalId,
                ImportedAtUtc = DateTime.UtcNow
            };
            db.GlobalApplicationSettings.Add(entity);
        }

        var now = DateTime.UtcNow;
        entity.AppName = string.IsNullOrWhiteSpace(settings.AppName) ? "نظام إدارة العمارات" : settings.AppName.Trim();
        entity.DefaultLanguage = string.IsNullOrWhiteSpace(settings.DefaultLanguage) ? "ar" : settings.DefaultLanguage.Trim();
        entity.EnablePolls = settings.FeatureFlags?.EnablePolls ?? true;
        entity.EnableMaintenance = settings.FeatureFlags?.EnableMaintenance ?? true;
        entity.EnableOcr = settings.FeatureFlags?.EnableOcr ?? true;
        entity.EnableQrAccess = settings.FeatureFlags?.EnableQrAccess ?? true;
        entity.EnableWhatsApp = settings.FeatureFlags?.EnableWhatsApp ?? true;
        entity.SessionTimeoutDays = Math.Clamp(settings.Security?.SessionTimeoutDays ?? 7, 1, 365);
        entity.MaxPinAttempts = Math.Clamp(settings.Security?.MaxPinAttempts ?? 5, 1, 100);
        entity.UpdatedAtUtc = now;
        entity.UpdatedByReference = userId;

        await SaveGlobalSecretAsync(db, "whatsapp", "ApiToken", settings.WhatsappApiToken, now);
        await SaveGlobalSecretAsync(db, "whatsapp", "PhoneId", settings.WhatsappPhoneId, now);
        await SaveGlobalSecretAsync(db, "cloudinary", "CloudName", settings.CloudinaryCloudName, now);
        await SaveGlobalSecretAsync(db, "cloudinary", "ApiKey", settings.CloudinaryApiKey, now);
        await db.SaveChangesAsync();

        await _audit.LogAsync(
            action: "settings.global.update",
            userId: userId,
            userRole: "superadmin",
            metadata: new { appName = entity.AppName, defaultLanguage = entity.DefaultLanguage },
            severity: "warning");
    }

    private async Task<string> ReadGlobalSecretAsync(AppDbContext db, string provider, string secretName)
    {
        var secret = await db.ProtectedIntegrationSecrets.AsNoTracking().SingleOrDefaultAsync(item =>
            item.ScopeType == 0 && item.ScopeReference == GlobalSecretScope
            && item.Provider == provider && item.SecretName == secretName && item.RevokedAtUtc == null);
        if (secret is null || secret.Ciphertext.Length == 0) return "";
        try
        {
            return _encryption.Decrypt(System.Text.Encoding.UTF8.GetString(secret.Ciphertext));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to decrypt global integration secret {Provider}/{SecretName}", provider, secretName);
            return "";
        }
    }

    private async Task SaveGlobalSecretAsync(AppDbContext db, string provider, string secretName, string? value, DateTime now)
    {
        var secret = await db.ProtectedIntegrationSecrets.SingleOrDefaultAsync(item =>
            item.ScopeType == 0 && item.ScopeReference == GlobalSecretScope
            && item.Provider == provider && item.SecretName == secretName);
        var normalized = value?.Trim() ?? "";
        if (secret is null && normalized.Length == 0) return;
        if (secret is null)
        {
            secret = new ProtectedIntegrationSecretEntity
            {
                SecretId = Guid.NewGuid(),
                ScopeType = 0,
                ScopeReference = GlobalSecretScope,
                Provider = provider,
                SecretName = secretName,
                CreatedAtUtc = now
            };
            db.ProtectedIntegrationSecrets.Add(secret);
        }
        else if (secret.RevokedAtUtc is not null || normalized.Length > 0)
        {
            secret.RotatedAtUtc = now;
        }

        secret.Ciphertext = System.Text.Encoding.UTF8.GetBytes(_encryption.Encrypt(normalized));
        secret.KeyVersion = GlobalSecretKeyVersion;
        secret.RevokedAtUtc = normalized.Length == 0 ? now : null;
    }

    // ============================================================
    // Building Settings (الدوال الأصلية)
    // ============================================================
    /// <summary>
    /// Gets the building.
    /// </summary>
    public async Task<BuildingSettings> GetBuildingAsync(string buildingId)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var entity = await db.BuildingSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BuildingId == buildingId);

        if (entity == null)
        {
            var building = await _buildings.GetByIdAsync(buildingId);
            if (building == null) throw new InvalidOperationException("building-not-found");

            return new BuildingSettings
            {
                DisplayName = building.Name,
                WhatsappNumber = building.AdminWhatsapp
            };
        }

        return new BuildingSettings
        {
            DisplayName = entity.DisplayName,
            Address = entity.Address,
            WhatsappNumber = entity.WhatsappNumber,
            InvoiceDayOfMonth = entity.InvoiceDayOfMonth,
            ExpenseDistribution = entity.ExpenseDistribution,
            VotingQuorumPercent = entity.VotingQuorumPercent,
            Currency = entity.Currency,
            UpdatedAt = entity.UpdatedAt?.ToString("o"),
            UpdatedBy = entity.UpdatedBy
        };
    }

    /// <summary>
    /// Saves the building.
    /// </summary>
    public async Task SaveBuildingAsync(string buildingId, BuildingSettings settings, string userId, string userRole)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var entity = await db.BuildingSettings.FirstOrDefaultAsync(x => x.BuildingId == buildingId);
        if (entity == null)
        {
            entity = new Data.Entities.BuildingSettingsEntity { BuildingId = buildingId };
            db.BuildingSettings.Add(entity);
        }

        entity.DisplayName = settings.DisplayName ?? "";
        entity.Address = settings.Address ?? "";
        entity.WhatsappNumber = settings.WhatsappNumber ?? "";
        entity.InvoiceDayOfMonth = settings.InvoiceDayOfMonth;
        entity.ExpenseDistribution = settings.ExpenseDistribution ?? "equal";
        entity.VotingQuorumPercent = settings.VotingQuorumPercent;
        entity.Currency = settings.Currency ?? "EGP";
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;

        await db.SaveChangesAsync();

        await _audit.LogAsync(
            action: "settings.building.update",
            buildingId: buildingId,
            userId: userId,
            userRole: userRole,
            metadata: new { displayName = entity.DisplayName },
            severity: "warning");
    }

    // ============================================================
    // Own Settings (الدوال الأصلية)
    // ============================================================
    /// <summary>
    /// Gets the own.
    /// </summary>
    public async Task<OwnSettings> GetOwnAsync(string uid, string? role = null,
        string? buildingId = null, string? apartmentId = null)
    {
        if (!Guid.TryParse(uid, out var accountId)) return new OwnSettings();

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var identity = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            where account.AccountId == accountId && account.AccountStatus == 1 && !person.IsArchived
            select new
            {
                account.AccountStatus,
                account.MustChangePin,
                account.NormalizedEmail,
                account.ExternalProvider,
                account.CreatedAtUtc,
                account.LastLoginAtUtc,
                person.LegalName,
                person.DisplayName,
                person.PrimaryEmail,
                person.PrimaryPhone,
                person.Whatsapp,
                person.PhotoUrl
            }).SingleOrDefaultAsync();

        if (identity is null) return new OwnSettings();

        var preferences = await db.UserPreferences.AsNoTracking()
            .SingleOrDefaultAsync(item => item.AccountId == accountId && item.PreferenceKey == OwnPreferencesKey);
        OwnPreferencesDocument? savedPreferences = null;
        if (!string.IsNullOrWhiteSpace(preferences?.PreferenceJson))
        {
            try
            {
                savedPreferences = JsonSerializer.Deserialize<OwnPreferencesDocument>(preferences.PreferenceJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Ignoring invalid personal preferences for account {AccountId}", accountId);
            }
        }

        var apartment = role == "resident" && !string.IsNullOrWhiteSpace(buildingId)
            && !string.IsNullOrWhiteSpace(apartmentId)
            ? await db.Apartments.AsNoTracking().FirstOrDefaultAsync(item =>
                item.BuildingId == buildingId && item.Id == apartmentId)
            : null;

        var twoFactor = await db.UserTwoFactorSettings.AsNoTracking()
            .SingleOrDefaultAsync(item => item.AccountId == accountId);
        var unusedRecoveryCodes = await db.UserRecoveryCodes.AsNoTracking()
            .CountAsync(item => item.AccountId == accountId && item.UsedAtUtc == null);

        var legacyLanguage = apartment?.Language;
        var language = savedPreferences?.Language ?? legacyLanguage;
        if (language is not ("ar" or "en" or "fr" or "de")) language = "ar";

        return new OwnSettings
        {
            LegalName = identity.LegalName ?? "",
            DisplayName = identity.DisplayName,
            PrimaryEmail = identity.PrimaryEmail,
            PrimaryPhone = identity.PrimaryPhone,
            Whatsapp = identity.Whatsapp,
            PhotoUrl = identity.PhotoUrl,
            LoginEmail = identity.NormalizedEmail,
            ExternalProvider = identity.ExternalProvider,
            AccountStatus = identity.AccountStatus,
            MustChangePin = identity.MustChangePin,
            CreatedAtUtc = identity.CreatedAtUtc,
            LastLoginAtUtc = identity.LastLoginAtUtc,
            TwoFactorEnabled = twoFactor?.IsEnabled ?? false,
            TwoFactorEnrolledAtUtc = twoFactor?.EnrolledAtUtc,
            TwoFactorLastVerifiedAtUtc = twoFactor?.LastVerifiedAtUtc,
            UnusedRecoveryCodes = unusedRecoveryCodes,
            ApartmentLabel = apartment?.Label,
            ApartmentNumber = apartment?.Number,
            Language = language,
            NotificationPreferences = savedPreferences?.NotificationPreferences ?? new NotificationPreferences
            {
                WhatsApp = apartment?.NotifyWhatsApp ?? true,
                Email = apartment?.NotifyEmail ?? false,
                InApp = apartment?.NotifyInApp ?? true
            },
            PreferredPaymentMethod = savedPreferences?.PreferredPaymentMethod
                ?? (string.IsNullOrWhiteSpace(apartment?.PreferredPaymentMethod) ? "instapay" : apartment.PreferredPaymentMethod),
            UpdatedAt = preferences?.UpdatedAtUtc.ToString("o") ?? apartment?.SettingsUpdatedAt?.ToString("o"),
            UpdatedBy = apartment?.SettingsUpdatedBy
        };
    }

    private const string OwnPreferencesKey = "profile.settings.v1";

    private sealed class OwnPreferencesDocument
    {
        public string Language { get; set; } = "ar";
        public NotificationPreferences NotificationPreferences { get; set; } = new();
        public string PreferredPaymentMethod { get; set; } = "instapay";
    }

    /// <summary>
    /// Saves the own.
    /// </summary>
    public async Task SaveOwnAsync(string uid, OwnSettings settings, string userId, string userRole,
        string? buildingId = null, string? apartmentId = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!Guid.TryParse(uid, out var accountId))
            throw new InvalidOperationException("account-not-found");

        var language = (settings.Language ?? "ar").Trim().ToLowerInvariant();
        if (language is not ("ar" or "en" or "fr" or "de"))
            throw new InvalidOperationException("unsupported-language");

        var paymentMethod = (settings.PreferredPaymentMethod ?? "instapay").Trim().ToLowerInvariant();
        if (paymentMethod is not ("instapay" or "cash" or "bank" or "vodafone"))
            throw new InvalidOperationException("unsupported-payment-method");

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var accountExists = await db.UserAccounts.AnyAsync(item =>
            item.AccountId == accountId && item.AccountStatus == 1);
        if (!accountExists) throw new InvalidOperationException("account-not-found");

        var now = DateTime.UtcNow;
        var preference = await db.UserPreferences.SingleOrDefaultAsync(item =>
            item.AccountId == accountId && item.PreferenceKey == OwnPreferencesKey);
        if (preference is null)
        {
            preference = new UserPreferenceEntity
            {
                PreferenceId = Guid.NewGuid(),
                AccountId = accountId,
                PreferenceKey = OwnPreferencesKey
            };
            db.UserPreferences.Add(preference);
        }

        preference.PreferenceJson = JsonSerializer.Serialize(new OwnPreferencesDocument
        {
            Language = language,
            NotificationPreferences = new NotificationPreferences
            {
                WhatsApp = settings.NotificationPreferences?.WhatsApp ?? true,
                Email = settings.NotificationPreferences?.Email ?? false,
                InApp = settings.NotificationPreferences?.InApp ?? true
            },
            PreferredPaymentMethod = paymentMethod
        });
        preference.UpdatedAtUtc = now;

        if (userRole == "resident")
        {
            if (string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(apartmentId))
                throw new InvalidOperationException("apartment-access-denied");

            var apartment = await db.Apartments.FirstOrDefaultAsync(item =>
                item.BuildingId == buildingId && item.Id == apartmentId)
                ?? throw new InvalidOperationException("apartment-not-found");
            apartment.Language = language;
            apartment.NotifyWhatsApp = settings.NotificationPreferences?.WhatsApp ?? true;
            apartment.NotifyEmail = settings.NotificationPreferences?.Email ?? false;
            apartment.NotifyInApp = settings.NotificationPreferences?.InApp ?? true;
            apartment.PreferredPaymentMethod = paymentMethod;
            apartment.SettingsUpdatedAt = now;
            apartment.SettingsUpdatedBy = userId;
        }

        await db.SaveChangesAsync();
        await _audit.LogAsync(
            action: "settings.own.update",
            buildingId: buildingId,
            apartmentId: apartmentId,
            userId: userId,
            userRole: userRole,
            metadata: new { language, paymentMethod },
            severity: "info");
    }

    /// <summary>Updates the editable canonical identity fields for the signed-in account.</summary>
    public async Task UpdateOwnIdentityProfileAsync(
        string uid, string role, string? buildingId, string? apartmentId,
        OwnSettings settings, string? newPin, string? currentPin)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!Guid.TryParse(uid, out var accountId))
            throw new InvalidOperationException("account-not-found");

        var displayName = settings.DisplayName?.Trim() ?? "";
        var legalName = settings.LegalName?.Trim();
        var email = string.IsNullOrWhiteSpace(settings.PrimaryEmail)
            ? null : settings.PrimaryEmail.Trim();
        var phone = string.IsNullOrWhiteSpace(settings.PrimaryPhone)
            ? null : AuthHelpers.NormalizePhone(settings.PrimaryPhone);
        var whatsapp = string.IsNullOrWhiteSpace(settings.Whatsapp)
            ? null : AuthHelpers.NormalizePhone(settings.Whatsapp);
        var photoUrl = string.IsNullOrWhiteSpace(settings.PhotoUrl) ? null : settings.PhotoUrl.Trim();

        if (displayName.Length is < 2 or > 160) throw new InvalidOperationException("invalid-display-name");
        if (settings.ApartmentLabel?.Length > 60)
            throw new InvalidOperationException("apartment-label-too-long");
        if (legalName?.Length > 200 || email?.Length > 254 || photoUrl?.Length > 2048)
            throw new InvalidOperationException("profile-field-too-long");
        if (email is not null && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            throw new InvalidOperationException("invalid-email-address");
        if (photoUrl is not null && (!Uri.TryCreate(photoUrl, UriKind.Absolute, out var photoUri)
            || photoUri.Scheme is not ("https" or "http")))
            throw new InvalidOperationException("invalid-photo-url");
        if (!string.IsNullOrWhiteSpace(newPin) &&
            (role == "superadmin" || newPin.Length is < 4 or > 12
                || newPin.Any(character => character < '0' || character > '9')))
            throw new InvalidOperationException("invalid-pin-change");

        await using var db = await _sqlFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var now = DateTime.UtcNow;
        var account = await db.UserAccounts.SingleOrDefaultAsync(item =>
            item.AccountId == accountId && item.AccountStatus == 1)
            ?? throw new InvalidOperationException("account-not-found");
        var person = await db.Persons.SingleOrDefaultAsync(item =>
            item.PersonId == account.PersonId && !item.IsArchived)
            ?? throw new InvalidOperationException("account-not-found");
        phone ??= person.PrimaryPhone;

        ApartmentEntity? apartment = null;
        if (role == "resident")
        {
            if (string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(apartmentId))
                throw new InvalidOperationException("apartment-access-denied");
            apartment = await db.Apartments.SingleOrDefaultAsync(item =>
                item.BuildingId == buildingId && item.Id == apartmentId)
                ?? throw new InvalidOperationException("apartment-not-found");
            var hasOccupancy = await db.ApartmentOccupancies.AnyAsync(item =>
                item.PersonId == person.PersonId && item.BuildingId == buildingId && item.ApartmentId == apartmentId
                && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now));
            if (!hasOccupancy) throw new InvalidOperationException("apartment-access-denied");
        }
        else if (role == "admin")
        {
            if (string.IsNullOrWhiteSpace(buildingId) || !await db.UserRoleAssignments.AnyAsync(item =>
                item.AccountId == accountId && (item.RoleKey == "admin" || item.RoleKey == "building-admin")
                && item.ScopeType == 1 && item.ScopeBuildingId == buildingId && item.RevokedAtUtc == null
                && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now)))
                throw new InvalidOperationException("building-access-denied");
        }
        else if (role == "superadmin")
        {
            if (!await db.UserRoleAssignments.AnyAsync(item => item.AccountId == accountId
                && item.RoleKey == "superadmin" && item.ScopeType == 0 && item.RevokedAtUtc == null
                && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now)))
                throw new InvalidOperationException("superadmin-access-denied");
        }
        else
        {
            throw new InvalidOperationException("unsupported-account-role");
        }

        var nameChanged = !string.Equals(person.DisplayName, displayName, StringComparison.Ordinal);
        var legalNameChanged = !string.Equals(person.LegalName ?? "", legalName ?? "", StringComparison.Ordinal);
        var emailChanged = !string.Equals(person.PrimaryEmail ?? "", email ?? "", StringComparison.OrdinalIgnoreCase);
        var phoneChanged = !string.Equals(person.PrimaryPhone ?? "", phone ?? "", StringComparison.Ordinal);
        var whatsappChanged = !string.Equals(person.Whatsapp ?? "", whatsapp ?? "", StringComparison.Ordinal);
        var photoChanged = !string.Equals(person.PhotoUrl ?? "", photoUrl ?? "", StringComparison.Ordinal);
        var pinChanged = !string.IsNullOrWhiteSpace(newPin);

        if (emailChanged && email is not null && await db.Persons.AsNoTracking().AnyAsync(item =>
            item.PersonId != person.PersonId && item.PrimaryEmail == email))
            throw new InvalidOperationException("email-already-in-use");
        if (phoneChanged && phone is not null && await db.Persons.AsNoTracking().AnyAsync(item =>
            item.PersonId != person.PersonId && item.PrimaryPhone == phone))
            throw new InvalidOperationException("phone-already-in-use");

        if ((role is "resident" or "admin") && (phoneChanged || emailChanged || whatsappChanged || pinChanged))
        {
            var credential = await db.UserCredentials.AsNoTracking()
                .Where(item => item.AccountId == accountId && item.CredentialType == 0
                    && item.RevokedAtUtc == null && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now))
                .OrderByDescending(item => item.CreatedAtUtc).FirstOrDefaultAsync();
            if (credential is null || string.IsNullOrWhiteSpace(currentPin)
                || !_hasher.VerifyPin(currentPin, credential.CredentialHash))
                throw new InvalidOperationException(Loc.T("The_Current_PIN_Is_Incorrect"));
        }
        if (role == "superadmin" && (phoneChanged || emailChanged || whatsappChanged))
        {
            var mfa = await db.UserTwoFactorSettings.AsNoTracking()
                .SingleOrDefaultAsync(item => item.AccountId == accountId);
            if (mfa?.IsEnabled != true || mfa.LastVerifiedAtUtc is null
                || mfa.LastVerifiedAtUtc < now.AddMinutes(-10))
                throw new InvalidOperationException("fresh-mfa-required-for-contact-change");
        }

        person.DisplayName = displayName;
        person.LegalName = string.IsNullOrWhiteSpace(legalName) ? null : legalName;
        person.PrimaryEmail = email;
        person.PrimaryPhone = phone;
        person.Whatsapp = whatsapp;
        person.PhotoUrl = photoUrl;
        if (apartment is not null)
        {
            apartment.Owner = displayName;
            apartment.Label = settings.ApartmentLabel?.Trim() ?? "";
            if (phoneChanged && phone is not null) apartment.Phone = phone;
        }

        if (pinChanged)
        {
            var oldCredentials = await db.UserCredentials.Where(item => item.AccountId == accountId
                && item.CredentialType == 0 && item.RevokedAtUtc == null).ToListAsync();
            foreach (var old in oldCredentials) old.RevokedAtUtc = now;
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = Guid.NewGuid(), AccountId = accountId, CredentialType = 0,
                CredentialHash = _hasher.HashPin(newPin!), HashAlgorithm = "BCrypt.Net-Next", CreatedAtUtc = now
            });
            account.MustChangePin = false;
        }

        if (nameChanged || legalNameChanged || emailChanged || phoneChanged || whatsappChanged || photoChanged || pinChanged)
        {
            person.UpdatedAtUtc = now;
            if (phoneChanged || pinChanged) account.SecurityStamp = Guid.NewGuid().ToString("N");
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        if (phoneChanged || pinChanged) _sessionRevalidator?.InvalidateAdmin(uid);

        await _audit.LogAsync(
            action: "account.profile_update",
            buildingId: buildingId,
            apartmentId: apartmentId,
            userId: uid,
            userRole: role,
            metadata: new { nameChanged, legalNameChanged, emailChanged, phoneChanged, whatsappChanged, photoChanged, pinChanged },
            severity: "info");
    }

    // ============================================================
    // Update Resident Profile (legacy compatibility)
    // ============================================================
    /// <summary>
    /// Updates the resident profile.
    /// </summary>
    public async Task UpdateResidentProfileAsync(
        string buildingId, string apartmentId, string ownerName, string aptLabel,
        string newPhone, string? newPin, string currentPin,
        string currentUid, string userRole)
    {
        if (!Guid.TryParse(currentUid, out var accountId))
            throw new InvalidOperationException("resident-account-not-found");
        if (!string.IsNullOrWhiteSpace(newPin) &&
            (newPin.Length != 4 || !newPin.All(char.IsDigit)))
            throw new InvalidOperationException(Loc.T("PIN_Must_Be_Exactly_4_Digits"));

        await using var db = await _sqlFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var now = DateTime.UtcNow;
        var apartment = await db.Apartments.SingleOrDefaultAsync(item =>
            item.BuildingId == buildingId && item.Id == apartmentId)
            ?? throw new InvalidOperationException("apartment-not-found");
        var account = await db.UserAccounts.SingleOrDefaultAsync(item =>
            item.AccountId == accountId && item.AccountStatus == 1)
            ?? throw new InvalidOperationException("resident-account-not-found");
        var person = await db.Persons.SingleOrDefaultAsync(item =>
            item.PersonId == account.PersonId && !item.IsArchived)
            ?? throw new InvalidOperationException("resident-account-not-found");

        var activeOccupancy = await db.ApartmentOccupancies.AnyAsync(item =>
            item.PersonId == person.PersonId && item.BuildingId == buildingId && item.ApartmentId == apartmentId
            && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now));
        if (!activeOccupancy) throw new InvalidOperationException("apartment-access-denied");

        var pinChanged = !string.IsNullOrWhiteSpace(newPin);
        if (pinChanged)
        {
            var credential = await db.UserCredentials.AsNoTracking()
                .Where(item => item.AccountId == accountId && item.CredentialType == 0
                    && item.RevokedAtUtc == null && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now))
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstOrDefaultAsync();
            if (credential is null || !_hasher.VerifyPin(currentPin, credential.CredentialHash))
                throw new InvalidOperationException(Loc.T("The_Current_PIN_Is_Incorrect"));
        }

        var normalizedPhone = string.IsNullOrWhiteSpace(newPhone)
            ? person.PrimaryPhone
            : AuthHelpers.NormalizePhone(newPhone);
        var phoneChanged = !string.IsNullOrWhiteSpace(normalizedPhone)
            && !string.Equals(normalizedPhone, person.PrimaryPhone, StringComparison.Ordinal);
        if (phoneChanged)
        {
            var duplicatePhone = await db.Persons.AsNoTracking().AnyAsync(item =>
                item.PersonId != person.PersonId && item.PrimaryPhone == normalizedPhone);
            if (duplicatePhone) throw new InvalidOperationException("phone-already-in-use");
        }

        if (!string.IsNullOrWhiteSpace(ownerName))
        {
            person.DisplayName = ownerName.Trim();
            apartment.Owner = ownerName.Trim();
        }
        if (!string.IsNullOrWhiteSpace(aptLabel)) apartment.Label = aptLabel.Trim();
        if (phoneChanged)
        {
            person.PrimaryPhone = normalizedPhone!;
            apartment.Phone = normalizedPhone!;
        }

        if (pinChanged)
        {
            var oldCredentials = await db.UserCredentials.Where(item => item.AccountId == accountId
                && item.CredentialType == 0 && item.RevokedAtUtc == null).ToListAsync();
            foreach (var old in oldCredentials) old.RevokedAtUtc = now;
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = Guid.NewGuid(),
                AccountId = accountId,
                CredentialType = 0,
                CredentialHash = _hasher.HashPin(newPin!),
                HashAlgorithm = "BCrypt.Net-Next",
                CreatedAtUtc = now
            });
            account.MustChangePin = false;
        }

        if (phoneChanged || pinChanged || !string.IsNullOrWhiteSpace(ownerName))
        {
            person.UpdatedAtUtc = now;
            account.SecurityStamp = Guid.NewGuid().ToString("N");
        }
        db.AuditEvents.Add(new AuditEventEntity
        {
            AuditEventId = Guid.NewGuid(),
            OccurredAtUtc = now,
            ActorAccountId = accountId,
            ActorReference = currentUid,
            BuildingId = buildingId,
            EventCategory = "ResidentProfile",
            EventType = "resident.profile_update",
            ResourceType = "Apartment",
            ResourceReference = apartmentId,
            Outcome = 0,
            SafeDetailsJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                nameChanged = !string.IsNullOrWhiteSpace(ownerName),
                phoneChanged,
                pinChanged,
                userRole
            })
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        if (pinChanged || phoneChanged) _sessionRevalidator?.InvalidateAdmin(currentUid);

        await _audit.LogAsync(
            action: "resident.profile_update",
            buildingId: buildingId,
            apartmentId: apartmentId,
            userId: currentUid,
            userRole: userRole,
            metadata: new { nameChanged = !string.IsNullOrWhiteSpace(ownerName), phoneChanged, pinChanged },
            severity: "warning");
    }

    /// <summary>Returns the canonical person profile for an authenticated target account.</summary>
    public async Task<(string Name, string Phone, string Whatsapp)?> GetAccountProfileAsync(string uid)
    {
        if (!Guid.TryParse(uid, out var accountId)) return null;
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var profile = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            where account.AccountId == accountId && account.AccountStatus == 1 && !person.IsArchived
            select new { person.DisplayName, Phone = person.PrimaryPhone ?? "", Whatsapp = person.Whatsapp ?? "" })
            .SingleOrDefaultAsync();
        return profile is null ? null : (profile.DisplayName, profile.Phone, profile.Whatsapp);
    }

    // ============================================================
    // Update Admin Profile (الدالة الأصلية)
    // ============================================================
    /// <summary>
    /// Updates the admin profile.
    /// </summary>
    public async Task UpdateAdminProfileAsync(
        string uid, string name, string newPhone, string newWhatsapp,
        string? newPin, string currentPin, string userRole)
    {
        if (!Guid.TryParse(uid, out var accountId))
            throw new InvalidOperationException("admin-not-found");
        var now = DateTime.UtcNow;
        var pinChanged = !string.IsNullOrWhiteSpace(newPin);
        if (pinChanged && (newPin!.Length != 4 || !newPin.All(char.IsDigit)))
            throw new InvalidOperationException(Loc.T("PIN_Must_Be_Exactly_4_Digits"));

        await using var db = await _sqlFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var account = await db.UserAccounts.SingleOrDefaultAsync(item =>
            item.AccountId == accountId && item.AccountStatus == 1)
            ?? throw new InvalidOperationException("admin-not-found");
        var person = await db.Persons.SingleOrDefaultAsync(item =>
            item.PersonId == account.PersonId && !item.IsArchived)
            ?? throw new InvalidOperationException("admin-not-found");
        var buildingId = await db.UserRoleAssignments.AsNoTracking()
            .Where(item => item.AccountId == accountId && (item.RoleKey == "admin" || item.RoleKey == "building-admin")
                && item.ScopeType == 1 && item.RevokedAtUtc == null)
            .Select(item => item.ScopeBuildingId).FirstOrDefaultAsync() ?? "";

        var normalizedPhone = string.IsNullOrWhiteSpace(newPhone)
            ? person.PrimaryPhone
            : AuthHelpers.NormalizePhone(newPhone);
        var phoneChanged = !string.IsNullOrWhiteSpace(normalizedPhone)
            && !string.Equals(normalizedPhone, person.PrimaryPhone, StringComparison.Ordinal);
        var normalizedWhatsapp = string.IsNullOrWhiteSpace(newWhatsapp)
            ? person.Whatsapp
            : AuthHelpers.NormalizePhone(newWhatsapp);
        var whatsappChanged = !string.IsNullOrWhiteSpace(normalizedWhatsapp)
            && !string.Equals(normalizedWhatsapp, person.Whatsapp, StringComparison.Ordinal);
        var nameChanged = !string.IsNullOrWhiteSpace(name)
            && !string.Equals(name.Trim(), person.DisplayName, StringComparison.Ordinal);

        if (phoneChanged || pinChanged)
        {
            var credential = await db.UserCredentials.AsNoTracking()
                .Where(item => item.AccountId == accountId && item.CredentialType == 0
                    && item.RevokedAtUtc == null && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now))
                .OrderByDescending(item => item.CreatedAtUtc).FirstOrDefaultAsync();
            if (credential is null || !_hasher.VerifyPin(currentPin, credential.CredentialHash))
                throw new InvalidOperationException(Loc.T("The_Current_PIN_Is_Incorrect"));
        }
        if (phoneChanged)
        {
            var duplicate = await db.Persons.AsNoTracking().AnyAsync(item =>
                item.PersonId != person.PersonId && item.PrimaryPhone == normalizedPhone);
            if (duplicate) throw new InvalidOperationException("phone-already-in-use");
            person.PrimaryPhone = normalizedPhone;
        }
        if (whatsappChanged) person.Whatsapp = normalizedWhatsapp;
        if (nameChanged) person.DisplayName = name.Trim();
        if (pinChanged)
        {
            var oldCredentials = await db.UserCredentials.Where(item => item.AccountId == accountId
                && item.CredentialType == 0 && item.RevokedAtUtc == null).ToListAsync();
            foreach (var old in oldCredentials) old.RevokedAtUtc = now;
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = Guid.NewGuid(), AccountId = accountId, CredentialType = 0,
                CredentialHash = _hasher.HashPin(newPin!), HashAlgorithm = "BCrypt.Net-Next", CreatedAtUtc = now
            });
            account.MustChangePin = false;
        }
        if (phoneChanged || whatsappChanged || nameChanged || pinChanged)
        {
            person.UpdatedAtUtc = now;
            account.SecurityStamp = Guid.NewGuid().ToString("N");
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        if (phoneChanged || pinChanged) _sessionRevalidator?.InvalidateAdmin(uid);

        await _audit.LogAsync(
            action: "admin.profile_update",
            buildingId: buildingId,
            userId: uid,
            userRole: userRole,
            metadata: new { nameChanged, phoneChanged, whatsappChanged, pinChanged },
            severity: "warning");
    }

    /// <summary>Updates the display name of a target-schema SuperAdmin account.</summary>
    public async Task UpdateSuperAdminNameAsync(string uid, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        if (!Guid.TryParse(uid, out var accountId))
            throw new InvalidOperationException("superadmin-not-found");

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var account = await db.UserAccounts.SingleOrDefaultAsync(item =>
            item.AccountId == accountId && item.AccountStatus == 1)
            ?? throw new InvalidOperationException("superadmin-not-found");
        var hasSuperAdminRole = await db.UserRoleAssignments.AnyAsync(item =>
            item.AccountId == accountId && item.RoleKey == "superadmin" && item.ScopeType == 0
            && item.RevokedAtUtc == null && item.ValidFromUtc <= DateTime.UtcNow
            && (item.ValidToUtc == null || item.ValidToUtc > DateTime.UtcNow));
        if (!hasSuperAdminRole) throw new InvalidOperationException("superadmin-not-found");
        var person = await db.Persons.SingleOrDefaultAsync(item => item.PersonId == account.PersonId && !item.IsArchived)
            ?? throw new InvalidOperationException("superadmin-not-found");
        person.DisplayName = name.Trim();
        person.UpdatedAtUtc = DateTime.UtcNow;
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync();
        _sessionRevalidator?.InvalidateAdmin(uid);

        await _audit.LogAsync(
            action: "superadmin.name_update",
            userId: uid,
            userRole: "superadmin",
            metadata: new { name = person.DisplayName },
            severity: "info");
    }
}
