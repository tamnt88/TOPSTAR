/* TOPSTAR - Persist the Brand's allocation preference. Safe to rerun. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF COL_LENGTH('dbo.Campaigns','AllocationMode') IS NULL
    ALTER TABLE dbo.Campaigns ADD AllocationMode nvarchar(10) NOT NULL CONSTRAINT DF_Campaigns_AllocationMode DEFAULT(N'Equal');
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.Campaigns') AND name='CK_Campaigns_AllocationMode')
    EXEC(N'ALTER TABLE dbo.Campaigns ADD CONSTRAINT CK_Campaigns_AllocationMode CHECK(AllocationMode IN(N''Equal'',N''Custom''));');
PRINT N'OK - Đã bổ sung lựa chọn phân bổ thù lao trên chiến dịch.';
