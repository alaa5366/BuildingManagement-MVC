namespace BuildingManagementMvc.Models;

public class QrToken
{
    public string Uid { get; set; } = "";
    public string Bld { get; set; } = "";
    public string Apt { get; set; } = "";
    public long Exp { get; set; }
    public string Use { get; set; } = "single";
    public long Ts { get; set; }
    public string By { get; set; } = "";
    public bool Used { get; set; }
    public string? UsedAt { get; set; }
    public string? UsedBy { get; set; }
    public string CreatedAt { get; set; } = "";
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