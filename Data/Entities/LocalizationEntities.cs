using System.ComponentModel.DataAnnotations;

namespace BuildingManagementMvc.Data.Entities;

public sealed class LanguageEntity
{
    [Key, MaxLength(20)] public string CultureCode { get; set; } = "";
    [Required, MaxLength(100)] public string EnglishName { get; set; } = "";
    [Required, MaxLength(100)] public string NativeName { get; set; } = "";
    [MaxLength(12)] public string Direction { get; set; } = "LTR";
    [MaxLength(20)] public string? FallbackCulture { get; set; }
    [MaxLength(8)] public string? Flag { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    [MaxLength(128)] public string? UpdatedBy { get; set; }
}

public sealed class TranslationEntryEntity
{
    [Key, MaxLength(200)] public string Key { get; set; } = "";
    [MaxLength(40)] public string Category { get; set; } = "System";
    [MaxLength(160)] public string SourceGroup { get; set; } = "Shared";
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsArchived { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    [MaxLength(128)] public string? UpdatedBy { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}

public sealed class TranslationValueEntity
{
    [MaxLength(200)] public string Key { get; set; } = "";
    [MaxLength(20)] public string CultureCode { get; set; } = "";
    [Required, MaxLength(4000)] public string Value { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    [MaxLength(128)] public string? UpdatedBy { get; set; }
}
