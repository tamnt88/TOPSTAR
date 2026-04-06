/*
Patch v12 - Lưu tuổi con cái (input thực tế)
Date: 2026-04-04
*/

SET XACT_ABORT ON;
GO

USE [TopstarDb];
GO

BEGIN TRANSACTION;
GO

IF COL_LENGTH('dbo.KocChildren', 'AgeYears') IS NULL
BEGIN
    ALTER TABLE [dbo].[KocChildren] ADD [AgeYears] INT NULL;
END
GO

COMMIT TRANSACTION;
GO

