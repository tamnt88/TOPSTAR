/*
Patch v9 - Add Users.Gender for unified account profile
Date: 2026-04-04
Run after: 02_create_topstar_v2_auth_rbac.sql
*/

SET XACT_ABORT ON;
GO

USE [TopstarDb];
GO

BEGIN TRANSACTION;
GO

IF COL_LENGTH('dbo.Users', 'Gender') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [Gender] NVARCHAR(20) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Users_Gender'
      AND parent_object_id = OBJECT_ID('dbo.Users')
)
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD CONSTRAINT [CK_Users_Gender]
    CHECK ([Gender] IS NULL OR [Gender] IN ('Male','Female','Other'));
END
GO

COMMIT TRANSACTION;
GO

