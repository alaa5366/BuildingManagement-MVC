using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Services;

public class SettingsService
{
    private readonly FirestoreDb _db;
    private readonly IAuditLogger _audit;
    private readonly BuildingsService _buildings;
    private readonly UsersService _users;
    private readonly FirebaseAuthRestService _firebaseAuth;
    private readonly FirebaseAdminService _fbAdmin;
    private readonly ILogger<SettingsService> _logger;
    private readonly BuildingSqlStore? _sqlStore;
    private const string SystemSettingsCol = "system_settings";
    private const string BuildingsCol = "buildings";
    private const string UsersCol = "users";
    private const string GlobalDocId = "global";

    public SettingsService(
    FirestoreContext ctx,
    IAuditLogger audit,
    BuildingsService buildings,
    UsersService users,
    FirebaseAuthRestService firebaseAuth,
    FirebaseAdminService fbAdmin,
    ILogger<SettingsService> logger,
    BuildingSqlStore? sqlStore = null)          
    {
        _db = ctx.Db;
        _audit = audit;
        _buildings = buildings;
        _users = users;
        _firebaseAuth = firebaseAuth;
        _fbAdmin = fbAdmin;
        _logger = logger;
        _sqlStore = sqlStore;                        
    }

    // ============================================================
    // 1. Global Settings
    // ============================================================
    public async Task<GlobalSettings> GetGlobalAsync()
    {
        var doc = await _db.Collection(SystemSettingsCol).Document(GlobalDocId).GetSnapshotAsync();
        if (!doc.Exists)
        {
            var defaults = new GlobalSettings();
            await _db.Collection(SystemSettingsCol).Document(GlobalDocId).SetAsync(defaults);
            return defaults;
        }
        return doc.ConvertTo<GlobalSettings>();
    }

    public async Task SaveGlobalAsync(GlobalSettings settings, string userId)
    {
        settings.UpdatedAt = DateTime.UtcNow.ToString("o");
        settings.UpdatedBy = userId;

        await _db.Collection(SystemSettingsCol).Document(GlobalDocId)
            .SetAsync(settings, SetOptions.Overwrite);

        await _audit.LogAsync(
            action: "settings.global.update",
            userId: userId,
            userRole: "superadmin",
            metadata: new
            {
                appName = settings.AppName,
                defaultLanguage = settings.DefaultLanguage,
                featureFlags = new
                {
                    settings.FeatureFlags.EnablePolls,
                    settings.FeatureFlags.EnableMaintenance,
                    settings.FeatureFlags.EnableOcr,
                    settings.FeatureFlags.EnableQrAccess
                }
            },
            severity: "warning");
    }

    // ============================================================
    // 2. Building Settings
    // ============================================================
    public async Task<BuildingSettings> GetBuildingAsync(string buildingId)
    {
        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) throw new InvalidOperationException("building-not-found");

        var doc = await _db.Collection(BuildingsCol).Document(buildingId).GetSnapshotAsync();
        if (!doc.Exists) throw new InvalidOperationException("building-not-found");

        var data = doc.ToDictionary();
        if (data.TryGetValue("settings", out var settingsRaw) && settingsRaw != null)
        {
            var settingsDict = ToDict(settingsRaw);
            if (settingsDict != null)
            {
                return new BuildingSettings
                {
                    DisplayName = GetString(settingsDict, "displayName", building.Name),
                    Address = GetString(settingsDict, "address"),
                    WhatsappNumber = GetString(settingsDict, "whatsappNumber", building.AdminWhatsapp),
                    InvoiceDayOfMonth = GetInt(settingsDict, "invoiceDayOfMonth", 1),
                    ExpenseDistribution = GetString(settingsDict, "expenseDistribution", "equal"),
                    VotingQuorumPercent = GetInt(settingsDict, "votingQuorumPercent", 50),
                    Currency = GetString(settingsDict, "currency", "EGP"),
                    UpdatedAt = GetStringOrNull(settingsDict, "updatedAt"),
                    UpdatedBy = GetStringOrNull(settingsDict, "updatedBy")
                };
            }
        }

        return new BuildingSettings
        {
            DisplayName = building.Name,
            WhatsappNumber = building.AdminWhatsapp
        };
    }

    public async Task SaveBuildingAsync(string buildingId, BuildingSettings settings, string userId, string userRole)
    {
        settings.UpdatedAt = DateTime.UtcNow.ToString("o");
        settings.UpdatedBy = userId;

        var dict = new Dictionary<string, object>
        {
            ["displayName"] = settings.DisplayName,
            ["address"] = settings.Address,
            ["whatsappNumber"] = settings.WhatsappNumber,
            ["invoiceDayOfMonth"] = settings.InvoiceDayOfMonth,
            ["expenseDistribution"] = settings.ExpenseDistribution,
            ["votingQuorumPercent"] = settings.VotingQuorumPercent,
            ["currency"] = settings.Currency,
            ["updatedAt"] = settings.UpdatedAt,
            ["updatedBy"] = settings.UpdatedBy
        };

        await _db.Collection(BuildingsCol).Document(buildingId)
            .SetAsync(new Dictionary<string, object> { ["settings"] = dict }, SetOptions.MergeAll);

        if (_sqlStore != null)
        {
            try
            {
                await _sqlStore.SaveSettingsAsync(buildingId, dict);
            }
            catch (Exception ex)
            {
                SqlMirrorHealth.RecordFailure($"settings {buildingId}", ex);
                _logger.LogError(ex, "SQL settings mirror failed for building {BuildingId}", buildingId);
            }
        }

        await _audit.LogAsync(
            action: "settings.building.update",
            buildingId: buildingId,
            userId: userId,
            userRole: userRole,
            metadata: new
            {
                displayName = settings.DisplayName,
                expenseDistribution = settings.ExpenseDistribution,
                votingQuorumPercent = settings.VotingQuorumPercent,
                currency = settings.Currency
            },
            severity: "warning");
    }

    // ============================================================
    // 3. Own Settings — القراءة
    // ============================================================
    public async Task<OwnSettings> GetOwnAsync(string uid, string? role = null,
        string? buildingId = null, string? apartmentId = null)
    {
        _logger.LogWarning(">>> GetOwnAsync START: uid={Uid}, role={Role}, bid={Bid}, aid={Aid}",
            uid, role, buildingId, apartmentId);

        // 1. users/{uid} — Super Admin, Admin
        var userDoc = await _db.Collection(UsersCol).Document(uid).GetSnapshotAsync();
        if (userDoc.Exists)
        {
            var data = userDoc.ToDictionary();

            if (data.TryGetValue("settings", out var settingsRaw) && settingsRaw != null)
            {
                var settingsDict = ToDict(settingsRaw);
                if (settingsDict != null)
                {
                    _logger.LogWarning(">>> GetOwnAsync (admin/superadmin) returning parsed");
                    return ParseSettingsFlexible(settingsDict);
                }
            }

            _logger.LogWarning(">>> GetOwnAsync (admin/superadmin) — no settings found");
            return new OwnSettings();
        }

        // 2. Resident — اقرأ من buildings/{buildingId}.apartments[i].settings
        if (role != "resident")
        {
            _logger.LogWarning(">>> GetOwnAsync: role != resident and user doc not found");
            return new OwnSettings();
        }

        if (string.IsNullOrEmpty(buildingId) || string.IsNullOrEmpty(apartmentId))
        {
            _logger.LogWarning(">>> GetOwnAsync (resident): missing bid/aid");
            return new OwnSettings();
        }

        var buildingDoc = await _db.Collection(BuildingsCol).Document(buildingId).GetSnapshotAsync();
        if (!buildingDoc.Exists)
        {
            _logger.LogWarning(">>> GetOwnAsync (resident): building not found");
            return new OwnSettings();
        }

        // ✅ ConvertTo<Building> — أسرع وأدق
        var building = buildingDoc.ConvertTo<Building>();
        if (building?.Apartments == null)
        {
            _logger.LogWarning(">>> GetOwnAsync (resident): building.Apartments is null");
            return new OwnSettings();
        }

        var apt = building.Apartments.FirstOrDefault(a => a.Id == apartmentId);
        if (apt == null)
        {
            _logger.LogWarning(">>> GetOwnAsync (resident): apartment {Aid} not found in building {Bid}",
                apartmentId, buildingId);
            return new OwnSettings();
        }

        if (apt.Settings != null)
        {
            _logger.LogWarning(">>> GetOwnAsync (resident) returning parsed: lang={Lang}, wa={Wa}",
                apt.Settings.Language, apt.Settings.NotificationPreferences?.WhatsApp);

            return new OwnSettings
            {
                Language = string.IsNullOrWhiteSpace(apt.Settings.Language) ? "ar" : apt.Settings.Language,
                NotificationPreferences = apt.Settings.NotificationPreferences ?? new(),
                PreferredPaymentMethod = string.IsNullOrWhiteSpace(apt.Settings.PreferredPaymentMethod)
                    ? "instapay" : apt.Settings.PreferredPaymentMethod,
                UpdatedAt = apt.Settings.UpdatedAt,
                UpdatedBy = apt.Settings.UpdatedBy
            };
        }

        _logger.LogWarning(">>> GetOwnAsync (resident): apt.Settings is null");
        return new OwnSettings();
    }

    // ✅ helper يدوي لقراءة settings من dict
    private static OwnSettings ParseSettingsFlexible(Dictionary<string, object> settingsDict)
    {
        var notif = new NotificationPreferences();

        if (settingsDict.TryGetValue("notificationPreferences", out var notifRaw) && notifRaw != null)
        {
            var notifDict = ToDict(notifRaw);
            if (notifDict != null)
            {
                notif.WhatsApp = GetBool(notifDict, "whatsapp", true);
                notif.Email = GetBool(notifDict, "email", false);
                notif.InApp = GetBool(notifDict, "inApp", true);
            }
        }

        return new OwnSettings
        {
            Language = GetString(settingsDict, "language", "ar"),
            NotificationPreferences = notif,
            PreferredPaymentMethod = GetString(settingsDict, "preferredPaymentMethod", "instapay"),
            UpdatedAt = GetStringOrNull(settingsDict, "updatedAt"),
            UpdatedBy = GetStringOrNull(settingsDict, "updatedBy")
        };
    }

    // ============================================================
    // 4. Own Settings — الحفظ
    // ============================================================
    public async Task SaveOwnAsync(string uid, OwnSettings settings, string userId, string userRole,
        string? buildingId = null, string? apartmentId = null)
    {
        _logger.LogWarning(">>> SaveOwnAsync START: uid={Uid}, role={Role}, bid={Bid}, aid={Aid}",
            uid, userRole, buildingId, apartmentId);

        settings.UpdatedAt = DateTime.UtcNow.ToString("o");
        settings.UpdatedBy = userId;

        var notifDict = new Dictionary<string, object>
        {
            ["whatsapp"] = settings.NotificationPreferences.WhatsApp,
            ["email"] = settings.NotificationPreferences.Email,
            ["inApp"] = settings.NotificationPreferences.InApp
        };

        var dict = new Dictionary<string, object>
        {
            ["language"] = settings.Language,
            ["notificationPreferences"] = notifDict,
            ["preferredPaymentMethod"] = settings.PreferredPaymentMethod,
            ["updatedAt"] = settings.UpdatedAt,
            ["updatedBy"] = settings.UpdatedBy
        };

        if (userRole == "resident" && !string.IsNullOrEmpty(buildingId) && !string.IsNullOrEmpty(apartmentId))
        {
            await UpdateApartmentSettingsAsync(buildingId, apartmentId, dict);
        }
        else
        {
            await _db.Collection(UsersCol).Document(uid)
                .SetAsync(new Dictionary<string, object> { ["settings"] = dict }, SetOptions.MergeAll);
        }

        await _audit.LogAsync(
            action: "settings.own.update",
            buildingId: buildingId,
            apartmentId: apartmentId,
            userId: userId,
            userRole: userRole,
            metadata: new
            {
                language = settings.Language,
                whatsapp = settings.NotificationPreferences.WhatsApp,
                email = settings.NotificationPreferences.Email,
                inApp = settings.NotificationPreferences.InApp
            },
            severity: "info");
    }

    // ============================================================
    // 5. تحديث بيانات الساكن
    // ============================================================
    public async Task UpdateResidentProfileAsync(
        string buildingId,
        string apartmentId,
        string ownerName,
        string aptLabel,
        string newPhone,
        string? newPin,
        string currentPin,
        string currentUid,
        string userRole)
    {
        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) throw new InvalidOperationException("building-not-found");

        var apt = building.Apartments.FirstOrDefault(a => a.Id == apartmentId);
        if (apt == null) throw new InvalidOperationException("apartment-not-found");

        var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);
        if (floor == null) throw new InvalidOperationException("floor-not-found");

        if (!string.IsNullOrWhiteSpace(newPin))
        {
            if (currentPin != apt.Pin)
                throw new InvalidOperationException(Loc.T("The_Current_PIN_Is_Incorrect"));
        }

        var oldPhone = apt.Phone;
        var oldPin = apt.Pin;
        var phoneChanged = false;
        var pinChanged = false;

        if (!string.IsNullOrWhiteSpace(ownerName))
            apt.Owner = ownerName.Trim();

        if (!string.IsNullOrWhiteSpace(aptLabel))
            apt.Label = aptLabel.Trim();

        if (!string.IsNullOrWhiteSpace(newPhone))
        {
            var normPhone = AuthHelpers.NormalizePhone(newPhone);
            if (normPhone != oldPhone)
            {
                apt.Phone = normPhone;
                phoneChanged = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(newPin))
        {
            if (newPin.Length != 4 || !newPin.All(char.IsDigit))
                throw new InvalidOperationException(Loc.T("PIN_Must_Be_Exactly_4_Digits"));

            apt.Pin = newPin;
            pinChanged = true;
        }

        if (pinChanged)
        {
            var email = AuthHelpers.ResidentInternalEmail(buildingId, floor.Order, apt.Number);
            var password = AuthHelpers.ResidentPassword(buildingId, apt.Number, apt.Pin);

            var uid = await _fbAdmin.GetUidByEmailAsync(email);
            if (!string.IsNullOrEmpty(uid))
            {
                var ok = await _fbAdmin.UpdatePasswordAsync(email, password);
                if (!ok)
                    throw new InvalidOperationException("firebase-auth-update-failed (resident)");
            }
            else
            {
                var created = await _fbAdmin.CreateOrGetUserAsync(email, password);
                if (string.IsNullOrEmpty(created))
                    throw new InvalidOperationException("firebase-auth-create-failed (resident)");
            }
        }

        await _buildings.SaveFullAsync(building);

        await _audit.LogAsync(
            action: "resident.profile_update",
            buildingId: buildingId,
            apartmentId: apartmentId,
            userId: currentUid,
            userRole: userRole,
            metadata: new
            {
                ownerName,
                aptLabel,
                phoneChanged,
                pinChanged,
                oldPhone,
                newPhone = apt.Phone
            },
            severity: "warning");
    }

    // ============================================================
    // 6. تحديث بيانات الأدمن
    // ============================================================
    public async Task UpdateAdminProfileAsync(
        string uid,
        string name,
        string newPhone,
        string newWhatsapp,
        string? newPin,
        string currentPin,
        string userRole)
    {
        var user = await _users.GetByUidAsync(uid);
        if (user == null) throw new InvalidOperationException("admin-not-found");

        var buildingId = user.BuildingIds.FirstOrDefault() ?? "";
        if (string.IsNullOrEmpty(buildingId))
            throw new InvalidOperationException("admin-has-no-building");

        var phoneChangedByUser = !string.IsNullOrWhiteSpace(newPhone) &&
                                 AuthHelpers.NormalizePhone(newPhone) != user.Phone;
        var pinChanged = !string.IsNullOrWhiteSpace(newPin);

        if ((phoneChangedByUser || pinChanged) && currentPin != user.Pin)
            throw new InvalidOperationException(Loc.T("The_Current_PIN_Is_Incorrect"));

        var oldPhone = user.Phone;
        var oldPin = user.Pin;
        var updates = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(name) && name != user.Name)
            updates["name"] = name.Trim();

        var newNormPhone = oldPhone;
        if (phoneChangedByUser)
        {
            newNormPhone = AuthHelpers.NormalizePhone(newPhone);
            updates["phone"] = newNormPhone;
        }

        if (!string.IsNullOrWhiteSpace(newWhatsapp))
        {
            var normWa = AuthHelpers.NormalizePhone(newWhatsapp);
            if (normWa != user.Whatsapp)
                updates["whatsapp"] = normWa;
        }

        var newPinValue = oldPin;
        if (pinChanged)
        {
            if (newPin!.Length != 4 || !newPin.All(char.IsDigit))
                throw new InvalidOperationException(Loc.T("PIN_Must_Be_Exactly_4_Digits"));

            updates["pin"] = newPin;
            newPinValue = newPin;
        }

        if (phoneChangedByUser || pinChanged)
        {
            var derivedOldEmail = AuthHelpers.AdminEmailForPhone(buildingId, oldPhone);
            var derivedNewEmail = phoneChangedByUser
                ? AuthHelpers.AdminEmailForPhone(buildingId, newNormPhone)
                : derivedOldEmail;

            var newPassword = AuthHelpers.AdminPasswordForPhone(newNormPhone, newPinValue);

            _logger.LogInformation(
                "[UpdateAdminProfile] uid={Uid} buildingId={BuildingId} " +
                "derivedOldEmail={OldEmail} storedEmail={StoredEmail} " +
                "phoneChanged={PhoneChanged} pinChanged={PinChanged}",
                uid, buildingId, derivedOldEmail, user.Email ?? "(null)",
                phoneChangedByUser, pinChanged);

            var oldUid = await _fbAdmin.GetUidByEmailAsync(derivedOldEmail);

            if (string.IsNullOrEmpty(oldUid) && !string.IsNullOrWhiteSpace(user.Email))
            {
                oldUid = await _fbAdmin.GetUidByEmailAsync(user.Email);
            }

            if (string.IsNullOrEmpty(oldUid))
            {
                var newUid = await _fbAdmin.CreateOrGetUserAsync(derivedNewEmail, newPassword);
                if (string.IsNullOrEmpty(newUid))
                    throw new InvalidOperationException("firebase-auth-create-failed (admin sdk)");

                updates["email"] = derivedNewEmail;
            }
            else if (phoneChangedByUser)
            {
                var existingNewUid = await _fbAdmin.GetUidByEmailAsync(derivedNewEmail);

                if (string.IsNullOrEmpty(existingNewUid))
                {
                    var created = await _fbAdmin.CreateOrGetUserAsync(derivedNewEmail, newPassword);
                    if (string.IsNullOrEmpty(created))
                        throw new InvalidOperationException("firebase-auth-create-failed (admin sdk)");
                }
                else
                {
                    var upd = await _fbAdmin.UpdatePasswordAsync(derivedNewEmail, newPassword);
                    if (!upd)
                        throw new InvalidOperationException("firebase-auth-update-failed (admin sdk)");
                }

                if (oldUid != existingNewUid)
                    await _fbAdmin.DeleteUserAsync(derivedOldEmail);

                updates["email"] = derivedNewEmail;
            }
            else
            {
                var upd = await _fbAdmin.UpdatePasswordAsync(derivedOldEmail, newPassword);
                if (!upd)
                {
                    var created = await _fbAdmin.CreateOrGetUserAsync(derivedOldEmail, newPassword);
                    if (string.IsNullOrEmpty(created))
                        throw new InvalidOperationException("firebase-auth-update-failed (admin sdk)");
                }

                if (user.Email != derivedOldEmail)
                    updates["email"] = derivedOldEmail;
            }
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
            metadata: new
            {
                nameChanged = !string.IsNullOrWhiteSpace(name) && name != user.Name,
                phoneChanged = phoneChangedByUser,
                whatsappChanged = updates.ContainsKey("whatsapp"),
                pinChanged,
                oldPhone,
                newPhone = newNormPhone
            },
            severity: "warning");
    }

    // ============================================================
    // 7. تحديث اسم السوبر أدمن
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

    // ============================================================
    // Helpers
    // ============================================================
    private async Task UpdateApartmentSettingsAsync(string buildingId, string apartmentId, Dictionary<string, object> settingsDict)
    {
        _logger.LogWarning(">>> UpdateApartmentSettingsAsync START: bid={Bid}, aid={Aid}", buildingId, apartmentId);

        var buildingDoc = await _db.Collection(BuildingsCol).Document(buildingId).GetSnapshotAsync();
        if (!buildingDoc.Exists) throw new InvalidOperationException("building-not-found");

        // ✅ ConvertTo<Building> بدل ToDictionary
        var building = buildingDoc.ConvertTo<Building>();
        if (building == null) throw new InvalidOperationException("building-convert-failed");

        var apt = building.Apartments.FirstOrDefault(a => a.Id == apartmentId);
        if (apt == null) throw new InvalidOperationException("apartment-not-found");

        // ✅ اقرأ القيم من settingsDict
        var newSettings = new UserSettingsDoc
        {
            Language = settingsDict.TryGetValue("language", out var lang)
                ? lang?.ToString() ?? "ar" : "ar",
            PreferredPaymentMethod = settingsDict.TryGetValue("preferredPaymentMethod", out var pay)
                ? pay?.ToString() ?? "instapay" : "instapay",
            UpdatedAt = settingsDict.TryGetValue("updatedAt", out var upd)
                ? upd?.ToString() : null,
            UpdatedBy = settingsDict.TryGetValue("updatedBy", out var updBy)
                ? updBy?.ToString() : null
        };

        // ✅ notificationPreferences
        if (settingsDict.TryGetValue("notificationPreferences", out var notifRaw) && notifRaw != null)
        {
            var notifDict = ToDict(notifRaw);
            if (notifDict != null)
            {
                newSettings.NotificationPreferences = new NotificationPreferences
                {
                    WhatsApp = GetBool(notifDict, "whatsapp", true),
                    Email = GetBool(notifDict, "email", false),
                    InApp = GetBool(notifDict, "inApp", true)
                };
            }
        }

        apt.Settings = newSettings;

        _logger.LogWarning(">>> UpdateApartmentSettingsAsync: saving lang={Lang}, wa={Wa}",
            newSettings.Language, newSettings.NotificationPreferences?.WhatsApp);

        // ✅ حفظ كامل للـ building
        await _db.Collection(BuildingsCol).Document(buildingId)
            .SetAsync(building, SetOptions.Overwrite);

        _logger.LogWarning(">>> UpdateApartmentSettingsAsync DONE");
    }

    // ✅ قوي جدًا — بيشتغل مع كل أنواع الـ Dictionary والـ Map
    private static Dictionary<string, object>? ToDict(object? obj)
    {
        if (obj == null) return null;

        if (obj is Dictionary<string, object> d1) return d1;

        if (obj is IDictionary<string, object> d2)
            return new Dictionary<string, object>(d2);

        if (obj is IReadOnlyDictionary<string, object> d3)
            return new Dictionary<string, object>(d3);

        if (obj is System.Collections.IDictionary d4)
        {
            var r = new Dictionary<string, object>();
            foreach (System.Collections.DictionaryEntry e in d4)
                if (e.Key is string k) r[k] = e.Value!;
            return r;
        }

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(obj);
            var parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (parsed != null) return parsed;
        }
        catch { }

        Console.WriteLine($"[ToDict] Unsupported type: {obj.GetType().FullName}");
        return null;
    }

    private static string GetString(Dictionary<string, object> dict, string key, string def = "")
    {
        if (dict.TryGetValue(key, out var v) && v != null) return v.ToString() ?? def;
        return def;
    }

    private static string? GetStringOrNull(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var v) && v != null) return v.ToString();
        return null;
    }

    private static int GetInt(Dictionary<string, object> dict, string key, int def)
    {
        if (dict.TryGetValue(key, out var v) && v != null && int.TryParse(v.ToString(), out var i)) return i;
        return def;
    }

    private static bool GetBool(Dictionary<string, object> dict, string key, bool def)
    {
        if (dict.TryGetValue(key, out var v) && v != null)
        {
            if (v is bool b) return b;
            var s = v.ToString();
            if (bool.TryParse(s, out var parsed)) return parsed;
            if (s == "1") return true;
            if (s == "0") return false;
        }
        return def;
    }
}