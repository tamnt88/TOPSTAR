/*
Topstar Database Schema v3 - Administrative Address (GHN-ready)
Date: 2026-04-04
Notes:
- Add master tables Provinces/Wards.
- Add address fields for Brand/KOC profile and Shipment snapshot.
- Keep backward-compatible: new columns are nullable for old data.
*/

SET XACT_ABORT ON;
GO
USE [TopstarDb];
GO

BEGIN TRANSACTION;
GO

IF OBJECT_ID(N'[dbo].[Provinces]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Provinces] (
      [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
      [ProvinceCode] INT NOT NULL,
      [Name] NVARCHAR(150) NOT NULL,
      [CodeName] NVARCHAR(150) NULL,
      [IsActive] BIT NOT NULL DEFAULT 1,
      [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
      [UpdatedAtUtc] DATETIME2(0) NULL,
      CONSTRAINT [PK_Provinces] PRIMARY KEY ([Id]),
      CONSTRAINT [UQ_Provinces_ProvinceCode] UNIQUE ([ProvinceCode])
    );
END
GO

IF OBJECT_ID(N'[dbo].[Wards]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Wards] (
      [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
      [WardCode] NVARCHAR(50) NOT NULL,
      [ProvinceId] UNIQUEIDENTIFIER NOT NULL,
      [Name] NVARCHAR(150) NOT NULL,
      [CodeName] NVARCHAR(150) NULL,
      [IsActive] BIT NOT NULL DEFAULT 1,
      [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
      [UpdatedAtUtc] DATETIME2(0) NULL,
      CONSTRAINT [PK_Wards] PRIMARY KEY ([Id]),
      CONSTRAINT [UQ_Wards_WardCode] UNIQUE ([WardCode]),
      CONSTRAINT [FK_Wards_Provinces] FOREIGN KEY ([ProvinceId]) REFERENCES [dbo].[Provinces]([Id])
    );
END
GO

IF COL_LENGTH('dbo.BrandProfiles', 'StreetAddress') IS NULL
    ALTER TABLE [dbo].[BrandProfiles] ADD [StreetAddress] NVARCHAR(300) NULL;
IF COL_LENGTH('dbo.BrandProfiles', 'WardId') IS NULL
    ALTER TABLE [dbo].[BrandProfiles] ADD [WardId] UNIQUEIDENTIFIER NULL;
IF COL_LENGTH('dbo.BrandProfiles', 'ProvinceId') IS NULL
    ALTER TABLE [dbo].[BrandProfiles] ADD [ProvinceId] UNIQUEIDENTIFIER NULL;
IF COL_LENGTH('dbo.BrandProfiles', 'AddressV2') IS NULL
    ALTER TABLE [dbo].[BrandProfiles] ADD [AddressV2] NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.BrandProfiles', 'WardCodeV2') IS NULL
    ALTER TABLE [dbo].[BrandProfiles] ADD [WardCodeV2] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.BrandProfiles', 'IsNewAddress') IS NULL
    ALTER TABLE [dbo].[BrandProfiles] ADD [IsNewAddress] BIT NOT NULL CONSTRAINT [DF_BrandProfiles_IsNewAddress] DEFAULT 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BrandProfiles_Wards')
    ALTER TABLE [dbo].[BrandProfiles] ADD CONSTRAINT [FK_BrandProfiles_Wards] FOREIGN KEY ([WardId]) REFERENCES [dbo].[Wards]([Id]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BrandProfiles_Provinces')
    ALTER TABLE [dbo].[BrandProfiles] ADD CONSTRAINT [FK_BrandProfiles_Provinces] FOREIGN KEY ([ProvinceId]) REFERENCES [dbo].[Provinces]([Id]);
GO

IF COL_LENGTH('dbo.KocProfiles', 'StreetAddress') IS NULL
    ALTER TABLE [dbo].[KocProfiles] ADD [StreetAddress] NVARCHAR(300) NULL;
IF COL_LENGTH('dbo.KocProfiles', 'WardId') IS NULL
    ALTER TABLE [dbo].[KocProfiles] ADD [WardId] UNIQUEIDENTIFIER NULL;
IF COL_LENGTH('dbo.KocProfiles', 'ProvinceId') IS NULL
    ALTER TABLE [dbo].[KocProfiles] ADD [ProvinceId] UNIQUEIDENTIFIER NULL;
IF COL_LENGTH('dbo.KocProfiles', 'AddressV2') IS NULL
    ALTER TABLE [dbo].[KocProfiles] ADD [AddressV2] NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.KocProfiles', 'WardCodeV2') IS NULL
    ALTER TABLE [dbo].[KocProfiles] ADD [WardCodeV2] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.KocProfiles', 'IsNewAddress') IS NULL
    ALTER TABLE [dbo].[KocProfiles] ADD [IsNewAddress] BIT NOT NULL CONSTRAINT [DF_KocProfiles_IsNewAddress] DEFAULT 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_KocProfiles_Wards')
    ALTER TABLE [dbo].[KocProfiles] ADD CONSTRAINT [FK_KocProfiles_Wards] FOREIGN KEY ([WardId]) REFERENCES [dbo].[Wards]([Id]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_KocProfiles_Provinces')
    ALTER TABLE [dbo].[KocProfiles] ADD CONSTRAINT [FK_KocProfiles_Provinces] FOREIGN KEY ([ProvinceId]) REFERENCES [dbo].[Provinces]([Id]);
GO

IF COL_LENGTH('dbo.Shipments', 'ReceiverName') IS NULL
    ALTER TABLE [dbo].[Shipments] ADD [ReceiverName] NVARCHAR(150) NULL;
IF COL_LENGTH('dbo.Shipments', 'ReceiverPhone') IS NULL
    ALTER TABLE [dbo].[Shipments] ADD [ReceiverPhone] NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.Shipments', 'StreetAddress') IS NULL
    ALTER TABLE [dbo].[Shipments] ADD [StreetAddress] NVARCHAR(300) NULL;
IF COL_LENGTH('dbo.Shipments', 'WardId') IS NULL
    ALTER TABLE [dbo].[Shipments] ADD [WardId] UNIQUEIDENTIFIER NULL;
IF COL_LENGTH('dbo.Shipments', 'ProvinceId') IS NULL
    ALTER TABLE [dbo].[Shipments] ADD [ProvinceId] UNIQUEIDENTIFIER NULL;
IF COL_LENGTH('dbo.Shipments', 'AddressV2') IS NULL
    ALTER TABLE [dbo].[Shipments] ADD [AddressV2] NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.Shipments', 'WardCodeV2') IS NULL
    ALTER TABLE [dbo].[Shipments] ADD [WardCodeV2] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.Shipments', 'IsNewAddress') IS NULL
    ALTER TABLE [dbo].[Shipments] ADD [IsNewAddress] BIT NOT NULL CONSTRAINT [DF_Shipments_IsNewAddress] DEFAULT 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Shipments_Wards')
    ALTER TABLE [dbo].[Shipments] ADD CONSTRAINT [FK_Shipments_Wards] FOREIGN KEY ([WardId]) REFERENCES [dbo].[Wards]([Id]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Shipments_Provinces')
    ALTER TABLE [dbo].[Shipments] ADD CONSTRAINT [FK_Shipments_Provinces] FOREIGN KEY ([ProvinceId]) REFERENCES [dbo].[Provinces]([Id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Wards_ProvinceId')
    CREATE INDEX [IX_Wards_ProvinceId] ON [dbo].[Wards]([ProvinceId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BrandProfiles_WardId')
    CREATE INDEX [IX_BrandProfiles_WardId] ON [dbo].[BrandProfiles]([WardId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_KocProfiles_WardId')
    CREATE INDEX [IX_KocProfiles_WardId] ON [dbo].[KocProfiles]([WardId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Shipments_WardId')
    CREATE INDEX [IX_Shipments_WardId] ON [dbo].[Shipments]([WardId]);
GO

COMMIT TRANSACTION;
GO
