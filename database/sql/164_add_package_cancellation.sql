/* Package cancellation requests. Run after 161 and 163, before binaries.
   Preserves purchases, payment receipts and balances. Safe to rerun. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.BrandBalanceLedgers',N'U') IS NULL OR OBJECT_ID(N'dbo.BrandPackagePurchaseHistories',N'U') IS NULL
 THROW 51000,N'Thiếu dữ liệu gói hoặc số dư. Chạy script 106 và 161 trước.',1;
BEGIN TRY
 BEGIN TRANSACTION;
 IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.BrandPackagePurchases') AND name=N'CK_BrandPackagePurchases_Status')
  ALTER TABLE dbo.BrandPackagePurchases DROP CONSTRAINT CK_BrandPackagePurchases_Status;
 ALTER TABLE dbo.BrandPackagePurchases WITH CHECK ADD CONSTRAINT CK_BrandPackagePurchases_Status
 CHECK(Status IN('Pending','Paid','Consumed','Expired','Cancelled','CancelRequested','Refunded'));
 IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.BrandPackagePurchaseHistories') AND name=N'CK_BrandPackagePurchaseHistories_Status')
  ALTER TABLE dbo.BrandPackagePurchaseHistories DROP CONSTRAINT CK_BrandPackagePurchaseHistories_Status;
 ALTER TABLE dbo.BrandPackagePurchaseHistories WITH CHECK ADD CONSTRAINT CK_BrandPackagePurchaseHistories_Status
 CHECK((FromStatus IS NULL OR FromStatus IN('Pending','Paid','Consumed','Expired','Cancelled','CancelRequested','Refunded'))
 AND ToStatus IN('Pending','Paid','Consumed','Expired','Cancelled','CancelRequested','Refunded'));
 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.BrandBalanceLedgers') AND name=N'UX_BrandBalanceLedgers_PackageRefund')
  CREATE UNIQUE INDEX UX_BrandBalanceLedgers_PackageRefund ON dbo.BrandBalanceLedgers(SourceId) WHERE EntryType=N'PackageRefund';
 COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
GO
SELECT name,is_disabled,is_not_trusted,definition FROM sys.check_constraints
WHERE name IN(N'CK_BrandPackagePurchases_Status',N'CK_BrandPackagePurchaseHistories_Status');
GO
