using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class Phase28_AuditAndSettingsDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.AuditEvents', N'Outcome') IS NOT NULL
                   AND NOT EXISTS
                   (
                       SELECT 1
                       FROM sys.default_constraints dc
                       INNER JOIN sys.columns c
                           ON c.object_id = dc.parent_object_id
                          AND c.column_id = dc.parent_column_id
                       WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AuditEvents')
                         AND c.name = N'Outcome'
                   )
                BEGIN
                    ALTER TABLE dbo.AuditEvents
                        ADD CONSTRAINT DF_AuditEvents_Outcome_Phase28 DEFAULT (0) FOR Outcome;
                END;

                IF OBJECT_ID(N'dbo.GlobalApplicationSettings', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.GlobalApplicationSettings', N'SessionTimeoutDays') IS NOT NULL
                   AND NOT EXISTS
                   (
                       SELECT 1
                       FROM sys.default_constraints dc
                       INNER JOIN sys.columns c
                           ON c.object_id = dc.parent_object_id
                          AND c.column_id = dc.parent_column_id
                       WHERE dc.parent_object_id = OBJECT_ID(N'dbo.GlobalApplicationSettings')
                         AND c.name = N'SessionTimeoutDays'
                   )
                BEGIN
                    ALTER TABLE dbo.GlobalApplicationSettings
                        ADD CONSTRAINT DF_GlobalApplicationSettings_SessionTimeoutDays_Phase28
                        DEFAULT (7) FOR SessionTimeoutDays;
                END;

                IF OBJECT_ID(N'dbo.GlobalApplicationSettings', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.GlobalApplicationSettings', N'MaxPinAttempts') IS NOT NULL
                   AND NOT EXISTS
                   (
                       SELECT 1
                       FROM sys.default_constraints dc
                       INNER JOIN sys.columns c
                           ON c.object_id = dc.parent_object_id
                          AND c.column_id = dc.parent_column_id
                       WHERE dc.parent_object_id = OBJECT_ID(N'dbo.GlobalApplicationSettings')
                         AND c.name = N'MaxPinAttempts'
                   )
                BEGIN
                    ALTER TABLE dbo.GlobalApplicationSettings
                        ADD CONSTRAINT DF_GlobalApplicationSettings_MaxPinAttempts_Phase28
                        DEFAULT (5) FOR MaxPinAttempts;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NOT NULL
                   AND OBJECT_ID(N'dbo.DF_AuditEvents_Outcome_Phase28', N'D') IS NOT NULL
                    ALTER TABLE dbo.AuditEvents DROP CONSTRAINT DF_AuditEvents_Outcome_Phase28;

                IF OBJECT_ID(N'dbo.GlobalApplicationSettings', N'U') IS NOT NULL
                   AND OBJECT_ID(N'dbo.DF_GlobalApplicationSettings_SessionTimeoutDays_Phase28', N'D') IS NOT NULL
                    ALTER TABLE dbo.GlobalApplicationSettings DROP CONSTRAINT DF_GlobalApplicationSettings_SessionTimeoutDays_Phase28;

                IF OBJECT_ID(N'dbo.GlobalApplicationSettings', N'U') IS NOT NULL
                   AND OBJECT_ID(N'dbo.DF_GlobalApplicationSettings_MaxPinAttempts_Phase28', N'D') IS NOT NULL
                    ALTER TABLE dbo.GlobalApplicationSettings DROP CONSTRAINT DF_GlobalApplicationSettings_MaxPinAttempts_Phase28;
                """);
        }
    }
}
