SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @now DATETIME2(7)=SYSUTCDATETIME();
 IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_ADMIN_COLLABORATIONS') INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES(NEWID(),'MENU_ADMIN_COLLABORATIONS',N'Hồ sơ hợp tác','/Admin/Collaborations','fa-solid fa-timeline',NULL,9,1,@now);
 IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_BRAND_COLLABORATIONS') INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES(NEWID(),'MENU_BRAND_COLLABORATIONS',N'Lịch sử hợp tác','/Brand/Collaborations','fa-solid fa-clock-rotate-left',NULL,9,1,@now);
 IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_KOC_COLLABORATIONS') INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES(NEWID(),'MENU_KOC_COLLABORATIONS',N'Lịch sử hợp tác','/Koc/Collaborations','fa-solid fa-clock-rotate-left',NULL,9,1,@now);
 INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc)
 SELECT r.Id,m.Id,@now FROM dbo.Roles r JOIN dbo.Menus m ON (UPPER(r.Code)='ADMIN' AND m.Code='MENU_ADMIN_COLLABORATIONS') OR (UPPER(r.Code)='BRAND' AND m.Code='MENU_BRAND_COLLABORATIONS') OR (UPPER(r.Code)='KOC' AND m.Code='MENU_KOC_COLLABORATIONS')
 WHERE NOT EXISTS(SELECT 1 FROM dbo.RoleMenus rm WHERE rm.RoleId=r.Id AND rm.MenuId=m.Id);
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
