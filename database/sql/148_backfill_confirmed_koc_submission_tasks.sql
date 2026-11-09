/* Bù đầu việc cho các lượt hợp tác đã xác nhận trước khi cơ chế tạo Submission được bổ sung. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

INSERT dbo.Submissions(Id,ParticipationId,DeliverableId,Status,IsVisible,CreatedAtUtc)
SELECT NEWID(),p.Id,d.Id,N'NotSubmitted',1,SYSUTCDATETIME()
FROM dbo.Participations p
JOIN dbo.CampaignDeliverables d ON d.CampaignId=p.CampaignId
WHERE p.Status IN(N'Confirmed',N'Completed')
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.Submissions s
      WHERE s.ParticipationId=p.Id AND s.DeliverableId=d.Id
  );

DECLARE @Inserted int=@@ROWCOUNT;
COMMIT TRANSACTION;
PRINT CONCAT(N'OK - Đã bổ sung ',@Inserted,N' đầu việc còn thiếu cho KOC đã được xác nhận.');
