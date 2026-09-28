using Google.Cloud.Firestore;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/features/qr-token-store.js (تخزين توكنات QR في Firestore)
public class QrTokenStoreService
{
    private readonly FirestoreDb _db;
    public QrTokenStoreService(FirestoreContext ctx) => _db = ctx.Db;

    private CollectionReference Col => _db.Collection("qr-tokens");

    public async Task<bool> SaveTokenAsync(AccessTokenPayload payload)
    {
        try
        {
            var token = new QrToken
            {
                Uid = payload.Uid,
                Bld = payload.Bld,
                Apt = payload.Apt,
                Exp = payload.Exp,
                Use = payload.Use,
                Ts = payload.Ts,
                By = payload.By,
                Used = false,
                CreatedAt = DateTime.UtcNow.ToString("o")
            };
            await Col.Document(payload.Uid).SetAsync(token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // التحقق من التوكن وتسجيل الاستخدام (بدون بصمة جهاز — نسخة السيرفر أبسط من الأصل)
    public async Task<(bool Allowed, string? Reason, string? Bld, string? Apt)> CheckAndUseTokenAsync(string uid)
    {
        var docRef = Col.Document(uid);
        var snap = await docRef.GetSnapshotAsync();
        if (!snap.Exists) return (false, "not-found", null, null);

        var data = snap.ConvertTo<QrToken>();

        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > data.Exp)
            return (false, "expired", null, null);

        if (data.Use == "single" && data.Used)
            return (false, "already-used", null, null);

        await docRef.SetAsync(new Dictionary<string, object>
        {
            ["used"] = true,
            ["usedAt"] = DateTime.UtcNow.ToString("o")
        }, SetOptions.MergeAll);

        return (true, null, data.Bld, data.Apt);
    }

    public async Task DeleteTokenAsync(string uid) => await Col.Document(uid).DeleteAsync();
}
