SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.CampaignFundings','VersionNo') IS NULL
    ALTER TABLE dbo.CampaignFundings ADD VersionNo INT NOT NULL CONSTRAINT DF_CampaignFundings_VersionNo DEFAULT(1);
IF COL_LENGTH('dbo.CampaignFundings','CancelledAtUtc') IS NULL ALTER TABLE dbo.CampaignFundings ADD CancelledAtUtc DATETIME2(7) NULL;
IF COL_LENGTH('dbo.CampaignFundings','CancellationReason') IS NULL ALTER TABLE dbo.CampaignFundings ADD CancellationReason NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.CampaignFundings','RefundedAmount') IS NULL ALTER TABLE dbo.CampaignFundings ADD RefundedAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_CampaignFundings_RefundedAmount DEFAULT(0);
IF COL_LENGTH('dbo.CampaignFundings','RefundedAtUtc') IS NULL ALTER TABLE dbo.CampaignFundings ADD RefundedAtUtc DATETIME2(7) NULL;
IF COL_LENGTH('dbo.CampaignFundings','RefundReference') IS NULL ALTER TABLE dbo.CampaignFundings ADD RefundReference NVARCHAR(200) NULL;

DECLARE @uqFunding sysname=(SELECT TOP 1 kc.name FROM sys.key_constraints kc JOIN sys.index_columns ic ON ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE kc.parent_object_id=OBJECT_ID('dbo.CampaignFundings') AND kc.type='UQ' GROUP BY kc.name HAVING COUNT(*)=1 AND MAX(c.name)='CampaignId');
IF @uqFunding IS NOT NULL BEGIN DECLARE @dropFundingUq NVARCHAR(MAX)=N'ALTER TABLE dbo.CampaignFundings DROP CONSTRAINT '+QUOTENAME(@uqFunding)+N';'; EXEC sys.sp_executesql @dropFundingUq; END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CampaignFundings') AND name='UX_CampaignFundings_Campaign_Version')
    EXEC(N'CREATE UNIQUE INDEX UX_CampaignFundings_Campaign_Version ON dbo.CampaignFundings(CampaignId,VersionNo);');
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CampaignFundings') AND name='UX_CampaignFundings_ActiveCampaign')
    EXEC(N'CREATE UNIQUE INDEX UX_CampaignFundings_ActiveCampaign ON dbo.CampaignFundings(CampaignId) WHERE Status<>''Cancelled'' AND Status<>''Refunded'';');

IF COL_LENGTH('dbo.CampaignFundingAllocations','AcceptedAmount') IS NULL ALTER TABLE dbo.CampaignFundingAllocations ADD AcceptedAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_CampaignFundingAllocations_Accepted DEFAULT(0);
IF COL_LENGTH('dbo.CampaignFundingAllocations','RefundedAmount') IS NULL ALTER TABLE dbo.CampaignFundingAllocations ADD RefundedAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_CampaignFundingAllocations_Refunded DEFAULT(0);
IF COL_LENGTH('dbo.CampaignFundingAllocations','Status') IS NULL ALTER TABLE dbo.CampaignFundingAllocations ADD Status NVARCHAR(30) NOT NULL CONSTRAINT DF_CampaignFundingAllocations_Status DEFAULT('Active');
IF COL_LENGTH('dbo.CampaignFundingAllocations','SettlementReason') IS NULL ALTER TABLE dbo.CampaignFundingAllocations ADD SettlementReason NVARCHAR(500) NULL;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_CampaignFundingAllocations_Settlement')
    EXEC(N'ALTER TABLE dbo.CampaignFundingAllocations ADD CONSTRAINT CK_CampaignFundingAllocations_Settlement CHECK(AcceptedAmount>=0 AND RefundedAmount>=0 AND AcceptedAmount+RefundedAmount<=Amount AND Status IN(''Active'',''Accepted'',''PartiallyAccepted'',''Rejected'',''Removed''));');
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CampaignFundingAllocations') AND name='UX_CampaignFundingAllocations_WalletTransaction')
    CREATE UNIQUE INDEX UX_CampaignFundingAllocations_WalletTransaction ON dbo.CampaignFundingAllocations(WalletTransactionId) WHERE WalletTransactionId IS NOT NULL;

DECLARE @uqPolicy sysname=(SELECT TOP 1 kc.name FROM sys.key_constraints kc JOIN sys.index_columns ic ON ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE kc.parent_object_id=OBJECT_ID('dbo.CampaignFeePolicies') AND kc.type='UQ' GROUP BY kc.name HAVING COUNT(*)=1 AND MAX(c.name)='Code');
IF @uqPolicy IS NOT NULL BEGIN DECLARE @dropPolicyUq NVARCHAR(MAX)=N'ALTER TABLE dbo.CampaignFeePolicies DROP CONSTRAINT '+QUOTENAME(@uqPolicy)+N';'; EXEC sys.sp_executesql @dropPolicyUq; END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CampaignFeePolicies') AND name='IX_CampaignFeePolicies_Code_Effective')
    CREATE INDEX IX_CampaignFeePolicies_Code_Effective ON dbo.CampaignFeePolicies(Code,IsActive,EffectiveAtUtc DESC);

COMMIT TRANSACTION;
GO

CREATE OR ALTER TRIGGER dbo.TR_CampaignFundings_SnapshotImmutable
ON dbo.CampaignFundings AFTER UPDATE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE d.Status<>'Draft' AND (
  ISNULL(i.OrderNumber,'')<>ISNULL(d.OrderNumber,'') OR i.CampaignId<>d.CampaignId OR i.VersionNo<>d.VersionNo OR i.AllocationMode<>d.AllocationMode OR i.PaymentMethod<>d.PaymentMethod OR
  i.CampaignBudgetSnapshot<>d.CampaignBudgetSnapshot OR i.FeeRateSnapshot<>d.FeeRateSnapshot OR i.FixedFeeSnapshot<>d.FixedFeeSnapshot OR i.PlatformFeeSnapshot<>d.PlatformFeeSnapshot OR
  i.GatewayFeeRateSnapshot<>d.GatewayFeeRateSnapshot OR i.GatewayFeeSnapshot<>d.GatewayFeeSnapshot OR i.GatewayFeeBearerSnapshot<>d.GatewayFeeBearerSnapshot OR
  i.TotalPayableSnapshot<>d.TotalPayableSnapshot OR i.DisputeHoldDaysSnapshot<>d.DisputeHoldDaysSnapshot OR i.PaymentDueDaysSnapshot<>d.PaymentDueDaysSnapshot OR
  i.RefundPolicySnapshot<>d.RefundPolicySnapshot OR ISNULL(i.SubmittedAtUtc,'19000101')<>ISNULL(d.SubmittedAtUtc,'19000101') OR ISNULL(i.DueAtUtc,'19000101')<>ISNULL(d.DueAtUtc,'19000101')))
 BEGIN THROW 51000,N'Snapshot ký quỹ không được thay đổi sau khi gửi yêu cầu thanh toán.',1; END
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_CampaignFundingAllocations_Immutable
ON dbo.CampaignFundingAllocations AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id WHERE i.Id IS NULL)
 OR EXISTS(SELECT 1 FROM inserted i LEFT JOIN deleted d ON d.Id=i.Id JOIN dbo.CampaignFundings f ON f.Id=i.FundingId WHERE d.Id IS NULL AND f.Status<>'Draft')
 OR EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.FundingId<>d.FundingId OR i.ParticipationId<>d.ParticipationId OR i.Amount<>d.Amount OR i.CreatedAtUtc<>d.CreatedAtUtc)
 BEGIN THROW 51000,N'Chỉ kết quả nghiệm thu được bổ sung; snapshot phân bổ không được sửa hoặc xóa.',1; END
END;
GO

PRINT N'OK - Đã hoàn thiện schema vòng đời quỹ chiến dịch.';
