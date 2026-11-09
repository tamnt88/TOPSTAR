SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.SubmissionHistories','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SubmissionHistories] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [SubmissionId] UNIQUEIDENTIFIER NOT NULL,
        [EventType] NVARCHAR(30) NOT NULL,
        [RevisionNo] INT NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [SubmissionUrl] NVARCHAR(1000) NULL,
        [ReviewComment] NVARCHAR(2000) NULL,
        [ActorUserId] UNIQUEIDENTIFIER NULL,
        [CreatedAtUtc] DATETIME2(0) NOT NULL,
        CONSTRAINT [PK_SubmissionHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SubmissionHistories_EventType] CHECK ([EventType] IN ('MigratedSnapshot','Submitted','Reviewed')),
        CONSTRAINT [CK_SubmissionHistories_RevisionNo] CHECK ([RevisionNo] >= 0),
        CONSTRAINT [CK_SubmissionHistories_Status] CHECK ([Status] IN ('NotSubmitted','Submitted','NeedRevision','Approved','Rejected')),
        CONSTRAINT [FK_SubmissionHistories_Submission] FOREIGN KEY ([SubmissionId]) REFERENCES [dbo].[Submissions]([Id]),
        CONSTRAINT [FK_SubmissionHistories_ActorUser] FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Users]([Id])
    );
    CREATE INDEX [IX_SubmissionHistories_Submission_Created]
        ON [dbo].[SubmissionHistories]([SubmissionId],[CreatedAtUtc]);
END;

IF OBJECT_ID('dbo.WalletTransactionStatusHistories','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WalletTransactionStatusHistories] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [WalletTransactionId] UNIQUEIDENTIFIER NOT NULL,
        [FromStatus] NVARCHAR(30) NULL,
        [ToStatus] NVARCHAR(30) NOT NULL,
        [Reason] NVARCHAR(500) NULL,
        [ActorUserId] UNIQUEIDENTIFIER NULL,
        [CreatedAtUtc] DATETIME2(0) NOT NULL,
        CONSTRAINT [PK_WalletTransactionStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_WalletTransactionStatusHistories_From] CHECK ([FromStatus] IS NULL OR [FromStatus] IN ('Pending','Available','OnHold','PaidOut','Failed')),
        CONSTRAINT [CK_WalletTransactionStatusHistories_To] CHECK ([ToStatus] IN ('Pending','Available','OnHold','PaidOut','Failed')),
        CONSTRAINT [FK_WalletTransactionStatusHistories_Transaction] FOREIGN KEY ([WalletTransactionId]) REFERENCES [dbo].[WalletTransactions]([Id]),
        CONSTRAINT [FK_WalletTransactionStatusHistories_ActorUser] FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Users]([Id])
    );
    CREATE INDEX [IX_WalletTransactionStatusHistories_Transaction_Created]
        ON [dbo].[WalletTransactionStatusHistories]([WalletTransactionId],[CreatedAtUtc]);
END;

IF OBJECT_ID('dbo.PayoutRequestStatusHistories','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PayoutRequestStatusHistories] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [PayoutRequestId] UNIQUEIDENTIFIER NOT NULL,
        [FromStatus] NVARCHAR(30) NULL,
        [ToStatus] NVARCHAR(30) NOT NULL,
        [Note] NVARCHAR(500) NULL,
        [ActorUserId] UNIQUEIDENTIFIER NULL,
        [CreatedAtUtc] DATETIME2(0) NOT NULL,
        CONSTRAINT [PK_PayoutRequestStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PayoutRequestStatusHistories_From] CHECK ([FromStatus] IS NULL OR [FromStatus] IN ('WithdrawRequested','PaidOut','Failed')),
        CONSTRAINT [CK_PayoutRequestStatusHistories_To] CHECK ([ToStatus] IN ('WithdrawRequested','PaidOut','Failed')),
        CONSTRAINT [FK_PayoutRequestStatusHistories_Request] FOREIGN KEY ([PayoutRequestId]) REFERENCES [dbo].[PayoutRequests]([Id]),
        CONSTRAINT [FK_PayoutRequestStatusHistories_ActorUser] FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Users]([Id])
    );
    CREATE INDEX [IX_PayoutRequestStatusHistories_Request_Created]
        ON [dbo].[PayoutRequestStatusHistories]([PayoutRequestId],[CreatedAtUtc]);
END;

EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_SubmissionHistories_AppendOnly] ON [dbo].[SubmissionHistories] INSTEAD OF UPDATE, DELETE AS BEGIN THROW 51010, ''SubmissionHistories is append-only.'', 1; END;');
EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_WalletTransactionStatusHistories_AppendOnly] ON [dbo].[WalletTransactionStatusHistories] INSTEAD OF UPDATE, DELETE AS BEGIN THROW 51011, ''WalletTransactionStatusHistories is append-only.'', 1; END;');
EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_PayoutRequestStatusHistories_AppendOnly] ON [dbo].[PayoutRequestStatusHistories] INSTEAD OF UPDATE, DELETE AS BEGIN THROW 51012, ''PayoutRequestStatusHistories is append-only.'', 1; END;');

INSERT INTO [dbo].[SubmissionHistories] ([Id],[SubmissionId],[EventType],[RevisionNo],[Status],[SubmissionUrl],[ReviewComment],[ActorUserId],[CreatedAtUtc])
SELECT NEWID(),s.[Id],'MigratedSnapshot',s.[RevisionCount],s.[Status],s.[SubmissionUrl],s.[ReviewComment],NULL,COALESCE(s.[UpdatedAtUtc],s.[CreatedAtUtc])
FROM [dbo].[Submissions] s
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[SubmissionHistories] h WHERE h.[SubmissionId]=s.[Id]);

INSERT INTO [dbo].[WalletTransactionStatusHistories] ([Id],[WalletTransactionId],[FromStatus],[ToStatus],[Reason],[ActorUserId],[CreatedAtUtc])
SELECT NEWID(),w.[Id],NULL,w.[Status],N'Migrated snapshot',NULL,COALESCE(w.[UpdatedAtUtc],w.[CreatedAtUtc])
FROM [dbo].[WalletTransactions] w
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[WalletTransactionStatusHistories] h WHERE h.[WalletTransactionId]=w.[Id]);

INSERT INTO [dbo].[PayoutRequestStatusHistories] ([Id],[PayoutRequestId],[FromStatus],[ToStatus],[Note],[ActorUserId],[CreatedAtUtc])
SELECT NEWID(),p.[Id],NULL,p.[Status],COALESCE(p.[PayoutReference],p.[FailureReason],N'Migrated snapshot'),NULL,COALESCE(p.[UpdatedAtUtc],p.[CreatedAtUtc])
FROM [dbo].[PayoutRequests] p
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[PayoutRequestStatusHistories] h WHERE h.[PayoutRequestId]=p.[Id]);

COMMIT TRANSACTION;
