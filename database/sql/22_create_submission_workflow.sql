SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Tạo task còn thiếu cho các participation đã tồn tại trước khi có workflow.
INSERT INTO [dbo].[Submissions]
    ([Id],[ParticipationId],[DeliverableId],[Status],[RevisionCount],[ViewCount],[IsVisible],[CreatedAtUtc])
SELECT NEWID(), p.[Id], d.[Id], 'NotSubmitted', 0, 0, 1, SYSUTCDATETIME()
FROM [dbo].[Participations] p
INNER JOIN [dbo].[CampaignDeliverables] d ON d.[CampaignId] = p.[CampaignId]
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Submissions] s
    WHERE s.[ParticipationId] = p.[Id] AND s.[DeliverableId] = d.[Id]
);

-- Chỉ tạo unique index khi dữ liệu hiện hữu không có trùng lặp.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'UX_Submissions_Participation_Deliverable' AND [object_id] = OBJECT_ID('dbo.Submissions'))
   AND NOT EXISTS (
       SELECT 1 FROM [dbo].[Submissions]
       GROUP BY [ParticipationId],[DeliverableId]
       HAVING COUNT(*) > 1
   )
BEGIN
    CREATE UNIQUE INDEX [UX_Submissions_Participation_Deliverable]
        ON [dbo].[Submissions]([ParticipationId],[DeliverableId]);
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_KOC_TASKS')
    INSERT INTO [dbo].[Menus] ([Id],[Code],[Title],[Path],[Icon],[ParentId],[OrderNo],[IsVisible],[CreatedAtUtc])
    VALUES (NEWID(),'MENU_KOC_TASKS',N'Đầu việc','/Koc/Tasks','fa-solid fa-list-check',NULL,3,1,SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_BRAND_DELIVERABLES')
    INSERT INTO [dbo].[Menus] ([Id],[Code],[Title],[Path],[Icon],[ParentId],[OrderNo],[IsVisible],[CreatedAtUtc])
    VALUES (NEWID(),'MENU_BRAND_DELIVERABLES',N'Duyệt nội dung','/Brand/Deliverables','fa-solid fa-clipboard-check',NULL,3,1,SYSUTCDATETIME());

DECLARE @KocRoleId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Roles] WHERE UPPER([Code]) = 'KOC');
DECLARE @BrandRoleId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Roles] WHERE UPPER([Code]) = 'BRAND');
DECLARE @KocMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Menus] WHERE [Code] = 'MENU_KOC_TASKS');
DECLARE @BrandMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Menus] WHERE [Code] = 'MENU_BRAND_DELIVERABLES');

IF @KocRoleId IS NOT NULL AND @KocMenuId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[RoleMenus] WHERE [RoleId]=@KocRoleId AND [MenuId]=@KocMenuId)
    INSERT INTO [dbo].[RoleMenus] ([RoleId],[MenuId],[CreatedAtUtc]) VALUES (@KocRoleId,@KocMenuId,SYSUTCDATETIME());
IF @BrandRoleId IS NOT NULL AND @BrandMenuId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[RoleMenus] WHERE [RoleId]=@BrandRoleId AND [MenuId]=@BrandMenuId)
    INSERT INTO [dbo].[RoleMenus] ([RoleId],[MenuId],[CreatedAtUtc]) VALUES (@BrandRoleId,@BrandMenuId,SYSUTCDATETIME());

COMMIT TRANSACTION;
