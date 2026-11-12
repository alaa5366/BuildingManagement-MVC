using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

public class UsersService
{
    private readonly UserSqlStore _sql;
    private readonly ILogger<UsersService> _log;

    public UsersService(UserSqlStore sql, ILogger<UsersService> log)
    {
        _sql = sql;
        _log = log;
    }

    // ============================================================
    // قراءة
    // ============================================================
    public async Task<AppUserDoc?> GetByUidAsync(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid)) return null;

        var entity = await _sql.GetByUidAsync(uid);
        return entity == null ? null : UserSqlMapper.ToDoc(entity);
    }

    public async Task<List<AppUserDoc>> GetAllAsync()
    {
        var users = await _sql.GetAllAsync();
        return users.Select(UserSqlMapper.ToDoc).ToList();
    }

    public async Task<List<AppUserDoc>> GetAdminsAsync()
    {
        var admins = await _sql.GetAdminsAsync();
        return admins.Select(UserSqlMapper.ToDoc).ToList();
    }

    public async Task<List<AppUserDoc>> GetBuildingAdminsAsync(string buildingId)
    {
        var sqlAdmins = await _sql.GetBuildingAdminsAsync(buildingId);
        return sqlAdmins.Select(UserSqlMapper.ToDoc).ToList();
    }

    // ============================================================
    // كتابة
    // ============================================================
    public async Task SetAsync(string uid, Dictionary<string, object> data)
    {
        var existing = await _sql.GetByUidAsync(uid);
        var doc = existing != null ? UserSqlMapper.ToDoc(existing) : new AppUserDoc { Uid = uid };

        // طبّق التعديلات
        foreach (var kv in data)
        {
            var v = kv.Value?.ToString() ?? "";
            switch (kv.Key)
            {
                case "email": doc.Email = v; break;
                case "name": doc.Name = v; break;
                case "phone": doc.Phone = v; break;
                case "whatsapp": doc.Whatsapp = v; break;
                case "pin": doc.Pin = v; break;
                case "photoUrl": doc.PhotoUrl = v; break;
                case "role": doc.Role = v; break;
                case "isActive": doc.IsActive = kv.Value is bool ib && ib; break;
                case "disabled": doc.Disabled = kv.Value is bool db && db; break;
                case "disabledReason": doc.DisabledReason = v; break;
                case "lastLoginAt": doc.LastLoginAt = v; break;
                case "createdAt": doc.CreatedAt = v; break;
                case "createdBy": doc.CreatedBy = v; break;
                case "updatedAt": doc.UpdatedAt = v; break;
                case "updatedBy": doc.UpdatedBy = v; break;
                case "permissions":
                    if (kv.Value is List<string> perms) doc.Permissions = perms;
                    break;
                case "buildingIds":
                    if (kv.Value is List<string> bids) doc.BuildingIds = bids;
                    break;
            }
        }

        await _sql.UpsertAsync(doc);
    }

    public async Task<bool> MirrorOneToSqlAsync(AppUserDoc user) =>
        await _sql.UpsertAsync(user);
}