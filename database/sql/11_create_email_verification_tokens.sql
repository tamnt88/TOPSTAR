/*
Patch v11 - Email verification tokens
Date: 2026-04-04
*/

SET XACT_ABORT ON;
GO

USE [TopstarDb];
GO

BEGIN TRANSACTION;
GO

IF OBJECT_ID('dbo.EmailVerificationTokens', 'U') IS NULL
BEGIN
CREATE TABLE [dbo].[EmailVerificationTokens] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [UserId] UNIQUEIDENTIFIER NOT NULL,
  [TokenHash] NVARCHAR(256) NOT NULL,
  [ExpiresAtUtc] DATETIME2(0) NOT NULL,
  [UsedAtUtc] DATETIME2(0) NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_EmailVerificationTokens] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_EmailVerificationTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
);
CREATE UNIQUE INDEX [IX_EmailVerificationTokens_TokenHash] ON [dbo].[EmailVerificationTokens]([TokenHash]);
END
GO

COMMIT TRANSACTION;
GO

