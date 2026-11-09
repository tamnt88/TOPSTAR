SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.CampaignStatusHistories','U') IS NULL THROW 51090,'CampaignStatusHistories does not exist.',1;
 IF OBJECT_ID('dbo.CampaignReviewFeedbackItems','U') IS NULL
 BEGIN
  CREATE TABLE dbo.CampaignReviewFeedbackItems(
   Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CampaignReviewFeedbackItems PRIMARY KEY,
   CampaignId UNIQUEIDENTIFIER NOT NULL, CampaignStatusHistoryId UNIQUEIDENTIFIER NOT NULL,
   FieldCode NVARCHAR(30) NOT NULL, Message NVARCHAR(500) NOT NULL, SortOrder INT NOT NULL,
   ActorUserId UNIQUEIDENTIFIER NOT NULL, CreatedAtUtc DATETIME2(7) NOT NULL,
   CONSTRAINT FK_CampaignReviewFeedbackItems_Campaigns FOREIGN KEY(CampaignId) REFERENCES dbo.Campaigns(Id),
   CONSTRAINT FK_CampaignReviewFeedbackItems_StatusHistory FOREIGN KEY(CampaignStatusHistoryId) REFERENCES dbo.CampaignStatusHistories(Id),
   CONSTRAINT FK_CampaignReviewFeedbackItems_Users FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
   CONSTRAINT CK_CampaignReviewFeedbackItems_Field CHECK(FieldCode IN('Title','Brief','Budget','Schedule','Deliverables','Targeting','Logistics','Other')),
   CONSTRAINT CK_CampaignReviewFeedbackItems_Message CHECK(LEN(LTRIM(RTRIM(Message))) BETWEEN 1 AND 500),
   CONSTRAINT CK_CampaignReviewFeedbackItems_Sort CHECK(SortOrder BETWEEN 0 AND 7),
   CONSTRAINT UQ_CampaignReviewFeedbackItems_Review_Field UNIQUE(CampaignStatusHistoryId,FieldCode)
  );
  CREATE INDEX IX_CampaignReviewFeedbackItems_Campaign_Created ON dbo.CampaignReviewFeedbackItems(CampaignId,CreatedAtUtc DESC) INCLUDE(CampaignStatusHistoryId,FieldCode,SortOrder);
 END;
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_CampaignReviewFeedbackItems_AppendOnly ON dbo.CampaignReviewFeedbackItems AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM deleted) THROW 51091,''Campaign review feedback is append-only.'',1; END;');
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_CampaignReviewFeedbackItems_ValidateReview ON dbo.CampaignReviewFeedbackItems AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i LEFT JOIN dbo.CampaignStatusHistories h ON h.Id=i.CampaignStatusHistoryId WHERE h.Id IS NULL OR h.CampaignId<>i.CampaignId OR h.ToStatus<>''NeedEdit'' OR h.ActorUserId<>i.ActorUserId) THROW 51092,''Feedback must belong to its NeedEdit review and actor.'',1; END;');
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
