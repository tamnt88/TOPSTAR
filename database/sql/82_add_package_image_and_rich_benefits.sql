SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.CampaignPackagePolicies','ImageUrl') IS NULL
    ALTER TABLE dbo.CampaignPackagePolicies ADD ImageUrl NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.CampaignPackagePolicyHistories','ImageUrl') IS NULL
    ALTER TABLE dbo.CampaignPackagePolicyHistories ADD ImageUrl NVARCHAR(500) NULL;

ALTER TABLE dbo.CampaignPackagePolicies ALTER COLUMN Benefits NVARCHAR(4000) NOT NULL;
ALTER TABLE dbo.CampaignPackagePolicyHistories ALTER COLUMN Benefits NVARCHAR(4000) NOT NULL;

COMMIT TRANSACTION;
