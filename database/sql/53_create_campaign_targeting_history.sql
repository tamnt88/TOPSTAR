SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.KocCategories','U') IS NULL OR OBJECT_ID('dbo.Provinces','U') IS NULL THROW 51100,'Targeting reference tables do not exist.',1;
 IF OBJECT_ID('dbo.CampaignTargetings','U') IS NULL
 BEGIN
  CREATE TABLE dbo.CampaignTargetings(CampaignId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CampaignTargetings PRIMARY KEY,Gender NVARCHAR(10) NOT NULL,MinimumFollowers INT NULL,MaximumFollowers INT NULL,MinimumEngagementRate DECIMAL(5,2) NULL,MaximumEngagementRate DECIMAL(5,2) NULL,CreatedAtUtc DATETIME2(7) NOT NULL,UpdatedAtUtc DATETIME2(7) NULL,CONSTRAINT FK_CampaignTargetings_Campaigns FOREIGN KEY(CampaignId) REFERENCES dbo.Campaigns(Id) ON DELETE CASCADE,CONSTRAINT CK_CampaignTargetings_Gender CHECK(Gender IN('Any','Female','Male')),CONSTRAINT CK_CampaignTargetings_Followers CHECK((MinimumFollowers IS NULL OR MinimumFollowers>=0) AND (MaximumFollowers IS NULL OR MaximumFollowers>=0) AND (MinimumFollowers IS NULL OR MaximumFollowers IS NULL OR MinimumFollowers<=MaximumFollowers)),CONSTRAINT CK_CampaignTargetings_Engagement CHECK((MinimumEngagementRate IS NULL OR MinimumEngagementRate BETWEEN 0 AND 100) AND (MaximumEngagementRate IS NULL OR MaximumEngagementRate BETWEEN 0 AND 100) AND (MinimumEngagementRate IS NULL OR MaximumEngagementRate IS NULL OR MinimumEngagementRate<=MaximumEngagementRate)));
  CREATE TABLE dbo.CampaignTargetProvinces(CampaignId UNIQUEIDENTIFIER NOT NULL,ProvinceId UNIQUEIDENTIFIER NOT NULL,CreatedAtUtc DATETIME2(7) NOT NULL,CONSTRAINT PK_CampaignTargetProvinces PRIMARY KEY(CampaignId,ProvinceId),CONSTRAINT FK_CampaignTargetProvinces_Campaigns FOREIGN KEY(CampaignId) REFERENCES dbo.Campaigns(Id) ON DELETE CASCADE,CONSTRAINT FK_CampaignTargetProvinces_Provinces FOREIGN KEY(ProvinceId) REFERENCES dbo.Provinces(Id));
  CREATE TABLE dbo.CampaignTargetCategories(CampaignId UNIQUEIDENTIFIER NOT NULL,CategoryId UNIQUEIDENTIFIER NOT NULL,CreatedAtUtc DATETIME2(7) NOT NULL,CONSTRAINT PK_CampaignTargetCategories PRIMARY KEY(CampaignId,CategoryId),CONSTRAINT FK_CampaignTargetCategories_Campaigns FOREIGN KEY(CampaignId) REFERENCES dbo.Campaigns(Id) ON DELETE CASCADE,CONSTRAINT FK_CampaignTargetCategories_Categories FOREIGN KEY(CategoryId) REFERENCES dbo.KocCategories(Id));
  CREATE INDEX IX_CampaignTargetProvinces_Province ON dbo.CampaignTargetProvinces(ProvinceId,CampaignId);CREATE INDEX IX_CampaignTargetCategories_Category ON dbo.CampaignTargetCategories(CategoryId,CampaignId);
 END;
 IF OBJECT_ID('dbo.CampaignTargetingHistories','U') IS NULL
 BEGIN
  CREATE TABLE dbo.CampaignTargetingHistories(Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CampaignTargetingHistories PRIMARY KEY,CampaignId UNIQUEIDENTIFIER NOT NULL,Action NVARCHAR(10) NOT NULL,FromSnapshotJson NVARCHAR(MAX) NULL,ToSnapshotJson NVARCHAR(MAX) NOT NULL,ActorUserId UNIQUEIDENTIFIER NOT NULL,CreatedAtUtc DATETIME2(7) NOT NULL,CONSTRAINT FK_CampaignTargetingHistories_Campaigns FOREIGN KEY(CampaignId) REFERENCES dbo.Campaigns(Id),CONSTRAINT FK_CampaignTargetingHistories_Users FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),CONSTRAINT CK_CampaignTargetingHistories_Action CHECK(Action IN('Created','Updated')),CONSTRAINT CK_CampaignTargetingHistories_Json CHECK(ISJSON(ToSnapshotJson)=1 AND (FromSnapshotJson IS NULL OR ISJSON(FromSnapshotJson)=1)),CONSTRAINT CK_CampaignTargetingHistories_Snapshot CHECK((Action='Created' AND FromSnapshotJson IS NULL) OR (Action='Updated' AND FromSnapshotJson IS NOT NULL)));
  CREATE INDEX IX_CampaignTargetingHistories_Campaign_Created ON dbo.CampaignTargetingHistories(CampaignId,CreatedAtUtc DESC) INCLUDE(Action,ActorUserId);
 END;
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_CampaignTargetingHistories_AppendOnly ON dbo.CampaignTargetingHistories AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM deleted) THROW 51101,''Campaign targeting history is append-only.'',1; END;');
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
