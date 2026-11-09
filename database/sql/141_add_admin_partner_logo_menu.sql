SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @parent UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.Menus WHERE Code = 'MENU_ADMIN_SETTINGS');
    DECLARE @menu UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.Menus WHERE Code = 'MENU_ADMIN_SETTINGS_PARTNERS');
    IF @menu IS NULL
    BEGIN
        SET @menu = NEWID();
        INSERT dbo.Menus(Id, Code, Title, Path, Icon, ParentId, OrderNo, IsVisible, CreatedAtUtc)
        VALUES(@menu, 'MENU_ADMIN_SETTINGS_PARTNERS', N'Logo đối tác', '/Admin/Settings/Partners', 'fa-solid fa-handshake', @parent, 4, 1, SYSUTCDATETIME());
    END
    ELSE
        UPDATE dbo.Menus SET Title=N'Logo đối tác', Path='/Admin/Settings/Partners', Icon='fa-solid fa-handshake', ParentId=@parent, OrderNo=4, IsVisible=1 WHERE Id=@menu;

    INSERT dbo.RoleMenus(RoleId, MenuId, CreatedAtUtc)
    SELECT r.Id, @menu, SYSUTCDATETIME()
    FROM dbo.Roles r
    WHERE UPPER(r.Code) = 'ADMIN'
      AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus rm WHERE rm.RoleId=r.Id AND rm.MenuId=@menu);

    COMMIT TRANSACTION;
    PRINT N'OK - Đã thêm menu quản lý logo đối tác.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
