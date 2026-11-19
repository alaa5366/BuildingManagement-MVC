using System;

namespace BuildingManagementMvc.Data.Entities;

/// <summary>Resident-submitted phone correction awaiting independent administrator verification.</summary>
public sealed class PersonPhoneChangeRequestEntity
{
    public Guid RequestId { get; set; }
    public Guid PersonId { get; set; }
    public string BuildingId { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public string PreviousPhone { get; set; } = "";
    public string RequestedPhone { get; set; } = "";
    public byte Status { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewReason { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}