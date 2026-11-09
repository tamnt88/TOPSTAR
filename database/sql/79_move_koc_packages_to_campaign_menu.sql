SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @CampaignGroupId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id FROM dbo.Menus WHERE Code = 'MENU_ADMIN_CAMPAIGN_GROUP'
);

IF @CampaignGroupId IS NULL
    THROW 51000, N'Không tìm thấy nhóm menu Chiến dịch của Admin.', 1;

UPDATE dbo.Menus
SET ParentId = @CampaignGroupId,
    Title = N'Gói KOC cho Brand',
    Icon = 'fa-solid fa-box-open',
    OrderNo = 7,
    IsVisible = 1,
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE Code = 'MENU_ADMIN_SETTINGS_PACKAGES';

COMMIT TRANSACTION;
