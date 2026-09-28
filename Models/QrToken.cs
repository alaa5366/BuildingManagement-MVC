using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Models;

// نفس شكل مستند qr-tokens/{uid} في Firestore (js/features/qr-token-store.js)
[FirestoreData]
public class QrToken
{
    [FirestoreDocumentId] public string Uid { get; set; } = "";
    [FirestoreProperty("bld")] public string Bld { get; set; } = "";
    [FirestoreProperty("apt")] public string Apt { get; set; } = "";
    [FirestoreProperty("exp")] public long Exp { get; set; } // Unix ms
    [FirestoreProperty("use")] public string Use { get; set; } = "single"; // single | multi
    [FirestoreProperty("ts")] public long Ts { get; set; }
    [FirestoreProperty("by")] public string By { get; set; } = "";
    [FirestoreProperty("used")] public bool Used { get; set; }
    [FirestoreProperty("usedAt")] public string? UsedAt { get; set; }
    [FirestoreProperty("usedBy")] public string? UsedBy { get; set; }
    [FirestoreProperty("createdAt")] public string CreatedAt { get; set; } = "";
}

public class AccessTokenPayload
{
    public string Bld { get; set; } = "";
    public string Apt { get; set; } = "";
    public long Exp { get; set; }
    public string Use { get; set; } = "single";
    public string Uid { get; set; } = "";
    public long Ts { get; set; }
    public string By { get; set; } = "";
    public string Sig { get; set; } = "";
}
