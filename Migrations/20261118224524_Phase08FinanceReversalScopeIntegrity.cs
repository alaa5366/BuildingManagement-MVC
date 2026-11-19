using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class Phase08FinanceReversalScopeIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LedgerTransactions_LedgerTransactions_ReversesTransactionId",
                table: "LedgerTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentDeposits_LedgerTransactions_ReversalTransactionId",
                table: "PaymentDeposits");

            migrationBuilder.DropIndex(
                name: "IX_PaymentDeposits_ReversalTransactionId",
                table: "PaymentDeposits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentDeposits_Resolution",
                table: "PaymentDeposits");

            migrationBuilder.DropIndex(
                name: "IX_LedgerTransactions_ReversesTransactionId",
                table: "LedgerTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDeposits_ReversalTransactionId_PeriodId_BuildingId",
                table: "PaymentDeposits",
                columns: new[] { "ReversalTransactionId", "PeriodId", "BuildingId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentDeposits_Resolution",
                table: "PaymentDeposits",
                sql: "([Status] = 0 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND [ReversalTransactionId] IS NULL) OR ([Status] = 1 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND [ReversalTransactionId] IS NULL) OR ([Status] = 2 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND [ReversalTransactionId] IS NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0) OR ([Status] = 3 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NOT NULL AND [ReversedByAccountId] IS NOT NULL AND [ReversalTransactionId] IS NOT NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0)");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_ReversesTransactionId_PeriodId_BuildingId",
                table: "LedgerTransactions",
                columns: new[] { "ReversesTransactionId", "PeriodId", "BuildingId" });

            migrationBuilder.AddForeignKey(
                name: "FK_LedgerTransactions_LedgerTransactions_ReversesTransactionId_PeriodId_BuildingId",
                table: "LedgerTransactions",
                columns: new[] { "ReversesTransactionId", "PeriodId", "BuildingId" },
                principalTable: "LedgerTransactions",
                principalColumns: new[] { "LedgerTransactionId", "PeriodId", "BuildingId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentDeposits_LedgerTransactions_ReversalTransactionId_PeriodId_BuildingId",
                table: "PaymentDeposits",
                columns: new[] { "ReversalTransactionId", "PeriodId", "BuildingId" },
                principalTable: "LedgerTransactions",
                principalColumns: new[] { "LedgerTransactionId", "PeriodId", "BuildingId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LedgerTransactions_LedgerTransactions_ReversesTransactionId_PeriodId_BuildingId",
                table: "LedgerTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentDeposits_LedgerTransactions_ReversalTransactionId_PeriodId_BuildingId",
                table: "PaymentDeposits");

            migrationBuilder.DropIndex(
                name: "IX_PaymentDeposits_ReversalTransactionId_PeriodId_BuildingId",
                table: "PaymentDeposits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentDeposits_Resolution",
                table: "PaymentDeposits");

            migrationBuilder.DropIndex(
                name: "IX_LedgerTransactions_ReversesTransactionId_PeriodId_BuildingId",
                table: "LedgerTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDeposits_ReversalTransactionId",
                table: "PaymentDeposits",
                column: "ReversalTransactionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentDeposits_Resolution",
                table: "PaymentDeposits",
                sql: "([Status] = 0 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL) OR ([Status] = 1 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL) OR ([Status] = 2 AND [PostedTransactionId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [ReversedAtUtc] IS NULL AND [ReversedByAccountId] IS NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0) OR ([Status] = 3 AND [PostedTransactionId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [ReversedAtUtc] IS NOT NULL AND [ReversedByAccountId] IS NOT NULL AND [ReversalTransactionId] IS NOT NULL AND LEN(LTRIM(RTRIM([ResolutionReason]))) > 0)");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_ReversesTransactionId",
                table: "LedgerTransactions",
                column: "ReversesTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_LedgerTransactions_LedgerTransactions_ReversesTransactionId",
                table: "LedgerTransactions",
                column: "ReversesTransactionId",
                principalTable: "LedgerTransactions",
                principalColumn: "LedgerTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentDeposits_LedgerTransactions_ReversalTransactionId",
                table: "PaymentDeposits",
                column: "ReversalTransactionId",
                principalTable: "LedgerTransactions",
                principalColumn: "LedgerTransactionId");
        }
    }
}
