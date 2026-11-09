/*
 Sửa tiếng Việt của bộ seed Reviews từng được chạy sai code page.
 Bắt buộc chạy bằng sqlcmd với tham số -f 65001.
 Script idempotent và chỉ sửa các ID thuộc bộ seed Reviews.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @KocUserId UNIQUEIDENTIFIER;
DECLARE @KocProfileId UNIQUEIDENTIFIER;

SELECT TOP (1) @KocUserId = u.Id, @KocProfileId = k.Id
FROM dbo.Users u
JOIN dbo.KocProfiles k ON k.UserId = u.Id
WHERE u.Email = 'seed.koc.0001@seed.topstar.local';

IF @KocUserId IS NULL
    THROW 51001, N'Không tìm thấy tài khoản KOC seed.', 1;

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

BEGIN TRY
BEGIN TRANSACTION;

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
DECLARE @ReceivedCount INT = @@ROWCOUNT;

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
DECLARE @SentCount INT = @@ROWCOUNT;

/*
 Hai bảng lịch sử là append-only. Chỉ tạm tắt đúng hai trigger trong transaction
 để sửa các ID seed xác định; trigger được bật lại trước khi commit.
*/
DISABLE TRIGGER dbo.TR_ReviewReplies_AppendOnly ON dbo.ReviewReplies;
DISABLE TRIGGER dbo.TR_ReviewStatusHistories_AppendOnly ON dbo.ReviewStatusHistories;

UPDATE rr
SET Body = N'Cảm ơn Brand đã phản hồi. Mình đã ghi nhận góp ý để cải thiện các lần hợp tác tiếp theo.'
FROM dbo.ReviewReplies rr
JOIN dbo.Reviews r ON r.Id = rr.ReviewId
JOIN #ReviewCampaigns c ON c.CampaignId = r.CampaignId
WHERE rr.Id = CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT('TOPSTAR-KOC-REVIEW-TAB-REPLY-', r.Id)));
DECLARE @ReplyCount INT = @@ROWCOUNT;

UPDATE h
SET Reason = N'Dữ liệu DEV/QA phục vụ kiểm thử DataTables và modal chi tiết.'
FROM dbo.ReviewStatusHistories h
JOIN dbo.Reviews r ON r.Id = h.ReviewId
JOIN #ReviewCampaigns c ON c.CampaignId = r.CampaignId
WHERE h.Id = CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT('TOPSTAR-KOC-REVIEW-TAB-HISTORY-', r.Id)));
DECLARE @HistoryCount INT = @@ROWCOUNT;

ENABLE TRIGGER dbo.TR_ReviewReplies_AppendOnly ON dbo.ReviewReplies;
ENABLE TRIGGER dbo.TR_ReviewStatusHistories_AppendOnly ON dbo.ReviewStatusHistories;

COMMIT TRANSACTION;

SELECT @ReceivedCount AS ReceivedReviewsRepaired,
       @SentCount AS SentReviewsRepaired,
       @ReplyCount AS RepliesRepaired,
       @HistoryCount AS HistoriesRepaired;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;

    IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_ReviewReplies_AppendOnly' AND is_disabled = 1)
        ENABLE TRIGGER dbo.TR_ReviewReplies_AppendOnly ON dbo.ReviewReplies;

    IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TR_ReviewStatusHistories_AppendOnly' AND is_disabled = 1)
        ENABLE TRIGGER dbo.TR_ReviewStatusHistories_AppendOnly ON dbo.ReviewStatusHistories;

    THROW;
END CATCH;
