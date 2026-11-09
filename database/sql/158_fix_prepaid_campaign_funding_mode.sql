/* TOPSTAR - Allow prepaid escrow mode. Safe to rerun; preserves all amounts and allocations.
   Run script 147 as well when deploying prepaid escrow for the first time. */
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
SELECT name, definition, is_disabled, is_not_trusted
FROM sys.check_constraints
WHERE parent_object_id=OBJECT_ID(N'dbo.CampaignFundings') AND name=N'CK_CampaignFundings_Mode';
GO

