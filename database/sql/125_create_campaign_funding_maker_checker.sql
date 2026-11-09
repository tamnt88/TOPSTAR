SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.CampaignFundingApprovals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CampaignFundingApprovals
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_CampaignFundingApprovals PRIMARY KEY,
        FundingId uniqueidentifier NOT NULL,
        ActionType nvarchar(40) NOT NULL,
        Amount decimal(18,2) NOT NULL,
        Reference nvarchar(200) NOT NULL,
        Status nvarchar(20) NOT NULL CONSTRAINT DF_CampaignFundingApprovals_Status DEFAULT N'Pending',
        RequestedByUserId uniqueidentifier NOT NULL,
        DecidedByUserId uniqueidentifier NULL,
        CreatedAtUtc datetime2 NOT NULL,
        DecidedAtUtc datetime2 NULL,
        CONSTRAINT FK_CampaignFundingApprovals_Funding FOREIGN KEY(FundingId) REFERENCES dbo.CampaignFundings(Id),
        CONSTRAINT CK_CampaignFundingApprovals_Action CHECK(ActionType IN(N'ConfirmPaid',N'ConfirmRefund')),
        CONSTRAINT CK_CampaignFundingApprovals_Status CHECK(Status IN(N'Pending',N'Approved',N'Rejected')),
        CONSTRAINT CK_CampaignFundingApprovals_Amount CHECK(Amount > 0),
        CONSTRAINT CK_CampaignFundingApprovals_Checker CHECK(DecidedByUserId IS NULL OR DecidedByUserId <> RequestedByUserId)
    );
END;

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CampaignFundingApprovals') AND name=N'UX_CampaignFundingApprovals_Pending')
    CREATE UNIQUE INDEX UX_CampaignFundingApprovals_Pending ON dbo.CampaignFundingApprovals(FundingId,ActionType) WHERE Status=N'Pending';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CampaignFundingApprovals') AND name=N'IX_CampaignFundingApprovals_Created')
    CREATE INDEX IX_CampaignFundingApprovals_Created ON dbo.CampaignFundingApprovals(CreatedAtUtc DESC);

COMMIT TRANSACTION;
PRINT N'OK - Đã tạo hàng đợi maker-checker quỹ chiến dịch.';
