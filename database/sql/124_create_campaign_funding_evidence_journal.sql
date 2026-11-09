/* TOPSTAR - Append-only Admin evidence journal for campaign funding. Safe to rerun. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.CampaignFundingEvidences','U') IS NULL
BEGIN
    CREATE TABLE dbo.CampaignFundingEvidences
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CampaignFundingEvidences PRIMARY KEY,
        FundingId UNIQUEIDENTIFIER NOT NULL,
        EvidenceType NVARCHAR(40) NOT NULL,
        FileUrl NVARCHAR(1000) NOT NULL,
        Note NVARCHAR(500) NOT NULL,
        Reference NVARCHAR(200) NULL,
        Amount DECIMAL(18,2) NULL,
        ActorUserId UNIQUEIDENTIFIER NOT NULL,
        CreatedAtUtc DATETIME2(7) NOT NULL,
        CONSTRAINT FK_CampaignFundingEvidences_CampaignFundings_FundingId
            FOREIGN KEY(FundingId) REFERENCES dbo.CampaignFundings(Id),
        CONSTRAINT CK_CampaignFundingEvidences_Type
            CHECK(EvidenceType IN (N'BrandPayment',N'Reconciliation',N'Refund')),
        CONSTRAINT CK_CampaignFundingEvidences_File
            CHECK(
                (EvidenceType = N'BrandPayment' AND FileUrl LIKE N'/uploads/payment-proofs/%')
                OR
                (EvidenceType IN (N'Reconciliation',N'Refund') AND FileUrl LIKE N'/uploads/campaign-funding-evidence/%')
            ),
        CONSTRAINT CK_CampaignFundingEvidences_Note
            CHECK(LEN(Note) BETWEEN 10 AND 500),
        CONSTRAINT CK_CampaignFundingEvidences_Amount
            CHECK(Amount IS NULL OR Amount > 0)
    );
END;
GO

IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.CampaignFundingEvidences') AND name='CK_CampaignFundingEvidences_Type')
    ALTER TABLE dbo.CampaignFundingEvidences DROP CONSTRAINT CK_CampaignFundingEvidences_Type;
ALTER TABLE dbo.CampaignFundingEvidences WITH CHECK ADD CONSTRAINT CK_CampaignFundingEvidences_Type
    CHECK(EvidenceType IN (N'BrandPayment',N'Reconciliation',N'Refund'));
GO

IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.CampaignFundingEvidences') AND name='CK_CampaignFundingEvidences_File')
    ALTER TABLE dbo.CampaignFundingEvidences DROP CONSTRAINT CK_CampaignFundingEvidences_File;
ALTER TABLE dbo.CampaignFundingEvidences WITH CHECK ADD CONSTRAINT CK_CampaignFundingEvidences_File
    CHECK(
        (EvidenceType = N'BrandPayment' AND FileUrl LIKE N'/uploads/payment-proofs/%')
        OR
        (EvidenceType IN (N'Reconciliation',N'Refund') AND FileUrl LIKE N'/uploads/campaign-funding-evidence/%')
    );
GO

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CampaignFundingEvidences') AND name='IX_CampaignFundingEvidences_FundingId_CreatedAtUtc')
    CREATE INDEX IX_CampaignFundingEvidences_FundingId_CreatedAtUtc
        ON dbo.CampaignFundingEvidences(FundingId,CreatedAtUtc DESC);
GO

INSERT dbo.CampaignFundingEvidences(Id,FundingId,EvidenceType,FileUrl,Note,Reference,Amount,ActorUserId,CreatedAtUtc)
SELECT NEWID(),f.Id,N'BrandPayment',f.PaymentProofUrl,
       CASE WHEN LEN(ISNULL(f.PaymentProofNote,N'')) BETWEEN 10 AND 500 THEN f.PaymentProofNote ELSE N'Chứng từ chuyển khoản Brand đã gửi.' END,
       NULL,NULL,b.UserId,COALESCE(f.PaymentProofSubmittedAtUtc,f.UpdatedAtUtc,f.CreatedAtUtc)
FROM dbo.CampaignFundings f
JOIN dbo.Campaigns c ON c.Id=f.CampaignId
JOIN dbo.BrandProfiles b ON b.Id=c.BrandProfileId
WHERE f.PaymentProofUrl IS NOT NULL
  AND NOT EXISTS(SELECT 1 FROM dbo.CampaignFundingEvidences e WHERE e.FundingId=f.Id AND e.EvidenceType=N'BrandPayment' AND e.FileUrl=f.PaymentProofUrl);
GO

CREATE OR ALTER TRIGGER dbo.TR_CampaignFundingEvidences_AppendOnly
ON dbo.CampaignFundingEvidences AFTER UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM deleted)
        THROW 51000,N'Chứng từ quỹ chiến dịch là lịch sử bất biến, không được sửa hoặc xóa.',1;
END;
GO

PRINT N'OK - Đã tạo journal chứng từ quỹ chiến dịch bất biến.';
