namespace BuildingManagementMvc.Data.Entities;

    /// <summary> Represents the MediaDeviceEntity data or service contract. </summary>
public sealed class MediaDeviceEntity
{
    public Guid MediaDeviceId { get; set; }
    public string BuildingId { get; set; } = "";
    public string Name { get; set; } = "";
    public string HostAddress { get; set; } = "";
    public int Port { get; set; }
    public string Brand { get; set; } = "";
    public string Username { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

    /// <summary> Represents the MediaDeviceChannelEntity data or service contract. </summary>
public sealed class MediaDeviceChannelEntity
{
    public Guid ChannelId { get; set; }
    public Guid MediaDeviceId { get; set; }
    public string BuildingId { get; set; } = "";
    public string Name { get; set; } = "";
    public int ChannelNumber { get; set; }
    public string RtspPath { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
