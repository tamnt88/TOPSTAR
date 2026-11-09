/* Campaign needs-first checkout and KOC platform fees. Run after 168, BEFORE binaries.
   Preserves legacy purchases/funding. Defaults are editable in Admin > Finance > Campaign creation. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.Campaigns',N'U') IS NULL OR COL_LENGTH(N'dbo.WalletTransactions',N'TaxState') IS NULL
 THROW 51000,N'Chạy các script chiến dịch và SQL 167/168 trước.',1;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID(N'dbo.CampaignCreationPolicies',N'U') IS NULL
 BEGIN
 CREATE TABLE dbo.CampaignCreationPolicies(
 Id int NOT NULL PRIMARY KEY CHECK(Id=1),
 MinimumBudget decimal(18,0) NOT NULL CHECK(MinimumBudget>0),
 SelfSelectionMinimumBudget decimal(18,0) NOT NULL,
 MinimumBudgetPerKoc decimal(18,0) NOT NULL CHECK(MinimumBudgetPerKoc>0),
 MaximumKocCount int NOT NULL CHECK(MaximumKocCount BETWEEN 1 AND 100),
 KocPlatformFeeRate decimal(5,2) NOT NULL CHECK(KocPlatformFeeRate BETWEEN 0 AND 99),
 UpdatedAtUtc datetime2 NOT NULL,ActorUserId uniqueidentifier NULL,
 CONSTRAINT CK_CampaignCreation_Threshold CHECK(SelfSelectionMinimumBudget>=MinimumBudget));
 INSERT dbo.CampaignCreationPolicies VALUES(1,5000000,10000000,1000000,100,0,SYSUTCDATETIME(),NULL);
 END;
 IF COL_LENGTH(N'dbo.CampaignPackagePolicies',N'ReferenceBudget') IS NULL
 ALTER TABLE dbo.CampaignPackagePolicies ADD ReferenceBudget decimal(18,0) NULL;
 IF COL_LENGTH(N'dbo.CampaignPackagePolicyHistories',N'ReferenceBudget') IS NULL
 ALTER TABLE dbo.CampaignPackagePolicyHistories ADD ReferenceBudget decimal(18,0) NULL;
 IF COL_LENGTH(N'dbo.Campaigns',N'CheckoutAtCreation') IS NULL
 ALTER TABLE dbo.Campaigns ADD CheckoutAtCreation bit NOT NULL CONSTRAINT DF_Campaign_CheckoutCreation DEFAULT 0,
 BrandSelectionSnapshot bit NOT NULL CONSTRAINT DF_Campaign_BrandSelection DEFAULT 0,
 SelectionQuotaSnapshot int NOT NULL CONSTRAINT DF_Campaign_SelectionQuota DEFAULT 0,
 KocPlatformFeeRateSnapshot decimal(5,2) NOT NULL CONSTRAINT DF_Campaign_KocFee DEFAULT 0;
 IF COL_LENGTH(N'dbo.WalletTransactions',N'PlatformFeeAmountSnapshot') IS NULL
 ALTER TABLE dbo.WalletTransactions ADD PlatformFeeRateSnapshot decimal(5,2) NOT NULL CONSTRAINT DF_Wallet_PlatformRate DEFAULT 0,
 PlatformFeeAmountSnapshot decimal(18,0) NOT NULL CONSTRAINT DF_Wallet_PlatformAmount DEFAULT 0;
 COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
GO
CREATE OR ALTER TRIGGER dbo.TR_WalletTransactions_TaxSnapshot ON dbo.WalletTransactions AFTER UPDATE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM inserted WHERE TaxState=N'Awaiting' AND Status<>N'OnHold')
 THROW 51000,N'Khoản chờ xác minh thuế phải được giữ.',1;
 IF EXISTS(SELECT 1 FROM deleted d JOIN inserted i ON d.Id=i.Id WHERE d.TaxState IN(N'Calculated',N'NotApplied') AND EXISTS(
 SELECT d.GrossAmountSnapshot,d.TaxAmountSnapshot,d.TaxRateSnapshot,d.TaxPolicyIdSnapshot,d.Amount,d.TaxState,d.TaxTreatmentSnapshot,d.PlatformFeeRateSnapshot,d.PlatformFeeAmountSnapshot
 EXCEPT SELECT i.GrossAmountSnapshot,i.TaxAmountSnapshot,i.TaxRateSnapshot,i.TaxPolicyIdSnapshot,i.Amount,i.TaxState,i.TaxTreatmentSnapshot,i.PlatformFeeRateSnapshot,i.PlatformFeeAmountSnapshot))
 THROW 51000,N'Không sửa số tiền, phí và thuế đã chốt.',1;
END;
GO
SELECT * FROM dbo.CampaignCreationPolicies;
GO
