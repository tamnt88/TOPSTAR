SET NOCOUNT ON;
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

DECLARE @RoleCode NVARCHAR(50) = 'ADMIN';
DECLARE @RoleId UNIQUEIDENTIFIER;

SELECT @RoleId = Id
FROM dbo.Roles
WHERE Code = @RoleCode;

SELECT MenuId
FROM dbo.RoleMenus
WHERE RoleId = @RoleId;

SELECT m.Id, m.Title, m.Path, m.Icon, m.ParentId, m.OrderNo
FROM dbo.Menus m
WHERE m.IsVisible = 1
  AND m.Id IN (SELECT rm.MenuId FROM dbo.RoleMenus rm WHERE rm.RoleId = @RoleId)
ORDER BY m.OrderNo, m.Title;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
