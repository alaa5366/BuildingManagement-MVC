-- =====================================================================
--  Add Residents Table — Phase 1
--  جدول الساكنين الجديد
-- =====================================================================

USE [BuildingManagement];
GO

-- ═══════════════════════════════════════════════════════════
-- 1. إنشاء الجدول
-- ═══════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Residents')
BEGIN
    CREATE TABLE [dbo].[Residents] (
        [Id]              NVARCHAR(64)   NOT NULL,
        [Uid]             NVARCHAR(128)  NOT NULL,
        [BuildingId]      NVARCHAR(64)   NOT NULL,
        [ApartmentId]     NVARCHAR(64)   NOT NULL,
        
        [Name]            NVARCHAR(200)  NOT NULL DEFAULT '',
        [Phone]           NVARCHAR(32)   NOT NULL DEFAULT '',
        [Whatsapp]        NVARCHAR(32)   NOT NULL DEFAULT '',
        [Email]           NVARCHAR(256)  NOT NULL DEFAULT '',
        [PhotoUrl]        NVARCHAR(1000) NOT NULL DEFAULT '',
        
        [PinHash]         NVARCHAR(256)  NOT NULL DEFAULT '',
        [PasswordHash]    NVARCHAR(256)  NULL,
        
        [IsActive]        BIT            NOT NULL DEFAULT 1,
        [IsDisabled]      BIT            NOT NULL DEFAULT 0,
        [DisabledReason]  NVARCHAR(500)  NOT NULL DEFAULT '',
        [IsOwner]         BIT            NOT NULL DEFAULT 1,
        [IsPrimary]       BIT            NOT NULL DEFAULT 1,
        
        [LastLoginAt]     DATETIME2(0)   NULL,
        [CreatedAt]       DATETIME2(0)   NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedBy]       NVARCHAR(128)  NULL,
        [UpdatedAt]       DATETIME2(0)   NULL,
        [UpdatedBy]       NVARCHAR(128)  NULL,
        
        CONSTRAINT [PK_Residents] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Residents_Apartments] 
            FOREIGN KEY ([BuildingId], [ApartmentId]) 
            REFERENCES [dbo].[Apartments] ([BuildingId], [Id])
            ON DELETE CASCADE
    );
    
    PRINT '✅ Residents table created';
END
ELSE
BEGIN
    PRINT '⚠️ Residents table already exists';
END
GO

-- ═══════════════════════════════════════════════════════════
-- 2. Indexes
-- ═══════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Residents_BuildingId')
    CREATE NONCLUSTERED INDEX [IX_Residents_BuildingId] 
        ON [dbo].[Residents] ([BuildingId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Residents_ApartmentId')
    CREATE NONCLUSTERED INDEX [IX_Residents_ApartmentId] 
        ON [dbo].[Residents] ([ApartmentId]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Residents_Phone')
    CREATE NONCLUSTERED INDEX [IX_Residents_Phone] 
        ON [dbo].[Residents] ([Phone]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Residents_Uid')
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Residents_Uid] 
        ON [dbo].[Residents] ([Uid]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Residents_Building_Phone')
    CREATE NONCLUSTERED INDEX [IX_Residents_Building_Phone] 
        ON [dbo].[Residents] ([BuildingId], [Phone]);
GO

PRINT '✅ Indexes created';
GO

-- ═══════════════════════════════════════════════════════════
-- 3. Migration من Apartments → Residents
-- ═══════════════════════════════════════════════════════════
-- ملاحظة: الـ PIN هيتم Hash باستخدام BCrypt
-- هنعمل Migration بعدين من C# (لأن BCrypt مش في SQL)

PRINT 'ℹ️ Data migration will be done via C# script';
GO

-- ═══════════════════════════════════════════════════════════
-- 4. Verification
-- ═══════════════════════════════════════════════════════════
SELECT 
    'Residents' AS TableName,
    COUNT(*) AS RowCount
FROM [dbo].[Residents];
GO