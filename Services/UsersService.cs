using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/services/users-service.js
// ✅ معدّل: بيقرأ وضع التخزين ديناميكيًا
public class UsersService
{
    private readonly FirestoreDb _db;
    private readonly ILogger<UsersService> _log;
    private readonly UserSqlStore? _sql;
    private readonly StorageSettingsService _storageSettings;
    private readonly IDbContextFactory<AppDbContext>? _sqlFactory;

    public UsersService(
        FirestoreContext ctx,
        ILogger<UsersService> log,
        UserSqlStore? sql,
        StorageSettingsService storageSettings,
        IDbContextFactory<AppDbContext>? sqlFactory = null)
    {
        _db = ctx.Db;
        _log = log;
        _sql = sql;
        _storageSettings = storageSettings;
        _sqlFactory = sqlFactory;
    }

    private CollectionReference Col => _db.Collection("users");

    private async Task<string> GetModeAsync() => await _storageSettings.GetModeAsync();

    private async Task RecordChangeAsync(string uid, string operation, object? payload = null)
    {
        if (_sqlFactory == null) return;
        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();
            db.SyncPendingChanges.Add(new Data.Entities.SyncPendingChangeEntity
            {
                EntityType = "User",
                EntityId = uid,
                Operation = operation,
                Payload = payload == null ? null : JsonSerializer.Serialize(payload),
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to record user change {Uid}", uid);
        }
    }

    // ============================================================
    //  قراءة
    // ============================================================
    public async Task<AppUserDoc?> GetByUidAsync(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid)) return null;

        var mode = await GetModeAsync();

        if (mode == "Sql" && _sql != null)
        {
            var entity = await _sql.GetByUidAsync(uid);
            return entity == null ? null : UserSqlMapper.ToDoc(entity);
        }

        // Firestore أو Dual
        var doc = await Col.Document(uid).GetSnapshotAsync();
        return doc.Exists ? doc.ConvertTo<AppUserDoc>() : null;
    }

    public async Task<List<AppUserDoc>> GetAllAsync()
    {
        var mode = await GetModeAsync();

        if (mode == "Sql" && _sql != null)
        {
            var users = await _sql.GetAllAsync();
            return users.Select(UserSqlMapper.ToDoc).ToList();
        }

        var snap = await Col.GetSnapshotAsync();
        return snap.Documents.Select(d => d.ConvertTo<AppUserDoc>()).ToList();
    }

    public async Task<List<AppUserDoc>> GetAdminsAsync()
    {
        var mode = await GetModeAsync();

        if (mode == "Sql" && _sql != null)
        {
            var admins = await _sql.GetAdminsAsync();
            return admins.Select(UserSqlMapper.ToDoc).ToList();
        }

        var snap = await Col.WhereEqualTo("role", "admin").GetSnapshotAsync();
        return snap.Documents.Select(d => d.ConvertTo<AppUserDoc>()).ToList();
    }

    public async Task<List<AppUserDoc>> GetBuildingAdminsAsync(string buildingId)
    {
        var mode = await GetModeAsync();

        if (mode == "Sql" && _sql != null)
        {
            var sqlAdmins = await _sql.GetBuildingAdminsAsync(buildingId);
            return sqlAdmins.Select(UserSqlMapper.ToDoc).ToList();
        }

        var admins = await GetAdminsAsync();
        return admins.Where(a => a.BuildingIds.Contains(buildingId)).ToList();
    }

    // ============================================================
    //  كتابة
    // ============================================================
    public async Task SetAsync(string uid, Dictionary<string, object> data)
    {
        var mode = await GetModeAsync();

        // 1. Firestore
        if (mode == "Firestore" || mode == "Dual")
        {
            await Col.Document(uid).SetAsync(data, SetOptions.MergeAll);
        }

        // 2. SQL (Dual و Sql)
        if ((mode == "Sql" || mode == "Dual") && _sql != null)
        {
            try
            {
                // في وضع Sql، بنبني من data مباشرة
                // في وضع Dual، بنقرأ من Firestore عشان نضمن التناسق
                AppUserDoc? userDoc;
                if (mode == "Sql")
                {
                    // حمّل من SQL، وطبق التعديلات
                    var existing = await _sql.GetByUidAsync(uid);
                    var baseDoc = existing != null ? UserSqlMapper.ToDoc(existing) : new AppUserDoc { Uid = uid };
                    // طبّق التعديلات من data (بسيط، ممكن يحتاج تحسين)
                    await _sql.UpsertAsync(baseDoc);
                    userDoc = baseDoc;
                }
                else
                {
                    var doc = await Col.Document(uid).GetSnapshotAsync();
                    userDoc = doc.Exists ? doc.ConvertTo<AppUserDoc>() : null;
                }

                if (userDoc != null)
                {
                    await _sql.UpsertAsync(userDoc);
                    if (mode == "Sql")
                        await RecordChangeAsync(uid, "Update", userDoc);
                }
            }
            catch (Exception ex)
            {
                SqlMirrorHealth.RecordFailure($"user {uid}", ex);
                _log.LogError(ex, "SQL write failed for user {Uid}", uid);
                if (mode == "Sql") throw;
            }
        }
    }

    // ============================================================
    //  مرآة SQL (للمزامنة اليدوية)
    // ============================================================
    public async Task MirrorToSqlAsync(IEnumerable<string>? uids)
    {
        if (_sql == null || uids == null) return;

        foreach (var uid in uids.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct())
        {
            try
            {
                var doc = await Col.Document(uid).GetSnapshotAsync();
                if (doc.Exists)
                    await _sql.UpsertAsync(doc.ConvertTo<AppUserDoc>());
            }
            catch (Exception ex)
            {
                SqlMirrorHealth.RecordFailure($"user {uid}", ex);
                _log.LogError(ex, "SQL mirror failed for user {Uid}", uid);
            }
        }
    }

    public async Task<bool> MirrorOneToSqlAsync(AppUserDoc user) =>
        _sql != null && await _sql.UpsertAsync(user);

    /// <summary>
    /// ✅ للمزامنة اليدوية: اقرأ كل المستخدمين من Firestore مباشرة
    /// (بتستخدمها SyncController.SeedUsersToSql)
    /// </summary>
    public async Task<List<AppUserDoc>> GetAllFromFirestoreAsync()
    {
        var snap = await Col.GetSnapshotAsync();
        return snap.Documents.Select(d => d.ConvertTo<AppUserDoc>()).ToList();
    }
}