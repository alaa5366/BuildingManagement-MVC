using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class Phase05_RemoveRedundantResidentIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The ApartmentId-only index may already have been removed manually
            // on the TEST database. Keep this migration safe for both states.
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.Residents', N'U') IS NOT NULL
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE name = N'IX_Residents_ApartmentId'
                          AND object_id = OBJECT_ID(N'dbo.Residents')
                    )
                        DROP INDEX [IX_Residents_ApartmentId] ON [dbo].[Residents];

                    IF EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE name = N'IX_Residents_BuildingId'
                          AND object_id = OBJECT_ID(N'dbo.Residents')
                    )
                        DROP INDEX [IX_Residents_BuildingId] ON [dbo].[Residents];
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.Residents', N'U') IS NOT NULL
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE name = N'IX_Residents_ApartmentId'
                          AND object_id = OBJECT_ID(N'dbo.Residents')
                    )
                        CREATE NONCLUSTERED INDEX [IX_Residents_ApartmentId]
                            ON [dbo].[Residents] ([ApartmentId]);

                    IF NOT EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE name = N'IX_Residents_BuildingId'
                          AND object_id = OBJECT_ID(N'dbo.Residents')
                    )
                        CREATE NONCLUSTERED INDEX [IX_Residents_BuildingId]
                            ON [dbo].[Residents] ([BuildingId]);
                END;
                """);
        }
    }
}