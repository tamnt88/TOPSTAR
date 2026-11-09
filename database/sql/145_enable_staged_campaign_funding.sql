SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.CampaignFundings', 'U') IS NULL
    THROW 51145, N'Thiếu bảng CampaignFundings. Hãy chạy các migration tài chính trước.', 1;

-- Một chiến dịch có thể có nhiều đợt đã thanh toán. Chỉ giới hạn một đợt đang soạn/chờ thanh toán.
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.CampaignFundings') AND name = 'UX_CampaignFundings_ActiveCampaign')
    DROP INDEX UX_CampaignFundings_ActiveCampaign ON dbo.CampaignFundings;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.CampaignFundings') AND name = 'UX_CampaignFundings_OpenBatch')
    DROP INDEX UX_CampaignFundings_OpenBatch ON dbo.CampaignFundings;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.CampaignFundings') AND name = 'IX_CampaignFundings_Campaign_Status')
    CREATE INDEX IX_CampaignFundings_Campaign_Status ON dbo.CampaignFundings(CampaignId, Status, VersionNo DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.CampaignFundings') AND name = 'UX_CampaignFundings_Campaign_Version')
    CREATE UNIQUE INDEX UX_CampaignFundings_Campaign_Version ON dbo.CampaignFundings(CampaignId, VersionNo);

COMMIT TRANSACTION;
GO
