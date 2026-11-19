using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class Phase08_FinancialLedgerFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialPeriods",
                columns: table => new
                {
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PeriodKey = table.Column<string>(type: "char(7)", maxLength: 7, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ClosedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CloseReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialPeriods", x => x.PeriodId);
                    table.UniqueConstraint("UQ_FinancialPeriods_Period_Building", x => new { x.PeriodId, x.BuildingId });
                    table.CheckConstraint("CK_FinancialPeriods_CloseStamp", "([Status] = 0 AND [ClosedAtUtc] IS NULL AND [ClosedByAccountId] IS NULL) OR ([Status] IN (1,2) AND [ClosedAtUtc] IS NOT NULL AND [ClosedByAccountId] IS NOT NULL)");
                    table.CheckConstraint("CK_FinancialPeriods_Key", "[PeriodKey] LIKE '[0-9][0-9][0-9][0-9]-[01][0-9]' AND SUBSTRING([PeriodKey],6,2) BETWEEN '01' AND '12'");
                    table.CheckConstraint("CK_FinancialPeriods_Range", "[EndsAtUtc] > [StartsAtUtc]");
                    table.CheckConstraint("CK_FinancialPeriods_Status", "[Status] IN (0,1,2)");
                    table.ForeignKey(
                        name: "FK_FinancialPeriods_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinancialPeriods_UserAccounts_ClosedByAccountId",
                        column: x => x.ClosedByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateTable(
                name: "FinancialReconciliationRuns",
                columns: table => new
                {
                    ReconciliationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleVersion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    StartedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SafeSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialReconciliationRuns", x => x.ReconciliationRunId);
                    table.CheckConstraint("CK_FinancialReconciliationRuns_Completion", "([Status] = 0 AND [CompletedAtUtc] IS NULL) OR ([Status] IN (1,2) AND [CompletedAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_FinancialReconciliationRuns_Status", "[Status] IN (0,1,2)");
                    table.ForeignKey(
                        name: "FK_FinancialReconciliationRuns_UserAccounts_StartedByAccountId",
                        column: x => x.StartedByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateTable(
                name: "LedgerTransactions",
                columns: table => new
                {
                    LedgerTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReversesTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerTransactions", x => x.LedgerTransactionId);
                    table.UniqueConstraint("UQ_LedgerTransactions_Period_Building", x => new { x.LedgerTransactionId, x.PeriodId, x.BuildingId });
                    table.CheckConstraint("CK_LedgerTransactions_Kind", "[Kind] IN (1,2,3,4,5,6,7,8)");
                    table.CheckConstraint("CK_LedgerTransactions_PostStamp", "([Status] IN (0,2) AND [PostedAtUtc] IS NULL) OR ([Status] = 1 AND [PostedAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_LedgerTransactions_ReversalLink", "([Kind] = 7 AND [ReversesTransactionId] IS NOT NULL) OR ([Kind] <> 7 AND [ReversesTransactionId] IS NULL)");
                    table.CheckConstraint("CK_LedgerTransactions_SourcePair", "([SourceSystem] IS NULL AND [SourceType] IS NULL AND [SourceKey] IS NULL) OR ([SourceSystem] IS NOT NULL AND [SourceType] IS NOT NULL AND [SourceKey] IS NOT NULL)");
                    table.CheckConstraint("CK_LedgerTransactions_Status", "[Status] IN (0,1,2)");
                    table.ForeignKey(
                        name: "FK_LedgerTransactions_FinancialPeriods_PeriodId_BuildingId",
                        columns: x => new { x.PeriodId, x.BuildingId },
                        principalTable: "FinancialPeriods",
                        principalColumns: new[] { "PeriodId", "BuildingId" });
                    table.ForeignKey(
                        name: "FK_LedgerTransactions_LedgerTransactions_ReversesTransactionId",
                        column: x => x.ReversesTransactionId,
                        principalTable: "LedgerTransactions",
                        principalColumn: "LedgerTransactionId");
                    table.ForeignKey(
                        name: "FK_LedgerTransactions_UserAccounts_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateTable(
                name: "FinancialReconciliationDifferences",
                columns: table => new
                {
                    ReconciliationDifferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReconciliationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DifferenceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ApartmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PeriodKey = table.Column<string>(type: "char(7)", maxLength: 7, nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SourceAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    TargetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    DifferenceAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    Classification = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Disposition = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ReviewedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialReconciliationDifferences", x => x.ReconciliationDifferenceId);
                    table.CheckConstraint("CK_FinancialReconciliationDifferences_PeriodKey", "[PeriodKey] IS NULL OR ([PeriodKey] LIKE '[0-9][0-9][0-9][0-9]-[01][0-9]' AND SUBSTRING([PeriodKey],6,2) BETWEEN '01' AND '12')");
                    table.CheckConstraint("CK_FinancialReconciliationDifferences_Scope", "[ApartmentId] IS NULL OR [BuildingId] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_FinancialReconciliationDifferences_Apartments_BuildingId_ApartmentId",
                        columns: x => new { x.BuildingId, x.ApartmentId },
                        principalTable: "Apartments",
                        principalColumns: new[] { "BuildingId", "Id" });
                    table.ForeignKey(
                        name: "FK_FinancialReconciliationDifferences_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FinancialReconciliationDifferences_FinancialReconciliationRuns_ReconciliationRunId",
                        column: x => x.ReconciliationRunId,
                        principalTable: "FinancialReconciliationRuns",
                        principalColumn: "ReconciliationRunId");
                    table.ForeignKey(
                        name: "FK_FinancialReconciliationDifferences_UserAccounts_ReviewedByAccountId",
                        column: x => x.ReviewedByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateTable(
                name: "FinancialAllocationBatches",
                columns: table => new
                {
                    AllocationBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BasisKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    InputFingerprint = table.Column<byte[]>(type: "varbinary(32)", nullable: false),
                    SourceAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", maxLength: 3, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAllocationBatches", x => x.AllocationBatchId);
                    table.UniqueConstraint("UQ_FinancialAllocationBatches_Period_Building", x => new { x.AllocationBatchId, x.PeriodId, x.BuildingId });
                    table.CheckConstraint("CK_FinancialAllocationBatches_Amounts", "[SourceAmount] >= 0 AND [AllocatedAmount] >= 0");
                    table.CheckConstraint("CK_FinancialAllocationBatches_Fingerprint", "DATALENGTH([InputFingerprint]) = 32");
                    table.CheckConstraint("CK_FinancialAllocationBatches_Status", "[Status] IN (0,1,2)");
                    table.ForeignKey(
                        name: "FK_FinancialAllocationBatches_FinancialPeriods_PeriodId_BuildingId",
                        columns: x => new { x.PeriodId, x.BuildingId },
                        principalTable: "FinancialPeriods",
                        principalColumns: new[] { "PeriodId", "BuildingId" });
                    table.ForeignKey(
                        name: "FK_FinancialAllocationBatches_LedgerTransactions_SourceTransactionId_PeriodId_BuildingId",
                        columns: x => new { x.SourceTransactionId, x.PeriodId, x.BuildingId },
                        principalTable: "LedgerTransactions",
                        principalColumns: new[] { "LedgerTransactionId", "PeriodId", "BuildingId" });
                    table.ForeignKey(
                        name: "FK_FinancialAllocationBatches_UserAccounts_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateTable(
                name: "FinancialSourceLinks",
                columns: table => new
                {
                    FinancialSourceLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LedgerTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceFingerprint = table.Column<byte[]>(type: "varbinary(32)", nullable: true),
                    LinkedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialSourceLinks", x => x.FinancialSourceLinkId);
                    table.CheckConstraint("CK_FinancialSourceLinks_Fingerprint", "[SourceFingerprint] IS NULL OR DATALENGTH([SourceFingerprint]) = 32");
                    table.ForeignKey(
                        name: "FK_FinancialSourceLinks_LedgerTransactions_LedgerTransactionId",
                        column: x => x.LedgerTransactionId,
                        principalTable: "LedgerTransactions",
                        principalColumn: "LedgerTransactionId");
                });

            migrationBuilder.CreateTable(
                name: "LedgerEntries",
                columns: table => new
                {
                    LedgerEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ApartmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntryKind = table.Column<byte>(type: "tinyint", nullable: false),
                    SignedAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EffectiveAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntries", x => x.LedgerEntryId);
                    table.CheckConstraint("CK_LedgerEntries_Amount", "[SignedAmount] <> 0");
                    table.CheckConstraint("CK_LedgerEntries_Kind", "[EntryKind] IN (1,2,3,4,5,6,7)");
                    table.ForeignKey(
                        name: "FK_LedgerEntries_Apartments_BuildingId_ApartmentId",
                        columns: x => new { x.BuildingId, x.ApartmentId },
                        principalTable: "Apartments",
                        principalColumns: new[] { "BuildingId", "Id" });
                    table.ForeignKey(
                        name: "FK_LedgerEntries_FinancialPeriods_PeriodId_BuildingId",
                        columns: x => new { x.PeriodId, x.BuildingId },
                        principalTable: "FinancialPeriods",
                        principalColumns: new[] { "PeriodId", "BuildingId" });
                    table.ForeignKey(
                        name: "FK_LedgerEntries_LedgerTransactions_LedgerTransactionId_PeriodId_BuildingId",
                        columns: x => new { x.LedgerTransactionId, x.PeriodId, x.BuildingId },
                        principalTable: "LedgerTransactions",
                        principalColumns: new[] { "LedgerTransactionId", "PeriodId", "BuildingId" });
                });

            migrationBuilder.CreateTable(
                name: "PaymentDeposits",
                columns: table => new
                {
                    PaymentDepositId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ApartmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PostedTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", maxLength: 3, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ConfirmedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CancelledByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ReversedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversalTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceSystem = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentDeposits", x => x.PaymentDepositId);
                    table.CheckConstraint("CK_PaymentDeposits_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_PaymentDeposits_Resolution", "([Status] = 0 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL) OR ([Status] = 1 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL) OR ([Status] = 2 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0) OR ([Status] = 3 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NOT NULL AND [ReversedByAccountId] IS NOT NULL AND [ReversalTransactionId] IS NOT NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0)");
                    table.CheckConstraint("CK_PaymentDeposits_Status", "[Status] IN (0,1,2,3)");
                    table.ForeignKey(
                        name: "FK_PaymentDeposits_Apartments_BuildingId_ApartmentId",
                        columns: x => new { x.BuildingId, x.ApartmentId },
                        principalTable: "Apartments",
                        principalColumns: new[] { "BuildingId", "Id" });
                    table.ForeignKey(
                        name: "FK_PaymentDeposits_FinancialPeriods_PeriodId_BuildingId",
                        columns: x => new { x.PeriodId, x.BuildingId },
                        principalTable: "FinancialPeriods",
                        principalColumns: new[] { "PeriodId", "BuildingId" });
                    table.ForeignKey(
                        name: "FK_PaymentDeposits_LedgerTransactions_PostedTransactionId_PeriodId_BuildingId",
                        columns: x => new { x.PostedTransactionId, x.PeriodId, x.BuildingId },
                        principalTable: "LedgerTransactions",
                        principalColumns: new[] { "LedgerTransactionId", "PeriodId", "BuildingId" });
                    table.ForeignKey(
                        name: "FK_PaymentDeposits_LedgerTransactions_ReversalTransactionId",
                        column: x => x.ReversalTransactionId,
                        principalTable: "LedgerTransactions",
                        principalColumn: "LedgerTransactionId");
                    table.ForeignKey(
                        name: "FK_PaymentDeposits_UserAccounts_CancelledByAccountId",
                        column: x => x.CancelledByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                    table.ForeignKey(
                        name: "FK_PaymentDeposits_UserAccounts_ConfirmedByAccountId",
                        column: x => x.ConfirmedByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                    table.ForeignKey(
                        name: "FK_PaymentDeposits_UserAccounts_ReversedByAccountId",
                        column: x => x.ReversedByAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateTable(
                name: "FinancialAllocationLines",
                columns: table => new
                {
                    AllocationLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocationBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ApartmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    ExactShare = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    RemainderRank = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAllocationLines", x => x.AllocationLineId);
                    table.CheckConstraint("CK_FinancialAllocationLines_Amounts", "[Weight] >= 0 AND [ExactShare] >= 0 AND [AllocatedAmount] >= 0");
                    table.CheckConstraint("CK_FinancialAllocationLines_Rank", "[RemainderRank] >= 0");
                    table.ForeignKey(
                        name: "FK_FinancialAllocationLines_Apartments_BuildingId_ApartmentId",
                        columns: x => new { x.BuildingId, x.ApartmentId },
                        principalTable: "Apartments",
                        principalColumns: new[] { "BuildingId", "Id" });
                    table.ForeignKey(
                        name: "FK_FinancialAllocationLines_FinancialAllocationBatches_AllocationBatchId_PeriodId_BuildingId",
                        columns: x => new { x.AllocationBatchId, x.PeriodId, x.BuildingId },
                        principalTable: "FinancialAllocationBatches",
                        principalColumns: new[] { "AllocationBatchId", "PeriodId", "BuildingId" });
                    table.ForeignKey(
                        name: "FK_FinancialAllocationLines_FinancialPeriods_PeriodId_BuildingId",
                        columns: x => new { x.PeriodId, x.BuildingId },
                        principalTable: "FinancialPeriods",
                        principalColumns: new[] { "PeriodId", "BuildingId" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAllocationBatches_CreatedByAccountId",
                table: "FinancialAllocationBatches",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAllocationBatches_PeriodId_BuildingId",
                table: "FinancialAllocationBatches",
                columns: new[] { "PeriodId", "BuildingId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAllocationBatches_Source",
                table: "FinancialAllocationBatches",
                columns: new[] { "PeriodId", "SourceTransactionId", "AlgorithmVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAllocationBatches_SourceTransactionId_PeriodId_BuildingId",
                table: "FinancialAllocationBatches",
                columns: new[] { "SourceTransactionId", "PeriodId", "BuildingId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAllocationLines_AllocationBatchId_PeriodId_BuildingId",
                table: "FinancialAllocationLines",
                columns: new[] { "AllocationBatchId", "PeriodId", "BuildingId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAllocationLines_BuildingId_ApartmentId",
                table: "FinancialAllocationLines",
                columns: new[] { "BuildingId", "ApartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAllocationLines_PeriodId_BuildingId",
                table: "FinancialAllocationLines",
                columns: new[] { "PeriodId", "BuildingId" });

            migrationBuilder.CreateIndex(
                name: "UX_FinancialAllocationLines_Batch_Apartment",
                table: "FinancialAllocationLines",
                columns: new[] { "AllocationBatchId", "ApartmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialPeriods_ClosedByAccountId",
                table: "FinancialPeriods",
                column: "ClosedByAccountId");

            migrationBuilder.CreateIndex(
                name: "UX_FinancialPeriods_Building_Key",
                table: "FinancialPeriods",
                columns: new[] { "BuildingId", "PeriodKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReconciliationDifferences_BuildingId_ApartmentId",
                table: "FinancialReconciliationDifferences",
                columns: new[] { "BuildingId", "ApartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReconciliationDifferences_ReviewedByAccountId",
                table: "FinancialReconciliationDifferences",
                column: "ReviewedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReconciliationDifferences_Run_Scope",
                table: "FinancialReconciliationDifferences",
                columns: new[] { "ReconciliationRunId", "BuildingId", "PeriodKey" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReconciliationRuns_StartedByAccountId",
                table: "FinancialReconciliationRuns",
                column: "StartedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialSourceLinks_Transaction",
                table: "FinancialSourceLinks",
                column: "LedgerTransactionId");

            migrationBuilder.CreateIndex(
                name: "UX_FinancialSourceLinks_Source",
                table: "FinancialSourceLinks",
                columns: new[] { "SourceSystem", "SourceType", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_Apartment_Period",
                table: "LedgerEntries",
                columns: new[] { "BuildingId", "ApartmentId", "PeriodId", "EffectiveAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_LedgerTransactionId_PeriodId_BuildingId",
                table: "LedgerEntries",
                columns: new[] { "LedgerTransactionId", "PeriodId", "BuildingId" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_PeriodId_BuildingId",
                table: "LedgerEntries",
                columns: new[] { "PeriodId", "BuildingId" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_Transaction",
                table: "LedgerEntries",
                column: "LedgerTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_ActorAccountId",
                table: "LedgerTransactions",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_Period_Status",
                table: "LedgerTransactions",
                columns: new[] { "PeriodId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_PeriodId_BuildingId",
                table: "LedgerTransactions",
                columns: new[] { "PeriodId", "BuildingId" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_ReversesTransactionId",
                table: "LedgerTransactions",
                column: "ReversesTransactionId");

            migrationBuilder.CreateIndex(
                name: "UX_LedgerTransactions_Idempotency",
                table: "LedgerTransactions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LedgerTransactions_Source",
                table: "LedgerTransactions",
                columns: new[] { "SourceSystem", "SourceType", "SourceKey" },
                unique: true,
                filter: "[SourceKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDeposits_Apartment_Submitted",
                table: "PaymentDeposits",
                columns: new[] { "BuildingId", "ApartmentId", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDeposits_CancelledByAccountId",
                table: "PaymentDeposits",
                column: "CancelledByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDeposits_ConfirmedByAccountId",
                table: "PaymentDeposits",
                column: "ConfirmedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDeposits_PeriodId_BuildingId",
                table: "PaymentDeposits",
                columns: new[] { "PeriodId", "BuildingId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDeposits_PostedTransactionId_PeriodId_BuildingId",
                table: "PaymentDeposits",
                columns: new[] { "PostedTransactionId", "PeriodId", "BuildingId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDeposits_ReversalTransactionId",
                table: "PaymentDeposits",
                column: "ReversalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDeposits_ReversedByAccountId",
                table: "PaymentDeposits",
                column: "ReversedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDeposits_Status_Submitted",
                table: "PaymentDeposits",
                columns: new[] { "BuildingId", "Status", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_PaymentDeposits_Source",
                table: "PaymentDeposits",
                columns: new[] { "SourceSystem", "SourceKey" },
                unique: true,
                filter: "[SourceKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialAllocationLines");

            migrationBuilder.DropTable(
                name: "FinancialReconciliationDifferences");

            migrationBuilder.DropTable(
                name: "FinancialSourceLinks");

            migrationBuilder.DropTable(
                name: "LedgerEntries");

            migrationBuilder.DropTable(
                name: "PaymentDeposits");

            migrationBuilder.DropTable(
                name: "FinancialAllocationBatches");

            migrationBuilder.DropTable(
                name: "FinancialReconciliationRuns");

            migrationBuilder.DropTable(
                name: "LedgerTransactions");

            migrationBuilder.DropTable(
                name: "FinancialPeriods");
        }
    }
}
