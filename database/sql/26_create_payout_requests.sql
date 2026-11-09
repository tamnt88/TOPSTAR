SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.KocPayoutAccounts','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KocPayoutAccounts] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [KocProfileId] UNIQUEIDENTIFIER NOT NULL,
        [BankCode] NVARCHAR(20) NOT NULL,
        [AccountHolderName] NVARCHAR(150) NOT NULL,
        [AccountNumberCiphertext] NVARCHAR(2000) NOT NULL,
        [AccountNumberLast4] NVARCHAR(4) NOT NULL,
        [CreatedAtUtc] DATETIME2(0) NOT NULL,
        [UpdatedAtUtc] DATETIME2(0) NULL,
        CONSTRAINT [PK_KocPayoutAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [UX_KocPayoutAccounts_KocProfile] UNIQUE ([KocProfileId]),
        CONSTRAINT [FK_KocPayoutAccounts_KocProfile] FOREIGN KEY ([KocProfileId]) REFERENCES [dbo].[KocProfiles]([Id])
    );
END;

IF OBJECT_ID('dbo.PayoutRequests','U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PayoutRequests] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [KocProfileId] UNIQUEIDENTIFIER NOT NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [BankCode] NVARCHAR(20) NOT NULL,
        [AccountHolderName] NVARCHAR(150) NOT NULL,
        [AccountNumberCiphertext] NVARCHAR(2000) NOT NULL,
        [AccountNumberLast4] NVARCHAR(4) NOT NULL,
        [PayoutReference] NVARCHAR(200) NULL,
        [FailureReason] NVARCHAR(200) NULL,
        [ProcessedByUserId] UNIQUEIDENTIFIER NULL,
        [RequestedAtUtc] DATETIME2(0) NOT NULL,
        [ProcessedAtUtc] DATETIME2(0) NULL,
        [CreatedAtUtc] DATETIME2(0) NOT NULL,
        [UpdatedAtUtc] DATETIME2(0) NULL,
        CONSTRAINT [PK_PayoutRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PayoutRequests_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_PayoutRequests_Status] CHECK ([Status] IN ('WithdrawRequested','PaidOut','Failed')),
        CONSTRAINT [FK_PayoutRequests_KocProfile] FOREIGN KEY ([KocProfileId]) REFERENCES [dbo].[KocProfiles]([Id]),
        CONSTRAINT [FK_PayoutRequests_ProcessedByUser] FOREIGN KEY ([ProcessedByUserId]) REFERENCES [dbo].[Users]([Id])
    );
    CREATE INDEX [IX_PayoutRequests_Koc_Status_Requested]
        ON [dbo].[PayoutRequests]([KocProfileId],[Status],[RequestedAtUtc] DESC);
    CREATE INDEX [IX_PayoutRequests_Status_Requested]
        ON [dbo].[PayoutRequests]([Status],[RequestedAtUtc]);
END;

COMMIT TRANSACTION;
