/* Allow package checkout from Brand balance. Run after 161, before binaries. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.BrandBalanceLedgers',N'U') IS NULL
 THROW 51000,N'Chạy script 161 trước.',1;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @drop nvarchar(max)=N'';
 SELECT @drop=@drop+N'ALTER TABLE dbo.BrandPackagePurchases DROP CONSTRAINT '+QUOTENAME(name)+N';'
 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.BrandPackagePurchases')
 AND (name IN(N'CK_BrandPackagePurchases_PaymentMethod',N'CK_BrandPackagePurchases_Method') OR definition LIKE N'%PaymentMethod%');
 EXEC sys.sp_executesql @drop;
 ALTER TABLE dbo.BrandPackagePurchases WITH CHECK ADD CONSTRAINT CK_BrandPackagePurchases_PaymentMethod
 CHECK(PaymentMethod IN(N'BankTransfer',N'OnePay',N'VnPay',N'BrandBalance'));
 COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
GO
SELECT name,definition,is_disabled,is_not_trusted FROM sys.check_constraints
WHERE parent_object_id=OBJECT_ID(N'dbo.BrandPackagePurchases') AND name=N'CK_BrandPackagePurchases_PaymentMethod';
GO
