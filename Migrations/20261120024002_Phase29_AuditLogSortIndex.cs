using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class Phase29_AuditLogSortIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditEvents_OccurredAt",
                table: "AuditEvents");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OccurredAt",
                table: "AuditEvents",
                columns: new[] { "OccurredAtUtc", "AuditEventId" },
                descending: new[] { true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditEvents_OccurredAt",
                table: "AuditEvents");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OccurredAt",
                table: "AuditEvents",
                columns: new[] { "OccurredAtUtc", "AuditEventId" },
                descending: new[] { true, false });
        }
    }
}
