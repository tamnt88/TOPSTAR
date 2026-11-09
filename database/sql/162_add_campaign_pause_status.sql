/* Pre-work campaign pause/resume. Run after 161, before binaries. Safe to rerun. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.CampaignStatusHistories',N'U') IS NULL
 THROW 51000,N'Thiếu lịch sử trạng thái chiến dịch. Chạy script 29 trước.',1;
IF COL_LENGTH(N'dbo.Campaigns',N'CancellationReason') IS NULL
 THROW 51000,N'Thiếu thông tin hủy chiến dịch. Chạy script 36 trước.',1;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @drop nvarchar(max)=N'';
 SELECT @drop=@drop+N'ALTER TABLE dbo.Campaigns DROP CONSTRAINT '+QUOTENAME(name)+N';'
 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.Campaigns')
 AND (name=N'CK_Campaigns_Status' OR (definition LIKE N'%[[]Status[]]%' AND definition LIKE N'%Published%'));
 EXEC sys.sp_executesql @drop;
 ALTER TABLE dbo.Campaigns WITH CHECK ADD CONSTRAINT CK_Campaigns_Status
 CHECK(Status IN(N'Draft',N'Submitted',N'NeedEdit',N'Approved',N'Published',N'Rejected',N'InProgress',N'Completed',N'Cancelled',N'Paused'));
 IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.CampaignStatusHistories') AND name=N'CK_CampaignStatusHistories_From')
 ALTER TABLE dbo.CampaignStatusHistories DROP CONSTRAINT CK_CampaignStatusHistories_From;
 IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.CampaignStatusHistories') AND name=N'CK_CampaignStatusHistories_To')
 ALTER TABLE dbo.CampaignStatusHistories DROP CONSTRAINT CK_CampaignStatusHistories_To;
 ALTER TABLE dbo.CampaignStatusHistories WITH CHECK ADD CONSTRAINT CK_CampaignStatusHistories_From
 CHECK(FromStatus IS NULL OR FromStatus IN(N'Draft',N'Submitted',N'NeedEdit',N'Approved',N'Published',N'Rejected',N'InProgress',N'Completed',N'Cancelled',N'Paused'));
 ALTER TABLE dbo.CampaignStatusHistories WITH CHECK ADD CONSTRAINT CK_CampaignStatusHistories_To
 CHECK(ToStatus IN(N'Draft',N'Submitted',N'NeedEdit',N'Approved',N'Published',N'Rejected',N'InProgress',N'Completed',N'Cancelled',N'Paused'));
 /* Self-service pre-work cancellations have no dispute case; retain actor/time/reason. */
 IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.Campaigns') AND name=N'CK_Campaigns_CancellationMetadata')
 ALTER TABLE dbo.Campaigns DROP CONSTRAINT CK_Campaigns_CancellationMetadata;
 ALTER TABLE dbo.Campaigns WITH CHECK ADD CONSTRAINT CK_Campaigns_CancellationMetadata
 CHECK((Status<>N'Cancelled' AND CancellationDisputeCaseId IS NULL AND CancelledByUserId IS NULL AND CancelledAtUtc IS NULL AND CancellationReason IS NULL)
 OR (Status=N'Cancelled' AND CancelledByUserId IS NOT NULL AND CancelledAtUtc IS NOT NULL AND CancellationReason IS NOT NULL AND LEN(CancellationReason)>=10));
 COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
GO
SELECT name,is_disabled,is_not_trusted FROM sys.check_constraints
WHERE name IN(N'CK_Campaigns_Status',N'CK_CampaignStatusHistories_From',N'CK_CampaignStatusHistories_To',N'CK_Campaigns_CancellationMetadata');
GO
