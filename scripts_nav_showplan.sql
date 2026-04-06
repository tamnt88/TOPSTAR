SET SHOWPLAN_TEXT ON;
GO
SELECT TOP (1) Id FROM dbo.Roles WHERE Code = 'ADMIN';
GO
SELECT rm.MenuId FROM dbo.RoleMenus rm WHERE rm.RoleId = (SELECT TOP (1) Id FROM dbo.Roles WHERE Code = 'ADMIN');
GO
SELECT m.Id, m.Title, m.Path, m.Icon, m.ParentId, m.OrderNo
FROM dbo.Menus m
WHERE m.IsVisible = 1
  AND m.Id IN (SELECT rm.MenuId FROM dbo.RoleMenus rm WHERE rm.RoleId = (SELECT TOP (1) Id FROM dbo.Roles WHERE Code = 'ADMIN'))
ORDER BY m.OrderNo, m.Title;
GO
SET SHOWPLAN_TEXT OFF;
GO
