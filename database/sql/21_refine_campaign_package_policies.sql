SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM [dbo].[CampaignPackagePolicies] WHERE [Code]='ASSIGNED_UNDER_20M')
   AND NOT EXISTS (SELECT 1 FROM [dbo].[CampaignPackagePolicies] WHERE [Code]='ASSIGNED_UNDER_5M')
BEGIN
    UPDATE [dbo].[CampaignPackagePolicies]
    SET [Code]='ASSIGNED_UNDER_5M', [Name]=N'Admin phân công KOC - dưới 5 triệu', [MinBudgetAmount]=0, [MaxBudgetAmount]=4999999.99, [BrandSelectionEnabled]=0, [SelectionQuota]=NULL, [UpdatedAtUtc]=SYSUTCDATETIME()
    WHERE [Code]='ASSIGNED_UNDER_20M';
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[CampaignPackagePolicies] WHERE [Code]='ASSIGNED_5_TO_20M')
BEGIN
    INSERT INTO [dbo].[CampaignPackagePolicies]
        ([Id],[Code],[Name],[MinBudgetAmount],[MaxBudgetAmount],[BrandSelectionEnabled],[SelectionQuota],[IsActive],[CreatedAtUtc])
    VALUES
        (NEWID(),'ASSIGNED_5_TO_20M',N'Admin phân công KOC - từ 5 đến dưới 20 triệu',5000000,19999999.99,0,NULL,1,SYSUTCDATETIME());
END;

COMMIT TRANSACTION;
