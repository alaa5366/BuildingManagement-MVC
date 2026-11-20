using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class Phase27_UserPreferencesModelMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // UserPreferences was created by Phase09 through guarded SQL. This migration
            // records its EF model mapping without attempting to recreate the existing table.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The table belongs to Phase09; rolling back this mapping must not drop user data.
        }
    }
}
