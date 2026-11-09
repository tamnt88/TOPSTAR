/* TOPSTAR - VNPay checkout and unique payment receipts. Run before deploying binaries.
   Safe to rerun. Does not enable a gateway or copy credentials from another website. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.PaymentMethodConfigurations',N'U') IS NULL
    THROW 51000,N'Thiếu bảng PaymentMethodConfigurations.',1;
BEGIN TRY
BEGIN TRANSACTION;
IF COL_LENGTH(N'dbo.PaymentMethodConfigurations',N'VnPayEnabled') IS NULL
 ALTER TABLE dbo.PaymentMethodConfigurations ADD VnPayEnabled bit NOT NULL CONSTRAINT DF_PaymentConfig_VnPayEnabled DEFAULT 0;
IF COL_LENGTH(N'dbo.PaymentMethodConfigurations',N'VnPayEnvironment') IS NULL
 ALTER TABLE dbo.PaymentMethodConfigurations ADD VnPayEnvironment nvarchar(20) NOT NULL CONSTRAINT DF_PaymentConfig_VnPayEnv DEFAULT N'Test';
IF COL_LENGTH(N'dbo.PaymentMethodConfigurations',N'VnPayTmnCode') IS NULL
 ALTER TABLE dbo.PaymentMethodConfigurations ADD VnPayTmnCode nvarchar(100) NULL;
IF COL_LENGTH(N'dbo.PaymentMethodConfigurations',N'VnPayHashSecretCiphertext') IS NULL
 ALTER TABLE dbo.PaymentMethodConfigurations ADD VnPayHashSecretCiphertext nvarchar(max) NULL;
IF COL_LENGTH(N'dbo.PaymentMethodConfigurations',N'VnPayBaseUrl') IS NULL
 ALTER TABLE dbo.PaymentMethodConfigurations ADD VnPayBaseUrl nvarchar(500) NOT NULL CONSTRAINT DF_PaymentConfig_VnPayBase DEFAULT N'https://sandbox.vnpayment.vn/paymentv2/vpcpay.html';
IF COL_LENGTH(N'dbo.PaymentMethodConfigurations',N'VnPayReturnUrl') IS NULL
 ALTER TABLE dbo.PaymentMethodConfigurations ADD VnPayReturnUrl nvarchar(500) NOT NULL CONSTRAINT DF_PaymentConfig_VnPayReturn DEFAULT N'https://topstar.vn/Brand/VnPayReturn';
IF COL_LENGTH(N'dbo.PaymentMethodConfigurations',N'VnPayIpnUrl') IS NULL
 ALTER TABLE dbo.PaymentMethodConfigurations ADD VnPayIpnUrl nvarchar(500) NOT NULL CONSTRAINT DF_PaymentConfig_VnPayIpn DEFAULT N'https://topstar.vn/api/payments/vnpay/ipn';
IF OBJECT_ID(N'dbo.VnPayReceipts',N'U') IS NULL
 CREATE TABLE dbo.VnPayReceipts(
  TransactionNo nvarchar(100) NOT NULL CONSTRAINT PK_VnPayReceipts PRIMARY KEY,
  TargetReference nvarchar(40) NOT NULL CONSTRAINT UQ_VnPayReceipts_Target UNIQUE,
  Amount decimal(18,0) NOT NULL CONSTRAINT CK_VnPayReceipts_Amount CHECK(Amount>0),
  CreatedAtUtc datetime2 NOT NULL
 );
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.CampaignFundings') AND name=N'CK_CampaignFundings_Method')
 ALTER TABLE dbo.CampaignFundings DROP CONSTRAINT CK_CampaignFundings_Method;
/* The original constraint is named PaymentMethod on some deployments. */
DECLARE @drop nvarchar(max)=N'';
SELECT @drop=@drop+N'ALTER TABLE dbo.CampaignFundings DROP CONSTRAINT '+QUOTENAME(name)+N';'
FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.CampaignFundings') AND name IN(N'CK_CampaignFundings_PaymentMethod',N'CK_CampaignFundings_Method');
EXEC sys.sp_executesql @drop;
ALTER TABLE dbo.CampaignFundings WITH CHECK ADD CONSTRAINT CK_CampaignFundings_PaymentMethod
 CHECK(PaymentMethod IS NULL OR PaymentMethod IN(N'BankTransfer',N'OnePay',N'VnPay'));
SET @drop=N'';
SELECT @drop=@drop+N'ALTER TABLE dbo.BrandPackagePurchases DROP CONSTRAINT '+QUOTENAME(name)+N';'
FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.BrandPackagePurchases') AND name IN(N'CK_BrandPackagePurchases_PaymentMethod',N'CK_BrandPackagePurchases_Method');
EXEC sys.sp_executesql @drop;
ALTER TABLE dbo.BrandPackagePurchases WITH CHECK ADD CONSTRAINT CK_BrandPackagePurchases_PaymentMethod
 CHECK(PaymentMethod IN(N'BankTransfer',N'OnePay',N'VnPay'));
COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
GO
SELECT name, max_length, is_nullable FROM sys.columns
WHERE object_id=OBJECT_ID(N'dbo.PaymentMethodConfigurations') AND name LIKE N'VnPay%';
SELECT name,definition,is_disabled,is_not_trusted FROM sys.check_constraints
WHERE parent_object_id IN(OBJECT_ID(N'dbo.CampaignFundings'),OBJECT_ID(N'dbo.BrandPackagePurchases'))
 AND name IN(N'CK_CampaignFundings_PaymentMethod',N'CK_BrandPackagePurchases_PaymentMethod');
GO
