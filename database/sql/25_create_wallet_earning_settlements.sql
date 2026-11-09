SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.WalletTransactions','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WalletTransactions] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [KocProfileId] UNIQUEIDENTIFIER NOT NULL,
        [CampaignId] UNIQUEIDENTIFIER NOT NULL,
        [ParticipationId] UNIQUEIDENTIFIER NOT NULL,
        [TransactionType] NVARCHAR(30) NOT NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [HoldReason] NVARCHAR(500) NULL,
        [CreatedByUserId] UNIQUEIDENTIFIER NOT NULL,
        [AvailableAtUtc] DATETIME2(0) NULL,
        [CreatedAtUtc] DATETIME2(0) NOT NULL,
        [UpdatedAtUtc] DATETIME2(0) NULL,
        CONSTRAINT [PK_WalletTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [UX_WalletTransactions_Participation] UNIQUE ([ParticipationId]),
        CONSTRAINT [CK_WalletTransactions_Type] CHECK ([TransactionType] IN ('Earning')),
        CONSTRAINT [CK_WalletTransactions_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_WalletTransactions_Status] CHECK ([Status] IN ('Pending','Available','OnHold','PaidOut','Failed')),
        CONSTRAINT [FK_WalletTransactions_KocProfile] FOREIGN KEY ([KocProfileId]) REFERENCES [dbo].[KocProfiles]([Id]),
        CONSTRAINT [FK_WalletTransactions_Campaign] FOREIGN KEY ([CampaignId]) REFERENCES [dbo].[Campaigns]([Id]),
        CONSTRAINT [FK_WalletTransactions_Participation] FOREIGN KEY ([ParticipationId]) REFERENCES [dbo].[Participations]([Id]),
        CONSTRAINT [FK_WalletTransactions_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users]([Id])
    );
    CREATE INDEX [IX_WalletTransactions_Koc_Status_Created]
        ON [dbo].[WalletTransactions]([KocProfileId],[Status],[CreatedAtUtc] DESC);
    CREATE INDEX [IX_WalletTransactions_Campaign]
        ON [dbo].[WalletTransactions]([CampaignId]);
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_ADMIN_FINANCE_SETTLEMENTS')
    INSERT INTO [dbo].[Menus] ([Id],[Code],[Title],[Path],[Icon],[ParentId],[OrderNo],[IsVisible],[CreatedAtUtc])
    VALUES (NEWID(),'MENU_ADMIN_FINANCE_SETTLEMENTS',N'Đối soát thu nhập','/Admin/Finance/Settlements','fa-solid fa-money-check-dollar',NULL,6,1,SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus] WHERE [Code] = 'MENU_KOC_WALLET')
    INSERT INTO [dbo].[Menus] ([Id],[Code],[Title],[Path],[Icon],[ParentId],[OrderNo],[IsVisible],[CreatedAtUtc])
    VALUES (NEWID(),'MENU_KOC_WALLET',N'Ví thu nhập','/Koc/Wallet','fa-solid fa-wallet',NULL,5,1,SYSUTCDATETIME());

DECLARE @AdminRoleId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Roles] WHERE UPPER([Code])='ADMIN');
DECLARE @KocRoleId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Roles] WHERE UPPER([Code])='KOC');
DECLARE @AdminMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Menus] WHERE [Code]='MENU_ADMIN_FINANCE_SETTLEMENTS');
DECLARE @KocMenuId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [dbo].[Menus] WHERE [Code]='MENU_KOC_WALLET');
IF @AdminRoleId IS NOT NULL AND @AdminMenuId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[RoleMenus] WHERE [RoleId]=@AdminRoleId AND [MenuId]=@AdminMenuId)
    INSERT INTO [dbo].[RoleMenus] ([RoleId],[MenuId],[CreatedAtUtc]) VALUES (@AdminRoleId,@AdminMenuId,SYSUTCDATETIME());
IF @KocRoleId IS NOT NULL AND @KocMenuId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[RoleMenus] WHERE [RoleId]=@KocRoleId AND [MenuId]=@KocMenuId)
    INSERT INTO [dbo].[RoleMenus] ([RoleId],[MenuId],[CreatedAtUtc]) VALUES (@KocRoleId,@KocMenuId,SYSUTCDATETIME());

COMMIT TRANSACTION;
