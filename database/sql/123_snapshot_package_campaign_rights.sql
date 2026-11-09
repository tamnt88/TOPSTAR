SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.BrandPackagePurchases','MinBudgetAmountSnapshot') IS NULL
        ALTER TABLE dbo.BrandPackagePurchases ADD MinBudgetAmountSnapshot decimal(18,2) NULL;
    IF COL_LENGTH('dbo.BrandPackagePurchases','MaxBudgetAmountSnapshot') IS NULL
        ALTER TABLE dbo.BrandPackagePurchases ADD MaxBudgetAmountSnapshot decimal(18,2) NULL;
    IF COL_LENGTH('dbo.BrandPackagePurchases','BrandSelectionEnabledSnapshot') IS NULL
        ALTER TABLE dbo.BrandPackagePurchases ADD BrandSelectionEnabledSnapshot bit NULL;
    IF COL_LENGTH('dbo.BrandPackagePurchases','SelectionQuotaSnapshot') IS NULL
        ALTER TABLE dbo.BrandPackagePurchases ADD SelectionQuotaSnapshot int NULL;

    EXEC sp_executesql N'
        UPDATE purchase
           SET MinBudgetAmountSnapshot = COALESCE(purchase.MinBudgetAmountSnapshot, policy.MinBudgetAmount),
               MaxBudgetAmountSnapshot = CASE WHEN purchase.MinBudgetAmountSnapshot IS NULL THEN policy.MaxBudgetAmount ELSE purchase.MaxBudgetAmountSnapshot END,
               BrandSelectionEnabledSnapshot = COALESCE(purchase.BrandSelectionEnabledSnapshot, policy.BrandSelectionEnabled),
               SelectionQuotaSnapshot = CASE WHEN purchase.BrandSelectionEnabledSnapshot IS NULL THEN policy.SelectionQuota ELSE purchase.SelectionQuotaSnapshot END
          FROM dbo.BrandPackagePurchases purchase
          JOIN dbo.CampaignPackagePolicies policy ON policy.Id = purchase.PackagePolicyId;

        IF EXISTS (SELECT 1 FROM dbo.BrandPackagePurchases WHERE MinBudgetAmountSnapshot IS NULL OR BrandSelectionEnabledSnapshot IS NULL)
            THROW 51000, N''Không thể snapshot quyền cho một hoặc nhiều đơn mua gói.'', 1;';

    EXEC(N'ALTER TABLE dbo.BrandPackagePurchases ALTER COLUMN MinBudgetAmountSnapshot decimal(18,2) NOT NULL;');
    EXEC(N'ALTER TABLE dbo.BrandPackagePurchases ALTER COLUMN BrandSelectionEnabledSnapshot bit NOT NULL;');

    IF COL_LENGTH('dbo.Campaigns','DesiredKocCount') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            UPDATE campaign
               SET DesiredKocCount = confirmed.KocCount
              FROM dbo.Campaigns campaign
              JOIN
              (
                  SELECT CampaignId, COUNT(*) AS KocCount
                    FROM dbo.Participations
                   WHERE Status IN (''Confirmed'',''Completed'')
                   GROUP BY CampaignId
              ) confirmed ON confirmed.CampaignId = campaign.Id
             WHERE confirmed.KocCount > campaign.DesiredKocCount;';
    END;

    COMMIT TRANSACTION;
    PRINT N'OK - Đã cố định quyền gói tại thời điểm mua.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
