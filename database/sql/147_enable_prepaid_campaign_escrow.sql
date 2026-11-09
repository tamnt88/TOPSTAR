/* Cho phép Brand ký quỹ trước toàn bộ ngân sách rồi phân bổ KOC sau. */
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
        JOIN deleted d ON d.Id=i.Id
        OUTER APPLY (SELECT SUM(a.Amount) AllocatedAmount FROM dbo.CampaignFundingAllocations a WHERE a.FundingId=i.Id) totals
        WHERE d.Status=N'Draft' AND i.Status<>N'Draft'
          AND i.AllocationMode<>N'Prepaid'
          AND ISNULL(totals.AllocatedAmount,0)<>i.CampaignBudgetSnapshot
    )
        THROW 51000,N'Tổng thù lao phân bổ phải bằng đúng ngân sách đợt trước khi gửi thanh toán.',1;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_CampaignFundingAllocations_Immutable
ON dbo.CampaignFundingAllocations AFTER INSERT,UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1 FROM deleted d
        LEFT JOIN inserted i ON i.Id=d.Id
        JOIN dbo.CampaignFundings f ON f.Id=d.FundingId
        WHERE i.Id IS NULL AND f.Status<>N'Draft'
    )
    OR EXISTS
    (
        SELECT 1 FROM inserted i
        LEFT JOIN deleted d ON d.Id=i.Id
        JOIN dbo.CampaignFundings f ON f.Id=i.FundingId
        WHERE d.Id IS NULL AND f.Status<>N'Draft'
          AND NOT(f.AllocationMode=N'Prepaid' AND f.Status IN(N'Paid',N'PartiallyReleased',N'Released'))
    )
    OR EXISTS
    (
        SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
        WHERE i.FundingId<>d.FundingId OR i.ParticipationId<>d.ParticipationId OR i.Amount<>d.Amount OR i.CreatedAtUtc<>d.CreatedAtUtc
    )
        THROW 51000,N'Khoản phân bổ đã chốt không được sửa hoặc xóa.',1;

    IF EXISTS
    (
        SELECT 1 FROM inserted i
        JOIN dbo.CampaignFundings f ON f.Id=i.FundingId
        CROSS APPLY (SELECT SUM(a.Amount) TotalAllocated FROM dbo.CampaignFundingAllocations a WHERE a.FundingId=f.Id) t
        WHERE t.TotalAllocated>f.CampaignBudgetSnapshot
    )
        THROW 51000,N'Tổng thù lao vượt quá số tiền đã ký quỹ.',1;

    IF EXISTS
    (
        SELECT 1
        FROM inserted i
        JOIN dbo.CampaignFundings fi ON fi.Id=i.FundingId
        JOIN dbo.CampaignFundingAllocations a ON a.ParticipationId=i.ParticipationId AND a.Id<>i.Id
        JOIN dbo.CampaignFundings fa ON fa.Id=a.FundingId AND fa.CampaignId=fi.CampaignId
        WHERE fi.Status<>N'Cancelled' AND fa.Status<>N'Cancelled'
    )
        THROW 51000,N'KOC đã có khoản thù lao trong một đợt ký quỹ khác.',1;
END;
GO

PRINT N'OK - Đã bật ký quỹ trước toàn bộ ngân sách và phân bổ KOC an toàn từ số dư.';
/* Keep the mode constraint aligned with the prepaid triggers below. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.CampaignFundings', N'U') IS NULL
    THROW 51000, N'Thiếu bảng dbo.CampaignFundings. Hãy chạy script 110 trước.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.CampaignFundings') AND name=N'CK_CampaignFundings_Mode')
        ALTER TABLE dbo.CampaignFundings DROP CONSTRAINT CK_CampaignFundings_Mode;
    ALTER TABLE dbo.CampaignFundings WITH CHECK ADD CONSTRAINT CK_CampaignFundings_Mode
        CHECK (AllocationMode IN (N'Equal', N'Custom', N'Prepaid'));
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
