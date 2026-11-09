/* Brand VAT rules: package purchase and campaign funding. Run before binaries.
   No existing totals changed. Tax stays disabled until Admin configures a policy. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
BEGIN TRY
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.BrandTaxPolicies',N'U') IS NULL
 CREATE TABLE dbo.BrandTaxPolicies(Id uniqueidentifier NOT NULL PRIMARY KEY,Scope nvarchar(20) NOT NULL CHECK(Scope IN(N'Package',N'Funding')),VatMode nvarchar(20) NOT NULL CHECK(VatMode IN(N'None',N'Included',N'Excluded')),VatRate decimal(5,2) NOT NULL CHECK(VatRate BETWEEN 0 AND 100),TaxBase nvarchar(20) NOT NULL CHECK(TaxBase IN(N'ServiceFee',N'Total')),EffectiveAtUtc datetime2 NOT NULL,CreatedAtUtc datetime2 NOT NULL,ActorUserId uniqueidentifier NOT NULL);
DECLARE @table sysname,@sql nvarchar(max);
DECLARE tables CURSOR LOCAL FAST_FORWARD FOR SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID(N'dbo') AND name IN(N'BrandPackagePurchases',N'CampaignFundings');
OPEN tables;FETCH NEXT FROM tables INTO @table;
WHILE @@FETCH_STATUS=0
BEGIN
 IF COL_LENGTH(N'dbo.'+@table,N'VatPolicyIdSnapshot') IS NULL BEGIN SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@table)+N' ADD VatPolicyIdSnapshot uniqueidentifier NULL,VatRateSnapshot decimal(5,2) NOT NULL DEFAULT 0,VatAmountSnapshot decimal(18,0) NOT NULL DEFAULT 0,VatAddedSnapshot decimal(18,0) NOT NULL DEFAULT 0,VatModeSnapshot nvarchar(20) NOT NULL DEFAULT N''None'';'; EXEC sys.sp_executesql @sql; END;
 FETCH NEXT FROM tables INTO @table;
END;
CLOSE tables;DEALLOCATE tables;
COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
GO

CREATE OR ALTER TRIGGER dbo.TR_BrandTaxPolicies_Immutable ON dbo.BrandTaxPolicies INSTEAD OF UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 ;THROW 51000,N'Không sửa hoặc xóa phiên bản thuế. Hãy tạo phiên bản mới.',1;
END;
GO
SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.CampaignFundings') AND name LIKE N'Vat%';
GO
