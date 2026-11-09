SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_PROFILE_REVIEWS')
BEGIN
    INSERT INTO [dbo].[Menus]
        ([Id], [Code], [Title], [Path], [Icon], [ParentId], [OrderNo], [IsVisible], [CreatedAtUtc])
    VALUES
        (NEWID(), 'MENU_ADMIN_PROFILE_REVIEWS', N'Duyệt hồ sơ', '/Admin/Profiles', 'fa-solid fa-user-check', NULL, 3, 1, SYSUTCDATETIME());
END;

DECLARE @AdminRoleId UNIQUEIDENTIFIER =
    (SELECT TOP 1 [Id] FROM [dbo].[Roles] WHERE UPPER([Code]) = 'ADMIN');
DECLARE @ProfileReviewMenuId UNIQUEIDENTIFIER =
    (SELECT TOP 1 [Id] FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_PROFILE_REVIEWS');

IF @AdminRoleId IS NOT NULL
   AND @ProfileReviewMenuId IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM [dbo].[RoleMenus]
       WHERE [RoleId] = @AdminRoleId AND [MenuId] = @ProfileReviewMenuId)
BEGIN
    INSERT INTO [dbo].[RoleMenus] ([RoleId], [MenuId], [CreatedAtUtc])
    VALUES (@AdminRoleId, @ProfileReviewMenuId, SYSUTCDATETIME());
END;

COMMIT TRANSACTION;
