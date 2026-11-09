SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.KocCategoryHistories', 'KocCategoryId') IS NOT NULL
       AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.KocCategoryHistories') AND name = 'KocCategoryId' AND is_nullable = 0)
        ALTER TABLE dbo.KocCategoryHistories ALTER COLUMN KocCategoryId UNIQUEIDENTIFIER NULL;

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('dbo.KocCategoryHistories') AND name = 'FK_KocCategoryHistories_KocCategories')
        ALTER TABLE dbo.KocCategoryHistories DROP CONSTRAINT FK_KocCategoryHistories_KocCategories;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('dbo.KocCategoryHistories') AND name = 'FK_KocCategoryHistories_KocCategories')
        ALTER TABLE dbo.KocCategoryHistories ADD CONSTRAINT FK_KocCategoryHistories_KocCategories
            FOREIGN KEY(KocCategoryId) REFERENCES dbo.KocCategories(Id) ON DELETE SET NULL;

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.KocCategoryHistories') AND name = 'CK_KocCategoryHistories_Action')
        ALTER TABLE dbo.KocCategoryHistories DROP CONSTRAINT CK_KocCategoryHistories_Action;

    ALTER TABLE dbo.KocCategoryHistories WITH CHECK ADD CONSTRAINT CK_KocCategoryHistories_Action
        CHECK(ActionName IN('Create','Update','Activate','Deactivate','Delete'));

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
GO
