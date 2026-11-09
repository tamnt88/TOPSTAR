/* Brand available balance, reserved withdrawals and immutable ledger.
   Run before binaries. No existing escrow is automatically moved or refunded. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.BrandProfiles',N'U') IS NULL OR OBJECT_ID(N'dbo.CampaignFundings',N'U') IS NULL
 THROW 51000,N'Thiếu dữ liệu Brand hoặc ký quỹ.',1;
BEGIN TRY
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.BrandBalances',N'U') IS NULL
 CREATE TABLE dbo.BrandBalances(
 BrandProfileId uniqueidentifier NOT NULL PRIMARY KEY REFERENCES dbo.BrandProfiles(Id),
 Available decimal(18,0) NOT NULL DEFAULT 0 CHECK(Available>=0),
 Reserved decimal(18,0) NOT NULL DEFAULT 0 CHECK(Reserved>=0),
 UpdatedAtUtc datetime2 NOT NULL);
IF OBJECT_ID(N'dbo.BrandBalancePolicies',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.BrandBalancePolicies(
 Id int NOT NULL PRIMARY KEY CHECK(Id=1),WithdrawalsEnabled bit NOT NULL,
 MinimumWithdrawal decimal(18,0) NOT NULL CHECK(MinimumWithdrawal>0),
 WithdrawalFee decimal(18,0) NOT NULL CHECK(WithdrawalFee>=0),
 DualApprovalThreshold decimal(18,0) NOT NULL CHECK(DualApprovalThreshold>0),
 ProcessingDays int NOT NULL CHECK(ProcessingDays BETWEEN 1 AND 30),
 UpdatedAtUtc datetime2 NOT NULL,UpdatedByUserId uniqueidentifier NULL);
 INSERT dbo.BrandBalancePolicies VALUES(1,1,50000,0,50000000,3,SYSUTCDATETIME(),NULL);
END;
IF OBJECT_ID(N'dbo.BrandBalanceWithdrawals',N'U') IS NULL
 CREATE TABLE dbo.BrandBalanceWithdrawals(
 Id uniqueidentifier NOT NULL PRIMARY KEY,
 BrandProfileId uniqueidentifier NOT NULL REFERENCES dbo.BrandProfiles(Id),
 RequestKey uniqueidentifier NOT NULL UNIQUE,
 Amount decimal(18,0) NOT NULL CHECK(Amount>0),Fee decimal(18,0) NOT NULL,
 BankName nvarchar(150) NOT NULL,AccountNumber nvarchar(50) NOT NULL,AccountHolder nvarchar(200) NOT NULL,
 Status nvarchar(20) NOT NULL CHECK(Status IN(N'Pending',N'Approved',N'Paid',N'Rejected',N'Cancelled')),
 DualApprovalRequired bit NOT NULL,FirstApprovedBy uniqueidentifier NULL,ApprovedBy uniqueidentifier NULL,
 PaymentReference nvarchar(100) NULL,EvidenceUrl nvarchar(500) NULL,Note nvarchar(500) NULL,
 CreatedAtUtc datetime2 NOT NULL,UpdatedAtUtc datetime2 NULL,
 CONSTRAINT CK_BrandBalanceWithdrawals_Fee CHECK(Fee>=0 AND Fee<Amount));
IF OBJECT_ID(N'dbo.BrandBalanceLedgers',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.BrandBalanceLedgers(
 Id uniqueidentifier NOT NULL PRIMARY KEY,BrandProfileId uniqueidentifier NOT NULL REFERENCES dbo.BrandProfiles(Id),
 EntryType nvarchar(40) NOT NULL,Amount decimal(18,0) NOT NULL,
 AvailableAfter decimal(18,0) NOT NULL CHECK(AvailableAfter>=0),
 ReservedAfter decimal(18,0) NOT NULL CHECK(ReservedAfter>=0),
 SourceId uniqueidentifier NOT NULL,OperationKey uniqueidentifier NOT NULL UNIQUE,
 Note nvarchar(500) NOT NULL,ActorUserId uniqueidentifier NULL,CreatedAtUtc datetime2 NOT NULL);
 CREATE INDEX IX_BrandBalanceLedgers_Brand_Date ON dbo.BrandBalanceLedgers(BrandProfileId,CreatedAtUtc DESC);
END;
IF COL_LENGTH(N'dbo.CampaignFundings',N'BalanceAppliedAmount') IS NULL
 ALTER TABLE dbo.CampaignFundings ADD BalanceAppliedAmount decimal(18,0) NOT NULL CONSTRAINT DF_CampaignFunding_Balance DEFAULT 0;
IF COL_LENGTH(N'dbo.CampaignFundings',N'OnlinePaymentStartedAtUtc') IS NULL
 ALTER TABLE dbo.CampaignFundings ADD OnlinePaymentStartedAtUtc datetime2 NULL;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_CampaignFunding_Balance')
 EXEC(N'ALTER TABLE dbo.CampaignFundings WITH CHECK ADD CONSTRAINT CK_CampaignFunding_Balance CHECK(BalanceAppliedAmount>=0 AND BalanceAppliedAmount<=TotalPayableSnapshot)');
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.CampaignFundings') AND name=N'CK_CampaignFundings_PaymentMethod')
 ALTER TABLE dbo.CampaignFundings DROP CONSTRAINT CK_CampaignFundings_PaymentMethod;
ALTER TABLE dbo.CampaignFundings WITH CHECK ADD CONSTRAINT CK_CampaignFundings_PaymentMethod CHECK(PaymentMethod IN(N'BankTransfer',N'OnePay',N'VnPay',N'BrandBalance'));
/* Dynamic batch keeps CREATE TRIGGER first in its batch and inside this transaction. */
EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_BrandBalanceLedgers_Immutable
ON dbo.BrandBalanceLedgers INSTEAD OF UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 ;THROW 51000,N''Không được sửa hoặc xóa sổ số dư Brand.'',1;
END;');
COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
GO
SELECT name FROM sys.tables
WHERE schema_id=SCHEMA_ID(N'dbo')
 AND name IN(N'BrandBalances',N'BrandBalancePolicies',N'BrandBalanceWithdrawals',N'BrandBalanceLedgers');
SELECT name,is_disabled,is_not_trusted,definition FROM sys.check_constraints
WHERE parent_object_id=OBJECT_ID(N'dbo.BrandBalanceWithdrawals')
 AND name=N'CK_BrandBalanceWithdrawals_Fee';
SELECT name,is_disabled FROM sys.triggers
WHERE parent_id=OBJECT_ID(N'dbo.BrandBalanceLedgers')
 AND name=N'TR_BrandBalanceLedgers_Immutable';
GO
