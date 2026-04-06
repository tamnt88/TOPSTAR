/*
Topstar Seed v3 - Administrative Address sample data (GHN-ready)
Date: 2026-04-04
Notes:
- Sample only for dev/test.
- Replace/extend by full GHN master list in production sync job.
*/

SET XACT_ABORT ON;
GO
USE [TopstarDb];
GO

BEGIN TRANSACTION;
GO

DECLARE @HcmId UNIQUEIDENTIFIER;
DECLARE @HnId UNIQUEIDENTIFIER;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Provinces] WHERE [ProvinceCode] = 202)
BEGIN
    SET @HcmId = NEWID();
    INSERT INTO [dbo].[Provinces] ([Id],[ProvinceCode],[Name],[CodeName],[IsActive],[CreatedAtUtc])
    VALUES (@HcmId,202,N'Thành phố Hồ Chí Minh',N'tp-ho-chi-minh',1,SYSUTCDATETIME());
END
ELSE
BEGIN
    SELECT @HcmId = [Id] FROM [dbo].[Provinces] WHERE [ProvinceCode] = 202;
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Provinces] WHERE [ProvinceCode] = 201)
BEGIN
    SET @HnId = NEWID();
    INSERT INTO [dbo].[Provinces] ([Id],[ProvinceCode],[Name],[CodeName],[IsActive],[CreatedAtUtc])
    VALUES (@HnId,201,N'Thành phố Hà Nội',N'tp-ha-noi',1,SYSUTCDATETIME());
END
ELSE
BEGIN
    SELECT @HnId = [Id] FROM [dbo].[Provinces] WHERE [ProvinceCode] = 201;
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Wards] WHERE [WardCode] = 1003508)
BEGIN
    INSERT INTO [dbo].[Wards] ([Id],[WardCode],[ProvinceId],[Name],[CodeName],[IsActive],[CreatedAtUtc])
    VALUES (NEWID(),1003508,@HcmId,N'Phường Diên Hồng',N'phuong-dien-hong',1,SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Wards] WHERE [WardCode] = 1002501)
BEGIN
    INSERT INTO [dbo].[Wards] ([Id],[WardCode],[ProvinceId],[Name],[CodeName],[IsActive],[CreatedAtUtc])
    VALUES (NEWID(),1002501,@HcmId,N'Phường Bến Thành',N'phuong-ben-thanh',1,SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Wards] WHERE [WardCode] = 2001001)
BEGIN
    INSERT INTO [dbo].[Wards] ([Id],[WardCode],[ProvinceId],[Name],[CodeName],[IsActive],[CreatedAtUtc])
    VALUES (NEWID(),2001001,@HnId,N'Phường Cửa Nam',N'phuong-cua-nam',1,SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Wards] WHERE [WardCode] = 2002001)
BEGIN
    INSERT INTO [dbo].[Wards] ([Id],[WardCode],[ProvinceId],[Name],[CodeName],[IsActive],[CreatedAtUtc])
    VALUES (NEWID(),2002001,@HnId,N'Phường Hoàn Kiếm',N'phuong-hoan-kiem',1,SYSUTCDATETIME());
END

COMMIT TRANSACTION;
GO

