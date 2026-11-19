using System;

namespace BuildingManagementMvc.Data.Entities;

/// <summary>Stable purpose categories for explicit apartment access grants.</summary>
public enum ApartmentAccessGrantType : byte
{
    Family = 1,
    Tenant = 2,
    Caretaker = 3,
    Emergency = 4,
    Temporary = 5
}

/// <summary>States whether an apartment ownership period has a complete, verified share total.</summary>
public enum OwnershipCompletenessStatus : byte
{
    Unknown = 0,
    Incomplete = 1,
    CompleteVerified = 2
}

/// <summary>Identifies how an ownership assertion entered the system.</summary>
public enum OwnershipRecordSource : byte
{
    Human = 0,
    Import = 1,
    System = 2
}

/// <summary>
/// Code-owned, immutable permission profile keys. Profiles intentionally carry no
/// permissions until an approved permission-scope policy is implemented.
/// </summary>
public static class ApartmentAccessPermissionProfileCatalog
{
    public const string Family = "apartment.access.family.v1";
    public const string Tenant = "apartment.access.tenant.v1";
    public const string Caretaker = "apartment.access.caretaker.v1";
    public const string Emergency = "apartment.access.emergency.v1";
    public const string Temporary = "apartment.access.temporary.v1";

    /// <summary>Gets the immutable versioned profile key for a grant type.</summary>
    /// <param name="grantType">The approved apartment access grant type.</param>
    /// <returns>The code-owned profile key associated with the grant type.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The grant type is not defined.</exception>
    public static string For(ApartmentAccessGrantType grantType) => grantType switch
    {
        ApartmentAccessGrantType.Family => Family,
        ApartmentAccessGrantType.Tenant => Tenant,
        ApartmentAccessGrantType.Caretaker => Caretaker,
        ApartmentAccessGrantType.Emergency => Emergency,
        ApartmentAccessGrantType.Temporary => Temporary,
        _ => throw new ArgumentOutOfRangeException(nameof(grantType), grantType, "Unknown apartment access grant type.")
    };

    /// <summary>Checks whether a profile key is known and paired with the supplied grant type.</summary>
    /// <param name="grantType">The apartment access grant type.</param>
    /// <param name="permissionSetKey">The proposed versioned profile key.</param>
    /// <returns><see langword="true"/> when the type and key match; otherwise <see langword="false"/>.</returns>
    public static bool IsValidFor(ApartmentAccessGrantType grantType, string? permissionSetKey) =>
        permissionSetKey is not null &&
        Enum.IsDefined(grantType) &&
        string.Equals(For(grantType), permissionSetKey, StringComparison.Ordinal);
}

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
    public bool MustChangePin { get; set; }
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
    public OwnershipCompletenessStatus CompletenessStatus { get; set; }
    public OwnershipRecordSource RecordSource { get; set; }
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Guid? RecordedByAccountId { get; set; }
    public string EvidenceReference { get; set; } = "";
    public DateTime? CompletenessVerifiedAtUtc { get; set; }
    public Guid? CompletenessVerifiedByAccountId { get; set; }
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
    public ApartmentAccessGrantType GrantType { get; set; }
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
