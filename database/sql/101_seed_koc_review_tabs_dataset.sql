/*
 DEV/QA only - seed dữ liệu cho hai tab đánh giá của KOC.
 - Brand đánh giá KOC: 30 bản ghi.
 - KOC đánh giá Brand: 22 đã đánh giá + 8 đang chờ.
 - Có phản hồi và lịch sử trạng thái để kiểm tra modal.
 Script idempotent, chỉ nhận dữ liệu thuộc miền seed.topstar.local.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() NOT IN (N'TopstarDb', N'TopstarDb_Dev', N'TopstarDb_Test')
    THROW 51000, N'Script chỉ được chạy trên database DEV/TEST Topstar.', 1;

DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();
DECLARE @KocUserId UNIQUEIDENTIFIER;
DECLARE @KocProfileId UNIQUEIDENTIFIER;

SELECT TOP (1)
    @KocUserId = u.Id,
    @KocProfileId = k.Id
FROM dbo.Users u
JOIN dbo.KocProfiles k ON k.UserId = u.Id
WHERE u.Email = 'seed.koc.0001@seed.topstar.local';

IF @KocUserId IS NULL OR @KocProfileId IS NULL
    THROW 51001, N'Không tìm thấy seed.koc.0001@seed.topstar.local. Hãy chạy script 84 trước.', 1;

DROP TABLE IF EXISTS #ReviewCampaigns;
;WITH source AS
(
    SELECT
        c.Id AS CampaignId,
        b.UserId AS BrandUserId,
        ROW_NUMBER() OVER (ORDER BY c.CreatedAtUtc DESC, c.Id) AS RowNo
    FROM dbo.Campaigns c
    JOIN dbo.BrandProfiles b ON b.Id = c.BrandProfileId
    JOIN dbo.Users bu ON bu.Id = b.UserId
    WHERE c.Status = 'Completed'
      AND bu.Email LIKE 'seed.brand.%@seed.topstar.local'
)
SELECT TOP (30) CampaignId, BrandUserId, RowNo
INTO #ReviewCampaigns
FROM source
ORDER BY RowNo;

IF (SELECT COUNT(*) FROM #ReviewCampaigns) < 30
    THROW 51002, N'Không đủ 30 chiến dịch seed đã hoàn tất chưa có KOC mục tiêu. Hãy chạy lại script 84.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT dbo.Participations
        (Id, CampaignId, KocProfileId, Status, ConfirmedAtUtc, CreatedAtUtc, UpdatedAtUtc)
    SELECT
        CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT('TOPSTAR-KOC-REVIEW-TAB-PART-', CampaignId))),
        CampaignId,
        @KocProfileId,
        'Completed',
        DATEADD(DAY, -45 + RowNo, @Now),
        DATEADD(DAY, -45 + RowNo, @Now),
        DATEADD(DAY, -30 + RowNo, @Now)
    FROM #ReviewCampaigns c
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Participations p
        WHERE p.CampaignId = c.CampaignId AND p.KocProfileId = @KocProfileId
    );

    UPDATE p
       SET p.Status = 'Completed',
           p.UpdatedAtUtc = COALESCE(p.UpdatedAtUtc, @Now)
    FROM dbo.Participations p
    JOIN #ReviewCampaigns c ON c.CampaignId = p.CampaignId
    WHERE p.KocProfileId = @KocProfileId
      AND p.Status <> 'Completed';

    /* Tab 1: Brand đánh giá KOC. */
    INSERT dbo.Reviews
        (Id, CampaignId, ReviewerUserId, RevieweeUserId, Rating, Comment, Status, ViewCount, IsVisible, CreatedAtUtc)
    SELECT
        CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT('TOPSTAR-KOC-RECEIVED-REVIEW-', CampaignId))),
        CampaignId,
        BrandUserId,
        @KocUserId,
        CAST(3 + (RowNo % 3) AS TINYINT),
        CASE RowNo % 4
            WHEN 0 THEN N'KOC phối hợp chuyên nghiệp, chủ động cập nhật tiến độ và bàn giao đúng yêu cầu.'
            WHEN 1 THEN N'Nội dung sáng tạo tốt, phản hồi nhanh và tuân thủ đầy đủ brief của chiến dịch.'
            WHEN 2 THEN N'Kết quả đạt mục tiêu; cần bổ sung báo cáo số liệu chi tiết hơn ở lần hợp tác sau.'
            ELSE N'Thái độ hợp tác tốt, sản phẩm nội dung chỉn chu và hoàn thành đúng thời hạn.'
        END,
        'Completed',
        RowNo * 3,
        1,
        DATEADD(DAY, -31 + RowNo, @Now)
    FROM #ReviewCampaigns c
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Reviews r
        WHERE r.CampaignId = c.CampaignId
          AND r.ReviewerUserId = c.BrandUserId
          AND r.RevieweeUserId = @KocUserId
    );

    /* Chuẩn hóa cả bản ghi cũ nếu file từng bị chạy bằng code page không phải UTF-8. */
    UPDATE r
    SET Comment =
        CASE c.RowNo % 4
            WHEN 0 THEN N'KOC phối hợp chuyên nghiệp, chủ động cập nhật tiến độ và bàn giao đúng yêu cầu.'
            WHEN 1 THEN N'Nội dung sáng tạo tốt, phản hồi nhanh và tuân thủ đầy đủ brief của chiến dịch.'
            WHEN 2 THEN N'Kết quả đạt mục tiêu; cần bổ sung báo cáo số liệu chi tiết hơn ở lần hợp tác sau.'
            ELSE N'Thái độ hợp tác tốt, sản phẩm nội dung chỉn chu và hoàn thành đúng thời hạn.'
        END
    FROM dbo.Reviews r
    JOIN #ReviewCampaigns c ON c.CampaignId = r.CampaignId
    WHERE r.Id = CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT('TOPSTAR-KOC-RECEIVED-REVIEW-', c.CampaignId)));

    /* Tab 2: 22 đánh giá KOC đã gửi; 8 participation còn lại là trạng thái chờ đánh giá. */
    INSERT dbo.Reviews
        (Id, CampaignId, ReviewerUserId, RevieweeUserId, Rating, Comment, Status, ViewCount, IsVisible, CreatedAtUtc)
    SELECT
        CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT('TOPSTAR-KOC-SENT-REVIEW-', CampaignId))),
        CampaignId,
        @KocUserId,
        BrandUserId,
        CAST(3 + ((RowNo + 1) % 3) AS TINYINT),
        CASE RowNo % 3
            WHEN 0 THEN N'Brand cung cấp brief rõ ràng, phản hồi nhanh và hỗ trợ KOC trong suốt chiến dịch.'
            WHEN 1 THEN N'Quy trình làm việc chuyên nghiệp, lịch duyệt hợp lý và đối soát đúng cam kết.'
            ELSE N'Trải nghiệm hợp tác tốt; mong Brand thống nhất đầu mối phản hồi sớm hơn ở chiến dịch sau.'
        END,
        'Completed',
        RowNo,
        1,
        DATEADD(DAY, -25 + RowNo, @Now)
    FROM #ReviewCampaigns c
    WHERE RowNo <= 22
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.Reviews r
          WHERE r.CampaignId = c.CampaignId
            AND r.ReviewerUserId = @KocUserId
            AND r.RevieweeUserId = c.BrandUserId
      );

    UPDATE r
    SET Comment =
        CASE c.RowNo % 3
            WHEN 0 THEN N'Brand cung cấp brief rõ ràng, phản hồi nhanh và hỗ trợ KOC trong suốt chiến dịch.'
            WHEN 1 THEN N'Quy trình làm việc chuyên nghiệp, lịch duyệt hợp lý và đối soát đúng cam kết.'
            ELSE N'Trải nghiệm hợp tác tốt; mong Brand thống nhất đầu mối phản hồi sớm hơn ở chiến dịch sau.'
        END
    FROM dbo.Reviews r
    JOIN #ReviewCampaigns c ON c.CampaignId = r.CampaignId
    WHERE c.RowNo <= 22
      AND r.Id = CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT('TOPSTAR-KOC-SENT-REVIEW-', c.CampaignId)));

    INSERT dbo.ReviewStatusHistories
        (Id, ReviewId, FromStatus, ToStatus, Reason, ActorUserId, CreatedAtUtc)
    SELECT
        CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT('TOPSTAR-KOC-REVIEW-TAB-HISTORY-', r.Id))),
        r.Id,
        NULL,
        'Completed',
        N'Dữ liệu DEV/QA phục vụ kiểm thử DataTables và modal chi tiết.',
        r.ReviewerUserId,
        r.CreatedAtUtc
    FROM dbo.Reviews r
    JOIN #ReviewCampaigns c ON c.CampaignId = r.CampaignId
    WHERE (r.RevieweeUserId = @KocUserId OR r.ReviewerUserId = @KocUserId)
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.ReviewStatusHistories h
          WHERE h.Id = CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT('TOPSTAR-KOC-REVIEW-TAB-HISTORY-', r.Id)))
      );

    INSERT dbo.ReviewReplies(Id, ReviewId, AuthorUserId, Body, CreatedAtUtc)
    SELECT
        CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT('TOPSTAR-KOC-REVIEW-TAB-REPLY-', r.Id))),
        r.Id,
        @KocUserId,
        N'Cảm ơn Brand đã phản hồi. Mình đã ghi nhận góp ý để cải thiện các lần hợp tác tiếp theo.',
        DATEADD(HOUR, 8, r.CreatedAtUtc)
    FROM dbo.Reviews r
    JOIN #ReviewCampaigns c ON c.CampaignId = r.CampaignId
    WHERE r.RevieweeUserId = @KocUserId
      AND c.RowNo % 3 = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.ReviewReplies x WHERE x.ReviewId = r.Id);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT N'Brand đánh giá KOC' AS [Tab], COUNT(*) AS [Số bản ghi]
FROM dbo.Reviews r
JOIN #ReviewCampaigns c ON c.CampaignId = r.CampaignId
WHERE r.RevieweeUserId = @KocUserId AND r.ReviewerUserId = c.BrandUserId
UNION ALL
SELECT N'KOC đã đánh giá Brand', COUNT(*)
FROM dbo.Reviews r
JOIN #ReviewCampaigns c ON c.CampaignId = r.CampaignId
WHERE r.ReviewerUserId = @KocUserId AND r.RevieweeUserId = c.BrandUserId
UNION ALL
SELECT N'KOC chờ đánh giá Brand', COUNT(*)
FROM dbo.Participations p
JOIN #ReviewCampaigns c ON c.CampaignId = p.CampaignId
WHERE p.KocProfileId = @KocProfileId
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.Reviews r
      WHERE r.CampaignId = p.CampaignId AND r.ReviewerUserId = @KocUserId
  );
