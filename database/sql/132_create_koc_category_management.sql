SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.KocCategories', 'SortOrder') IS NULL
        ALTER TABLE dbo.KocCategories ADD SortOrder INT NOT NULL CONSTRAINT DF_KocCategories_SortOrder DEFAULT(0);

    IF COL_LENGTH('dbo.KocCategories', 'IsActive') IS NULL
        ALTER TABLE dbo.KocCategories ADD IsActive BIT NOT NULL CONSTRAINT DF_KocCategories_IsActive DEFAULT(1);

    EXEC sys.sp_executesql N'
        ;WITH ordered AS
        (
            SELECT Id, ROW_NUMBER() OVER (ORDER BY Name, Code, Id) * 10 AS NewOrder
            FROM dbo.KocCategories
        )
        UPDATE category SET SortOrder = ordered.NewOrder
        FROM dbo.KocCategories category
        INNER JOIN ordered ON ordered.Id = category.Id
        WHERE category.SortOrder = 0;';

    IF OBJECT_ID('dbo.KocCategoryHistories', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.KocCategoryHistories
        (
            Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_KocCategoryHistories PRIMARY KEY,
            KocCategoryId UNIQUEIDENTIFIER NOT NULL,
            ActionName VARCHAR(30) NOT NULL,
            PreviousJson NVARCHAR(MAX) NOT NULL,
            CurrentJson NVARCHAR(MAX) NOT NULL,
            Reason NVARCHAR(500) NOT NULL,
            ActorUserId UNIQUEIDENTIFIER NOT NULL,
            CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_KocCategoryHistories_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
            CONSTRAINT FK_KocCategoryHistories_KocCategories FOREIGN KEY(KocCategoryId) REFERENCES dbo.KocCategories(Id),
            CONSTRAINT FK_KocCategoryHistories_Users FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
            CONSTRAINT CK_KocCategoryHistories_Action CHECK(ActionName IN('Create','Update','Activate','Deactivate')),
            CONSTRAINT CK_KocCategoryHistories_Reason CHECK(LEN(LTRIM(RTRIM(Reason))) BETWEEN 10 AND 500)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.KocCategories') AND name='IX_KocCategories_IsActive_SortOrder')
        EXEC sys.sp_executesql N'CREATE INDEX IX_KocCategories_IsActive_SortOrder ON dbo.KocCategories(IsActive, SortOrder) INCLUDE(Name, Code);';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.KocCategoryHistories') AND name='IX_KocCategoryHistories_Category_Created')
        CREATE INDEX IX_KocCategoryHistories_Category_Created ON dbo.KocCategoryHistories(KocCategoryId, CreatedAtUtc DESC);

    DECLARE @parent UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_ADMIN_SETTINGS');
    DECLARE @menu UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_ADMIN_SETTINGS_KOC_CATEGORIES');
    IF @menu IS NULL
    BEGIN
        SET @menu = NEWID();
        INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc)
        VALUES(@menu,'MENU_ADMIN_SETTINGS_KOC_CATEGORIES',N'Lĩnh vực nội dung KOC','/Admin/Settings/KocCategories','fa-solid fa-tags',@parent,35,1,SYSUTCDATETIME());
    END
    ELSE
        UPDATE dbo.Menus SET Title=N'Lĩnh vực nội dung KOC',Path='/Admin/Settings/KocCategories',Icon='fa-solid fa-tags',ParentId=@parent,IsVisible=1 WHERE Id=@menu;

    INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc)
    SELECT role.Id,@menu,SYSUTCDATETIME()
    FROM dbo.Roles role
    WHERE UPPER(role.Code)='ADMIN'
      AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus existing WHERE existing.RoleId=role.Id AND existing.MenuId=@menu);

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
GO
