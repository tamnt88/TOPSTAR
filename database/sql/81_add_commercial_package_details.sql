SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.CampaignPackagePolicies','SalePrice') IS NULL
    ALTER TABLE dbo.CampaignPackagePolicies ADD SalePrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_CampaignPackagePolicies_SalePrice DEFAULT(0) WITH VALUES;
IF COL_LENGTH('dbo.CampaignPackagePolicies','Description') IS NULL
    ALTER TABLE dbo.CampaignPackagePolicies ADD Description NVARCHAR(1000) NOT NULL CONSTRAINT DF_CampaignPackagePolicies_Description DEFAULT(N'Gói dịch vụ KOC dành cho Brand') WITH VALUES;
IF COL_LENGTH('dbo.CampaignPackagePolicies','Benefits') IS NULL
    ALTER TABLE dbo.CampaignPackagePolicies ADD Benefits NVARCHAR(2000) NOT NULL CONSTRAINT DF_CampaignPackagePolicies_Benefits DEFAULT(N'Tư vấn và hỗ trợ triển khai chiến dịch') WITH VALUES;
IF COL_LENGTH('dbo.CampaignPackagePolicies','ValidityDays') IS NULL
    ALTER TABLE dbo.CampaignPackagePolicies ADD ValidityDays INT NULL;
IF COL_LENGTH('dbo.CampaignPackagePolicies','IsFeatured') IS NULL
    ALTER TABLE dbo.CampaignPackagePolicies ADD IsFeatured BIT NOT NULL CONSTRAINT DF_CampaignPackagePolicies_IsFeatured DEFAULT(0) WITH VALUES;
IF COL_LENGTH('dbo.CampaignPackagePolicies','DisplayOrder') IS NULL
    ALTER TABLE dbo.CampaignPackagePolicies ADD DisplayOrder INT NOT NULL CONSTRAINT DF_CampaignPackagePolicies_DisplayOrder DEFAULT(0) WITH VALUES;

IF COL_LENGTH('dbo.CampaignPackagePolicyHistories','SalePrice') IS NULL
    ALTER TABLE dbo.CampaignPackagePolicyHistories ADD SalePrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_CampaignPackagePolicyHistories_SalePrice DEFAULT(0) WITH VALUES;
IF COL_LENGTH('dbo.CampaignPackagePolicyHistories','Description') IS NULL
    ALTER TABLE dbo.CampaignPackagePolicyHistories ADD Description NVARCHAR(1000) NOT NULL CONSTRAINT DF_CampaignPackagePolicyHistories_Description DEFAULT(N'') WITH VALUES;
IF COL_LENGTH('dbo.CampaignPackagePolicyHistories','Benefits') IS NULL
    ALTER TABLE dbo.CampaignPackagePolicyHistories ADD Benefits NVARCHAR(2000) NOT NULL CONSTRAINT DF_CampaignPackagePolicyHistories_Benefits DEFAULT(N'') WITH VALUES;
IF COL_LENGTH('dbo.CampaignPackagePolicyHistories','ValidityDays') IS NULL
    ALTER TABLE dbo.CampaignPackagePolicyHistories ADD ValidityDays INT NULL;
IF COL_LENGTH('dbo.CampaignPackagePolicyHistories','IsFeatured') IS NULL
    ALTER TABLE dbo.CampaignPackagePolicyHistories ADD IsFeatured BIT NOT NULL CONSTRAINT DF_CampaignPackagePolicyHistories_IsFeatured DEFAULT(0) WITH VALUES;
IF COL_LENGTH('dbo.CampaignPackagePolicyHistories','DisplayOrder') IS NULL
    ALTER TABLE dbo.CampaignPackagePolicyHistories ADD DisplayOrder INT NOT NULL CONSTRAINT DF_CampaignPackagePolicyHistories_DisplayOrder DEFAULT(0) WITH VALUES;

COMMIT TRANSACTION;
