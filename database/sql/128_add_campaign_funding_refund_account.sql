SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.CampaignFundings', N'RefundBankName') IS NULL
    ALTER TABLE dbo.CampaignFundings ADD RefundBankName nvarchar(200) NULL;

IF COL_LENGTH(N'dbo.CampaignFundings', N'RefundAccountNumber') IS NULL
    ALTER TABLE dbo.CampaignFundings ADD RefundAccountNumber nvarchar(100) NULL;

IF COL_LENGTH(N'dbo.CampaignFundings', N'RefundAccountHolder') IS NULL
    ALTER TABLE dbo.CampaignFundings ADD RefundAccountHolder nvarchar(200) NULL;

COMMIT TRANSACTION;

IF COL_LENGTH(N'dbo.CampaignFundings', N'RefundBankName') IS NULL
   OR COL_LENGTH(N'dbo.CampaignFundings', N'RefundAccountNumber') IS NULL
   OR COL_LENGTH(N'dbo.CampaignFundings', N'RefundAccountHolder') IS NULL
    THROW 51000, N'Không thể bổ sung thông tin tài khoản nhận hoàn cho quỹ chiến dịch.', 1;

PRINT N'OK - Đã bổ sung tài khoản nhận hoàn độc lập cho quỹ chiến dịch.';
