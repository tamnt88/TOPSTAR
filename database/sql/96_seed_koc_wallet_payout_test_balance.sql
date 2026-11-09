/*
 DEV/TEST only - seed số dư ví cho seed.koc.0001@seed.topstar.local.
 Bổ sung: 12.000.000 khả dụng, 1.500.000 đối soát, 750.000 tạm giữ.
 Chạy lặp an toàn; không tạo payout và không sửa/xóa lịch sử cũ.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF DB_NAME() NOT IN (N'TopstarDb', N'TopstarDb_Dev', N'TopstarDb_Test')
    THROW 51000, N'Script 96 chỉ được chạy trên database DEV/TEST Topstar.', 1;

IF OBJECT_ID(N'dbo.WalletTransactions', N'U') IS NULL
   OR OBJECT_ID(N'dbo.WalletTransactionStatusHistories', N'U') IS NULL
    THROW 51001, N'Thiếu schema ví hoặc lịch sử. Hãy chạy migration đến script 95 trước.', 1;

DECLARE @Email NVARCHAR(256) = N'seed.koc.0001@seed.topstar.local';
DECLARE @Marker NVARCHAR(120) = N'[DEV-WALLET-PAYOUT-TEST-20260730]';
DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();
DECLARE @KocId UNIQUEIDENTIFIER;
DECLARE @AdminId UNIQUEIDENTIFIER;

SELECT @KocId = p.Id
FROM dbo.Users u
JOIN dbo.KocProfiles p ON p.UserId = u.Id
WHERE LOWER(u.Email) = LOWER(@Email);

SELECT TOP (1) @AdminId = u.Id
FROM dbo.Users u
JOIN dbo.UserRoles ur ON ur.UserId = u.Id
JOIN dbo.Roles r ON r.Id = ur.RoleId
WHERE UPPER(r.Code) = N'ADMIN'
ORDER BY u.CreatedAtUtc, u.Id;

IF @KocId IS NULL
    THROW 51002, N'Không tìm thấy KOC test. Hãy chạy script 84 trước.', 1;
IF @AdminId IS NULL
    THROW 51003, N'Không tìm thấy Admin để ghi nhận người tạo lịch sử.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.WalletTransactions
        WHERE KocProfileId = @KocId
          AND LEFT(Description, LEN(@Marker)) = @Marker
    )
    BEGIN
        DROP TABLE IF EXISTS #Plan;
        CREATE TABLE #Plan
        (
            RowNo INT NOT NULL PRIMARY KEY,
            ParticipationId UNIQUEIDENTIFIER NOT NULL,
            CampaignId UNIQUEIDENTIFIER NOT NULL,
            Amount DECIMAL(18,2) NOT NULL,
            FinalStatus NVARCHAR(30) NOT NULL,
            HoldReason NVARCHAR(500) NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL
        );

        ;WITH FreeRows AS
        (
            SELECT p.Id ParticipationId, p.CampaignId,
                   ROW_NUMBER() OVER (ORDER BY p.CreatedAtUtc DESC, p.Id) RowNo
            FROM dbo.Participations p
            WHERE p.KocProfileId = @KocId
              AND NOT EXISTS
                  (SELECT 1 FROM dbo.WalletTransactions w WHERE w.ParticipationId = p.Id)
        )
        INSERT #Plan
        SELECT RowNo, ParticipationId, CampaignId,
               CASE RowNo WHEN 1 THEN 6000000 WHEN 2 THEN 4000000
                    WHEN 3 THEN 2000000 WHEN 4 THEN 1500000 ELSE 750000 END,
               CASE WHEN RowNo <= 3 THEN N'Available'
                    WHEN RowNo = 4 THEN N'Pending' ELSE N'OnHold' END,
               CASE WHEN RowNo = 5
                    THEN N'Tạm giữ mẫu để kiểm thử ví và lịch sử đối soát.' END,
               DATEADD(MINUTE, -RowNo, @Now)
        FROM FreeRows
        WHERE RowNo <= 5;

        IF (SELECT COUNT(*) FROM #Plan) < 5
            THROW 51004, N'KOC test không còn đủ 5 lượt hợp tác chưa gắn giao dịch ví.', 1;

        INSERT dbo.WalletTransactions
        (
            Id, KocProfileId, CampaignId, ParticipationId, TransactionType,
            Amount, Status, Description, HoldReason, CreatedByUserId,
            AvailableAtUtc, CreatedAtUtc, UpdatedAtUtc
        )
        SELECT
            CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT(@Marker, N'-TX-', RowNo))),
            @KocId, CampaignId, ParticipationId, N'Earning', Amount, FinalStatus,
            CONCAT(@Marker, N' Số dư kiểm thử #', RowNo, N'.'),
            HoldReason, @AdminId,
            CASE WHEN FinalStatus = N'Available' THEN CreatedAtUtc END,
            CreatedAtUtc, CreatedAtUtc
        FROM #Plan;

        -- Lịch sử bắt đầu từ Pending.
        INSERT dbo.WalletTransactionStatusHistories
            (Id, WalletTransactionId, FromStatus, ToStatus, Reason, ActorUserId, CreatedAtUtc)
        SELECT
            CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT(@Marker, N'-H-PENDING-', RowNo))),
            CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT(@Marker, N'-TX-', RowNo))),
            NULL, N'Pending',
            N'Khởi tạo khoản thu nhập DEV để kiểm thử ví và payout.',
            @AdminId, DATEADD(SECOND, -1, CreatedAtUtc)
        FROM #Plan;

        -- Ghi thêm chuyển trạng thái; không thay thế snapshot lịch sử.
        INSERT dbo.WalletTransactionStatusHistories
            (Id, WalletTransactionId, FromStatus, ToStatus, Reason, ActorUserId, CreatedAtUtc)
        SELECT
            CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT(@Marker, N'-H-FINAL-', RowNo))),
            CONVERT(UNIQUEIDENTIFIER, HASHBYTES('MD5', CONCAT(@Marker, N'-TX-', RowNo))),
            N'Pending', FinalStatus,
            CASE FinalStatus
                WHEN N'Available' THEN N'Đã đối soát; số dư sẵn sàng kiểm thử rút tiền.'
                ELSE N'Tạm giữ mẫu có căn cứ để kiểm thử lịch sử trạng thái.' END,
            @AdminId, CreatedAtUtc
        FROM #Plan
        WHERE FinalStatus IN (N'Available', N'OnHold');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

DECLARE @Reserved DECIMAL(18,2) =
(
    SELECT COALESCE(SUM(Amount), 0)
    FROM dbo.PayoutRequests
    WHERE KocProfileId = @KocId AND Status IN (N'WithdrawRequested', N'PaidOut')
);

SELECT @Email KocEmail,
       SUM(CASE WHEN Status=N'Available' THEN Amount ELSE 0 END) AvailableLedger,
       @Reserved ReservedByPayout,
       SUM(CASE WHEN Status=N'Available' THEN Amount ELSE 0 END)-@Reserved WithdrawableBalance,
       SUM(CASE WHEN Status=N'Pending' THEN Amount ELSE 0 END) PendingBalance,
       SUM(CASE WHEN Status=N'OnHold' THEN Amount ELSE 0 END) OnHoldBalance
FROM dbo.WalletTransactions
WHERE KocProfileId=@KocId;

PRINT N'Seed ví KOC hoàn tất. Chạy script 97 để kiểm tra chi tiết.';
