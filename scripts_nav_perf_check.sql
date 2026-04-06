SET NOCOUNT ON;
DECLARE @RoleCode NVARCHAR(50) = 'ADMIN';
DECLARE @RoleId UNIQUEIDENTIFIER;

PRINT '=== ESTIMATED PLAN: Q1 Roles by Code ===';
SET SHOWPLAN_TEXT ON;
GO
SELECT TOP (1) Id
FROM dbo.Roles WITH (NOLOCK)
WHERE Code = 'ADMIN';
GO
SET SHOWPLAN_TEXT OFF;
GO

SET STATISTICS IO ON;
SET STATISTICS TIME ON;

PRINT '=== ACTUAL RUN: Q1 Roles by Code ===';
SELECT @RoleId = Id
FROM dbo.Roles
WHERE Code = @RoleCode;

PRINT '=== ACTUAL RUN: Q2 RoleMenus by RoleId ===';
SELECT MenuId
FROM dbo.RoleMenus
WHERE RoleId = @RoleId;

PRINT '=== ACTUAL RUN: Q3 Menus by IsVisible + MenuIds ===';
SELECT m.Id, m.Title, m.Path, m.Icon, m.ParentId, m.OrderNo
FROM dbo.Menus m
WHERE m.IsVisible = 1
  AND m.Id IN (SELECT rm.MenuId FROM dbo.RoleMenus rm WHERE rm.RoleId = @RoleId)
ORDER BY m.OrderNo, m.Title;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
