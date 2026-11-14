using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class SettingsService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly IAuditLogger _audit;
    private readonly BuildingsService _buildings;
    private readonly UsersService _users;
    private readonly ResidentsService _residents; // ✅ تمت الإضافة
    private readonly ILogger<SettingsService> _logger;
    private const string GlobalId = "global";

    public SettingsService(
        IDbContextFactory<AppDbContext> sqlFactory,
        IAuditLogger audit,
        BuildingsService buildings,
        UsersService users,
        ResidentsService residents, // ✅ تمت الإضافة
        ILogger<SettingsService> logger)
    {
        _sqlFactory = sqlFactory;
        _audit = audit;
        _buildings = buildings;
        _users = users;
        _residents = residents; // ✅ تمت الإضافة
        _logger = logger;
    }

    // ============================================================
    // Global Settings (الدوال الأصلية)
    // ============================================================
    public async Task<GlobalSettings> GetGlobalAsync()
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var entity = await db.GlobalSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == GlobalId);

        if (entity == null)
        {
            entity = new Data.Entities.GlobalSettingsEntity { Id = GlobalId };
            db.GlobalSettings.Add(entity);
            await db.SaveChangesAsync();
        }

        return new GlobalSettings
        {
            Id = entity.Id,
            AppName = entity.AppName,
            DefaultLanguage = entity.DefaultLanguage,
            WhatsappApiToken = entity.WhatsappApiToken,
            WhatsappPhoneId = entity.WhatsappPhoneId,
            CloudinaryCloudName = entity.CloudinaryCloudName,
            CloudinaryApiKey = entity.CloudinaryApiKey,
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
            UpdatedAt = entity.UpdatedAt?.ToString("o"),
            UpdatedBy = entity.UpdatedBy
        };
    }

    public async Task SaveGlobalAsync(GlobalSettings settings, string userId)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var entity = await db.GlobalSettings.FirstOrDefaultAsync(x => x.Id == GlobalId);
        if (entity == null)
        {
            entity = new Data.Entities.GlobalSettingsEntity { Id = GlobalId };
            db.GlobalSettings.Add(entity);
        }

        entity.AppName = settings.AppName ?? "نظام إدارة العمارات";
        entity.DefaultLanguage = settings.DefaultLanguage ?? "ar";
        entity.WhatsappApiToken = settings.WhatsappApiToken ?? "";
        entity.WhatsappPhoneId = settings.WhatsappPhoneId ?? "";
        entity.CloudinaryCloudName = settings.CloudinaryCloudName ?? "";
        entity.CloudinaryApiKey = settings.CloudinaryApiKey ?? "";
        entity.EnablePolls = settings.FeatureFlags?.EnablePolls ?? true;
        entity.EnableMaintenance = settings.FeatureFlags?.EnableMaintenance ?? true;
        entity.EnableOcr = settings.FeatureFlags?.EnableOcr ?? true;
        entity.EnableQrAccess = settings.FeatureFlags?.EnableQrAccess ?? true;
        entity.EnableWhatsApp = settings.FeatureFlags?.EnableWhatsApp ?? true;
        entity.SessionTimeoutDays = settings.Security?.SessionTimeoutDays ?? 7;
        entity.MaxPinAttempts = settings.Security?.MaxPinAttempts ?? 5;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;

        await db.SaveChangesAsync();

        await _audit.LogAsync(
            action: "settings.global.update",
            userId: userId,
            userRole: "superadmin",
            metadata: new { appName = entity.AppName, defaultLanguage = entity.DefaultLanguage },
            severity: "warning");
    }

    // ============================================================
    // Building Settings (الدوال الأصلية)
    // ============================================================
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
    public async Task<OwnSettings> GetOwnAsync(string uid, string? role = null,
        string? buildingId = null, string? apartmentId = null)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();

        if (role == "resident" && !string.IsNullOrEmpty(buildingId) && !string.IsNullOrEmpty(apartmentId))
        {
            var apt = await db.Apartments.AsNoTracking()
                .FirstOrDefaultAsync(a => a.BuildingId == buildingId && a.Id == apartmentId);

            if (apt == null) return new OwnSettings();

            return new OwnSettings
            {
                Language = string.IsNullOrWhiteSpace(apt.Language) ? "ar" : apt.Language,
                NotificationPreferences = new NotificationPreferences
                {
                    WhatsApp = apt.NotifyWhatsApp,
                    Email = apt.NotifyEmail,
                    InApp = apt.NotifyInApp
                },
                PreferredPaymentMethod = string.IsNullOrWhiteSpace(apt.PreferredPaymentMethod)
                    ? "instapay" : apt.PreferredPaymentMethod,
                UpdatedAt = apt.SettingsUpdatedAt?.ToString("o"),
                UpdatedBy = apt.SettingsUpdatedBy
            };
        }

        var user = await _users.GetByUidAsync(uid);
        if (user == null) return new OwnSettings();

        return new OwnSettings
        {
            Language = "ar",
            NotificationPreferences = new NotificationPreferences(),
            PreferredPaymentMethod = "instapay"
        };
    }

    public async Task SaveOwnAsync(string uid, OwnSettings settings, string userId, string userRole,
        string? buildingId = null, string? apartmentId = null)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();

        if (userRole == "resident" && !string.IsNullOrEmpty(buildingId) && !string.IsNullOrEmpty(apartmentId))
        {
            var apt = await db.Apartments
                .FirstOrDefaultAsync(a => a.BuildingId == buildingId && a.Id == apartmentId);

            if (apt != null)
            {
                apt.Language = settings.Language ?? "ar";
                apt.NotifyWhatsApp = settings.NotificationPreferences?.WhatsApp ?? true;
                apt.NotifyEmail = settings.NotificationPreferences?.Email ?? false;
                apt.NotifyInApp = settings.NotificationPreferences?.InApp ?? true;
                apt.PreferredPaymentMethod = settings.PreferredPaymentMethod ?? "instapay";
                apt.SettingsUpdatedAt = DateTime.UtcNow;
                apt.SettingsUpdatedBy = userId;
                await db.SaveChangesAsync();
            }
        }

        await _audit.LogAsync(
            action: "settings.own.update",
            buildingId: buildingId,
            apartmentId: apartmentId,
            userId: userId,
            userRole: userRole,
            metadata: new { language = settings.Language },
            severity: "info");
    }

    // ============================================================
    // Update Resident Profile (✅ المعدلة)
    // ============================================================
    public async Task UpdateResidentProfileAsync(
        string buildingId, string apartmentId, string ownerName, string aptLabel,
        string newPhone, string? newPin, string currentPin,
        string currentUid, string userRole)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();

        var apt = await db.Apartments
            .FirstOrDefaultAsync(a => a.BuildingId == buildingId && a.Id == apartmentId)
            ?? throw new InvalidOperationException("apartment-not-found");

        var residents = await db.Residents
            .Where(r => r.ApartmentId == apartmentId)
            .ToListAsync();

        var primaryResident = residents.FirstOrDefault(r => r.IsPrimary) ?? residents.FirstOrDefault();

        // التحقق من الـ PIN الحالي
        if (!string.IsNullOrWhiteSpace(newPin))
        {
            var isCurrentPinValid = primaryResident != null &&
                                    SecretVerifier.Verify(currentPin, primaryResident.PinHash);

            if (!isCurrentPinValid)
                throw new InvalidOperationException(Loc.T("The_Current_PIN_Is_Incorrect"));
        }

        var oldPhone = apt.Phone;
        var phoneChanged = false;
        var pinChanged = false;

        // تحديث الاسم والموبايل في جدول الشقق
        if (!string.IsNullOrWhiteSpace(ownerName)) apt.Owner = ownerName.Trim();
        if (!string.IsNullOrWhiteSpace(aptLabel)) apt.Label = aptLabel.Trim();

        if (!string.IsNullOrWhiteSpace(newPhone))
        {
            var normPhone = AuthHelpers.NormalizePhone(newPhone);
            if (normPhone != oldPhone)
            {
                apt.Phone = normPhone;
                phoneChanged = true;
            }
        }

        // تحديث الاسم والموبايل في جدول السكان
        if (residents.Any())
        {
            foreach (var res in residents)
            {
                if (!string.IsNullOrWhiteSpace(ownerName)) res.Name = ownerName.Trim();
                if (!string.IsNullOrWhiteSpace(newPhone)) res.Phone = AuthHelpers.NormalizePhone(newPhone);
            }
        }

        await db.SaveChangesAsync();

        // ✅ استدعاء الدالة المركزية من ResidentsService
        if (!string.IsNullOrWhiteSpace(newPin))
        {
            if (newPin.Length != 4 || !newPin.All(char.IsDigit))
                throw new InvalidOperationException(Loc.T("PIN_Must_Be_Exactly_4_Digits"));

            await _residents.UpdateResidentPinAsync(buildingId, apartmentId, newPin);
            pinChanged = true;
        }

        await _audit.LogAsync(
            action: "resident.profile_update",
            buildingId: buildingId,
            apartmentId: apartmentId,
            userId: currentUid,
            userRole: userRole,
            metadata: new { ownerName, phoneChanged, pinChanged },
            severity: "warning");
    }

    // ============================================================
    // Update Admin Profile (الدالة الأصلية)
    // ============================================================
    public async Task UpdateAdminProfileAsync(
        string uid, string name, string newPhone, string newWhatsapp,
        string? newPin, string currentPin, string userRole)
    {
        var user = await _users.GetByUidAsync(uid)
            ?? throw new InvalidOperationException("admin-not-found");

        var buildingId = user.BuildingIds.FirstOrDefault() ?? "";

        var phoneChangedByUser = !string.IsNullOrWhiteSpace(newPhone) &&
                                 AuthHelpers.NormalizePhone(newPhone) != user.Phone;
        var pinChanged = !string.IsNullOrWhiteSpace(newPin);

        if ((phoneChangedByUser || pinChanged) && !SecretVerifier.Verify(currentPin, user.Pin))
            throw new InvalidOperationException(Loc.T("The_Current_PIN_Is_Incorrect"));

        var updates = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(name) && name != user.Name) updates["name"] = name.Trim();
        if (phoneChangedByUser) updates["phone"] = AuthHelpers.NormalizePhone(newPhone);
        if (!string.IsNullOrWhiteSpace(newWhatsapp))
        {
            var normWa = AuthHelpers.NormalizePhone(newWhatsapp);
            if (normWa != user.Whatsapp) updates["whatsapp"] = normWa;
        }
        if (pinChanged)
        {
            if (newPin!.Length != 4 || !newPin.All(char.IsDigit))
                throw new InvalidOperationException(Loc.T("PIN_Must_Be_Exactly_4_Digits"));
            updates["pin"] = newPin;
        }

        if (updates.Count > 0)
        {
            updates["updatedAt"] = DateTime.UtcNow.ToString("o");
            updates["updatedBy"] = uid;
            await _users.SetAsync(uid, updates);
        }

        await _audit.LogAsync(
            action: "admin.profile_update",
            buildingId: buildingId,
            userId: uid,
            userRole: userRole,
            metadata: new { nameChanged = updates.ContainsKey("name"), phoneChanged = phoneChangedByUser, pinChanged },
            severity: "warning");
    }

    // ============================================================
    // Update SuperAdmin Name (الدالة الأصلية)
    // ============================================================
    public async Task UpdateSuperAdminNameAsync(string uid, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        await _users.SetAsync(uid, new Dictionary<string, object>
        {
            ["name"] = name.Trim(),
            ["updatedAt"] = DateTime.UtcNow.ToString("o"),
            ["updatedBy"] = uid
        });

        await _audit.LogAsync(
            action: "superadmin.name_update",
            userId: uid,
            userRole: "superadmin",
            metadata: new { name },
            severity: "info");
    }
}