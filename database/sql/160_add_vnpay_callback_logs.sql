/* TOPSTAR - Full VNPay Return/IPN journal. Safe to rerun; no financial data changed.
   Existing transactions cannot be backfilled without their original signed responses. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
BEGIN TRY
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.VnPayCallbackLogs',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.VnPayCallbackLogs(
  Id uniqueidentifier NOT NULL CONSTRAINT PK_VnPayCallbackLogs PRIMARY KEY,
  Source nvarchar(10) NOT NULL CONSTRAINT CK_VnPayCallbackLogs_Source CHECK(Source IN(N'Return',N'IPN')),
  TargetReference nvarchar(100) NULL,
  TransactionNo nvarchar(100) NULL,
  RawQuery nvarchar(max) NOT NULL,
  PayloadJson nvarchar(max) NOT NULL CONSTRAINT CK_VnPayCallbackLogs_Json CHECK(ISJSON(PayloadJson)=1),
  RemoteIp nvarchar(100) NULL,
  UserAgent nvarchar(2000) NULL,
  SignatureValid bit NULL,
  Success bit NULL,
  ResponseCode nvarchar(10) NULL,
  ResultMessage nvarchar(2000) NULL,
  ErrorDetail nvarchar(4000) NULL,
  ReceivedAtUtc datetime2 NOT NULL,
  ProcessedAtUtc datetime2 NULL
 );
 CREATE INDEX IX_VnPayCallbackLogs_TargetReference_ReceivedAtUtc ON dbo.VnPayCallbackLogs(TargetReference,ReceivedAtUtc DESC);
END;
COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
GO
CREATE OR ALTER TRIGGER dbo.TR_VnPayCallbackLogs_PreserveOriginal
ON dbo.VnPayCallbackLogs AFTER UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id WHERE i.Id IS NULL)
  THROW 51000,N'Không được xóa nhật ký phản hồi VNPay.',1;
 IF UPDATE(Id) OR UPDATE(Source) OR UPDATE(TargetReference) OR UPDATE(TransactionNo)
    OR UPDATE(RawQuery) OR UPDATE(PayloadJson) OR UPDATE(RemoteIp) OR UPDATE(UserAgent) OR UPDATE(ReceivedAtUtc)
  THROW 51000,N'Không được sửa dữ liệu gốc của phản hồi VNPay.',1;
END;
GO
SELECT name FROM sys.tables WHERE object_id=OBJECT_ID(N'dbo.VnPayCallbackLogs');
GO
