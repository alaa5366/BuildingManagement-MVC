USE [master]
GO
/****** Object:  Database [BuildingManagement]    Script Date: 16/11/2026 01:14:38 PM ******/
ALTER DATABASE [BuildingManagement] SET QUOTED_IDENTIFIER ON
GO
ALTER DATABASE [BuildingManagement] SET ANSI_NULLS ON
GO
ALTER DATABASE [BuildingManagement] SET ANSI_PADDING ON
GO
ALTER DATABASE [BuildingManagement] SET ANSI_WARNINGS ON
GO
ALTER DATABASE [BuildingManagement] SET ARITHABORT ON
GO
ALTER DATABASE [BuildingManagement] SET CONCAT_NULL_YIELDS_NULL ON
GO
ALTER DATABASE [BuildingManagement] SET NUMERIC_ROUNDABORT OFF
GO
ALTER DATABASE [BuildingManagement] SET AUTO_CLOSE OFF
GO
USE [BuildingManagement]
GO
CREATE TABLE [dbo].[Apartments](
	[BuildingId] [nvarchar](64) NOT NULL,
	[Id] [nvarchar](64) NOT NULL,
	[FloorId] [nvarchar](64) NOT NULL,
	[Number] [int] NOT NULL,
	[Owner] [nvarchar](200) NOT NULL,
	[Phone] [nvarchar](32) NOT NULL,
	[Email] [nvarchar](256) NOT NULL,
	[MonthlyFee] [decimal](18, 2) NOT NULL,
	[Pin] [nvarchar](256) NOT NULL,
	[IsClosed] [bit] NOT NULL,
	[Label] [nvarchar](100) NOT NULL,
	[Notes] [nvarchar](1000) NOT NULL,
	[OpenDate] [date] NULL,
	[CloseDate] [date] NULL,
	[IsDisabled] [bit] NOT NULL,
	[DisabledReason] [nvarchar](500) NOT NULL,
	[Language] [nvarchar](10) NOT NULL,
	[NotifyWhatsApp] [bit] NOT NULL,
	[NotifyEmail] [bit] NOT NULL,
	[NotifyInApp] [bit] NOT NULL,
	[PreferredPaymentMethod] [nvarchar](30) NOT NULL,
	[SettingsUpdatedAt] [datetime2](0) NULL,
	[SettingsUpdatedBy] [nvarchar](128) NULL,
 CONSTRAINT [PK_Apartments] PRIMARY KEY CLUSTERED 
(
	[BuildingId] ASC,
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Apartments_Number] UNIQUE NONCLUSTERED 
(
	[BuildingId] ASC,
	[Number] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Deposits]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Deposits](
	[BuildingId] [nvarchar](64) NOT NULL,
	[Id] [nvarchar](64) NOT NULL,
	[MonthKey] [char](7) NOT NULL,
	[Number] [nvarchar](50) NOT NULL,
	[ApartmentId] [nvarchar](64) NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[Note] [nvarchar](1000) NOT NULL,
	[Status] [nvarchar](20) NOT NULL,
	[ReceiptUrl] [nvarchar](1000) NULL,
	[ReceiptPath] [nvarchar](500) NULL,
	[ReceiptFileName] [nvarchar](260) NULL,
	[ReceiptFileSize] [bigint] NULL,
	[ReceiptFileType] [nvarchar](100) NULL,
	[ReceiptSuccess] [bit] NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
	[CreatedBy] [nvarchar](128) NOT NULL,
	[ConfirmedAt] [datetime2](0) NULL,
	[ConfirmedBy] [nvarchar](128) NULL,
	[CancelledAt] [datetime2](0) NULL,
	[CancelledBy] [nvarchar](128) NULL,
	[CancelledReason] [nvarchar](500) NULL,
	[UpdatedAt] [datetime2](0) NULL,
	[UpdatedBy] [nvarchar](128) NULL,
	[UpdateReason] [nvarchar](500) NULL,
 CONSTRAINT [PK_Deposits] PRIMARY KEY CLUSTERED 
(
	[BuildingId] ASC,
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WalletAdjustments]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WalletAdjustments](
	[BuildingId] [nvarchar](64) NOT NULL,
	[Id] [nvarchar](64) NOT NULL,
	[ApartmentId] [nvarchar](64) NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[Reason] [nvarchar](500) NOT NULL,
	[MonthKey] [char](7) NOT NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
	[CreatedBy] [nvarchar](128) NOT NULL,
 CONSTRAINT [PK_WalletAdjustments] PRIMARY KEY CLUSTERED 
(
	[BuildingId] ASC,
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[vw_ApartmentWalletMovements]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* =====================================================================
   15) View مساعد: مجموع الدفعات المؤكدة + التعديلات لكل شقة
   (ده مجموع حركات المحفظة فقط؛ منطق الرصيد الكامل لسه في WalletService)
   ===================================================================== */
CREATE VIEW [dbo].[vw_ApartmentWalletMovements]
AS
SELECT
    a.BuildingId,
    a.Id AS ApartmentId,
    a.Number AS ApartmentNumber,
    ISNULL(d.ConfirmedDeposits, 0) AS ConfirmedDeposits,
    ISNULL(w.Adjustments, 0)       AS Adjustments,
    ISNULL(d.ConfirmedDeposits, 0) + ISNULL(w.Adjustments, 0) AS NetMovements
FROM dbo.Apartments a
OUTER APPLY (
    SELECT SUM(Amount) AS ConfirmedDeposits
    FROM dbo.Deposits
    WHERE BuildingId = a.BuildingId AND ApartmentId = a.Id AND Status = N'confirmed'
) d
OUTER APPLY (
    SELECT SUM(Amount) AS Adjustments
    FROM dbo.WalletAdjustments
    WHERE BuildingId = a.BuildingId AND ApartmentId = a.Id
) w;

GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AuditLog]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AuditLog](
	[AuditId] [bigint] IDENTITY(1,1) NOT NULL,
	[SourceId] [nvarchar](64) NULL,
	[BuildingId] [nvarchar](64) NULL,
	[Ts] [datetime2](0) NOT NULL,
	[Action] [nvarchar](100) NOT NULL,
	[Label] [nvarchar](300) NOT NULL,
	[Actor] [nvarchar](200) NOT NULL,
	[ActorRole] [nvarchar](20) NOT NULL,
	[Details] [nvarchar](max) NOT NULL,
	[MonthKey] [nvarchar](7) NOT NULL,
	[BuildingNumber] [nvarchar](50) NULL,
	[AptNumber] [int] NULL,
	[ApartmentId] [nvarchar](64) NULL,
	[Severity] [nvarchar](20) NOT NULL,
 CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED 
(
	[AuditId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BackupHistory]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BackupHistory](
	[Id] [nvarchar](64) NOT NULL,
	[FileName] [nvarchar](260) NOT NULL,
	[FileSize] [bigint] NOT NULL,
	[CollectionsJson] [nvarchar](max) NOT NULL,
	[TotalDocuments] [int] NOT NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
	[DurationSeconds] [float] NOT NULL,
	[Status] [nvarchar](20) NOT NULL,
	[Source] [nvarchar](20) NOT NULL,
	[ErrorMessage] [nvarchar](max) NULL,
 CONSTRAINT [PK_BackupHistory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BuildingAdmins]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BuildingAdmins](
	[BuildingId] [nvarchar](64) NOT NULL,
	[AdminUid] [nvarchar](128) NOT NULL,
 CONSTRAINT [PK_BuildingAdmins] PRIMARY KEY CLUSTERED 
(
	[BuildingId] ASC,
	[AdminUid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Buildings]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Buildings](
	[Id] [nvarchar](64) NOT NULL,
	[BuildingNumber] [nvarchar](50) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[AdminPin] [nvarchar](256) NOT NULL,
	[DataVersion] [nvarchar](20) NOT NULL,
	[LogoUrl] [nvarchar](1000) NOT NULL,
	[AdminWhatsapp] [nvarchar](32) NOT NULL,
	[PaymentLabel] [nvarchar](200) NOT NULL,
	[PaymentAccountNumber] [nvarchar](100) NOT NULL,
	[PaymentPhone] [nvarchar](32) NOT NULL,
	[PaymentNotes] [nvarchar](500) NOT NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
	[RowVersion] [timestamp] NOT NULL,
 CONSTRAINT [PK_Buildings] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Buildings_BuildingNumber] UNIQUE NONCLUSTERED 
(
	[BuildingNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BuildingSettings]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BuildingSettings](
	[BuildingId] [nvarchar](64) NOT NULL,
	[DisplayName] [nvarchar](200) NOT NULL,
	[Address] [nvarchar](500) NOT NULL,
	[WhatsappNumber] [nvarchar](32) NOT NULL,
	[InvoiceDayOfMonth] [int] NOT NULL,
	[ExpenseDistribution] [nvarchar](30) NOT NULL,
	[VotingQuorumPercent] [int] NOT NULL,
	[Currency] [nvarchar](10) NOT NULL,
	[UpdatedAt] [datetime2](0) NULL,
	[UpdatedBy] [nvarchar](128) NULL,
 CONSTRAINT [PK_BuildingSettings] PRIMARY KEY CLUSTERED 
(
	[BuildingId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Cameras]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Cameras](
	[Id] [nvarchar](64) NOT NULL,
	[DvrId] [nvarchar](64) NOT NULL,
	[BuildingId] [nvarchar](64) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Channel] [int] NOT NULL,
	[RtspPath] [nvarchar](300) NOT NULL,
	[HlsUrl] [nvarchar](300) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
 CONSTRAINT [PK_Cameras] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DepositEditHistory]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DepositEditHistory](
	[EditId] [bigint] IDENTITY(1,1) NOT NULL,
	[BuildingId] [nvarchar](64) NOT NULL,
	[DepositId] [nvarchar](64) NOT NULL,
	[Ts] [datetime2](0) NOT NULL,
	[ByUser] [nvarchar](128) NOT NULL,
	[Reason] [nvarchar](500) NOT NULL,
	[OldAmount] [decimal](18, 2) NOT NULL,
	[NewAmount] [decimal](18, 2) NOT NULL,
	[OldNote] [nvarchar](1000) NOT NULL,
	[NewNote] [nvarchar](1000) NOT NULL,
 CONSTRAINT [PK_DepositEditHistory] PRIMARY KEY CLUSTERED 
(
	[EditId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Dvrs]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Dvrs](
	[Id] [nvarchar](64) NOT NULL,
	[BuildingId] [nvarchar](64) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[IpAddress] [nvarchar](50) NOT NULL,
	[Port] [int] NOT NULL,
	[Brand] [nvarchar](50) NOT NULL,
	[Username] [nvarchar](100) NOT NULL,
	[PasswordEncrypted] [nvarchar](500) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
	[UpdatedAt] [datetime2](0) NULL,
 CONSTRAINT [PK_Dvrs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Expenses]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Expenses](
	[BuildingId] [nvarchar](64) NOT NULL,
	[Id] [nvarchar](64) NOT NULL,
	[MonthKey] [char](7) NOT NULL,
	[CategoryId] [nvarchar](64) NULL,
	[CategoryKind] [nvarchar](10) NOT NULL,
	[Note] [nvarchar](1000) NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[ExpenseDate] [date] NOT NULL,
	[ReceiptUrl] [nvarchar](1000) NULL,
 CONSTRAINT [PK_Expenses] PRIMARY KEY CLUSTERED 
(
	[BuildingId] ASC,
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FinancialCategories]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FinancialCategories](
	[BuildingId] [nvarchar](64) NOT NULL,
	[Id] [nvarchar](64) NOT NULL,
	[Kind] [nvarchar](10) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Color] [nvarchar](20) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[SortOrder] [int] NOT NULL,
 CONSTRAINT [PK_FinancialCategories] PRIMARY KEY CLUSTERED 
(
	[BuildingId] ASC,
	[Kind] ASC,
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Floors]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Floors](
	[BuildingId] [nvarchar](64) NOT NULL,
	[Id] [nvarchar](64) NOT NULL,
	[Label] [nvarchar](100) NOT NULL,
	[SortOrder] [int] NOT NULL,
 CONSTRAINT [PK_Floors] PRIMARY KEY CLUSTERED 
(
	[BuildingId] ASC,
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[GlobalSettings]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GlobalSettings](
	[Id] [nvarchar](20) NOT NULL,
	[AppName] [nvarchar](200) NOT NULL,
	[DefaultLanguage] [nvarchar](10) NOT NULL,
	[WhatsappApiToken] [nvarchar](500) NOT NULL,
	[WhatsappPhoneId] [nvarchar](100) NOT NULL,
	[CloudinaryCloudName] [nvarchar](100) NOT NULL,
	[CloudinaryApiKey] [nvarchar](100) NOT NULL,
	[StorageMode] [nvarchar](20) NOT NULL,
	[EnablePolls] [bit] NOT NULL,
	[EnableMaintenance] [bit] NOT NULL,
	[EnableOcr] [bit] NOT NULL,
	[EnableQrAccess] [bit] NOT NULL,
	[EnableWhatsApp] [bit] NOT NULL,
	[SessionTimeoutDays] [int] NOT NULL,
	[MaxPinAttempts] [int] NOT NULL,
	[UpdatedAt] [datetime2](0) NULL,
	[UpdatedBy] [nvarchar](128) NULL,
 CONSTRAINT [PK_GlobalSettings] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[LoginThrottle]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LoginThrottle](
	[Key] [nvarchar](80) NOT NULL,
	[Count] [int] NOT NULL,
	[ExpiresAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Key] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MaintenanceRecords]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MaintenanceRecords](
	[BuildingId] [nvarchar](64) NOT NULL,
	[Id] [nvarchar](64) NOT NULL,
	[Title] [nvarchar](300) NOT NULL,
	[Description] [nvarchar](2000) NOT NULL,
	[MaintenanceDate] [date] NOT NULL,
	[Cost] [decimal](18, 2) NOT NULL,
	[Vendor] [nvarchar](200) NOT NULL,
	[Status] [nvarchar](20) NOT NULL,
	[CreatedBy] [nvarchar](128) NOT NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
	[AddedToExpense] [bit] NOT NULL,
	[AddedToExpenseAt] [datetime2](0) NULL,
 CONSTRAINT [PK_MaintenanceRecords] PRIMARY KEY CLUSTERED 
(
	[BuildingId] ASC,
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MonthlyApartmentShares]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MonthlyApartmentShares](
	[BuildingId] [nvarchar](64) NOT NULL,
	[MonthKey] [char](7) NOT NULL,
	[ApartmentId] [nvarchar](64) NOT NULL,
	[ExpenseShare] [decimal](18, 2) NOT NULL,
	[RevenueShare] [decimal](18, 2) NOT NULL,
	[CarryOver] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_MonthlyApartmentShares] PRIMARY KEY CLUSTERED 
(
	[BuildingId] ASC,
	[MonthKey] ASC,
	[ApartmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Notifications]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Notifications](
	[BuildingId] [nvarchar](64) NOT NULL,
	[Id] [nvarchar](64) NOT NULL,
	[Ts] [datetime2](0) NOT NULL,
	[RecipientType] [nvarchar](10) NOT NULL,
	[RecipientApartmentId] [nvarchar](64) NULL,
	[Type] [nvarchar](30) NOT NULL,
	[Icon] [nvarchar](20) NOT NULL,
	[Title] [nvarchar](300) NOT NULL,
	[Body] [nvarchar](2000) NOT NULL,
	[IsRead] [bit] NOT NULL,
	[ReadAt] [datetime2](0) NULL,
 CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED 
(
	[BuildingId] ASC,
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PollOptions]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PollOptions](
	[BuildingId] [nvarchar](64) NOT NULL,
	[PollId] [nvarchar](64) NOT NULL,
	[Id] [nvarchar](64) NOT NULL,
	[Text] [nvarchar](500) NOT NULL,
	[SortOrder] [int] NOT NULL,
 CONSTRAINT [PK_PollOptions] PRIMARY KEY CLUSTERED 
(
	[BuildingId] ASC,
	[PollId] ASC,
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Polls]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Polls](
	[BuildingId] [nvarchar](64) NOT NULL,
	[Id] [nvarchar](64) NOT NULL,
	[Question] [nvarchar](500) NOT NULL,
	[Description] [nvarchar](2000) NOT NULL,
	[CreatedBy] [nvarchar](128) NOT NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
	[Deadline] [datetime2](0) NULL,
	[IsClosed] [bit] NOT NULL,
 CONSTRAINT [PK_Polls] PRIMARY KEY CLUSTERED 
(
	[BuildingId] ASC,
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PollVotes]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PollVotes](
	[BuildingId] [nvarchar](64) NOT NULL,
	[PollId] [nvarchar](64) NOT NULL,
	[ApartmentId] [nvarchar](64) NOT NULL,
	[OptionId] [nvarchar](64) NOT NULL,
	[VotedAt] [datetime2](0) NOT NULL,
 CONSTRAINT [PK_PollVotes] PRIMARY KEY CLUSTERED 
(
	[BuildingId] ASC,
	[PollId] ASC,
	[ApartmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Presence]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Presence](
	[Uid] [nvarchar](128) NOT NULL,
	[BuildingId] [nvarchar](64) NULL,
	[ApartmentId] [nvarchar](64) NULL,
	[LastSeen] [datetime2](3) NOT NULL,
 CONSTRAINT [PK_Presence] PRIMARY KEY CLUSTERED 
(
	[Uid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QrTokens]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QrTokens](
	[Uid] [nvarchar](128) NOT NULL,
	[BuildingId] [nvarchar](64) NOT NULL,
	[ApartmentId] [nvarchar](64) NOT NULL,
	[ExpiresAt] [datetime2](3) NOT NULL,
	[UseType] [nvarchar](10) NOT NULL,
	[IssuedAt] [datetime2](3) NOT NULL,
	[CreatedBy] [nvarchar](128) NOT NULL,
	[IsUsed] [bit] NOT NULL,
	[UsedAt] [datetime2](0) NULL,
	[UsedBy] [nvarchar](128) NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
 CONSTRAINT [PK_QrTokens] PRIMARY KEY CLUSTERED 
(
	[Uid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QrUsages]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QrUsages](
	[Id] [nvarchar](128) NOT NULL,
	[TokenId] [nvarchar](128) NOT NULL,
	[UseCount] [int] NOT NULL,
	[FirstUsedAt] [nvarchar](max) NOT NULL,
	[LastUsedAt] [nvarchar](max) NOT NULL,
	[DeviceFingerprint] [nvarchar](max) NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
 CONSTRAINT [PK_QrUsages] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Residents]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Residents](
	[Id] [nvarchar](64) NOT NULL,
	[Uid] [nvarchar](128) NOT NULL,
	[BuildingId] [nvarchar](64) NOT NULL,
	[ApartmentId] [nvarchar](64) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Phone] [nvarchar](32) NOT NULL,
	[Whatsapp] [nvarchar](32) NOT NULL,
	[Email] [nvarchar](256) NOT NULL,
	[PhotoUrl] [nvarchar](1000) NOT NULL,
	[PinHash] [nvarchar](256) NOT NULL,
	[PasswordHash] [nvarchar](256) NULL,
	[IsActive] [bit] NOT NULL,
	[IsDisabled] [bit] NOT NULL,
	[DisabledReason] [nvarchar](500) NOT NULL,
	[IsOwner] [bit] NOT NULL,
	[IsPrimary] [bit] NOT NULL,
	[LastLoginAt] [datetime2](0) NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
	[CreatedBy] [nvarchar](128) NULL,
	[UpdatedAt] [datetime2](0) NULL,
	[UpdatedBy] [nvarchar](128) NULL,
 CONSTRAINT [PK_Residents] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Revenues]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Revenues](
	[BuildingId] [nvarchar](64) NOT NULL,
	[Id] [nvarchar](64) NOT NULL,
	[MonthKey] [char](7) NOT NULL,
	[CategoryId] [nvarchar](64) NULL,
	[CategoryKind] [nvarchar](10) NOT NULL,
	[Note] [nvarchar](1000) NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[RevenueDate] [date] NOT NULL,
 CONSTRAINT [PK_Revenues] PRIMARY KEY CLUSTERED 
(
	[BuildingId] ASC,
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ScheduledBackupSettings]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ScheduledBackupSettings](
	[Id] [nvarchar](20) NOT NULL,
	[Enabled] [bit] NOT NULL,
	[Frequency] [nvarchar](20) NOT NULL,
	[Hour] [int] NOT NULL,
	[DayOfWeek] [int] NULL,
	[DayOfMonth] [int] NULL,
	[CollectionsJson] [nvarchar](max) NOT NULL,
	[MaxBackupsToKeep] [int] NOT NULL,
	[LastRunAt] [datetime2](0) NULL,
	[LastRunStatus] [nvarchar](30) NULL,
	[UpdatedAt] [datetime2](0) NULL,
	[UpdatedBy] [nvarchar](128) NULL,
 CONSTRAINT [PK_SBS] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SyncPendingChanges]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SyncPendingChanges](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[EntityType] [nvarchar](50) NOT NULL,
	[EntityId] [nvarchar](128) NOT NULL,
	[Operation] [nvarchar](20) NOT NULL,
	[Payload] [nvarchar](max) NULL,
	[CreatedAt] [datetime2](0) NOT NULL,
	[Applied] [bit] NOT NULL,
	[AppliedAt] [datetime2](0) NULL,
	[RetryCount] [int] NOT NULL,
	[ErrorMessage] [nvarchar](max) NULL,
 CONSTRAINT [PK_SyncPendingChanges] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserPermissions]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserPermissions](
	[Uid] [nvarchar](128) NOT NULL,
	[Permission] [nvarchar](60) NOT NULL,
 CONSTRAINT [PK_UserPermissions] PRIMARY KEY CLUSTERED 
(
	[Uid] ASC,
	[Permission] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 16/11/2026 01:14:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Uid] [nvarchar](128) NOT NULL,
	[Email] [nvarchar](256) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Phone] [nvarchar](32) NOT NULL,
	[Whatsapp] [nvarchar](32) NOT NULL,
	[Pin] [nvarchar](256) NOT NULL,
	[PhotoUrl] [nvarchar](1000) NOT NULL,
	[Role] [nvarchar](20) NOT NULL,
	[IsDisabled] [bit] NOT NULL,
	[DisabledReason] [nvarchar](500) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[LastLoginAt] [datetime2](0) NULL,
	[CreatedAt] [datetime2](0) NULL,
	[CreatedBy] [nvarchar](128) NULL,
	[UpdatedAt] [datetime2](0) NULL,
	[UpdatedBy] [nvarchar](128) NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[Uid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Apartments_Floor]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Apartments_Floor] ON [dbo].[Apartments]
(
	[BuildingId] ASC,
	[FloorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Apartments_Phone]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Apartments_Phone] ON [dbo].[Apartments]
(
	[Phone] ASC
)
WHERE ([Phone]<>N'')
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Audit_Action]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Audit_Action] ON [dbo].[AuditLog]
(
	[Action] ASC,
	[Ts] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Audit_Apt]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Audit_Apt] ON [dbo].[AuditLog]
(
	[BuildingId] ASC,
	[AptNumber] ASC,
	[Ts] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Audit_Building_Ts]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Audit_Building_Ts] ON [dbo].[AuditLog]
(
	[BuildingId] ASC,
	[Ts] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_AuditLog_ApartmentId_Ts]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_AuditLog_ApartmentId_Ts] ON [dbo].[AuditLog]
(
	[ApartmentId] ASC,
	[Ts] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_BH_CreatedAt]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_BH_CreatedAt] ON [dbo].[BackupHistory]
(
	[CreatedAt] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_BuildingAdmins_AdminUid]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_BuildingAdmins_AdminUid] ON [dbo].[BuildingAdmins]
(
	[AdminUid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Cameras_BuildingId]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Cameras_BuildingId] ON [dbo].[Cameras]
(
	[BuildingId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Cameras_DvrId_Channel]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Cameras_DvrId_Channel] ON [dbo].[Cameras]
(
	[DvrId] ASC,
	[Channel] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_DEH_Deposit]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_DEH_Deposit] ON [dbo].[DepositEditHistory]
(
	[BuildingId] ASC,
	[DepositId] ASC,
	[Ts] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Dep_Apartment]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Dep_Apartment] ON [dbo].[Deposits]
(
	[BuildingId] ASC,
	[ApartmentId] ASC,
	[Status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Dep_Month]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Dep_Month] ON [dbo].[Deposits]
(
	[BuildingId] ASC,
	[MonthKey] ASC,
	[Status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Dvrs_BuildingId]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Dvrs_BuildingId] ON [dbo].[Dvrs]
(
	[BuildingId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Exp_Month]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Exp_Month] ON [dbo].[Expenses]
(
	[BuildingId] ASC,
	[MonthKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Cat_Kind]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Cat_Kind] ON [dbo].[FinancialCategories]
(
	[BuildingId] ASC,
	[Kind] ASC,
	[SortOrder] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Mnt_Status]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Mnt_Status] ON [dbo].[MaintenanceRecords]
(
	[BuildingId] ASC,
	[Status] ASC,
	[MaintenanceDate] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Notif_Recipient]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Notif_Recipient] ON [dbo].[Notifications]
(
	[BuildingId] ASC,
	[RecipientType] ASC,
	[RecipientApartmentId] ASC,
	[IsRead] ASC,
	[Ts] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Presence_LastSeen]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Presence_LastSeen] ON [dbo].[Presence]
(
	[BuildingId] ASC,
	[LastSeen] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Qr_Apartment]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Qr_Apartment] ON [dbo].[QrTokens]
(
	[BuildingId] ASC,
	[ApartmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Qr_Expires]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Qr_Expires] ON [dbo].[QrTokens]
(
	[ExpiresAt] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_QrUsages_CreatedAt]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_QrUsages_CreatedAt] ON [dbo].[QrUsages]
(
	[CreatedAt] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_QrUsages_TokenId]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_QrUsages_TokenId] ON [dbo].[QrUsages]
(
	[TokenId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Residents_BuildingId_Phone]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Residents_BuildingId_Phone] ON [dbo].[Residents]
(
	[BuildingId] ASC,
	[Phone] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Residents_BuildingId_ApartmentId] ******/
CREATE NONCLUSTERED INDEX [IX_Residents_BuildingId_ApartmentId] ON [dbo].[Residents]
(
	[BuildingId] ASC,
	[ApartmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Residents_Phone]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Residents_Phone] ON [dbo].[Residents]
(
	[Phone] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Residents_Uid]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Residents_Uid] ON [dbo].[Residents]
(
	[Uid] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Rev_Month]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Rev_Month] ON [dbo].[Revenues]
(
	[BuildingId] ASC,
	[MonthKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_SPC_Applied]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_SPC_Applied] ON [dbo].[SyncPendingChanges]
(
	[Applied] ASC,
	[CreatedAt] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_SPC_Entity]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_SPC_Entity] ON [dbo].[SyncPendingChanges]
(
	[EntityType] ASC,
	[EntityId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Users_Email]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users]
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_Users_Role]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_Users_Role] ON [dbo].[Users]
(
	[Role] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_WA_Apartment]    Script Date: 16/11/2026 01:14:39 PM ******/
CREATE NONCLUSTERED INDEX [IX_WA_Apartment] ON [dbo].[WalletAdjustments]
(
	[BuildingId] ASC,
	[ApartmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Apartments] ADD  CONSTRAINT [DF_Apt_Owner]  DEFAULT (N'') FOR [Owner]
GO
ALTER TABLE [dbo].[Apartments] ADD  CONSTRAINT [DF_Apt_Phone]  DEFAULT (N'') FOR [Phone]
GO
ALTER TABLE [dbo].[Apartments] ADD  CONSTRAINT [DF_Apt_Email]  DEFAULT (N'') FOR [Email]
GO
ALTER TABLE [dbo].[Apartments] ADD  CONSTRAINT [DF_Apt_Fee]  DEFAULT ((0)) FOR [MonthlyFee]
GO
ALTER TABLE [dbo].[Apartments] ADD  CONSTRAINT [DF_Apt_Pin]  DEFAULT (N'') FOR [Pin]
GO
ALTER TABLE [dbo].[Apartments] ADD  CONSTRAINT [DF_Apt_IsClosed]  DEFAULT ((0)) FOR [IsClosed]
GO
ALTER TABLE [dbo].[Apartments] ADD  CONSTRAINT [DF_Apt_Label]  DEFAULT (N'') FOR [Label]
GO
ALTER TABLE [dbo].[Apartments] ADD  CONSTRAINT [DF_Apt_Notes]  DEFAULT (N'') FOR [Notes]
GO
ALTER TABLE [dbo].[Apartments] ADD  CONSTRAINT [DF_Apt_IsDisabled]  DEFAULT ((0)) FOR [IsDisabled]
GO
ALTER TABLE [dbo].[Apartments] ADD  CONSTRAINT [DF_Apt_DisabledReason]  DEFAULT (N'') FOR [DisabledReason]
GO
ALTER TABLE [dbo].[Apartments] ADD  CONSTRAINT [DF_Apt_Lang]  DEFAULT (N'ar') FOR [Language]
GO
ALTER TABLE [dbo].[Apartments] ADD  CONSTRAINT [DF_Apt_NWa]  DEFAULT ((1)) FOR [NotifyWhatsApp]
GO
ALTER TABLE [dbo].[Apartments] ADD  CONSTRAINT [DF_Apt_NEm]  DEFAULT ((0)) FOR [NotifyEmail]
GO
ALTER TABLE [dbo].[Apartments] ADD  CONSTRAINT [DF_Apt_NIn]  DEFAULT ((1)) FOR [NotifyInApp]
GO
ALTER TABLE [dbo].[Apartments] ADD  CONSTRAINT [DF_Apt_PayMethod]  DEFAULT (N'instapay') FOR [PreferredPaymentMethod]
GO
ALTER TABLE [dbo].[AuditLog] ADD  CONSTRAINT [DF_Audit_Label]  DEFAULT (N'') FOR [Label]
GO
ALTER TABLE [dbo].[AuditLog] ADD  CONSTRAINT [DF_Audit_Actor]  DEFAULT (N'') FOR [Actor]
GO
ALTER TABLE [dbo].[AuditLog] ADD  CONSTRAINT [DF_Audit_ActorRole]  DEFAULT (N'') FOR [ActorRole]
GO
ALTER TABLE [dbo].[AuditLog] ADD  CONSTRAINT [DF_Audit_Details]  DEFAULT (N'') FOR [Details]
GO
ALTER TABLE [dbo].[AuditLog] ADD  CONSTRAINT [DF_Audit_Month]  DEFAULT (N'') FOR [MonthKey]
GO
ALTER TABLE [dbo].[AuditLog] ADD  DEFAULT (N'info') FOR [Severity]
GO
ALTER TABLE [dbo].[BackupHistory] ADD  CONSTRAINT [DF_BH_Size]  DEFAULT ((0)) FOR [FileSize]
GO
ALTER TABLE [dbo].[BackupHistory] ADD  CONSTRAINT [DF_BH_Cols]  DEFAULT (N'[]') FOR [CollectionsJson]
GO
ALTER TABLE [dbo].[BackupHistory] ADD  CONSTRAINT [DF_BH_Docs]  DEFAULT ((0)) FOR [TotalDocuments]
GO
ALTER TABLE [dbo].[BackupHistory] ADD  CONSTRAINT [DF_BH_CreatedAt]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[BackupHistory] ADD  CONSTRAINT [DF_BH_Dur]  DEFAULT ((0)) FOR [DurationSeconds]
GO
ALTER TABLE [dbo].[BackupHistory] ADD  CONSTRAINT [DF_BH_Status]  DEFAULT (N'success') FOR [Status]
GO
ALTER TABLE [dbo].[BackupHistory] ADD  CONSTRAINT [DF_BH_Source]  DEFAULT (N'scheduled') FOR [Source]
GO
ALTER TABLE [dbo].[Buildings] ADD  CONSTRAINT [DF_Buildings_AdminPin]  DEFAULT (N'') FOR [AdminPin]
GO
ALTER TABLE [dbo].[Buildings] ADD  CONSTRAINT [DF_Buildings_DataVersion]  DEFAULT (N'3.2') FOR [DataVersion]
GO
ALTER TABLE [dbo].[Buildings] ADD  CONSTRAINT [DF_Buildings_LogoUrl]  DEFAULT (N'') FOR [LogoUrl]
GO
ALTER TABLE [dbo].[Buildings] ADD  CONSTRAINT [DF_Buildings_AdminWhatsapp]  DEFAULT (N'') FOR [AdminWhatsapp]
GO
ALTER TABLE [dbo].[Buildings] ADD  CONSTRAINT [DF_Buildings_PayLabel]  DEFAULT (N'') FOR [PaymentLabel]
GO
ALTER TABLE [dbo].[Buildings] ADD  CONSTRAINT [DF_Buildings_PayAcc]  DEFAULT (N'') FOR [PaymentAccountNumber]
GO
ALTER TABLE [dbo].[Buildings] ADD  CONSTRAINT [DF_Buildings_PayPhone]  DEFAULT (N'') FOR [PaymentPhone]
GO
ALTER TABLE [dbo].[Buildings] ADD  CONSTRAINT [DF_Buildings_PayNotes]  DEFAULT (N'') FOR [PaymentNotes]
GO
ALTER TABLE [dbo].[Buildings] ADD  CONSTRAINT [DF_Buildings_CreatedAt]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[BuildingSettings] ADD  CONSTRAINT [DF_BS_DisplayName]  DEFAULT (N'') FOR [DisplayName]
GO
ALTER TABLE [dbo].[BuildingSettings] ADD  CONSTRAINT [DF_BS_Address]  DEFAULT (N'') FOR [Address]
GO
ALTER TABLE [dbo].[BuildingSettings] ADD  CONSTRAINT [DF_BS_Whatsapp]  DEFAULT (N'') FOR [WhatsappNumber]
GO
ALTER TABLE [dbo].[BuildingSettings] ADD  CONSTRAINT [DF_BS_InvoiceDay]  DEFAULT ((1)) FOR [InvoiceDayOfMonth]
GO
ALTER TABLE [dbo].[BuildingSettings] ADD  CONSTRAINT [DF_BS_ExpDist]  DEFAULT (N'equal') FOR [ExpenseDistribution]
GO
ALTER TABLE [dbo].[BuildingSettings] ADD  CONSTRAINT [DF_BS_Quorum]  DEFAULT ((50)) FOR [VotingQuorumPercent]
GO
ALTER TABLE [dbo].[BuildingSettings] ADD  CONSTRAINT [DF_BS_Currency]  DEFAULT (N'EGP') FOR [Currency]
GO
ALTER TABLE [dbo].[Cameras] ADD  DEFAULT ('') FOR [RtspPath]
GO
ALTER TABLE [dbo].[Cameras] ADD  DEFAULT ('') FOR [HlsUrl]
GO
ALTER TABLE [dbo].[Cameras] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Cameras] ADD  DEFAULT (getutcdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[DepositEditHistory] ADD  CONSTRAINT [DF_DEH_Reason]  DEFAULT (N'') FOR [Reason]
GO
ALTER TABLE [dbo].[DepositEditHistory] ADD  CONSTRAINT [DF_DEH_OldNote]  DEFAULT (N'') FOR [OldNote]
GO
ALTER TABLE [dbo].[DepositEditHistory] ADD  CONSTRAINT [DF_DEH_NewNote]  DEFAULT (N'') FOR [NewNote]
GO
ALTER TABLE [dbo].[Deposits] ADD  CONSTRAINT [DF_Dep_Number]  DEFAULT (N'') FOR [Number]
GO
ALTER TABLE [dbo].[Deposits] ADD  CONSTRAINT [DF_Dep_Note]  DEFAULT (N'') FOR [Note]
GO
ALTER TABLE [dbo].[Deposits] ADD  CONSTRAINT [DF_Dep_Status]  DEFAULT (N'pending') FOR [Status]
GO
ALTER TABLE [dbo].[Deposits] ADD  CONSTRAINT [DF_Dep_CreatedAt]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Deposits] ADD  CONSTRAINT [DF_Dep_CreatedBy]  DEFAULT (N'') FOR [CreatedBy]
GO
ALTER TABLE [dbo].[Dvrs] ADD  DEFAULT ((554)) FOR [Port]
GO
ALTER TABLE [dbo].[Dvrs] ADD  DEFAULT ('') FOR [Brand]
GO
ALTER TABLE [dbo].[Dvrs] ADD  DEFAULT ('') FOR [Username]
GO
ALTER TABLE [dbo].[Dvrs] ADD  DEFAULT ('') FOR [PasswordEncrypted]
GO
ALTER TABLE [dbo].[Dvrs] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Dvrs] ADD  DEFAULT (getutcdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Expenses] ADD  CONSTRAINT [DF_Exp_CatKind]  DEFAULT (N'expense') FOR [CategoryKind]
GO
ALTER TABLE [dbo].[Expenses] ADD  CONSTRAINT [DF_Exp_Note]  DEFAULT (N'') FOR [Note]
GO
ALTER TABLE [dbo].[FinancialCategories] ADD  CONSTRAINT [DF_Cat_Color]  DEFAULT (N'') FOR [Color]
GO
ALTER TABLE [dbo].[FinancialCategories] ADD  CONSTRAINT [DF_Cat_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[FinancialCategories] ADD  CONSTRAINT [DF_Cat_SortOrder]  DEFAULT ((0)) FOR [SortOrder]
GO
ALTER TABLE [dbo].[Floors] ADD  CONSTRAINT [DF_Floors_SortOrder]  DEFAULT ((0)) FOR [SortOrder]
GO
ALTER TABLE [dbo].[GlobalSettings] ADD  CONSTRAINT [DF_GS_Id]  DEFAULT (N'global') FOR [Id]
GO
ALTER TABLE [dbo].[GlobalSettings] ADD  CONSTRAINT [DF_GS_AppName]  DEFAULT (N'نظام إدارة العمارات') FOR [AppName]
GO
ALTER TABLE [dbo].[GlobalSettings] ADD  CONSTRAINT [DF_GS_Lang]  DEFAULT (N'ar') FOR [DefaultLanguage]
GO
ALTER TABLE [dbo].[GlobalSettings] ADD  CONSTRAINT [DF_GS_WaToken]  DEFAULT (N'') FOR [WhatsappApiToken]
GO
ALTER TABLE [dbo].[GlobalSettings] ADD  CONSTRAINT [DF_GS_WaPhone]  DEFAULT (N'') FOR [WhatsappPhoneId]
GO
ALTER TABLE [dbo].[GlobalSettings] ADD  CONSTRAINT [DF_GS_CloudName]  DEFAULT (N'') FOR [CloudinaryCloudName]
GO
ALTER TABLE [dbo].[GlobalSettings] ADD  CONSTRAINT [DF_GS_CloudKey]  DEFAULT (N'') FOR [CloudinaryApiKey]
GO
ALTER TABLE [dbo].[GlobalSettings] ADD  CONSTRAINT [DF_GS_StorageMode]  DEFAULT (N'Dual') FOR [StorageMode]
GO
ALTER TABLE [dbo].[GlobalSettings] ADD  CONSTRAINT [DF_GS_Polls]  DEFAULT ((1)) FOR [EnablePolls]
GO
ALTER TABLE [dbo].[GlobalSettings] ADD  CONSTRAINT [DF_GS_Maint]  DEFAULT ((1)) FOR [EnableMaintenance]
GO
ALTER TABLE [dbo].[GlobalSettings] ADD  CONSTRAINT [DF_GS_Ocr]  DEFAULT ((1)) FOR [EnableOcr]
GO
ALTER TABLE [dbo].[GlobalSettings] ADD  CONSTRAINT [DF_GS_Qr]  DEFAULT ((1)) FOR [EnableQrAccess]
GO
ALTER TABLE [dbo].[GlobalSettings] ADD  CONSTRAINT [DF_GS_Wa]  DEFAULT ((1)) FOR [EnableWhatsApp]
GO
ALTER TABLE [dbo].[GlobalSettings] ADD  CONSTRAINT [DF_GS_Timeout]  DEFAULT ((7)) FOR [SessionTimeoutDays]
GO
ALTER TABLE [dbo].[GlobalSettings] ADD  CONSTRAINT [DF_GS_MaxPin]  DEFAULT ((5)) FOR [MaxPinAttempts]
GO
ALTER TABLE [dbo].[MaintenanceRecords] ADD  CONSTRAINT [DF_Mnt_Desc]  DEFAULT (N'') FOR [Description]
GO
ALTER TABLE [dbo].[MaintenanceRecords] ADD  CONSTRAINT [DF_Mnt_Cost]  DEFAULT ((0)) FOR [Cost]
GO
ALTER TABLE [dbo].[MaintenanceRecords] ADD  CONSTRAINT [DF_Mnt_Vendor]  DEFAULT (N'') FOR [Vendor]
GO
ALTER TABLE [dbo].[MaintenanceRecords] ADD  CONSTRAINT [DF_Mnt_Status]  DEFAULT (N'open') FOR [Status]
GO
ALTER TABLE [dbo].[MaintenanceRecords] ADD  CONSTRAINT [DF_Mnt_CreatedBy]  DEFAULT (N'') FOR [CreatedBy]
GO
ALTER TABLE [dbo].[MaintenanceRecords] ADD  CONSTRAINT [DF_Mnt_CreatedAt]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[MaintenanceRecords] ADD  CONSTRAINT [DF_Mnt_AddedToExp]  DEFAULT ((0)) FOR [AddedToExpense]
GO
ALTER TABLE [dbo].[MonthlyApartmentShares] ADD  CONSTRAINT [DF_MAS_Exp]  DEFAULT ((0)) FOR [ExpenseShare]
GO
ALTER TABLE [dbo].[MonthlyApartmentShares] ADD  CONSTRAINT [DF_MAS_Rev]  DEFAULT ((0)) FOR [RevenueShare]
GO
ALTER TABLE [dbo].[MonthlyApartmentShares] ADD  CONSTRAINT [DF_MAS_Carry]  DEFAULT ((0)) FOR [CarryOver]
GO
ALTER TABLE [dbo].[Notifications] ADD  CONSTRAINT [DF_Notif_Type]  DEFAULT (N'info') FOR [Type]
GO
ALTER TABLE [dbo].[Notifications] ADD  CONSTRAINT [DF_Notif_Icon]  DEFAULT (N'🔔') FOR [Icon]
GO
ALTER TABLE [dbo].[Notifications] ADD  CONSTRAINT [DF_Notif_Title]  DEFAULT (N'') FOR [Title]
GO
ALTER TABLE [dbo].[Notifications] ADD  CONSTRAINT [DF_Notif_Body]  DEFAULT (N'') FOR [Body]
GO
ALTER TABLE [dbo].[Notifications] ADD  CONSTRAINT [DF_Notif_IsRead]  DEFAULT ((0)) FOR [IsRead]
GO
ALTER TABLE [dbo].[PollOptions] ADD  CONSTRAINT [DF_PollOptions_Sort]  DEFAULT ((0)) FOR [SortOrder]
GO
ALTER TABLE [dbo].[Polls] ADD  CONSTRAINT [DF_Poll_Desc]  DEFAULT (N'') FOR [Description]
GO
ALTER TABLE [dbo].[Polls] ADD  CONSTRAINT [DF_Poll_CreatedBy]  DEFAULT (N'') FOR [CreatedBy]
GO
ALTER TABLE [dbo].[Polls] ADD  CONSTRAINT [DF_Poll_CreatedAt]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Polls] ADD  CONSTRAINT [DF_Poll_IsClosed]  DEFAULT ((0)) FOR [IsClosed]
GO
ALTER TABLE [dbo].[PollVotes] ADD  CONSTRAINT [DF_PollVotes_VotedAt]  DEFAULT (sysutcdatetime()) FOR [VotedAt]
GO
ALTER TABLE [dbo].[QrTokens] ADD  CONSTRAINT [DF_Qr_UseType]  DEFAULT (N'single') FOR [UseType]
GO
ALTER TABLE [dbo].[QrTokens] ADD  CONSTRAINT [DF_Qr_CreatedBy]  DEFAULT (N'') FOR [CreatedBy]
GO
ALTER TABLE [dbo].[QrTokens] ADD  CONSTRAINT [DF_Qr_IsUsed]  DEFAULT ((0)) FOR [IsUsed]
GO
ALTER TABLE [dbo].[QrTokens] ADD  CONSTRAINT [DF_Qr_CreatedAt]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Residents] ADD  DEFAULT ('') FOR [Name]
GO
ALTER TABLE [dbo].[Residents] ADD  DEFAULT ('') FOR [Phone]
GO
ALTER TABLE [dbo].[Residents] ADD  DEFAULT ('') FOR [Whatsapp]
GO
ALTER TABLE [dbo].[Residents] ADD  DEFAULT ('') FOR [Email]
GO
ALTER TABLE [dbo].[Residents] ADD  DEFAULT ('') FOR [PhotoUrl]
GO
ALTER TABLE [dbo].[Residents] ADD  DEFAULT ('') FOR [PinHash]
GO
ALTER TABLE [dbo].[Residents] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Residents] ADD  DEFAULT ((0)) FOR [IsDisabled]
GO
ALTER TABLE [dbo].[Residents] ADD  DEFAULT ('') FOR [DisabledReason]
GO
ALTER TABLE [dbo].[Residents] ADD  DEFAULT ((1)) FOR [IsOwner]
GO
ALTER TABLE [dbo].[Residents] ADD  DEFAULT ((1)) FOR [IsPrimary]
GO
ALTER TABLE [dbo].[Residents] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Revenues] ADD  CONSTRAINT [DF_Rev_CatKind]  DEFAULT (N'revenue') FOR [CategoryKind]
GO
ALTER TABLE [dbo].[Revenues] ADD  CONSTRAINT [DF_Rev_Note]  DEFAULT (N'') FOR [Note]
GO
ALTER TABLE [dbo].[ScheduledBackupSettings] ADD  CONSTRAINT [DF_SBS_Id]  DEFAULT (N'default') FOR [Id]
GO
ALTER TABLE [dbo].[ScheduledBackupSettings] ADD  CONSTRAINT [DF_SBS_Enabled]  DEFAULT ((0)) FOR [Enabled]
GO
ALTER TABLE [dbo].[ScheduledBackupSettings] ADD  CONSTRAINT [DF_SBS_Freq]  DEFAULT (N'daily') FOR [Frequency]
GO
ALTER TABLE [dbo].[ScheduledBackupSettings] ADD  CONSTRAINT [DF_SBS_Hour]  DEFAULT ((2)) FOR [Hour]
GO
ALTER TABLE [dbo].[ScheduledBackupSettings] ADD  CONSTRAINT [DF_SBS_Cols]  DEFAULT (N'[]') FOR [CollectionsJson]
GO
ALTER TABLE [dbo].[ScheduledBackupSettings] ADD  CONSTRAINT [DF_SBS_Max]  DEFAULT ((7)) FOR [MaxBackupsToKeep]
GO
ALTER TABLE [dbo].[SyncPendingChanges] ADD  CONSTRAINT [DF_SPC_CreatedAt]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[SyncPendingChanges] ADD  CONSTRAINT [DF_SPC_Applied]  DEFAULT ((0)) FOR [Applied]
GO
ALTER TABLE [dbo].[SyncPendingChanges] ADD  CONSTRAINT [DF_SPC_Retry]  DEFAULT ((0)) FOR [RetryCount]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_Email]  DEFAULT (N'') FOR [Email]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_Name]  DEFAULT (N'') FOR [Name]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_Phone]  DEFAULT (N'') FOR [Phone]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_Whatsapp]  DEFAULT (N'') FOR [Whatsapp]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_Pin]  DEFAULT (N'') FOR [Pin]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_PhotoUrl]  DEFAULT (N'') FOR [PhotoUrl]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_IsDisabled]  DEFAULT ((0)) FOR [IsDisabled]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_DisabledReason]  DEFAULT (N'') FOR [DisabledReason]
GO
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[WalletAdjustments] ADD  CONSTRAINT [DF_WA_Reason]  DEFAULT (N'') FOR [Reason]
GO
ALTER TABLE [dbo].[WalletAdjustments] ADD  CONSTRAINT [DF_WA_CreatedAt]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[WalletAdjustments] ADD  CONSTRAINT [DF_WA_CreatedBy]  DEFAULT (N'') FOR [CreatedBy]
GO
ALTER TABLE [dbo].[Apartments]  WITH CHECK ADD  CONSTRAINT [FK_Apartments_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Apartments] CHECK CONSTRAINT [FK_Apartments_Buildings]
GO
ALTER TABLE [dbo].[Apartments]  WITH CHECK ADD  CONSTRAINT [FK_Apartments_Floors] FOREIGN KEY([BuildingId], [FloorId])
REFERENCES [dbo].[Floors] ([BuildingId], [Id])
GO
ALTER TABLE [dbo].[Apartments] CHECK CONSTRAINT [FK_Apartments_Floors]
GO
ALTER TABLE [dbo].[AuditLog]  WITH CHECK ADD  CONSTRAINT [FK_Audit_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AuditLog] CHECK CONSTRAINT [FK_Audit_Buildings]
GO
ALTER TABLE [dbo].[BuildingAdmins]  WITH CHECK ADD  CONSTRAINT [FK_BuildingAdmins_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[BuildingAdmins] CHECK CONSTRAINT [FK_BuildingAdmins_Buildings]
GO
ALTER TABLE [dbo].[BuildingAdmins]  WITH CHECK ADD  CONSTRAINT [FK_BuildingAdmins_Users] FOREIGN KEY([AdminUid])
REFERENCES [dbo].[Users] ([Uid])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[BuildingAdmins] CHECK CONSTRAINT [FK_BuildingAdmins_Users]
GO
ALTER TABLE [dbo].[BuildingSettings]  WITH CHECK ADD  CONSTRAINT [FK_BuildingSettings_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[BuildingSettings] CHECK CONSTRAINT [FK_BuildingSettings_Buildings]
GO
ALTER TABLE [dbo].[Cameras]  WITH CHECK ADD  CONSTRAINT [FK_Cameras_Dvrs] FOREIGN KEY([DvrId])
REFERENCES [dbo].[Dvrs] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Cameras] CHECK CONSTRAINT [FK_Cameras_Dvrs]
GO
ALTER TABLE [dbo].[DepositEditHistory]  WITH CHECK ADD  CONSTRAINT [FK_DEH_Deposits] FOREIGN KEY([BuildingId], [DepositId])
REFERENCES [dbo].[Deposits] ([BuildingId], [Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[DepositEditHistory] CHECK CONSTRAINT [FK_DEH_Deposits]
GO
ALTER TABLE [dbo].[Deposits]  WITH CHECK ADD  CONSTRAINT [FK_Dep_Apartments] FOREIGN KEY([BuildingId], [ApartmentId])
REFERENCES [dbo].[Apartments] ([BuildingId], [Id])
GO
ALTER TABLE [dbo].[Deposits] CHECK CONSTRAINT [FK_Dep_Apartments]
GO
ALTER TABLE [dbo].[Deposits]  WITH CHECK ADD  CONSTRAINT [FK_Dep_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Deposits] CHECK CONSTRAINT [FK_Dep_Buildings]
GO
ALTER TABLE [dbo].[Dvrs]  WITH CHECK ADD  CONSTRAINT [FK_Dvrs_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Dvrs] CHECK CONSTRAINT [FK_Dvrs_Buildings]
GO
ALTER TABLE [dbo].[Expenses]  WITH CHECK ADD  CONSTRAINT [FK_Exp_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Expenses] CHECK CONSTRAINT [FK_Exp_Buildings]
GO
ALTER TABLE [dbo].[Expenses]  WITH CHECK ADD  CONSTRAINT [FK_Exp_Category] FOREIGN KEY([BuildingId], [CategoryKind], [CategoryId])
REFERENCES [dbo].[FinancialCategories] ([BuildingId], [Kind], [Id])
GO
ALTER TABLE [dbo].[Expenses] CHECK CONSTRAINT [FK_Exp_Category]
GO
ALTER TABLE [dbo].[FinancialCategories]  WITH CHECK ADD  CONSTRAINT [FK_Cat_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FinancialCategories] CHECK CONSTRAINT [FK_Cat_Buildings]
GO
ALTER TABLE [dbo].[Floors]  WITH CHECK ADD  CONSTRAINT [FK_Floors_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Floors] CHECK CONSTRAINT [FK_Floors_Buildings]
GO
ALTER TABLE [dbo].[MaintenanceRecords]  WITH CHECK ADD  CONSTRAINT [FK_Mnt_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MaintenanceRecords] CHECK CONSTRAINT [FK_Mnt_Buildings]
GO
ALTER TABLE [dbo].[MonthlyApartmentShares]  WITH CHECK ADD  CONSTRAINT [FK_MAS_Apartments] FOREIGN KEY([BuildingId], [ApartmentId])
REFERENCES [dbo].[Apartments] ([BuildingId], [Id])
GO
ALTER TABLE [dbo].[MonthlyApartmentShares] CHECK CONSTRAINT [FK_MAS_Apartments]
GO
ALTER TABLE [dbo].[MonthlyApartmentShares]  WITH CHECK ADD  CONSTRAINT [FK_MAS_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MonthlyApartmentShares] CHECK CONSTRAINT [FK_MAS_Buildings]
GO
ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notif_Apartments] FOREIGN KEY([BuildingId], [RecipientApartmentId])
REFERENCES [dbo].[Apartments] ([BuildingId], [Id])
GO
ALTER TABLE [dbo].[Notifications] CHECK CONSTRAINT [FK_Notif_Apartments]
GO
ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notif_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Notifications] CHECK CONSTRAINT [FK_Notif_Buildings]
GO
ALTER TABLE [dbo].[PollOptions]  WITH CHECK ADD  CONSTRAINT [FK_PollOptions_Polls] FOREIGN KEY([BuildingId], [PollId])
REFERENCES [dbo].[Polls] ([BuildingId], [Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PollOptions] CHECK CONSTRAINT [FK_PollOptions_Polls]
GO
ALTER TABLE [dbo].[Polls]  WITH CHECK ADD  CONSTRAINT [FK_Polls_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Polls] CHECK CONSTRAINT [FK_Polls_Buildings]
GO
ALTER TABLE [dbo].[PollVotes]  WITH CHECK ADD  CONSTRAINT [FK_PollVotes_Apartment] FOREIGN KEY([BuildingId], [ApartmentId])
REFERENCES [dbo].[Apartments] ([BuildingId], [Id])
GO
ALTER TABLE [dbo].[PollVotes] CHECK CONSTRAINT [FK_PollVotes_Apartment]
GO
ALTER TABLE [dbo].[PollVotes]  WITH CHECK ADD  CONSTRAINT [FK_PollVotes_Option] FOREIGN KEY([BuildingId], [PollId], [OptionId])
REFERENCES [dbo].[PollOptions] ([BuildingId], [PollId], [Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PollVotes] CHECK CONSTRAINT [FK_PollVotes_Option]
GO
ALTER TABLE [dbo].[QrTokens]  WITH CHECK ADD  CONSTRAINT [FK_Qr_Apartments] FOREIGN KEY([BuildingId], [ApartmentId])
REFERENCES [dbo].[Apartments] ([BuildingId], [Id])
GO
ALTER TABLE [dbo].[QrTokens] CHECK CONSTRAINT [FK_Qr_Apartments]
GO
ALTER TABLE [dbo].[QrTokens]  WITH CHECK ADD  CONSTRAINT [FK_Qr_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[QrTokens] CHECK CONSTRAINT [FK_Qr_Buildings]
GO
ALTER TABLE [dbo].[Residents]  WITH CHECK ADD  CONSTRAINT [FK_Residents_Apartments] FOREIGN KEY([BuildingId], [ApartmentId])
REFERENCES [dbo].[Apartments] ([BuildingId], [Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Residents] CHECK CONSTRAINT [FK_Residents_Apartments]
GO
ALTER TABLE [dbo].[Revenues]  WITH CHECK ADD  CONSTRAINT [FK_Rev_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Revenues] CHECK CONSTRAINT [FK_Rev_Buildings]
GO
ALTER TABLE [dbo].[Revenues]  WITH CHECK ADD  CONSTRAINT [FK_Rev_Category] FOREIGN KEY([BuildingId], [CategoryKind], [CategoryId])
REFERENCES [dbo].[FinancialCategories] ([BuildingId], [Kind], [Id])
GO
ALTER TABLE [dbo].[Revenues] CHECK CONSTRAINT [FK_Rev_Category]
GO
ALTER TABLE [dbo].[UserPermissions]  WITH CHECK ADD  CONSTRAINT [FK_UserPermissions_Users] FOREIGN KEY([Uid])
REFERENCES [dbo].[Users] ([Uid])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[UserPermissions] CHECK CONSTRAINT [FK_UserPermissions_Users]
GO
ALTER TABLE [dbo].[WalletAdjustments]  WITH CHECK ADD  CONSTRAINT [FK_WA_Apartments] FOREIGN KEY([BuildingId], [ApartmentId])
REFERENCES [dbo].[Apartments] ([BuildingId], [Id])
GO
ALTER TABLE [dbo].[WalletAdjustments] CHECK CONSTRAINT [FK_WA_Apartments]
GO
ALTER TABLE [dbo].[WalletAdjustments]  WITH CHECK ADD  CONSTRAINT [FK_WA_Buildings] FOREIGN KEY([BuildingId])
REFERENCES [dbo].[Buildings] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[WalletAdjustments] CHECK CONSTRAINT [FK_WA_Buildings]
GO
ALTER TABLE [dbo].[Apartments]  WITH CHECK ADD  CONSTRAINT [CK_Apartments_Fee] CHECK  (([MonthlyFee]>=(0)))
GO
ALTER TABLE [dbo].[Apartments] CHECK CONSTRAINT [CK_Apartments_Fee]
GO
ALTER TABLE [dbo].[Apartments]  WITH CHECK ADD  CONSTRAINT [CK_Apartments_Number] CHECK  (([Number]>=(0)))
GO
ALTER TABLE [dbo].[Apartments] CHECK CONSTRAINT [CK_Apartments_Number]
GO
ALTER TABLE [dbo].[BackupHistory]  WITH CHECK ADD  CONSTRAINT [CK_BH_Json] CHECK  ((isjson([CollectionsJson])=(1)))
GO
ALTER TABLE [dbo].[BackupHistory] CHECK CONSTRAINT [CK_BH_Json]
GO
ALTER TABLE [dbo].[BuildingSettings]  WITH CHECK ADD  CONSTRAINT [CK_BS_InvoiceDay] CHECK  (([InvoiceDayOfMonth]>=(1) AND [InvoiceDayOfMonth]<=(31)))
GO
ALTER TABLE [dbo].[BuildingSettings] CHECK CONSTRAINT [CK_BS_InvoiceDay]
GO
ALTER TABLE [dbo].[BuildingSettings]  WITH CHECK ADD  CONSTRAINT [CK_BS_Quorum] CHECK  (([VotingQuorumPercent]>=(0) AND [VotingQuorumPercent]<=(100)))
GO
ALTER TABLE [dbo].[BuildingSettings] CHECK CONSTRAINT [CK_BS_Quorum]
GO
ALTER TABLE [dbo].[Deposits]  WITH CHECK ADD  CONSTRAINT [CK_Dep_Amount] CHECK  (([Amount]>(0)))
GO
ALTER TABLE [dbo].[Deposits] CHECK CONSTRAINT [CK_Dep_Amount]
GO
ALTER TABLE [dbo].[Deposits]  WITH CHECK ADD  CONSTRAINT [CK_Dep_MonthKey] CHECK  (([MonthKey] like '[0-9][0-9][0-9][0-9]-[0-9][0-9]'))
GO
ALTER TABLE [dbo].[Deposits] CHECK CONSTRAINT [CK_Dep_MonthKey]
GO
ALTER TABLE [dbo].[Deposits]  WITH CHECK ADD  CONSTRAINT [CK_Dep_Status] CHECK  (([Status]=N'cancelled' OR [Status]=N'confirmed' OR [Status]=N'pending'))
GO
ALTER TABLE [dbo].[Deposits] CHECK CONSTRAINT [CK_Dep_Status]
GO
ALTER TABLE [dbo].[Expenses]  WITH CHECK ADD  CONSTRAINT [CK_Exp_Amount] CHECK  (([Amount]>=(0)))
GO
ALTER TABLE [dbo].[Expenses] CHECK CONSTRAINT [CK_Exp_Amount]
GO
ALTER TABLE [dbo].[Expenses]  WITH CHECK ADD  CONSTRAINT [CK_Exp_CatKind] CHECK  (([CategoryKind]=N'expense'))
GO
ALTER TABLE [dbo].[Expenses] CHECK CONSTRAINT [CK_Exp_CatKind]
GO
ALTER TABLE [dbo].[Expenses]  WITH CHECK ADD  CONSTRAINT [CK_Exp_MonthKey] CHECK  (([MonthKey] like '[0-9][0-9][0-9][0-9]-[0-9][0-9]'))
GO
ALTER TABLE [dbo].[Expenses] CHECK CONSTRAINT [CK_Exp_MonthKey]
GO
ALTER TABLE [dbo].[FinancialCategories]  WITH CHECK ADD  CONSTRAINT [CK_Cat_Kind] CHECK  (([Kind]=N'revenue' OR [Kind]=N'expense'))
GO
ALTER TABLE [dbo].[FinancialCategories] CHECK CONSTRAINT [CK_Cat_Kind]
GO
ALTER TABLE [dbo].[GlobalSettings]  WITH CHECK ADD  CONSTRAINT [CK_GS_SingleRow] CHECK  (([Id]=N'global'))
GO
ALTER TABLE [dbo].[GlobalSettings] CHECK CONSTRAINT [CK_GS_SingleRow]
GO
ALTER TABLE [dbo].[MaintenanceRecords]  WITH CHECK ADD  CONSTRAINT [CK_Mnt_Cost] CHECK  (([Cost]>=(0)))
GO
ALTER TABLE [dbo].[MaintenanceRecords] CHECK CONSTRAINT [CK_Mnt_Cost]
GO
ALTER TABLE [dbo].[MaintenanceRecords]  WITH CHECK ADD  CONSTRAINT [CK_Mnt_Status] CHECK  (([Status]=N'cancelled' OR [Status]=N'done' OR [Status]=N'inprogress' OR [Status]=N'open'))
GO
ALTER TABLE [dbo].[MaintenanceRecords] CHECK CONSTRAINT [CK_Mnt_Status]
GO
ALTER TABLE [dbo].[MonthlyApartmentShares]  WITH CHECK ADD  CONSTRAINT [CK_MAS_MonthKey] CHECK  (([MonthKey] like '[0-9][0-9][0-9][0-9]-[0-9][0-9]'))
GO
ALTER TABLE [dbo].[MonthlyApartmentShares] CHECK CONSTRAINT [CK_MAS_MonthKey]
GO
ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [CK_Notif_RecipientType] CHECK  (([RecipientType]=N'resident' OR [RecipientType]=N'admin'))
GO
ALTER TABLE [dbo].[Notifications] CHECK CONSTRAINT [CK_Notif_RecipientType]
GO
ALTER TABLE [dbo].[QrTokens]  WITH CHECK ADD  CONSTRAINT [CK_Qr_UseType] CHECK  (([UseType]=N'multi' OR [UseType]=N'single'))
GO
ALTER TABLE [dbo].[QrTokens] CHECK CONSTRAINT [CK_Qr_UseType]
GO
ALTER TABLE [dbo].[Revenues]  WITH CHECK ADD  CONSTRAINT [CK_Rev_Amount] CHECK  (([Amount]>=(0)))
GO
ALTER TABLE [dbo].[Revenues] CHECK CONSTRAINT [CK_Rev_Amount]
GO
ALTER TABLE [dbo].[Revenues]  WITH CHECK ADD  CONSTRAINT [CK_Rev_CatKind] CHECK  (([CategoryKind]=N'revenue'))
GO
ALTER TABLE [dbo].[Revenues] CHECK CONSTRAINT [CK_Rev_CatKind]
GO
ALTER TABLE [dbo].[Revenues]  WITH CHECK ADD  CONSTRAINT [CK_Rev_MonthKey] CHECK  (([MonthKey] like '[0-9][0-9][0-9][0-9]-[0-9][0-9]'))
GO
ALTER TABLE [dbo].[Revenues] CHECK CONSTRAINT [CK_Rev_MonthKey]
GO
ALTER TABLE [dbo].[ScheduledBackupSettings]  WITH CHECK ADD  CONSTRAINT [CK_SBS_Dom] CHECK  (([DayOfMonth] IS NULL OR [DayOfMonth]>=(1) AND [DayOfMonth]<=(31)))
GO
ALTER TABLE [dbo].[ScheduledBackupSettings] CHECK CONSTRAINT [CK_SBS_Dom]
GO
ALTER TABLE [dbo].[ScheduledBackupSettings]  WITH CHECK ADD  CONSTRAINT [CK_SBS_Dow] CHECK  (([DayOfWeek] IS NULL OR [DayOfWeek]>=(0) AND [DayOfWeek]<=(6)))
GO
ALTER TABLE [dbo].[ScheduledBackupSettings] CHECK CONSTRAINT [CK_SBS_Dow]
GO
ALTER TABLE [dbo].[ScheduledBackupSettings]  WITH CHECK ADD  CONSTRAINT [CK_SBS_Hour] CHECK  (([Hour]>=(0) AND [Hour]<=(23)))
GO
ALTER TABLE [dbo].[ScheduledBackupSettings] CHECK CONSTRAINT [CK_SBS_Hour]
GO
ALTER TABLE [dbo].[ScheduledBackupSettings]  WITH CHECK ADD  CONSTRAINT [CK_SBS_Json] CHECK  ((isjson([CollectionsJson])=(1)))
GO
ALTER TABLE [dbo].[ScheduledBackupSettings] CHECK CONSTRAINT [CK_SBS_Json]
GO
ALTER TABLE [dbo].[Users]  WITH CHECK ADD  CONSTRAINT [CK_Users_Role] CHECK  (([Role]=N'resident' OR [Role]=N'admin' OR [Role]=N'superadmin'))
GO
ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [CK_Users_Role]
GO
ALTER TABLE [dbo].[WalletAdjustments]  WITH CHECK ADD  CONSTRAINT [CK_WA_MonthKey] CHECK  (([MonthKey] like '[0-9][0-9][0-9][0-9]-[0-9][0-9]'))
GO
ALTER TABLE [dbo].[WalletAdjustments] CHECK CONSTRAINT [CK_WA_MonthKey]
GO
USE [master]
GO
ALTER DATABASE [BuildingManagement] SET  READ_WRITE 
GO
