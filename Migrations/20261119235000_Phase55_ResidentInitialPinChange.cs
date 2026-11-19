using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261119235000_Phase55_ResidentInitialPinChange")]
public sealed class Phase55_ResidentInitialPinChange : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "MustChangePin",
            table: "UserAccounts",
            type: "bit",
            nullable: false,
            defaultValue: false);

        // The resident role is a code-owned baseline role. It carries no admin grants.
        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM dbo.RoleDefinitions WHERE RoleKey = N'resident')
            BEGIN
                INSERT dbo.RoleDefinitions(RoleKey, DisplayName, IsSystemRole, IsActive)
                VALUES (N'resident', N'Resident', 1, 1);
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MustChangePin",
            table: "UserAccounts");
    }
}
