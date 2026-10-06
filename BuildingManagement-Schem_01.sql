-- >>> THIS IS THE CORRECT FILE - v3.2 FINAL - 2026-10-06 <<<
-- >>> لو مش شايف السطر ده في SSMS يبقى الملف ده مش اللي بتشغله <<<
/* =====================================================================
   BuildingManagement - SQL Server (T-SQL) schema  [ v3.2 - Idempotent ]
   =====================================================================
   - كل جدول محمي بـ IF OBJECT_ID ... IS NULL
   - كل Index محمي بـ IF NOT EXISTS على sys.indexes
   - كل FK/CHECK محمي جواه CREATE TABLE (فمش محتاج حماية)
   - السكربت آمن يتنفذ أكثر من مرة
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---------------------------------------------------------------------
   0) إنشاء قاعدة البيانات (لو مش موجودة)
   --------------------------------------------------------------------- */
IF DB_ID(N'BuildingManagement') IS NULL
BEGIN
    CREATE DATABASE BuildingManagement COLLATE Arabic_100_CI_AS;
    PRINT N'✅ Database BuildingManagement created';
END
ELSE
    PRINT N'⚠️ Database BuildingManagement already exists';
GO

USE BuildingManagement;
GO

/* =====================================================================
   1) Users
   ===================================================================== */
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Uid             NVARCHAR(128)  NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Email           NVARCHAR(256)  NOT NULL CONSTRAINT DF_Users_Email DEFAULT (N''),
        Name            NVARCHAR(200)  NOT NULL CONSTRAINT DF_Users_Name DEFAULT (N''),
        Phone           NVARCHAR(32)   NOT NULL CONSTRAINT DF_Users_Phone DEFAULT (N''),
        Whatsapp        NVARCHAR(32)   NOT NULL CONSTRAINT DF_Users_Whatsapp DEFAULT (N''),
        Pin             NVARCHAR(256)  NOT NULL CONSTRAINT DF_Users_Pin DEFAULT (N''),
        PhotoUrl        NVARCHAR(1000) NOT NULL CONSTRAINT DF_Users_PhotoUrl DEFAULT (N''),
        Role            NVARCHAR(20)   NOT NULL,
        IsDisabled      BIT            NOT NULL CONSTRAINT DF_Users_IsDisabled DEFAULT (0),
        DisabledReason  NVARCHAR(500)  NOT NULL CONSTRAINT DF_Users_DisabledReason DEFAULT (N''),
        IsActive        BIT            NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        LastLoginAt     DATETIME2(0)   NULL,
        CreatedAt       DATETIME2(0)   NULL,
        CreatedBy       NVARCHAR(128)  NULL,
        UpdatedAt       DATETIME2(0)   NULL,
        UpdatedBy       NVARCHAR(128)  NULL,
        CONSTRAINT CK_Users_Role CHECK (Role IN (N'superadmin', N'admin', N'resident'))
    );
    PRINT N'✅ dbo.Users created';
END
ELSE
    PRINT N'⚠️ dbo.Users already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_Email' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE INDEX IX_Users_Email ON dbo.Users (Email);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_Role' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE INDEX IX_Users_Role ON dbo.Users (Role);
GO

IF OBJECT_ID(N'dbo.UserPermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserPermissions (
        Uid        NVARCHAR(128) NOT NULL,
        Permission NVARCHAR(60)  NOT NULL,
        CONSTRAINT PK_UserPermissions PRIMARY KEY (Uid, Permission),
        CONSTRAINT FK_UserPermissions_Users FOREIGN KEY (Uid)
            REFERENCES dbo.Users (Uid) ON DELETE CASCADE
    );
    PRINT N'✅ dbo.UserPermissions created';
END
ELSE
    PRINT N'⚠️ dbo.UserPermissions already exists';
GO

/* =====================================================================
   2) Buildings
   ===================================================================== */
IF OBJECT_ID(N'dbo.Buildings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Buildings (
        Id              NVARCHAR(64)   NOT NULL CONSTRAINT PK_Buildings PRIMARY KEY,
        BuildingNumber  NVARCHAR(50)   NOT NULL,
        Name            NVARCHAR(200)  NOT NULL,
        AdminPin        NVARCHAR(256)  NOT NULL CONSTRAINT DF_Buildings_AdminPin DEFAULT (N''),
        DataVersion     NVARCHAR(20)   NOT NULL CONSTRAINT DF_Buildings_DataVersion DEFAULT (N'3.2'),
        LogoUrl         NVARCHAR(1000) NOT NULL CONSTRAINT DF_Buildings_LogoUrl DEFAULT (N''),
        AdminWhatsapp   NVARCHAR(32)   NOT NULL CONSTRAINT DF_Buildings_AdminWhatsapp DEFAULT (N''),
        PaymentLabel         NVARCHAR(200) NOT NULL CONSTRAINT DF_Buildings_PayLabel DEFAULT (N''),
        PaymentAccountNumber NVARCHAR(100) NOT NULL CONSTRAINT DF_Buildings_PayAcc   DEFAULT (N''),
        PaymentPhone         NVARCHAR(32)  NOT NULL CONSTRAINT DF_Buildings_PayPhone DEFAULT (N''),
        PaymentNotes         NVARCHAR(500) NOT NULL CONSTRAINT DF_Buildings_PayNotes DEFAULT (N''),
        CreatedAt       DATETIME2(0)   NOT NULL CONSTRAINT DF_Buildings_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Buildings_BuildingNumber UNIQUE (BuildingNumber)
    );
    PRINT N'✅ dbo.Buildings created';
END
ELSE
    PRINT N'⚠️ dbo.Buildings already exists';
GO

IF OBJECT_ID(N'dbo.BuildingSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BuildingSettings (
        BuildingId          NVARCHAR(64)  NOT NULL CONSTRAINT PK_BuildingSettings PRIMARY KEY,
        DisplayName         NVARCHAR(200) NOT NULL CONSTRAINT DF_BS_DisplayName DEFAULT (N''),
        Address             NVARCHAR(500) NOT NULL CONSTRAINT DF_BS_Address     DEFAULT (N''),
        WhatsappNumber      NVARCHAR(32)  NOT NULL CONSTRAINT DF_BS_Whatsapp    DEFAULT (N''),
        InvoiceDayOfMonth   INT           NOT NULL CONSTRAINT DF_BS_InvoiceDay  DEFAULT (1),
        ExpenseDistribution NVARCHAR(30)  NOT NULL CONSTRAINT DF_BS_ExpDist     DEFAULT (N'equal'),
        VotingQuorumPercent INT           NOT NULL CONSTRAINT DF_BS_Quorum      DEFAULT (50),
        Currency            NVARCHAR(10)  NOT NULL CONSTRAINT DF_BS_Currency    DEFAULT (N'EGP'),
        UpdatedAt           DATETIME2(0)  NULL,
        UpdatedBy           NVARCHAR(128) NULL,
        CONSTRAINT FK_BuildingSettings_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE CASCADE,
        CONSTRAINT CK_BS_InvoiceDay CHECK (InvoiceDayOfMonth BETWEEN 1 AND 31),
        CONSTRAINT CK_BS_Quorum     CHECK (VotingQuorumPercent BETWEEN 0 AND 100)
    );
    PRINT N'✅ dbo.BuildingSettings created';
END
ELSE
    PRINT N'⚠️ dbo.BuildingSettings already exists';
GO

IF OBJECT_ID(N'dbo.BuildingAdmins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BuildingAdmins (
        BuildingId NVARCHAR(64)  NOT NULL,
        AdminUid   NVARCHAR(128) NOT NULL,
        CONSTRAINT PK_BuildingAdmins PRIMARY KEY (BuildingId, AdminUid),
        CONSTRAINT FK_BuildingAdmins_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE CASCADE,
        CONSTRAINT FK_BuildingAdmins_Users FOREIGN KEY (AdminUid)
            REFERENCES dbo.Users (Uid) ON DELETE CASCADE
    );
    PRINT N'✅ dbo.BuildingAdmins created';
END
ELSE
    PRINT N'⚠️ dbo.BuildingAdmins already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BuildingAdmins_AdminUid' AND object_id = OBJECT_ID(N'dbo.BuildingAdmins'))
    CREATE INDEX IX_BuildingAdmins_AdminUid ON dbo.BuildingAdmins (AdminUid);
GO

IF OBJECT_ID(N'dbo.Floors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Floors (
        BuildingId NVARCHAR(64)  NOT NULL,
        Id         NVARCHAR(64)  NOT NULL,
        Label      NVARCHAR(100) NOT NULL,
        SortOrder  INT           NOT NULL CONSTRAINT DF_Floors_SortOrder DEFAULT (0),
        CONSTRAINT PK_Floors PRIMARY KEY (BuildingId, Id),
        CONSTRAINT FK_Floors_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE CASCADE
    );
    PRINT N'✅ dbo.Floors created';
END
ELSE
    PRINT N'⚠️ dbo.Floors already exists';
GO

IF OBJECT_ID(N'dbo.Apartments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Apartments (
        BuildingId     NVARCHAR(64)  NOT NULL,
        Id             NVARCHAR(64)  NOT NULL,
        FloorId        NVARCHAR(64)  NOT NULL,
        Number         INT           NOT NULL,
        Owner          NVARCHAR(200) NOT NULL CONSTRAINT DF_Apt_Owner DEFAULT (N''),
        Phone          NVARCHAR(32)  NOT NULL CONSTRAINT DF_Apt_Phone DEFAULT (N''),
        Email          NVARCHAR(256) NOT NULL CONSTRAINT DF_Apt_Email DEFAULT (N''),
        MonthlyFee     DECIMAL(18,2) NOT NULL CONSTRAINT DF_Apt_Fee   DEFAULT (0),
        Pin            NVARCHAR(256) NOT NULL CONSTRAINT DF_Apt_Pin   DEFAULT (N''),
        IsClosed       BIT           NOT NULL CONSTRAINT DF_Apt_IsClosed DEFAULT (0),
        Label          NVARCHAR(100) NOT NULL CONSTRAINT DF_Apt_Label DEFAULT (N''),
        Notes          NVARCHAR(1000) NOT NULL CONSTRAINT DF_Apt_Notes DEFAULT (N''),
        OpenDate       DATE          NULL,
        CloseDate      DATE          NULL,
        IsDisabled     BIT           NOT NULL CONSTRAINT DF_Apt_IsDisabled DEFAULT (0),
        DisabledReason NVARCHAR(500) NOT NULL CONSTRAINT DF_Apt_DisabledReason DEFAULT (N''),
        Language               NVARCHAR(10) NOT NULL CONSTRAINT DF_Apt_Lang DEFAULT (N'ar'),
        NotifyWhatsApp         BIT          NOT NULL CONSTRAINT DF_Apt_NWa  DEFAULT (1),
        NotifyEmail            BIT          NOT NULL CONSTRAINT DF_Apt_NEm  DEFAULT (0),
        NotifyInApp            BIT          NOT NULL CONSTRAINT DF_Apt_NIn  DEFAULT (1),
        PreferredPaymentMethod NVARCHAR(30) NOT NULL CONSTRAINT DF_Apt_PayMethod DEFAULT (N'instapay'),
        SettingsUpdatedAt      DATETIME2(0) NULL,
        SettingsUpdatedBy      NVARCHAR(128) NULL,
        CONSTRAINT PK_Apartments PRIMARY KEY (BuildingId, Id),
        CONSTRAINT UQ_Apartments_Number UNIQUE (BuildingId, Number),
        CONSTRAINT FK_Apartments_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Apartments_Floors FOREIGN KEY (BuildingId, FloorId)
            REFERENCES dbo.Floors (BuildingId, Id),
        CONSTRAINT CK_Apartments_Fee CHECK (MonthlyFee >= 0)
    );
    PRINT N'✅ dbo.Apartments created';
END
ELSE
    PRINT N'⚠️ dbo.Apartments already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Apartments_Floor' AND object_id = OBJECT_ID(N'dbo.Apartments'))
    CREATE INDEX IX_Apartments_Floor ON dbo.Apartments (BuildingId, FloorId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Apartments_Phone' AND object_id = OBJECT_ID(N'dbo.Apartments'))
    CREATE INDEX IX_Apartments_Phone ON dbo.Apartments (Phone) WHERE Phone <> N'';
GO

/* =====================================================================
   3) Dvrs + Cameras
   ===================================================================== */
IF OBJECT_ID(N'dbo.Dvrs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Dvrs (
        Id                 NVARCHAR(64)   NOT NULL,
        BuildingId         NVARCHAR(64)   NOT NULL,
        Name               NVARCHAR(200)  NOT NULL,
        IpAddress          NVARCHAR(50)   NOT NULL,
        Port               INT            NOT NULL CONSTRAINT DF_Dvrs_Port   DEFAULT (554),
        Brand              NVARCHAR(50)   NOT NULL CONSTRAINT DF_Dvrs_Brand  DEFAULT (N''),
        Username           NVARCHAR(100)  NOT NULL CONSTRAINT DF_Dvrs_User   DEFAULT (N''),
        PasswordEncrypted  NVARCHAR(500)  NOT NULL CONSTRAINT DF_Dvrs_Pwd    DEFAULT (N''),
        IsActive           BIT            NOT NULL CONSTRAINT DF_Dvrs_Active DEFAULT (1),
        CreatedAt          DATETIME2(0)   NOT NULL CONSTRAINT DF_Dvrs_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt          DATETIME2(0)   NULL,
        CONSTRAINT PK_Dvrs PRIMARY KEY (Id),
        CONSTRAINT FK_Dvrs_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE CASCADE
    );
    PRINT N'✅ dbo.Dvrs created';
END
ELSE
    PRINT N'⚠️ dbo.Dvrs already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Dvrs_BuildingId' AND object_id = OBJECT_ID(N'dbo.Dvrs'))
    CREATE INDEX IX_Dvrs_BuildingId ON dbo.Dvrs (BuildingId);
GO

IF OBJECT_ID(N'dbo.Cameras', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cameras (
        Id          NVARCHAR(64)   NOT NULL,
        DvrId       NVARCHAR(64)   NOT NULL,
        BuildingId  NVARCHAR(64)   NOT NULL,
        Name        NVARCHAR(200)  NOT NULL,
        Channel     INT            NOT NULL,
        RtspPath    NVARCHAR(300)  NOT NULL CONSTRAINT DF_Cameras_Rtsp DEFAULT (N''),
        HlsUrl      NVARCHAR(300)  NOT NULL CONSTRAINT DF_Cameras_Hls  DEFAULT (N''),
        IsActive    BIT            NOT NULL CONSTRAINT DF_Cameras_Active DEFAULT (1),
        CreatedAt   DATETIME2(0)   NOT NULL CONSTRAINT DF_Cameras_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Cameras PRIMARY KEY (Id),
        CONSTRAINT FK_Cameras_Dvrs FOREIGN KEY (DvrId)
            REFERENCES dbo.Dvrs (Id) ON DELETE CASCADE
    );
    PRINT N'✅ dbo.Cameras created';
END
ELSE
    PRINT N'⚠️ dbo.Cameras already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Cameras_BuildingId' AND object_id = OBJECT_ID(N'dbo.Cameras'))
    CREATE INDEX IX_Cameras_BuildingId ON dbo.Cameras (BuildingId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Cameras_DvrId_Channel' AND object_id = OBJECT_ID(N'dbo.Cameras'))
    CREATE UNIQUE INDEX IX_Cameras_DvrId_Channel ON dbo.Cameras (DvrId, Channel);
GO

/* =====================================================================
   4) FinancialCategories + Expenses + Revenues + MonthlyApartmentShares
   ===================================================================== */
IF OBJECT_ID(N'dbo.FinancialCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FinancialCategories (
        BuildingId NVARCHAR(64)  NOT NULL,
        Id         NVARCHAR(64)  NOT NULL,
        Kind       NVARCHAR(10)  NOT NULL,
        Name       NVARCHAR(200) NOT NULL,
        Color      NVARCHAR(20)  NOT NULL CONSTRAINT DF_Cat_Color DEFAULT (N''),
        IsActive   BIT           NOT NULL CONSTRAINT DF_Cat_IsActive DEFAULT (1),
        SortOrder  INT           NOT NULL CONSTRAINT DF_Cat_SortOrder DEFAULT (0),
        CONSTRAINT PK_FinancialCategories PRIMARY KEY (BuildingId, Kind, Id),
        CONSTRAINT FK_Cat_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE CASCADE,
        CONSTRAINT CK_Cat_Kind CHECK (Kind IN (N'expense', N'revenue'))
    );
    PRINT N'✅ dbo.FinancialCategories created';
END
ELSE
    PRINT N'⚠️ dbo.FinancialCategories already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Cat_Kind' AND object_id = OBJECT_ID(N'dbo.FinancialCategories'))
    CREATE INDEX IX_Cat_Kind ON dbo.FinancialCategories (BuildingId, Kind, SortOrder);
GO

IF OBJECT_ID(N'dbo.Expenses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Expenses (
        BuildingId   NVARCHAR(64)  NOT NULL,
        Id           NVARCHAR(64)  NOT NULL,
        MonthKey     CHAR(7)       NOT NULL,
        CategoryId   NVARCHAR(64)  NULL,
        CategoryKind NVARCHAR(10)  NOT NULL CONSTRAINT DF_Exp_CatKind DEFAULT (N'expense'),
        Note         NVARCHAR(1000) NOT NULL CONSTRAINT DF_Exp_Note DEFAULT (N''),
        Amount       DECIMAL(18,2) NOT NULL,
        ExpenseDate  DATE          NOT NULL,
        ReceiptUrl   NVARCHAR(1000) NULL,
        CONSTRAINT PK_Expenses PRIMARY KEY (BuildingId, Id),
        CONSTRAINT FK_Exp_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Exp_Category FOREIGN KEY (BuildingId, CategoryKind, CategoryId)
            REFERENCES dbo.FinancialCategories (BuildingId, Kind, Id),
        CONSTRAINT CK_Exp_CatKind  CHECK (CategoryKind = N'expense'),
        CONSTRAINT CK_Exp_MonthKey CHECK (MonthKey LIKE '[0-9][0-9][0-9][0-9]-[0-9][0-9]'),
        CONSTRAINT CK_Exp_Amount   CHECK (Amount >= 0)
    );
    PRINT N'✅ dbo.Expenses created';
END
ELSE
    PRINT N'⚠️ dbo.Expenses already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Exp_Month' AND object_id = OBJECT_ID(N'dbo.Expenses'))
    CREATE INDEX IX_Exp_Month ON dbo.Expenses (BuildingId, MonthKey);
GO

IF OBJECT_ID(N'dbo.Revenues', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Revenues (
        BuildingId   NVARCHAR(64)  NOT NULL,
        Id           NVARCHAR(64)  NOT NULL,
        MonthKey     CHAR(7)       NOT NULL,
        CategoryId   NVARCHAR(64)  NULL,
        CategoryKind NVARCHAR(10)  NOT NULL CONSTRAINT DF_Rev_CatKind DEFAULT (N'revenue'),
        Note         NVARCHAR(1000) NOT NULL CONSTRAINT DF_Rev_Note DEFAULT (N''),
        Amount       DECIMAL(18,2) NOT NULL,
        RevenueDate  DATE          NOT NULL,
        CONSTRAINT PK_Revenues PRIMARY KEY (BuildingId, Id),
        CONSTRAINT FK_Rev_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Rev_Category FOREIGN KEY (BuildingId, CategoryKind, CategoryId)
            REFERENCES dbo.FinancialCategories (BuildingId, Kind, Id),
        CONSTRAINT CK_Rev_CatKind  CHECK (CategoryKind = N'revenue'),
        CONSTRAINT CK_Rev_MonthKey CHECK (MonthKey LIKE '[0-9][0-9][0-9][0-9]-[0-9][0-9]'),
        CONSTRAINT CK_Rev_Amount   CHECK (Amount >= 0)
    );
    PRINT N'✅ dbo.Revenues created';
END
ELSE
    PRINT N'⚠️ dbo.Revenues already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Rev_Month' AND object_id = OBJECT_ID(N'dbo.Revenues'))
    CREATE INDEX IX_Rev_Month ON dbo.Revenues (BuildingId, MonthKey);
GO

IF OBJECT_ID(N'dbo.MonthlyApartmentShares', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MonthlyApartmentShares (
        BuildingId   NVARCHAR(64)  NOT NULL,
        MonthKey     CHAR(7)       NOT NULL,
        ApartmentId  NVARCHAR(64)  NOT NULL,
        ExpenseShare DECIMAL(18,2) NOT NULL CONSTRAINT DF_MAS_Exp   DEFAULT (0),
        RevenueShare DECIMAL(18,2) NOT NULL CONSTRAINT DF_MAS_Rev   DEFAULT (0),
        CarryOver    DECIMAL(18,2) NOT NULL CONSTRAINT DF_MAS_Carry DEFAULT (0),
        CONSTRAINT PK_MonthlyApartmentShares PRIMARY KEY (BuildingId, MonthKey, ApartmentId),
        CONSTRAINT FK_MAS_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE CASCADE,
        CONSTRAINT FK_MAS_Apartments FOREIGN KEY (BuildingId, ApartmentId)
            REFERENCES dbo.Apartments (BuildingId, Id),
        CONSTRAINT CK_MAS_MonthKey CHECK (MonthKey LIKE '[0-9][0-9][0-9][0-9]-[0-9][0-9]')
    );
    PRINT N'✅ dbo.MonthlyApartmentShares created';
END
ELSE
    PRINT N'⚠️ dbo.MonthlyApartmentShares already exists';
GO

/* =====================================================================
   5) Deposits + DepositEditHistory + WalletAdjustments
   ===================================================================== */
IF OBJECT_ID(N'dbo.Deposits', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Deposits (
        BuildingId      NVARCHAR(64)  NOT NULL,
        Id              NVARCHAR(64)  NOT NULL,
        MonthKey        CHAR(7)       NOT NULL,
        Number          NVARCHAR(50)  NOT NULL CONSTRAINT DF_Dep_Number DEFAULT (N''),
        ApartmentId     NVARCHAR(64)  NOT NULL,
        Amount          DECIMAL(18,2) NOT NULL,
        Note            NVARCHAR(1000) NOT NULL CONSTRAINT DF_Dep_Note DEFAULT (N''),
        Status          NVARCHAR(20)  NOT NULL CONSTRAINT DF_Dep_Status DEFAULT (N'pending'),
        ReceiptUrl      NVARCHAR(1000) NULL,
        ReceiptPath     NVARCHAR(500)  NULL,
        ReceiptFileName NVARCHAR(260)  NULL,
        ReceiptFileSize BIGINT         NULL,
        ReceiptFileType NVARCHAR(100)  NULL,
        ReceiptSuccess  BIT            NULL,
        CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_Dep_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy       NVARCHAR(128) NOT NULL CONSTRAINT DF_Dep_CreatedBy DEFAULT (N''),
        ConfirmedAt     DATETIME2(0)  NULL,
        ConfirmedBy     NVARCHAR(128) NULL,
        CancelledAt     DATETIME2(0)  NULL,
        CancelledBy     NVARCHAR(128) NULL,
        CancelledReason NVARCHAR(500) NULL,
        UpdatedAt       DATETIME2(0)  NULL,
        UpdatedBy       NVARCHAR(128) NULL,
        UpdateReason    NVARCHAR(500) NULL,
        CONSTRAINT PK_Deposits PRIMARY KEY (BuildingId, Id),
        CONSTRAINT FK_Dep_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Dep_Apartments FOREIGN KEY (BuildingId, ApartmentId)
            REFERENCES dbo.Apartments (BuildingId, Id),
        CONSTRAINT CK_Dep_Status   CHECK (Status IN (N'pending', N'confirmed', N'cancelled')),
        CONSTRAINT CK_Dep_MonthKey CHECK (MonthKey LIKE '[0-9][0-9][0-9][0-9]-[0-9][0-9]'),
        CONSTRAINT CK_Dep_Amount   CHECK (Amount > 0)
    );
    PRINT N'✅ dbo.Deposits created';
END
ELSE
    PRINT N'⚠️ dbo.Deposits already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Dep_Apartment' AND object_id = OBJECT_ID(N'dbo.Deposits'))
    CREATE INDEX IX_Dep_Apartment ON dbo.Deposits (BuildingId, ApartmentId, Status);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Dep_Month' AND object_id = OBJECT_ID(N'dbo.Deposits'))
    CREATE INDEX IX_Dep_Month ON dbo.Deposits (BuildingId, MonthKey, Status);
GO

IF OBJECT_ID(N'dbo.DepositEditHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DepositEditHistory (
        EditId     BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DepositEditHistory PRIMARY KEY,
        BuildingId NVARCHAR(64)  NOT NULL,
        DepositId  NVARCHAR(64)  NOT NULL,
        Ts         DATETIME2(0)  NOT NULL,
        ByUser     NVARCHAR(128) NOT NULL,
        Reason     NVARCHAR(500) NOT NULL CONSTRAINT DF_DEH_Reason  DEFAULT (N''),
        OldAmount  DECIMAL(18,2) NOT NULL,
        NewAmount  DECIMAL(18,2) NOT NULL,
        OldNote    NVARCHAR(1000) NOT NULL CONSTRAINT DF_DEH_OldNote DEFAULT (N''),
        NewNote    NVARCHAR(1000) NOT NULL CONSTRAINT DF_DEH_NewNote DEFAULT (N''),
        CONSTRAINT FK_DEH_Deposits FOREIGN KEY (BuildingId, DepositId)
            REFERENCES dbo.Deposits (BuildingId, Id) ON DELETE CASCADE
    );
    PRINT N'✅ dbo.DepositEditHistory created';
END
ELSE
    PRINT N'⚠️ dbo.DepositEditHistory already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DEH_Deposit' AND object_id = OBJECT_ID(N'dbo.DepositEditHistory'))
    CREATE INDEX IX_DEH_Deposit ON dbo.DepositEditHistory (BuildingId, DepositId, Ts);
GO

IF OBJECT_ID(N'dbo.WalletAdjustments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WalletAdjustments (
        BuildingId  NVARCHAR(64)  NOT NULL,
        Id          NVARCHAR(64)  NOT NULL,
        ApartmentId NVARCHAR(64)  NOT NULL,
        Amount      DECIMAL(18,2) NOT NULL,
        Reason      NVARCHAR(500) NOT NULL CONSTRAINT DF_WA_Reason DEFAULT (N''),
        MonthKey    CHAR(7)       NOT NULL,
        CreatedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_WA_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy   NVARCHAR(128) NOT NULL CONSTRAINT DF_WA_CreatedBy DEFAULT (N''),
        CONSTRAINT PK_WalletAdjustments PRIMARY KEY (BuildingId, Id),
        CONSTRAINT FK_WA_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE CASCADE,
        CONSTRAINT FK_WA_Apartments FOREIGN KEY (BuildingId, ApartmentId)
            REFERENCES dbo.Apartments (BuildingId, Id),
        CONSTRAINT CK_WA_MonthKey CHECK (MonthKey LIKE '[0-9][0-9][0-9][0-9]-[0-9][0-9]')
    );
    PRINT N'✅ dbo.WalletAdjustments created';
END
ELSE
    PRINT N'⚠️ dbo.WalletAdjustments already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WA_Apartment' AND object_id = OBJECT_ID(N'dbo.WalletAdjustments'))
    CREATE INDEX IX_WA_Apartment ON dbo.WalletAdjustments (BuildingId, ApartmentId);
GO

/* =====================================================================
   6) Receipts
   ===================================================================== */
IF OBJECT_ID(N'dbo.Receipts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Receipts (
        ReceiptId      BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Receipts PRIMARY KEY,
        BuildingId     NVARCHAR(64)   NULL,
        Url            NVARCHAR(1000) NOT NULL,
        PublicId       NVARCHAR(300)  NOT NULL CONSTRAINT DF_Receipts_PublicId DEFAULT (N''),
        FileName       NVARCHAR(260)  NOT NULL CONSTRAINT DF_Receipts_FileName DEFAULT (N''),
        UploadedAt     DATETIME2(0)   NOT NULL CONSTRAINT DF_Receipts_UploadedAt DEFAULT (SYSUTCDATETIME()),
        OcrText        NVARCHAR(MAX)  NULL,
        OcrAmountGuess DECIMAL(18,2)  NULL,
        CONSTRAINT FK_Receipts_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE SET NULL
    );
    PRINT N'✅ dbo.Receipts created';
END
ELSE
    PRINT N'⚠️ dbo.Receipts already exists';
GO

/* =====================================================================
   7) AuditLog
   ===================================================================== */
IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLog (
        AuditId        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLog PRIMARY KEY,
        SourceId       NVARCHAR(64)  NULL,
        BuildingId     NVARCHAR(64)  NULL,
        Ts             DATETIME2(0)  NOT NULL,
        Action         NVARCHAR(100) NOT NULL,
        Label          NVARCHAR(300) NOT NULL CONSTRAINT DF_Audit_Label DEFAULT (N''),
        Actor          NVARCHAR(200) NOT NULL CONSTRAINT DF_Audit_Actor DEFAULT (N''),
        ActorRole      NVARCHAR(20)  NOT NULL CONSTRAINT DF_Audit_ActorRole DEFAULT (N''),
        Details        NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Audit_Details DEFAULT (N''),
        MonthKey       NVARCHAR(7)   NOT NULL CONSTRAINT DF_Audit_Month DEFAULT (N''),
        BuildingNumber NVARCHAR(50)  NULL,
        AptNumber      INT           NULL,
        CONSTRAINT FK_Audit_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE SET NULL
    );
    PRINT N'✅ dbo.AuditLog created';
END
ELSE
    PRINT N'⚠️ dbo.AuditLog already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Audit_Building_Ts' AND object_id = OBJECT_ID(N'dbo.AuditLog'))
    CREATE INDEX IX_Audit_Building_Ts ON dbo.AuditLog (BuildingId, Ts DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Audit_Apt' AND object_id = OBJECT_ID(N'dbo.AuditLog'))
    CREATE INDEX IX_Audit_Apt ON dbo.AuditLog (BuildingId, AptNumber, Ts DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Audit_Action' AND object_id = OBJECT_ID(N'dbo.AuditLog'))
    CREATE INDEX IX_Audit_Action ON dbo.AuditLog (Action, Ts DESC);
GO

/* =====================================================================
   8) Notifications
   ===================================================================== */
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications (
        BuildingId           NVARCHAR(64)  NOT NULL,
        Id                   NVARCHAR(64)  NOT NULL,
        Ts                   DATETIME2(0)  NOT NULL,
        RecipientType        NVARCHAR(10)  NOT NULL,
        RecipientApartmentId NVARCHAR(64)  NULL,
        Type                 NVARCHAR(30)  NOT NULL CONSTRAINT DF_Notif_Type DEFAULT (N'info'),
        Icon                 NVARCHAR(20)  NOT NULL CONSTRAINT DF_Notif_Icon DEFAULT (N'🔔'),
        Title                NVARCHAR(300) NOT NULL CONSTRAINT DF_Notif_Title DEFAULT (N''),
        Body                 NVARCHAR(2000) NOT NULL CONSTRAINT DF_Notif_Body DEFAULT (N''),
        IsRead               BIT           NOT NULL CONSTRAINT DF_Notif_IsRead DEFAULT (0),
        ReadAt               DATETIME2(0)  NULL,
        CONSTRAINT PK_Notifications PRIMARY KEY (BuildingId, Id),
        CONSTRAINT FK_Notif_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Notif_Apartments FOREIGN KEY (BuildingId, RecipientApartmentId)
            REFERENCES dbo.Apartments (BuildingId, Id),
        CONSTRAINT CK_Notif_RecipientType CHECK (RecipientType IN (N'admin', N'resident'))
    );
    PRINT N'✅ dbo.Notifications created';
END
ELSE
    PRINT N'⚠️ dbo.Notifications already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Notif_Recipient' AND object_id = OBJECT_ID(N'dbo.Notifications'))
    CREATE INDEX IX_Notif_Recipient ON dbo.Notifications
        (BuildingId, RecipientType, RecipientApartmentId, IsRead, Ts DESC);
GO

/* =====================================================================
   9) Polls + PollOptions + PollVotes
   ===================================================================== */
IF OBJECT_ID(N'dbo.Polls', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Polls (
        BuildingId  NVARCHAR(64)  NOT NULL,
        Id          NVARCHAR(64)  NOT NULL,
        Question    NVARCHAR(500) NOT NULL,
        Description NVARCHAR(2000) NOT NULL CONSTRAINT DF_Poll_Desc DEFAULT (N''),
        CreatedBy   NVARCHAR(128) NOT NULL CONSTRAINT DF_Poll_CreatedBy DEFAULT (N''),
        CreatedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_Poll_CreatedAt DEFAULT (SYSUTCDATETIME()),
        Deadline    DATETIME2(0)  NULL,
        IsClosed    BIT           NOT NULL CONSTRAINT DF_Poll_IsClosed DEFAULT (0),
        CONSTRAINT PK_Polls PRIMARY KEY (BuildingId, Id),
        CONSTRAINT FK_Polls_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE CASCADE
    );
    PRINT N'✅ dbo.Polls created';
END
ELSE
    PRINT N'⚠️ dbo.Polls already exists';
GO

IF OBJECT_ID(N'dbo.PollOptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PollOptions (
        BuildingId NVARCHAR(64)  NOT NULL,
        PollId     NVARCHAR(64)  NOT NULL,
        Id         NVARCHAR(64)  NOT NULL,
        Text       NVARCHAR(500) NOT NULL,
        SortOrder  INT           NOT NULL CONSTRAINT DF_PollOptions_Sort DEFAULT (0),
        CONSTRAINT PK_PollOptions PRIMARY KEY (BuildingId, PollId, Id),
        CONSTRAINT FK_PollOptions_Polls FOREIGN KEY (BuildingId, PollId)
            REFERENCES dbo.Polls (BuildingId, Id) ON DELETE CASCADE
    );
    PRINT N'✅ dbo.PollOptions created';
END
ELSE
    PRINT N'⚠️ dbo.PollOptions already exists';
GO

IF OBJECT_ID(N'dbo.PollVotes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PollVotes (
        BuildingId  NVARCHAR(64) NOT NULL,
        PollId      NVARCHAR(64) NOT NULL,
        ApartmentId NVARCHAR(64) NOT NULL,
        OptionId    NVARCHAR(64) NOT NULL,
        VotedAt     DATETIME2(0) NOT NULL CONSTRAINT DF_PollVotes_VotedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_PollVotes PRIMARY KEY (BuildingId, PollId, ApartmentId),
        CONSTRAINT FK_PollVotes_Option FOREIGN KEY (BuildingId, PollId, OptionId)
            REFERENCES dbo.PollOptions (BuildingId, PollId, Id) ON DELETE CASCADE,
        CONSTRAINT FK_PollVotes_Apartment FOREIGN KEY (BuildingId, ApartmentId)
            REFERENCES dbo.Apartments (BuildingId, Id)
    );
    PRINT N'✅ dbo.PollVotes created';
END
ELSE
    PRINT N'⚠️ dbo.PollVotes already exists';
GO

/* =====================================================================
   10) MaintenanceRecords
   ===================================================================== */
IF OBJECT_ID(N'dbo.MaintenanceRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MaintenanceRecords (
        BuildingId       NVARCHAR(64)  NOT NULL,
        Id               NVARCHAR(64)  NOT NULL,
        Title            NVARCHAR(300) NOT NULL,
        Description      NVARCHAR(2000) NOT NULL CONSTRAINT DF_Mnt_Desc DEFAULT (N''),
        MaintenanceDate  DATE          NOT NULL,
        Cost             DECIMAL(18,2) NOT NULL CONSTRAINT DF_Mnt_Cost DEFAULT (0),
        Vendor           NVARCHAR(200) NOT NULL CONSTRAINT DF_Mnt_Vendor DEFAULT (N''),
        Status           NVARCHAR(20)  NOT NULL CONSTRAINT DF_Mnt_Status DEFAULT (N'open'),
        CreatedBy        NVARCHAR(128) NOT NULL CONSTRAINT DF_Mnt_CreatedBy DEFAULT (N''),
        CreatedAt        DATETIME2(0)  NOT NULL CONSTRAINT DF_Mnt_CreatedAt DEFAULT (SYSUTCDATETIME()),
        AddedToExpense   BIT           NOT NULL CONSTRAINT DF_Mnt_AddedToExp DEFAULT (0),
        AddedToExpenseAt DATETIME2(0)  NULL,
        CONSTRAINT PK_MaintenanceRecords PRIMARY KEY (BuildingId, Id),
        CONSTRAINT FK_Mnt_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE CASCADE,
        CONSTRAINT CK_Mnt_Status CHECK (Status IN (N'open', N'inprogress', N'done', N'cancelled')),
        CONSTRAINT CK_Mnt_Cost   CHECK (Cost >= 0)
    );
    PRINT N'✅ dbo.MaintenanceRecords created';
END
ELSE
    PRINT N'⚠️ dbo.MaintenanceRecords already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Mnt_Status' AND object_id = OBJECT_ID(N'dbo.MaintenanceRecords'))
    CREATE INDEX IX_Mnt_Status ON dbo.MaintenanceRecords (BuildingId, Status, MaintenanceDate DESC);
GO

/* =====================================================================
   11) QrTokens
   ===================================================================== */
IF OBJECT_ID(N'dbo.QrTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QrTokens (
        Uid         NVARCHAR(128) NOT NULL CONSTRAINT PK_QrTokens PRIMARY KEY,
        BuildingId  NVARCHAR(64)  NOT NULL,
        ApartmentId NVARCHAR(64)  NOT NULL,
        ExpiresAt   DATETIME2(3)  NOT NULL,
        UseType     NVARCHAR(10)  NOT NULL CONSTRAINT DF_Qr_UseType DEFAULT (N'single'),
        IssuedAt    DATETIME2(3)  NOT NULL,
        CreatedBy   NVARCHAR(128) NOT NULL CONSTRAINT DF_Qr_CreatedBy DEFAULT (N''),
        IsUsed      BIT           NOT NULL CONSTRAINT DF_Qr_IsUsed DEFAULT (0),
        UsedAt      DATETIME2(0)  NULL,
        UsedBy      NVARCHAR(128) NULL,
        CreatedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_Qr_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Qr_Buildings FOREIGN KEY (BuildingId)
            REFERENCES dbo.Buildings (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Qr_Apartments FOREIGN KEY (BuildingId, ApartmentId)
            REFERENCES dbo.Apartments (BuildingId, Id),
        CONSTRAINT CK_Qr_UseType CHECK (UseType IN (N'single', N'multi'))
    );
    PRINT N'✅ dbo.QrTokens created';
END
ELSE
    PRINT N'⚠️ dbo.QrTokens already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Qr_Apartment' AND object_id = OBJECT_ID(N'dbo.QrTokens'))
    CREATE INDEX IX_Qr_Apartment ON dbo.QrTokens (BuildingId, ApartmentId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Qr_Expires' AND object_id = OBJECT_ID(N'dbo.QrTokens'))
    CREATE INDEX IX_Qr_Expires ON dbo.QrTokens (ExpiresAt);
GO

/* =====================================================================
   12) Presence
   ===================================================================== */
IF OBJECT_ID(N'dbo.Presence', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Presence (
        Uid         NVARCHAR(128) NOT NULL CONSTRAINT PK_Presence PRIMARY KEY,
        BuildingId  NVARCHAR(64)  NULL,
        ApartmentId NVARCHAR(64)  NULL,
        LastSeen    DATETIME2(3)  NOT NULL
    );
    PRINT N'✅ dbo.Presence created';
END
ELSE
    PRINT N'⚠️ dbo.Presence already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Presence_LastSeen' AND object_id = OBJECT_ID(N'dbo.Presence'))
    CREATE INDEX IX_Presence_LastSeen ON dbo.Presence (BuildingId, LastSeen DESC);
GO

/* =====================================================================
   13) GlobalSettings
   ===================================================================== */
IF OBJECT_ID(N'dbo.GlobalSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GlobalSettings (
        Id                  NVARCHAR(20)  NOT NULL CONSTRAINT PK_GlobalSettings PRIMARY KEY
                            CONSTRAINT DF_GS_Id DEFAULT (N'global'),
        AppName             NVARCHAR(200) NOT NULL CONSTRAINT DF_GS_AppName DEFAULT (N'نظام إدارة العمارات'),
        DefaultLanguage     NVARCHAR(10)  NOT NULL CONSTRAINT DF_GS_Lang DEFAULT (N'ar'),
        WhatsappApiToken    NVARCHAR(500) NOT NULL CONSTRAINT DF_GS_WaToken DEFAULT (N''),
        WhatsappPhoneId     NVARCHAR(100) NOT NULL CONSTRAINT DF_GS_WaPhone DEFAULT (N''),
        CloudinaryCloudName NVARCHAR(100) NOT NULL CONSTRAINT DF_GS_CloudName DEFAULT (N''),
        CloudinaryApiKey    NVARCHAR(100) NOT NULL CONSTRAINT DF_GS_CloudKey DEFAULT (N''),
        FirebaseProjectId   NVARCHAR(100) NOT NULL CONSTRAINT DF_GS_FbProject DEFAULT (N''),
        StorageMode         NVARCHAR(20)  NOT NULL CONSTRAINT DF_GS_StorageMode DEFAULT (N'Dual'),
        EnablePolls         BIT NOT NULL CONSTRAINT DF_GS_Polls DEFAULT (1),
        EnableMaintenance   BIT NOT NULL CONSTRAINT DF_GS_Maint DEFAULT (1),
        EnableOcr           BIT NOT NULL CONSTRAINT DF_GS_Ocr   DEFAULT (1),
        EnableQrAccess      BIT NOT NULL CONSTRAINT DF_GS_Qr    DEFAULT (1),
        EnableWhatsApp      BIT NOT NULL CONSTRAINT DF_GS_Wa    DEFAULT (1),
        SessionTimeoutDays  INT NOT NULL CONSTRAINT DF_GS_Timeout DEFAULT (7),
        MaxPinAttempts      INT NOT NULL CONSTRAINT DF_GS_MaxPin  DEFAULT (5),
        UpdatedAt           DATETIME2(0)  NULL,
        UpdatedBy           NVARCHAR(128) NULL,
        CONSTRAINT CK_GS_SingleRow CHECK (Id = N'global')
    );

    INSERT INTO dbo.GlobalSettings (Id) VALUES (N'global');
    PRINT N'✅ dbo.GlobalSettings created + seeded';
END
ELSE
    PRINT N'⚠️ dbo.GlobalSettings already exists';
GO

/* =====================================================================
   14) ScheduledBackupSettings + BackupHistory
   ===================================================================== */
IF OBJECT_ID(N'dbo.ScheduledBackupSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ScheduledBackupSettings (
        Id               NVARCHAR(20)  NOT NULL CONSTRAINT PK_SBS PRIMARY KEY
                         CONSTRAINT DF_SBS_Id DEFAULT (N'default'),
        Enabled          BIT           NOT NULL CONSTRAINT DF_SBS_Enabled DEFAULT (0),
        Frequency        NVARCHAR(20)  NOT NULL CONSTRAINT DF_SBS_Freq DEFAULT (N'daily'),
        Hour             INT           NOT NULL CONSTRAINT DF_SBS_Hour DEFAULT (2),
        DayOfWeek        INT           NULL,
        DayOfMonth       INT           NULL,
        CollectionsJson  NVARCHAR(MAX) NOT NULL CONSTRAINT DF_SBS_Cols DEFAULT (N'[]'),
        MaxBackupsToKeep INT           NOT NULL CONSTRAINT DF_SBS_Max  DEFAULT (7),
        LastRunAt        DATETIME2(0)  NULL,
        LastRunStatus    NVARCHAR(30)  NULL,
        UpdatedAt        DATETIME2(0)  NULL,
        UpdatedBy        NVARCHAR(128) NULL,
        CONSTRAINT CK_SBS_Hour CHECK (Hour BETWEEN 0 AND 23),
        CONSTRAINT CK_SBS_Dow  CHECK (DayOfWeek IS NULL OR DayOfWeek BETWEEN 0 AND 6),
        CONSTRAINT CK_SBS_Dom  CHECK (DayOfMonth IS NULL OR DayOfMonth BETWEEN 1 AND 31),
        CONSTRAINT CK_SBS_Json CHECK (ISJSON(CollectionsJson) = 1)
    );

    INSERT INTO dbo.ScheduledBackupSettings (Id) VALUES (N'default');
    PRINT N'✅ dbo.ScheduledBackupSettings created + seeded';
END
ELSE
    PRINT N'⚠️ dbo.ScheduledBackupSettings already exists';
GO

IF OBJECT_ID(N'dbo.BackupHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BackupHistory (
        Id              NVARCHAR(64)  NOT NULL CONSTRAINT PK_BackupHistory PRIMARY KEY,
        FileName        NVARCHAR(260) NOT NULL,
        FileSize        BIGINT        NOT NULL CONSTRAINT DF_BH_Size DEFAULT (0),
        CollectionsJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_BH_Cols DEFAULT (N'[]'),
        TotalDocuments  INT           NOT NULL CONSTRAINT DF_BH_Docs DEFAULT (0),
        CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_BH_CreatedAt DEFAULT (SYSUTCDATETIME()),
        DurationSeconds FLOAT         NOT NULL CONSTRAINT DF_BH_Dur DEFAULT (0),
        Status          NVARCHAR(20)  NOT NULL CONSTRAINT DF_BH_Status DEFAULT (N'success'),
        Source          NVARCHAR(20)  NOT NULL CONSTRAINT DF_BH_Source DEFAULT (N'scheduled'),
        ErrorMessage    NVARCHAR(MAX) NULL,
        CONSTRAINT CK_BH_Json CHECK (ISJSON(CollectionsJson) = 1)
    );
    PRINT N'✅ dbo.BackupHistory created';
END
ELSE
    PRINT N'⚠️ dbo.BackupHistory already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BH_CreatedAt' AND object_id = OBJECT_ID(N'dbo.BackupHistory'))
    CREATE INDEX IX_BH_CreatedAt ON dbo.BackupHistory (CreatedAt DESC);
GO

/* =====================================================================
   15) SyncPendingChanges
   ===================================================================== */
IF OBJECT_ID(N'dbo.SyncPendingChanges', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SyncPendingChanges (
        Id           BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SyncPendingChanges PRIMARY KEY,
        EntityType   NVARCHAR(50)  NOT NULL,
        EntityId     NVARCHAR(128) NOT NULL,
        Operation    NVARCHAR(20)  NOT NULL,
        Payload      NVARCHAR(MAX) NULL,
        CreatedAt    DATETIME2(0)  NOT NULL CONSTRAINT DF_SPC_CreatedAt DEFAULT (SYSUTCDATETIME()),
        Applied      BIT           NOT NULL CONSTRAINT DF_SPC_Applied DEFAULT (0),
        AppliedAt    DATETIME2(0)  NULL,
        RetryCount   INT           NOT NULL CONSTRAINT DF_SPC_Retry DEFAULT (0),
        ErrorMessage NVARCHAR(MAX) NULL
    );
    PRINT N'✅ dbo.SyncPendingChanges created';
END
ELSE
    PRINT N'⚠️ dbo.SyncPendingChanges already exists';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SPC_Applied' AND object_id = OBJECT_ID(N'dbo.SyncPendingChanges'))
    CREATE INDEX IX_SPC_Applied ON dbo.SyncPendingChanges (Applied, CreatedAt);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SPC_Entity' AND object_id = OBJECT_ID(N'dbo.SyncPendingChanges'))
    CREATE INDEX IX_SPC_Entity ON dbo.SyncPendingChanges (EntityType, EntityId);
GO

/* =====================================================================
   16) View: vw_ApartmentWalletMovements
   ===================================================================== */
IF OBJECT_ID(N'dbo.vw_ApartmentWalletMovements', N'V') IS NULL
BEGIN
    EXEC(N'
        CREATE VIEW dbo.vw_ApartmentWalletMovements
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
            WHERE BuildingId = a.BuildingId AND ApartmentId = a.Id AND Status = N''confirmed''
        ) d
        OUTER APPLY (
            SELECT SUM(Amount) AS Adjustments
            FROM dbo.WalletAdjustments
            WHERE BuildingId = a.BuildingId AND ApartmentId = a.Id
        ) w;
    ');
    PRINT N'✅ dbo.vw_ApartmentWalletMovements created';
END
ELSE
    PRINT N'⚠️ dbo.vw_ApartmentWalletMovements already exists';
GO

/* =====================================================================
   17) تأكيد نهائي
   ===================================================================== */
PRINT N'====================================================';
PRINT N'✅ BuildingManagement schema is ready.';
PRINT N'====================================================';
GO

USE BuildingManagement;
GO

SET NOCOUNT ON;

/* =====================================================================
   عدّ السجلات في كل جدول User Table في قاعدة البيانات
   ===================================================================== */

DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql = @sql + 
    N'SELECT N''' + s.name + N'.' + t.name + N''' AS [TableName], ' +
    N'COUNT_BIG(*) AS [RowCount] FROM ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) +
    N' UNION ALL ' + CHAR(13) + CHAR(10)
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;

-- شيل آخر UNION ALL
SET @sql = LEFT(@sql, LEN(@sql) - LEN(' UNION ALL ' + CHAR(13) + CHAR(10)));

SET @sql = @sql + N' ORDER BY [TableName];';

PRINT @sql;  -- لو حابب تشوف الكود المتولّد
EXEC sp_executesql @sql;
GO