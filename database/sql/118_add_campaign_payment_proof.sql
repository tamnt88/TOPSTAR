/* TOPSTAR - Brand bank-transfer payment proof (safe to rerun). */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.CampaignFundings','PaymentProofUrl') IS NULL
    ALTER TABLE dbo.CampaignFundings ADD PaymentProofUrl NVARCHAR(1000) NULL;
IF COL_LENGTH('dbo.CampaignFundings','PaymentProofNote') IS NULL
    ALTER TABLE dbo.CampaignFundings ADD PaymentProofNote NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.CampaignFundings','PaymentProofSubmittedAtUtc') IS NULL
    ALTER TABLE dbo.CampaignFundings ADD PaymentProofSubmittedAtUtc DATETIME2(7) NULL;
GO

IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.CampaignFundings') AND name='CK_CampaignFundings_PaymentProof')
    EXEC(N'ALTER TABLE dbo.CampaignFundings ADD CONSTRAINT CK_CampaignFundings_PaymentProof CHECK((PaymentProofUrl IS NULL AND PaymentProofSubmittedAtUtc IS NULL) OR (PaymentMethod=''BankTransfer'' AND PaymentProofUrl LIKE ''/uploads/payment-proofs/%'' AND LEN(PaymentProofNote) BETWEEN 10 AND 500 AND PaymentProofSubmittedAtUtc IS NOT NULL));');
GO

PRINT N'OK - Đã bổ sung bằng chứng chuyển khoản cho quỹ chiến dịch.';
