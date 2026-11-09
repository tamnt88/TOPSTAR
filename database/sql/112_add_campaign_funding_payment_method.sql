SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.CampaignFundings','U') IS NULL
 THROW 51000, N'Chưa có bảng CampaignFundings. Hãy chạy file 110 trước.', 1;

IF COL_LENGTH('dbo.CampaignFundings','PaymentMethod') IS NULL
 EXEC(N'ALTER TABLE dbo.CampaignFundings ADD PaymentMethod NVARCHAR(20) NOT NULL CONSTRAINT DF_CampaignFundings_PaymentMethod DEFAULT(''BankTransfer'');');

IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.CampaignFundings') AND name='CK_CampaignFundings_PaymentMethod')
 EXEC(N'ALTER TABLE dbo.CampaignFundings ADD CONSTRAINT CK_CampaignFundings_PaymentMethod CHECK(PaymentMethod IN(''BankTransfer'',''OnePay''));');

COMMIT TRANSACTION;

GO
CREATE OR ALTER TRIGGER dbo.TR_CampaignFundings_SnapshotImmutable
ON dbo.CampaignFundings
AFTER UPDATE
AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(
  SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
  WHERE d.Status<>'Draft' AND (
   ISNULL(i.OrderNumber,'')<>ISNULL(d.OrderNumber,'') OR i.CampaignId<>d.CampaignId OR i.AllocationMode<>d.AllocationMode OR i.PaymentMethod<>d.PaymentMethod
   OR i.CampaignBudgetSnapshot<>d.CampaignBudgetSnapshot OR i.FeeRateSnapshot<>d.FeeRateSnapshot
   OR i.FixedFeeSnapshot<>d.FixedFeeSnapshot OR i.PlatformFeeSnapshot<>d.PlatformFeeSnapshot
   OR i.GatewayFeeRateSnapshot<>d.GatewayFeeRateSnapshot OR i.GatewayFeeSnapshot<>d.GatewayFeeSnapshot
   OR i.GatewayFeeBearerSnapshot<>d.GatewayFeeBearerSnapshot OR i.TotalPayableSnapshot<>d.TotalPayableSnapshot
   OR i.DisputeHoldDaysSnapshot<>d.DisputeHoldDaysSnapshot OR i.PaymentDueDaysSnapshot<>d.PaymentDueDaysSnapshot
   OR i.RefundPolicySnapshot<>d.RefundPolicySnapshot OR ISNULL(i.SubmittedAtUtc,'19000101')<>ISNULL(d.SubmittedAtUtc,'19000101')
   OR ISNULL(i.DueAtUtc,'19000101')<>ISNULL(d.DueAtUtc,'19000101')
  )
 )
 BEGIN
  RAISERROR(N'Snapshot ký quỹ không được thay đổi sau khi gửi yêu cầu thanh toán.',16,1);
  ROLLBACK TRANSACTION;
 END
END;
GO
