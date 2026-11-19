namespace BuildingManagementMvc.Data.Entities;

    /// <summary> Represents the QrCapabilityEntity data or service contract. </summary>
public sealed class QrCapabilityEntity
{
    public Guid CapabilityId { get; set; }
    public string BuildingId { get; set; } = "";
    public string? ApartmentId { get; set; }
    public byte TargetType { get; set; }
    public string TargetReference { get; set; } = "";
    public byte[] TokenHash { get; set; } = Array.Empty<byte>();
    public DateTime IssuedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public int MaxUses { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokedByReference { get; set; }
}

    /// <summary> Represents the QrCapabilityUseEntity data or service contract. </summary>
public sealed class QrCapabilityUseEntity
{
    public Guid UseId { get; set; }
    public Guid CapabilityId { get; set; }
    public DateTime UsedAtUtc { get; set; }
    public string? UseReference { get; set; }
    public byte Outcome { get; set; }
}
