SET NOCOUNT ON;
DECLARE @errors TABLE(Message NVARCHAR(500));
IF COL_LENGTH('dbo.CampaignFundings','VersionNo') IS NULL INSERT @errors VALUES(N'Thiếu CampaignFundings.VersionNo.');
IF COL_LENGTH('dbo.CampaignFundings','CancellationReason') IS NULL INSERT @errors VALUES(N'Thiếu thông tin hủy yêu cầu.');
IF COL_LENGTH('dbo.CampaignFundings','RefundedAmount') IS NULL INSERT @errors VALUES(N'Thiếu thông tin hoàn tiền.');
IF COL_LENGTH('dbo.CampaignFundingAllocations','AcceptedAmount') IS NULL INSERT @errors VALUES(N'Thiếu số tiền nghiệm thu.');
IF COL_LENGTH('dbo.CampaignFundingAllocations','RefundedAmount') IS NULL INSERT @errors VALUES(N'Thiếu số tiền hoàn theo KOC.');
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CampaignFundings') AND name='UX_CampaignFundings_Campaign_Version') INSERT @errors VALUES(N'Thiếu unique index phiên bản quỹ.');
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CampaignFundings') AND name='UX_CampaignFundings_ActiveCampaign') INSERT @errors VALUES(N'Thiếu unique filtered index quỹ đang hoạt động.');
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CampaignFundingAllocations') AND name='UX_CampaignFundingAllocations_WalletTransaction') INSERT @errors VALUES(N'Thiếu chống tạo thu nhập trùng.');
IF OBJECT_ID('dbo.TR_CampaignFundings_SnapshotImmutable','TR') IS NULL INSERT @errors VALUES(N'Thiếu trigger bất biến snapshot quỹ.');
IF OBJECT_ID('dbo.TR_CampaignFundingAllocations_Immutable','TR') IS NULL INSERT @errors VALUES(N'Thiếu trigger bất biến phân bổ.');
IF EXISTS(SELECT 1 FROM @errors) BEGIN SELECT Message AS VerificationError FROM @errors; THROW 51000,N'Kiểm tra vòng đời quỹ chiến dịch không đạt.',1; END;
EXEC(N'SELECT N''OK'' Result,
 (SELECT COUNT(*) FROM dbo.CampaignFundings) FundingVersions,
 (SELECT COUNT(*) FROM dbo.CampaignFundings WHERE Status=''Pending'') PendingPayments,
 (SELECT COUNT(*) FROM dbo.CampaignFundingAllocations WHERE ReleasedAtUtc IS NOT NULL) SettledAllocations,
 (SELECT ISNULL(SUM(RefundedAmount),0) FROM dbo.CampaignFundings) RefundedAmount;');
