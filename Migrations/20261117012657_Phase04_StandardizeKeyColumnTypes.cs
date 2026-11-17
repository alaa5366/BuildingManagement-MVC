using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class Phase04_StandardizeKeyColumnTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server does not allow altering columns while PK/FK constraints
            // reference them. Drop affected constraints, alter columns, then restore.
            DropPhase04Constraints(migrationBuilder);
            migrationBuilder.AlterColumn<string>(
                name: "MonthKey",
                table: "WalletAdjustments",
                type: "char(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ApartmentId",
                table: "WalletAdjustments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "WalletAdjustments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "WalletAdjustments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Uid",
                table: "Users",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Permission",
                table: "UserPermissions",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Uid",
                table: "UserPermissions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "ScheduledBackupSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "MonthKey",
                table: "Revenues",
                type: "char(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CategoryKind",
                table: "Revenues",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "CategoryId",
                table: "Revenues",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Revenues",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Revenues",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Uid",
                table: "Residents",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Residents",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ApartmentId",
                table: "Residents",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Residents",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "QrTokens",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ApartmentId",
                table: "QrTokens",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Uid",
                table: "QrTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Presence",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApartmentId",
                table: "Presence",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Uid",
                table: "Presence",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "OptionId",
                table: "PollVotes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ApartmentId",
                table: "PollVotes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "PollId",
                table: "PollVotes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "PollVotes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Polls",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Polls",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "PollOptions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "PollId",
                table: "PollOptions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "PollOptions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "RecipientApartmentId",
                table: "Notifications",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Notifications",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Notifications",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ApartmentId",
                table: "MonthlyApartmentShares",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "MonthKey",
                table: "MonthlyApartmentShares",
                type: "char(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "MonthlyApartmentShares",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "MaintenanceRecords",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "MaintenanceRecords",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "GlobalSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Floors",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Floors",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "FinancialCategories",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "FinancialCategories",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "FinancialCategories",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "MonthKey",
                table: "Expenses",
                type: "char(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CategoryKind",
                table: "Expenses",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "CategoryId",
                table: "Expenses",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Expenses",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Expenses",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Dvrs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Dvrs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "MonthKey",
                table: "Deposits",
                type: "char(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ApartmentId",
                table: "Deposits",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Deposits",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Deposits",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "DepositId",
                table: "DepositEditHistory",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "DepositEditHistory",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "DvrId",
                table: "Cameras",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Cameras",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Cameras",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "BuildingSettings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Buildings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "AdminUid",
                table: "BuildingAdmins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "BuildingAdmins",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "BackupHistory",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "MonthKey",
                table: "AuditLog",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "AuditLog",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FloorId",
                table: "Apartments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Apartments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Apartments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
            AddPhase04Constraints(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse safely: constraints must be removed before restoring old types.
            DropPhase04Constraints(migrationBuilder);
            migrationBuilder.AlterColumn<string>(
                name: "MonthKey",
                table: "WalletAdjustments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(7)",
                oldMaxLength: 7);

            migrationBuilder.AlterColumn<string>(
                name: "ApartmentId",
                table: "WalletAdjustments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "WalletAdjustments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "WalletAdjustments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Uid",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "Permission",
                table: "UserPermissions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<string>(
                name: "Uid",
                table: "UserPermissions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "ScheduledBackupSettings",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "MonthKey",
                table: "Revenues",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(7)",
                oldMaxLength: 7);

            migrationBuilder.AlterColumn<string>(
                name: "CategoryKind",
                table: "Revenues",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "CategoryId",
                table: "Revenues",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Revenues",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Revenues",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Uid",
                table: "Residents",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Residents",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "ApartmentId",
                table: "Residents",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Residents",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "QrTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "ApartmentId",
                table: "QrTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Uid",
                table: "QrTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Presence",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApartmentId",
                table: "Presence",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Uid",
                table: "Presence",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "OptionId",
                table: "PollVotes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "ApartmentId",
                table: "PollVotes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "PollId",
                table: "PollVotes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "PollVotes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Polls",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Polls",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "PollOptions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "PollId",
                table: "PollOptions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "PollOptions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "RecipientApartmentId",
                table: "Notifications",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Notifications",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Notifications",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "ApartmentId",
                table: "MonthlyApartmentShares",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "MonthKey",
                table: "MonthlyApartmentShares",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(7)",
                oldMaxLength: 7);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "MonthlyApartmentShares",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "MaintenanceRecords",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "MaintenanceRecords",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "GlobalSettings",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Floors",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Floors",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "FinancialCategories",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "FinancialCategories",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "FinancialCategories",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "MonthKey",
                table: "Expenses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(7)",
                oldMaxLength: 7);

            migrationBuilder.AlterColumn<string>(
                name: "CategoryKind",
                table: "Expenses",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "CategoryId",
                table: "Expenses",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Expenses",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Expenses",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Dvrs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Dvrs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "MonthKey",
                table: "Deposits",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(7)",
                oldMaxLength: 7);

            migrationBuilder.AlterColumn<string>(
                name: "ApartmentId",
                table: "Deposits",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Deposits",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Deposits",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "DepositId",
                table: "DepositEditHistory",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "DepositEditHistory",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "DvrId",
                table: "Cameras",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Cameras",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Cameras",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "BuildingSettings",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Buildings",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "AdminUid",
                table: "BuildingAdmins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "BuildingAdmins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "BackupHistory",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "MonthKey",
                table: "AuditLog",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(7)",
                oldMaxLength: 7);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "AuditLog",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FloorId",
                table: "Apartments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Apartments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "BuildingId",
                table: "Apartments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);
            AddPhase04Constraints(migrationBuilder);
        }

        private static void DropPhase04Constraints(MigrationBuilder migrationBuilder)
        {
            // Resolve constraints by their actual SQL Server metadata, not their names.
            // This supports both known databases whose FK names differ. Every expected
            // relationship/key must match exactly once; otherwise the migration fails closed.
            migrationBuilder.Sql(@"SET NOCOUNT ON;
DECLARE @constraintName sysname;
DECLARE @matches int;
DECLARE @sql nvarchar(max);
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Apartments -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[Apartments] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Floors') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'FloorId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Apartments -> dbo.Floors.', 1;
SET @sql = N'ALTER TABLE [dbo].[Apartments] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.AuditLog') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 2 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.AuditLog -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[AuditLog] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.BuildingAdmins') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.BuildingAdmins -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[BuildingAdmins] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.BuildingAdmins') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Users') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'AdminUid' AND rc.name=N'Uid');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.BuildingAdmins -> dbo.Users.', 1;
SET @sql = N'ALTER TABLE [dbo].[BuildingAdmins] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.BuildingSettings') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.BuildingSettings -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[BuildingSettings] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Cameras') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Dvrs') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'DvrId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Cameras -> dbo.Dvrs.', 1;
SET @sql = N'ALTER TABLE [dbo].[Cameras] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.DepositEditHistory') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Deposits') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'DepositId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.DepositEditHistory -> dbo.Deposits.', 1;
SET @sql = N'ALTER TABLE [dbo].[DepositEditHistory] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Deposits') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'ApartmentId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Deposits -> dbo.Apartments.', 1;
SET @sql = N'ALTER TABLE [dbo].[Deposits] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Deposits') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Deposits -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[Deposits] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Dvrs') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Dvrs -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[Dvrs] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Expenses') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Expenses -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[Expenses] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Expenses') AND fk.referenced_object_id = OBJECT_ID(N'dbo.FinancialCategories') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 3 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'CategoryKind' AND rc.name=N'Kind') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=3 AND pc.name=N'CategoryId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Expenses -> dbo.FinancialCategories.', 1;
SET @sql = N'ALTER TABLE [dbo].[Expenses] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.FinancialCategories') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.FinancialCategories -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[FinancialCategories] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Floors') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Floors -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[Floors] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.MaintenanceRecords') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.MaintenanceRecords -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[MaintenanceRecords] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.MonthlyApartmentShares') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'ApartmentId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.MonthlyApartmentShares -> dbo.Apartments.', 1;
SET @sql = N'ALTER TABLE [dbo].[MonthlyApartmentShares] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.MonthlyApartmentShares') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.MonthlyApartmentShares -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[MonthlyApartmentShares] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Notifications') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'RecipientApartmentId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Notifications -> dbo.Apartments.', 1;
SET @sql = N'ALTER TABLE [dbo].[Notifications] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Notifications') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Notifications -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[Notifications] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.PollOptions') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Polls') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'PollId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.PollOptions -> dbo.Polls.', 1;
SET @sql = N'ALTER TABLE [dbo].[PollOptions] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Polls') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Polls -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[Polls] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.PollVotes') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'ApartmentId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.PollVotes -> dbo.Apartments.', 1;
SET @sql = N'ALTER TABLE [dbo].[PollVotes] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.PollVotes') AND fk.referenced_object_id = OBJECT_ID(N'dbo.PollOptions') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 3 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'PollId' AND rc.name=N'PollId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=3 AND pc.name=N'OptionId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.PollVotes -> dbo.PollOptions.', 1;
SET @sql = N'ALTER TABLE [dbo].[PollVotes] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.QrTokens') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'ApartmentId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.QrTokens -> dbo.Apartments.', 1;
SET @sql = N'ALTER TABLE [dbo].[QrTokens] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.QrTokens') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.QrTokens -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[QrTokens] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Residents') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'ApartmentId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Residents -> dbo.Apartments.', 1;
SET @sql = N'ALTER TABLE [dbo].[Residents] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Revenues') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Revenues -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[Revenues] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Revenues') AND fk.referenced_object_id = OBJECT_ID(N'dbo.FinancialCategories') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 3 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'CategoryKind' AND rc.name=N'Kind') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=3 AND pc.name=N'CategoryId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.Revenues -> dbo.FinancialCategories.', 1;
SET @sql = N'ALTER TABLE [dbo].[Revenues] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.UserPermissions') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Users') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'Uid' AND rc.name=N'Uid');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.UserPermissions -> dbo.Users.', 1;
SET @sql = N'ALTER TABLE [dbo].[UserPermissions] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.WalletAdjustments') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Apartments') AND fk.delete_referential_action = 0 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 2 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=2 AND pc.name=N'ApartmentId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.WalletAdjustments -> dbo.Apartments.', 1;
SET @sql = N'ALTER TABLE [dbo].[WalletAdjustments] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(fk.name) FROM sys.foreign_keys fk WHERE fk.parent_object_id = OBJECT_ID(N'dbo.WalletAdjustments') AND fk.referenced_object_id = OBJECT_ID(N'dbo.Buildings') AND fk.delete_referential_action = 1 AND fk.is_disabled = 0 AND fk.is_not_trusted = 0 AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id=fk.object_id) = 1 AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE fkc.constraint_object_id=fk.object_id AND fkc.constraint_column_id=1 AND pc.name=N'BuildingId' AND rc.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one FK: dbo.WalletAdjustments -> dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[WalletAdjustments] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
-- Drop only the 26 primary keys whose key columns change.
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Apartments') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Apartments.', 1;
SET @sql = N'ALTER TABLE [dbo].[Apartments] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.BackupHistory') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.BackupHistory.', 1;
SET @sql = N'ALTER TABLE [dbo].[BackupHistory] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.BuildingAdmins') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'AdminUid');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.BuildingAdmins.', 1;
SET @sql = N'ALTER TABLE [dbo].[BuildingAdmins] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Buildings') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Buildings.', 1;
SET @sql = N'ALTER TABLE [dbo].[Buildings] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.BuildingSettings') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.BuildingSettings.', 1;
SET @sql = N'ALTER TABLE [dbo].[BuildingSettings] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Cameras') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Cameras.', 1;
SET @sql = N'ALTER TABLE [dbo].[Cameras] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Deposits') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Deposits.', 1;
SET @sql = N'ALTER TABLE [dbo].[Deposits] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Dvrs') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Dvrs.', 1;
SET @sql = N'ALTER TABLE [dbo].[Dvrs] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Expenses') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Expenses.', 1;
SET @sql = N'ALTER TABLE [dbo].[Expenses] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.FinancialCategories') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=3 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Kind') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=3 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.FinancialCategories.', 1;
SET @sql = N'ALTER TABLE [dbo].[FinancialCategories] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Floors') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Floors.', 1;
SET @sql = N'ALTER TABLE [dbo].[Floors] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.GlobalSettings') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.GlobalSettings.', 1;
SET @sql = N'ALTER TABLE [dbo].[GlobalSettings] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.MaintenanceRecords') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.MaintenanceRecords.', 1;
SET @sql = N'ALTER TABLE [dbo].[MaintenanceRecords] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.MonthlyApartmentShares') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=3 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'MonthKey') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=3 AND c.name=N'ApartmentId');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.MonthlyApartmentShares.', 1;
SET @sql = N'ALTER TABLE [dbo].[MonthlyApartmentShares] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Notifications') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Notifications.', 1;
SET @sql = N'ALTER TABLE [dbo].[Notifications] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.PollOptions') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=3 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'PollId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=3 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.PollOptions.', 1;
SET @sql = N'ALTER TABLE [dbo].[PollOptions] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Polls') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Polls.', 1;
SET @sql = N'ALTER TABLE [dbo].[Polls] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.PollVotes') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=3 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'PollId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=3 AND c.name=N'ApartmentId');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.PollVotes.', 1;
SET @sql = N'ALTER TABLE [dbo].[PollVotes] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Presence') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Uid');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Presence.', 1;
SET @sql = N'ALTER TABLE [dbo].[Presence] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.QrTokens') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Uid');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.QrTokens.', 1;
SET @sql = N'ALTER TABLE [dbo].[QrTokens] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Residents') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Residents.', 1;
SET @sql = N'ALTER TABLE [dbo].[Residents] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Revenues') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Revenues.', 1;
SET @sql = N'ALTER TABLE [dbo].[Revenues] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.ScheduledBackupSettings') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.ScheduledBackupSettings.', 1;
SET @sql = N'ALTER TABLE [dbo].[ScheduledBackupSettings] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.UserPermissions') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Uid') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Permission');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.UserPermissions.', 1;
SET @sql = N'ALTER TABLE [dbo].[UserPermissions] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.Users') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=1 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'Uid');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.Users.', 1;
SET @sql = N'ALTER TABLE [dbo].[Users] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
SET @constraintName = NULL; SET @matches = 0;
SELECT @matches = COUNT(*), @constraintName = MAX(kc.name) FROM sys.key_constraints kc WHERE kc.type='PK' AND kc.parent_object_id=OBJECT_ID(N'dbo.WalletAdjustments') AND (SELECT COUNT(*) FROM sys.index_columns ix WHERE ix.object_id=kc.parent_object_id AND ix.index_id=kc.unique_index_id AND ix.key_ordinal > 0)=2 AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=1 AND c.name=N'BuildingId') AND EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id AND ic.key_ordinal=2 AND c.name=N'Id');
IF @matches <> 1 THROW 51000, N'Phase04 expected exactly one PK on dbo.WalletAdjustments.', 1;
SET @sql = N'ALTER TABLE [dbo].[WalletAdjustments] DROP CONSTRAINT ' + QUOTENAME(@constraintName); EXEC sys.sp_executesql @sql;
");
        }

        private static void AddPhase04Constraints(MigrationBuilder migrationBuilder)
        {
            // Restore primary keys with their original names and column order.
            migrationBuilder.AddPrimaryKey(name: "PK_Apartments", table: "Apartments", columns: new[] { "BuildingId", "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_BackupHistory", table: "BackupHistory", columns: new[] { "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_BuildingAdmins", table: "BuildingAdmins", columns: new[] { "BuildingId", "AdminUid" });
            migrationBuilder.AddPrimaryKey(name: "PK_Buildings", table: "Buildings", columns: new[] { "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_BuildingSettings", table: "BuildingSettings", columns: new[] { "BuildingId" });
            migrationBuilder.AddPrimaryKey(name: "PK_Cameras", table: "Cameras", columns: new[] { "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_Deposits", table: "Deposits", columns: new[] { "BuildingId", "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_Dvrs", table: "Dvrs", columns: new[] { "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_Expenses", table: "Expenses", columns: new[] { "BuildingId", "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_FinancialCategories", table: "FinancialCategories", columns: new[] { "BuildingId", "Kind", "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_Floors", table: "Floors", columns: new[] { "BuildingId", "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_GlobalSettings", table: "GlobalSettings", columns: new[] { "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_MaintenanceRecords", table: "MaintenanceRecords", columns: new[] { "BuildingId", "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_MonthlyApartmentShares", table: "MonthlyApartmentShares", columns: new[] { "BuildingId", "MonthKey", "ApartmentId" });
            migrationBuilder.AddPrimaryKey(name: "PK_Notifications", table: "Notifications", columns: new[] { "BuildingId", "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_PollOptions", table: "PollOptions", columns: new[] { "BuildingId", "PollId", "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_Polls", table: "Polls", columns: new[] { "BuildingId", "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_PollVotes", table: "PollVotes", columns: new[] { "BuildingId", "PollId", "ApartmentId" });
            migrationBuilder.AddPrimaryKey(name: "PK_Presence", table: "Presence", columns: new[] { "Uid" });
            migrationBuilder.AddPrimaryKey(name: "PK_QrTokens", table: "QrTokens", columns: new[] { "Uid" });
            migrationBuilder.AddPrimaryKey(name: "PK_Residents", table: "Residents", columns: new[] { "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_Revenues", table: "Revenues", columns: new[] { "BuildingId", "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_ScheduledBackupSettings", table: "ScheduledBackupSettings", columns: new[] { "Id" });
            migrationBuilder.AddPrimaryKey(name: "PK_UserPermissions", table: "UserPermissions", columns: new[] { "Uid", "Permission" });
            migrationBuilder.AddPrimaryKey(name: "PK_Users", table: "Users", columns: new[] { "Uid" });
            migrationBuilder.AddPrimaryKey(name: "PK_WalletAdjustments", table: "WalletAdjustments", columns: new[] { "BuildingId", "Id" });

            // Restore foreign keys and their original delete behaviors.
            migrationBuilder.AddForeignKey(name: "FK_Apartments_Buildings_BuildingId", table: "Apartments", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Apartments_Floors_BuildingId_FloorId", table: "Apartments", columns: new[] { "BuildingId", "FloorId" }, principalTable: "Floors", principalColumns: new[] { "BuildingId", "Id" }, onDelete: ReferentialAction.NoAction);
            migrationBuilder.AddForeignKey(name: "FK_AuditLog_Buildings_BuildingId", table: "AuditLog", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(name: "FK_BuildingAdmins_Buildings_BuildingId", table: "BuildingAdmins", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_BuildingAdmins_Users_AdminUid", table: "BuildingAdmins", columns: new[] { "AdminUid" }, principalTable: "Users", principalColumns: new[] { "Uid" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_BuildingSettings_Buildings_BuildingId", table: "BuildingSettings", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Cameras_Dvrs_DvrId", table: "Cameras", columns: new[] { "DvrId" }, principalTable: "Dvrs", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_DepositEditHistory_Deposits_BuildingId_DepositId", table: "DepositEditHistory", columns: new[] { "BuildingId", "DepositId" }, principalTable: "Deposits", principalColumns: new[] { "BuildingId", "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Deposits_Apartments_BuildingId_ApartmentId", table: "Deposits", columns: new[] { "BuildingId", "ApartmentId" }, principalTable: "Apartments", principalColumns: new[] { "BuildingId", "Id" }, onDelete: ReferentialAction.NoAction);
            migrationBuilder.AddForeignKey(name: "FK_Deposits_Buildings_BuildingId", table: "Deposits", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Dvrs_Buildings_BuildingId", table: "Dvrs", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Expenses_Buildings_BuildingId", table: "Expenses", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Expenses_FinancialCategories_BuildingId_CategoryKind_CategoryId", table: "Expenses", columns: new[] { "BuildingId", "CategoryKind", "CategoryId" }, principalTable: "FinancialCategories", principalColumns: new[] { "BuildingId", "Kind", "Id" }, onDelete: ReferentialAction.NoAction);
            migrationBuilder.AddForeignKey(name: "FK_FinancialCategories_Buildings_BuildingId", table: "FinancialCategories", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Floors_Buildings_BuildingId", table: "Floors", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_MaintenanceRecords_Buildings_BuildingId", table: "MaintenanceRecords", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_MonthlyApartmentShares_Apartments_BuildingId_ApartmentId", table: "MonthlyApartmentShares", columns: new[] { "BuildingId", "ApartmentId" }, principalTable: "Apartments", principalColumns: new[] { "BuildingId", "Id" }, onDelete: ReferentialAction.NoAction);
            migrationBuilder.AddForeignKey(name: "FK_MonthlyApartmentShares_Buildings_BuildingId", table: "MonthlyApartmentShares", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Notifications_Apartments_BuildingId_RecipientApartmentId", table: "Notifications", columns: new[] { "BuildingId", "RecipientApartmentId" }, principalTable: "Apartments", principalColumns: new[] { "BuildingId", "Id" }, onDelete: ReferentialAction.NoAction);
            migrationBuilder.AddForeignKey(name: "FK_Notifications_Buildings_BuildingId", table: "Notifications", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_PollOptions_Polls_BuildingId_PollId", table: "PollOptions", columns: new[] { "BuildingId", "PollId" }, principalTable: "Polls", principalColumns: new[] { "BuildingId", "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Polls_Buildings_BuildingId", table: "Polls", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_PollVotes_Apartments_BuildingId_ApartmentId", table: "PollVotes", columns: new[] { "BuildingId", "ApartmentId" }, principalTable: "Apartments", principalColumns: new[] { "BuildingId", "Id" }, onDelete: ReferentialAction.NoAction);
            migrationBuilder.AddForeignKey(name: "FK_PollVotes_PollOptions_BuildingId_PollId_OptionId", table: "PollVotes", columns: new[] { "BuildingId", "PollId", "OptionId" }, principalTable: "PollOptions", principalColumns: new[] { "BuildingId", "PollId", "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_QrTokens_Apartments_BuildingId_ApartmentId", table: "QrTokens", columns: new[] { "BuildingId", "ApartmentId" }, principalTable: "Apartments", principalColumns: new[] { "BuildingId", "Id" }, onDelete: ReferentialAction.NoAction);
            migrationBuilder.AddForeignKey(name: "FK_QrTokens_Buildings_BuildingId", table: "QrTokens", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Residents_Apartments_BuildingId_ApartmentId", table: "Residents", columns: new[] { "BuildingId", "ApartmentId" }, principalTable: "Apartments", principalColumns: new[] { "BuildingId", "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Revenues_Buildings_BuildingId", table: "Revenues", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Revenues_FinancialCategories_BuildingId_CategoryKind_CategoryId", table: "Revenues", columns: new[] { "BuildingId", "CategoryKind", "CategoryId" }, principalTable: "FinancialCategories", principalColumns: new[] { "BuildingId", "Kind", "Id" }, onDelete: ReferentialAction.NoAction);
            migrationBuilder.AddForeignKey(name: "FK_UserPermissions_Users_Uid", table: "UserPermissions", columns: new[] { "Uid" }, principalTable: "Users", principalColumns: new[] { "Uid" }, onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_WalletAdjustments_Apartments_BuildingId_ApartmentId", table: "WalletAdjustments", columns: new[] { "BuildingId", "ApartmentId" }, principalTable: "Apartments", principalColumns: new[] { "BuildingId", "Id" }, onDelete: ReferentialAction.NoAction);
            migrationBuilder.AddForeignKey(name: "FK_WalletAdjustments_Buildings_BuildingId", table: "WalletAdjustments", columns: new[] { "BuildingId" }, principalTable: "Buildings", principalColumns: new[] { "Id" }, onDelete: ReferentialAction.Cascade);
        }
    }
}
