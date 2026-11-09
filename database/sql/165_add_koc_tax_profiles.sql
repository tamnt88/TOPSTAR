/* Private KOC tax identity. Run before app binaries. Safe to rerun.
   No public image URLs or plaintext identity numbers are stored. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.KocProfiles',N'U') IS NULL
 THROW 51000,N'Thiếu bảng KocProfiles.',1;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID(N'dbo.KocTaxProfiles',N'U') IS NULL
 CREATE TABLE dbo.KocTaxProfiles(
  KocProfileId uniqueidentifier NOT NULL CONSTRAINT PK_KocTaxProfiles PRIMARY KEY REFERENCES dbo.KocProfiles(Id),
  TaxNumberCiphertext nvarchar(max) NOT NULL,
  IdentityNumberCiphertext nvarchar(max) NOT NULL,
  FrontFile nvarchar(100) NOT NULL,
  BackFile nvarchar(100) NOT NULL,
  Version uniqueidentifier NOT NULL,
  UpdatedAtUtc datetime2 NOT NULL,
  UpdatedByUserId uniqueidentifier NOT NULL
 );
 COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
GO
SELECT name FROM sys.tables WHERE object_id=OBJECT_ID(N'dbo.KocTaxProfiles');
GO
