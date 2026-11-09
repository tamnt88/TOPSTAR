SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @adminRoleId UNIQUEIDENTIFIER =
        (SELECT TOP (1) Id FROM dbo.Roles WHERE UPPER(Code) = 'ADMIN');
    DECLARE @accountsMenuId UNIQUEIDENTIFIER =
        (SELECT TOP (1) Id FROM dbo.Menus WHERE Code = 'MENU_ADMIN_ACCOUNTS');
    DECLARE @brandMenuId UNIQUEIDENTIFIER =
        (SELECT TOP (1) Id FROM dbo.Menus WHERE Code = 'MENU_ADMIN_ACCOUNT_DIRECTORY');
    DECLARE @kocMenuId UNIQUEIDENTIFIER =
        (SELECT TOP (1) Id FROM dbo.Menus WHERE Code = 'MENU_ADMIN_KOC_ACCOUNTS');

    IF @accountsMenuId IS NULL
        THROW 51000, N'Không tìm thấy menu cha MENU_ADMIN_ACCOUNTS. Hãy chạy migration 75 trước.', 1;

    IF @brandMenuId IS NULL
    BEGIN
        SET @brandMenuId = NEWID();
        INSERT dbo.Menus(Id, Code, Title, Path, Icon, ParentId, OrderNo, IsVisible, CreatedAtUtc)
        VALUES
        (
            @brandMenuId,
            'MENU_ADMIN_ACCOUNT_DIRECTORY',
            N'Quản lý Brand',
            '/Admin/Accounts/Brand',
            'fa-regular fa-building',
            @accountsMenuId,
            1,
            1,
            @now
        );
    END
    ELSE
    BEGIN
        UPDATE dbo.Menus
        SET Title = N'Quản lý Brand',
            Path = '/Admin/Accounts/Brand',
            Icon = 'fa-regular fa-building',
            ParentId = @accountsMenuId,
            OrderNo = 1,
            IsVisible = 1
        WHERE Id = @brandMenuId;
    END;

    IF @kocMenuId IS NULL
    BEGIN
        SET @kocMenuId = NEWID();
        INSERT dbo.Menus(Id, Code, Title, Path, Icon, ParentId, OrderNo, IsVisible, CreatedAtUtc)
        VALUES
        (
            @kocMenuId,
            'MENU_ADMIN_KOC_ACCOUNTS',
            N'Quản lý KOC',
            '/Admin/Accounts/KOC',
            'fa-solid fa-users',
            @accountsMenuId,
            2,
            1,
            @now
        );
    END
    ELSE
    BEGIN
        UPDATE dbo.Menus
        SET Title = N'Quản lý KOC',
            Path = '/Admin/Accounts/KOC',
            Icon = 'fa-solid fa-users',
            ParentId = @accountsMenuId,
            OrderNo = 2,
            IsVisible = 1
        WHERE Id = @kocMenuId;
    END;

    UPDATE dbo.Menus
    SET ParentId = @accountsMenuId, OrderNo = 3, IsVisible = 1
    WHERE Code = 'MENU_ADMIN_PROFILE_REVIEWS';

    UPDATE dbo.Menus
    SET ParentId = @accountsMenuId, OrderNo = 4, IsVisible = 1
    WHERE Code = 'MENU_ADMIN_VIOLATIONS';

    IF @adminRoleId IS NOT NULL
    BEGIN
        INSERT dbo.RoleMenus(RoleId, MenuId, CreatedAtUtc)
        SELECT @adminRoleId, source.MenuId, @now
        FROM (VALUES (@brandMenuId), (@kocMenuId)) source(MenuId)
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.RoleMenus existing
            WHERE existing.RoleId = @adminRoleId
              AND existing.MenuId = source.MenuId
        );
    END;

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
