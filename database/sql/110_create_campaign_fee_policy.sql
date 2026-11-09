SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.CampaignFeePolicies','U') IS NULL
BEGIN
 CREATE TABLE dbo.CampaignFeePolicies(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CampaignFeePolicies PRIMARY KEY,
  Code NVARCHAR(30) NOT NULL, FeeRate DECIMAL(9,6) NOT NULL, FixedFee DECIMAL(18,2) NOT NULL,
  MinimumFee DECIMAL(18,2) NULL, MaximumFee DECIMAL(18,2) NULL, GatewayFeeRate DECIMAL(9,6) NOT NULL,
  GatewayFeeBearer NVARCHAR(10) NOT NULL, DisputeHoldDays INT NOT NULL, PaymentDueDays INT NOT NULL,
  RefundPolicy NVARCHAR(1000) NOT NULL, EffectiveAtUtc DATETIME2(7) NOT NULL, IsActive BIT NOT NULL,
  UpdatedByUserId UNIQUEIDENTIFIER NULL, CreatedAtUtc DATETIME2(7) NOT NULL, UpdatedAtUtc DATETIME2(7) NULL,
  CONSTRAINT UQ_CampaignFeePolicies_Code UNIQUE(Code),
  CONSTRAINT CK_CampaignFeePolicies_Values CHECK(FeeRate BETWEEN 0 AND 1 AND GatewayFeeRate BETWEEN 0 AND 1 AND FixedFee>=0 AND (MinimumFee IS NULL OR MinimumFee>=0) AND (MaximumFee IS NULL OR MaximumFee>=0) AND (MinimumFee IS NULL OR MaximumFee IS NULL OR MinimumFee<=MaximumFee) AND GatewayFeeBearer IN('Brand','Topstar') AND DisputeHoldDays BETWEEN 0 AND 90 AND PaymentDueDays BETWEEN 1 AND 90)
 );
END;

IF OBJECT_ID('dbo.CampaignFeePolicyHistories','U') IS NULL
BEGIN
 CREATE TABLE dbo.CampaignFeePolicyHistories(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CampaignFeePolicyHistories PRIMARY KEY, PolicyId UNIQUEIDENTIFIER NOT NULL,
  FeeRate DECIMAL(9,6) NOT NULL, FixedFee DECIMAL(18,2) NOT NULL, MinimumFee DECIMAL(18,2) NULL, MaximumFee DECIMAL(18,2) NULL,
  GatewayFeeRate DECIMAL(9,6) NOT NULL, GatewayFeeBearer NVARCHAR(10) NOT NULL, DisputeHoldDays INT NOT NULL, PaymentDueDays INT NOT NULL,
  RefundPolicy NVARCHAR(1000) NOT NULL, Reason NVARCHAR(500) NOT NULL, ActorUserId UNIQUEIDENTIFIER NULL, CreatedAtUtc DATETIME2(7) NOT NULL,
  CONSTRAINT FK_CampaignFeePolicyHistories_Policy FOREIGN KEY(PolicyId) REFERENCES dbo.CampaignFeePolicies(Id)
 );
 CREATE INDEX IX_CampaignFeePolicyHistories_Policy_Created ON dbo.CampaignFeePolicyHistories(PolicyId,CreatedAtUtc DESC);
END;

DECLARE @id UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.CampaignFeePolicies WHERE Code='DEFAULT');
IF @id IS NULL
BEGIN
 SET @id=NEWID();
 INSERT dbo.CampaignFeePolicies VALUES(@id,'DEFAULT',.15,0,NULL,NULL,0,'Brand',7,7,N'Hoàn phần ngân sách chưa giải ngân sau khi khấu trừ nghĩa vụ đã nghiệm thu.',SYSUTCDATETIME(),1,NULL,SYSUTCDATETIME(),NULL);
 INSERT dbo.CampaignFeePolicyHistories VALUES(NEWID(),@id,.15,0,NULL,NULL,0,'Brand',7,7,N'Hoàn phần ngân sách chưa giải ngân sau khi khấu trừ nghĩa vụ đã nghiệm thu.',N'Khởi tạo chính sách mặc định',NULL,SYSUTCDATETIME());
END;

IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_CampaignBillingInvoices_Amounts')
 ALTER TABLE dbo.CampaignBillingInvoices DROP CONSTRAINT CK_CampaignBillingInvoices_Amounts;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_CampaignBillingInvoices_Amounts_Configurable')
 ALTER TABLE dbo.CampaignBillingInvoices ADD CONSTRAINT CK_CampaignBillingInvoices_Amounts_Configurable CHECK(CampaignBudgetSnapshot>0 AND FeeRate BETWEEN 0 AND 1 AND FeeAmount>=0);

IF OBJECT_ID('dbo.CampaignFundings','U') IS NULL
BEGIN
 CREATE TABLE dbo.CampaignFundings(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CampaignFundings PRIMARY KEY,CampaignId UNIQUEIDENTIFIER NOT NULL,
  AllocationMode NVARCHAR(10) NOT NULL,Status NVARCHAR(30) NOT NULL,OrderNumber NVARCHAR(50) NULL,PaymentMethod NVARCHAR(20) NOT NULL CONSTRAINT DF_CampaignFundings_PaymentMethod DEFAULT('BankTransfer'),
  CampaignBudgetSnapshot DECIMAL(18,2) NOT NULL,FeeRateSnapshot DECIMAL(9,6) NOT NULL,FixedFeeSnapshot DECIMAL(18,2) NOT NULL,
  PlatformFeeSnapshot DECIMAL(18,2) NOT NULL,GatewayFeeRateSnapshot DECIMAL(9,6) NOT NULL,GatewayFeeSnapshot DECIMAL(18,2) NOT NULL,
  GatewayFeeBearerSnapshot NVARCHAR(10) NOT NULL,TotalPayableSnapshot DECIMAL(18,2) NOT NULL,DisputeHoldDaysSnapshot INT NOT NULL,
  PaymentDueDaysSnapshot INT NOT NULL,RefundPolicySnapshot NVARCHAR(1000) NOT NULL,SubmittedAtUtc DATETIME2(7) NULL,DueAtUtc DATETIME2(7) NULL,
  PaidAtUtc DATETIME2(7) NULL,PaymentReference NVARCHAR(200) NULL,UpdatedByUserId UNIQUEIDENTIFIER NULL,CreatedAtUtc DATETIME2(7) NOT NULL,UpdatedAtUtc DATETIME2(7) NULL,
  CONSTRAINT UQ_CampaignFundings_Campaign UNIQUE(CampaignId),
  CONSTRAINT FK_CampaignFundings_Campaign FOREIGN KEY(CampaignId) REFERENCES dbo.Campaigns(Id),
  CONSTRAINT CK_CampaignFundings_Mode CHECK(AllocationMode IN('Equal','Custom')),
  CONSTRAINT CK_CampaignFundings_PaymentMethod CHECK(PaymentMethod IN('BankTransfer','OnePay')),
  CONSTRAINT CK_CampaignFundings_Status CHECK(Status IN('Draft','Pending','Paid','PartiallyReleased','Released','Cancelled','Refunded')),
  CONSTRAINT CK_CampaignFundings_Amounts CHECK(CampaignBudgetSnapshot>0 AND PlatformFeeSnapshot>=0 AND GatewayFeeSnapshot>=0 AND TotalPayableSnapshot>=CampaignBudgetSnapshot)
 );
END;
IF COL_LENGTH('dbo.CampaignFundings','PaymentMethod') IS NULL
 EXEC(N'ALTER TABLE dbo.CampaignFundings ADD PaymentMethod NVARCHAR(20) NOT NULL CONSTRAINT DF_CampaignFundings_PaymentMethod DEFAULT(''BankTransfer'');');
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.CampaignFundings') AND name='CK_CampaignFundings_PaymentMethod')
 EXEC(N'ALTER TABLE dbo.CampaignFundings ADD CONSTRAINT CK_CampaignFundings_PaymentMethod CHECK(PaymentMethod IN(''BankTransfer'',''OnePay''));');
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CampaignFundings') AND name='UX_CampaignFundings_OrderNumber')
 CREATE UNIQUE INDEX UX_CampaignFundings_OrderNumber ON dbo.CampaignFundings(OrderNumber) WHERE OrderNumber IS NOT NULL;
IF OBJECT_ID('dbo.CampaignFundingAllocations','U') IS NULL
BEGIN
 CREATE TABLE dbo.CampaignFundingAllocations(Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CampaignFundingAllocations PRIMARY KEY,FundingId UNIQUEIDENTIFIER NOT NULL,ParticipationId UNIQUEIDENTIFIER NOT NULL,Amount DECIMAL(18,2) NOT NULL,ReleasedAtUtc DATETIME2(7) NULL,WalletTransactionId UNIQUEIDENTIFIER NULL,CreatedAtUtc DATETIME2(7) NOT NULL,CONSTRAINT UQ_CampaignFundingAllocations_Participation UNIQUE(FundingId,ParticipationId),CONSTRAINT FK_CampaignFundingAllocations_Funding FOREIGN KEY(FundingId) REFERENCES dbo.CampaignFundings(Id),CONSTRAINT FK_CampaignFundingAllocations_Participation FOREIGN KEY(ParticipationId) REFERENCES dbo.Participations(Id),CONSTRAINT CK_CampaignFundingAllocations_Amount CHECK(Amount>0));
END;
IF OBJECT_ID('dbo.CampaignFundLedgers','U') IS NULL
BEGIN
 CREATE TABLE dbo.CampaignFundLedgers(Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CampaignFundLedgers PRIMARY KEY,FundingId UNIQUEIDENTIFIER NOT NULL,AllocationId UNIQUEIDENTIFIER NULL,EntryType NVARCHAR(30) NOT NULL,Amount DECIMAL(18,2) NOT NULL,Note NVARCHAR(500) NOT NULL,ActorUserId UNIQUEIDENTIFIER NULL,CreatedAtUtc DATETIME2(7) NOT NULL,CONSTRAINT FK_CampaignFundLedgers_Funding FOREIGN KEY(FundingId) REFERENCES dbo.CampaignFundings(Id),CONSTRAINT FK_CampaignFundLedgers_Allocation FOREIGN KEY(AllocationId) REFERENCES dbo.CampaignFundingAllocations(Id));
 CREATE INDEX IX_CampaignFundLedgers_Funding_Created ON dbo.CampaignFundLedgers(FundingId,CreatedAtUtc);
END;

IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_ADMIN_CAMPAIGN_FEE_POLICY')
 INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES(NEWID(),'MENU_ADMIN_CAMPAIGN_FEE_POLICY',N'Phí chiến dịch','/Admin/Finance/CampaignFeePolicy','fa-solid fa-percent',NULL,13,1,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_ADMIN_CAMPAIGN_FUNDING')
 INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES(NEWID(),'MENU_ADMIN_CAMPAIGN_FUNDING',N'Ký quỹ chiến dịch','/Admin/Finance/CampaignFunding','fa-solid fa-vault',NULL,14,1,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_BRAND_CAMPAIGN_FUNDING')
 INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES(NEWID(),'MENU_BRAND_CAMPAIGN_FUNDING',N'Thanh toán chiến dịch','/Brand/CampaignFunding','fa-solid fa-money-check-dollar',NULL,14,1,SYSUTCDATETIME());

DECLARE @adminRole UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='ADMIN'),@brandRole UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='BRAND');
IF @adminRole IS NOT NULL INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc) SELECT @adminRole,m.Id,SYSUTCDATETIME() FROM dbo.Menus m WHERE m.Code IN('MENU_ADMIN_CAMPAIGN_FEE_POLICY','MENU_ADMIN_CAMPAIGN_FUNDING') AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus rm WHERE rm.RoleId=@adminRole AND rm.MenuId=m.Id);
IF @brandRole IS NOT NULL INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc) SELECT @brandRole,m.Id,SYSUTCDATETIME() FROM dbo.Menus m WHERE m.Code='MENU_BRAND_CAMPAIGN_FUNDING' AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus rm WHERE rm.RoleId=@brandRole AND rm.MenuId=m.Id);

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
CREATE OR ALTER TRIGGER dbo.TR_CampaignFundingAllocations_Immutable
ON dbo.CampaignFundingAllocations
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id WHERE i.Id IS NULL)
    OR EXISTS(SELECT 1 FROM inserted i LEFT JOIN deleted d ON d.Id=i.Id JOIN dbo.CampaignFundings f ON f.Id=i.FundingId WHERE d.Id IS NULL AND f.Status<>'Draft')
    OR EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.FundingId<>d.FundingId OR i.ParticipationId<>d.ParticipationId OR i.Amount<>d.Amount OR i.CreatedAtUtc<>d.CreatedAtUtc)
 BEGIN
  RAISERROR(N'Phân bổ thù lao không được thay đổi sau khi gửi yêu cầu thanh toán.',16,1);
  ROLLBACK TRANSACTION;
 END
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_CampaignFundLedgers_AppendOnly
ON dbo.CampaignFundLedgers
INSTEAD OF UPDATE, DELETE
AS
BEGIN
 RAISERROR(N'Sổ quỹ chiến dịch chỉ cho phép ghi bổ sung, không cho phép sửa hoặc xóa.',16,1);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_CampaignFeePolicyHistories_AppendOnly
ON dbo.CampaignFeePolicyHistories
INSTEAD OF UPDATE, DELETE
AS
BEGIN
 RAISERROR(N'Lịch sử chính sách phí chỉ cho phép ghi bổ sung, không cho phép sửa hoặc xóa.',16,1);
END;
GO
