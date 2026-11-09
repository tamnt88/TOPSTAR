SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.PayoutRequests', N'U') IS NULL
    THROW 51000, N'Không tìm thấy bảng dbo.PayoutRequests.', 1;
IF OBJECT_ID(N'dbo.PayoutOtpChallenges', N'U') IS NULL
    THROW 51000, N'Không tìm thấy bảng dbo.PayoutOtpChallenges.', 1;

-- Vô hiệu hóa OTP định dạng SHA-256 cũ trước khi chuyển sang token được mã hóa
-- bằng ASP.NET Data Protection. Người dùng chỉ cần yêu cầu OTP mới.
UPDATE dbo.PayoutOtpChallenges
SET ConsumedAtUtc = COALESCE(ConsumedAtUtc, SYSUTCDATETIME())
WHERE ConsumedAtUtc IS NULL;

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.PayoutOtpChallenges')
      AND name = N'CK_PayoutOtpChallenges_Hash')
    ALTER TABLE dbo.PayoutOtpChallenges DROP CONSTRAINT CK_PayoutOtpChallenges_Hash;

ALTER TABLE dbo.PayoutOtpChallenges ALTER COLUMN CodeHash NVARCHAR(500) NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.PayoutRequests') AND name = N'CK_PayoutRequests_WholeVnd')
    ALTER TABLE dbo.PayoutRequests WITH CHECK ADD CONSTRAINT CK_PayoutRequests_WholeVnd
        CHECK (Amount = FLOOR(Amount) AND Amount <= 9999999999999999);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.PayoutOtpChallenges') AND name = N'CK_PayoutOtpChallenges_WholeVnd')
    ALTER TABLE dbo.PayoutOtpChallenges WITH CHECK ADD CONSTRAINT CK_PayoutOtpChallenges_WholeVnd
        CHECK (Amount = FLOOR(Amount) AND Amount <= 9999999999999999);

-- Chốt cuối tại database: khóa theo hồ sơ KOC rồi từ chối mọi INSERT/UPDATE
-- khiến tổng tiền đã yêu cầu/đã chi lớn hơn tổng thu nhập khả dụng.
EXEC(N'
CREATE OR ALTER TRIGGER dbo.TR_PayoutRequests_PreventOverdraw
ON dbo.PayoutRequests
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @lockRows BIGINT;
    ;WITH AffectedKocs AS
    (
        SELECT KocProfileId FROM inserted
        UNION
        SELECT KocProfileId FROM deleted
    )
    SELECT @lockRows = COUNT_BIG(*)
    FROM dbo.KocProfiles k WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN AffectedKocs a ON a.KocProfileId = k.Id;

    IF EXISTS
    (
        SELECT 1
        FROM
        (
            SELECT KocProfileId FROM inserted
            UNION
            SELECT KocProfileId FROM deleted
        ) a
        OUTER APPLY
        (
            SELECT COALESCE(SUM(w.Amount), 0) AS AvailableAmount
            FROM dbo.WalletTransactions w WITH (HOLDLOCK)
            WHERE w.KocProfileId = a.KocProfileId
              AND w.Status = N''Available''
        ) available
        OUTER APPLY
        (
            SELECT COALESCE(SUM(p.Amount), 0) AS ReservedAmount
            FROM dbo.PayoutRequests p WITH (HOLDLOCK)
            WHERE p.KocProfileId = a.KocProfileId
              AND p.Status IN (N''WithdrawRequested'', N''PaidOut'')
        ) reserved
        WHERE reserved.ReservedAmount > available.AvailableAmount
    )
        THROW 51001, N''Yêu cầu rút tiền vượt quá số dư khả dụng của KOC.'', 1;
END');

IF EXISTS
(
    SELECT 1
    FROM dbo.KocProfiles k
    OUTER APPLY
    (
        SELECT COALESCE(SUM(w.Amount), 0) AS AvailableAmount
        FROM dbo.WalletTransactions w
        WHERE w.KocProfileId = k.Id AND w.Status = N'Available'
    ) available
    OUTER APPLY
    (
        SELECT COALESCE(SUM(p.Amount), 0) AS ReservedAmount
        FROM dbo.PayoutRequests p
        WHERE p.KocProfileId = k.Id AND p.Status IN (N'WithdrawRequested', N'PaidOut')
    ) reserved
    WHERE reserved.ReservedAmount > available.AvailableAmount
)
    THROW 51002, N'Dữ liệu hiện tại đã có KOC rút vượt số dư; migration đã bị hủy để kiểm tra thủ công.', 1;

COMMIT TRANSACTION;
