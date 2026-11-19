﻿namespace BuildingManagementMvc.Data.Entities;

/// <summary>Canonical application-wide settings stored separately from legacy operational configuration.</summary>
public sealed class GlobalApplicationSettingsEntity
{
    public string SettingsId { get; set; } = "global";
    public string AppName { get; set; } = "نظام إدارة العمارات";
    public string DefaultLanguage { get; set; } = "ar";
    public bool EnablePolls { get; set; } = true;
    public bool EnableMaintenance { get; set; } = true;
    public bool EnableOcr { get; set; } = true;
    public bool EnableQrAccess { get; set; } = true;
    public bool EnableWhatsApp { get; set; } = true;
    public int SessionTimeoutDays { get; set; } = 7;
    public int MaxPinAttempts { get; set; } = 5;
    public string StorageMode { get; set; } = "local";
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedByReference { get; set; }
    public DateTime ImportedAtUtc { get; set; }
}

/// <summary>Encrypted reference to an integration secret; plaintext is never persisted in settings rows.</summary>
public sealed class ProtectedIntegrationSecretEntity
{
    public Guid SecretId { get; set; }
    public byte ScopeType { get; set; }
    public string ScopeReference { get; set; } = "";
    public string Provider { get; set; } = "";
    public string SecretName { get; set; } = "";
    public byte[] Ciphertext { get; set; } = Array.Empty<byte>();
    public string KeyVersion { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RotatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
