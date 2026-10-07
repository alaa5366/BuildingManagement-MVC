using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackupHistory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    CollectionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalDocuments = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    DurationSeconds = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BuildingNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdminPin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdminWhatsapp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlobalSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AppName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultLanguage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WhatsappApiToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WhatsappPhoneId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CloudinaryCloudName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CloudinaryApiKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirebaseProjectId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnablePolls = table.Column<bool>(type: "bit", nullable: false),
                    EnableMaintenance = table.Column<bool>(type: "bit", nullable: false),
                    EnableOcr = table.Column<bool>(type: "bit", nullable: false),
                    EnableQrAccess = table.Column<bool>(type: "bit", nullable: false),
                    EnableWhatsApp = table.Column<bool>(type: "bit", nullable: false),
                    SessionTimeoutDays = table.Column<int>(type: "int", nullable: false),
                    MaxPinAttempts = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StorageMode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Presence",
                columns: table => new
                {
                    Uid = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BuildingId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApartmentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSeen = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Presence", x => x.Uid);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledBackupSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hour = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: true),
                    DayOfMonth = table.Column<int>(type: "int", nullable: true),
                    CollectionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxBackupsToKeep = table.Column<int>(type: "int", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    LastRunStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledBackupSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncPendingChanges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    Applied = table.Column<bool>(type: "bit", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncPendingChanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Uid = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Whatsapp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    DisabledReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Uid);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Ts = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActorRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MonthKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BuildingNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AptNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.AuditId);
                    table.ForeignKey(
                        name: "FK_AuditLog_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BuildingSettings",
                columns: table => new
                {
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WhatsappNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InvoiceDayOfMonth = table.Column<int>(type: "int", nullable: false),
                    ExpenseDistribution = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VotingQuorumPercent = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingSettings", x => x.BuildingId);
                    table.ForeignKey(
                        name: "FK_BuildingSettings_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dvrs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dvrs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dvrs_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinancialCategories",
                columns: table => new
                {
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialCategories", x => new { x.BuildingId, x.Kind, x.Id });
                    table.ForeignKey(
                        name: "FK_FinancialCategories_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Floors",
                columns: table => new
                {
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Floors", x => new { x.BuildingId, x.Id });
                    table.ForeignKey(
                        name: "FK_Floors_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRecords",
                columns: table => new
                {
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaintenanceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Vendor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    AddedToExpense = table.Column<bool>(type: "bit", nullable: false),
                    AddedToExpenseAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRecords", x => new { x.BuildingId, x.Id });
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Polls",
                columns: table => new
                {
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    Deadline = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Polls", x => new { x.BuildingId, x.Id });
                    table.ForeignKey(
                        name: "FK_Polls_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Receipts",
                columns: table => new
                {
                    ReceiptId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublicId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    OcrText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OcrAmountGuess = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipts", x => x.ReceiptId);
                    table.ForeignKey(
                        name: "FK_Receipts_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BuildingAdmins",
                columns: table => new
                {
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AdminUid = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingAdmins", x => new { x.BuildingId, x.AdminUid });
                    table.ForeignKey(
                        name: "FK_BuildingAdmins_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BuildingAdmins_Users_AdminUid",
                        column: x => x.AdminUid,
                        principalTable: "Users",
                        principalColumn: "Uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    Uid = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Permission = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => new { x.Uid, x.Permission });
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_Uid",
                        column: x => x.Uid,
                        principalTable: "Users",
                        principalColumn: "Uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cameras",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DvrId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    RtspPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HlsUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cameras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cameras_Dvrs_DvrId",
                        column: x => x.DvrId,
                        principalTable: "Dvrs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MonthKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategoryId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CategoryKind = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpenseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReceiptUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => new { x.BuildingId, x.Id });
                    table.ForeignKey(
                        name: "FK_Expenses_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Expenses_FinancialCategories_BuildingId_CategoryKind_CategoryId",
                        columns: x => new { x.BuildingId, x.CategoryKind, x.CategoryId },
                        principalTable: "FinancialCategories",
                        principalColumns: new[] { "BuildingId", "Kind", "Id" });
                });

            migrationBuilder.CreateTable(
                name: "Revenues",
                columns: table => new
                {
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MonthKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategoryId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CategoryKind = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RevenueDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Revenues", x => new { x.BuildingId, x.Id });
                    table.ForeignKey(
                        name: "FK_Revenues_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Revenues_FinancialCategories_BuildingId_CategoryKind_CategoryId",
                        columns: x => new { x.BuildingId, x.CategoryKind, x.CategoryId },
                        principalTable: "FinancialCategories",
                        principalColumns: new[] { "BuildingId", "Kind", "Id" });
                });

            migrationBuilder.CreateTable(
                name: "Apartments",
                columns: table => new
                {
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FloorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MonthlyFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Pin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OpenDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CloseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    DisabledReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotifyWhatsApp = table.Column<bool>(type: "bit", nullable: false),
                    NotifyEmail = table.Column<bool>(type: "bit", nullable: false),
                    NotifyInApp = table.Column<bool>(type: "bit", nullable: false),
                    PreferredPaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SettingsUpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    SettingsUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apartments", x => new { x.BuildingId, x.Id });
                    table.ForeignKey(
                        name: "FK_Apartments_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Apartments_Floors_BuildingId_FloorId",
                        columns: x => new { x.BuildingId, x.FloorId },
                        principalTable: "Floors",
                        principalColumns: new[] { "BuildingId", "Id" });
                });

            migrationBuilder.CreateTable(
                name: "PollOptions",
                columns: table => new
                {
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PollId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollOptions", x => new { x.BuildingId, x.PollId, x.Id });
                    table.ForeignKey(
                        name: "FK_PollOptions_Polls_BuildingId_PollId",
                        columns: x => new { x.BuildingId, x.PollId },
                        principalTable: "Polls",
                        principalColumns: new[] { "BuildingId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Deposits",
                columns: table => new
                {
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MonthKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApartmentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceiptUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiptPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiptFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiptFileSize = table.Column<long>(type: "bigint", nullable: true),
                    ReceiptFileType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiptSuccess = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    ConfirmedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    CancelledBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CancelledReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdateReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deposits", x => new { x.BuildingId, x.Id });
                    table.ForeignKey(
                        name: "FK_Deposits_Apartments_BuildingId_ApartmentId",
                        columns: x => new { x.BuildingId, x.ApartmentId },
                        principalTable: "Apartments",
                        principalColumns: new[] { "BuildingId", "Id" });
                    table.ForeignKey(
                        name: "FK_Deposits_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyApartmentShares",
                columns: table => new
                {
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MonthKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApartmentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpenseShare = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RevenueShare = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CarryOver = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyApartmentShares", x => new { x.BuildingId, x.MonthKey, x.ApartmentId });
                    table.ForeignKey(
                        name: "FK_MonthlyApartmentShares_Apartments_BuildingId_ApartmentId",
                        columns: x => new { x.BuildingId, x.ApartmentId },
                        principalTable: "Apartments",
                        principalColumns: new[] { "BuildingId", "Id" });
                    table.ForeignKey(
                        name: "FK_MonthlyApartmentShares_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Ts = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    RecipientType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecipientApartmentId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => new { x.BuildingId, x.Id });
                    table.ForeignKey(
                        name: "FK_Notifications_Apartments_BuildingId_RecipientApartmentId",
                        columns: x => new { x.BuildingId, x.RecipientApartmentId },
                        principalTable: "Apartments",
                        principalColumns: new[] { "BuildingId", "Id" });
                    table.ForeignKey(
                        name: "FK_Notifications_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QrTokens",
                columns: table => new
                {
                    Uid = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApartmentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UseType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    UsedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrTokens", x => x.Uid);
                    table.ForeignKey(
                        name: "FK_QrTokens_Apartments_BuildingId_ApartmentId",
                        columns: x => new { x.BuildingId, x.ApartmentId },
                        principalTable: "Apartments",
                        principalColumns: new[] { "BuildingId", "Id" });
                    table.ForeignKey(
                        name: "FK_QrTokens_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WalletAdjustments",
                columns: table => new
                {
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApartmentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MonthKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletAdjustments", x => new { x.BuildingId, x.Id });
                    table.ForeignKey(
                        name: "FK_WalletAdjustments_Apartments_BuildingId_ApartmentId",
                        columns: x => new { x.BuildingId, x.ApartmentId },
                        principalTable: "Apartments",
                        principalColumns: new[] { "BuildingId", "Id" });
                    table.ForeignKey(
                        name: "FK_WalletAdjustments_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PollVotes",
                columns: table => new
                {
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PollId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApartmentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OptionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollVotes", x => new { x.BuildingId, x.PollId, x.ApartmentId });
                    table.ForeignKey(
                        name: "FK_PollVotes_Apartments_BuildingId_ApartmentId",
                        columns: x => new { x.BuildingId, x.ApartmentId },
                        principalTable: "Apartments",
                        principalColumns: new[] { "BuildingId", "Id" });
                    table.ForeignKey(
                        name: "FK_PollVotes_PollOptions_BuildingId_PollId_OptionId",
                        columns: x => new { x.BuildingId, x.PollId, x.OptionId },
                        principalTable: "PollOptions",
                        principalColumns: new[] { "BuildingId", "PollId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DepositEditHistory",
                columns: table => new
                {
                    EditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuildingId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DepositId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Ts = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    ByUser = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OldNote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewNote = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositEditHistory", x => x.EditId);
                    table.ForeignKey(
                        name: "FK_DepositEditHistory_Deposits_BuildingId_DepositId",
                        columns: x => new { x.BuildingId, x.DepositId },
                        principalTable: "Deposits",
                        principalColumns: new[] { "BuildingId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_BuildingId_FloorId",
                table: "Apartments",
                columns: new[] { "BuildingId", "FloorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_BuildingId_Number",
                table: "Apartments",
                columns: new[] { "BuildingId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_BuildingId_Ts",
                table: "AuditLog",
                columns: new[] { "BuildingId", "Ts" });

            migrationBuilder.CreateIndex(
                name: "IX_BuildingAdmins_AdminUid",
                table: "BuildingAdmins",
                column: "AdminUid");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_BuildingNumber",
                table: "Buildings",
                column: "BuildingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_BuildingId",
                table: "Cameras",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_DvrId_Channel",
                table: "Cameras",
                columns: new[] { "DvrId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepositEditHistory_BuildingId_DepositId",
                table: "DepositEditHistory",
                columns: new[] { "BuildingId", "DepositId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_BuildingId_ApartmentId",
                table: "Deposits",
                columns: new[] { "BuildingId", "ApartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Dvrs_BuildingId",
                table: "Dvrs",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BuildingId_CategoryKind_CategoryId",
                table: "Expenses",
                columns: new[] { "BuildingId", "CategoryKind", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyApartmentShares_BuildingId_ApartmentId",
                table: "MonthlyApartmentShares",
                columns: new[] { "BuildingId", "ApartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BuildingId_RecipientApartmentId",
                table: "Notifications",
                columns: new[] { "BuildingId", "RecipientApartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_BuildingId_ApartmentId",
                table: "PollVotes",
                columns: new[] { "BuildingId", "ApartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PollVotes_BuildingId_PollId_OptionId",
                table: "PollVotes",
                columns: new[] { "BuildingId", "PollId", "OptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_QrTokens_BuildingId_ApartmentId",
                table: "QrTokens",
                columns: new[] { "BuildingId", "ApartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_BuildingId",
                table: "Receipts",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_Revenues_BuildingId_CategoryKind_CategoryId",
                table: "Revenues",
                columns: new[] { "BuildingId", "CategoryKind", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncPendingChanges_Applied_CreatedAt",
                table: "SyncPendingChanges",
                columns: new[] { "Applied", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncPendingChanges_EntityType_EntityId",
                table: "SyncPendingChanges",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_WalletAdjustments_BuildingId_ApartmentId",
                table: "WalletAdjustments",
                columns: new[] { "BuildingId", "ApartmentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "BackupHistory");

            migrationBuilder.DropTable(
                name: "BuildingAdmins");

            migrationBuilder.DropTable(
                name: "BuildingSettings");

            migrationBuilder.DropTable(
                name: "Cameras");

            migrationBuilder.DropTable(
                name: "DepositEditHistory");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "GlobalSettings");

            migrationBuilder.DropTable(
                name: "MaintenanceRecords");

            migrationBuilder.DropTable(
                name: "MonthlyApartmentShares");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PollVotes");

            migrationBuilder.DropTable(
                name: "Presence");

            migrationBuilder.DropTable(
                name: "QrTokens");

            migrationBuilder.DropTable(
                name: "Receipts");

            migrationBuilder.DropTable(
                name: "Revenues");

            migrationBuilder.DropTable(
                name: "ScheduledBackupSettings");

            migrationBuilder.DropTable(
                name: "SyncPendingChanges");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "WalletAdjustments");

            migrationBuilder.DropTable(
                name: "Dvrs");

            migrationBuilder.DropTable(
                name: "Deposits");

            migrationBuilder.DropTable(
                name: "PollOptions");

            migrationBuilder.DropTable(
                name: "FinancialCategories");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Apartments");

            migrationBuilder.DropTable(
                name: "Polls");

            migrationBuilder.DropTable(
                name: "Floors");

            migrationBuilder.DropTable(
                name: "Buildings");
        }
    }
}
