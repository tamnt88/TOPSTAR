SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_MATCHING')
BEGIN
    INSERT INTO [dbo].[Menus]
        ([Id], [Code], [Title], [Path], [Icon], [ParentId], [OrderNo], [IsVisible], [CreatedAtUtc])
    VALUES
        (NEWID(), 'MENU_ADMIN_MATCHING', N'Matching KOC', '/Admin/Matching', 'fa-solid fa-people-arrows', NULL, 5, 1, SYSUTCDATETIME());
END;

DECLARE @AdminRoleId UNIQUEIDENTIFIER =
    (SELECT TOP 1 [Id] FROM [dbo].[Roles] WHERE UPPER([Code]) = 'ADMIN');
DECLARE @MatchingMenuId UNIQUEIDENTIFIER =
    (SELECT TOP 1 [Id] FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_MATCHING');

IF @AdminRoleId IS NOT NULL
   AND @MatchingMenuId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[RoleMenus] WHERE [RoleId] = @AdminRoleId AND [MenuId] = @MatchingMenuId)
BEGIN
    INSERT INTO [dbo].[RoleMenus] ([RoleId], [MenuId], [CreatedAtUtc])
    VALUES (@AdminRoleId, @MatchingMenuId, SYSUTCDATETIME());
END;

COMMIT TRANSACTION;
