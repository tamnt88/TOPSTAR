SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (
    SELECT 1 FROM [dbo].[Reviews]
    GROUP BY [CampaignId],[ReviewerUserId],[RevieweeUserId]
    HAVING COUNT(*) > 1
)
    THROW 51000, 'Reviews contains duplicate campaign/reviewer/reviewee rows. Resolve duplicates before running migration 24.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE [object_id] = OBJECT_ID('dbo.Reviews')
      AND [name] = 'UX_Reviews_Campaign_Reviewer_Reviewee'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Reviews_Campaign_Reviewer_Reviewee]
        ON [dbo].[Reviews]([CampaignId],[ReviewerUserId],[RevieweeUserId]);
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_BRAND_REVIEWS')
    INSERT INTO [dbo].[Menus] ([Id],[Code],[Title],[Path],[Icon],[ParentId],[OrderNo],[IsVisible],[CreatedAtUtc])
    VALUES (NEWID(),'MENU_BRAND_REVIEWS',N'Đánh giá KOC','/Brand/Reviews','fa-solid fa-star',NULL,4,1,SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_KOC_REVIEWS')
    INSERT INTO [dbo].[Menus] ([Id],[Code],[Title],[Path],[Icon],[ParentId],[OrderNo],[IsVisible],[CreatedAtUtc])
    VALUES (NEWID(),'MENU_KOC_REVIEWS',N'Đánh giá Brand','/Koc/Reviews','fa-solid fa-star',NULL,4,1,SYSUTCDATETIME());

DECLARE @BrandRoleId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Roles] WHERE UPPER([Code]) = 'BRAND');
DECLARE @KocRoleId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Roles] WHERE UPPER([Code]) = 'KOC');
DECLARE @BrandMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Menus] WHERE [Code] = 'MENU_BRAND_REVIEWS');
DECLARE @KocMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Menus] WHERE [Code] = 'MENU_KOC_REVIEWS');

IF @BrandRoleId IS NOT NULL AND @BrandMenuId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[RoleMenus] WHERE [RoleId]=@BrandRoleId AND [MenuId]=@BrandMenuId)
    INSERT INTO [dbo].[RoleMenus] ([RoleId],[MenuId],[CreatedAtUtc]) VALUES (@BrandRoleId,@BrandMenuId,SYSUTCDATETIME());
IF @KocRoleId IS NOT NULL AND @KocMenuId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[RoleMenus] WHERE [RoleId]=@KocRoleId AND [MenuId]=@KocMenuId)
    INSERT INTO [dbo].[RoleMenus] ([RoleId],[MenuId],[CreatedAtUtc]) VALUES (@KocRoleId,@KocMenuId,SYSUTCDATETIME());

COMMIT TRANSACTION;
