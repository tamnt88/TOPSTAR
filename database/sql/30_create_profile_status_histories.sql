SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.BrandProfileStatusHistories','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BrandProfileStatusHistories] (
        [Id] UNIQUEIDENTIFIER NOT NULL,[BrandProfileId] UNIQUEIDENTIFIER NOT NULL,[FromStatus] NVARCHAR(30) NULL,[ToStatus] NVARCHAR(30) NOT NULL,
        [ReviewNote] NVARCHAR(1000) NULL,[CreatedAtUtc] DATETIME2(0) NOT NULL,
        CONSTRAINT [PK_BrandProfileStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BrandProfileStatusHistories_From] CHECK ([FromStatus] IS NULL OR [FromStatus] IN ('Draft','PendingReview','NeedUpdate','Approved','Rejected','Locked')),
        CONSTRAINT [CK_BrandProfileStatusHistories_To] CHECK ([ToStatus] IN ('Draft','PendingReview','NeedUpdate','Approved','Rejected','Locked')),
        CONSTRAINT [FK_BrandProfileStatusHistories_Profile] FOREIGN KEY ([BrandProfileId]) REFERENCES [dbo].[BrandProfiles]([Id])
    );
    CREATE INDEX [IX_BrandProfileStatusHistories_Profile_Created] ON [dbo].[BrandProfileStatusHistories]([BrandProfileId],[CreatedAtUtc]);
END;

IF OBJECT_ID('dbo.KocProfileStatusHistories','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KocProfileStatusHistories] (
        [Id] UNIQUEIDENTIFIER NOT NULL,[KocProfileId] UNIQUEIDENTIFIER NOT NULL,[FromStatus] NVARCHAR(30) NULL,[ToStatus] NVARCHAR(30) NOT NULL,
        [ReviewNote] NVARCHAR(1000) NULL,[CreatedAtUtc] DATETIME2(0) NOT NULL,
        CONSTRAINT [PK_KocProfileStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_KocProfileStatusHistories_From] CHECK ([FromStatus] IS NULL OR [FromStatus] IN ('Draft','PendingReview','NeedUpdate','Approved','Rejected','Locked')),
        CONSTRAINT [CK_KocProfileStatusHistories_To] CHECK ([ToStatus] IN ('Draft','PendingReview','NeedUpdate','Approved','Rejected','Locked')),
        CONSTRAINT [FK_KocProfileStatusHistories_Profile] FOREIGN KEY ([KocProfileId]) REFERENCES [dbo].[KocProfiles]([Id])
    );
    CREATE INDEX [IX_KocProfileStatusHistories_Profile_Created] ON [dbo].[KocProfileStatusHistories]([KocProfileId],[CreatedAtUtc]);
END;

INSERT INTO [dbo].[BrandProfileStatusHistories] ([Id],[BrandProfileId],[FromStatus],[ToStatus],[ReviewNote],[CreatedAtUtc])
SELECT NEWID(),p.[Id],NULL,p.[Status],COALESCE(p.[ReviewNote],N'Migrated snapshot'),COALESCE(p.[UpdatedAtUtc],p.[CreatedAtUtc]) FROM [dbo].[BrandProfiles] p
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[BrandProfileStatusHistories] h WHERE h.[BrandProfileId]=p.[Id]);
INSERT INTO [dbo].[KocProfileStatusHistories] ([Id],[KocProfileId],[FromStatus],[ToStatus],[ReviewNote],[CreatedAtUtc])
SELECT NEWID(),p.[Id],NULL,p.[Status],COALESCE(p.[ReviewNote],N'Migrated snapshot'),COALESCE(p.[UpdatedAtUtc],p.[CreatedAtUtc]) FROM [dbo].[KocProfiles] p
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[KocProfileStatusHistories] h WHERE h.[KocProfileId]=p.[Id]);

EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_BrandProfileStatusHistories_AppendOnly] ON [dbo].[BrandProfileStatusHistories] INSTEAD OF UPDATE, DELETE AS BEGIN THROW 51017, ''BrandProfileStatusHistories is append-only.'', 1; END;');
EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_KocProfileStatusHistories_AppendOnly] ON [dbo].[KocProfileStatusHistories] INSTEAD OF UPDATE, DELETE AS BEGIN THROW 51018, ''KocProfileStatusHistories is append-only.'', 1; END;');

EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_BrandProfiles_CaptureStatusHistory] ON [dbo].[BrandProfiles] AFTER INSERT, UPDATE AS BEGIN SET NOCOUNT ON; INSERT INTO [dbo].[BrandProfileStatusHistories] ([Id],[BrandProfileId],[FromStatus],[ToStatus],[ReviewNote],[CreatedAtUtc]) SELECT NEWID(),i.[Id],d.[Status],i.[Status],i.[ReviewNote],SYSUTCDATETIME() FROM inserted i LEFT JOIN deleted d ON d.[Id]=i.[Id] WHERE d.[Id] IS NULL OR ISNULL(d.[Status],'''')<>ISNULL(i.[Status],''''); END;');
EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_KocProfiles_CaptureStatusHistory] ON [dbo].[KocProfiles] AFTER INSERT, UPDATE AS BEGIN SET NOCOUNT ON; INSERT INTO [dbo].[KocProfileStatusHistories] ([Id],[KocProfileId],[FromStatus],[ToStatus],[ReviewNote],[CreatedAtUtc]) SELECT NEWID(),i.[Id],d.[Status],i.[Status],i.[ReviewNote],SYSUTCDATETIME() FROM inserted i LEFT JOIN deleted d ON d.[Id]=i.[Id] WHERE d.[Id] IS NULL OR ISNULL(d.[Status],'''')<>ISNULL(i.[Status],''''); END;');

COMMIT TRANSACTION;
