SET NOCOUNT ON;SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @now DATETIME2(7)=SYSUTCDATETIME(),@admin UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='ADMIN');
 DECLARE @groups TABLE(Code VARCHAR(80),Title NVARCHAR(100),Icon VARCHAR(100),OrderNo INT);
 INSERT @groups VALUES
 ('MENU_ADMIN_ACCOUNTS',N'Tài khoản','fa-solid fa-users',10),
 ('MENU_ADMIN_CAMPAIGN_GROUP',N'Chiến dịch','fa-solid fa-bullhorn',20),
 ('MENU_ADMIN_OPERATIONS_GROUP',N'Vận hành','fa-solid fa-gears',30),
 ('MENU_ADMIN_FINANCE_GROUP',N'Tài chính','fa-solid fa-wallet',40),
 ('MENU_ADMIN_INSIGHTS_GROUP',N'Phân tích','fa-solid fa-chart-line',50);
 INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc)
 SELECT NEWID(),g.Code,g.Title,NULL,g.Icon,NULL,g.OrderNo,1,@now FROM @groups g
 WHERE NOT EXISTS(SELECT 1 FROM dbo.Menus m WHERE m.Code=g.Code);
 UPDATE m SET m.Title=g.Title,m.Path=NULL,m.Icon=g.Icon,m.ParentId=NULL,m.OrderNo=g.OrderNo,m.IsVisible=1
 FROM dbo.Menus m JOIN @groups g ON g.Code=m.Code;
 INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc)
 SELECT @admin,m.Id,@now FROM dbo.Menus m JOIN @groups g ON g.Code=m.Code
 WHERE @admin IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus rm WHERE rm.RoleId=@admin AND rm.MenuId=m.Id);

 DECLARE @accounts UNIQUEIDENTIFIER=(SELECT Id FROM dbo.Menus WHERE Code='MENU_ADMIN_ACCOUNTS'),
 @campaign UNIQUEIDENTIFIER=(SELECT Id FROM dbo.Menus WHERE Code='MENU_ADMIN_CAMPAIGN_GROUP'),
 @operations UNIQUEIDENTIFIER=(SELECT Id FROM dbo.Menus WHERE Code='MENU_ADMIN_OPERATIONS_GROUP'),
 @finance UNIQUEIDENTIFIER=(SELECT Id FROM dbo.Menus WHERE Code='MENU_ADMIN_FINANCE_GROUP'),
 @insights UNIQUEIDENTIFIER=(SELECT Id FROM dbo.Menus WHERE Code='MENU_ADMIN_INSIGHTS_GROUP');
 UPDATE dbo.Menus SET ParentId=@accounts WHERE Code IN('MENU_ADMIN_PROFILE_REVIEWS','MENU_ADMIN_VIOLATIONS');
 UPDATE dbo.Menus SET ParentId=@campaign WHERE Code IN('MENU_ADMIN_CAMPAIGNS','MENU_ADMIN_MATCHING','MENU_ADMIN_COLLABORATIONS');
 UPDATE dbo.Menus SET ParentId=@operations WHERE Code IN('MENU_ADMIN_OPERATIONS','MENU_ADMIN_BOOKING_CALENDAR','MENU_ADMIN_DISPUTES','MENU_ADMIN_REVIEW_MODERATION','MENU_ADMIN_NOTIFICATIONS');
 UPDATE dbo.Menus SET ParentId=@finance WHERE Code IN('MENU_ADMIN_FINANCE_SETTLEMENTS','MENU_ADMIN_BILLING','MENU_ADMIN_PAYOUT_POLICY');
 UPDATE dbo.Menus SET ParentId=@insights WHERE Code IN('MENU_ADMIN_REPORTS','MENU_ADMIN_ACADEMY');
 UPDATE dbo.Menus SET Title=N'Duyệt hồ sơ',Path='/Admin/Profiles',OrderNo=1 WHERE Code='MENU_ADMIN_PROFILE_REVIEWS';
 UPDATE dbo.Menus SET Title=N'Cấu hình package KOC' WHERE Code='MENU_ADMIN_SETTINGS_PACKAGES';
 UPDATE dbo.Menus SET Title=N'Tổng quan',OrderNo=1 WHERE Code='MENU_ADMIN_DASHBOARD';
 UPDATE dbo.Menus SET Title=N'Cài đặt hệ thống',OrderNo=90 WHERE Code='MENU_ADMIN_SETTINGS';
 UPDATE dbo.Menus SET IsVisible=0
 WHERE Code='MENU_ADMIN_PERMISSIONS' OR Path IN('/Admin/Users','/Admin/Permissions');
 COMMIT;
END TRY
BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH;
