SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @now DATETIME2(7)=SYSUTCDATETIME();
    DECLARE @admin UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='ADMIN');

    DECLARE @groups TABLE(Code VARCHAR(100),Title NVARCHAR(120),Icon VARCHAR(100),OrderNo INT);
    INSERT @groups VALUES
      ('MENU_ADMIN_WORK_QUEUE',N'Công việc','fa-solid fa-list-check',10),
      ('MENU_ADMIN_ACCOUNTS',N'Đối tác','fa-solid fa-handshake',20),
      ('MENU_ADMIN_CAMPAIGN_GROUP',N'Chiến dịch','fa-solid fa-bullhorn',30),
      ('MENU_ADMIN_OPERATIONS_GROUP',N'Vận hành','fa-solid fa-gears',40),
      ('MENU_ADMIN_FINANCE_GROUP',N'Tài chính','fa-solid fa-wallet',50),
      ('MENU_ADMIN_INSIGHTS_GROUP',N'Báo cáo','fa-solid fa-chart-line',60),
      ('MENU_ADMIN_CONTENT_GROUP',N'Nội dung','fa-solid fa-pen-ruler',70),
      ('MENU_ADMIN_SETTINGS',N'Cấu hình','fa-solid fa-gear',90);

    INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc)
    SELECT NEWID(),g.Code,g.Title,NULL,g.Icon,NULL,g.OrderNo,1,@now
    FROM @groups g WHERE NOT EXISTS(SELECT 1 FROM dbo.Menus m WHERE m.Code=g.Code);
    UPDATE m SET Title=g.Title,Path=NULL,Icon=g.Icon,ParentId=NULL,OrderNo=g.OrderNo,IsVisible=1
    FROM dbo.Menus m JOIN @groups g ON g.Code=m.Code;
    INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc)
    SELECT @admin,m.Id,@now FROM dbo.Menus m JOIN @groups g ON g.Code=m.Code
    WHERE @admin IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus rm WHERE rm.RoleId=@admin AND rm.MenuId=m.Id);

    DECLARE @work UNIQUEIDENTIFIER=(SELECT Id FROM dbo.Menus WHERE Code='MENU_ADMIN_WORK_QUEUE');
    DECLARE @accounts UNIQUEIDENTIFIER=(SELECT Id FROM dbo.Menus WHERE Code='MENU_ADMIN_ACCOUNTS');
    DECLARE @campaign UNIQUEIDENTIFIER=(SELECT Id FROM dbo.Menus WHERE Code='MENU_ADMIN_CAMPAIGN_GROUP');
    DECLARE @operations UNIQUEIDENTIFIER=(SELECT Id FROM dbo.Menus WHERE Code='MENU_ADMIN_OPERATIONS_GROUP');
    DECLARE @finance UNIQUEIDENTIFIER=(SELECT Id FROM dbo.Menus WHERE Code='MENU_ADMIN_FINANCE_GROUP');
    DECLARE @reports UNIQUEIDENTIFIER=(SELECT Id FROM dbo.Menus WHERE Code='MENU_ADMIN_INSIGHTS_GROUP');
    DECLARE @content UNIQUEIDENTIFIER=(SELECT Id FROM dbo.Menus WHERE Code='MENU_ADMIN_CONTENT_GROUP');
    DECLARE @settings UNIQUEIDENTIFIER=(SELECT Id FROM dbo.Menus WHERE Code='MENU_ADMIN_SETTINGS');

    UPDATE dbo.Menus SET ParentId=@work,OrderNo=1,Title=N'Hồ sơ chờ duyệt' WHERE Code='MENU_ADMIN_PROFILE_REVIEWS';
    UPDATE dbo.Menus SET ParentId=@work,OrderNo=2,Title=N'Chiến dịch chờ duyệt' WHERE Code='MENU_ADMIN_CAMPAIGNS';
    UPDATE dbo.Menus SET ParentId=@work,OrderNo=3,Title=N'Đánh giá chờ xử lý' WHERE Code='MENU_ADMIN_REVIEW_MODERATION';
    UPDATE dbo.Menus SET ParentId=@work,OrderNo=4,Title=N'Sự cố & ngoại lệ' WHERE Code='MENU_ADMIN_OPERATIONS';
    UPDATE dbo.Menus SET ParentId=@work,OrderNo=5 WHERE Code='MENU_ADMIN_DISPUTES';

    UPDATE dbo.Menus SET ParentId=@accounts,OrderNo=1 WHERE Code='MENU_ADMIN_ACCOUNT_DIRECTORY';
    UPDATE dbo.Menus SET ParentId=@accounts,OrderNo=2 WHERE Code='MENU_ADMIN_KOC_ACCOUNTS';
    UPDATE dbo.Menus SET ParentId=@accounts,OrderNo=3 WHERE Code='MENU_ADMIN_VIOLATIONS';
    UPDATE dbo.Menus SET ParentId=@campaign,OrderNo=1 WHERE Code='MENU_ADMIN_MATCHING';
    UPDATE dbo.Menus SET ParentId=@campaign,OrderNo=2 WHERE Code='MENU_ADMIN_SETTINGS_PACKAGES';
    UPDATE dbo.Menus SET ParentId=@campaign,OrderNo=3 WHERE Code='MENU_ADMIN_COLLABORATIONS';
    UPDATE dbo.Menus SET ParentId=@operations,OrderNo=1 WHERE Code='MENU_ADMIN_BOOKING_CALENDAR';
    UPDATE dbo.Menus SET ParentId=@operations,OrderNo=2 WHERE Code='MENU_ADMIN_NOTIFICATIONS';

    UPDATE dbo.Menus SET ParentId=@finance,OrderNo=1 WHERE Code='MENU_ADMIN_CAMPAIGN_FUNDING';
    UPDATE dbo.Menus SET ParentId=@finance,OrderNo=2 WHERE Code='MENU_ADMIN_CAMPAIGN_FEE_POLICY';
    UPDATE dbo.Menus SET ParentId=@finance,OrderNo=3 WHERE Code='MENU_ADMIN_FINANCE_SETTLEMENTS';
    UPDATE dbo.Menus SET ParentId=@finance,OrderNo=4 WHERE Code='MENU_ADMIN_BILLING';
    UPDATE dbo.Menus SET ParentId=@finance,OrderNo=5 WHERE Code='MENU_ADMIN_PAYOUT_POLICY';
    UPDATE dbo.Menus SET ParentId=@finance,OrderNo=6 WHERE Code='MENU_ADMIN_SETTINGS_PAYMENTS';

    UPDATE dbo.Menus SET ParentId=@reports,OrderNo=1 WHERE Code='MENU_ADMIN_REPORTS';
    UPDATE dbo.Menus SET ParentId=@reports,OrderNo=2 WHERE Code='MENU_ADMIN_AUDIT_LOGS';
    UPDATE dbo.Menus SET ParentId=@content,OrderNo=1 WHERE Code='MENU_ADMIN_ACADEMY';
    UPDATE dbo.Menus SET ParentId=@content,OrderNo=2 WHERE Code='MENU_ADMIN_SETTINGS_HOMEPAGE';
    UPDATE dbo.Menus SET ParentId=@content,OrderNo=3 WHERE Code='MENU_ADMIN_SETTINGS_SEO';
    UPDATE dbo.Menus SET ParentId=@content,OrderNo=4 WHERE Code='MENU_ADMIN_NOTIFICATION_TEMPLATES';
    UPDATE dbo.Menus SET ParentId=@content,OrderNo=5 WHERE Code='MENU_ADMIN_SETTINGS_KOC_CATEGORIES';

    UPDATE dbo.Menus SET ParentId=@settings,OrderNo=1 WHERE Code='MENU_ADMIN_SETTINGS_WEBSITE';
    UPDATE dbo.Menus SET ParentId=@settings,OrderNo=2 WHERE Code='MENU_ADMIN_SETTINGS_BRANDING';
    UPDATE dbo.Menus SET ParentId=@settings,OrderNo=3 WHERE Code='MENU_ADMIN_SETTINGS_EMAIL';
    UPDATE dbo.Menus SET ParentId=@settings,OrderNo=4 WHERE Code='MENU_ADMIN_SETTINGS_SOCIAL_LOGIN';
    UPDATE dbo.Menus SET ParentId=@settings,OrderNo=5 WHERE Code='MENU_ADMIN_SETTINGS_SHIPPING';
    UPDATE dbo.Menus SET ParentId=@settings,OrderNo=6 WHERE Code='MENU_ADMIN_SETTINGS_OPERATIONAL';
    UPDATE dbo.Menus SET ParentId=@settings,OrderNo=7 WHERE Code='MENU_ADMIN_SETTINGS_MENUS';
    UPDATE dbo.Menus SET ParentId=@settings,OrderNo=8 WHERE Code='MENU_ADMIN_SETTINGS_SAMPLE_DATA';

    UPDATE dbo.Menus SET IsVisible=0 WHERE Code IN('MENU_ADMIN_TÀI_KHOẢN','MENU_ADMIN_PERMISSIONS');
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
