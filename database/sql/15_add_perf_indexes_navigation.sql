-- 15_add_perf_indexes_navigation.sql
-- T?i uu truy v?n menu theo role và hi?n th? trên header/dashboard.

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Roles_Code' AND object_id = OBJECT_ID('dbo.Roles')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Roles_Code
        ON dbo.Roles(Code);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RoleMenus_RoleId_MenuId' AND object_id = OBJECT_ID('dbo.RoleMenus')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_RoleMenus_RoleId_MenuId
        ON dbo.RoleMenus(RoleId, MenuId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Menus_IsVisible_OrderNo' AND object_id = OBJECT_ID('dbo.Menus')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Menus_IsVisible_OrderNo
        ON dbo.Menus(IsVisible, OrderNo)
        INCLUDE (Id, Title, Path, Icon, ParentId, Code);
END
GO
