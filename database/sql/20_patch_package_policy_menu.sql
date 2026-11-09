SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @SettingsId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_SETTINGS');
IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_SETTINGS_PACKAGES')
BEGIN
    INSERT INTO [dbo].[Menus] ([Id],[Code],[Title],[Path],[Icon],[ParentId],[OrderNo],[IsVisible],[CreatedAtUtc])
    VALUES (NEWID(),'MENU_ADMIN_SETTINGS_PACKAGES',N'Cấu hình package KOC','/Admin/Settings/PackagePolicies','fa-solid fa-box-open',@SettingsId,7,1,SYSUTCDATETIME());
END;
DECLARE @AdminRoleId UNIQUEIDENTIFIER=(SELECT TOP 1 [Id] FROM [dbo].[Roles] WHERE UPPER([Code])='ADMIN');
DECLARE @MenuId UNIQUEIDENTIFIER=(SELECT TOP 1 [Id] FROM [dbo].[Menus] WHERE [Code]='MENU_ADMIN_SETTINGS_PACKAGES');
IF @AdminRoleId IS NOT NULL AND @MenuId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM [dbo].[RoleMenus] WHERE [RoleId]=@AdminRoleId AND [MenuId]=@MenuId)
    INSERT INTO [dbo].[RoleMenus]([RoleId],[MenuId],[CreatedAtUtc]) VALUES(@AdminRoleId,@MenuId,SYSUTCDATETIME());
COMMIT TRANSACTION;
