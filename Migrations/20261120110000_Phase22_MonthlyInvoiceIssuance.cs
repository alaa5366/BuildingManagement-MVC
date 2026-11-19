using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261120110000_Phase22_MonthlyInvoiceIssuance")]
public sealed class Phase22_MonthlyInvoiceIssuance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InvoiceIssues",
            columns: table => new
            {
                InvoiceSerial = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ApartmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                MonthKey = table.Column<string>(type: "char(7)", maxLength: 7, nullable: false),
                InvoiceNo = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                IssuedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InvoiceIssues", x => x.InvoiceSerial);
                table.CheckConstraint("CK_InvoiceIssues_MonthKey", "[MonthKey] LIKE '[0-9][0-9][0-9][0-9]-[01][0-9]' AND SUBSTRING([MonthKey],6,2) BETWEEN '01' AND '12'");
                table.ForeignKey(
                    name: "FK_InvoiceIssues_Apartments_BuildingId_ApartmentId",
                    columns: x => new { x.BuildingId, x.ApartmentId },
                    principalTable: "Apartments",
                    principalColumns: new[] { "BuildingId", "Id" },
                    onDelete: ReferentialAction.NoAction);
            });

        migrationBuilder.CreateIndex(
            name: "UX_InvoiceIssues_Apartment_Month",
            table: "InvoiceIssues",
            columns: new[] { "BuildingId", "ApartmentId", "MonthKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_InvoiceIssues_Number",
            table: "InvoiceIssues",
            column: "InvoiceNo",
            unique: true,
            filter: "[InvoiceNo] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "InvoiceIssues");
    }
}
