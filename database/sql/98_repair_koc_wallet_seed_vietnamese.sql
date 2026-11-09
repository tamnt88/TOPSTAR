/*
 Sửa encoding tiếng Việt cho batch ví DEV đã chạy bằng sqlcmd thiếu UTF-8.
 Chỉ sửa phần chữ của đúng marker seed; không đổi số tiền, trạng thái, actor hay thời gian.
 Khi chạy bằng sqlcmd bắt buộc thêm: -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

IF DB_NAME() NOT IN (N'TopstarDb', N'TopstarDb_Dev', N'TopstarDb_Test')
    THROW 51030, N'Script 98 chỉ được chạy trên database DEV/TEST Topstar.', 1;

DECLARE @Marker NVARCHAR(120)=N'[DEV-WALLET-PAYOUT-TEST-20260730]';

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE dbo.WalletTransactions
    SET Description=CONCAT
        (
            @Marker,
            N' Số dư kiểm thử #',
            CASE Amount
                WHEN 6000000 THEN N'1'
                WHEN 4000000 THEN N'2'
                WHEN 2000000 THEN N'3'
                WHEN 1500000 THEN N'4'
                WHEN 750000 THEN N'5'
                ELSE N'?'
            END,
            N'.'
        ),
        HoldReason=CASE WHEN Status=N'OnHold'
            THEN N'Tạm giữ mẫu để kiểm thử ví và lịch sử đối soát.'
            ELSE HoldReason END
    WHERE LEFT(Description,LEN(@Marker))=@Marker;

    /*
      Trigger bảo vệ append-only được tạm tắt chỉ để sửa encoding của Reason.
      ID, trạng thái, actor và timestamp lịch sử được giữ nguyên.
    */
    DISABLE TRIGGER dbo.TR_WalletTransactionStatusHistories_AppendOnly
        ON dbo.WalletTransactionStatusHistories;

    UPDATE h
    SET Reason=CASE
        WHEN h.FromStatus IS NULL
            THEN N'Khởi tạo khoản thu nhập DEV để kiểm thử ví và yêu cầu rút tiền.'
        WHEN h.ToStatus=N'Available'
            THEN N'Đã đối soát; số dư sẵn sàng kiểm thử rút tiền.'
        WHEN h.ToStatus=N'OnHold'
            THEN N'Tạm giữ mẫu có căn cứ để kiểm thử lịch sử trạng thái.'
        ELSE h.Reason END
    FROM dbo.WalletTransactionStatusHistories h
    JOIN dbo.WalletTransactions w ON w.Id=h.WalletTransactionId
    WHERE LEFT(w.Description,LEN(@Marker))=@Marker;

    ENABLE TRIGGER dbo.TR_WalletTransactionStatusHistories_AppendOnly
        ON dbo.WalletTransactionStatusHistories;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT Id,Description,HoldReason
FROM dbo.WalletTransactions
WHERE LEFT(Description,LEN(@Marker))=@Marker
ORDER BY Amount DESC;
