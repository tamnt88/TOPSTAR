SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.CampaignFundingEvidenceReviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CampaignFundingEvidenceReviews
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_CampaignFundingEvidenceReviews PRIMARY KEY,
        EvidenceId uniqueidentifier NOT NULL,
        Status nvarchar(30) NOT NULL,
        Reason nvarchar(500) NOT NULL,
        ActorUserId uniqueidentifier NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        CONSTRAINT FK_CampaignFundingEvidenceReviews_Evidence FOREIGN KEY(EvidenceId) REFERENCES dbo.CampaignFundingEvidences(Id),
        CONSTRAINT FK_CampaignFundingEvidenceReviews_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_CampaignFundingEvidenceReviews_Status CHECK(Status IN(N'Accepted',N'SupplementRequired',N'Rejected')),
        CONSTRAINT CK_CampaignFundingEvidenceReviews_Reason CHECK(LEN(LTRIM(RTRIM(Reason))) BETWEEN 10 AND 500)
    );
END;

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CampaignFundingEvidenceReviews') AND name=N'IX_CampaignFundingEvidenceReviews_Evidence_Created')
    CREATE INDEX IX_CampaignFundingEvidenceReviews_Evidence_Created ON dbo.CampaignFundingEvidenceReviews(EvidenceId,CreatedAtUtc DESC);

COMMIT TRANSACTION;
GO

CREATE OR ALTER TRIGGER dbo.TR_CampaignFundingEvidenceReviews_AppendOnly
ON dbo.CampaignFundingEvidenceReviews AFTER UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    THROW 51000,N'Lịch sử xét duyệt chứng từ chỉ được bổ sung, không được sửa hoặc xóa.',1;
END;
GO

PRINT N'OK - Đã tạo lịch sử xét duyệt từng phiên bản chứng từ quỹ chiến dịch.';
