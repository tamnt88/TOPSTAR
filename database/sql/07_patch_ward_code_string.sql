/*
Patch v3.1 - GHN ward code supports alphanumeric values
Date: 2026-04-04
*/
SET XACT_ABORT ON;
GO
USE [TopstarDb];
GO

BEGIN TRANSACTION;
GO

IF COL_LENGTH('dbo.Wards', 'WardCode') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Wards] DROP CONSTRAINT [UQ_Wards_WardCode];
    ALTER TABLE [dbo].[Wards] ALTER COLUMN [WardCode] NVARCHAR(50) NOT NULL;
    ALTER TABLE [dbo].[Wards] ADD CONSTRAINT [UQ_Wards_WardCode] UNIQUE ([WardCode]);
END
GO

IF COL_LENGTH('dbo.BrandProfiles', 'WardCodeV2') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[BrandProfiles] ALTER COLUMN [WardCodeV2] NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH('dbo.KocProfiles', 'WardCodeV2') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[KocProfiles] ALTER COLUMN [WardCodeV2] NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH('dbo.Shipments', 'WardCodeV2') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Shipments] ALTER COLUMN [WardCodeV2] NVARCHAR(50) NULL;
END
GO

COMMIT TRANSACTION;
GO

