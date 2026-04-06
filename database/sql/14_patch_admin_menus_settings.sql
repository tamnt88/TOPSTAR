SET NOCOUNT ON;
GO

USE [TopstarDb];
GO

DECLARE @RoleAdmin UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Roles] WHERE [Code] = 'ADMIN');

-- Parent menu: Cài đặt
IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_SETTINGS')
BEGIN
    INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[ParentId],[OrderNo],[IsVisible],[CreatedAtUtc])
    VALUES ('MENU_ADMIN_SETTINGS', N'Cài đặt', NULL, 'fa-solid fa-gear', NULL, 90, 1, SYSUTCDATETIME());
END

DECLARE @SettingsId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_SETTINGS');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_SETTINGS_EMAIL')
BEGIN
    INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[ParentId],[OrderNo],[IsVisible],[CreatedAtUtc])
    VALUES ('MENU_ADMIN_SETTINGS_EMAIL', N'Tài khoản gửi & nhận email', '/Admin/Settings/EmailAccounts', 'fa-regular fa-envelope', @SettingsId, 1, 1, SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_SETTINGS_SHIPPING')
BEGIN
    INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[ParentId],[OrderNo],[IsVisible],[CreatedAtUtc])
    VALUES ('MENU_ADMIN_SETTINGS_SHIPPING', N'Cấu hình giao nhận', '/Admin/Settings/Shipping', 'fa-solid fa-truck', @SettingsId, 2, 1, SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_SETTINGS_WEBSITE')
BEGIN
    INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[ParentId],[OrderNo],[IsVisible],[CreatedAtUtc])
    VALUES ('MENU_ADMIN_SETTINGS_WEBSITE', N'Cấu hình thông tin website', '/Admin/Settings/WebsiteInfo', 'fa-solid fa-globe', @SettingsId, 3, 1, SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_SETTINGS_BRANDING')
BEGIN
    INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[ParentId],[OrderNo],[IsVisible],[CreatedAtUtc])
    VALUES ('MENU_ADMIN_SETTINGS_BRANDING', N'Cấu hình logo & fav', '/Admin/Settings/Branding', 'fa-regular fa-image', @SettingsId, 4, 1, SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_SETTINGS_SEO')
BEGIN
    INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[ParentId],[OrderNo],[IsVisible],[CreatedAtUtc])
    VALUES ('MENU_ADMIN_SETTINGS_SEO', N'Cấu hình SEO & Social trang public', '/Admin/Settings/SeoSocial', 'fa-solid fa-share-nodes', @SettingsId, 5, 1, SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_SETTINGS_MENUS')
BEGIN
    INSERT INTO [dbo].[Menus] ([Code],[Title],[Path],[Icon],[ParentId],[OrderNo],[IsVisible],[CreatedAtUtc])
    VALUES ('MENU_ADMIN_SETTINGS_MENUS', N'CRUD quản lý menu Admin/KOC/Brand', '/Admin/Settings/MenuManagement', 'fa-solid fa-bars-staggered', @SettingsId, 6, 1, SYSUTCDATETIME());
END

-- Ensure admin role can see all admin menus
IF @RoleAdmin IS NOT NULL
BEGIN
    INSERT INTO [dbo].[RoleMenus] ([RoleId],[MenuId],[CreatedAtUtc])
    SELECT @RoleAdmin, m.[Id], SYSUTCDATETIME()
    FROM [dbo].[Menus] m
    WHERE m.[Code] LIKE 'MENU_ADMIN_%'
      AND NOT EXISTS (
        SELECT 1
        FROM [dbo].[RoleMenus] rm
        WHERE rm.[RoleId] = @RoleAdmin
          AND rm.[MenuId] = m.[Id]
      );
END
GO
