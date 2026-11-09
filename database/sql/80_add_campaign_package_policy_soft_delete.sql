SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.CampaignPackagePolicies','IsDeleted') IS NULL
BEGIN
    ALTER TABLE dbo.CampaignPackagePolicies
        ADD IsDeleted BIT NOT NULL
            CONSTRAINT DF_CampaignPackagePolicies_IsDeleted DEFAULT(0) WITH VALUES;
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.CampaignPackagePolicies')
      AND name = 'IX_CampaignPackagePolicies_ActiveRange'
)
BEGIN
    CREATE INDEX IX_CampaignPackagePolicies_ActiveRange
        ON dbo.CampaignPackagePolicies(IsDeleted,IsActive,MinBudgetAmount,MaxBudgetAmount);
END;

COMMIT TRANSACTION;
