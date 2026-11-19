using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261120010000_Phase21_DepositSubmissionEvidence")]
public sealed class Phase21_DepositSubmissionEvidence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Note",
            table: "PaymentDeposits",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ReceiptMetadataJson",
            table: "PaymentDeposits",
            type: "nvarchar(8000)",
            maxLength: 8000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Note", table: "PaymentDeposits");
        migrationBuilder.DropColumn(name: "ReceiptMetadataJson", table: "PaymentDeposits");
    }
}
