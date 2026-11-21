namespace BuildingManagementMvc.Models;

    /// <summary>Represents the complete data set displayed by the identity governance console.</summary>
public sealed class IdentityGovernancePage
{
    public int PersonCount { get; set; }
    public int AccountCount { get; set; }
    public int ActiveOccupancyCount { get; set; }
    public int ActiveRoleAssignmentCount { get; set; }
    public int PendingPhoneChangeCount { get; set; }
    public string SearchTerm { get; set; } = "";
    public List<IdentityGovernancePersonRow> Persons { get; set; } = new();
    public List<IdentityGovernanceOccupancyRow> Occupancies { get; set; } = new();
    public List<IdentityGovernanceRoleRow> RoleAssignments { get; set; } = new();
    public List<IdentityGovernanceAccountRow> Accounts { get; set; } = new();
    public List<IdentityGovernanceCatalogRow> RoleCatalog { get; set; } = new();
    public List<IdentityGovernanceCatalogRow> PermissionCatalog { get; set; } = new();
    public List<IdentityGovernanceSecurityRow> Security { get; set; } = new();
    public List<IdentityGovernanceCatalogRow> PermissionOverrides { get; set; } = new();
    public List<IdentityGovernanceCatalogRow> ApartmentAccessGrants { get; set; } = new();
    public List<IdentityGovernanceCatalogRow> OwnershipRecords { get; set; } = new();
    public List<IdentityGovernanceCatalogRow> LegacyMappings { get; set; } = new();
    public List<IdentityGovernanceCatalogRow> PhoneChangeRequests { get; set; } = new();
    public List<IdentityGovernanceCatalogRow> PresenceSessions { get; set; } = new();
    public List<IdentityGovernanceCatalogRow> AuditEvents { get; set; } = new();
    public List<IdentityGovernanceCatalogRow> Buildings { get; set; } = new();
    public List<IdentityGovernanceCatalogRow> Apartments { get; set; } = new();
}

    /// <summary>Represents a person and their associated account, occupancy and role summary.</summary>
public sealed class IdentityGovernancePersonRow
{
    public Guid PersonId { get; set; }
    public string DisplayName { get; set; } = "";
    public string? LegalName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsArchived { get; set; }
    public Guid? AccountId { get; set; }
    public byte? AccountStatus { get; set; }
    public int ActiveOccupancies { get; set; }
    public int ActiveRoles { get; set; }
}

    /// <summary>Represents account status and non-secret credential and security metadata.</summary>
public sealed class IdentityGovernanceAccountRow
{
    public Guid AccountId { get; set; }
    public Guid PersonId { get; set; }
    public string PersonName { get; set; } = "";
    public byte Status { get; set; }
    public bool MustChangePin { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public int ActiveCredentials { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public int UnusedRecoveryCodes { get; set; }
    public int PreferenceCount { get; set; }
}

    /// <summary>Represents an apartment occupancy and its effective period.</summary>
public sealed class IdentityGovernanceOccupancyRow
{
    public Guid OccupancyId { get; set; }
    public Guid PersonId { get; set; }
    public string PersonName { get; set; } = "";
    public string BuildingId { get; set; } = "";
    public string BuildingName { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public string ApartmentNumber { get; set; } = "";
    public byte OccupancyType { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public bool IsCurrent { get; set; }
}

    /// <summary>Represents a scoped role assignment and its effective or revoked state.</summary>
public sealed class IdentityGovernanceRoleRow
{
    public Guid AssignmentId { get; set; }
    public Guid AccountId { get; set; }
    public string PersonName { get; set; } = "";
    public string RoleKey { get; set; } = "";
    public byte ScopeType { get; set; }
    public string? BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public string? ApartmentId { get; set; }
    public string? ApartmentNumber { get; set; }
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? GrantReason { get; set; }
}

    /// <summary>Represents account security counts without exposing credential secrets.</summary>
public sealed class IdentityGovernanceSecurityRow
{
    public Guid AccountId { get; set; }
    public string PersonName { get; set; } = "";
    public int CredentialCount { get; set; }
    public int ActiveCredentialCount { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public int RecoveryCodeCount { get; set; }
    public int UnusedRecoveryCodeCount { get; set; }
}

    /// <summary>Represents a read-only catalog or governance inventory entry.</summary>
public sealed class IdentityGovernanceCatalogRow
{
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Status { get; set; } = "";
}
