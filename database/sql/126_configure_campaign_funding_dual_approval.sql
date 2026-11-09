SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.CampaignFeePolicies', N'DualApprovalThreshold') IS NULL
BEGIN
    ALTER TABLE dbo.CampaignFeePolicies
        ADD DualApprovalThreshold decimal(18,2) NOT NULL
            CONSTRAINT DF_CampaignFeePolicies_DualApprovalThreshold DEFAULT (50000000) WITH VALUES;
END;

IF COL_LENGTH(N'dbo.CampaignFeePolicyHistories', N'DualApprovalThreshold') IS NULL
BEGIN
    ALTER TABLE dbo.CampaignFeePolicyHistories
        ADD DualApprovalThreshold decimal(18,2) NOT NULL
            CONSTRAINT DF_CampaignFeePolicyHistories_DualApprovalThreshold DEFAULT (50000000) WITH VALUES;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.CampaignFeePolicies')
      AND name = N'CK_CampaignFeePolicies_DualApprovalThreshold'
)
BEGIN
    EXEC sys.sp_executesql N'
        ALTER TABLE dbo.CampaignFeePolicies WITH CHECK
            ADD CONSTRAINT CK_CampaignFeePolicies_DualApprovalThreshold
                CHECK (DualApprovalThreshold >= 0);';
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.CampaignFeePolicyHistories')
      AND name = N'CK_CampaignFeePolicyHistories_DualApprovalThreshold'
)
BEGIN
    EXEC sys.sp_executesql N'
        ALTER TABLE dbo.CampaignFeePolicyHistories WITH CHECK
            ADD CONSTRAINT CK_CampaignFeePolicyHistories_DualApprovalThreshold
                CHECK (DualApprovalThreshold >= 0);';
END;

IF COL_LENGTH(N'dbo.CampaignFeePolicies', N'DualApprovalThreshold') IS NULL
   OR COL_LENGTH(N'dbo.CampaignFeePolicyHistories', N'DualApprovalThreshold') IS NULL
   OR NOT EXISTS
      (
          SELECT 1 FROM sys.check_constraints
          WHERE parent_object_id = OBJECT_ID(N'dbo.CampaignFeePolicies')
            AND name = N'CK_CampaignFeePolicies_DualApprovalThreshold'
      )
   OR NOT EXISTS
      (
          SELECT 1 FROM sys.check_constraints
          WHERE parent_object_id = OBJECT_ID(N'dbo.CampaignFeePolicyHistories')
            AND name = N'CK_CampaignFeePolicyHistories_DualApprovalThreshold'
      )
    THROW 51000, N'Không thể hoàn tất cấu hình ngưỡng duyệt hai lớp.', 1;

COMMIT TRANSACTION;
PRINT N'OK - Đã bổ sung ngưỡng duyệt hai lớp có cấu hình và lịch sử.';
