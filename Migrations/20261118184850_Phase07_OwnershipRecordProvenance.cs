using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class Phase07_OwnershipRecordProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "RecordSource",
                table: "ApartmentOwnerships",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApartmentOwnerships_RecordActor",
                table: "ApartmentOwnerships",
                sql: "([RecordSource] <> 0 OR [RecordedByAccountId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApartmentOwnerships_RecordSource",
                table: "ApartmentOwnerships",
                sql: "[RecordSource] IN (0,1,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ApartmentOwnerships_RecordActor",
                table: "ApartmentOwnerships");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApartmentOwnerships_RecordSource",
                table: "ApartmentOwnerships");

            migrationBuilder.DropColumn(
                name: "RecordSource",
                table: "ApartmentOwnerships");
        }
    }
}
