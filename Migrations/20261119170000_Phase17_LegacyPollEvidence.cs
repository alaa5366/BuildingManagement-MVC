using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BuildingManagementMvc.Data;
#nullable disable
namespace BuildingManagementMvc.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261119170000_Phase17_LegacyPollEvidence")]
public sealed class Phase17_LegacyPollEvidence : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder)
 {
  migrationBuilder.Sql("""
IF OBJECT_ID(N'dbo.PollLifecycleEvents',N'U') IS NULL
 THROW 51017,'Phase09 PollLifecycleEvents table is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PollLifecycleEvents') AND name=N'IX_PollLifecycleEvents_LegacyPoll')
 CREATE INDEX IX_PollLifecycleEvents_LegacyPoll ON dbo.PollLifecycleEvents(BuildingId,PollId,OccurredAtUtc);
""");
 }
 protected override void Down(MigrationBuilder migrationBuilder)
 {
  // Intentionally non-destructive.
 }
}
