using System;

namespace BuildingManagementMvc.Data.Entities;

/// <summary>Canonical human identity, independent from login credentials and apartment relationships.</summary>
public sealed class PersonEntity
{
    public Guid PersonId { get; set; }
    public string? LegalName { get; set; }
    public string DisplayName { get; set; } = "";
    public string? PrimaryEmail { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? Whatsapp { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>Interactive account. One account at most per person.</summary>
public sealed class UserAccountEntity
{
    public Guid AccountId { get; set; }
    public Guid PersonId { get; set; }
    public byte AccountStatus { get; set; }
    public string? NormalizedEmail { get; set; }
    public string? ExternalProvider { get; set; }
    public string? ExternalSubject { get; set; }
    public string SecurityStamp { get; set; } = "";
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>Versioned account credential material; only hashes are stored.</summary>
public sealed class UserCredentialEntity
{
    public Guid CredentialId { get; set; }
    public Guid AccountId { get; set; }
    public byte CredentialType { get; set; }
    public string CredentialHash { get; set; } = "";
    public string HashAlgorithm { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? ReplacedByCredentialId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>Account MFA state and encrypted authenticator secret.</summary>
public sealed class UserTwoFactorSettingEntity
{
    public Guid AccountId { get; set; }
    public bool IsEnabled { get; set; }
    public byte[]? SecretCiphertext { get; set; }
    public string? KeyVersion { get; set; }
    public long? LastAcceptedTimeStep { get; set; }
    public DateTime? EnrolledAtUtc { get; set; }
    public DateTime? LastVerifiedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>Single-use recovery code hashes.</summary>
public sealed class UserRecoveryCodeEntity
{
    public Guid RecoveryCodeId { get; set; }
    public Guid AccountId { get; set; }
    public byte[] CodeHash { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
}

/// <summary>Defines a stable application role and whether it is a system-managed role.</summary>
public sealed class RoleDefinitionEntity
{
    public string RoleKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Associates a role with an explicit permission effect.</summary>
public sealed class RolePermissionEntity
{
    public string RoleKey { get; set; } = "";
    public string PermissionKey { get; set; } = "";
    public byte Effect { get; set; }
}

/// <summary>Time-bounded role assignment with explicit scope and grant/revocation provenance.</summary>
public sealed class UserRoleAssignmentEntity
{
    public Guid RoleAssignmentId { get; set; }
    public Guid AccountId { get; set; }
    public string RoleKey { get; set; } = "";
    public byte ScopeType { get; set; }
    public string? ScopeBuildingId { get; set; }
    public string? ScopeApartmentId { get; set; }
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public DateTime GrantedAtUtc { get; set; }
    public Guid? GrantedByAccountId { get; set; }
    public string? GrantReason { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? RevokedByAccountId { get; set; }
    public string? RevocationReason { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>Evidence-backed ownership history; percentage aggregation is enforced by domain transactions.</summary>
public sealed class ApartmentOwnershipEntity
{
    public Guid OwnershipId { get; set; }
    public string BuildingId { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public Guid PersonId { get; set; }
    public decimal OwnershipSharePercent { get; set; }
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Guid? RecordedByAccountId { get; set; }
    public string EvidenceReference { get; set; } = "";
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>Effective-dated occupancy relationship.</summary>
public sealed class ApartmentOccupancyEntity
{
    public Guid OccupancyId { get; set; }
    public string BuildingId { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public Guid PersonId { get; set; }
    public byte OccupancyType { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Guid? RecordedByAccountId { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>Explicit temporary or permanent apartment access grant.</summary>
public sealed class ApartmentAccessGrantEntity
{
    public Guid GrantId { get; set; }
    public string BuildingId { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public Guid PersonId { get; set; }
    public byte GrantType { get; set; }
    public string PermissionSetKey { get; set; } = "";
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public DateTime GrantedAtUtc { get; set; }
    public Guid GrantedByAccountId { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? RevokedByAccountId { get; set; }
    public string Reason { get; set; } = "";
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>Explicit account-level allow/deny override, always evaluated within its declared scope.</summary>
public sealed class AccountPermissionOverrideEntity
{
    public Guid OverrideId { get; set; }
    public Guid AccountId { get; set; }
    public string PermissionKey { get; set; } = "";
    public byte Effect { get; set; }
    public byte ScopeType { get; set; }
    public string? ScopeBuildingId { get; set; }
    public string? ScopeApartmentId { get; set; }
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public DateTime GrantedAtUtc { get; set; }
    public Guid? GrantedByAccountId { get; set; }
    public string? GrantReason { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? RevokedByAccountId { get; set; }
    public string? RevocationReason { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>Auditable source mapping; unresolved legacy identities remain explicitly unresolved.</summary>
public sealed class LegacyIdentityMapEntity
{
    public Guid LegacyIdentityMapId { get; set; }
    public string SourceSystem { get; set; } = "";
    public string SourceTable { get; set; } = "";
    public string SourceKey { get; set; } = "";
    public Guid? PersonId { get; set; }
    public Guid? AccountId { get; set; }
    public byte MappingStatus { get; set; }
    public string? MatchMethod { get; set; }
    public string? EvidenceReference { get; set; }
    public string? ReviewReason { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByAccountId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
