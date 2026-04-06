/*
Patch v3.2 - Align Provinces/Wards schema with GHN v3 APIs
Date: 2026-04-04
APIs:
- /v3/master-data/province/all
- /v3/master-data/ward/all-by-province-id
*/
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
USE [TopstarDb];

BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Provinces', 'GhnProvinceId') IS NULL
    ALTER TABLE [dbo].[Provinces] ADD [GhnProvinceId] INT NULL;
IF COL_LENGTH('dbo.Provinces', 'ExtensionNamesJson') IS NULL
    ALTER TABLE [dbo].[Provinces] ADD [ExtensionNamesJson] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.Provinces', 'Type') IS NULL
    ALTER TABLE [dbo].[Provinces] ADD [Type] NVARCHAR(30) NULL;
IF COL_LENGTH('dbo.Provinces', 'ParentId') IS NULL
    ALTER TABLE [dbo].[Provinces] ADD [ParentId] INT NULL;
IF COL_LENGTH('dbo.Provinces', 'GhnStatus') IS NULL
    ALTER TABLE [dbo].[Provinces] ADD [GhnStatus] INT NULL;
IF COL_LENGTH('dbo.Provinces', 'UpdatedIp') IS NULL
    ALTER TABLE [dbo].[Provinces] ADD [UpdatedIp] NVARCHAR(64) NULL;
IF COL_LENGTH('dbo.Provinces', 'UpdatedEmployee') IS NULL
    ALTER TABLE [dbo].[Provinces] ADD [UpdatedEmployee] INT NULL;
IF COL_LENGTH('dbo.Provinces', 'UpdatedSource') IS NULL
    ALTER TABLE [dbo].[Provinces] ADD [UpdatedSource] NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.Provinces', 'UpdatedDate') IS NULL
    ALTER TABLE [dbo].[Provinces] ADD [UpdatedDate] DATETIME2(3) NULL;
IF COL_LENGTH('dbo.Provinces', 'CreatedIp') IS NULL
    ALTER TABLE [dbo].[Provinces] ADD [CreatedIp] NVARCHAR(64) NULL;
IF COL_LENGTH('dbo.Provinces', 'CreatedEmployee') IS NULL
    ALTER TABLE [dbo].[Provinces] ADD [CreatedEmployee] INT NULL;
IF COL_LENGTH('dbo.Provinces', 'CreatedSource') IS NULL
    ALTER TABLE [dbo].[Provinces] ADD [CreatedSource] NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.Provinces', 'CreatedDate') IS NULL
    ALTER TABLE [dbo].[Provinces] ADD [CreatedDate] DATETIME2(3) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Provinces_GhnProvinceId' AND object_id = OBJECT_ID('dbo.Provinces'))
    EXEC(N'CREATE UNIQUE INDEX [UQ_Provinces_GhnProvinceId] ON [dbo].[Provinces]([GhnProvinceId]) WHERE [GhnProvinceId] IS NOT NULL;');

IF COL_LENGTH('dbo.Wards', 'GhnWardId') IS NULL
    ALTER TABLE [dbo].[Wards] ADD [GhnWardId] INT NULL;
IF COL_LENGTH('dbo.Wards', 'ExtensionNamesJson') IS NULL
    ALTER TABLE [dbo].[Wards] ADD [ExtensionNamesJson] NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.Wards', 'Type') IS NULL
    ALTER TABLE [dbo].[Wards] ADD [Type] NVARCHAR(30) NULL;
IF COL_LENGTH('dbo.Wards', 'GhnParentProvinceId') IS NULL
    ALTER TABLE [dbo].[Wards] ADD [GhnParentProvinceId] INT NULL;
IF COL_LENGTH('dbo.Wards', 'GhnStatus') IS NULL
    ALTER TABLE [dbo].[Wards] ADD [GhnStatus] INT NULL;
IF COL_LENGTH('dbo.Wards', 'UpdatedIp') IS NULL
    ALTER TABLE [dbo].[Wards] ADD [UpdatedIp] NVARCHAR(64) NULL;
IF COL_LENGTH('dbo.Wards', 'UpdatedEmployee') IS NULL
    ALTER TABLE [dbo].[Wards] ADD [UpdatedEmployee] INT NULL;
IF COL_LENGTH('dbo.Wards', 'UpdatedSource') IS NULL
    ALTER TABLE [dbo].[Wards] ADD [UpdatedSource] NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.Wards', 'UpdatedDate') IS NULL
    ALTER TABLE [dbo].[Wards] ADD [UpdatedDate] DATETIME2(3) NULL;
IF COL_LENGTH('dbo.Wards', 'CreatedIp') IS NULL
    ALTER TABLE [dbo].[Wards] ADD [CreatedIp] NVARCHAR(64) NULL;
IF COL_LENGTH('dbo.Wards', 'CreatedEmployee') IS NULL
    ALTER TABLE [dbo].[Wards] ADD [CreatedEmployee] INT NULL;
IF COL_LENGTH('dbo.Wards', 'CreatedSource') IS NULL
    ALTER TABLE [dbo].[Wards] ADD [CreatedSource] NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.Wards', 'CreatedDate') IS NULL
    ALTER TABLE [dbo].[Wards] ADD [CreatedDate] DATETIME2(3) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Wards_GhnWardId' AND object_id = OBJECT_ID('dbo.Wards'))
    EXEC(N'CREATE UNIQUE INDEX [UQ_Wards_GhnWardId] ON [dbo].[Wards]([GhnWardId]) WHERE [GhnWardId] IS NOT NULL;');

COMMIT TRANSACTION;

