-- 20260406_add_brand_legal_tax_columns.sql
-- Add dedicated Brand legal/tax columns (no JSON storage)

IF COL_LENGTH('BrandProfiles', 'TaxCompanyName') IS NULL
BEGIN
    ALTER TABLE [dbo].[BrandProfiles]
    ADD [TaxCompanyName] NVARCHAR(255) NULL;
END

IF COL_LENGTH('BrandProfiles', 'TaxCode') IS NULL
BEGIN
    ALTER TABLE [dbo].[BrandProfiles]
    ADD [TaxCode] NVARCHAR(64) NULL;
END

IF COL_LENGTH('BrandProfiles', 'TaxInvoiceAddress') IS NULL
BEGIN
    ALTER TABLE [dbo].[BrandProfiles]
    ADD [TaxInvoiceAddress] NVARCHAR(500) NULL;
END

IF COL_LENGTH('BrandProfiles', 'BusinessLicenseFileUrl') IS NULL
BEGIN
    ALTER TABLE [dbo].[BrandProfiles]
    ADD [BusinessLicenseFileUrl] NVARCHAR(500) NULL;
END
