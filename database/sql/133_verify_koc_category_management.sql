SET NOCOUNT ON;

SELECT
    COL_LENGTH('dbo.KocCategories','SortOrder') AS SortOrderColumnBytes,
    COL_LENGTH('dbo.KocCategories','IsActive') AS IsActiveColumnBytes,
    OBJECT_ID('dbo.KocCategoryHistories','U') AS HistoryTableId;

SELECT Id, Code, Name, SortOrder, IsActive, CreatedAtUtc, UpdatedAtUtc
FROM dbo.KocCategories
ORDER BY SortOrder, Name;

SELECT menu.Code, menu.Title, menu.Path, menu.IsVisible, role.Code AS RoleCode
FROM dbo.Menus menu
LEFT JOIN dbo.RoleMenus roleMenu ON roleMenu.MenuId=menu.Id
LEFT JOIN dbo.Roles role ON role.Id=roleMenu.RoleId
WHERE menu.Code='MENU_ADMIN_SETTINGS_KOC_CATEGORIES';

SELECT TOP (20) Id, KocCategoryId, ActionName, Reason, ActorUserId, CreatedAtUtc
FROM dbo.KocCategoryHistories
ORDER BY Id DESC;
