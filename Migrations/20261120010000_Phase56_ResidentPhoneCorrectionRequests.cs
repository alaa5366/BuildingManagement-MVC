using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261120010000_Phase56_ResidentPhoneCorrectionRequests")]
public sealed class Phase56_ResidentPhoneCorrectionRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PersonPhoneChangeRequests",
            columns: table => new
            {
                RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BuildingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ApartmentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                PreviousPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                RequestedPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                RequestedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                ReviewedByUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                ReviewedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                ReviewReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PersonPhoneChangeRequests", x => x.RequestId);
                table.ForeignKey("FK_PersonPhoneChangeRequests_Persons_PersonId", x => x.PersonId, "Persons", "PersonId");
                table.CheckConstraint("CK_PersonPhoneChangeRequests_Status", "[Status] IN (0,1,2)");
                table.CheckConstraint("CK_PersonPhoneChangeRequests_Review", "(([Status] = 0 AND [ReviewedAtUtc] IS NULL AND [ReviewedByUserId] IS NULL AND [ReviewReason] IS NULL) OR ([Status] IN (1,2) AND [ReviewedAtUtc] IS NOT NULL AND [ReviewedByUserId] IS NOT NULL AND [ReviewReason] IS NOT NULL AND LEN(LTRIM(RTRIM([ReviewReason]))) > 0))");
            });

        migrationBuilder.CreateIndex("IX_PersonPhoneChangeRequests_Queue", "PersonPhoneChangeRequests", new[] { "Status", "BuildingId", "RequestedAtUtc" });
        migrationBuilder.CreateIndex("IX_PersonPhoneChangeRequests_Person_Status", "PersonPhoneChangeRequests", new[] { "PersonId", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable("PersonPhoneChangeRequests");
}