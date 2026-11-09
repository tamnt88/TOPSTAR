SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @adminRoleId UNIQUEIDENTIFIER =
        (SELECT TOP (1) Id FROM dbo.Roles WHERE UPPER(Code) = 'ADMIN');
    DECLARE @accountsMenuId UNIQUEIDENTIFIER =
        (SELECT TOP (1) Id FROM dbo.Menus WHERE Code = 'MENU_ADMIN_ACCOUNTS');
    DECLARE @directoryMenuId UNIQUEIDENTIFIER =
        (SELECT TOP (1) Id FROM dbo.Menus WHERE Code = 'MENU_ADMIN_ACCOUNT_DIRECTORY');

    IF @accountsMenuId IS NULL
        THROW 51000, N'Không tìm thấy menu cha MENU_ADMIN_ACCOUNTS. Hãy chạy migration 75 trước.', 1;

    IF @directoryMenuId IS NULL
    BEGIN
        SET @directoryMenuId = NEWID();

        INSERT dbo.Menus
        (
            Id, Code, Title, Path, Icon, ParentId, OrderNo, IsVisible, CreatedAtUtc
        )
        VALUES
        (
            @directoryMenuId,
            'MENU_ADMIN_ACCOUNT_DIRECTORY',
            N'Quản lý KOC & Brand',
            '/Admin/Brands',
            'fa-solid fa-address-book',
            @accountsMenuId,
            1,
            1,
            @now
        );
    END
    ELSE
    BEGIN
        UPDATE dbo.Menus
        SET Title = N'Quản lý KOC & Brand',
            Path = '/Admin/Brands',
            Icon = 'fa-solid fa-address-book',
            ParentId = @accountsMenuId,
            OrderNo = 1,
            IsVisible = 1
        WHERE Id = @directoryMenuId;
    END;

    UPDATE dbo.Menus
    SET ParentId = @accountsMenuId,
        Title = N'Hồ sơ chờ duyệt',
        OrderNo = 2,
        IsVisible = 1
    WHERE Code = 'MENU_ADMIN_PROFILE_REVIEWS';

    UPDATE dbo.Menus
    SET ParentId = @accountsMenuId,
        Title = N'Vi phạm tài khoản',
        OrderNo = 3,
        IsVisible = 1
    WHERE Code = 'MENU_ADMIN_VIOLATIONS';

    IF @adminRoleId IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM dbo.RoleMenus
           WHERE RoleId = @adminRoleId
             AND MenuId = @directoryMenuId
       )
    BEGIN
        INSERT dbo.RoleMenus(RoleId, MenuId, CreatedAtUtc)
        VALUES(@adminRoleId, @directoryMenuId, @now);
    END;

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
