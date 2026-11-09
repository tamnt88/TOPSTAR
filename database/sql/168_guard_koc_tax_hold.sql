/* Guard direct wallet status updates while tax verification is pending.
   Run after 167. No existing amounts changed. Safe to rerun. */
SET NOCOUNT ON;
GO
IF COL_LENGTH(N'dbo.WalletTransactions',N'TaxState') IS NULL
 THROW 51000,N'Chạy SQL 167 trước.',1;
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
