using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

/// <summary>
/// EF Core mapping for the additive identity and authorization foundation.
/// Kept separate from legacy mappings so the new bounded context remains reviewable.
/// </summary>
internal static class IdentityModelConfiguration
{
    public static void Configure(ModelBuilder mb)
    {
        mb.Entity<PersonEntity>(e =>
        {
            e.ToTable("Persons", t => t.HasCheckConstraint("CK_Persons_ArchiveState",
                "(([IsArchived] = 0 AND [ArchivedAtUtc] IS NULL) OR ([IsArchived] = 1 AND [ArchivedAtUtc] IS NOT NULL))"));
            e.HasKey(x => x.PersonId);
            e.Property(x => x.PersonId).ValueGeneratedNever();
            e.Property(x => x.LegalName).HasMaxLength(200);
            e.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
            e.Property(x => x.PrimaryEmail).HasMaxLength(254);
            e.Property(x => x.PrimaryPhone).HasMaxLength(32);
            e.Property(x => x.Whatsapp).HasMaxLength(32);
            e.Property(x => x.PhotoUrl).HasMaxLength(2048);
            Utc3(e.Property(x => x.ArchivedAtUtc));
            Utc3(e.Property(x => x.CreatedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            Utc3(e.Property(x => x.UpdatedAtUtc));
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasIndex(x => x.PrimaryEmail).HasDatabaseName("IX_Persons_PrimaryEmail").HasFilter("[PrimaryEmail] IS NOT NULL");
            e.HasIndex(x => x.PrimaryPhone).HasDatabaseName("IX_Persons_PrimaryPhone").HasFilter("[PrimaryPhone] IS NOT NULL");
        });

        mb.Entity<UserAccountEntity>(e =>
        {
            e.ToTable("UserAccounts", t =>
            {
                t.HasCheckConstraint("CK_UserAccounts_AccountStatus", "[AccountStatus] IN (0,1,2,3)");
                t.HasCheckConstraint("CK_UserAccounts_ExternalLoginPair",
                    "(([ExternalProvider] IS NULL AND [ExternalSubject] IS NULL) OR ([ExternalProvider] IS NOT NULL AND [ExternalSubject] IS NOT NULL))");
            });
            e.HasKey(x => x.AccountId);
            e.Property(x => x.AccountId).ValueGeneratedNever();
            e.Property(x => x.NormalizedEmail).HasMaxLength(254);
            e.Property(x => x.ExternalProvider).HasMaxLength(40);
            e.Property(x => x.ExternalSubject).HasMaxLength(200);
            e.Property(x => x.SecurityStamp).HasMaxLength(128).IsRequired();
            Utc3(e.Property(x => x.LastLoginAtUtc));
            Utc3(e.Property(x => x.CreatedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<PersonEntity>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.PersonId).IsUnique().HasDatabaseName("UQ_UserAccounts_PersonId");
            e.HasIndex(x => new { x.ExternalProvider, x.ExternalSubject }).IsUnique()
                .HasDatabaseName("UX_UserAccounts_ExternalLogin")
                .HasFilter("[ExternalProvider] IS NOT NULL AND [ExternalSubject] IS NOT NULL");
            e.HasIndex(x => x.NormalizedEmail).HasDatabaseName("IX_UserAccounts_NormalizedEmail")
                .HasFilter("[NormalizedEmail] IS NOT NULL");
        });

        mb.Entity<UserCredentialEntity>(e =>
        {
            e.ToTable("UserCredentials", t =>
            {
                t.HasCheckConstraint("CK_UserCredentials_Type", "[CredentialType] IN (0,1)");
                t.HasCheckConstraint("CK_UserCredentials_Expiry", "[ExpiresAtUtc] IS NULL OR [ExpiresAtUtc] > [CreatedAtUtc]");
            });
            e.HasKey(x => x.CredentialId);
            e.Property(x => x.CredentialId).ValueGeneratedNever();
            e.Property(x => x.CredentialHash).HasMaxLength(512).IsRequired();
            e.Property(x => x.HashAlgorithm).HasMaxLength(40).IsRequired();
            Utc3(e.Property(x => x.CreatedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            Utc3(e.Property(x => x.ExpiresAtUtc));
            Utc3(e.Property(x => x.RevokedAtUtc));
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasAlternateKey(x => new { x.CredentialId, x.AccountId }).HasName("UQ_UserCredentials_Credential_Account");
            e.HasOne<UserCredentialEntity>().WithMany()
                .HasForeignKey(x => new { x.ReplacedByCredentialId, x.AccountId })
                .HasPrincipalKey(x => new { x.CredentialId, x.AccountId })
                .OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.AccountId, x.CredentialType, x.RevokedAtUtc })
                .HasDatabaseName("IX_UserCredentials_Account_Type");
        });

        mb.Entity<UserTwoFactorSettingEntity>(e =>
        {
            e.ToTable("UserTwoFactorSettings", t => t.HasCheckConstraint("CK_UserTwoFactorSettings_EnabledSecret",
                "(([IsEnabled] = 0 AND [SecretCiphertext] IS NULL AND [KeyVersion] IS NULL) OR ([IsEnabled] = 1 AND [SecretCiphertext] IS NOT NULL AND [KeyVersion] IS NOT NULL AND [EnrolledAtUtc] IS NOT NULL))"));
            e.HasKey(x => x.AccountId);
            e.Property(x => x.AccountId).ValueGeneratedNever();
            e.Property(x => x.KeyVersion).HasMaxLength(64);
            Utc3(e.Property(x => x.EnrolledAtUtc));
            Utc3(e.Property(x => x.LastVerifiedAtUtc));
            Utc3(e.Property(x => x.UpdatedAtUtc));
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<UserRecoveryCodeEntity>(e =>
        {
            e.ToTable("UserRecoveryCodes");
            e.HasKey(x => x.RecoveryCodeId);
            e.Property(x => x.RecoveryCodeId).ValueGeneratedNever();
            e.Property(x => x.CodeHash).HasColumnType("varbinary(64)").IsRequired();
            Utc3(e.Property(x => x.CreatedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            Utc3(e.Property(x => x.UsedAtUtc));
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.CodeHash).IsUnique().HasDatabaseName("UX_UserRecoveryCodes_CodeHash");
            e.HasIndex(x => new { x.AccountId, x.UsedAtUtc }).HasDatabaseName("IX_UserRecoveryCodes_Account_Unused");
        });

        mb.Entity<RoleDefinitionEntity>(e =>
        {
            e.ToTable("RoleDefinitions");
            e.HasKey(x => x.RoleKey);
            e.Property(x => x.RoleKey).HasMaxLength(64);
            e.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            e.Property(x => x.IsSystemRole).HasDefaultValue(false);
            e.Property(x => x.IsActive).HasDefaultValue(true);
        });

        mb.Entity<RolePermissionEntity>(e =>
        {
            e.ToTable("RolePermissions", t => t.HasCheckConstraint("CK_RolePermissions_Effect", "[Effect] IN (0,1)"));
            e.HasKey(x => new { x.RoleKey, x.PermissionKey });
            e.Property(x => x.RoleKey).HasMaxLength(64);
            e.Property(x => x.PermissionKey).HasMaxLength(100);
            e.HasOne<RoleDefinitionEntity>().WithMany().HasForeignKey(x => x.RoleKey).OnDelete(DeleteBehavior.NoAction);
        });

        mb.Entity<UserRoleAssignmentEntity>(e =>
        {
            e.ToTable("UserRoleAssignments", t =>
            {
                t.HasCheckConstraint("CK_UserRoleAssignments_Scope",
                    "(([ScopeType] IN (0,3) AND [ScopeBuildingId] IS NULL AND [ScopeApartmentId] IS NULL) OR ([ScopeType] = 1 AND [ScopeBuildingId] IS NOT NULL AND [ScopeApartmentId] IS NULL) OR ([ScopeType] = 2 AND [ScopeBuildingId] IS NOT NULL AND [ScopeApartmentId] IS NOT NULL))");
                t.HasCheckConstraint("CK_UserRoleAssignments_Validity", "[ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]");
                t.HasCheckConstraint("CK_UserRoleAssignments_Revocation",
                    "(([RevokedAtUtc] IS NULL AND [RevokedByAccountId] IS NULL AND [RevocationReason] IS NULL) OR ([RevokedAtUtc] IS NOT NULL AND [RevokedByAccountId] IS NOT NULL))");
            });
            e.HasKey(x => x.RoleAssignmentId);
            e.Property(x => x.RoleAssignmentId).ValueGeneratedNever();
            e.Property(x => x.RoleKey).HasMaxLength(64);
            e.Property(x => x.ScopeBuildingId).HasMaxLength(64);
            e.Property(x => x.ScopeApartmentId).HasMaxLength(64);
            e.Property(x => x.GrantReason).HasMaxLength(500);
            e.Property(x => x.RevocationReason).HasMaxLength(500);
            Utc3(e.Property(x => x.ValidFromUtc));
            Utc3(e.Property(x => x.ValidToUtc));
            Utc3(e.Property(x => x.GrantedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            Utc3(e.Property(x => x.RevokedAtUtc));
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<RoleDefinitionEntity>().WithMany().HasForeignKey(x => x.RoleKey).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.GrantedByAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.RevokedByAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<BuildingEntity>().WithMany().HasForeignKey(x => x.ScopeBuildingId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<ApartmentEntity>().WithMany().HasForeignKey(x => new { x.ScopeBuildingId, x.ScopeApartmentId })
                .HasPrincipalKey(x => new { x.BuildingId, x.Id }).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.AccountId, x.ScopeType, x.ScopeBuildingId, x.ScopeApartmentId }).HasDatabaseName("IX_UserRoleAssignments_Account_Scope");
            e.HasIndex(x => new { x.RoleKey, x.ScopeType, x.ScopeBuildingId, x.ScopeApartmentId }).HasDatabaseName("IX_UserRoleAssignments_Role_Scope");
        });

        mb.Entity<ApartmentOwnershipEntity>(e =>
        {
            e.ToTable("ApartmentOwnerships", t =>
            {
                t.HasCheckConstraint("CK_ApartmentOwnerships_Share", "[OwnershipSharePercent] > 0 AND [OwnershipSharePercent] <= 100");
                t.HasCheckConstraint("CK_ApartmentOwnerships_Validity", "[ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]");
            });
            e.HasKey(x => x.OwnershipId);
            e.Property(x => x.OwnershipId).ValueGeneratedNever();
            e.Property(x => x.BuildingId).HasMaxLength(64);
            e.Property(x => x.ApartmentId).HasMaxLength(64);
            e.Property(x => x.OwnershipSharePercent).HasPrecision(9, 6);
            e.Property(x => x.EvidenceReference).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(1000);
            Utc3(e.Property(x => x.ValidFromUtc));
            Utc3(e.Property(x => x.ValidToUtc));
            Utc3(e.Property(x => x.RecordedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<PersonEntity>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.RecordedByAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<ApartmentEntity>().WithMany().HasForeignKey(x => new { x.BuildingId, x.ApartmentId })
                .HasPrincipalKey(x => new { x.BuildingId, x.Id }).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.BuildingId, x.ApartmentId, x.ValidFromUtc, x.ValidToUtc }).HasDatabaseName("IX_ApartmentOwnerships_Apartment_Period");
            e.HasIndex(x => new { x.PersonId, x.ValidFromUtc, x.ValidToUtc }).HasDatabaseName("IX_ApartmentOwnerships_Person_Period");
        });

        mb.Entity<ApartmentOccupancyEntity>(e =>
        {
            e.ToTable("ApartmentOccupancies", t =>
            {
                t.HasCheckConstraint("CK_ApartmentOccupancies_Type", "[OccupancyType] IN (0,1,2,3)");
                t.HasCheckConstraint("CK_ApartmentOccupancies_Validity", "[ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]");
            });
            e.HasKey(x => x.OccupancyId);
            e.Property(x => x.OccupancyId).ValueGeneratedNever();
            e.Property(x => x.BuildingId).HasMaxLength(64);
            e.Property(x => x.ApartmentId).HasMaxLength(64);
            e.Property(x => x.Notes).HasMaxLength(1000);
            Utc3(e.Property(x => x.ValidFromUtc));
            Utc3(e.Property(x => x.ValidToUtc));
            Utc3(e.Property(x => x.RecordedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<PersonEntity>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.RecordedByAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<ApartmentEntity>().WithMany().HasForeignKey(x => new { x.BuildingId, x.ApartmentId })
                .HasPrincipalKey(x => new { x.BuildingId, x.Id }).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.BuildingId, x.ApartmentId, x.ValidFromUtc, x.ValidToUtc }).HasDatabaseName("IX_ApartmentOccupancies_Apartment_Period");
            e.HasIndex(x => new { x.PersonId, x.ValidFromUtc, x.ValidToUtc }).HasDatabaseName("IX_ApartmentOccupancies_Person_Period");
            e.HasIndex(x => new { x.BuildingId, x.ApartmentId }).IsUnique()
                .HasDatabaseName("UX_ApartmentOccupancies_OneOpenPrimary")
                .HasFilter("[IsPrimary] = 1 AND [ValidToUtc] IS NULL");
        });

        mb.Entity<ApartmentAccessGrantEntity>(e =>
        {
            e.ToTable("ApartmentAccessGrants", t =>
            {
                t.HasCheckConstraint("CK_ApartmentAccessGrants_Validity", "[ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]");
                t.HasCheckConstraint("CK_ApartmentAccessGrants_Revocation",
                    "(([RevokedAtUtc] IS NULL AND [RevokedByAccountId] IS NULL) OR ([RevokedAtUtc] IS NOT NULL AND [RevokedByAccountId] IS NOT NULL))");
            });
            e.HasKey(x => x.GrantId);
            e.Property(x => x.GrantId).ValueGeneratedNever();
            e.Property(x => x.BuildingId).HasMaxLength(64);
            e.Property(x => x.ApartmentId).HasMaxLength(64);
            e.Property(x => x.PermissionSetKey).HasMaxLength(80);
            e.Property(x => x.Reason).HasMaxLength(500);
            Utc3(e.Property(x => x.ValidFromUtc));
            Utc3(e.Property(x => x.ValidToUtc));
            Utc3(e.Property(x => x.GrantedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            Utc3(e.Property(x => x.RevokedAtUtc));
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<PersonEntity>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.GrantedByAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.RevokedByAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<ApartmentEntity>().WithMany().HasForeignKey(x => new { x.BuildingId, x.ApartmentId })
                .HasPrincipalKey(x => new { x.BuildingId, x.Id }).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.BuildingId, x.ApartmentId, x.ValidFromUtc, x.ValidToUtc }).HasDatabaseName("IX_ApartmentAccessGrants_Apartment_Period");
            e.HasIndex(x => new { x.PersonId, x.ValidFromUtc, x.ValidToUtc }).HasDatabaseName("IX_ApartmentAccessGrants_Person_Period");
        });

        mb.Entity<AccountPermissionOverrideEntity>(e =>
        {
            e.ToTable("AccountPermissionOverrides", t =>
            {
                t.HasCheckConstraint("CK_AccountPermissionOverrides_Effect", "[Effect] IN (0,1)");
                t.HasCheckConstraint("CK_AccountPermissionOverrides_Scope",
                    "(([ScopeType] IN (0,3) AND [ScopeBuildingId] IS NULL AND [ScopeApartmentId] IS NULL) OR ([ScopeType] = 1 AND [ScopeBuildingId] IS NOT NULL AND [ScopeApartmentId] IS NULL) OR ([ScopeType] = 2 AND [ScopeBuildingId] IS NOT NULL AND [ScopeApartmentId] IS NOT NULL))");
                t.HasCheckConstraint("CK_AccountPermissionOverrides_Validity", "[ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]");
                t.HasCheckConstraint("CK_AccountPermissionOverrides_Revocation",
                    "(([RevokedAtUtc] IS NULL AND [RevokedByAccountId] IS NULL AND [RevocationReason] IS NULL) OR ([RevokedAtUtc] IS NOT NULL AND [RevokedByAccountId] IS NOT NULL))");
            });
            e.HasKey(x => x.OverrideId);
            e.Property(x => x.OverrideId).ValueGeneratedNever();
            e.Property(x => x.PermissionKey).HasMaxLength(100);
            e.Property(x => x.ScopeBuildingId).HasMaxLength(64);
            e.Property(x => x.ScopeApartmentId).HasMaxLength(64);
            e.Property(x => x.GrantReason).HasMaxLength(500);
            e.Property(x => x.RevocationReason).HasMaxLength(500);
            Utc3(e.Property(x => x.ValidFromUtc));
            Utc3(e.Property(x => x.ValidToUtc));
            Utc3(e.Property(x => x.GrantedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            Utc3(e.Property(x => x.RevokedAtUtc));
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.GrantedByAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.RevokedByAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<BuildingEntity>().WithMany().HasForeignKey(x => x.ScopeBuildingId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<ApartmentEntity>().WithMany().HasForeignKey(x => new { x.ScopeBuildingId, x.ScopeApartmentId })
                .HasPrincipalKey(x => new { x.BuildingId, x.Id }).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.AccountId, x.PermissionKey, x.ScopeType, x.ScopeBuildingId, x.ScopeApartmentId, x.ValidFromUtc })
                .HasDatabaseName("IX_AccountPermissionOverrides_Account_Permission_Scope");
            e.HasIndex(x => new { x.PermissionKey, x.ScopeType, x.ScopeBuildingId, x.ScopeApartmentId, x.ValidFromUtc })
                .HasDatabaseName("IX_AccountPermissionOverrides_Permission_Scope");
        });

        mb.Entity<LegacyIdentityMapEntity>(e =>
        {
            e.ToTable("LegacyIdentityMap", t =>
            {
                t.HasCheckConstraint("CK_LegacyIdentityMap_SourceTable", "[SourceTable] IN (N'Users', N'Residents')");
                t.HasCheckConstraint("CK_LegacyIdentityMap_Status", "[MappingStatus] IN (0,1,2,3,4)");
                t.HasCheckConstraint("CK_LegacyIdentityMap_ReviewStamp",
                    "(([ReviewedAtUtc] IS NULL AND [ReviewedByAccountId] IS NULL) OR ([ReviewedAtUtc] IS NOT NULL AND [ReviewedByAccountId] IS NOT NULL))");
                t.HasCheckConstraint("CK_LegacyIdentityMap_ResolvedReview",
                    "([MappingStatus] NOT IN (1,2) OR ([ReviewedAtUtc] IS NOT NULL AND [ReviewedByAccountId] IS NOT NULL))");
                t.HasCheckConstraint("CK_LegacyIdentityMap_LinkTargets",
                    "(([MappingStatus] IN (0,3,4) AND [PersonId] IS NULL AND [AccountId] IS NULL) OR ([MappingStatus] IN (1,2) AND [PersonId] IS NOT NULL))");
                t.HasCheckConstraint("CK_LegacyIdentityMap_UserAccountTarget",
                    "(([SourceTable] <> N'Users' OR [MappingStatus] NOT IN (1,2) OR [AccountId] IS NOT NULL) AND ([SourceTable] <> N'Residents' OR [AccountId] IS NULL))");
            });
            e.HasKey(x => x.LegacyIdentityMapId);
            e.Property(x => x.LegacyIdentityMapId).ValueGeneratedNever();
            e.Property(x => x.SourceSystem).HasMaxLength(80);
            e.Property(x => x.SourceTable).HasMaxLength(128);
            e.Property(x => x.SourceKey).HasMaxLength(256);
            e.Property(x => x.MatchMethod).HasMaxLength(80);
            e.Property(x => x.EvidenceReference).HasMaxLength(500);
            e.Property(x => x.ReviewReason).HasMaxLength(1000);
            Utc3(e.Property(x => x.ReviewedAtUtc));
            Utc3(e.Property(x => x.CreatedAtUtc)).HasDefaultValueSql("SYSUTCDATETIME()");
            Utc3(e.Property(x => x.UpdatedAtUtc));
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<PersonEntity>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserAccountEntity>().WithMany().HasForeignKey(x => x.ReviewedByAccountId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.SourceSystem, x.SourceTable, x.SourceKey }).IsUnique().HasDatabaseName("UQ_LegacyIdentityMap_Source");
            e.HasIndex(x => x.AccountId).IsUnique().HasDatabaseName("UX_LegacyIdentityMap_Account").HasFilter("[AccountId] IS NOT NULL");
            e.HasIndex(x => x.PersonId).HasDatabaseName("IX_LegacyIdentityMap_Person").HasFilter("[PersonId] IS NOT NULL");
            e.HasIndex(x => new { x.MappingStatus, x.SourceTable, x.SourceSystem }).HasDatabaseName("IX_LegacyIdentityMap_Status_Source");
        });
    }

    private static PropertyBuilder<DateTime> Utc3(PropertyBuilder<DateTime> property)
        => property.HasColumnType("datetime2(3)");

    private static PropertyBuilder<DateTime?> Utc3(PropertyBuilder<DateTime?> property)
        => property.HasColumnType("datetime2(3)");
}
