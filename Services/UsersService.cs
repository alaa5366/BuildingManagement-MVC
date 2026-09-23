using Google.Cloud.Firestore;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/services/users-service.js
public class UsersService
{
    private readonly FirestoreDb _db;

    public UsersService(FirestoreContext ctx) => _db = ctx.Db;

    private CollectionReference Col => _db.Collection("users");

    public async Task<AppUserDoc?> GetByUidAsync(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid)) return null;
        var doc = await Col.Document(uid).GetSnapshotAsync();
        return doc.Exists ? doc.ConvertTo<AppUserDoc>() : null;
    }

    public async Task SetAsync(string uid, Dictionary<string, object> data) =>
        await Col.Document(uid).SetAsync(data, SetOptions.MergeAll);

    public async Task<List<AppUserDoc>> GetAdminsAsync()
    {
        var snap = await Col.WhereEqualTo("role", "admin").GetSnapshotAsync();
        return snap.Documents.Select(d => d.ConvertTo<AppUserDoc>()).ToList();
    }

    public async Task<List<AppUserDoc>> GetBuildingAdminsAsync(string buildingId)
    {
        var admins = await GetAdminsAsync();
        return admins.Where(a => a.BuildingIds.Contains(buildingId)).ToList();
    }
}
