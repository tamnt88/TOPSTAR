/* TOPSTAR - Repair campaign-funding evidence file-path validation. Safe to rerun. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.CampaignFundingEvidences','U') IS NULL
    THROW 51000, N'Thiếu bảng dbo.CampaignFundingEvidences. Hãy chạy script 124 trước.', 1;
GO

BEGIN TRANSACTION;

IF EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.CampaignFundingEvidences')
      AND name = 'CK_CampaignFundingEvidences_File'
)
    ALTER TABLE dbo.CampaignFundingEvidences DROP CONSTRAINT CK_CampaignFundingEvidences_File;

ALTER TABLE dbo.CampaignFundingEvidences WITH CHECK
ADD CONSTRAINT CK_CampaignFundingEvidences_File
CHECK
(
    (EvidenceType = N'BrandPayment' AND FileUrl LIKE N'/uploads/payment-proofs/%')
    OR
    (EvidenceType IN (N'Reconciliation',N'Refund') AND FileUrl LIKE N'/uploads/campaign-funding-evidence/%')
);

COMMIT TRANSACTION;
GO

SELECT name, definition, is_disabled, is_not_trusted
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID('dbo.CampaignFundingEvidences')
  AND name = 'CK_CampaignFundingEvidences_File';
GO
