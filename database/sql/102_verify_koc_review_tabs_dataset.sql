SET NOCOUNT ON;

DECLARE @KocUserId UNIQUEIDENTIFIER;
DECLARE @KocProfileId UNIQUEIDENTIFIER;

SELECT TOP (1) @KocUserId = u.Id, @KocProfileId = k.Id
FROM dbo.Users u
JOIN dbo.KocProfiles k ON k.UserId = u.Id
WHERE u.Email = 'seed.koc.0001@seed.topstar.local';

IF @KocUserId IS NULL
    THROW 51001, N'Không tìm thấy KOC seed cần kiểm tra.', 1;

SELECT N'Brand đánh giá bạn' AS [Tab], COUNT(*) AS [Tổng],
       SUM(CASE WHEN rr.Id IS NULL THEN 1 ELSE 0 END) AS [Chưa phản hồi],
       SUM(CASE WHEN rr.Id IS NOT NULL THEN 1 ELSE 0 END) AS [Đã phản hồi]
FROM dbo.Reviews r
LEFT JOIN dbo.ReviewReplies rr ON rr.ReviewId = r.Id
WHERE r.RevieweeUserId = @KocUserId
UNION ALL
SELECT N'Bạn đánh giá Brand', COUNT(*), NULL, NULL
FROM dbo.Reviews r
JOIN dbo.BrandProfiles b ON b.UserId = r.RevieweeUserId
WHERE r.ReviewerUserId = @KocUserId;

SELECT
    COUNT(*) AS [Tổng chiến dịch đủ điều kiện],
    SUM(CASE WHEN r.Id IS NULL THEN 1 ELSE 0 END) AS [Chờ đánh giá],
    SUM(CASE WHEN r.Id IS NOT NULL THEN 1 ELSE 0 END) AS [Đã đánh giá]
FROM dbo.Participations p
JOIN dbo.Campaigns c ON c.Id = p.CampaignId
JOIN dbo.BrandProfiles b ON b.Id = c.BrandProfileId
LEFT JOIN dbo.Reviews r
    ON r.CampaignId = c.Id
   AND r.ReviewerUserId = @KocUserId
   AND r.RevieweeUserId = b.UserId
WHERE p.KocProfileId = @KocProfileId
  AND p.Status = 'Completed'
  AND c.Status = 'Completed';

SELECT TOP (50)
    r.CreatedAtUtc,
    c.Title AS [Chiến dịch],
    reviewer.Email AS [Người đánh giá],
    reviewee.Email AS [Người được đánh giá],
    r.Rating,
    r.Comment,
    CASE WHEN rr.Id IS NULL THEN N'Chưa phản hồi' ELSE N'Đã phản hồi' END AS [Phản hồi]
FROM dbo.Reviews r
JOIN dbo.Campaigns c ON c.Id = r.CampaignId
JOIN dbo.Users reviewer ON reviewer.Id = r.ReviewerUserId
JOIN dbo.Users reviewee ON reviewee.Id = r.RevieweeUserId
LEFT JOIN dbo.ReviewReplies rr ON rr.ReviewId = r.Id
WHERE r.ReviewerUserId = @KocUserId OR r.RevieweeUserId = @KocUserId
ORDER BY r.CreatedAtUtc DESC;
