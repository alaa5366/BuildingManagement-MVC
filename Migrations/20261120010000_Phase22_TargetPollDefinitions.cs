using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations;

/// <summary>Additive replacement for legacy poll-definition and option tables.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261120010000_Phase22_TargetPollDefinitions")]
public sealed class Phase22TargetPollDefinitions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.PollDefinitions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.PollDefinitions
                (
                    BuildingId nvarchar(64) NOT NULL,
                    Id nvarchar(128) NOT NULL,
                    Question nvarchar(300) NOT NULL,
                    Description nvarchar(2000) NOT NULL CONSTRAINT DF_PollDefinitions_Description DEFAULT N'',
                    CreatedBy nvarchar(160) NOT NULL,
                    CreatedAt datetime2(3) NOT NULL,
                    Deadline datetime2(3) NULL,
                    IsClosed bit NOT NULL,
                    CONSTRAINT PK_PollDefinitions PRIMARY KEY CLUSTERED (BuildingId, Id),
                    CONSTRAINT FK_PollDefinitions_Buildings FOREIGN KEY (BuildingId)
                        REFERENCES dbo.Buildings(Id) ON DELETE CASCADE
                );
                CREATE INDEX IX_PollDefinitions_Building_CreatedAt
                    ON dbo.PollDefinitions(BuildingId, CreatedAt DESC, Id DESC);
            END;

            IF OBJECT_ID(N'dbo.PollChoiceOptions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.PollChoiceOptions
                (
                    BuildingId nvarchar(64) NOT NULL,
                    PollId nvarchar(128) NOT NULL,
                    Id nvarchar(128) NOT NULL,
                    Text nvarchar(200) NOT NULL,
                    SortOrder int NOT NULL,
                    CONSTRAINT PK_PollChoiceOptions PRIMARY KEY CLUSTERED (BuildingId, PollId, Id),
                    CONSTRAINT FK_PollChoiceOptions_PollDefinitions FOREIGN KEY (BuildingId, PollId)
                        REFERENCES dbo.PollDefinitions(BuildingId, Id) ON DELETE CASCADE
                );
                CREATE INDEX IX_PollChoiceOptions_Order
                    ON dbo.PollChoiceOptions(BuildingId, PollId, SortOrder);
            END;

            IF COL_LENGTH(N'dbo.BackupJobs', N'MetadataJson') IS NULL
                ALTER TABLE dbo.BackupJobs ADD MetadataJson nvarchar(max) NULL;

            IF OBJECT_ID(N'dbo.MediaDevices', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.MediaDevices
                (
                    MediaDeviceId uniqueidentifier NOT NULL CONSTRAINT PK_MediaDevices PRIMARY KEY,
                    BuildingId nvarchar(64) NOT NULL,
                    Name nvarchar(160) NOT NULL,
                    HostAddress nvarchar(255) NOT NULL,
                    Port int NOT NULL,
                    Brand nvarchar(80) NOT NULL,
                    Username nvarchar(160) NOT NULL,
                    IsActive bit NOT NULL,
                    CreatedAtUtc datetime2(3) NOT NULL,
                    UpdatedAtUtc datetime2(3) NULL,
                    CONSTRAINT FK_MediaDevices_Buildings FOREIGN KEY(BuildingId)
                        REFERENCES dbo.Buildings(Id)
                );
                CREATE UNIQUE INDEX UQ_MediaDevices_Building_Host_Port
                    ON dbo.MediaDevices(BuildingId, HostAddress, Port);
            END;

            IF OBJECT_ID(N'dbo.MediaDeviceChannels', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.MediaDeviceChannels
                (
                    ChannelId uniqueidentifier NOT NULL CONSTRAINT PK_MediaDeviceChannels PRIMARY KEY,
                    MediaDeviceId uniqueidentifier NOT NULL,
                    BuildingId nvarchar(64) NOT NULL,
                    Name nvarchar(160) NOT NULL,
                    ChannelNumber int NOT NULL,
                    RtspPath nvarchar(500) NOT NULL,
                    IsActive bit NOT NULL,
                    CreatedAtUtc datetime2(3) NOT NULL,
                    UpdatedAtUtc datetime2(3) NULL,
                    CONSTRAINT FK_MediaDeviceChannels_Device FOREIGN KEY(MediaDeviceId)
                        REFERENCES dbo.MediaDevices(MediaDeviceId) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX UQ_MediaDeviceChannels_Device_Channel
                    ON dbo.MediaDeviceChannels(MediaDeviceId, ChannelNumber);
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.QrCapabilities')
                           AND name = N'IX_QrCapabilities_Target_Expiry')
                CREATE INDEX IX_QrCapabilities_Target_Expiry
                    ON dbo.QrCapabilities(BuildingId, ApartmentId, ExpiresAtUtc);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.QrCapabilityUses')
                           AND name = N'IX_QrCapabilityUses_Capability_UsedAt')
                CREATE INDEX IX_QrCapabilityUses_Capability_UsedAt
                    ON dbo.QrCapabilityUses(CapabilityId, UsedAtUtc);
            """);
    }

    // Target poll definitions are operational history. Down intentionally preserves them.
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
