SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @CampaignStatusConstraint SYSNAME;
SELECT TOP 1 @CampaignStatusConstraint=cc.[name] FROM sys.check_constraints cc
WHERE cc.[parent_object_id]=OBJECT_ID('dbo.Campaigns') AND cc.[definition] LIKE '%Status%';
IF @CampaignStatusConstraint IS NOT NULL
BEGIN
    DECLARE @DropCampaignStatusSql NVARCHAR(500)=N'ALTER TABLE [dbo].[Campaigns] DROP CONSTRAINT '+QUOTENAME(@CampaignStatusConstraint)+N';';
    EXEC sp_executesql @DropCampaignStatusSql;
END;
ALTER TABLE [dbo].[Campaigns] WITH CHECK ADD CONSTRAINT [CK_Campaigns_Status]
    CHECK ([Status] IN ('Draft','Submitted','NeedEdit','Approved','Published','Rejected','InProgress','Completed','Cancelled'));

IF OBJECT_ID('dbo.CampaignStatusHistories','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CampaignStatusHistories] (
        [Id] UNIQUEIDENTIFIER NOT NULL,[CampaignId] UNIQUEIDENTIFIER NOT NULL,[FromStatus] NVARCHAR(30) NULL,[ToStatus] NVARCHAR(30) NOT NULL,
        [Note] NVARCHAR(2000) NULL,[ActorUserId] UNIQUEIDENTIFIER NULL,[CreatedAtUtc] DATETIME2(0) NOT NULL,
        CONSTRAINT [PK_CampaignStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CampaignStatusHistories_From] CHECK ([FromStatus] IS NULL OR [FromStatus] IN ('Draft','Submitted','NeedEdit','Approved','Published','Rejected','InProgress','Completed','Cancelled')),
        CONSTRAINT [CK_CampaignStatusHistories_To] CHECK ([ToStatus] IN ('Draft','Submitted','NeedEdit','Approved','Published','Rejected','InProgress','Completed','Cancelled')),
        CONSTRAINT [FK_CampaignStatusHistories_Campaign] FOREIGN KEY ([CampaignId]) REFERENCES [dbo].[Campaigns]([Id]),
        CONSTRAINT [FK_CampaignStatusHistories_ActorUser] FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Users]([Id])
    );
    CREATE INDEX [IX_CampaignStatusHistories_Campaign_Created] ON [dbo].[CampaignStatusHistories]([CampaignId],[CreatedAtUtc]);
END;

IF OBJECT_ID('dbo.ApplicationStatusHistories','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ApplicationStatusHistories] (
        [Id] UNIQUEIDENTIFIER NOT NULL,[ApplicationId] UNIQUEIDENTIFIER NOT NULL,[FromStatus] NVARCHAR(30) NULL,[ToStatus] NVARCHAR(30) NOT NULL,
        [Note] NVARCHAR(2000) NULL,[ActorUserId] UNIQUEIDENTIFIER NULL,[CreatedAtUtc] DATETIME2(0) NOT NULL,
        CONSTRAINT [PK_ApplicationStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ApplicationStatusHistories_From] CHECK ([FromStatus] IS NULL OR [FromStatus] IN ('Applied','Shortlisted','NotSelected','Confirmed','Withdrawn','CancelledByAdmin')),
        CONSTRAINT [CK_ApplicationStatusHistories_To] CHECK ([ToStatus] IN ('Applied','Shortlisted','NotSelected','Confirmed','Withdrawn','CancelledByAdmin')),
        CONSTRAINT [FK_ApplicationStatusHistories_Application] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications]([Id]),
        CONSTRAINT [FK_ApplicationStatusHistories_ActorUser] FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Users]([Id])
    );
    CREATE INDEX [IX_ApplicationStatusHistories_Application_Created] ON [dbo].[ApplicationStatusHistories]([ApplicationId],[CreatedAtUtc]);
END;

IF OBJECT_ID('dbo.ParticipationStatusHistories','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ParticipationStatusHistories] (
        [Id] UNIQUEIDENTIFIER NOT NULL,[ParticipationId] UNIQUEIDENTIFIER NOT NULL,[FromStatus] NVARCHAR(30) NULL,[ToStatus] NVARCHAR(30) NOT NULL,
        [Note] NVARCHAR(2000) NULL,[ActorUserId] UNIQUEIDENTIFIER NULL,[CreatedAtUtc] DATETIME2(0) NOT NULL,
        CONSTRAINT [PK_ParticipationStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ParticipationStatusHistories_From] CHECK ([FromStatus] IS NULL OR [FromStatus] IN ('Confirmed','Completed','Cancelled')),
        CONSTRAINT [CK_ParticipationStatusHistories_To] CHECK ([ToStatus] IN ('Confirmed','Completed','Cancelled')),
        CONSTRAINT [FK_ParticipationStatusHistories_Participation] FOREIGN KEY ([ParticipationId]) REFERENCES [dbo].[Participations]([Id]),
        CONSTRAINT [FK_ParticipationStatusHistories_ActorUser] FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Users]([Id])
    );
    CREATE INDEX [IX_ParticipationStatusHistories_Participation_Created] ON [dbo].[ParticipationStatusHistories]([ParticipationId],[CreatedAtUtc]);
END;

EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_CampaignStatusHistories_AppendOnly] ON [dbo].[CampaignStatusHistories] INSTEAD OF UPDATE, DELETE AS BEGIN THROW 51014, ''CampaignStatusHistories is append-only.'', 1; END;');
EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_ApplicationStatusHistories_AppendOnly] ON [dbo].[ApplicationStatusHistories] INSTEAD OF UPDATE, DELETE AS BEGIN THROW 51015, ''ApplicationStatusHistories is append-only.'', 1; END;');
EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_ParticipationStatusHistories_AppendOnly] ON [dbo].[ParticipationStatusHistories] INSTEAD OF UPDATE, DELETE AS BEGIN THROW 51016, ''ParticipationStatusHistories is append-only.'', 1; END;');

INSERT INTO [dbo].[CampaignStatusHistories] ([Id],[CampaignId],[FromStatus],[ToStatus],[Note],[ActorUserId],[CreatedAtUtc])
SELECT NEWID(),c.[Id],NULL,c.[Status],N'Migrated snapshot',NULL,COALESCE(c.[UpdatedAtUtc],c.[CreatedAtUtc]) FROM [dbo].[Campaigns] c
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[CampaignStatusHistories] h WHERE h.[CampaignId]=c.[Id]);
INSERT INTO [dbo].[ApplicationStatusHistories] ([Id],[ApplicationId],[FromStatus],[ToStatus],[Note],[ActorUserId],[CreatedAtUtc])
SELECT NEWID(),a.[Id],NULL,a.[Status],N'Migrated snapshot',NULL,COALESCE(a.[UpdatedAtUtc],a.[CreatedAtUtc]) FROM [dbo].[Applications] a
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[ApplicationStatusHistories] h WHERE h.[ApplicationId]=a.[Id]);
INSERT INTO [dbo].[ParticipationStatusHistories] ([Id],[ParticipationId],[FromStatus],[ToStatus],[Note],[ActorUserId],[CreatedAtUtc])
SELECT NEWID(),p.[Id],NULL,p.[Status],N'Migrated snapshot',NULL,COALESCE(p.[UpdatedAtUtc],p.[CreatedAtUtc]) FROM [dbo].[Participations] p
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[ParticipationStatusHistories] h WHERE h.[ParticipationId]=p.[Id]);

COMMIT TRANSACTION;
