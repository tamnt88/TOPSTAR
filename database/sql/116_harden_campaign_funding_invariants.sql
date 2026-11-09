/* TOPSTAR - Harden campaign funding invariants (safe to rerun). */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

CREATE OR ALTER TRIGGER dbo.TR_CampaignFundings_AllocationTotal
ON dbo.CampaignFundings AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN deleted d ON d.Id = i.Id
        OUTER APPLY
        (
            SELECT SUM(a.Amount) AllocatedAmount
            FROM dbo.CampaignFundingAllocations a
            WHERE a.FundingId = i.Id
        ) totals
        WHERE d.Status = N'Draft' AND i.Status <> N'Draft'
          AND ISNULL(totals.AllocatedAmount, 0) <> i.CampaignBudgetSnapshot
    )
        THROW 51000, N'Tổng thù lao phân bổ phải bằng đúng ngân sách chiến dịch trước khi gửi thanh toán.', 1;
END;
GO

PRINT N'OK - Đã bảo vệ tổng phân bổ quỹ chiến dịch.';
