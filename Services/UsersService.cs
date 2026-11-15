using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Business logic for admin and super admin accounts (create, read, update; PINs are stored hashed).
/// </summary>
public class UsersService
{
    private readonly UserSqlStore _sql;
    private readonly ILogger<UsersService> _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersService"/> class.
    /// </summary>
    public UsersService(UserSqlStore sql, ILogger<UsersService> log)
    {
        _sql = sql;
        _log = log;
    }

    // ============================================================
    // قراءة
    // ============================================================
    /// <summary>
    /// Gets the by user ID.
    /// </summary>
    public async Task<AppUserDoc?> GetByUidAsync(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid)) return null;

        var entity = await _sql.GetByUidAsync(uid);
        return entity == null ? null : UserSqlMapper.ToDoc(entity);
    }

    /// <summary>
    /// Gets the all.
    /// </summary>
    public async Task<List<AppUserDoc>> GetAllAsync()
    {
        var users = await _sql.GetAllAsync();
        return users.Select(UserSqlMapper.ToDoc).ToList();
    }

    /// <summary>
    /// Gets the admins.
    /// </summary>
    public async Task<List<AppUserDoc>> GetAdminsAsync()
    {
        var admins = await _sql.GetAdminsAsync();
        return admins.Select(UserSqlMapper.ToDoc).ToList();
    }

    /// <summary>
    /// Gets the building admins.
    /// </summary>
    public async Task<List<AppUserDoc>> GetBuildingAdminsAsync(string buildingId)
    {
        var sqlAdmins = await _sql.GetBuildingAdminsAsync(buildingId);
        return sqlAdmins.Select(UserSqlMapper.ToDoc).ToList();
    }

    // ============================================================
    // كتابة
    // ============================================================
    /// <summary>
    /// Sets the operation on the users.
    /// </summary>
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
                case "pin": doc.Pin = string.IsNullOrEmpty(v) ? v : SecretVerifier.HashIfPlain(v); break;
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

    /// <summary>
    /// Returns the mirror one to SQL.
    /// </summary>
    public async Task<bool> MirrorOneToSqlAsync(AppUserDoc user) =>
        await _sql.UpsertAsync(user);
}