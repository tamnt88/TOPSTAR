/*
Topstar v2 seed data - Auth/RBAC/Menu/Dashboard
Run after: 02_create_topstar_v2_auth_rbac.sql
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE [TopstarDb];
GO

BEGIN TRANSACTION;
GO

DECLARE @RoleAdmin UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Roles] WHERE [Code] = 'ADMIN');
DECLARE @RoleBrand UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Roles] WHERE [Code] = 'BRAND');
DECLARE @RoleKoc UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Roles] WHERE [Code] = 'KOC');

-- Permissions
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Code] = 'AUTH_LOGIN')
INSERT INTO [dbo].[Permissions] ([Code],[Name],[Module],[Description]) VALUES
('AUTH_LOGIN',N'Đăng nhập hệ thống','AUTH',N'Cho phép đăng nhập');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Code] = 'AUTH_REGISTER')
INSERT INTO [dbo].[Permissions] ([Code],[Name],[Module],[Description]) VALUES
('AUTH_REGISTER',N'Đăng ký tài khoản','AUTH',N'Cho phép đăng ký tài khoản');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Code] = 'AUTH_FORGOT_PASSWORD')
INSERT INTO [dbo].[Permissions] ([Code],[Name],[Module],[Description]) VALUES
('AUTH_FORGOT_PASSWORD',N'Quên mật khẩu','AUTH',N'Cho phép yêu cầu đặt lại mật khẩu');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Code] = 'DASHBOARD_ADMIN_VIEW')
INSERT INTO [dbo].[Permissions] ([Code],[Name],[Module],[Description]) VALUES
('DASHBOARD_ADMIN_VIEW',N'Xem dashboard Admin','DASHBOARD',N'Xem dashboard quản trị');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Code] = 'DASHBOARD_BRAND_VIEW')
INSERT INTO [dbo].[Permissions] ([Code],[Name],[Module],[Description]) VALUES
('DASHBOARD_BRAND_VIEW',N'Xem dashboard Brand','DASHBOARD',N'Xem dashboard Brand');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Code] = 'DASHBOARD_KOC_VIEW')
INSERT INTO [dbo].[Permissions] ([Code],[Name],[Module],[Description]) VALUES
('DASHBOARD_KOC_VIEW',N'Xem dashboard KOC','DASHBOARD',N'Xem dashboard KOC');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Code] = 'ADMIN_ACCOUNT_MANAGE')
INSERT INTO [dbo].[Permissions] ([Code],[Name],[Module],[Description]) VALUES
('ADMIN_ACCOUNT_MANAGE',N'Quản lý tài khoản','ADMIN',N'Tạo/sửa/khóa tài khoản');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Code] = 'ADMIN_PERMISSION_MANAGE')
INSERT INTO [dbo].[Permissions] ([Code],[Name],[Module],[Description]) VALUES
('ADMIN_PERMISSION_MANAGE',N'Phân quyền tài khoản','ADMIN',N'Gán quyền theo role và user');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Code] = 'CAMPAIGN_MANAGE')
INSERT INTO [dbo].[Permissions] ([Code],[Name],[Module],[Description]) VALUES
('CAMPAIGN_MANAGE',N'Quản lý chiến dịch','CAMPAIGN',N'Tạo và vận hành chiến dịch');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissions] WHERE [Code] = 'REPORT_VIEW')
INSERT INTO [dbo].[Permissions] ([Code],[Name],[Module],[Description]) VALUES
('REPORT_VIEW',N'Xem báo cáo','REPORT',N'Xem báo cáo theo vai trò');

-- Role permissions
INSERT INTO [dbo].[RolePermissions] ([RoleId],[PermissionId])
SELECT @RoleAdmin, p.[Id]
FROM [dbo].[Permissions] p
WHERE @RoleAdmin IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM [dbo].[RolePermissions] rp WHERE rp.[RoleId] = @RoleAdmin AND rp.[PermissionId] = p.[Id]
);

INSERT INTO [dbo].[RolePermissions] ([RoleId],[PermissionId])
SELECT @RoleBrand, p.[Id]
FROM [dbo].[Permissions] p
WHERE @RoleBrand IS NOT NULL
  AND p.[Code] IN ('AUTH_LOGIN','AUTH_REGISTER','AUTH_FORGOT_PASSWORD','DASHBOARD_BRAND_VIEW','CAMPAIGN_MANAGE','REPORT_VIEW')
  AND NOT EXISTS (
    SELECT 1 FROM [dbo].[RolePermissions] rp WHERE rp.[RoleId] = @RoleBrand AND rp.[PermissionId] = p.[Id]
);

INSERT INTO [dbo].[RolePermissions] ([RoleId],[PermissionId])
SELECT @RoleKoc, p.[Id]
FROM [dbo].[Permissions] p
WHERE @RoleKoc IS NOT NULL
  AND p.[Code] IN ('AUTH_LOGIN','AUTH_REGISTER','AUTH_FORGOT_PASSWORD','DASHBOARD_KOC_VIEW','REPORT_VIEW')
  AND NOT EXISTS (
    SELECT 1 FROM [dbo].[RolePermissions] rp WHERE rp.[RoleId] = @RoleKoc AND rp.[PermissionId] = p.[Id]
);

-- Menus
IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_DASHBOARD')
INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[OrderNo]) VALUES
('MENU_ADMIN_DASHBOARD',N'Tổng quan Admin','/Admin/Dashboard','fa-solid fa-chart-pie',1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_ACCOUNTS')
INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[OrderNo]) VALUES
('MENU_ADMIN_ACCOUNTS',N'Quản lý tài khoản','/Admin/Users','fa-solid fa-users-gear',2);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_PERMISSIONS')
INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[OrderNo]) VALUES
('MENU_ADMIN_PERMISSIONS',N'Phân quyền','/Admin/Permissions','fa-solid fa-shield-halved',3);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_BRAND_DASHBOARD')
INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[OrderNo]) VALUES
('MENU_BRAND_DASHBOARD',N'Tổng quan Brand','/Brand/Dashboard','fa-solid fa-gauge-high',1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_BRAND_CAMPAIGNS')
INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[OrderNo]) VALUES
('MENU_BRAND_CAMPAIGNS',N'Chiến dịch','/Brand/Campaigns','fa-solid fa-bullhorn',2);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_BRAND_REPORTS')
INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[OrderNo]) VALUES
('MENU_BRAND_REPORTS',N'Báo cáo','/Brand/Reports','fa-solid fa-chart-column',3);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_KOC_DASHBOARD')
INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[OrderNo]) VALUES
('MENU_KOC_DASHBOARD',N'Tổng quan KOC','/Koc/Dashboard','fa-solid fa-gauge-high',1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_KOC_JOBS')
INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[OrderNo]) VALUES
('MENU_KOC_JOBS',N'Job của tôi','/Koc/Jobs','fa-solid fa-briefcase',2);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_KOC_PROFILE')
INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[OrderNo]) VALUES
('MENU_KOC_PROFILE',N'Hồ sơ KOC','/Koc/Profile','fa-solid fa-id-card',3);

INSERT INTO [dbo].[RoleMenus] ([RoleId],[MenuId])
SELECT @RoleAdmin, m.[Id]
FROM [dbo].[Menus] m
WHERE @RoleAdmin IS NOT NULL AND m.[Code] LIKE 'MENU_ADMIN_%'
AND NOT EXISTS (SELECT 1 FROM [dbo].[RoleMenus] rm WHERE rm.[RoleId] = @RoleAdmin AND rm.[MenuId] = m.[Id]);

INSERT INTO [dbo].[RoleMenus] ([RoleId],[MenuId])
SELECT @RoleBrand, m.[Id]
FROM [dbo].[Menus] m
WHERE @RoleBrand IS NOT NULL AND m.[Code] LIKE 'MENU_BRAND_%'
AND NOT EXISTS (SELECT 1 FROM [dbo].[RoleMenus] rm WHERE rm.[RoleId] = @RoleBrand AND rm.[MenuId] = m.[Id]);

INSERT INTO [dbo].[RoleMenus] ([RoleId],[MenuId])
SELECT @RoleKoc, m.[Id]
FROM [dbo].[Menus] m
WHERE @RoleKoc IS NOT NULL AND m.[Code] LIKE 'MENU_KOC_%'
AND NOT EXISTS (SELECT 1 FROM [dbo].[RoleMenus] rm WHERE rm.[RoleId] = @RoleKoc AND rm.[MenuId] = m.[Id]);

-- Dashboard widgets
IF NOT EXISTS (SELECT 1 FROM [dbo].[DashboardWidgets] WHERE [Code] = 'W_ADMIN_KPI')
INSERT INTO [dbo].[DashboardWidgets] ([Code],[Title],[DataSource],[RoleScope]) VALUES
('W_ADMIN_KPI',N'KPI tổng quan Admin','vw_admin_kpi','ADMIN');

IF NOT EXISTS (SELECT 1 FROM [dbo].[DashboardWidgets] WHERE [Code] = 'W_BRAND_CAMPAIGN_KPI')
INSERT INTO [dbo].[DashboardWidgets] ([Code],[Title],[DataSource],[RoleScope]) VALUES
('W_BRAND_CAMPAIGN_KPI',N'KPI chiến dịch Brand','vw_brand_campaign_kpi','BRAND');

IF NOT EXISTS (SELECT 1 FROM [dbo].[DashboardWidgets] WHERE [Code] = 'W_KOC_PERFORMANCE')
INSERT INTO [dbo].[DashboardWidgets] ([Code],[Title],[DataSource],[RoleScope]) VALUES
('W_KOC_PERFORMANCE',N'Hiệu suất KOC','vw_koc_performance','KOC');

INSERT INTO [dbo].[RoleDashboardConfigs] ([RoleId],[WidgetId],[Position],[IsEnabled])
SELECT @RoleAdmin, w.[Id], 1, 1
FROM [dbo].[DashboardWidgets] w
WHERE @RoleAdmin IS NOT NULL AND w.[Code] = 'W_ADMIN_KPI'
AND NOT EXISTS (SELECT 1 FROM [dbo].[RoleDashboardConfigs] c WHERE c.[RoleId] = @RoleAdmin AND c.[WidgetId] = w.[Id]);

INSERT INTO [dbo].[RoleDashboardConfigs] ([RoleId],[WidgetId],[Position],[IsEnabled])
SELECT @RoleBrand, w.[Id], 1, 1
FROM [dbo].[DashboardWidgets] w
WHERE @RoleBrand IS NOT NULL AND w.[Code] = 'W_BRAND_CAMPAIGN_KPI'
AND NOT EXISTS (SELECT 1 FROM [dbo].[RoleDashboardConfigs] c WHERE c.[RoleId] = @RoleBrand AND c.[WidgetId] = w.[Id]);

INSERT INTO [dbo].[RoleDashboardConfigs] ([RoleId],[WidgetId],[Position],[IsEnabled])
SELECT @RoleKoc, w.[Id], 1, 1
FROM [dbo].[DashboardWidgets] w
WHERE @RoleKoc IS NOT NULL AND w.[Code] = 'W_KOC_PERFORMANCE'
AND NOT EXISTS (SELECT 1 FROM [dbo].[RoleDashboardConfigs] c WHERE c.[RoleId] = @RoleKoc AND c.[WidgetId] = w.[Id]);

-- Admin profile for existing admin user
DECLARE @AdminUser UNIQUEIDENTIFIER = (
    SELECT TOP 1 u.[Id]
    FROM [dbo].[Users] u
    INNER JOIN [dbo].[UserRoles] ur ON ur.[UserId] = u.[Id]
    INNER JOIN [dbo].[Roles] r ON r.[Id] = ur.[RoleId]
    WHERE r.[Code] = 'ADMIN'
);

IF @AdminUser IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM [dbo].[AdminProfiles] WHERE [UserId] = @AdminUser)
BEGIN
    INSERT INTO [dbo].[AdminProfiles] ([UserId],[FullName],[Department],[Title],[Status])
    VALUES (@AdminUser, N'System Admin', N'Operation', N'Platform Administrator', 'Active');
END

-- Device + auth event sample for each user
INSERT INTO [dbo].[UserDevices] ([UserId],[DeviceFingerprint],[DeviceName],[DeviceType],[OsName],[BrowserName],[UserAgent],[IpAddress],[CountryCode],[City],[IsTrusted],[LastSeenAtUtc])
SELECT u.[Id], CONCAT('seed-', CONVERT(NVARCHAR(36), u.[Id])), N'Demo Device', N'Web', N'Windows 11', N'Chrome', N'Mozilla/5.0 Demo', '127.0.0.1', 'VN', N'Ho Chi Minh', 1, SYSUTCDATETIME()
FROM [dbo].[Users] u
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[UserDevices] d WHERE d.[DeviceFingerprint] = CONCAT('seed-', CONVERT(NVARCHAR(36), u.[Id])));

INSERT INTO [dbo].[AuthEvents] ([UserId],[EmailOrPhone],[EventType],[EventStatus],[UserDeviceId],[IpAddress],[UserAgent],[OccurredAtUtc])
SELECT u.[Id], u.[Email], 'Login', 'Success', d.[Id], '127.0.0.1', N'Mozilla/5.0 Demo', SYSUTCDATETIME()
FROM [dbo].[Users] u
INNER JOIN [dbo].[UserDevices] d ON d.[DeviceFingerprint] = CONCAT('seed-', CONVERT(NVARCHAR(36), u.[Id]))
WHERE NOT EXISTS (
  SELECT 1 FROM [dbo].[AuthEvents] e WHERE e.[UserId] = u.[Id] AND e.[EventType] = 'Login' AND e.[EventStatus] = 'Success'
);

COMMIT TRANSACTION;
GO
