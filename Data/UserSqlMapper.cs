// =====================================================================
//  UserSqlMapper - تحويل بين AppUserDoc (Firestore) و UserEntity (SQL)
// =====================================================================
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Data;

public static class UserSqlMapper
{
    public static UserEntity ToEntity(AppUserDoc u)
    {
        return new UserEntity
        {
            Uid = BuildingSqlMapper.Cut(u.Uid, 128),
            Email = BuildingSqlMapper.Cut(u.Email, 256),
            Name = BuildingSqlMapper.Cut(u.Name, 200),
            Phone = BuildingSqlMapper.Cut(u.Phone, 32),
            Whatsapp = BuildingSqlMapper.Cut(u.Whatsapp, 32),
            Pin = BuildingSqlMapper.Cut(u.Pin, 256),
            PhotoUrl = BuildingSqlMapper.Cut(u.PhotoUrl, 1000),
            Role = u.Role,
            IsDisabled = u.Disabled,
            DisabledReason = BuildingSqlMapper.Cut(u.DisabledReason, 500),
            IsActive = u.IsActive,
            LastLoginAt = BuildingSqlMapper.ParseTs(u.LastLoginAt),
            CreatedAt = BuildingSqlMapper.ParseTs(u.CreatedAt),
            CreatedBy = u.CreatedBy == null ? null : BuildingSqlMapper.Cut(u.CreatedBy, 128),
            UpdatedAt = BuildingSqlMapper.ParseTs(u.UpdatedAt),
            UpdatedBy = u.UpdatedBy == null ? null : BuildingSqlMapper.Cut(u.UpdatedBy, 128)
        };
    }

    public static AppUserDoc ToDoc(UserEntity e)
    {
        return new AppUserDoc
        {
            Uid = e.Uid,
            Email = e.Email,
            Name = e.Name,
            Phone = e.Phone,
            Whatsapp = e.Whatsapp,
            Pin = e.Pin,
            PhotoUrl = e.PhotoUrl,
            Role = e.Role,
            Disabled = e.IsDisabled,
            DisabledReason = e.DisabledReason,
            IsActive = e.IsActive,
            LastLoginAt = e.LastLoginAt?.ToString("o"),
            CreatedAt = e.CreatedAt?.ToString("o"),
            CreatedBy = e.CreatedBy,
            UpdatedAt = e.UpdatedAt?.ToString("o"),
            UpdatedBy = e.UpdatedBy,
            Permissions = e.Permissions?.Select(p => p.Permission).ToList() ?? new(),
            BuildingIds = e.BuildingAdmins?.Select(ba => ba.BuildingId).ToList() ?? new()
        };
    }
}