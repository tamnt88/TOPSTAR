SET NOCOUNT ON;SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.CampaignPackagePolicyHistories','U') IS NULL
 BEGIN
  CREATE TABLE dbo.CampaignPackagePolicyHistories(
   Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CampaignPackagePolicyHistories PRIMARY KEY,
   PolicyId UNIQUEIDENTIFIER NOT NULL,Name NVARCHAR(150) NOT NULL,MinBudgetAmount DECIMAL(18,2) NOT NULL,MaxBudgetAmount DECIMAL(18,2) NULL,
   BrandSelectionEnabled BIT NOT NULL,SelectionQuota INT NULL,IsActive BIT NOT NULL,Reason NVARCHAR(1000) NOT NULL,
   ActorUserId UNIQUEIDENTIFIER NOT NULL,CreatedAtUtc DATETIME2(7) NOT NULL,
   CONSTRAINT FK_CampaignPackagePolicyHistories_Policy FOREIGN KEY(PolicyId) REFERENCES dbo.CampaignPackagePolicies(Id),
   CONSTRAINT FK_CampaignPackagePolicyHistories_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
   CONSTRAINT CK_CampaignPackagePolicyHistories_Budget CHECK(MinBudgetAmount>=0 AND(MaxBudgetAmount IS NULL OR MaxBudgetAmount>=MinBudgetAmount)),
   CONSTRAINT CK_CampaignPackagePolicyHistories_Quota CHECK((BrandSelectionEnabled=0 AND SelectionQuota IS NULL)OR(BrandSelectionEnabled=1 AND SelectionQuota>0)),
   CONSTRAINT CK_CampaignPackagePolicyHistories_Reason CHECK(LEN(LTRIM(RTRIM(Reason))) BETWEEN 5 AND 1000));
  CREATE INDEX IX_CampaignPackagePolicyHistories_Policy_Created ON dbo.CampaignPackagePolicyHistories(PolicyId,CreatedAtUtc);
 END;
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_CampaignPackagePolicyHistories_AppendOnly ON dbo.CampaignPackagePolicyHistories AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON;THROW 51000,N''Lịch sử package chỉ được phép ghi thêm.'',1;END;');
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_CampaignPackagePolicies_ProtectIdentity ON dbo.CampaignPackagePolicies AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON;IF NOT EXISTS(SELECT 1 FROM inserted) THROW 51000,N''Không được xóa package policy; hãy tắt IsActive.'',1;IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.Code<>d.Code OR i.CreatedAtUtc<>d.CreatedAtUtc) THROW 51000,N''Mã và thời điểm tạo package policy là bất biến.'',1;END;');
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK TRANSACTION;THROW;END CATCH;
