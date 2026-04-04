/*
Topstar Database Schema v2 - Auth, RBAC, Device Tracking
Date: 2026-04-04
Run after: 00_create_topstar_v1.sql
*/

SET XACT_ABORT ON;
GO

USE [TopstarDb];
GO

BEGIN TRANSACTION;
GO

-- Users auth extension
IF COL_LENGTH('dbo.Users', 'EmailConfirmed') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [EmailConfirmed] BIT NOT NULL CONSTRAINT [DF_Users_EmailConfirmed] DEFAULT 0;
END
GO

IF COL_LENGTH('dbo.Users', 'PhoneConfirmed') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [PhoneConfirmed] BIT NOT NULL CONSTRAINT [DF_Users_PhoneConfirmed] DEFAULT 0;
END
GO

IF COL_LENGTH('dbo.Users', 'LastLoginAtUtc') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [LastLoginAtUtc] DATETIME2(0) NULL;
END
GO

IF COL_LENGTH('dbo.Users', 'PasswordChangedAtUtc') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [PasswordChangedAtUtc] DATETIME2(0) NULL;
END
GO

IF COL_LENGTH('dbo.Users', 'FailedLoginCount') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [FailedLoginCount] INT NOT NULL CONSTRAINT [DF_Users_FailedLoginCount] DEFAULT 0;
END
GO

IF COL_LENGTH('dbo.Users', 'LockoutEndAtUtc') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [LockoutEndAtUtc] DATETIME2(0) NULL;
END
GO

-- Admin profile
IF OBJECT_ID('dbo.AdminProfiles', 'U') IS NULL
BEGIN
CREATE TABLE [dbo].[AdminProfiles] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [UserId] UNIQUEIDENTIFIER NOT NULL,
  [FullName] NVARCHAR(150) NOT NULL,
  [Department] NVARCHAR(100) NULL,
  [Title] NVARCHAR(100) NULL,
  [Status] NVARCHAR(30) NOT NULL DEFAULT 'Active' CHECK ([Status] IN ('Active','Inactive','Locked')),
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_AdminProfiles] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_AdminProfiles_UserId] UNIQUE ([UserId]),
  CONSTRAINT [FK_AdminProfiles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
);
END
GO

-- Permissions
IF OBJECT_ID('dbo.Permissions', 'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Permissions] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [Code] NVARCHAR(100) NOT NULL,
  [Name] NVARCHAR(200) NOT NULL,
  [Module] NVARCHAR(100) NOT NULL,
  [Description] NVARCHAR(500) NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_Permissions_Code] UNIQUE ([Code])
);
END
GO

IF OBJECT_ID('dbo.RolePermissions', 'U') IS NULL
BEGIN
CREATE TABLE [dbo].[RolePermissions] (
  [RoleId] UNIQUEIDENTIFIER NOT NULL,
  [PermissionId] UNIQUEIDENTIFIER NOT NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId],[PermissionId]),
  CONSTRAINT [FK_RolePermissions_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]),
  CONSTRAINT [FK_RolePermissions_Permissions] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permissions]([Id])
);
END
GO

IF OBJECT_ID('dbo.UserPermissions', 'U') IS NULL
BEGIN
CREATE TABLE [dbo].[UserPermissions] (
  [UserId] UNIQUEIDENTIFIER NOT NULL,
  [PermissionId] UNIQUEIDENTIFIER NOT NULL,
  [IsAllowed] BIT NOT NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_UserPermissions] PRIMARY KEY ([UserId],[PermissionId]),
  CONSTRAINT [FK_UserPermissions_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
  CONSTRAINT [FK_UserPermissions_Permissions] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permissions]([Id])
);
END
GO

-- Dynamic menus per role/user
IF OBJECT_ID('dbo.Menus', 'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Menus] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [Code] NVARCHAR(100) NOT NULL,
  [Title] NVARCHAR(150) NOT NULL,
  [Path] NVARCHAR(300) NULL,
  [Icon] NVARCHAR(100) NULL,
  [ParentId] UNIQUEIDENTIFIER NULL,
  [OrderNo] INT NOT NULL DEFAULT 0,
  [IsVisible] BIT NOT NULL DEFAULT 1,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_Menus] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_Menus_Code] UNIQUE ([Code]),
  CONSTRAINT [FK_Menus_Parent] FOREIGN KEY ([ParentId]) REFERENCES [dbo].[Menus]([Id])
);
END
GO

IF OBJECT_ID('dbo.RoleMenus', 'U') IS NULL
BEGIN
CREATE TABLE [dbo].[RoleMenus] (
  [RoleId] UNIQUEIDENTIFIER NOT NULL,
  [MenuId] UNIQUEIDENTIFIER NOT NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_RoleMenus] PRIMARY KEY ([RoleId],[MenuId]),
  CONSTRAINT [FK_RoleMenus_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]),
  CONSTRAINT [FK_RoleMenus_Menus] FOREIGN KEY ([MenuId]) REFERENCES [dbo].[Menus]([Id])
);
END
GO

IF OBJECT_ID('dbo.UserMenus', 'U') IS NULL
BEGIN
CREATE TABLE [dbo].[UserMenus] (
  [UserId] UNIQUEIDENTIFIER NOT NULL,
  [MenuId] UNIQUEIDENTIFIER NOT NULL,
  [IsVisible] BIT NOT NULL DEFAULT 1,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_UserMenus] PRIMARY KEY ([UserId],[MenuId]),
  CONSTRAINT [FK_UserMenus_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
  CONSTRAINT [FK_UserMenus_Menus] FOREIGN KEY ([MenuId]) REFERENCES [dbo].[Menus]([Id])
);
END
GO

-- Dashboard config per role/user
IF OBJECT_ID('dbo.DashboardWidgets', 'U') IS NULL
BEGIN
CREATE TABLE [dbo].[DashboardWidgets] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [Code] NVARCHAR(100) NOT NULL,
  [Title] NVARCHAR(200) NOT NULL,
  [DataSource] NVARCHAR(200) NULL,
  [RoleScope] NVARCHAR(30) NULL,
  [IsVisible] BIT NOT NULL DEFAULT 1,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_DashboardWidgets] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_DashboardWidgets_Code] UNIQUE ([Code])
);
END
GO

IF OBJECT_ID('dbo.RoleDashboardConfigs', 'U') IS NULL
BEGIN
CREATE TABLE [dbo].[RoleDashboardConfigs] (
  [RoleId] UNIQUEIDENTIFIER NOT NULL,
  [WidgetId] UNIQUEIDENTIFIER NOT NULL,
  [Position] INT NOT NULL,
  [IsEnabled] BIT NOT NULL DEFAULT 1,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_RoleDashboardConfigs] PRIMARY KEY ([RoleId],[WidgetId]),
  CONSTRAINT [FK_RoleDashboardConfigs_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]),
  CONSTRAINT [FK_RoleDashboardConfigs_Widgets] FOREIGN KEY ([WidgetId]) REFERENCES [dbo].[DashboardWidgets]([Id])
);
END
GO

IF OBJECT_ID('dbo.UserDashboardConfigs', 'U') IS NULL
BEGIN
CREATE TABLE [dbo].[UserDashboardConfigs] (
  [UserId] UNIQUEIDENTIFIER NOT NULL,
  [WidgetId] UNIQUEIDENTIFIER NOT NULL,
  [Position] INT NOT NULL,
  [IsEnabled] BIT NOT NULL DEFAULT 1,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_UserDashboardConfigs] PRIMARY KEY ([UserId],[WidgetId]),
  CONSTRAINT [FK_UserDashboardConfigs_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
  CONSTRAINT [FK_UserDashboardConfigs_Widgets] FOREIGN KEY ([WidgetId]) REFERENCES [dbo].[DashboardWidgets]([Id])
);
END
GO

-- Device tracking + auth flow tables
IF OBJECT_ID('dbo.UserDevices', 'U') IS NULL
BEGIN
CREATE TABLE [dbo].[UserDevices] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [UserId] UNIQUEIDENTIFIER NULL,
  [DeviceFingerprint] NVARCHAR(200) NOT NULL,
  [DeviceName] NVARCHAR(200) NULL,
  [DeviceType] NVARCHAR(50) NULL,
  [OsName] NVARCHAR(100) NULL,
  [OsVersion] NVARCHAR(100) NULL,
  [BrowserName] NVARCHAR(100) NULL,
  [BrowserVersion] NVARCHAR(100) NULL,
  [UserAgent] NVARCHAR(1000) NULL,
  [IpAddress] NVARCHAR(64) NULL,
  [CountryCode] NVARCHAR(5) NULL,
  [City] NVARCHAR(100) NULL,
  [IsTrusted] BIT NOT NULL DEFAULT 0,
  [LastSeenAtUtc] DATETIME2(0) NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_UserDevices] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_UserDevices_Fingerprint] UNIQUE ([DeviceFingerprint]),
  CONSTRAINT [FK_UserDevices_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
);
END
GO

IF OBJECT_ID('dbo.AuthEvents', 'U') IS NULL
BEGIN
CREATE TABLE [dbo].[AuthEvents] (
  [Id] BIGINT IDENTITY(1,1) NOT NULL,
  [UserId] UNIQUEIDENTIFIER NULL,
  [EmailOrPhone] NVARCHAR(320) NULL,
  [EventType] NVARCHAR(30) NOT NULL CHECK ([EventType] IN ('Register','Login','Logout','ForgotPassword','ResetPassword','VerifyEmail','VerifyPhone')),
  [EventStatus] NVARCHAR(20) NOT NULL CHECK ([EventStatus] IN ('Success','Failed','Blocked')),
  [FailureReason] NVARCHAR(500) NULL,
  [UserDeviceId] UNIQUEIDENTIFIER NULL,
  [IpAddress] NVARCHAR(64) NULL,
  [UserAgent] NVARCHAR(1000) NULL,
  [OccurredAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_AuthEvents] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_AuthEvents_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
  CONSTRAINT [FK_AuthEvents_UserDevices] FOREIGN KEY ([UserDeviceId]) REFERENCES [dbo].[UserDevices]([Id])
);
END
GO

IF OBJECT_ID('dbo.AuthSessions', 'U') IS NULL
BEGIN
CREATE TABLE [dbo].[AuthSessions] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [UserId] UNIQUEIDENTIFIER NOT NULL,
  [UserDeviceId] UNIQUEIDENTIFIER NULL,
  [RefreshTokenHash] NVARCHAR(512) NOT NULL,
  [IssuedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [ExpiresAtUtc] DATETIME2(0) NOT NULL,
  [RevokedAtUtc] DATETIME2(0) NULL,
  [RevokeReason] NVARCHAR(300) NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_AuthSessions] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_AuthSessions_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
  CONSTRAINT [FK_AuthSessions_UserDevices] FOREIGN KEY ([UserDeviceId]) REFERENCES [dbo].[UserDevices]([Id])
);
END
GO

IF OBJECT_ID('dbo.PasswordResetTokens', 'U') IS NULL
BEGIN
CREATE TABLE [dbo].[PasswordResetTokens] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [UserId] UNIQUEIDENTIFIER NOT NULL,
  [TokenHash] NVARCHAR(512) NOT NULL,
  [ExpiresAtUtc] DATETIME2(0) NOT NULL,
  [UsedAtUtc] DATETIME2(0) NULL,
  [RequestedByDeviceId] UNIQUEIDENTIFIER NULL,
  [RequestedIpAddress] NVARCHAR(64) NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_PasswordResetTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
  CONSTRAINT [FK_PasswordResetTokens_UserDevices] FOREIGN KEY ([RequestedByDeviceId]) REFERENCES [dbo].[UserDevices]([Id])
);
END
GO

-- Useful indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserDevices_UserId' AND object_id = OBJECT_ID('dbo.UserDevices'))
    CREATE INDEX [IX_UserDevices_UserId] ON [dbo].[UserDevices]([UserId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuthEvents_User_Occurred' AND object_id = OBJECT_ID('dbo.AuthEvents'))
    CREATE INDEX [IX_AuthEvents_User_Occurred] ON [dbo].[AuthEvents]([UserId],[OccurredAtUtc]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuthEvents_Device_Occurred' AND object_id = OBJECT_ID('dbo.AuthEvents'))
    CREATE INDEX [IX_AuthEvents_Device_Occurred] ON [dbo].[AuthEvents]([UserDeviceId],[OccurredAtUtc]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuthSessions_User_Expires' AND object_id = OBJECT_ID('dbo.AuthSessions'))
    CREATE INDEX [IX_AuthSessions_User_Expires] ON [dbo].[AuthSessions]([UserId],[ExpiresAtUtc]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PasswordResetTokens_User_Expires' AND object_id = OBJECT_ID('dbo.PasswordResetTokens'))
    CREATE INDEX [IX_PasswordResetTokens_User_Expires] ON [dbo].[PasswordResetTokens]([UserId],[ExpiresAtUtc]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RolePermissions_PermissionId' AND object_id = OBJECT_ID('dbo.RolePermissions'))
    CREATE INDEX [IX_RolePermissions_PermissionId] ON [dbo].[RolePermissions]([PermissionId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserPermissions_PermissionId' AND object_id = OBJECT_ID('dbo.UserPermissions'))
    CREATE INDEX [IX_UserPermissions_PermissionId] ON [dbo].[UserPermissions]([PermissionId]);
GO

COMMIT TRANSACTION;
GO
