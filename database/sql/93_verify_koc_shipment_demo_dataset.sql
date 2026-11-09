SET NOCOUNT ON;

DECLARE @Email NVARCHAR(320)='seed.koc.0001@seed.topstar.local';

SELECT u.Email,k.DisplayName,c.Title,s.Carrier,s.TrackingCode,s.Status,s.IssueNote,
       (SELECT COUNT(*) FROM dbo.ShipmentStatusHistories h WHERE h.ShipmentId=s.Id) HistoryCount,
       (SELECT COUNT(*) FROM dbo.ShipmentTrackingEvents t WHERE t.ShipmentId=s.Id) TrackingCount,
       (SELECT COUNT(*) FROM dbo.ShipmentEvidence e WHERE e.ShipmentId=s.Id) EvidenceCount,
       s.CreatedAtUtc,s.UpdatedAtUtc
FROM dbo.Shipments s
JOIN dbo.Participations p ON p.Id=s.ParticipationId
JOIN dbo.Campaigns c ON c.Id=p.CampaignId
JOIN dbo.KocProfiles k ON k.Id=p.KocProfileId
JOIN dbo.Users u ON u.Id=k.UserId
WHERE u.Email=@Email
ORDER BY s.CreatedAtUtc DESC;

SELECT N'Tổng vận đơn' Metric,COUNT(*) Value
FROM dbo.Shipments s JOIN dbo.Participations p ON p.Id=s.ParticipationId
JOIN dbo.KocProfiles k ON k.Id=p.KocProfileId JOIN dbo.Users u ON u.Id=k.UserId WHERE u.Email=@Email
UNION ALL
SELECT N'Lịch sử trạng thái',COUNT(*) FROM dbo.ShipmentStatusHistories h JOIN dbo.Shipments s ON s.Id=h.ShipmentId
JOIN dbo.Participations p ON p.Id=s.ParticipationId JOIN dbo.KocProfiles k ON k.Id=p.KocProfileId JOIN dbo.Users u ON u.Id=k.UserId WHERE u.Email=@Email
UNION ALL
SELECT N'Checkpoint hành trình',COUNT(*) FROM dbo.ShipmentTrackingEvents t JOIN dbo.Shipments s ON s.Id=t.ShipmentId
JOIN dbo.Participations p ON p.Id=s.ParticipationId JOIN dbo.KocProfiles k ON k.Id=p.KocProfileId JOIN dbo.Users u ON u.Id=k.UserId WHERE u.Email=@Email
UNION ALL
SELECT N'Ảnh bằng chứng',COUNT(*) FROM dbo.ShipmentEvidence e JOIN dbo.Shipments s ON s.Id=e.ShipmentId
JOIN dbo.Participations p ON p.Id=s.ParticipationId JOIN dbo.KocProfiles k ON k.Id=p.KocProfileId JOIN dbo.Users u ON u.Id=k.UserId WHERE u.Email=@Email;
