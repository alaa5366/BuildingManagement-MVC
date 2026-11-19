using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261120120000_Phase23_SavedWhatsAppTemplates")]
public sealed class Phase23_SavedWhatsAppTemplates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SavedWhatsAppTemplates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                CreatedByUid = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SavedWhatsAppTemplates", x => x.Id);
                table.CheckConstraint("CK_SavedWhatsAppTemplates_Name", "LEN(LTRIM(RTRIM([Name]))) > 0");
            });

        migrationBuilder.CreateIndex(
            name: "IX_SavedWhatsAppTemplates_Building_Active_Name",
            table: "SavedWhatsAppTemplates",
            columns: new[] { "BuildingId", "IsActive", "Name" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SavedWhatsAppTemplates");
    }
}
