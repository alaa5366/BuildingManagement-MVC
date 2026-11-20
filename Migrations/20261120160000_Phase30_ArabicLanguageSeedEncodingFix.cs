using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261120160000_Phase30_ArabicLanguageSeedEncodingFix")]
public sealed class Phase30_ArabicLanguageSeedEncodingFix : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Use Unicode code points so command-line/source encoding cannot replace Arabic glyphs with '?'.
        migrationBuilder.Sql("""
            UPDATE dbo.Languages
            SET NativeName =
                NCHAR(1575) + NCHAR(1604) + NCHAR(1593) + NCHAR(1585) +
                NCHAR(1576) + NCHAR(1610) + NCHAR(1577)
            WHERE CultureCode = N'ar';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep the corrected Unicode label; rolling it back would reintroduce corrupted text.
    }
}
