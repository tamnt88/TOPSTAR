SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.CampaignDeliverables','U') IS NULL THROW 51080,'CampaignDeliverables does not exist.',1;
 IF OBJECT_ID('dbo.CampaignDeliverableHistories','U') IS NULL
 BEGIN
  CREATE TABLE dbo.CampaignDeliverableHistories(
   Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CampaignDeliverableHistories PRIMARY KEY,
   CampaignId UNIQUEIDENTIFIER NOT NULL,
   DeliverableId UNIQUEIDENTIFIER NOT NULL,
   Action NVARCHAR(10) NOT NULL,
   FromDeliverableType NVARCHAR(100) NULL, FromQuantity INT NULL, FromDueDateUtc DATETIME2(7) NULL,
   ToDeliverableType NVARCHAR(100) NULL, ToQuantity INT NULL, ToDueDateUtc DATETIME2(7) NULL,
   ActorUserId UNIQUEIDENTIFIER NULL,
   CreatedAtUtc DATETIME2(7) NOT NULL,
   CONSTRAINT FK_CampaignDeliverableHistories_Campaigns FOREIGN KEY(CampaignId) REFERENCES dbo.Campaigns(Id),
   CONSTRAINT FK_CampaignDeliverableHistories_Users FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
   CONSTRAINT CK_CampaignDeliverableHistories_Action CHECK(Action IN('Created','Updated','Deleted')),
   CONSTRAINT CK_CampaignDeliverableHistories_Snapshot CHECK((Action='Created' AND FromDeliverableType IS NULL AND ToDeliverableType IS NOT NULL AND ToQuantity BETWEEN 1 AND 100) OR (Action='Updated' AND FromDeliverableType IS NOT NULL AND ToDeliverableType IS NOT NULL AND FromQuantity BETWEEN 1 AND 100 AND ToQuantity BETWEEN 1 AND 100) OR (Action='Deleted' AND FromDeliverableType IS NOT NULL AND FromQuantity BETWEEN 1 AND 100 AND ToDeliverableType IS NULL))
  );
  CREATE INDEX IX_CampaignDeliverableHistories_Campaign_Created ON dbo.CampaignDeliverableHistories(CampaignId,CreatedAtUtc DESC) INCLUDE(DeliverableId,Action,ActorUserId);
 END;
 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CampaignDeliverables') AND name='IX_CampaignDeliverables_Campaign_Created')
  CREATE INDEX IX_CampaignDeliverables_Campaign_Created ON dbo.CampaignDeliverables(CampaignId,CreatedAtUtc) INCLUDE(DeliverableType,Quantity,DueDateUtc);
 INSERT dbo.CampaignDeliverableHistories(Id,CampaignId,DeliverableId,Action,ToDeliverableType,ToQuantity,ToDueDateUtc,CreatedAtUtc)
 SELECT NEWID(),d.CampaignId,d.Id,'Created',d.DeliverableType,d.Quantity,d.DueDateUtc,d.CreatedAtUtc
 FROM dbo.CampaignDeliverables d
 WHERE NOT EXISTS(SELECT 1 FROM dbo.CampaignDeliverableHistories h WHERE h.DeliverableId=d.Id);
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_CampaignDeliverableHistories_AppendOnly ON dbo.CampaignDeliverableHistories AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM deleted) THROW 51081,''Campaign deliverable history is append-only.'',1; END;');
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
