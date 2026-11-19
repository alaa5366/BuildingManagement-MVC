using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class Phase06_IdentityAuthorizationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PrimaryEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    PrimaryPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Whatsapp = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.PersonId);
                    table.CheckConstraint("CK_Persons_ArchiveState", "(([IsArchived] = 0 AND [ArchivedAtUtc] IS NULL) OR ([IsArchived] = 1 AND [ArchivedAtUtc] IS NOT NULL))");
                });

            migrationBuilder.CreateTable(
                name: "RoleDefinitions",
                columns: table => new
                {
                    RoleKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleDefinitions", x => x.RoleKey);
                });

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    ExternalProvider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ExternalSubject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastLoginAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.AccountId);
                    table.UniqueConstraint("UQ_UserAccounts_PersonId", x => x.PersonId);
                    table.CheckConstraint("CK_UserAccounts_AccountStatus", "[AccountStatus] IN (0,1,2,3)");
                    table.CheckConstraint("CK_UserAccounts_ExternalLoginPair", "(([ExternalProvider] IS NULL AND [ExternalSubject] IS NULL) OR ([ExternalProvider] IS NOT NULL AND [ExternalSubject] IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_UserAccounts_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId");
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PermissionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Effect = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleKey, x.PermissionKey });
                    table.CheckConstraint("CK_RolePermissions_Effect", "[Effect] IN (0,1)");
                    table.ForeignKey(
                        name: "FK_RolePermissions_RoleDefinitions_RoleKey",
                        column: x => x.RoleKey,
                        principalTable: "RoleDefinitions",
                        principalColumn: "RoleKey");
                });

            migrationBuilder.CreateTable(
                name: "AccountPermissionOverrides",
                columns: table => new
                {
                    OverrideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Effect = table.Column<byte>(type: "tinyint", nullable: false),
                    ScopeType = table.Column<byte>(type: "tinyint", nullable: false),
                    ScopeBuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ScopeApartmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    GrantedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    GrantedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GrantReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RevokedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountPermissionOverrides", x => x.OverrideId);
                    table.CheckConstraint("CK_AccountPermissionOverrides_Effect", "[Effect] IN (0,1)");
                    table.CheckConstraint("CK_AccountPermissionOverrides_Revocation", "(([RevokedAtUtc] IS NULL AND [RevokedByAccountId] IS NULL AND [RevocationReason] IS NULL) OR ([RevokedAtUtc] IS NOT NULL AND [RevokedByAccountId] IS NOT NULL))");
                    table.CheckConstraint("CK_AccountPermissionOverrides_Scope", "(([ScopeType] IN (0,3) AND [ScopeBuildingId] IS NULL AND [ScopeApartmentId] IS NULL) OR ([ScopeType] = 1 AND [ScopeBuildingId] IS NOT NULL AND [ScopeApartmentId] IS NULL) OR ([ScopeType] = 2 AND [ScopeBuildingId] IS NOT NULL AND [ScopeApartmentId] IS NOT NULL))");
                    table.CheckConstraint("CK_AccountPermissionOverrides_Validity", "[ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]");
                    table.ForeignKey(
                        name: "FK_AccountPermissionOverrides_Apartments_ScopeBuildingId_ScopeApartmentId",
                        columns: x => new { x.ScopeBuildingId, x.ScopeApartmentId },
                        principalTable: "Apartments",
                        principalColumns: new[] { "BuildingId", "Id" });
                    table.ForeignKey(
                        name: "FK_AccountPermissionOverrides_Buildings_ScopeBuildingId",
                        column: x => x.ScopeBuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AccountPermissionOverrides_UserAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                    table.ForeignKey(
                        name: "FK_AccountPermissionOverrides_UserAccounts_GrantedByAccountId",
                        column: x => x.GrantedByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                    table.ForeignKey(
                        name: "FK_AccountPermissionOverrides_UserAccounts_RevokedByAccountId",
                        column: x => x.RevokedByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateTable(
                name: "ApartmentAccessGrants",
                columns: table => new
                {
                    GrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ApartmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantType = table.Column<byte>(type: "tinyint", nullable: false),
                    PermissionSetKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    GrantedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    GrantedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RevokedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApartmentAccessGrants", x => x.GrantId);
                    table.CheckConstraint("CK_ApartmentAccessGrants_Revocation", "(([RevokedAtUtc] IS NULL AND [RevokedByAccountId] IS NULL) OR ([RevokedAtUtc] IS NOT NULL AND [RevokedByAccountId] IS NOT NULL))");
                    table.CheckConstraint("CK_ApartmentAccessGrants_Validity", "[ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]");
                    table.ForeignKey(
                        name: "FK_ApartmentAccessGrants_Apartments_BuildingId_ApartmentId",
                        columns: x => new { x.BuildingId, x.ApartmentId },
                        principalTable: "Apartments",
                        principalColumns: new[] { "BuildingId", "Id" });
                    table.ForeignKey(
                        name: "FK_ApartmentAccessGrants_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId");
                    table.ForeignKey(
                        name: "FK_ApartmentAccessGrants_UserAccounts_GrantedByAccountId",
                        column: x => x.GrantedByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                    table.ForeignKey(
                        name: "FK_ApartmentAccessGrants_UserAccounts_RevokedByAccountId",
                        column: x => x.RevokedByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateTable(
                name: "ApartmentOccupancies",
                columns: table => new
                {
                    OccupancyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ApartmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccupancyType = table.Column<byte>(type: "tinyint", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RecordedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApartmentOccupancies", x => x.OccupancyId);
                    table.CheckConstraint("CK_ApartmentOccupancies_Type", "[OccupancyType] IN (0,1,2,3)");
                    table.CheckConstraint("CK_ApartmentOccupancies_Validity", "[ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]");
                    table.ForeignKey(
                        name: "FK_ApartmentOccupancies_Apartments_BuildingId_ApartmentId",
                        columns: x => new { x.BuildingId, x.ApartmentId },
                        principalTable: "Apartments",
                        principalColumns: new[] { "BuildingId", "Id" });
                    table.ForeignKey(
                        name: "FK_ApartmentOccupancies_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId");
                    table.ForeignKey(
                        name: "FK_ApartmentOccupancies_UserAccounts_RecordedByAccountId",
                        column: x => x.RecordedByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateTable(
                name: "ApartmentOwnerships",
                columns: table => new
                {
                    OwnershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ApartmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnershipSharePercent = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RecordedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EvidenceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApartmentOwnerships", x => x.OwnershipId);
                    table.CheckConstraint("CK_ApartmentOwnerships_Share", "[OwnershipSharePercent] > 0 AND [OwnershipSharePercent] <= 100");
                    table.CheckConstraint("CK_ApartmentOwnerships_Validity", "[ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]");
                    table.ForeignKey(
                        name: "FK_ApartmentOwnerships_Apartments_BuildingId_ApartmentId",
                        columns: x => new { x.BuildingId, x.ApartmentId },
                        principalTable: "Apartments",
                        principalColumns: new[] { "BuildingId", "Id" });
                    table.ForeignKey(
                        name: "FK_ApartmentOwnerships_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId");
                    table.ForeignKey(
                        name: "FK_ApartmentOwnerships_UserAccounts_RecordedByAccountId",
                        column: x => x.RecordedByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateTable(
                name: "LegacyIdentityMap",
                columns: table => new
                {
                    LegacyIdentityMapId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceTable = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MappingStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    MatchMethod = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    EvidenceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ReviewedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyIdentityMap", x => x.LegacyIdentityMapId);
                    table.UniqueConstraint("UQ_LegacyIdentityMap_Source", x => new { x.SourceSystem, x.SourceTable, x.SourceKey });
                    table.CheckConstraint("CK_LegacyIdentityMap_LinkTargets", "(([MappingStatus] IN (0,3,4) AND [PersonId] IS NULL AND [AccountId] IS NULL) OR ([MappingStatus] IN (1,2) AND [PersonId] IS NOT NULL))");
                    table.CheckConstraint("CK_LegacyIdentityMap_ResolvedReview", "([MappingStatus] NOT IN (1,2) OR ([ReviewedAtUtc] IS NOT NULL AND [ReviewedByAccountId] IS NOT NULL))");
                    table.CheckConstraint("CK_LegacyIdentityMap_ReviewStamp", "(([ReviewedAtUtc] IS NULL AND [ReviewedByAccountId] IS NULL) OR ([ReviewedAtUtc] IS NOT NULL AND [ReviewedByAccountId] IS NOT NULL))");
                    table.CheckConstraint("CK_LegacyIdentityMap_SourceTable", "[SourceTable] IN (N'Users', N'Residents')");
                    table.CheckConstraint("CK_LegacyIdentityMap_Status", "[MappingStatus] IN (0,1,2,3,4)");
                    table.CheckConstraint("CK_LegacyIdentityMap_UserAccountTarget", "(([SourceTable] <> N'Users' OR [MappingStatus] NOT IN (1,2) OR [AccountId] IS NOT NULL) AND ([SourceTable] <> N'Residents' OR [AccountId] IS NULL))");
                    table.ForeignKey(
                        name: "FK_LegacyIdentityMap_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId");
                    table.ForeignKey(
                        name: "FK_LegacyIdentityMap_UserAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                    table.ForeignKey(
                        name: "FK_LegacyIdentityMap_UserAccounts_ReviewedByAccountId",
                        column: x => x.ReviewedByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateTable(
                name: "UserCredentials",
                columns: table => new
                {
                    CredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CredentialType = table.Column<byte>(type: "tinyint", nullable: false),
                    CredentialHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    HashAlgorithm = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ReplacedByCredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCredentials", x => x.CredentialId);
                    table.UniqueConstraint("UQ_UserCredentials_Credential_Account", x => new { x.CredentialId, x.AccountId });
                    table.CheckConstraint("CK_UserCredentials_Expiry", "[ExpiresAtUtc] IS NULL OR [ExpiresAtUtc] > [CreatedAtUtc]");
                    table.CheckConstraint("CK_UserCredentials_Type", "[CredentialType] IN (0,1)");
                    table.ForeignKey(
                        name: "FK_UserCredentials_UserAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                    table.ForeignKey(
                        name: "FK_UserCredentials_UserCredentials_ReplacedByCredentialId_AccountId",
                        columns: x => new { x.ReplacedByCredentialId, x.AccountId },
                        principalTable: "UserCredentials",
                        principalColumns: new[] { "CredentialId", "AccountId" });
                });

            migrationBuilder.CreateTable(
                name: "UserRecoveryCodes",
                columns: table => new
                {
                    RecoveryCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeHash = table.Column<byte[]>(type: "varbinary(64)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRecoveryCodes", x => x.RecoveryCodeId);
                    table.ForeignKey(
                        name: "FK_UserRecoveryCodes_UserAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateTable(
                name: "UserRoleAssignments",
                columns: table => new
                {
                    RoleAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScopeType = table.Column<byte>(type: "tinyint", nullable: false),
                    ScopeBuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ScopeApartmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    GrantedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    GrantedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GrantReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RevokedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleAssignments", x => x.RoleAssignmentId);
                    table.CheckConstraint("CK_UserRoleAssignments_Revocation", "(([RevokedAtUtc] IS NULL AND [RevokedByAccountId] IS NULL AND [RevocationReason] IS NULL) OR ([RevokedAtUtc] IS NOT NULL AND [RevokedByAccountId] IS NOT NULL))");
                    table.CheckConstraint("CK_UserRoleAssignments_Scope", "(([ScopeType] IN (0,3) AND [ScopeBuildingId] IS NULL AND [ScopeApartmentId] IS NULL) OR ([ScopeType] = 1 AND [ScopeBuildingId] IS NOT NULL AND [ScopeApartmentId] IS NULL) OR ([ScopeType] = 2 AND [ScopeBuildingId] IS NOT NULL AND [ScopeApartmentId] IS NOT NULL))");
                    table.CheckConstraint("CK_UserRoleAssignments_Validity", "[ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]");
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_Apartments_ScopeBuildingId_ScopeApartmentId",
                        columns: x => new { x.ScopeBuildingId, x.ScopeApartmentId },
                        principalTable: "Apartments",
                        principalColumns: new[] { "BuildingId", "Id" });
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_Buildings_ScopeBuildingId",
                        column: x => x.ScopeBuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_RoleDefinitions_RoleKey",
                        column: x => x.RoleKey,
                        principalTable: "RoleDefinitions",
                        principalColumn: "RoleKey");
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_UserAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_UserAccounts_GrantedByAccountId",
                        column: x => x.GrantedByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_UserAccounts_RevokedByAccountId",
                        column: x => x.RevokedByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateTable(
                name: "UserTwoFactorSettings",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SecretCiphertext = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    KeyVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastAcceptedTimeStep = table.Column<long>(type: "bigint", nullable: true),
                    EnrolledAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    LastVerifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTwoFactorSettings", x => x.AccountId);
                    table.CheckConstraint("CK_UserTwoFactorSettings_EnabledSecret", "(([IsEnabled] = 0 AND [SecretCiphertext] IS NULL AND [KeyVersion] IS NULL) OR ([IsEnabled] = 1 AND [SecretCiphertext] IS NOT NULL AND [KeyVersion] IS NOT NULL AND [EnrolledAtUtc] IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_UserTwoFactorSettings_UserAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountPermissionOverrides_Account_Permission_Scope",
                table: "AccountPermissionOverrides",
                columns: new[] { "AccountId", "PermissionKey", "ScopeType", "ScopeBuildingId", "ScopeApartmentId", "ValidFromUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountPermissionOverrides_GrantedByAccountId",
                table: "AccountPermissionOverrides",
                column: "GrantedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountPermissionOverrides_Permission_Scope",
                table: "AccountPermissionOverrides",
                columns: new[] { "PermissionKey", "ScopeType", "ScopeBuildingId", "ScopeApartmentId", "ValidFromUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountPermissionOverrides_RevokedByAccountId",
                table: "AccountPermissionOverrides",
                column: "RevokedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountPermissionOverrides_ScopeBuildingId_ScopeApartmentId",
                table: "AccountPermissionOverrides",
                columns: new[] { "ScopeBuildingId", "ScopeApartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentAccessGrants_Apartment_Period",
                table: "ApartmentAccessGrants",
                columns: new[] { "BuildingId", "ApartmentId", "ValidFromUtc", "ValidToUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentAccessGrants_GrantedByAccountId",
                table: "ApartmentAccessGrants",
                column: "GrantedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentAccessGrants_Person_Period",
                table: "ApartmentAccessGrants",
                columns: new[] { "PersonId", "ValidFromUtc", "ValidToUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentAccessGrants_RevokedByAccountId",
                table: "ApartmentAccessGrants",
                column: "RevokedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentOccupancies_Apartment_Period",
                table: "ApartmentOccupancies",
                columns: new[] { "BuildingId", "ApartmentId", "ValidFromUtc", "ValidToUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentOccupancies_Person_Period",
                table: "ApartmentOccupancies",
                columns: new[] { "PersonId", "ValidFromUtc", "ValidToUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentOccupancies_RecordedByAccountId",
                table: "ApartmentOccupancies",
                column: "RecordedByAccountId");

            migrationBuilder.CreateIndex(
                name: "UX_ApartmentOccupancies_OneOpenPrimary",
                table: "ApartmentOccupancies",
                columns: new[] { "BuildingId", "ApartmentId" },
                unique: true,
                filter: "[IsPrimary] = 1 AND [ValidToUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentOwnerships_Apartment_Period",
                table: "ApartmentOwnerships",
                columns: new[] { "BuildingId", "ApartmentId", "ValidFromUtc", "ValidToUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentOwnerships_Person_Period",
                table: "ApartmentOwnerships",
                columns: new[] { "PersonId", "ValidFromUtc", "ValidToUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApartmentOwnerships_RecordedByAccountId",
                table: "ApartmentOwnerships",
                column: "RecordedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyIdentityMap_Person",
                table: "LegacyIdentityMap",
                column: "PersonId",
                filter: "[PersonId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyIdentityMap_ReviewedByAccountId",
                table: "LegacyIdentityMap",
                column: "ReviewedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyIdentityMap_Status_Source",
                table: "LegacyIdentityMap",
                columns: new[] { "MappingStatus", "SourceTable", "SourceSystem" });

            migrationBuilder.CreateIndex(
                name: "UX_LegacyIdentityMap_Account",
                table: "LegacyIdentityMap",
                column: "AccountId",
                unique: true,
                filter: "[AccountId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_PrimaryEmail",
                table: "Persons",
                column: "PrimaryEmail",
                filter: "[PrimaryEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_PrimaryPhone",
                table: "Persons",
                column: "PrimaryPhone",
                filter: "[PrimaryPhone] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_NormalizedEmail",
                table: "UserAccounts",
                column: "NormalizedEmail",
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_UserAccounts_ExternalLogin",
                table: "UserAccounts",
                columns: new[] { "ExternalProvider", "ExternalSubject" },
                unique: true,
                filter: "[ExternalProvider] IS NOT NULL AND [ExternalSubject] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserCredentials_Account_Type",
                table: "UserCredentials",
                columns: new[] { "AccountId", "CredentialType", "RevokedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserCredentials_ReplacedByCredentialId_AccountId",
                table: "UserCredentials",
                columns: new[] { "ReplacedByCredentialId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRecoveryCodes_Account_Unused",
                table: "UserRecoveryCodes",
                columns: new[] { "AccountId", "UsedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_UserRecoveryCodes_CodeHash",
                table: "UserRecoveryCodes",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_Account_Scope",
                table: "UserRoleAssignments",
                columns: new[] { "AccountId", "ScopeType", "ScopeBuildingId", "ScopeApartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_GrantedByAccountId",
                table: "UserRoleAssignments",
                column: "GrantedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_RevokedByAccountId",
                table: "UserRoleAssignments",
                column: "RevokedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_Role_Scope",
                table: "UserRoleAssignments",
                columns: new[] { "RoleKey", "ScopeType", "ScopeBuildingId", "ScopeApartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_ScopeBuildingId_ScopeApartmentId",
                table: "UserRoleAssignments",
                columns: new[] { "ScopeBuildingId", "ScopeApartmentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountPermissionOverrides");

            migrationBuilder.DropTable(
                name: "ApartmentAccessGrants");

            migrationBuilder.DropTable(
                name: "ApartmentOccupancies");

            migrationBuilder.DropTable(
                name: "ApartmentOwnerships");

            migrationBuilder.DropTable(
                name: "LegacyIdentityMap");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UserCredentials");

            migrationBuilder.DropTable(
                name: "UserRecoveryCodes");

            migrationBuilder.DropTable(
                name: "UserRoleAssignments");

            migrationBuilder.DropTable(
                name: "UserTwoFactorSettings");

            migrationBuilder.DropTable(
                name: "RoleDefinitions");

            migrationBuilder.DropTable(
                name: "UserAccounts");

            migrationBuilder.DropTable(
                name: "Persons");
        }
    }
}
