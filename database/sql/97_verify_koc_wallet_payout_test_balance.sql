/* Verify read-only cho seed ví/payout KOC. */
SET NOCOUNT ON;

DECLARE @Email NVARCHAR(256)=N'seed.koc.0001@seed.topstar.local';
DECLARE @Marker NVARCHAR(120)=N'[DEV-WALLET-PAYOUT-TEST-20260730]';
DECLARE @KocId UNIQUEIDENTIFIER;

SELECT @KocId=p.Id
FROM dbo.Users u JOIN dbo.KocProfiles p ON p.UserId=u.Id
WHERE LOWER(u.Email)=LOWER(@Email);

IF @KocId IS NULL THROW 51020, N'Không tìm thấy KOC test cần kiểm tra.', 1;

DECLARE @Reserved DECIMAL(18,2)=
(
    SELECT COALESCE(SUM(Amount),0) FROM dbo.PayoutRequests
    WHERE KocProfileId=@KocId AND Status IN (N'WithdrawRequested',N'PaidOut')
);

SELECT @Email KocEmail,
       SUM(CASE WHEN Status=N'Available' THEN Amount ELSE 0 END) AvailableLedger,
       @Reserved ReservedByPayout,
       SUM(CASE WHEN Status=N'Available' THEN Amount ELSE 0 END)-@Reserved WithdrawableBalance,
       SUM(CASE WHEN Status=N'Pending' THEN Amount ELSE 0 END) PendingBalance,
       SUM(CASE WHEN Status=N'OnHold' THEN Amount ELSE 0 END) OnHoldBalance,
       SUM(CASE WHEN Status<>N'Failed' THEN Amount ELSE 0 END) TotalRecordedIncome
FROM dbo.WalletTransactions WHERE KocProfileId=@KocId;

SELECT Id,Amount,Status,Description,HoldReason,AvailableAtUtc,CreatedAtUtc
FROM dbo.WalletTransactions
WHERE KocProfileId=@KocId AND LEFT(Description,LEN(@Marker))=@Marker
ORDER BY CreatedAtUtc DESC;

SELECT h.WalletTransactionId,h.FromStatus,h.ToStatus,h.Reason,h.CreatedAtUtc
FROM dbo.WalletTransactionStatusHistories h
JOIN dbo.WalletTransactions w ON w.Id=h.WalletTransactionId
WHERE w.KocProfileId=@KocId AND LEFT(w.Description,LEN(@Marker))=@Marker
ORDER BY h.CreatedAtUtc,h.Id;

SELECT CASE WHEN EXISTS
(
    SELECT 1 FROM dbo.KocPayoutAccounts WHERE KocProfileId=@KocId
) THEN N'Đã có'
  ELSE N'Chưa có - nhập trên giao diện KOC để dữ liệu được mã hóa đúng chuẩn' END
  AS PayoutAccountStatus;

SELECT TOP (1) Code,MinimumAmount,DailyRequestLimit,FixedFee,FeeRate,EffectiveAtUtc
FROM dbo.PayoutPolicies
WHERE EffectiveAtUtc<=SYSUTCDATETIME()
ORDER BY EffectiveAtUtc DESC,CreatedAtUtc DESC;
