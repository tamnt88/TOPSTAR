SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
CREATE OR ALTER TRIGGER dbo.TR_CampaignFundingAllocations_Immutable
ON dbo.CampaignFundingAllocations AFTER INSERT,UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id JOIN dbo.CampaignFundings f ON f.Id=d.FundingId WHERE i.Id IS NULL AND f.Status<>N'Draft')
 OR EXISTS(SELECT 1 FROM inserted i LEFT JOIN deleted d ON d.Id=i.Id JOIN dbo.CampaignFundings f ON f.Id=i.FundingId WHERE d.Id IS NULL AND f.Status<>N'Draft')
 OR EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.FundingId<>d.FundingId OR i.ParticipationId<>d.ParticipationId OR i.Amount<>d.Amount OR i.CreatedAtUtc<>d.CreatedAtUtc)
 BEGIN THROW 51000,N'Chỉ bảng phân bổ nháp được phép lập lại; snapshot đã chốt không được sửa hoặc xóa.',1; END
END;
GO
PRINT N'OK - Có thể lập lại bảng phân bổ khi quỹ còn ở trạng thái nháp.';
