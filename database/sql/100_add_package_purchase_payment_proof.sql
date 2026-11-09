SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.BrandPackagePurchases', 'PaymentMethod') IS NULL
    ALTER TABLE dbo.BrandPackagePurchases ADD PaymentMethod nvarchar(50) NOT NULL CONSTRAINT DF_BrandPackagePurchases_PaymentMethod DEFAULT N'BankTransfer';

IF COL_LENGTH('dbo.BrandPackagePurchases', 'PaymentProofUrl') IS NULL
    ALTER TABLE dbo.BrandPackagePurchases ADD PaymentProofUrl nvarchar(1000) NULL;

IF COL_LENGTH('dbo.BrandPackagePurchases', 'PaymentProofNote') IS NULL
    ALTER TABLE dbo.BrandPackagePurchases ADD PaymentProofNote nvarchar(500) NULL;

IF COL_LENGTH('dbo.BrandPackagePurchases', 'PaymentProofSubmittedAtUtc') IS NULL
    ALTER TABLE dbo.BrandPackagePurchases ADD PaymentProofSubmittedAtUtc datetime2 NULL;

COMMIT TRANSACTION;
