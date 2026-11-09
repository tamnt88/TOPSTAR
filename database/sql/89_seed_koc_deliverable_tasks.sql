SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

/*
  Seed đầu việc KOC phục vụ kiểm thử /Koc/Tasks.
  - Idempotent theo các GUID sinh từ TOPSTAR-SEED-TASK-*.
  - Tạo 24 đầu việc cho seed.koc.0001@seed.topstar.local.
  - Có đủ trạng thái, deadline, phản hồi và lịch sử.
  Yêu cầu: đã chạy bộ seed lớn 84.
*/

DECLARE @Now DATETIME2(0)=SYSUTCDATETIME();
DECLARE @KocUserId UNIQUEIDENTIFIER;
DECLARE @KocProfileId UNIQUEIDENTIFIER;

SELECT TOP(1) @KocUserId=u.Id,@KocProfileId=k.Id
FROM dbo.Users u
JOIN dbo.KocProfiles k ON k.UserId=u.Id
WHERE u.Email='seed.koc.0001@seed.topstar.local';

IF @KocUserId IS NULL OR @KocProfileId IS NULL
    THROW 51089, 'Chưa có seed.koc.0001. Hãy chạy 84_seed_large_dev_activity_dataset.sql trước.', 1;

BEGIN TRANSACTION;

DROP TABLE IF EXISTS #TaskSeed;
;WITH N AS
(
    SELECT TOP(24) ROW_NUMBER() OVER(ORDER BY (SELECT NULL)) No
    FROM sys.all_objects
)
SELECT
    n.No,
    CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-CAMPAIGN-',n.No))) CampaignId,
    CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-TASK-PART-',n.No))) NewParticipationId,
    CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-TASK-DELIVERABLE-',n.No))) DeliverableId,
    CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-TASK-SUBMISSION-',n.No))) SubmissionId
INTO #TaskSeed
FROM N n;

IF EXISTS(SELECT 1 FROM #TaskSeed s LEFT JOIN dbo.Campaigns c ON c.Id=s.CampaignId WHERE c.Id IS NULL)
    THROW 51090, 'Thiếu campaign seed. Hãy chạy lại script 84 trước script 89.', 1;

UPDATE c
SET c.Status='InProgress',
    c.StartDate=DATEADD(DAY,-(2+s.No%12),CAST(@Now AS DATE)),
    c.EndDate=DATEADD(DAY,18+(s.No%24),CAST(@Now AS DATE)),
    c.IsVisible=1,
    c.UpdatedAtUtc=@Now
FROM dbo.Campaigns c
JOIN #TaskSeed s ON s.CampaignId=c.Id;

INSERT dbo.Participations(Id,CampaignId,KocProfileId,Status,ConfirmedAtUtc,CreatedAtUtc)
SELECT s.NewParticipationId,s.CampaignId,@KocProfileId,'Confirmed',DATEADD(DAY,-10-s.No,@Now),DATEADD(DAY,-10-s.No,@Now)
FROM #TaskSeed s
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.Participations p
    WHERE p.CampaignId=s.CampaignId AND p.KocProfileId=@KocProfileId
);

DROP TABLE IF EXISTS #ResolvedTasks;
SELECT s.*,p.Id ParticipationId
INTO #ResolvedTasks
FROM #TaskSeed s
JOIN dbo.Participations p ON p.CampaignId=s.CampaignId AND p.KocProfileId=@KocProfileId;

INSERT dbo.CampaignDeliverables(Id,CampaignId,DeliverableType,Quantity,DueDateUtc,CreatedAtUtc,UpdatedAtUtc)
SELECT r.DeliverableId,r.CampaignId,
       CASE r.No%5
           WHEN 0 THEN N'Video TikTok'
           WHEN 1 THEN N'Bài đăng Facebook'
           WHEN 2 THEN N'Reels Instagram'
           WHEN 3 THEN N'Video review YouTube'
           ELSE N'Bộ ảnh sản phẩm'
       END,
       1+(r.No%3),
       CASE WHEN r.No%7=0 THEN NULL ELSE DATEADD(DAY,(r.No%13)-4,@Now) END,
       DATEADD(DAY,-9-r.No,@Now),NULL
FROM #ResolvedTasks r
WHERE NOT EXISTS(SELECT 1 FROM dbo.CampaignDeliverables d WHERE d.Id=r.DeliverableId);

INSERT dbo.Submissions
(
    Id,ParticipationId,DeliverableId,Status,SubmissionUrl,ReviewComment,
    RevisionCount,SubmittedAtUtc,ReviewedAtUtc,ViewCount,IsVisible,CreatedAtUtc,UpdatedAtUtc
)
SELECT
    r.SubmissionId,r.ParticipationId,r.DeliverableId,
    CASE r.No%6
        WHEN 0 THEN 'Approved'
        WHEN 1 THEN 'NotSubmitted'
        WHEN 2 THEN 'NeedRevision'
        WHEN 3 THEN 'Submitted'
        WHEN 4 THEN 'Approved'
        ELSE 'Rejected'
    END,
    CASE WHEN r.No%6=1 THEN NULL ELSE CONCAT('https://social.example.test/koc-0001/task-',FORMAT(r.No,'00')) END,
    CASE r.No%6
        WHEN 2 THEN N'Vui lòng bổ sung cảnh quay cận sản phẩm và sửa CTA ở cuối video.'
        WHEN 5 THEN N'Nội dung chưa đúng brief đã thống nhất với Brand.'
        ELSE NULL
    END,
    CASE WHEN r.No%6=2 THEN 1 ELSE 0 END,
    CASE WHEN r.No%6=1 THEN NULL ELSE DATEADD(DAY,-(r.No%8),@Now) END,
    CASE WHEN r.No%6 IN(0,2,4,5) THEN DATEADD(HOUR,-12,DATEADD(DAY,-(r.No%8),@Now)) ELSE NULL END,
    r.No*3,1,DATEADD(DAY,-9-r.No,@Now),@Now
FROM #ResolvedTasks r
WHERE NOT EXISTS(SELECT 1 FROM dbo.Submissions x WHERE x.Id=r.SubmissionId)
  AND NOT EXISTS(SELECT 1 FROM dbo.Submissions x WHERE x.ParticipationId=r.ParticipationId AND x.DeliverableId=r.DeliverableId);

INSERT dbo.SubmissionHistories
(
    Id,SubmissionId,EventType,RevisionNo,Status,SubmissionUrl,ReviewComment,ActorUserId,CreatedAtUtc
)
SELECT
    CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-TASK-HISTORY-',r.No))),
    s.Id,'MigratedSnapshot',s.RevisionCount,s.Status,s.SubmissionUrl,s.ReviewComment,NULL,s.CreatedAtUtc
FROM #ResolvedTasks r
JOIN dbo.Submissions s ON s.Id=r.SubmissionId
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.SubmissionHistories h
    WHERE h.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-TASK-HISTORY-',r.No)))
);

INSERT dbo.AuditLogs(ActorUserId,EntityName,EntityId,ActionName,PayloadJson,CreatedAtUtc)
SELECT @KocUserId,'Submission',CONVERT(NVARCHAR(36),r.SubmissionId),'SeedCreated',
       CONCAT('{"source":"89_seed_koc_deliverable_tasks","number":',r.No,'}'),@Now
FROM #ResolvedTasks r
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.AuditLogs a
    WHERE a.EntityName='Submission' AND a.EntityId=CONVERT(NVARCHAR(36),r.SubmissionId) AND a.ActionName='SeedCreated'
);

COMMIT TRANSACTION;

SELECT s.Status,COUNT(*) Total
FROM dbo.Submissions s
JOIN #ResolvedTasks r ON r.SubmissionId=s.Id
GROUP BY s.Status
ORDER BY s.Status;

SELECT COUNT(*) TaskCount,MIN(d.DueDateUtc) EarliestDueUtc,MAX(d.DueDateUtc) LatestDueUtc
FROM #ResolvedTasks r
JOIN dbo.Submissions s ON s.Id=r.SubmissionId
JOIN dbo.CampaignDeliverables d ON d.Id=s.DeliverableId;
