/*
 Allow account lock/unlock history without forcing profile review status to change.
 Required by the separated Account status / Profile status design.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.UserAccountStatusHistories','U') IS NULL
  THROW 51000,N'Thiếu bảng UserAccountStatusHistories. Hãy chạy migration 69 trước.',1;

 IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.UserAccountStatusHistories') AND name='CK_UserAccountStatusHistories_ProfileStatus')
  ALTER TABLE dbo.UserAccountStatusHistories DROP CONSTRAINT CK_UserAccountStatusHistories_ProfileStatus;

 ALTER TABLE dbo.UserAccountStatusHistories WITH CHECK ADD CONSTRAINT CK_UserAccountStatusHistories_ProfileStatus
 CHECK(
   FromProfileStatus IN('Draft','PendingReview','NeedUpdate','Approved','Rejected','Locked')
   AND ToProfileStatus IN('Draft','PendingReview','NeedUpdate','Approved','Rejected','Locked')
 );

 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
