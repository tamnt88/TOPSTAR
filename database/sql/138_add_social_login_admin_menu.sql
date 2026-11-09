SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @now DATETIME2(7)=SYSUTCDATETIME();
    DECLARE @admin UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='ADMIN');
    DECLARE @parent UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_ADMIN_SETTINGS');
    DECLARE @menu UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_ADMIN_SETTINGS_SOCIAL_LOGIN');
    IF @parent IS NULL THROW 51000, N'Không tìm thấy menu Cài đặt hệ thống.', 1;
    IF @menu IS NULL
    BEGIN
        SET @menu=NEWID();
        INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc)
        VALUES(@menu,'MENU_ADMIN_SETTINGS_SOCIAL_LOGIN',N'Đăng nhập Google & Facebook','/Admin/Settings/SocialLogin','fa-solid fa-right-to-bracket',@parent,3,1,@now);
    END
    ELSE UPDATE dbo.Menus SET Title=N'Đăng nhập Google & Facebook',Path='/Admin/Settings/SocialLogin',Icon='fa-solid fa-right-to-bracket',ParentId=@parent,OrderNo=3,IsVisible=1 WHERE Id=@menu;
    IF @admin IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus WHERE RoleId=@admin AND MenuId=@menu)
        INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc) VALUES(@admin,@menu,@now);
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
