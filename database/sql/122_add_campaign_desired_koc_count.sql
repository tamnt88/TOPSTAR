SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.Campaigns', 'DesiredKocCount') IS NULL
    BEGIN
        ALTER TABLE dbo.Campaigns
            ADD DesiredKocCount int NOT NULL
                CONSTRAINT DF_Campaigns_DesiredKocCount DEFAULT (1) WITH VALUES;
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID('dbo.Campaigns')
          AND name = 'CK_Campaigns_DesiredKocCount'
    )
    BEGIN
        EXEC(N'ALTER TABLE dbo.Campaigns WITH CHECK
            ADD CONSTRAINT CK_Campaigns_DesiredKocCount
                CHECK (DesiredKocCount BETWEEN 1 AND 100);');
    END;

    EXEC sp_executesql N'
        UPDATE dbo.Campaigns
           SET DesiredKocCount = 2
         WHERE Title LIKE N''[QA Quỹ%''
           AND DesiredKocCount = 1;';

    COMMIT TRANSACTION;
    PRINT N'OK - Đã bổ sung số lượng KOC mong muốn cho chiến dịch.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
