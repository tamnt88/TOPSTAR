SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.ReviewStatusHistories','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReviewStatusHistories] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [ReviewId] UNIQUEIDENTIFIER NOT NULL,
        [FromStatus] NVARCHAR(40) NULL,
        [ToStatus] NVARCHAR(40) NOT NULL,
        [Reason] NVARCHAR(500) NULL,
        [ActorUserId] UNIQUEIDENTIFIER NULL,
        [CreatedAtUtc] DATETIME2(0) NOT NULL,
        CONSTRAINT [PK_ReviewStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ReviewStatusHistories_From] CHECK ([FromStatus] IS NULL OR [FromStatus] IN ('Pending','Completed','Hidden')),
        CONSTRAINT [CK_ReviewStatusHistories_To] CHECK ([ToStatus] IN ('Pending','Completed','Hidden')),
        CONSTRAINT [FK_ReviewStatusHistories_Review] FOREIGN KEY ([ReviewId]) REFERENCES [dbo].[Reviews]([Id]),
        CONSTRAINT [FK_ReviewStatusHistories_ActorUser] FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Users]([Id])
    );
    CREATE INDEX [IX_ReviewStatusHistories_Review_Created]
        ON [dbo].[ReviewStatusHistories]([ReviewId],[CreatedAtUtc]);
END;

EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_ReviewStatusHistories_AppendOnly] ON [dbo].[ReviewStatusHistories] INSTEAD OF UPDATE, DELETE AS BEGIN THROW 51013, ''ReviewStatusHistories is append-only.'', 1; END;');

INSERT INTO [dbo].[ReviewStatusHistories] ([Id],[ReviewId],[FromStatus],[ToStatus],[Reason],[ActorUserId],[CreatedAtUtc])
SELECT NEWID(),r.[Id],NULL,r.[Status],N'Migrated snapshot',NULL,COALESCE(r.[UpdatedAtUtc],r.[CreatedAtUtc])
FROM [dbo].[Reviews] r
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[ReviewStatusHistories] h WHERE h.[ReviewId]=r.[Id]);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code]='MENU_ADMIN_REVIEW_MODERATION')
    INSERT INTO [dbo].[Menus] ([Id],[Code],[Title],[Path],[Icon],[ParentId],[OrderNo],[IsVisible],[CreatedAtUtc])
    VALUES (NEWID(),'MENU_ADMIN_REVIEW_MODERATION',N'Kiểm duyệt đánh giá','/Admin/ReviewModeration','fa-solid fa-shield-halved',NULL,7,1,SYSUTCDATETIME());

DECLARE @AdminRoleId UNIQUEIDENTIFIER=(SELECT TOP 1 [Id] FROM [dbo].[Roles] WHERE UPPER([Code])='ADMIN');
DECLARE @MenuId UNIQUEIDENTIFIER=(SELECT TOP 1 [Id] FROM [dbo].[Menus] WHERE [Code]='MENU_ADMIN_REVIEW_MODERATION');
IF @AdminRoleId IS NOT NULL AND @MenuId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[RoleMenus] WHERE [RoleId]=@AdminRoleId AND [MenuId]=@MenuId)
    INSERT INTO [dbo].[RoleMenus] ([RoleId],[MenuId],[CreatedAtUtc]) VALUES (@AdminRoleId,@MenuId,SYSUTCDATETIME());

COMMIT TRANSACTION;
