/* KOC tax at wallet credit. Run after 165/166 before binaries.
   Existing wallet balances unchanged. Deduction disabled until Admin creates an enabled policy. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.KocTaxProfiles',N'U') IS NULL OR OBJECT_ID(N'dbo.WalletTransactions',N'U') IS NULL
 THROW 51000,N'Chạy SQL 165 và các script ví KOC trước.',1;
BEGIN TRY
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.KocIncomeTaxPolicies',N'U') IS NULL
 CREATE TABLE dbo.KocIncomeTaxPolicies(Id uniqueidentifier NOT NULL PRIMARY KEY,IsEnabled bit NOT NULL,Rate decimal(5,2) NOT NULL CHECK(Rate BETWEEN 0 AND 100),Threshold decimal(18,0) NOT NULL CHECK(Threshold>=0),EffectiveAtUtc datetime2 NOT NULL,CreatedAtUtc datetime2 NOT NULL,ActorUserId uniqueidentifier NOT NULL);
IF OBJECT_ID(N'dbo.KocTaxTreatments',N'U') IS NULL
 CREATE TABLE dbo.KocTaxTreatments(KocProfileId uniqueidentifier NOT NULL PRIMARY KEY REFERENCES dbo.KocProfiles(Id),Treatment nvarchar(20) NOT NULL CHECK(Treatment IN(N'Withholding',N'Business',N'Exempt')),Reason nvarchar(1000) NOT NULL,UpdatedAtUtc datetime2 NOT NULL,ActorUserId uniqueidentifier NOT NULL);
IF COL_LENGTH(N'dbo.WalletTransactions',N'TaxState') IS NULL
 ALTER TABLE dbo.WalletTransactions ADD GrossAmountSnapshot decimal(18,0) NULL,TaxAmountSnapshot decimal(18,0) NOT NULL DEFAULT 0,TaxRateSnapshot decimal(5,2) NOT NULL DEFAULT 0,TaxPolicyIdSnapshot uniqueidentifier NULL,TaxState nvarchar(20) NOT NULL DEFAULT N'Legacy',TaxTreatmentSnapshot nvarchar(20) NULL,TaxRemittanceReference nvarchar(200) NULL,TaxRemittedAtUtc datetime2 NULL;
COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
GO
CREATE OR ALTER TRIGGER dbo.TR_KocIncomeTaxPolicies_Immutable ON dbo.KocIncomeTaxPolicies INSTEAD OF UPDATE,DELETE AS
BEGIN SET NOCOUNT ON; ;THROW 51000,N'Tạo phiên bản chính sách mới thay vì sửa hoặc xóa.',1; END;
GO

CREATE OR ALTER TRIGGER dbo.TR_WalletTransactions_TaxSnapshot ON dbo.WalletTransactions AFTER UPDATE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM inserted WHERE TaxState=N'Awaiting' AND Status<>N'OnHold')
  THROW 51000,N'Khoản thu nhập chờ xác minh thuế phải được giữ, chưa thể sử dụng hoặc rút.',1;
 IF EXISTS(SELECT 1 FROM deleted d JOIN inserted i ON d.Id=i.Id WHERE d.TaxState=N'Calculated' AND EXISTS(
  SELECT d.GrossAmountSnapshot,d.TaxAmountSnapshot,d.TaxRateSnapshot,d.TaxPolicyIdSnapshot,d.Amount,d.TaxState,d.TaxTreatmentSnapshot
  EXCEPT SELECT i.GrossAmountSnapshot,i.TaxAmountSnapshot,i.TaxRateSnapshot,i.TaxPolicyIdSnapshot,i.Amount,i.TaxState,i.TaxTreatmentSnapshot))
  THROW 51000,N'Không sửa số tiền và thuế đã chốt. Cần thực hiện giao dịch điều chỉnh.',1;
END;
GO
