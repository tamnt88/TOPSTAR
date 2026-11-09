SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.SystemWebsiteConfigs', 'HomepageContentJson') IS NULL
        ALTER TABLE dbo.SystemWebsiteConfigs ADD HomepageContentJson NVARCHAR(MAX) NULL;

    IF OBJECT_ID('dbo.HomepageContentHistories', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.HomepageContentHistories
        (
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_HomepageContentHistories PRIMARY KEY,
            ContentJson NVARCHAR(MAX) NOT NULL,
            Reason NVARCHAR(1000) NOT NULL,
            ActorUserId UNIQUEIDENTIFIER NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL,
            CONSTRAINT CK_HomepageContentHistories_ContentJson CHECK (ISJSON(ContentJson) = 1),
            CONSTRAINT CK_HomepageContentHistories_Reason CHECK (LEN(LTRIM(RTRIM(Reason))) BETWEEN 10 AND 1000),
            CONSTRAINT FK_HomepageContentHistories_Actor FOREIGN KEY (ActorUserId) REFERENCES dbo.Users(Id)
        );
        CREATE INDEX IX_HomepageContentHistories_CreatedAtUtc ON dbo.HomepageContentHistories(CreatedAtUtc DESC);
    END;

    DECLARE @parent UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.Menus WHERE Code = 'MENU_ADMIN_SETTINGS');
    DECLARE @menu UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.Menus WHERE Code = 'MENU_ADMIN_SETTINGS_HOMEPAGE');
    IF @menu IS NULL
    BEGIN
        SET @menu = NEWID();
        INSERT dbo.Menus(Id, Code, Title, Path, Icon, ParentId, OrderNo, IsVisible, CreatedAtUtc)
        VALUES(@menu, 'MENU_ADMIN_SETTINGS_HOMEPAGE', N'Nội dung trang chủ', '/Admin/Settings/HomepageContent', 'fa-solid fa-house-chimney-window', @parent, 3, 1, SYSUTCDATETIME());
    END
    ELSE
        UPDATE dbo.Menus SET Title=N'Nội dung trang chủ', Path='/Admin/Settings/HomepageContent', Icon='fa-solid fa-house-chimney-window', ParentId=@parent, IsVisible=1 WHERE Id=@menu;

    INSERT dbo.RoleMenus(RoleId, MenuId, CreatedAtUtc)
    SELECT r.Id, @menu, SYSUTCDATETIME()
    FROM dbo.Roles r
    WHERE UPPER(r.Code) = 'ADMIN'
      AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus rm WHERE rm.RoleId=r.Id AND rm.MenuId=@menu);

    COMMIT TRANSACTION;
    PRINT N'OK - Đã tạo quản trị nội dung trang chủ và lịch sử phiên bản.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
