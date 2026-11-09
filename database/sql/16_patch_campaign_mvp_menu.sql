SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_CAMPAIGNS')
BEGIN
    INSERT INTO [dbo].[Menus]
        ([Id], [Code], [Title], [Path], [Icon], [ParentId], [OrderNo], [IsVisible], [CreatedAtUtc])
    VALUES
        (NEWID(), 'MENU_ADMIN_CAMPAIGNS', N'Duyệt chiến dịch', '/Admin/Campaigns', 'fa-solid fa-bullhorn', NULL, 4, 1, SYSUTCDATETIME());
END;

DECLARE @AdminRoleId UNIQUEIDENTIFIER =
    (SELECT TOP 1 [Id] FROM [dbo].[Roles] WHERE UPPER([Code]) = 'ADMIN');
DECLARE @CampaignMenuId UNIQUEIDENTIFIER =
    (SELECT TOP 1 [Id] FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_CAMPAIGNS');

IF @AdminRoleId IS NOT NULL
   AND @CampaignMenuId IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM [dbo].[RoleMenus]
       WHERE [RoleId] = @AdminRoleId AND [MenuId] = @CampaignMenuId)
BEGIN
    INSERT INTO [dbo].[RoleMenus]
        ([RoleId], [MenuId], [CreatedAtUtc])
    VALUES
        (@AdminRoleId, @CampaignMenuId, SYSUTCDATETIME());
END;

COMMIT TRANSACTION;
