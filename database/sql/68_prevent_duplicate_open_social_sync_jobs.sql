SET NOCOUNT ON;SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 ;WITH OpenJobs AS(
  SELECT Id,KocProfileId,ROW_NUMBER() OVER(PARTITION BY KocProfileId ORDER BY RequestedAtUtc,Id) AS RowNo
  FROM dbo.SocialSyncJobs WITH(UPDLOCK,HOLDLOCK)
  WHERE KocProfileId IS NOT NULL AND Status IN('Pending','Running'))
 UPDATE j SET Status='Failed',ErrorMessage=N'Đóng khi nâng cấp: trùng job social sync đang mở.',FinishedAtUtc=SYSUTCDATETIME()
 FROM dbo.SocialSyncJobs j JOIN OpenJobs o ON o.Id=j.Id WHERE o.RowNo>1;
 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.SocialSyncJobs') AND name='IX_SocialSyncJobs_Koc_Status')
  CREATE INDEX IX_SocialSyncJobs_Koc_Status ON dbo.SocialSyncJobs(KocProfileId,Status,RequestedAtUtc);
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_SocialSyncJobs_OneOpenPerKoc ON dbo.SocialSyncJobs AFTER INSERT,UPDATE AS
 BEGIN
  SET NOCOUNT ON;
  IF EXISTS(
   SELECT j.KocProfileId FROM dbo.SocialSyncJobs j WITH(UPDLOCK,HOLDLOCK)
   JOIN(SELECT DISTINCT KocProfileId FROM inserted WHERE KocProfileId IS NOT NULL) i ON i.KocProfileId=j.KocProfileId
   WHERE j.Status IN(''Pending'',''Running'') GROUP BY j.KocProfileId HAVING COUNT_BIG(*)>1)
   THROW 51000,N''Mỗi KOC chỉ được có một job social sync đang mở.'',1;
 END;');
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK TRANSACTION;THROW;END CATCH;
