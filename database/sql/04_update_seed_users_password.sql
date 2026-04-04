/*
Topstar - Update seed users password for direct login test
Date: 2026-04-04
Password (plain text): Topstar@123
Applies to:
- admin@topstar.local
- brand@topstar.local
- koc@topstar.local
*/

SET XACT_ABORT ON;
GO

USE [TopstarDb];
GO

BEGIN TRANSACTION;
GO

DECLARE @PasswordHash NVARCHAR(512) = N'PBKDF2$100000$vFIOXCKcqzFlK18/BPIXcQ==$ydvsRa6XngROmq1HcpCR0th1IuftQ6tt4w4VawPR9sA=';

UPDATE [dbo].[Users]
SET [PasswordHash] = @PasswordHash,
    [Status] = 'Active',
    [FailedLoginCount] = 0,
    [LockoutEndAtUtc] = NULL,
    [PasswordChangedAtUtc] = SYSUTCDATETIME(),
    [UpdatedAtUtc] = SYSUTCDATETIME()
WHERE [Email] IN (N'admin@topstar.local', N'brand@topstar.local', N'koc@topstar.local');

SELECT [Email], [Status], [FailedLoginCount], [LockoutEndAtUtc]
FROM [dbo].[Users]
WHERE [Email] IN (N'admin@topstar.local', N'brand@topstar.local', N'koc@topstar.local');

COMMIT TRANSACTION;
GO
