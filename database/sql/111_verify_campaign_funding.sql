SET NOCOUNT ON;

DECLARE @errors TABLE(Message NVARCHAR(500));

IF OBJECT_ID('dbo.CampaignFeePolicies','U') IS NULL INSERT @errors VALUES(N'Thiếu bảng CampaignFeePolicies.');
IF OBJECT_ID('dbo.CampaignFeePolicyHistories','U') IS NULL INSERT @errors VALUES(N'Thiếu bảng CampaignFeePolicyHistories.');
IF OBJECT_ID('dbo.CampaignFundings','U') IS NULL INSERT @errors VALUES(N'Thiếu bảng CampaignFundings.');
IF OBJECT_ID('dbo.CampaignFundings','U') IS NOT NULL AND COL_LENGTH('dbo.CampaignFundings','PaymentMethod') IS NULL INSERT @errors VALUES(N'Thiếu snapshot phương thức thanh toán chiến dịch.');
IF OBJECT_ID('dbo.CampaignFundingAllocations','U') IS NULL INSERT @errors VALUES(N'Thiếu bảng CampaignFundingAllocations.');
IF OBJECT_ID('dbo.CampaignFundLedgers','U') IS NULL INSERT @errors VALUES(N'Thiếu bảng CampaignFundLedgers.');

IF OBJECT_ID('dbo.CampaignFeePolicies','U') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM dbo.CampaignFeePolicies WHERE Code='DEFAULT' AND IsActive=1)
 INSERT @errors VALUES(N'Chưa có chính sách phí DEFAULT đang hoạt động.');

IF OBJECT_ID('dbo.CampaignFundings','U') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CampaignFundings') AND name='UX_CampaignFundings_OrderNumber' AND is_unique=1 AND has_filter=1)
 INSERT @errors VALUES(N'Thiếu unique filtered index cho mã yêu cầu thanh toán.');

IF OBJECT_ID('dbo.TR_CampaignFundingAllocations_Immutable','TR') IS NULL INSERT @errors VALUES(N'Thiếu trigger khóa phân bổ thù lao.');
IF OBJECT_ID('dbo.TR_CampaignFundings_SnapshotImmutable','TR') IS NULL INSERT @errors VALUES(N'Thiếu trigger khóa snapshot ký quỹ.');
IF OBJECT_ID('dbo.TR_CampaignFundLedgers_AppendOnly','TR') IS NULL INSERT @errors VALUES(N'Thiếu trigger bảo vệ sổ quỹ.');
IF OBJECT_ID('dbo.TR_CampaignFeePolicyHistories_AppendOnly','TR') IS NULL INSERT @errors VALUES(N'Thiếu trigger bảo vệ lịch sử chính sách phí.');

IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_ADMIN_CAMPAIGN_FEE_POLICY') INSERT @errors VALUES(N'Thiếu menu cấu hình phí chiến dịch.');
IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_ADMIN_CAMPAIGN_FUNDING') INSERT @errors VALUES(N'Thiếu menu quản lý ký quỹ chiến dịch.');
IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_BRAND_CAMPAIGN_FUNDING') INSERT @errors VALUES(N'Thiếu menu thanh toán chiến dịch của Brand.');

IF EXISTS(SELECT 1 FROM @errors)
BEGIN
 SELECT Message AS VerificationError FROM @errors;
 THROW 51000, N'Kiểm tra campaign funding không đạt. Xem danh sách VerificationError.', 1;
END;

SELECT N'OK' AS Result,
       (SELECT COUNT(*) FROM dbo.CampaignFeePolicies WHERE IsActive=1) AS ActivePolicies,
       (SELECT COUNT(*) FROM dbo.CampaignFundings) AS CampaignFundings,
       (SELECT COUNT(*) FROM dbo.CampaignFundingAllocations) AS Allocations,
       (SELECT COUNT(*) FROM dbo.CampaignFundLedgers) AS LedgerEntries;
