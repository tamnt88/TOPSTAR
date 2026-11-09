/*
 KOC navigation only:
 - Fix Vietnamese labels.
 - Keep Dashboard as the direct entry.
 - Group remaining features under four second-level menus.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @KocRoleId UNIQUEIDENTIFIER =
        (SELECT TOP (1) Id FROM dbo.Roles WHERE UPPER(Code) = 'KOC');
    IF @KocRoleId IS NULL
        THROW 51000, N'Không tìm thấy role KOC.', 1;

    DECLARE @WorkId UNIQUEIDENTIFIER =
        (SELECT TOP (1) Id FROM dbo.Menus WHERE Code = 'MENU_KOC_GROUP_WORK');
    DECLARE @ProfileId UNIQUEIDENTIFIER =
        (SELECT TOP (1) Id FROM dbo.Menus WHERE Code = 'MENU_KOC_GROUP_PROFILE');
    DECLARE @FinanceId UNIQUEIDENTIFIER =
        (SELECT TOP (1) Id FROM dbo.Menus WHERE Code = 'MENU_KOC_GROUP_FINANCE');
    DECLARE @SupportId UNIQUEIDENTIFIER =
        (SELECT TOP (1) Id FROM dbo.Menus WHERE Code = 'MENU_KOC_GROUP_SUPPORT');

    IF @WorkId IS NULL
    BEGIN
        SET @WorkId = NEWID();
        INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc)
        VALUES(@WorkId,'MENU_KOC_GROUP_WORK',N'Công việc',NULL,'fa-solid fa-briefcase',NULL,2,1,SYSUTCDATETIME());
    END;
    IF @ProfileId IS NULL
    BEGIN
        SET @ProfileId = NEWID();
        INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc)
        VALUES(@ProfileId,'MENU_KOC_GROUP_PROFILE',N'Hồ sơ & uy tín',NULL,'fa-solid fa-user-shield',NULL,3,1,SYSUTCDATETIME());
    END;
    IF @FinanceId IS NULL
    BEGIN
        SET @FinanceId = NEWID();
        INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc)
        VALUES(@FinanceId,'MENU_KOC_GROUP_FINANCE',N'Tài chính',NULL,'fa-solid fa-wallet',NULL,4,1,SYSUTCDATETIME());
    END;
    IF @SupportId IS NULL
    BEGIN
        SET @SupportId = NEWID();
        INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc)
        VALUES(@SupportId,'MENU_KOC_GROUP_SUPPORT',N'Hỗ trợ',NULL,'fa-solid fa-headset',NULL,5,1,SYSUTCDATETIME());
    END;

    UPDATE dbo.Menus SET Title=N'Tổng quan',OrderNo=1,ParentId=NULL,UpdatedAtUtc=SYSUTCDATETIME()
    WHERE Code='MENU_KOC_DASHBOARD';

    UPDATE dbo.Menus SET Title=N'Việc làm phù hợp',ParentId=@WorkId,OrderNo=1,UpdatedAtUtc=SYSUTCDATETIME()
    WHERE Code='MENU_KOC_JOBS';
    UPDATE dbo.Menus SET Title=N'Đầu việc của tôi',ParentId=@WorkId,OrderNo=2,UpdatedAtUtc=SYSUTCDATETIME()
    WHERE Code='MENU_KOC_TASKS';
    UPDATE dbo.Menus SET Title=N'Lịch booking',ParentId=@WorkId,OrderNo=3,UpdatedAtUtc=SYSUTCDATETIME()
    WHERE Code='MENU_KOC_BOOKINGS';
    UPDATE dbo.Menus SET Title=N'Đơn hàng',ParentId=@WorkId,OrderNo=4,UpdatedAtUtc=SYSUTCDATETIME()
    WHERE Code='MENU_KOC_SHIPMENTS';
    UPDATE dbo.Menus SET Title=N'Lịch sử hợp tác',ParentId=@WorkId,OrderNo=5,UpdatedAtUtc=SYSUTCDATETIME()
    WHERE Code='MENU_KOC_COLLABORATIONS';

    UPDATE dbo.Menus SET Title=N'Hồ sơ KOC',ParentId=@ProfileId,OrderNo=1,UpdatedAtUtc=SYSUTCDATETIME()
    WHERE Code='MENU_KOC_PROFILE';
    UPDATE dbo.Menus SET Title=N'Đánh giá từ Brand',ParentId=@ProfileId,OrderNo=2,UpdatedAtUtc=SYSUTCDATETIME()
    WHERE Code='MENU_KOC_REVIEWS';

    UPDATE dbo.Menus SET Title=N'Ví thu nhập',ParentId=@FinanceId,OrderNo=1,UpdatedAtUtc=SYSUTCDATETIME()
    WHERE Code='MENU_KOC_WALLET';

    UPDATE dbo.Menus SET Title=N'Tranh chấp & hỗ trợ',ParentId=@SupportId,OrderNo=1,UpdatedAtUtc=SYSUTCDATETIME()
    WHERE Code='MENU_KOC_DISPUTES';
    UPDATE dbo.Menus SET Title=N'Thông báo',ParentId=@SupportId,OrderNo=2,UpdatedAtUtc=SYSUTCDATETIME()
    WHERE Code='MENU_KOC_NOTIFICATIONS';

    INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc)
    SELECT @KocRoleId,v.MenuId,SYSUTCDATETIME()
    FROM (VALUES(@WorkId),(@ProfileId),(@FinanceId),(@SupportId)) v(MenuId)
    WHERE NOT EXISTS(
        SELECT 1 FROM dbo.RoleMenus rm
        WHERE rm.RoleId=@KocRoleId AND rm.MenuId=v.MenuId
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT p.Title AS NhomCap1,c.Title AS MenuCap2,c.Path,c.OrderNo
FROM dbo.Menus p
LEFT JOIN dbo.Menus c ON c.ParentId=p.Id AND c.IsVisible=1
WHERE p.Code IN ('MENU_KOC_DASHBOARD','MENU_KOC_GROUP_WORK','MENU_KOC_GROUP_PROFILE','MENU_KOC_GROUP_FINANCE','MENU_KOC_GROUP_SUPPORT')
ORDER BY p.OrderNo,c.OrderNo;
